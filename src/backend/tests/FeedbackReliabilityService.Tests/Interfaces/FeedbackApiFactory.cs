using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace FeedbackReliabilityService.Tests.Interfaces;

public class FeedbackApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "https://feedback-tests.invalid";
    // Test-only signing material, never loaded by the application.
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes("feedback-tests-only-012345678901234567890123456789"));
    public ConcurrentDictionary<Guid, SourceAlert> Alerts { get; } = new();
    public Guid OperationId { get; } = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAlertSource>();
            services.AddSingleton<IAlertSource>(new TestAlertSource(Alerts));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = Issuer };
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = "minesense-feedback";
                options.TokenValidationParameters.IssuerSigningKey = Key;
            });
        });
    }

    public HttpClient Client(Guid userId, string role, Guid? operationId = null)
    {
        var client = CreateClient();
        var token = new JwtSecurityToken(Issuer, "minesense-feedback",
            [new Claim("sub", userId.ToString()), new Claim("role", role),
             new Claim("operation_id", (operationId ?? OperationId).ToString())],
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10), new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private sealed class TestAlertSource(ConcurrentDictionary<Guid, SourceAlert> alerts) : IAlertSource
    {
        public Task<SourceAlert?> FindAsync(Guid alertId, CancellationToken cancellationToken) =>
            Task.FromResult(alerts.GetValueOrDefault(alertId));
    }
}
