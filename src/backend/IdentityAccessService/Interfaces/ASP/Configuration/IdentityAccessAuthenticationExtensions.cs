using IdentityAccessService.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace IdentityAccessService.Interfaces.ASP.Configuration;

public static class IdentityAccessAuthenticationExtensions
{
    /// <summary>
    /// Bearer authentication with the tokens this service signs. Every rejected request is
    /// logged with its date, time and result (QAS04).
    /// </summary>
    public static IServiceCollection AddIdentityAccessAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => options.AccessTokenMinutes > 0 && options.RefreshTokenDays > 0,
                "Token lifetimes must be positive.")
            .ValidateOnStart();
        services.AddSingleton<RsaSigningKeyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<RsaSigningKeyProvider, IOptions<JwtOptions>>((bearer, keyProvider, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = keyProvider.PublicKey,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    NameClaimType = JwtRegisteredClaimNames.UniqueName,
                    RoleClaimType = JwtTokenService.RoleClaim,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                bearer.Events = new JwtBearerEvents
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
            });
        services.AddAuthorization();

        services.ConfigureSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token returned by /api/v1/authentication/sign-in"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = []
            });
        });
        return services;
    }

    private static void LogRejection(HttpContext context, int statusCode, string reason)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentityAccessService.AccessAudit");
        logger.LogWarning("Access rejected {StatusCode} {Method} {Path} at {OccurredAt:O} for {User}: {Reason}",
            statusCode, context.Request.Method, context.Request.Path, DateTimeOffset.UtcNow,
            context.User.Identity?.Name ?? "anonymous", reason);
    }
}
