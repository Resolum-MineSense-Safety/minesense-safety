using DeviceManagementService.Application.Internal.CommandServices;
using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;
using DeviceManagementService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();
builder.Services.AddScoped<IDeviceQueryService, DeviceQueryService>();

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