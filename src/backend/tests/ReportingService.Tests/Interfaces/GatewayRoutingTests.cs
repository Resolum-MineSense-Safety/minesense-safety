using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ReportingService.Tests.Interfaces;

/// <summary>
/// Every route this service exposes must reach it through the gateway (ALB Ingress and the local
/// nginx are rendered from the same <c>minesense-gateway</c> values), otherwise it is unreachable.
/// </summary>
public class GatewayRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ServiceName = "reporting-service";
    private readonly WebApplicationFactory<Program> factory;

    public GatewayRoutingTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void EveryApiRoute_IsRoutedToThisServiceByTheGateway()
    {
        // Arrange
        var routes = GatewayRoutes();
        var templates = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => "/" + endpoint.RoutePattern.RawText!.TrimStart('/'))
            .Where(template => template.StartsWith("/api/", StringComparison.Ordinal))
            .Distinct()
            .ToList();

        // Act
        var misrouted = templates
            .Select(template => (template, service: Resolve(routes, Sample(template))))
            .Where(entry => entry.service != ServiceName)
            .Select(entry => $"{entry.template} -> {entry.service ?? "none"}")
            .ToList();

        // Assert
        Assert.NotEmpty(templates);
        Assert.Empty(misrouted);
    }

    private static string Sample(string template) =>
        Regex.Replace(template, @"\{[^}]+\}", "7c9e6679-7425-40de-944b-e07fc1f90ae7");

    /// <summary>First matching rule wins, as in the ALB and the rendered nginx.</summary>
    private static string? Resolve(IEnumerable<(string Path, string Service)> routes, string path) =>
        routes.FirstOrDefault(route => Regex.IsMatch(path,
            "^" + Regex.Escape(route.Path).Replace(@"\*", "[^/]+") + "(/.*)?$")).Service;

    private static List<(string Path, string Service)> GatewayRoutes()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            directory = directory.Parent;
        Assert.NotNull(directory);

        var values = File.ReadAllLines(Path.Combine(directory.FullName,
            "infrastructure", "helm", "minesense-gateway", "values.yaml"));
        var routes = new List<(string, string)>();
        string? path = null;
        foreach (var line in values.Select(l => l.Trim()))
        {
            if (line.StartsWith("- path:", StringComparison.Ordinal))
                path = line["- path:".Length..].Trim();
            else if (line.StartsWith("service:", StringComparison.Ordinal) && path is not null)
            {
                routes.Add((path, line["service:".Length..].Trim()));
                path = null;
            }
        }
        return routes;
    }
}
