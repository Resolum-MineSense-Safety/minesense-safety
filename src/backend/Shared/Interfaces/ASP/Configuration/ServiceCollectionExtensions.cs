using Microsoft.Extensions.DependencyInjection;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers controllers, kebab-case routes, domain error handling and Swagger
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
        return services;
    }
}
