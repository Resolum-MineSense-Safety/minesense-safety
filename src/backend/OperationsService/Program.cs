using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using OperationsService.Application.Internal.CommandServices;
using OperationsService.Application.Internal.QueryServices;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;
using OperationsService.Infrastructure.Persistence.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Operations bounded context (EP08): operational structure and operator assignments
builder.Services.AddSingleton<IMiningUnitRepository, InMemoryMiningUnitRepository>();
builder.Services.AddSingleton<IFleetRepository, InMemoryFleetRepository>();
builder.Services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
builder.Services.AddSingleton<IOperatorAssignmentRepository, InMemoryOperatorAssignmentRepository>();
builder.Services.AddScoped<OperationalStructureValidator>();
builder.Services.AddScoped<IMiningUnitCommandService, MiningUnitCommandService>();
builder.Services.AddScoped<IMiningUnitQueryService, MiningUnitQueryService>();
builder.Services.AddScoped<IFleetCommandService, FleetCommandService>();
builder.Services.AddScoped<IFleetQueryService, FleetQueryService>();
builder.Services.AddScoped<IVehicleCommandService, VehicleCommandService>();
builder.Services.AddScoped<IVehicleQueryService, VehicleQueryService>();
builder.Services.AddScoped<IOperatorAssignmentCommandService, OperatorAssignmentCommandService>();
builder.Services.AddScoped<IOperatorAssignmentQueryService, OperatorAssignmentQueryService>();

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
