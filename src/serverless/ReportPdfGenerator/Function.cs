using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using ReportPdfGenerator.Model;
using ReportPdfGenerator.Pdf;
using ReportPdfGenerator.Storage;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ReportPdfGenerator;

/// <summary>
/// Serverless function of section 3.5: consumes report requests from SQS,
/// renders each PDF and stores it in S3. Failed messages are reported
/// individually so only they return to the queue (and to the DLQ after 3 tries).
/// </summary>
public class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IReportStorage _storage;
    private readonly TimeProvider _timeProvider;

    /// <summary>Constructor used by the Lambda runtime.</summary>
    public Function() : this(
        new S3ReportStorage(new AmazonS3Client(),
            Environment.GetEnvironmentVariable("REPORTS_BUCKET")
            ?? throw new InvalidOperationException("REPORTS_BUCKET is not configured."),
            TimeProvider.System),
        TimeProvider.System)
    {
    }

    public Function(IReportStorage storage, TimeProvider timeProvider)
    {
        _storage = storage;
        _timeProvider = timeProvider;
    }

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in sqsEvent.Records)
        {
            try
            {
                var key = await ProcessAsync(message.Body, CancellationToken.None);
                context.Logger.LogInformation($"Report stored at {key} (message {message.MessageId}).");
            }
            catch (InvalidReportRequestException exception)
            {
                // A malformed request will never succeed: log it and drop it instead of retrying.
                context.Logger.LogError($"Discarded message {message.MessageId}: {exception.Message}");
            }
            catch (Exception exception)
            {
                context.Logger.LogError($"Message {message.MessageId} failed: {exception.Message}");
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            }
        }

        return new SQSBatchResponse(failures);
    }

    public async Task<string> ProcessAsync(string body, CancellationToken cancellationToken)
    {
        ReportRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ReportRequest>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidReportRequestException($"Invalid JSON: {exception.Message}");
        }

        var errors = ReportRequestValidator.Validate(request);
        if (errors.Count > 0)
            throw new InvalidReportRequestException(string.Join(" ", errors));

        var document = ReportDocumentBuilder.Build(request!, _timeProvider.GetUtcNow());
        return await _storage.SaveAsync(request!.ReportId, document, cancellationToken);
    }
}

public class InvalidReportRequestException(string message) : Exception(message);
