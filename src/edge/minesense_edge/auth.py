"""Access tokens for the cloud services (Identity & Access, RS256, ADR-007).

Three modes, chosen from the environment (secrets are never stored in code):

* ``EDGE_ACCESS_TOKEN``: a token issued beforehand; used as is.
* ``IDENTITY_URL`` + ``EDGE_USERNAME`` + ``EDGE_PASSWORD``: the agent signs in
  at ``/api/v1/authentication/sign-in``, caches the token until shortly before
  it expires and renews it with the refresh token.
* none of them: no Authorization header (services started without
  ``Authentication:IdentityUrl`` stay open, as in the local API tests).
"""

from __future__ import annotations

import logging
import time
from datetime import datetime
from typing import Callable

import requests

from .errors import CloudAuthError, CloudUnavailableError

logger = logging.getLogger(__name__)

SIGN_IN_PATH = "/api/v1/authentication/sign-in"
REFRESH_PATH = "/api/v1/authentication/refresh"
RENEW_BEFORE_SECONDS = 30


class NoAuth:
    def authorization_header(self) -> dict:
        return {}

    def invalidate(self) -> None:
        return None


class StaticTokenProvider:
    def __init__(self, token: str) -> None:
        self._token = token

    def authorization_header(self) -> dict:
        return {"Authorization": f"Bearer {self._token}"}

    def invalidate(self) -> None:
        # A fixed token cannot be renewed; the 401 is reported as CloudAuthError upstream.
        return None


class PasswordTokenProvider:
    def __init__(self, identity_url: str, username: str, password: str,
                 session: requests.Session | None = None, timeout_seconds: float = 3.0,
                 clock: Callable[[], float] = time.time) -> None:
        base = identity_url.rstrip("/")
        self._sign_in_url = base + SIGN_IN_PATH
        self._refresh_url = base + REFRESH_PATH
        self._username = username
        self._password = password
        self._session = session or requests.Session()
        self._timeout = timeout_seconds
        self._clock = clock
        self._access_token: str | None = None
        self._refresh_token: str | None = None
        self._expires_at = 0.0

    def authorization_header(self) -> dict:
        if self._access_token is None or self._clock() >= self._expires_at - RENEW_BEFORE_SECONDS:
            self._renew()
        return {"Authorization": f"Bearer {self._access_token}"}

    def invalidate(self) -> None:
        self._access_token = None

    def _renew(self) -> None:
        if self._refresh_token:
            try:
                self._store(self._post(self._refresh_url, {"refreshToken": self._refresh_token}))
                return
            except CloudAuthError:
                logger.info("Refresh token rejected, signing in again")
                self._refresh_token = None
        self._store(self._post(self._sign_in_url,
                               {"username": self._username, "password": self._password}))
        logger.info("Signed in to Identity & Access as %s", self._username)

    def _store(self, body: dict) -> None:
        try:
            self._access_token = body["accessToken"]
        except (KeyError, TypeError) as error:
            raise CloudAuthError("Identity & Access answered without an access token") from error
        self._refresh_token = body.get("refreshToken")
        self._expires_at = self._expiry(body)

    def _expiry(self, body: dict) -> float:
        expires_at = body.get("accessTokenExpiresAt")
        if expires_at:
            try:
                return datetime.fromisoformat(expires_at.replace("Z", "+00:00")).timestamp()
            except ValueError:
                pass
        return self._clock() + float(body.get("expiresIn") or 300)

    def _post(self, url: str, body: dict) -> dict:
        try:
            response = self._session.post(url, json=body, timeout=self._timeout)
        except requests.RequestException as error:
            raise CloudUnavailableError(f"Identity & Access unreachable: {error}") from error
        if response.status_code in (400, 401, 403):
            raise CloudAuthError(f"Identity & Access refused the edge credentials ({response.status_code})")
        if response.status_code >= 300:
            raise CloudUnavailableError(f"Identity & Access returned {response.status_code}")
        return response.json()


def create_token_provider(identity_url: str | None, access_token: str | None,
                          username: str | None, password: str | None,
                          session: requests.Session | None = None, timeout_seconds: float = 3.0):
    if access_token:
        return StaticTokenProvider(access_token)
    if identity_url and username and password:
        return PasswordTokenProvider(identity_url, username, password, session, timeout_seconds)
    if username or password:
        logger.warning("EDGE_USERNAME/EDGE_PASSWORD need IDENTITY_URL; sending requests without a token")
    return NoAuth()
