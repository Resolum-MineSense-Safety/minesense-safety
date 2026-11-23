using AnalyticsService.Application.Internal.CommandServices;
using AnalyticsService.Application.Internal.EventHandlers;
using AnalyticsService.Application.Internal.QueryServices;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;
using AnalyticsService.Infrastructure.Persistence.InMemory;
using AnalyticsService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422/409) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Domain events over Kafka (ADR-004), enabled when Kafka:BootstrapServers is set
builder.Services.AddMineSenseEvents(builder.Configuration, "analytics-service");

// Analytics bounded context (EP11)
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_analytics"))
{
    builder.Services.AddMongoRepository<IFatigueMetricRepository, MongoFatigueMetricRepository>();
    builder.Services.AddMongoRepository<IPreventiveMeasureRepository, MongoPreventiveMeasureRepository>();
    builder.Services.AddMongoRepository<IOperatorContextRepository, MongoOperatorContextRepository>();
}
else
{
    builder.Services.AddSingleton<IFatigueMetricRepository, InMemoryFatigueMetricRepository>();
    builder.Services.AddSingleton<IPreventiveMeasureRepository, InMemoryPreventiveMeasureRepository>();
    builder.Services.AddSingleton<IOperatorContextRepository, InMemoryOperatorContextRepository>();
}
builder.Services.AddScoped<IFatigueMetricCommandService, FatigueMetricCommandService>();
builder.Services.AddScoped<IPreventiveMeasureCommandService, PreventiveMeasureCommandService>();
builder.Services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
builder.Services.AddIntegrationEventHandler<FleetOperatorUpdatedEventHandler>();
builder.Services.AddIntegrationEventHandler<AlertIssuedEventHandler>();

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
