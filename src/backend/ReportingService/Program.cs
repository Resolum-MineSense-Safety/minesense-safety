using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;
using ReportingService.Infrastructure.Persistence.InMemory;
using ReportingService.Infrastructure.Persistence.MongoDb;
using ReportingService.Infrastructure.Rendering;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Reporting bounded context (EP10): versioned indicators, report jobs rendered in process
// (PDF/CSV) and downloadable artifacts (the bytes are kept with the artifact document)
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
