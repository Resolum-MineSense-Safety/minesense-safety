namespace IdentityAccessService.Interfaces.REST.Resources;

/// <summary>Result of a successful sign-in.</summary>
public record AuthenticatedUserResource(Guid Id, string Username, string Role);
