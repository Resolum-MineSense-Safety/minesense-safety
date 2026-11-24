using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers controllers, kebab-case routes, domain error handling, Swagger and health checks
    /// the same way in every microservice.
    /// </summary>
    public static IServiceCollection AddMineSenseWebApi(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Conventions.Add(new KebabCaseRouteNamingConvention());
            options.Filters.Add<DomainExceptionFilter>();
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();
        services.AddTransient<IStartupFilter, HealthProbesStartupFilter>();
        return services;
    }
}
