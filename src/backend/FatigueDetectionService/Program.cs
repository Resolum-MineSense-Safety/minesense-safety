using FatigueDetectionService.Application.Internal.CommandServices;
using FatigueDetectionService.Application.Internal.QueryServices;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;
using FatigueDetectionService.Infrastructure.Persistence.InMemory;
using FatigueDetectionService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Domain events over Kafka (ADR-004), enabled when Kafka:BootstrapServers is set
builder.Services.AddMineSenseEvents(builder.Configuration, "fatigue-detection-service");

// Fatigue Detection bounded context (EP02)
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_fatigue"))
    builder.Services.AddMongoRepository<IFatigueAssessmentRepository, MongoFatigueAssessmentRepository>();
else
    builder.Services.AddSingleton<IFatigueAssessmentRepository, InMemoryFatigueAssessmentRepository>();
builder.Services.AddScoped<IFatigueAssessmentCommandService, FatigueAssessmentCommandService>();
builder.Services.AddScoped<IFatigueAssessmentQueryService, FatigueAssessmentQueryService>();

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
