using FleetMonitoringService.Application.Internal.CommandServices;
using FleetMonitoringService.Application.Internal.QueryServices;
using FleetMonitoringService.Domain.Repositories;
using FleetMonitoringService.Domain.Services;
using FleetMonitoringService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Fleet Monitoring bounded context (EP04)
builder.Services.AddSingleton<IMonitoredOperatorRepository, InMemoryMonitoredOperatorRepository>();
builder.Services.AddScoped<IMonitoredOperatorCommandService, MonitoredOperatorCommandService>();
builder.Services.AddScoped<IMonitoredOperatorQueryService, MonitoredOperatorQueryService>();

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
