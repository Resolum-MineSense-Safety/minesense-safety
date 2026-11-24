using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Adds <c>X-Content-Type-Options: nosniff</c> to every API response (section 4.4.4). The OWASP ZAP
/// API scan of 8 November 2026 reported the header as missing in the services (rule 10021, which
/// is FAIL in tests/security/zap/rules.tsv); registering it once in the shared library covers
/// the thirteen microservices without touching any Program.cs.
/// </summary>
public class SecurityHeadersStartupFilter : IStartupFilter
{
    public const string ContentTypeOptions = "X-Content-Type-Options";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[ContentTypeOptions] = "nosniff";
                return Task.CompletedTask;
            });
            await nextMiddleware();
        });
        next(app);
    };
}
