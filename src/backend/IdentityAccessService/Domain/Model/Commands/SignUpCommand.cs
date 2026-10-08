using IdentityAccessService.Domain.Model.ValueObjects;

namespace IdentityAccessService.Domain.Model.Commands;

public record SignUpCommand(string Username, string FullName, string Password, UserRole Role);
