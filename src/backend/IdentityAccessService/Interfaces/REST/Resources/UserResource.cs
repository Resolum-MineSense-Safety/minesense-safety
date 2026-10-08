namespace IdentityAccessService.Interfaces.REST.Resources;

public record UserResource(Guid Id, string Username, string FullName, string Role, bool IsActive);
