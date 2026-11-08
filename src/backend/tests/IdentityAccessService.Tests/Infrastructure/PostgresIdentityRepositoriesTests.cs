using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Infrastructure.Persistence.PostgreSql;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace IdentityAccessService.Tests.Infrastructure;

public class PostgresIdentityRepositoriesTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, 123, TimeSpan.Zero);

    [Fact]
    public void Snapshot_RoundTrip_RestoresUserScopesAndSessionRevocation()
    {
        // Arrange
        var miningUnitId = Guid.NewGuid();
        var user = new User("  Supervisor.One ", "Ana Torres", UserRole.Supervisor, "hash");
        user.GrantMiningUnit(miningUnitId);
        user.Deactivate();
        var session = new Session(user.Id, "token-hash", Start.AddTicks(7), TimeSpan.FromDays(7));
        session.Revoke(Start.AddHours(1).ToOffset(TimeSpan.FromHours(-5)));

        // Act
        var restoredUser = AggregateSnapshot.Deserialize<User>(AggregateSnapshot.Serialize(user));
        var restoredSession = AggregateSnapshot.Deserialize<Session>(AggregateSnapshot.Serialize(session));

        // Assert
        Assert.Equal("supervisor.one", restoredUser.Username);
        Assert.False(restoredUser.IsActive);
        Assert.Equal(new[] { miningUnitId }, restoredUser.MiningUnitIds);
        Assert.Equal(user.PasswordHash, restoredUser.PasswordHash);
        Assert.Equal(session.CreatedAt.Ticks, restoredSession.CreatedAt.Ticks);
        Assert.Equal(TimeSpan.FromHours(-5), restoredSession.RevokedAt!.Value.Offset);
        Assert.True(restoredSession.IsRevoked);
    }

    [PostgresFact]
    public async Task Users_RealServer_PersistAcrossRestartAndKeepUsernamesUnique()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("identity_tests");
        var repository = await postgres.PrepareAsync(new PostgresUserRepository(postgres.DataSource));
        var miningUnitId = Guid.NewGuid();
        var supervisor = new User("Supervisor.One", "Ana Torres", UserRole.Supervisor, "hash-1");
        var operatorUser = new User("operator.two", "Luis Rojas", UserRole.Operator, "hash-2");

        // Act
        await repository.AddAsync(supervisor);
        await repository.AddAsync(operatorUser);
        supervisor.GrantMiningUnit(miningUnitId);
        supervisor.ChangeRole(UserRole.SafetyManager);
        await repository.UpdateAsync(supervisor);
        var duplicate = await Assert.ThrowsAsync<DomainConflictException>(() =>
            repository.AddAsync(new User(" SUPERVISOR.one ", "Other", UserRole.Operator, "hash-3")));
        var restarted = new PostgresUserRepository(postgres.Reopen());

        // Assert
        Assert.NotNull(duplicate);
        var stored = (await restarted.FindByUsernameAsync("  SUPERVISOR.ONE"))!;
        Assert.Equal(supervisor.Id, stored.Id);
        Assert.Equal(UserRole.SafetyManager, stored.Role);
        Assert.Equal(new[] { miningUnitId }, stored.MiningUnitIds);
        Assert.True(await restarted.ExistsByUsernameAsync("Operator.Two"));
        Assert.False(await restarted.ExistsByUsernameAsync("nobody"));
        Assert.Null(await restarted.FindByUsernameAsync("nobody"));
        Assert.Equal(2, (await restarted.ListAsync()).Count());
    }

    [PostgresFact]
    public async Task Sessions_RealServer_FilterByUserAndTokenAndKeepTokenHashesUnique()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("identity_tests");
        var repository = await postgres.PrepareAsync(new PostgresSessionRepository(postgres.DataSource));
        var userId = Guid.NewGuid();
        var first = new Session(userId, "hash-a", Start, TimeSpan.FromDays(7));
        var rotated = new Session(userId, "hash-b", Start.AddMinutes(30), TimeSpan.FromDays(7));
        var other = new Session(Guid.NewGuid(), "hash-c", Start.AddMinutes(10), TimeSpan.FromDays(7));

        // Act
        await repository.AddAsync(rotated);
        await repository.AddAsync(first);
        await repository.AddAsync(other);
        first.Revoke(Start.AddMinutes(30), rotated.Id);
        await repository.UpdateAsync(first);
        var duplicate = await Assert.ThrowsAsync<DomainConflictException>(() =>
            repository.AddAsync(new Session(Guid.NewGuid(), "hash-b", Start, TimeSpan.FromDays(1))));
        var restarted = new PostgresSessionRepository(postgres.Reopen());

        // Assert
        Assert.NotNull(duplicate);
        var revoked = (await restarted.FindByRefreshTokenHashAsync("hash-a"))!;
        Assert.Equal(first.Id, revoked.Id);
        Assert.Equal(rotated.Id, revoked.ReplacedBySessionId);
        Assert.False(revoked.IsActiveAt(Start.AddMinutes(31)));
        Assert.Equal(new[] { first.Id, rotated.Id }, (await restarted.FindByUserIdAsync(userId)).Select(s => s.Id));
        Assert.Null(await restarted.FindByRefreshTokenHashAsync("unknown"));
    }
}
