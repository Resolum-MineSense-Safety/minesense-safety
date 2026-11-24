namespace MineSenseSafety.Shared.Domain.Model;

/// <summary>
/// Raised when an operation refers to a resource that does not exist
/// (e.g. registering a vehicle in an unknown fleet). Mapped to HTTP 404.
/// </summary>
public class DomainNotFoundException(string message) : DomainException(message);
