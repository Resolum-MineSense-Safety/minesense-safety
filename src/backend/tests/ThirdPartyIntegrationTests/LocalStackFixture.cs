using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Testcontainers.LocalStack;

namespace MineSenseSafety.ThirdPartyIntegrationTests;

/// <summary>
/// One LocalStack container (SQS and S3) shared by the tests of a class. The AWS SDK clients
/// point at it with dummy credentials, exactly as they would point at us-east-1 in AWS.
/// </summary>
public sealed class LocalStackFixture : IAsyncLifetime
{
    private readonly LocalStackContainer _container = new LocalStackBuilder()
        .WithImage("localstack/localstack:4.7")
        .WithEnvironment("SERVICES", "sqs,s3")
        .Build();

    public IAmazonSQS Sqs { get; private set; } = null!;

    public IAmazonS3 S3 { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var credentials = new BasicAWSCredentials("test", "test");
        var endpoint = _container.GetConnectionString();
        Sqs = new AmazonSQSClient(credentials, new AmazonSQSConfig { ServiceURL = endpoint, AuthenticationRegion = "us-east-1" });
        S3 = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1"
        });
    }

    public async Task DisposeAsync()
    {
        Sqs.Dispose();
        S3.Dispose();
        await _container.DisposeAsync();
    }
}
