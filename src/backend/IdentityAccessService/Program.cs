using IdentityAccessService.Application.Internal.CommandServices;
using IdentityAccessService.Application.Internal.OutboundServices;
using IdentityAccessService.Application.Internal.QueryServices;
using IdentityAccessService.Domain.Repositories;
using IdentityAccessService.Domain.Services;
using IdentityAccessService.Infrastructure.Hashing;
using IdentityAccessService.Infrastructure.Persistence.InMemory;
using IdentityAccessService.Infrastructure.Seeding;
using IdentityAccessService.Infrastructure.Tokens;
using IdentityAccessService.Interfaces.ASP.Configuration;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers, kebab-case routes, domain error handling (HTTP 422/409) and Swagger
builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);

// JWT bearer tokens signed with RS256 and published as JWKS (ADR-007)
builder.Services.AddIdentityAccessAuthentication(builder.Configuration);

// Identity & Access bounded context (EP08)
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
builder.Services.AddSingleton<IHashingService, Pbkdf2HashingService>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IAuthenticationCommandService, AuthenticationCommandService>();

builder.Services.Configure<BootstrapAdministratorOptions>(
    builder.Configuration.GetSection(BootstrapAdministratorOptions.SectionName));
builder.Services.AddHostedService<BootstrapAdministratorSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
