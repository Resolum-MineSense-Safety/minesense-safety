using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Tests.Domain;

public class MonitoringSessionTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);

    private static MonitoringSession StartSession(params SignalType[] signals) =>
        new(new StartMonitoringCommand(Guid.NewGuid(), "CAM-001", Guid.NewGuid(), signals), Guid.NewGuid(), StartedAt);

    [Fact]
    public void Constructor_WithAllSignals_StartsActive()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var command = new StartMonitoringCommand(operatorId, " CAM-001 ", null,
            [SignalType.EyeTracking, SignalType.HeartRate]);

        // Act
        var session = new MonitoringSession(command, assignmentId, StartedAt);

        // Assert
        Assert.Equal(MonitoringStatus.Active, session.Status);
        Assert.Equal(operatorId, session.OperatorId);
        Assert.Equal("CAM-001", session.VehicleCode);
        Assert.Equal(assignmentId, session.AssignmentId);
        Assert.Null(session.PreShiftCheckId);
        Assert.Equal(StartedAt, session.StartedAt);
        Assert.Null(session.StoppedAt);
        Assert.True(session.IsOpen);
        Assert.Empty(session.MissingSignals);
        Assert.Empty(session.SignalEvents);
    }

    [Fact]
    public void Constructor_WithoutSignals_StartsUnavailable()
    {
        // Act
        var session = StartSession();

        // Assert
        Assert.Equal(MonitoringStatus.Unavailable, session.Status);
        Assert.Equal(2, session.MissingSignals.Count);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "CAM-001", "11111111-1111-1111-1111-111111111111")]
    [InlineData("22222222-2222-2222-2222-222222222222", " ", "11111111-1111-1111-1111-111111111111")]
    [InlineData("22222222-2222-2222-2222-222222222222", "CAM-001", "00000000-0000-0000-0000-000000000000")]
    public void Constructor_WithMissingData_ThrowsDomainException(string operatorId, string vehicleCode, string assignmentId)
    {
        // Arrange
        var command = new StartMonitoringCommand(Guid.Parse(operatorId), vehicleCode, null, [SignalType.HeartRate]);

        // Act & Assert
        Assert.Throws<DomainException>(() => new MonitoringSession(command, Guid.Parse(assignmentId), StartedAt));
    }

    [Fact]
    public void ReportSignalLost_AvailableSignal_BecomesPartialAndRecordsEvent()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking, SignalType.HeartRate);
        var lostAt = StartedAt.AddMinutes(10);

        // Act
        session.ReportSignalLost(SignalType.HeartRate, lostAt);

        // Assert
        Assert.Equal(MonitoringStatus.Partial, session.Status);
        Assert.Equal([SignalType.HeartRate], session.MissingSignals);
        Assert.Equal([SignalType.EyeTracking], session.AvailableSignals);
        var signalEvent = Assert.Single(session.SignalEvents);
        Assert.Equal(new SignalEvent(SignalType.HeartRate, SignalEventType.Lost, lostAt), signalEvent);
    }

    [Fact]
    public void ReportSignalLost_LastSignal_BecomesUnavailable()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking);

        // Act
        session.ReportSignalLost(SignalType.EyeTracking, StartedAt.AddMinutes(1));

        // Assert
        Assert.Equal(MonitoringStatus.Unavailable, session.Status);
    }

    [Fact]
    public void ReportSignalLost_UnavailableSignal_ThrowsDomainException()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking);

        // Act & Assert
        Assert.Throws<DomainException>(() => session.ReportSignalLost(SignalType.HeartRate, StartedAt));
    }

    [Fact]
    public void ReportSignalRestored_LostSignal_BecomesActiveAgain()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking, SignalType.HeartRate);
        session.ReportSignalLost(SignalType.EyeTracking, StartedAt.AddMinutes(1));

        // Act
        session.ReportSignalRestored(SignalType.EyeTracking, StartedAt.AddMinutes(2));

        // Assert
        Assert.Equal(MonitoringStatus.Active, session.Status);
        Assert.Equal(2, session.SignalEvents.Count);
        Assert.Equal(SignalEventType.Restored, session.SignalEvents[1].Type);
    }

    [Fact]
    public void ReportSignalRestored_AvailableSignal_ThrowsDomainException()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking);

        // Act & Assert
        Assert.Throws<DomainException>(() => session.ReportSignalRestored(SignalType.EyeTracking, StartedAt));
    }

    [Fact]
    public void Stop_OpenSession_RecordsStopTimeAndStatus()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking);
        var stoppedAt = StartedAt.AddHours(8);

        // Act
        session.Stop(stoppedAt);

        // Assert
        Assert.Equal(MonitoringStatus.Stopped, session.Status);
        Assert.Equal(stoppedAt, session.StoppedAt);
        Assert.False(session.IsOpen);
        Assert.Empty(session.MissingSignals);
    }

    [Fact]
    public void Stop_Twice_ThrowsDomainException()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking);
        session.Stop(StartedAt.AddHours(8));

        // Act & Assert
        Assert.Throws<DomainException>(() => session.Stop(StartedAt.AddHours(9)));
    }

    [Fact]
    public void ReportSignal_OnStoppedSession_ThrowsDomainException()
    {
        // Arrange
        var session = StartSession(SignalType.EyeTracking);
        session.Stop(StartedAt.AddHours(8));

        // Act & Assert
        Assert.Throws<DomainException>(() => session.ReportSignalLost(SignalType.EyeTracking, StartedAt));
        Assert.Throws<DomainException>(() => session.ReportSignalRestored(SignalType.HeartRate, StartedAt));
    }
}
