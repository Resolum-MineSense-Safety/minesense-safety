using MonitoringService.Application.Internal.CommandServices;
using MonitoringService.Application.Internal.OutboundServices;
using MonitoringService.Application.Internal.QueryServices;
using MonitoringService.Domain.Repositories;
using MonitoringService.Domain.Services;
using MonitoringService.Infrastructure.DeviceManagement;
using MonitoringService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Monitoring bounded context (EP01)
builder.Services.AddSingleton<IMonitoringSessionRepository, InMemoryMonitoringSessionRepository>();
builder.Services.AddScoped<IMonitoringSessionCommandService, MonitoringSessionCommandService>();
builder.Services.AddScoped<IMonitoringSessionQueryService, MonitoringSessionQueryService>();

// Outbound: Device Management bounded context (devices available before the shift)
var deviceManagementServiceUrl =
    builder.Configuration["Services:DeviceManagementServiceUrl"] ?? "http://localhost:5031";
var batteryThreshold = builder.Configuration.GetValue("Monitoring:BatteryThreshold", 20m);
builder.Services.AddHttpClient(nameof(IDeviceAvailabilityService), client =>
        client.BaseAddress = new Uri(deviceManagementServiceUrl.TrimEnd('/') + "/"))
    .AddTypedClient<IDeviceAvailabilityService>(httpClient =>
        new HttpDeviceAvailabilityService(httpClient, batteryThreshold));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();

public partial class Program;
