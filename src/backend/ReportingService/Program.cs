using Amazon.S3;
using Amazon.SQS;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Application.Internal.Serverless;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;
using ReportingService.Infrastructure.Persistence.InMemory;
using ReportingService.Infrastructure.Persistence.MongoDb;
using ReportingService.Infrastructure.Rendering;
using ReportingService.Infrastructure.Serverless;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Reporting bounded context (EP10): versioned indicators, report jobs and downloadable artifacts
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_reporting"))
{
    builder.Services.AddMongoRepository<IReportDefinitionRepository, MongoReportDefinitionRepository>();
    builder.Services.AddMongoRepository<IReportJobRepository, MongoReportJobRepository>();
    builder.Services.AddMongoRepository<IReportArtifactRepository, MongoReportArtifactRepository>();
}
else
{
    builder.Services.AddSingleton<IReportDefinitionRepository, InMemoryReportDefinitionRepository>();
    builder.Services.AddSingleton<IReportJobRepository, InMemoryReportJobRepository>();
    builder.Services.AddSingleton<IReportArtifactRepository, InMemoryReportArtifactRepository>();
}
builder.Services.AddSingleton<IReportRenderer, InProcessReportRenderer>();
builder.Services.AddScoped<IReportingCommandService, ReportingCommandService>();
builder.Services.AddScoped<IReportingQueryService, ReportingQueryService>();

// Serverless PDFs (ADR-008, section 3.5): SQS -> Lambda -> S3 when Reporting:SqsQueueUrl and
// Reporting:S3Bucket are set; otherwise every report is rendered in process. AWS credentials and
// region come from the pod (IRSA). The clients are created on first use because their constructors
// fail without credentials: a role missing from the pod must fail the PDF jobs, not every route.
var serverless = ServerlessReportingOptions.FromConfiguration(builder.Configuration);
if (serverless is not null)
{
    builder.Services.AddSingleton(serverless);
    builder.Services.AddSingleton<IReportRequestQueue>(new SqsReportRequestQueue(
        new Lazy<IAmazonSQS>(() => new AmazonSQSClient(), LazyThreadSafetyMode.PublicationOnly), serverless.QueueUrl));
    builder.Services.AddSingleton<IReportObjectStore>(new S3ReportObjectStore(
        new Lazy<IAmazonS3>(() => new AmazonS3Client(), LazyThreadSafetyMode.PublicationOnly), serverless.Bucket));
    builder.Services.AddScoped<ReportCompletionService>();
    builder.Services.AddHostedService<ReportCompletionPoller>();
}

var app = builder.Build();
await app.Services.InitializeMongoCollectionsAsync(app.Lifetime.ApplicationStopping);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();

public partial class Program;
