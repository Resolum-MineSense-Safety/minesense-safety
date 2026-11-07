using Amazon.S3;
using Amazon.S3.Model;

namespace ReportPdfGenerator.Storage;

/// <summary>Stores PDFs under reports/yyyy/MM/{reportId}.pdf in the bucket created by Terraform.</summary>
public class S3ReportStorage(IAmazonS3 s3, string bucketName, TimeProvider timeProvider) : IReportStorage
{
    public async Task<string> SaveAsync(Guid reportId, byte[] document, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var key = $"reports/{now:yyyy}/{now:MM}/{reportId}.pdf";

        using var content = new MemoryStream(document);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            ContentType = "application/pdf",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS
        }, cancellationToken);

        return key;
    }
}
