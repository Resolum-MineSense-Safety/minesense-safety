using IdentityAccessService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Domain.Model.Aggregates;

/// <summary>
/// Person who can sign in to MineSense (EP08). The role restricts features by responsibility,
/// the mining units limit the operational scope (US18) and only a hash of the password is stored.
/// </summary>
public class User : AggregateRoot
{
    private readonly HashSet<Guid> _miningUnitIds = [];

    public string Username { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Mining units (Operations bounded context) the user is allowed to act on.</summary>
    public IReadOnlyCollection<Guid> MiningUnitIds => _miningUnitIds;

    public User(string username, string fullName, UserRole role, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("A user must have a username.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("A user must have a full name.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A user must have a password hash.");

        Username = username.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        Role = role;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public void ChangeRole(UserRole role) => Role = role;

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("The user is already deactivated.");

        IsActive = false;
    }

    /// <summary>Grants access to a mining unit. Granting it twice has no effect.</summary>
    public void GrantMiningUnit(Guid miningUnitId)
    {
        if (miningUnitId == Guid.Empty)
            throw new DomainException("A scope must reference a mining unit.");
        if (!IsActive)
            throw new DomainConflictException("A deactivated user cannot receive new scopes.");

        _miningUnitIds.Add(miningUnitId);
    }

    /// <summary>Removes access to a mining unit; the user's history is kept untouched.</summary>
    public void RevokeMiningUnit(Guid miningUnitId) => _miningUnitIds.Remove(miningUnitId);

    public bool CanAccessMiningUnit(Guid miningUnitId) =>
        Role == UserRole.Administrator || _miningUnitIds.Contains(miningUnitId);
}
