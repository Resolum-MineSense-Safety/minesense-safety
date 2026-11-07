namespace IdentityAccessService.Domain.Model.ValueObjects;

/// <summary>Responsibility of a user; determines which features are available (EP08).</summary>
public enum UserRole
{
    Operator,
    Supervisor,
    SafetyManager,
    Administrator
}
