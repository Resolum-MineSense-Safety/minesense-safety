namespace IdentityAccessService.Application.Internal.OutboundServices;

/// <summary>Outbound port for one-way password hashing.</summary>
public interface IHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
