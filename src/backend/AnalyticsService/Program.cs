using AnalyticsService.Application.Internal.CommandServices;
using AnalyticsService.Application.Internal.QueryServices;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;
using AnalyticsService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422/409) and Swagger
builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Analytics bounded context (EP11)
builder.Services.AddSingleton<IFatigueMetricRepository, InMemoryFatigueMetricRepository>();
builder.Services.AddSingleton<IPreventiveMeasureRepository, InMemoryPreventiveMeasureRepository>();
builder.Services.AddScoped<IFatigueMetricCommandService, FatigueMetricCommandService>();
builder.Services.AddScoped<IPreventiveMeasureCommandService, PreventiveMeasureCommandService>();
builder.Services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();

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
