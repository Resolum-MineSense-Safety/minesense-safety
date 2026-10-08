using FatigueDetectionService.Application.Internal.CommandServices;
using FatigueDetectionService.Application.Internal.QueryServices;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;
using FatigueDetectionService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Fatigue Detection bounded context (EP02)
builder.Services.AddSingleton<IFatigueAssessmentRepository, InMemoryFatigueAssessmentRepository>();
builder.Services.AddScoped<IFatigueAssessmentCommandService, FatigueAssessmentCommandService>();
builder.Services.AddScoped<IFatigueAssessmentQueryService, FatigueAssessmentQueryService>();

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