using FeedbackReliabilityService.Application.Internal.CommandServices;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Application.Internal.QueryServices;
using FeedbackReliabilityService.Domain.Repositories;
using FeedbackReliabilityService.Domain.Services;
using FeedbackReliabilityService.Infrastructure.Alerts;
using FeedbackReliabilityService.Infrastructure.Persistence.InMemory;
using FeedbackReliabilityService.Infrastructure.Persistence.MongoDb;
using FeedbackReliabilityService.Infrastructure.Security;
using FeedbackReliabilityService.Interfaces.REST;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMineSenseWebApi();
builder.Services.AddControllers(options => options.Filters.Add<FeedbackExceptionFilter>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
builder.Services.AddScoped<IFeedbackCommandService, FeedbackCommandService>();
builder.Services.AddScoped<FeedbackQueryService>();
builder.Services.AddOptions<ReliabilityOptions>().BindConfiguration("Reliability")
    .Validate(options => options.MinimumSampleSize > 0, "MinimumSampleSize must be positive.").ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.Audience = builder.Configuration["Authentication:Audience"] ?? "minesense-feedback";
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "sub", RoleClaimType = "role", ValidateIssuer = true, ValidateAudience = true,
        ValidateLifetime = true, ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var subject) || subject == Guid.Empty)
                context.Fail("A GUID subject is required.");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
builder.Services.AddHttpClient<IAlertSource, HttpAlertSource>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AlertServiceUrl"] ?? "http://localhost:5233/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var mongoConnection = builder.Configuration.GetConnectionString("MongoDb");
if (!string.IsNullOrWhiteSpace(mongoConnection))
{
    builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));
    builder.Services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>()
        .GetDatabase(builder.Configuration["MongoDb:Database"] ?? "minesense_feedback"));
    builder.Services.AddSingleton<MongoAlertEvaluationRepository>();
    builder.Services.AddSingleton<IAlertEvaluationRepository>(provider => provider.GetRequiredService<MongoAlertEvaluationRepository>());
}
else if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IAlertEvaluationRepository, InMemoryAlertEvaluationRepository>();
}
else
{
    throw new InvalidOperationException("ConnectionStrings:MongoDb is required outside Development/Testing.");
}
builder.Services.AddHealthChecks();

var app = builder.Build();
if (!string.IsNullOrWhiteSpace(mongoConnection))
    await app.Services.GetRequiredService<MongoAlertEvaluationRepository>().InitializeAsync(app.Lifetime.ApplicationStopping);
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live").AllowAnonymous();
await app.RunAsync();
public partial class Program;
