using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Repositories;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Infrastructure.Persistence.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422) and Swagger
builder.Services.AddMineSenseWebApi();

// Identity & Access tokens (ADR-007), enforced when Authentication:IdentityUrl is set
builder.Services.AddMineSenseAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Privacy & Audit bounded context (EP09/EP10, scaffold: endpoints return 501 until Fase 1)
builder.Services.AddSingleton<IProcessingConsentRepository, InMemoryProcessingConsentRepository>();
builder.Services.AddSingleton<IAccessAuditRepository, InMemoryAccessAuditRepository>();
builder.Services.AddSingleton<IComplianceReportRepository, InMemoryComplianceReportRepository>();
builder.Services.AddScoped<IPrivacyAuditCommandService, PrivacyAuditCommandService>();
builder.Services.AddScoped<IPrivacyAuditQueryService, PrivacyAuditQueryService>();

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
