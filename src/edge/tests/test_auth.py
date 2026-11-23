from unittest.mock import MagicMock

import pytest
import requests

from minesense_edge.auth import (NoAuth, PasswordTokenProvider, StaticTokenProvider,
                                 create_token_provider)
from minesense_edge.errors import CloudAuthError, CloudUnavailableError


def response(status, body=None):
    mock = MagicMock()
    mock.status_code = status
    mock.json.return_value = body or {}
    return mock


def token_body(token, expires_in=900, refresh="r-1"):
    return {"accessToken": token, "tokenType": "Bearer", "expiresIn": expires_in, "refreshToken": refresh}


def test_mode_is_chosen_from_configuration():
    assert isinstance(create_token_provider(None, None, None, None), NoAuth)
    assert isinstance(create_token_provider("http://iam", "tok", "u", "p"), StaticTokenProvider)
    assert isinstance(create_token_provider("http://iam", None, "u", "p"), PasswordTokenProvider)
    assert isinstance(create_token_provider(None, None, "u", "p"), NoAuth)


def test_signs_in_once_and_caches_the_token():
    session = MagicMock()
    session.post.return_value = response(200, token_body("t-1"))
    provider = PasswordTokenProvider("http://iam/", "edge", "secret", session=session, clock=lambda: 1000)

    assert provider.authorization_header() == {"Authorization": "Bearer t-1"}
    assert provider.authorization_header() == {"Authorization": "Bearer t-1"}
    session.post.assert_called_once()
    url = session.post.call_args.args[0]
    assert url == "http://iam/api/v1/authentication/sign-in"
    assert session.post.call_args.kwargs["json"] == {"username": "edge", "password": "secret"}


def test_renews_with_refresh_token_before_expiry():
    now = [1000.0]
    session = MagicMock()
    session.post.side_effect = [response(200, token_body("t-1", 60)), response(200, token_body("t-2"))]
    provider = PasswordTokenProvider("http://iam", "edge", "secret", session=session, clock=lambda: now[0])

    provider.authorization_header()
    now[0] += 45  # inside the 30 s renewal window
    assert provider.authorization_header() == {"Authorization": "Bearer t-2"}
    assert session.post.call_args.args[0] == "http://iam/api/v1/authentication/refresh"
    assert session.post.call_args.kwargs["json"] == {"refreshToken": "r-1"}


def test_rejected_refresh_falls_back_to_sign_in():
    session = MagicMock()
    session.post.side_effect = [response(200, token_body("t-1")), response(401), response(200, token_body("t-3"))]
    provider = PasswordTokenProvider("http://iam", "edge", "secret", session=session, clock=lambda: 0)

    provider.authorization_header()
    provider.invalidate()
    assert provider.authorization_header() == {"Authorization": "Bearer t-3"}
    assert session.post.call_args.args[0].endswith("/sign-in")


def test_wrong_credentials_raise_auth_error():
    session = MagicMock()
    session.post.return_value = response(401)
    with pytest.raises(CloudAuthError):
        PasswordTokenProvider("http://iam", "edge", "bad", session=session).authorization_header()


def test_identity_unreachable_is_unavailable():
    session = MagicMock()
    session.post.side_effect = requests.ConnectionError("down")
    with pytest.raises(CloudUnavailableError):
        PasswordTokenProvider("http://iam", "edge", "secret", session=session).authorization_header()


def test_expiry_uses_access_token_expires_at_when_present():
    session = MagicMock()
    session.post.return_value = response(200, {**token_body("t-1"),
                                               "accessTokenExpiresAt": "2026-11-01T10:15:00Z"})
    provider = PasswordTokenProvider("http://iam", "edge", "secret", session=session, clock=lambda: 0)
    provider.authorization_header()
    assert provider._expires_at == pytest.approx(1793528100.0)
