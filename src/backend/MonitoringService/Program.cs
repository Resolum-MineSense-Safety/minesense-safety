using MonitoringService.Application.Internal.CommandServices;
using MonitoringService.Application.Internal.OutboundServices;
using MonitoringService.Application.Internal.QueryServices;
using MonitoringService.Domain.Repositories;
using MonitoringService.Domain.Services;
using MonitoringService.Infrastructure.Operations;
using MonitoringService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Monitoring bounded context (EP01)
builder.Services.AddSingleton<IMonitoringSessionRepository, InMemoryMonitoringSessionRepository>();
builder.Services.AddScoped<IMonitoringSessionCommandService, MonitoringSessionCommandService>();
builder.Services.AddScoped<IMonitoringSessionQueryService, MonitoringSessionQueryService>();

// Outbound: Operations bounded context (operator assignment / shift context)
var operationsServiceUrl = builder.Configuration["Services:OperationsServiceUrl"] ?? "http://localhost:5103";
builder.Services.AddHttpClient<IOperationalContextService, HttpOperationalContextService>(client =>
    client.BaseAddress = new Uri(operationsServiceUrl.TrimEnd('/') + "/"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
