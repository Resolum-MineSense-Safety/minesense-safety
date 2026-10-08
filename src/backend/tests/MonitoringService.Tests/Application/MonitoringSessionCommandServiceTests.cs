using MonitoringService.Application.Internal.CommandServices;
using MonitoringService.Application.Internal.OutboundServices;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Infrastructure.Persistence.InMemory;
using MonitoringService.Tests.Fakes;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Tests.Application;

public class MonitoringSessionCommandServiceTests
{
    private readonly InMemoryMonitoringSessionRepository _repository = new();
    private readonly FakeOperationalContextService _operationalContext = new();
    private readonly MonitoringSessionCommandService _service;

    public MonitoringSessionCommandServiceTests()
    {
        _service = new MonitoringSessionCommandService(_repository, _operationalContext, TimeProvider.System);
    }

    private static StartMonitoringCommand StartCommand(Guid operatorId, string vehicleCode = "CAM-001") =>
        new(operatorId, vehicleCode, Guid.NewGuid(), [SignalType.EyeTracking, SignalType.HeartRate]);

    [Fact]
    public async Task Handle_StartWithValidContext_PersistsSessionWithAssignment()
    {
        // Arrange
        var command = StartCommand(Guid.NewGuid());

        // Act
        var session = await _service.Handle(command);

        // Assert
        Assert.NotNull(await _repository.FindByIdAsync(session.Id));
        Assert.Equal(_operationalContext.AssignmentId, session.AssignmentId);
        Assert.Equal(MonitoringStatus.Active, session.Status);
    }

    [Fact]
    public async Task Handle_StartWithInvalidContext_ThrowsDomainExceptionWithReason()
    {
        // Arrange
        var command = StartCommand(Guid.NewGuid(), FakeOperationalContextService.UnassignedPrefix + "-9");

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.Handle(command));

        // Assert
        Assert.Equal(FakeOperationalContextService.InvalidReason, exception.Message);
        Assert.Empty(await _repository.ListAsync());
    }

    [Fact]
    public async Task Handle_StartWithValidContextWithoutAssignment_ThrowsDomainException()
    {
        // Arrange
        var service = new MonitoringSessionCommandService(_repository,
            new StubOperationalContextService(new OperationalContext(true, null, null)), TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.Handle(StartCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task Handle_StartWithInvalidContextWithoutReason_ThrowsDomainException()
    {
        // Arrange
        var service = new MonitoringSessionCommandService(_repository,
            new StubOperationalContextService(new OperationalContext(false, null, null)), TimeProvider.System);

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() => service.Handle(StartCommand(Guid.NewGuid())));

        // Assert
        Assert.Equal("The operational context is not valid.", exception.Message);
    }

    [Fact]
    public async Task Handle_StartSecondOpenSession_ThrowsDomainExceptionWithoutValidating()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _service.Handle(StartCommand(operatorId));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.Handle(StartCommand(operatorId, "CAM-002")));
        Assert.Equal(1, _operationalContext.Calls);
    }

    [Fact]
    public async Task Handle_StartAfterStop_CreatesNewSession()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var first = await _service.Handle(StartCommand(operatorId));
        await _service.Handle(new StopMonitoringCommand(first.Id));

        // Act
        var second = await _service.Handle(StartCommand(operatorId));

        // Assert
        Assert.NotEqual(first.Id, second.Id);
        Assert.True(second.IsOpen);
    }

    [Fact]
    public async Task Handle_SignalLostAndRestored_UpdatesStatus()
    {
        // Arrange
        var session = await _service.Handle(StartCommand(Guid.NewGuid()));

        // Act
        var afterLoss = await _service.Handle(new ReportSignalLostCommand(session.Id, SignalType.HeartRate));
        var lossStatus = afterLoss!.Status;
        var afterRestore = await _service.Handle(new ReportSignalRestoredCommand(session.Id, SignalType.HeartRate));

        // Assert
        Assert.Equal(MonitoringStatus.Partial, lossStatus);
        Assert.Equal(MonitoringStatus.Active, afterRestore!.Status);
        Assert.Equal(2, afterRestore.SignalEvents.Count);
    }

    [Fact]
    public async Task Handle_Stop_MarksSessionStopped()
    {
        // Arrange
        var session = await _service.Handle(StartCommand(Guid.NewGuid()));

        // Act
        var stopped = await _service.Handle(new StopMonitoringCommand(session.Id));

        // Assert
        Assert.Equal(MonitoringStatus.Stopped, stopped!.Status);
        Assert.NotNull(stopped.StoppedAt);
    }

    [Fact]
    public async Task Handle_CommandsOnUnknownSession_ReturnNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var lost = await _service.Handle(new ReportSignalLostCommand(unknownId, SignalType.EyeTracking));
        var restored = await _service.Handle(new ReportSignalRestoredCommand(unknownId, SignalType.EyeTracking));
        var stopped = await _service.Handle(new StopMonitoringCommand(unknownId));

        // Assert
        Assert.Null(lost);
        Assert.Null(restored);
        Assert.Null(stopped);
    }

    private sealed class StubOperationalContextService(OperationalContext context) : IOperationalContextService
    {
        public Task<OperationalContext> ValidateAsync(Guid operatorId, string vehicleCode, DateOnly date) =>
            Task.FromResult(context);
    }
}
