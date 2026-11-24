using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace OperationsService.Tests.Interfaces;

/// <summary>
/// Hosts the service with a test-only signing key instead of the Identity &amp; Access JWKS,
/// and signs every client in as an administrator unless the test asks for another role.
/// </summary>
public class OperationsApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "https://operations-tests.invalid";
    private const string Audience = "minesense-services";
    // Test-only signing material, never loaded by the application.
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes("operations-tests-only-0123456789012345678901234567"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
                options.TokenValidationParameters.IssuerSigningKey = Key;
            }));
    }

    protected override void ConfigureClient(HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = BearerFor("Administrator");

    /// <summary>A client that sends no token at all.</summary>
    public HttpClient AnonymousClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }

    /// <summary>A client signed in with the given role (Operator, Supervisor, Administrator...).</summary>
    public HttpClient ClientWithRole(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = BearerFor(role);
        return client;
    }

    private static AuthenticationHeaderValue BearerFor(string role)
    {
        var token = new JwtSecurityToken(Issuer, Audience,
            [new Claim("unique_name", "tester"), new Claim("role", role)],
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10),
            new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}
