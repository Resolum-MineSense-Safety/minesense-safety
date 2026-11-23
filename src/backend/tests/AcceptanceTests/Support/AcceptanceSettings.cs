namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>
/// Where the scenarios run. Without <c>ACCEPTANCE_BASE_URL</c> every API is hosted in memory; with it
/// the steps call the deployed environment through its API gateway (the ALB of the Helm chart
/// minesense-gateway, which routes every <c>/api/v1/...</c> path to its microservice).
/// </summary>
public static class AcceptanceSettings
{
    /// <summary>Tag of the scenarios that need something only the in-memory host offers (a fake clock, a second host with authentication on).</summary>
    public const string InMemoryOnlyTag = "solo-en-memoria";

    public static Uri? BaseUrl { get; } = ReadBaseUrl();

    public static bool IsRemote => BaseUrl is not null;

    /// <summary>Bearer token used for every request against the deployed environment.</summary>
    public static string? Token => Read("ACCEPTANCE_TOKEN");

    /// <summary>User that signs in through Identity &amp; Access when no token is given.</summary>
    public static string? User => Read("ACCEPTANCE_USER");

    public static string? Password => Read("ACCEPTANCE_PASSWORD");

    /// <summary>Timeout of each request against the deployed environment.</summary>
    public static TimeSpan RemoteTimeout { get; } =
        TimeSpan.FromSeconds(int.TryParse(Read("ACCEPTANCE_TIMEOUT_SECONDS"), out var seconds) ? seconds : 30);

    private static Uri? ReadBaseUrl()
    {
        var value = Read("ACCEPTANCE_BASE_URL");
        if (value is null) return null;
        if (!Uri.TryCreate(value.TrimEnd('/') + "/", UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"ACCEPTANCE_BASE_URL is not an absolute URL: '{value}'.");
        return uri;
    }

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
