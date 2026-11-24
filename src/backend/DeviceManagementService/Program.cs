using DeviceManagementService.Application.Internal.CommandServices;
using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;
using DeviceManagementService.Infrastructure.Persistence.InMemory;
using DeviceManagementService.Infrastructure.Persistence.PostgreSql;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);

// Persistence (ADR-005): PostgreSQL when ConnectionStrings:PostgreSql is set, in memory otherwise
if (builder.Services.AddMineSensePostgres(builder.Configuration))
    builder.Services.AddPostgresRepository<IDeviceRepository, PostgresDeviceRepository>();
else
    builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();
builder.Services.AddScoped<IDeviceQueryService, DeviceQueryService>();
builder.Services.AddScoped<IPreShiftAvailabilityQueryService, PreShiftAvailabilityQueryService>();

var app = builder.Build();
await app.Services.InitializePostgresTablesAsync(app.Lifetime.ApplicationStopping);

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