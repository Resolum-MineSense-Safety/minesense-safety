"""Errors raised when synchronizing with the cloud."""


class CloudUnavailableError(Exception):
    """The cloud could not be reached, timed out or answered 5xx/408/429; retry later."""


class CloudAuthError(CloudUnavailableError):
    """Identity & Access refused the edge credentials or token.

    It is a kind of unavailability on purpose: pending events must never be
    discarded because a token expired or credentials were rotated.
    """


class CloudRejectedError(Exception):
    """The cloud refused the payload (400/422); retrying the same batch will not help."""
