using MonitoringService.Application.Internal.QueryServices;
using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Model.Queries;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Infrastructure.Persistence.InMemory;

namespace MonitoringService.Tests.Application;

public class MonitoringSessionQueryServiceTests
{
    private static readonly DateTimeOffset Morning = new(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);

    private readonly InMemoryMonitoringSessionRepository _repository = new();
    private readonly MonitoringSessionQueryService _service;

    public MonitoringSessionQueryServiceTests()
    {
        _service = new MonitoringSessionQueryService(_repository);
    }

    private async Task<MonitoringSession> AddSessionAsync(Guid operatorId, DateTimeOffset startedAt,
        params SignalType[] signals)
    {
        var session = new MonitoringSession(
            new StartMonitoringCommand(operatorId, "CAM-001", null, signals), Guid.NewGuid(), startedAt);
        await _repository.AddAsync(session);
        return session;
    }

    [Fact]
    public async Task Handle_GetCurrentStatus_PrefersOpenSession()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var open = await AddSessionAsync(operatorId, Morning, SignalType.EyeTracking);
        var laterStopped = await AddSessionAsync(operatorId, Morning.AddHours(1), SignalType.HeartRate);
        laterStopped.Stop(Morning.AddHours(2));

        // Act
        var current = await _service.Handle(new GetCurrentStatusByOperatorQuery(operatorId));

        // Assert
        Assert.Equal(open.Id, current!.Id);
    }

    [Fact]
    public async Task Handle_GetCurrentStatus_WithoutOpenSession_ReturnsLatest()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var older = await AddSessionAsync(operatorId, Morning, SignalType.EyeTracking);
        older.Stop(Morning.AddHours(1));
        var latest = await AddSessionAsync(operatorId, Morning.AddHours(2), SignalType.EyeTracking);
        latest.Stop(Morning.AddHours(3));

        // Act
        var current = await _service.Handle(new GetCurrentStatusByOperatorQuery(operatorId));

        // Assert
        Assert.Equal(latest.Id, current!.Id);
    }

    [Fact]
    public async Task Handle_GetCurrentStatus_UnknownOperator_ReturnsNull()
    {
        // Act
        var current = await _service.Handle(new GetCurrentStatusByOperatorQuery(Guid.NewGuid()));

        // Assert
        Assert.Null(current);
    }

    [Fact]
    public async Task Handle_GetSessions_FiltersByStatus()
    {
        // Arrange
        var active = await AddSessionAsync(Guid.NewGuid(), Morning, SignalType.EyeTracking, SignalType.HeartRate);
        var partial = await AddSessionAsync(Guid.NewGuid(), Morning, SignalType.HeartRate);

        // Act
        var all = await _service.Handle(new GetSessionsQuery());
        var onlyPartial = await _service.Handle(new GetSessionsQuery(MonitoringStatus.Partial));

        // Assert
        Assert.Equal(2, all.Count());
        Assert.Equal(partial.Id, Assert.Single(onlyPartial).Id);
        Assert.Equal(active.Id, (await _service.Handle(new GetSessionByIdQuery(active.Id)))!.Id);
    }
}
