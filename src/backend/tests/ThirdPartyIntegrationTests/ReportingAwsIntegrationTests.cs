using System.Text;
using Amazon.S3.Model;
using Amazon.SQS.Model;
using ReportingService.Infrastructure.Serverless;

namespace MineSenseSafety.ThirdPartyIntegrationTests;

/// <summary>
/// Contract of the Reporting Service with the AWS services of the serverless PDF flow (3.5):
/// the request goes to the SQS queue that triggers the Lambda, and the PDF is read back from S3.
/// </summary>
[Trait("Category", "ThirdParty")]
public class ReportingAwsIntegrationTests(LocalStackFixture aws) : IClassFixture<LocalStackFixture>
{
    [Fact]
    public async Task SqsQueue_DeliversTheReportRequestUnchanged()
    {
        // Arrange
        var queueUrl = (await aws.Sqs.CreateQueueAsync($"report-requests-{Guid.NewGuid():N}")).QueueUrl;
        var queue = new SqsReportRequestQueue(new Lazy<Amazon.SQS.IAmazonSQS>(aws.Sqs), queueUrl);
        var body = """{"jobId":"7a1c","definition":"fatigue-by-shift","format":"pdf"}""";

        // Act
        await queue.SendAsync(body, CancellationToken.None);

        // Assert: what the Lambda will receive
        var received = await aws.Sqs.ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl) { WaitTimeSeconds = 5 });
        var message = Assert.Single(received.Messages);
        Assert.Equal(body, message.Body);
    }

    [Fact]
    public async Task S3Store_ReadsTheGeneratedPdf()
    {
        // Arrange: the object the Lambda writes
        var bucket = $"reports-{Guid.NewGuid():N}";
        await aws.S3.PutBucketAsync(bucket);
        var pdf = "%PDF-1.7 MineSense report"u8.ToArray();
        await aws.S3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = "reports/7a1c.pdf",
            InputStream = new MemoryStream(pdf),
            ContentType = "application/pdf"
        });
        var store = new S3ReportObjectStore(new Lazy<Amazon.S3.IAmazonS3>(aws.S3), bucket);

        // Act
        await using var stream = await store.OpenReadAsync("reports/7a1c.pdf", CancellationToken.None);

        // Assert
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!, Encoding.ASCII);
        Assert.Equal("%PDF-1.7 MineSense report", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task S3Store_ReturnsNullWhileThePdfDoesNotExist()
    {
        // Arrange: the Lambda has not finished yet
        var bucket = $"reports-{Guid.NewGuid():N}";
        await aws.S3.PutBucketAsync(bucket);
        var store = new S3ReportObjectStore(new Lazy<Amazon.S3.IAmazonS3>(aws.S3), bucket);

        // Act
        var stream = await store.OpenReadAsync("reports/pending.pdf", CancellationToken.None);

        // Assert: 404 is "not ready", never an error
        Assert.Null(stream);
    }
}
