using FatigueDetectionService.Application.Internal.CommandServices;
using FatigueDetectionService.Application.Internal.QueryServices;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;
using FatigueDetectionService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Fatigue Detection bounded context (EP02)
builder.Services.AddSingleton<IFatigueAssessmentRepository, InMemoryFatigueAssessmentRepository>();
builder.Services.AddScoped<IFatigueAssessmentCommandService, FatigueAssessmentCommandService>();
builder.Services.AddScoped<IFatigueAssessmentQueryService, FatigueAssessmentQueryService>();

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
