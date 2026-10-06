using IdentityAccessService.Application.Internal.CommandServices;
using IdentityAccessService.Application.Internal.OutboundServices;
using IdentityAccessService.Application.Internal.QueryServices;
using IdentityAccessService.Domain.Repositories;
using IdentityAccessService.Domain.Services;
using IdentityAccessService.Infrastructure.Hashing;
using IdentityAccessService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMineSenseWebApi();

// Identity & Access bounded context (EP08)
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IHashingService, Pbkdf2HashingService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();

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
