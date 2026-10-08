"""Errors raised when synchronizing with the cloud."""


class CloudUnavailableError(Exception):
    """The cloud could not be reached or answered 5xx; retry later."""


class CloudRejectedError(Exception):
    """The cloud refused the payload (4xx); retrying will not help."""
