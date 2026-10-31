using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MineSenseSafety.Shared.Infrastructure.Security;

/// <summary>
/// Downloads the public keys that Identity &amp; Access publishes at <c>/.well-known/jwks.json</c>
/// (ADR-007) and caches them. An unknown <c>kid</c> forces a refresh, at most once every
/// 30 seconds, so a key rotation in Identity &amp; Access is picked up without a restart.
/// </summary>
public sealed class JwksSigningKeyResolver(HttpClient httpClient, string jwksUrl, ILogger logger)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyList<SecurityKey> _keys = [];
    private DateTimeOffset _fetchedAt = DateTimeOffset.MinValue;

    public IEnumerable<SecurityKey> Resolve(string? keyId)
    {
        var now = DateTimeOffset.UtcNow;
        var unknownKey = keyId is not null && _keys.All(key => key.KeyId != keyId);
        if (now - _fetchedAt > CacheLifetime || (unknownKey && now - _fetchedAt > MinimumRefreshInterval))
            Refresh(now);

        return keyId is null ? _keys : _keys.Where(key => key.KeyId == keyId);
    }

    private void Refresh(DateTimeOffset now)
    {
        if (!_refreshLock.Wait(TimeSpan.FromSeconds(5))) return;
        try
        {
            var json = httpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
            _keys = new JsonWebKeySet(json).GetSigningKeys().ToList();
            _fetchedAt = now;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or ArgumentException)
        {
            // Keep the previous keys: tokens signed with them stay valid while Identity is unreachable.
            logger.LogWarning(exception, "Could not download the signing keys from {JwksUrl}", jwksUrl);
            _fetchedAt = now - CacheLifetime + MinimumRefreshInterval;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
