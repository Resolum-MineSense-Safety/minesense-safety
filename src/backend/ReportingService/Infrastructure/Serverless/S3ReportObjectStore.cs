using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using ReportingService.Domain.Services;

namespace ReportingService.Infrastructure.Serverless;

/// <summary>
/// Reads the PDFs of the reports bucket. S3 only answers 404 for a missing key to a role with
/// s3:ListBucket on the bucket (403 otherwise), which is why the Terraform role includes it.
/// </summary>
public class S3ReportObjectStore(Lazy<IAmazonS3> s3, string bucket) : IReportObjectStore
{
    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await s3.Value.GetObjectAsync(
                new GetObjectRequest { BucketName = bucket, Key = key }, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
