namespace IdentityAccessService.Interfaces.REST.Resources;

public record SignUpResource(string Username, string FullName, string Password, string Role);
