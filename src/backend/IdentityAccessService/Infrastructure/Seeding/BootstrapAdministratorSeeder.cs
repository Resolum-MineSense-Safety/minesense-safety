using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Domain.Repositories;
using IdentityAccessService.Domain.Services;
using Microsoft.Extensions.Options;

namespace IdentityAccessService.Infrastructure.Seeding;

/// <summary>Creates the configured administrator on startup when it does not exist yet.</summary>
public class BootstrapAdministratorSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<BootstrapAdministratorOptions> options,
    ILogger<BootstrapAdministratorSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var administrator = options.Value;
        if (string.IsNullOrWhiteSpace(administrator.Username) || string.IsNullOrEmpty(administrator.Password)) return;

        using var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (await userRepository.ExistsByUsernameAsync(administrator.Username)) return;

        var userCommandService = scope.ServiceProvider.GetRequiredService<IUserCommandService>();
        await userCommandService.Handle(new SignUpCommand(administrator.Username, administrator.FullName,
            administrator.Password, UserRole.Administrator));
        logger.LogInformation("Bootstrap administrator '{Username}' created.", administrator.Username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
