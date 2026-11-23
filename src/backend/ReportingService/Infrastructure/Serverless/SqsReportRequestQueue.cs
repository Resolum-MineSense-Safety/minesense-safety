using Amazon.SQS;
using Amazon.SQS.Model;
using ReportingService.Domain.Services;

namespace ReportingService.Infrastructure.Serverless;

public class SqsReportRequestQueue(Lazy<IAmazonSQS> sqs, string queueUrl) : IReportRequestQueue
{
    public Task SendAsync(string messageBody, CancellationToken cancellationToken) =>
        sqs.Value.SendMessageAsync(new SendMessageRequest(queueUrl, messageBody), cancellationToken);
}
