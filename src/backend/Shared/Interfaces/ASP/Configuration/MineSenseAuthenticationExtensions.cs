using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MineSenseSafety.Shared.Infrastructure.Security;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Validates the access tokens issued by Identity &amp; Access (RS256, ADR-007) in every other
/// microservice. Enforcement is switched on by <c>Authentication:IdentityUrl</c>: when it is set,
/// every endpoint requires a valid token (health probes and Swagger stay public because they run
/// before authentication); when it is empty the service stays open, which keeps the in-process
/// API tests and a bare <c>dotnet run</c> working without Identity &amp; Access.
/// </summary>
public static class MineSenseAuthenticationExtensions
{
    public const string IdentityUrlKey = "Authentication:IdentityUrl";
    public const string DefaultIssuer = "minesense-identity-access";
    public const string DefaultAudience = "minesense-services";

    public static IServiceCollection AddMineSenseAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var identityUrl = configuration[IdentityUrlKey];
        if (string.IsNullOrWhiteSpace(identityUrl))
            return services;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<ILoggerFactory>((options, loggers) => ConfigureIdentityTokens(options, configuration,
                identityUrl, loggers.CreateLogger<JwksSigningKeyResolver>()));
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    /// <summary>
    /// Points a JWT bearer scheme at the keys, issuer and audience of Identity &amp; Access. Shared
    /// with services that register their own scheme, such as Feedback &amp; Reliability.
    /// </summary>
    public static void ConfigureIdentityTokens(JwtBearerOptions options, IConfiguration configuration,
        string identityUrl, ILogger logger)
    {
        var jwksUrl = identityUrl.TrimEnd('/') + "/.well-known/jwks.json";
        var resolver = new JwksSigningKeyResolver(new HttpClient { Timeout = TimeSpan.FromSeconds(5) }, jwksUrl, logger);

        options.Authority = null;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = configuration["Authentication:Issuer"] ?? DefaultIssuer,
            ValidAudience = configuration["Authentication:Audience"] ?? DefaultAudience,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            IssuerSigningKeyResolver = (_, _, keyId, _) => resolver.Resolve(keyId),
            NameClaimType = "unique_name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                LogRejection(context.HttpContext, StatusCodes.Status401Unauthorized,
                    context.AuthenticateFailure?.Message ?? context.ErrorDescription ?? "missing token");
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                LogRejection(context.HttpContext, StatusCodes.Status403Forbidden, "insufficient role");
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>Every rejected request is logged with its date, time and reason (QAS04).</summary>
    private static void LogRejection(HttpContext context, int statusCode, string reason)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MineSenseSafety.AccessAudit");
        logger.LogWarning("Access rejected {StatusCode} {Method} {Path} at {OccurredAt:O} for {User}: {Reason}",
            statusCode, context.Request.Method, context.Request.Path, DateTimeOffset.UtcNow,
            context.User.Identity?.Name ?? "anonymous", reason);
    }
}
