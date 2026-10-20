using IdentityAccessService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Domain.Model.Aggregates;

/// <summary>
/// Person who can sign in to MineSense (EP08). The role restricts features by responsibility;
/// only a hash of the password is ever stored.
/// </summary>
public class User : AggregateRoot
{
    public string Username { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

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
}
