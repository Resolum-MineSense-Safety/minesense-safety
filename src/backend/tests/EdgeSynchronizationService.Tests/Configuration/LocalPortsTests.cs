using System.Text.Json;
using System.Text.RegularExpressions;

namespace EdgeSynchronizationService.Tests.Configuration;

/// <summary>
/// Guards the local ports: two services sharing a <c>dotnet run</c> port cannot run side by side,
/// and the Edge agent and the READMEs rely on the same host port that docker-compose publishes.
/// </summary>
public sealed partial class LocalPortsTests
{
    private static readonly DirectoryInfo RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void LaunchSettings_HttpPortsAreUniqueAcrossServices()
    {
        var ports = LaunchSettingsHttpPorts();

        var duplicates = ports
            .GroupBy(entry => entry.Port)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.Service))}")
            .ToList();

        Assert.NotEmpty(ports);
        Assert.Empty(duplicates);
    }

    [Theory]
    [InlineData("EdgeSynchronizationService", "edge-synchronization-service", 5280)]
    [InlineData("FeedbackReliabilityService", "feedback-reliability-service", 5281)]
    public void LaunchSettings_PortMatchesDockerComposeHostPort(
        string project, string composeService, int expectedPort)
    {
        var launchPort = LaunchSettingsHttpPorts().Single(entry => entry.Service == project).Port;

        Assert.Equal(expectedPort, launchPort);
        Assert.Equal(expectedPort, ComposeHostPort(composeService));
    }

    private static List<(string Service, int Port)> LaunchSettingsHttpPorts()
    {
        var backend = Path.Combine(RepositoryRoot.FullName, "src", "backend");
        var result = new List<(string, int)>();
        foreach (var file in Directory.GetFiles(backend, "launchSettings.json", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;

            var service = Directory.GetParent(file)!.Parent!.Name;
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (!document.RootElement.GetProperty("profiles").TryGetProperty("http", out var http)
                || !http.TryGetProperty("applicationUrl", out var url))
                continue;

            result.Add((service, new Uri(url.GetString()!.Split(';')[0]).Port));
        }
        return result;
    }

    private static int ComposeHostPort(string service)
    {
        var compose = File.ReadAllText(Path.Combine(RepositoryRoot.FullName, "docker-compose.yml"));
        var block = Regex.Match(compose, $@"^  {Regex.Escape(service)}:\s*$(.*?)(?=^  \S|\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(block.Success, $"Service '{service}' not found in docker-compose.yml.");

        return int.Parse(HostPortPattern().Match(block.Groups[1].Value).Groups[1].Value);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            directory = directory.Parent;
        return directory ?? throw new InvalidOperationException("Repository root not found.");
    }

    [GeneratedRegex(@"- ""(\d+):8080""")]
    private static partial Regex HostPortPattern();
}
