using FleetMonitoringService.Application.Internal.CommandServices;
using FleetMonitoringService.Application.Internal.Configuration;
using FleetMonitoringService.Application.Internal.EventHandlers;
using FleetMonitoringService.Application.Internal.QueryServices;
using FleetMonitoringService.Domain.Repositories;
using FleetMonitoringService.Domain.Services;
using FleetMonitoringService.Infrastructure.Persistence.InMemory;
using FleetMonitoringService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Domain events over Kafka (ADR-004), enabled when Kafka:BootstrapServers is set
builder.Services.AddMineSenseEvents(builder.Configuration, "fleet-monitoring-service");
builder.Services.AddOptions<FleetMonitoringOptions>()
    .Bind(builder.Configuration.GetSection(FleetMonitoringOptions.SectionName))
    .Validate(options => options.StaleAfterMinutes > 0, "FleetMonitoring:StaleAfterMinutes must be positive.")
    .ValidateOnStart();

// Fleet Monitoring bounded context (EP04)
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_fleet"))
    builder.Services.AddMongoRepository<IMonitoredOperatorRepository, MongoMonitoredOperatorRepository>();
else
    builder.Services.AddSingleton<IMonitoredOperatorRepository, InMemoryMonitoredOperatorRepository>();
builder.Services.AddScoped<IMonitoredOperatorCommandService, MonitoredOperatorCommandService>();
builder.Services.AddScoped<IMonitoredOperatorQueryService, MonitoredOperatorQueryService>();
builder.Services.AddIntegrationEventHandler<FatigueAssessedEventHandler>();

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
