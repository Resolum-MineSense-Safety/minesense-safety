namespace MineSenseSafety.Shared.Domain.Model;

/// <summary>
/// Raised when a request lacks mandatory data or sends malformed values
/// (e.g. a missing employee code). Mapped to HTTP 400.
/// </summary>
public class DomainValidationException(string message) : DomainException(message);
