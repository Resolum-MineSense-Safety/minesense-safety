"""In-process fake of Identity & Access + Edge Synchronization Service for tests.

It mirrors the real contract: POST /api/v1/synchronization-batches answers 202
for a new BatchId and 200 for a known one, validates the required fields and
the 1..500 events rule, and requires a Bearer token when ``require_token`` is set.
Modes simulate the network: ``online``, ``down`` (503) and ``drop_after_store``
(stores the batch, then closes the socket before answering).
"""

from __future__ import annotations

import json
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

REQUIRED = ("batchId", "edgeNodeId", "deviceId", "sequenceNumber", "capturedAt", "events")


class FakeCloud:
    def __init__(self, require_token: bool = False, username: str = "edge", password: str = "edge-pass") -> None:
        self.require_token = require_token
        self.username = username
        self.password = password
        self.mode = "online"
        self.go_down_after: int | None = None  # accept N new batches, then answer 503
        self.batches: dict[str, dict] = {}
        self.order: list[str] = []
        self.requests: list[dict] = []
        self.sign_ins = 0
        self.valid_tokens: set[str] = set()
        self._lock = threading.Lock()
        self._server = ThreadingHTTPServer(("127.0.0.1", 0), self._handler())
        self._thread = threading.Thread(target=self._server.serve_forever, daemon=True)

    @property
    def url(self) -> str:
        host, port = self._server.server_address[:2]
        return f"http://{host}:{port}"

    def start(self) -> "FakeCloud":
        self._thread.start()
        return self

    def stop(self) -> None:
        self._server.shutdown()
        self._server.server_close()

    def stored_event_ids(self) -> list[str]:
        return [event["eventId"] for batch_id in self.order for event in self.batches[batch_id]["events"]]

    def revoke_tokens(self) -> None:
        self.valid_tokens.clear()

    def _handler(self):
        cloud = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):  # silence test output
                return None

            def do_POST(self):  # noqa: N802
                length = int(self.headers.get("Content-Length") or 0)
                body = json.loads(self.rfile.read(length) or b"{}")
                if self.path == "/api/v1/authentication/sign-in":
                    return self._sign_in(body)
                if self.path == "/api/v1/synchronization-batches":
                    return self._submit(body)
                self._reply(404, {})

            def _sign_in(self, body):
                if body.get("username") != cloud.username or body.get("password") != cloud.password:
                    return self._reply(401, {"message": "Invalid credentials."})
                with cloud._lock:
                    cloud.sign_ins += 1
                    token = f"token-{cloud.sign_ins}"
                    cloud.valid_tokens.add(token)
                self._reply(200, {"accessToken": token, "tokenType": "Bearer", "expiresIn": 900,
                                  "refreshToken": None})

            def _submit(self, body):
                cloud.requests.append({"headers": dict(self.headers), "body": body})
                if cloud.go_down_after is not None and len(cloud.batches) >= cloud.go_down_after:
                    cloud.mode = "down"
                if cloud.mode == "down":
                    return self._reply(503, {})
                if cloud.require_token:
                    auth = self.headers.get("Authorization", "")
                    if not auth.startswith("Bearer ") or auth[7:] not in cloud.valid_tokens:
                        return self._reply(401, {})
                if any(field not in body for field in REQUIRED) or not 1 <= len(body["events"]) <= 500:
                    return self._reply(400, {"title": "One or more validation errors occurred."})
                with cloud._lock:
                    duplicate = body["batchId"] in cloud.batches
                    if not duplicate:
                        cloud.batches[body["batchId"]] = body
                        cloud.order.append(body["batchId"])
                if cloud.mode == "drop_after_store":
                    # The batch reached the service but the answer is lost on the way back.
                    self.close_connection = True
                    self.connection.shutdown(2)
                    return None
                self._reply(200 if duplicate else 202, {**body, "status": "Accepted"})

            def _reply(self, status, payload):
                data = json.dumps(payload).encode()
                self.send_response(status)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)

        return Handler
