namespace ReportingService.Application.Internal.Serverless;

/// <summary>Settings of the serverless PDF flow (ADR-008, section 3.5).</summary>
public record ServerlessReportingOptions(string QueueUrl, string Bucket, TimeSpan GenerationTimeout, TimeSpan PollInterval)
{
    /// <summary>
    /// Long enough for the queue's three attempts (visibility timeout of 12 minutes each, Terraform
    /// reporting module) before the message goes to the dead-letter queue.
    /// </summary>
    public const int DefaultGenerationTimeoutMinutes = 40;
    public const int DefaultPollIntervalSeconds = 15;

    /// <summary>
    /// The flow is on when <c>Reporting:SqsQueueUrl</c> and <c>Reporting:S3Bucket</c> are set; returns null
    /// when neither is (in-process rendering). Only one of them is a deployment mistake and stops the start.
    /// </summary>
    public static ServerlessReportingOptions? FromConfiguration(IConfiguration configuration)
    {
        var queueUrl = configuration["Reporting:SqsQueueUrl"];
        var bucket = configuration["Reporting:S3Bucket"];
        if (string.IsNullOrWhiteSpace(queueUrl) && string.IsNullOrWhiteSpace(bucket)) return null;
        if (string.IsNullOrWhiteSpace(queueUrl) || string.IsNullOrWhiteSpace(bucket))
            throw new InvalidOperationException("Reporting:SqsQueueUrl and Reporting:S3Bucket must be set together.");

        var timeoutMinutes = configuration.GetValue("Reporting:GenerationTimeoutMinutes", DefaultGenerationTimeoutMinutes);
        var pollSeconds = configuration.GetValue("Reporting:PollIntervalSeconds", DefaultPollIntervalSeconds);
        if (timeoutMinutes <= 0 || pollSeconds <= 0)
            throw new InvalidOperationException("Reporting:GenerationTimeoutMinutes and Reporting:PollIntervalSeconds must be positive.");

        return new ServerlessReportingOptions(queueUrl, bucket,
            TimeSpan.FromMinutes(timeoutMinutes), TimeSpan.FromSeconds(pollSeconds));
    }
}
