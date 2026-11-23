namespace MineSenseSafety.Shared.Infrastructure.Messaging;

/// <summary>
/// Kafka settings (section <c>Kafka</c>). Without <see cref="BootstrapServers"/> the service runs
/// with a logging publisher and no consumers, so tests and a bare <c>dotnet run</c> keep working.
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>Kebab-case name of the service (client id and default consumer group), set in code.</summary>
    public string ServiceName { get; set; } = "minesense";

    /// <summary>Comma-separated brokers, e.g. <c>kafka:9092</c> or the MSK Serverless IAM endpoint.</summary>
    public string? BootstrapServers { get; set; }

    /// <summary>Consumer group; defaults to <c>minesense-&lt;ServiceName&gt;</c>.</summary>
    public string? ConsumerGroup { get; set; }

    /// <summary><c>None</c> (plaintext, local) or <c>AwsMskIam</c> (SASL/OAUTHBEARER signed with the pod role).</summary>
    public string Authentication { get; set; } = "None";

    /// <summary>AWS region used to sign the MSK IAM tokens.</summary>
    public string AwsRegion { get; set; } = "us-east-1";

    /// <summary>Partitions of the topics a consumer creates when they do not exist yet.</summary>
    public int TopicPartitions { get; set; } = 3;

    /// <summary>Attempts per event before it is considered poison and skipped.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Wait before the second attempt; it doubles on each retry.</summary>
    public int RetryDelayMilliseconds { get; set; } = 500;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BootstrapServers);

    public bool UsesAwsMskIam => string.Equals(Authentication, "AwsMskIam", StringComparison.OrdinalIgnoreCase);
}
