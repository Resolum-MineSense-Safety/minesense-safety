namespace MineSenseSafety.Shared.Domain.Model;

/// <summary>
/// Raised when an operation would break a business invariant of an aggregate.
/// </summary>
public class DomainException(string message) : Exception(message);
