using IncidentManagementService.Application.Internal.CommandServices;
using IncidentManagementService.Application.Internal.QueryServices;
using IncidentManagementService.Domain.Repositories;
using IncidentManagementService.Domain.Services;
using IncidentManagementService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Incident Management bounded context (EP05)
builder.Services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
builder.Services.AddScoped<IIncidentCommandService, IncidentCommandService>();
builder.Services.AddScoped<IIncidentQueryService, IncidentQueryService>();

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
