using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ReportingService.Infrastructure.Serverless;

namespace ReportingService.Tests.Infrastructure;

public class ServerlessAdaptersTests
{
    [Fact]
    public async Task Sqs_SendsTheBodyToTheConfiguredQueue()
    {
        // Arrange
        SendMessageRequest? sent = null;
        var sqs = AwsClient<IAmazonSQS>.For(arguments =>
        {
            sent = (SendMessageRequest)arguments[0]!;
            return Task.FromResult(new SendMessageResponse());
        });

        // Act
        await new SqsReportRequestQueue(new Lazy<IAmazonSQS>(sqs), "https://sqs.test/queue").SendAsync("{}", CancellationToken.None);

        // Assert
        Assert.Equal("https://sqs.test/queue", sent!.QueueUrl);
        Assert.Equal("{}", sent.MessageBody);
    }

    [Fact]
    public async Task S3_ExistingObject_ReturnsItsContent()
    {
        // Arrange
        GetObjectRequest? requested = null;
        var s3 = AwsClient<IAmazonS3>.For(arguments =>
        {
            requested = (GetObjectRequest)arguments[0]!;
            return Task.FromResult(new GetObjectResponse { ResponseStream = new MemoryStream([1, 2]) });
        });

        // Act
        await using var content = await new S3ReportObjectStore(new Lazy<IAmazonS3>(s3), "bucket").OpenReadAsync("reports/x.pdf", CancellationToken.None);

        // Assert
        Assert.Equal("bucket", requested!.BucketName);
        Assert.Equal("reports/x.pdf", requested.Key);
        Assert.Equal(2, content!.Length);
    }

    [Fact]
    public async Task S3_MissingObject_ReturnsNull()
    {
        // Arrange
        var s3 = AwsClient<IAmazonS3>.For(_ =>
            Task.FromException<GetObjectResponse>(new AmazonS3Exception("missing") { StatusCode = HttpStatusCode.NotFound }));

        // Act + Assert
        Assert.Null(await new S3ReportObjectStore(new Lazy<IAmazonS3>(s3), "bucket").OpenReadAsync("key", CancellationToken.None));
    }

    [Fact]
    public async Task S3_AccessDenied_IsNotTreatedAsMissing()
    {
        // Arrange
        var s3 = AwsClient<IAmazonS3>.For(_ =>
            Task.FromException<GetObjectResponse>(new AmazonS3Exception("denied") { StatusCode = HttpStatusCode.Forbidden }));

        // Act + Assert
        await Assert.ThrowsAsync<AmazonS3Exception>(() =>
            new S3ReportObjectStore(new Lazy<IAmazonS3>(s3), "bucket").OpenReadAsync("key", CancellationToken.None));
    }

    [Fact]
    public void Adapters_DoNotCreateTheClientUntilFirstUse()
    {
        // Arrange
        var created = false;

        // Act
        _ = new SqsReportRequestQueue(new Lazy<IAmazonSQS>(() =>
        {
            created = true;
            throw new InvalidOperationException("No AWS credentials");
        }), "url");

        // Assert
        Assert.False(created);
    }

    /// <summary>The AWS client interfaces have hundreds of members: every call goes to one handler.</summary>
    public class AwsClient<T> : DispatchProxy where T : class
    {
        private Func<object?[], object?> handler = _ => null;

        public static T For(Func<object?[], object?> handler)
        {
            var proxy = Create<T, AwsClient<T>>();
            ((AwsClient<T>)(object)proxy).handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => handler(args ?? []);
    }
}
