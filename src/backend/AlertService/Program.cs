using AlertService.Application.Internal.CommandServices;
using AlertService.Application.Internal.EventHandlers;
using AlertService.Application.Internal.QueryServices;
using AlertService.Domain.Repositories;
using AlertService.Domain.Services;
using AlertService.Infrastructure.Persistence.InMemory;
using AlertService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Domain events over Kafka (ADR-004), enabled when Kafka:BootstrapServers is set
builder.Services.AddMineSenseEvents(builder.Configuration, "alert-service");

// Alerts bounded context (EP03)
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_alerts"))
    builder.Services.AddMongoRepository<IAlertRepository, MongoAlertRepository>();
else
    builder.Services.AddSingleton<IAlertRepository, InMemoryAlertRepository>();
builder.Services.AddScoped<IAlertCommandService, AlertCommandService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
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
