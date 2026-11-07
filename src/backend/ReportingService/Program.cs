using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;
using ReportingService.Infrastructure.Persistence.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Reporting bounded context (EP10, scaffold: endpoints return 501 until Fase 1)
builder.Services.AddSingleton<IReportDefinitionRepository, InMemoryReportDefinitionRepository>();
builder.Services.AddSingleton<IReportJobRepository, InMemoryReportJobRepository>();
builder.Services.AddSingleton<IReportArtifactRepository, InMemoryReportArtifactRepository>();
builder.Services.AddScoped<IReportingCommandService, ReportingCommandService>();
builder.Services.AddScoped<IReportingQueryService, ReportingQueryService>();

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
