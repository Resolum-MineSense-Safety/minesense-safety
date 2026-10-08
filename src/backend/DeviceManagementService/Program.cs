using DeviceManagementService.Application.Internal.CommandServices;
using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;
using DeviceManagementService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Device Management bounded context (EP07)
builder.Services.AddSingleton<IMonitoringDeviceRepository, InMemoryMonitoringDeviceRepository>();
builder.Services.AddSingleton<IPreShiftCheckRepository, InMemoryPreShiftCheckRepository>();
builder.Services.AddScoped<IMonitoringDeviceCommandService, MonitoringDeviceCommandService>();
builder.Services.AddScoped<IMonitoringDeviceQueryService, MonitoringDeviceQueryService>();
builder.Services.AddScoped<IPreShiftCheckCommandService, PreShiftCheckCommandService>();
builder.Services.AddScoped<IPreShiftCheckQueryService, PreShiftCheckQueryService>();

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
