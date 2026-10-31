using AnalyticsService.Application.Internal.CommandServices;
using AnalyticsService.Application.Internal.EventHandlers;
using AnalyticsService.Application.Internal.QueryServices;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;
using AnalyticsService.Infrastructure.Persistence.InMemory;
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
builder.Services.AddSingleton<IFatigueMetricRepository, InMemoryFatigueMetricRepository>();
builder.Services.AddSingleton<IPreventiveMeasureRepository, InMemoryPreventiveMeasureRepository>();
builder.Services.AddScoped<IFatigueMetricCommandService, FatigueMetricCommandService>();
builder.Services.AddScoped<IPreventiveMeasureCommandService, PreventiveMeasureCommandService>();
builder.Services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
builder.Services.AddSingleton<IOperatorContextRepository, InMemoryOperatorContextRepository>();
builder.Services.AddIntegrationEventHandler<FleetOperatorUpdatedEventHandler>();
builder.Services.AddIntegrationEventHandler<AlertIssuedEventHandler>();

var app = builder.Build();

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
