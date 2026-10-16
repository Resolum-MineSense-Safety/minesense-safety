using EdgeSynchronizationService.Application.Internal.CommandServices;
using EdgeSynchronizationService.Application.Internal.QueryServices;
using EdgeSynchronizationService.Domain.Repositories;
using EdgeSynchronizationService.Domain.Services;
using EdgeSynchronizationService.Infrastructure.Messaging;
using EdgeSynchronizationService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMineSenseWebApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton<IOutboxEventRepository, InMemoryOutboxEventRepository>();
builder.Services.AddScoped<ISynchronizationCommandService, SynchronizationCommandService>();
builder.Services.AddScoped<ISynchronizationQueryService, SynchronizationQueryService>();
builder.Services.AddSingleton<IMqttPublisher, MqttPublisher>();
builder.Services.AddHostedService<MqttSubscriber>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapControllers();
await app.RunAsync();

public partial class Program;
