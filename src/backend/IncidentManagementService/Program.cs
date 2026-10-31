using IncidentManagementService.Application.Internal.CommandServices;
using IncidentManagementService.Application.Internal.EventHandlers;
using IncidentManagementService.Application.Internal.QueryServices;
using IncidentManagementService.Application.Internal.Scheduling;
using IncidentManagementService.Domain.Repositories;
using IncidentManagementService.Domain.Services;
using IncidentManagementService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422/409) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Domain events over Kafka (ADR-004), enabled when Kafka:BootstrapServers is set
builder.Services.AddMineSenseEvents(builder.Configuration, "incident-management-service");

// Incident Management bounded context (EP05)
builder.Services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
builder.Services.AddScoped<IIncidentCommandService, IncidentCommandService>();
builder.Services.AddScoped<IIncidentQueryService, IncidentQueryService>();
builder.Services.AddHostedService<IncidentEscalationWorker>();
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
