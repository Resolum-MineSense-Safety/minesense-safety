namespace IdentityAccessService.Infrastructure.Seeding;

/// <summary>First administrator, needed because only administrators can create other privileged users.</summary>
public class BootstrapAdministratorOptions
{
    public const string SectionName = "IdentityAccess:BootstrapAdministrator";

    public string? Username { get; set; }
    public string FullName { get; set; } = "MineSense Administrator";
    public string? Password { get; set; }
}
