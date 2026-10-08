using AlertService.Application.Internal.CommandServices;
using AlertService.Application.Internal.QueryServices;
using AlertService.Domain.Repositories;
using AlertService.Domain.Services;
using AlertService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Alerts bounded context (EP03)
builder.Services.AddSingleton<IAlertRepository, InMemoryAlertRepository>();
builder.Services.AddScoped<IAlertCommandService, AlertCommandService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();

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