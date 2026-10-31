using EdgeSynchronizationService.Application.Internal.CommandServices;
using EdgeSynchronizationService.Application.Internal.QueryServices;
using EdgeSynchronizationService.Domain.Repositories;
using EdgeSynchronizationService.Domain.Services;
using EdgeSynchronizationService.Infrastructure.Persistence.InMemory;
using EdgeSynchronizationService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
// Persistence (ADR-005): MongoDB when ConnectionStrings:MongoDb is set, in memory otherwise
if (builder.Services.AddMineSenseMongo(builder.Configuration, "minesense_edge_sync"))
    builder.Services.AddMongoRepository<ISynchronizationBatchRepository, MongoSynchronizationBatchRepository>();
else
    builder.Services.AddSingleton<ISynchronizationBatchRepository, InMemorySynchronizationBatchRepository>();
builder.Services.AddScoped<ISynchronizationCommandService, SynchronizationCommandService>();
builder.Services.AddScoped<ISynchronizationQueryService, SynchronizationQueryService>();

var app = builder.Build();
await app.Services.InitializeMongoCollectionsAsync(app.Lifetime.ApplicationStopping);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();

public partial class Program;
