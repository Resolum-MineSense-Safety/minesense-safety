using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using OperationsService.Application.Internal.CommandServices;
using OperationsService.Application.Internal.QueryServices;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;
using OperationsService.Infrastructure.Persistence.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// Tokens are signed by Identity & Access and validated here with its public key (JWKS, ADR-007).
// Authentication:Authority is the base URL of Identity & Access, which publishes the discovery document.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.Audience = builder.Configuration["Authentication:Audience"] ?? "minesense-services";
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:RequireHttpsMetadata", true);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "unique_name", RoleClaimType = "role", ValidateIssuer = true, ValidateAudience = true,
        ValidateLifetime = true, ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

// Operations bounded context (EP08): operational structure and operator assignments
builder.Services.AddSingleton<IMiningUnitRepository, InMemoryMiningUnitRepository>();
builder.Services.AddSingleton<IFleetRepository, InMemoryFleetRepository>();
builder.Services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
builder.Services.AddSingleton<IOperatorRepository, InMemoryOperatorRepository>();
builder.Services.AddSingleton<IOperatorAssignmentRepository, InMemoryOperatorAssignmentRepository>();
builder.Services.AddScoped<OperationalStructureValidator>();
builder.Services.AddScoped<IMiningUnitCommandService, MiningUnitCommandService>();
builder.Services.AddScoped<IMiningUnitQueryService, MiningUnitQueryService>();
builder.Services.AddScoped<IFleetCommandService, FleetCommandService>();
builder.Services.AddScoped<IFleetQueryService, FleetQueryService>();
builder.Services.AddScoped<IVehicleCommandService, VehicleCommandService>();
builder.Services.AddScoped<IVehicleQueryService, VehicleQueryService>();
builder.Services.AddScoped<IOperatorCommandService, OperatorCommandService>();
builder.Services.AddScoped<IOperatorQueryService, OperatorQueryService>();
builder.Services.AddScoped<IOperatorAssignmentCommandService, OperatorAssignmentCommandService>();
builder.Services.AddScoped<IOperatorAssignmentQueryService, OperatorAssignmentQueryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

public partial class Program;
