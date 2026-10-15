namespace MineSenseSafety.Shared.Domain.Model;

/// <summary>
/// Raised when an operation contradicts the current state of an aggregate
/// (e.g. assigning an incident another supervisor already took). Mapped to HTTP 409.
/// </summary>
public class DomainConflictException(string message) : DomainException(message);
