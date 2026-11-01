using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.EventHandlers;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Repositories;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Infrastructure.Persistence.InMemory;
using PrivacyAuditService.Infrastructure.Persistence.MongoDb;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Domain events over Kafka (ADR-004), enabled when Kafka:BootstrapServers is set
builder.Services.AddMineSenseEvents(builder.Configuration, "privacy-audit-service");

// Privacy & Audit bounded context (EP09/EP10, scaffold: endpoints return 501 until Fase 1)
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_privacy"))
{
    builder.Services.AddMongoRepository<IProcessingConsentRepository, MongoProcessingConsentRepository>();
    builder.Services.AddMongoRepository<IAccessAuditRepository, MongoAccessAuditRepository>();
    builder.Services.AddMongoRepository<IComplianceReportRepository, MongoComplianceReportRepository>();
    builder.Services.AddMongoRepository<IIncidentLifecycleRepository, MongoIncidentLifecycleRepository>();
}
else
{
    builder.Services.AddSingleton<IProcessingConsentRepository, InMemoryProcessingConsentRepository>();
    builder.Services.AddSingleton<IAccessAuditRepository, InMemoryAccessAuditRepository>();
    builder.Services.AddSingleton<IComplianceReportRepository, InMemoryComplianceReportRepository>();
    builder.Services.AddSingleton<IIncidentLifecycleRepository, InMemoryIncidentLifecycleRepository>();
}
builder.Services.AddScoped<IPrivacyAuditCommandService, PrivacyAuditCommandService>();
builder.Services.AddScoped<IPrivacyAuditQueryService, PrivacyAuditQueryService>();
builder.Services.AddIntegrationEventHandler<IncidentOpenedEventHandler>();
builder.Services.AddIntegrationEventHandler<IncidentClosedEventHandler>();

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
