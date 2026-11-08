"""HTTP client for the Edge Synchronization Service (TS-13).

POST /api/v1/synchronization-batches with SubmitSynchronizationBatchResource:
BatchId, EdgeNodeId, DeviceId, SequenceNumber, CapturedAt and Events. The
service answers 202 Accepted for a new batch and 200 OK for a BatchId it has
already stored, so resending a batch after a lost answer is safe.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass

import requests

from .auth import NoAuth
from .buffer import SealedBatch
from .errors import CloudAuthError, CloudRejectedError, CloudUnavailableError

logger = logging.getLogger(__name__)

SYNCHRONIZATION_BATCHES_PATH = "/api/v1/synchronization-batches"
RETRYABLE_STATUS = {408, 425, 429}


@dataclass(frozen=True)
class SubmitResult:
    batch_id: str
    duplicate: bool
    status: str | None


class SyncClient:
    def __init__(self, sync_service_url: str, edge_node_id: str, device_id: str,
                 token_provider=None, timeout_seconds: float = 3.0,
                 session: requests.Session | None = None) -> None:
        self._url = sync_service_url.rstrip("/") + SYNCHRONIZATION_BATCHES_PATH
        self._edge_node_id = edge_node_id
        self._device_id = device_id
        self._tokens = token_provider or NoAuth()
        self._timeout = timeout_seconds
        self._session = session or requests.Session()

    def to_resource(self, batch: SealedBatch) -> dict:
        return {
            "batchId": batch.batch_id,
            "edgeNodeId": self._edge_node_id,
            "deviceId": self._device_id,
            "sequenceNumber": batch.sequence_number,
            "capturedAt": batch.captured_at,
            "events": [event.to_resource() for event in batch.events],
        }

    def submit(self, batch: SealedBatch) -> SubmitResult:
        body = self.to_resource(batch)
        response = self._post(body)
        if response.status_code == 401:
            # Expired or revoked token: renew it once before giving up.
            self._tokens.invalidate()
            response = self._post(body)
            if response.status_code == 401:
                raise CloudAuthError("Edge Synchronization Service rejected the access token (401)")
        if response.status_code == 403:
            raise CloudAuthError("The edge user is not allowed to synchronize batches (403)")
        if response.status_code >= 500 or response.status_code in RETRYABLE_STATUS:
            raise CloudUnavailableError(f"Edge Synchronization Service returned {response.status_code}")
        if response.status_code >= 400:
            raise CloudRejectedError(
                f"Edge Synchronization Service rejected batch {batch.batch_id} "
                f"({response.status_code}): {response.text[:200]}")
        try:
            payload = response.json()
        except ValueError:
            payload = {}
        return SubmitResult(batch.batch_id, response.status_code == 200, payload.get("status"))

    def _post(self, body: dict) -> requests.Response:
        headers = self._tokens.authorization_header()
        try:
            return self._session.post(self._url, json=body, headers=headers, timeout=self._timeout)
        except requests.RequestException as error:
            raise CloudUnavailableError(f"POST {self._url} failed: {error}") from error
