using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Domain.Services;

namespace MonitoringService.Tests.Domain;

public class MonitoringStatusPolicyTests
{
    [Fact]
    public void Evaluate_BothSignals_ReturnsActive()
    {
        // Arrange
        var signals = new[] { SignalType.EyeTracking, SignalType.HeartRate };

        // Act
        var status = MonitoringStatusPolicy.Evaluate(signals, stopped: false);

        // Assert
        Assert.Equal(MonitoringStatus.Active, status);
        Assert.Empty(MonitoringStatusPolicy.MissingSignals(signals));
    }

    [Fact]
    public void Evaluate_EyeTrackingOnly_ReturnsPartialWithWearableMissing()
    {
        // Arrange
        var signals = new[] { SignalType.EyeTracking };

        // Act
        var status = MonitoringStatusPolicy.Evaluate(signals, stopped: false);

        // Assert
        Assert.Equal(MonitoringStatus.Partial, status);
        Assert.Equal([SignalType.HeartRate], MonitoringStatusPolicy.MissingSignals(signals));
    }

    [Fact]
    public void Evaluate_HeartRateOnly_ReturnsPartialWithCameraMissing()
    {
        // Arrange
        var signals = new[] { SignalType.HeartRate, SignalType.HeartRate };

        // Act
        var status = MonitoringStatusPolicy.Evaluate(signals, stopped: false);

        // Assert
        Assert.Equal(MonitoringStatus.Partial, status);
        Assert.Equal([SignalType.EyeTracking], MonitoringStatusPolicy.MissingSignals(signals));
    }

    [Fact]
    public void Evaluate_NoSignals_ReturnsUnavailable()
    {
        // Act
        var status = MonitoringStatusPolicy.Evaluate([], stopped: false);

        // Assert
        Assert.Equal(MonitoringStatus.Unavailable, status);
        Assert.Equal(2, MonitoringStatusPolicy.MissingSignals([]).Count);
    }

    [Fact]
    public void Evaluate_StoppedSession_ReturnsStopped()
    {
        // Act
        var status = MonitoringStatusPolicy.Evaluate([SignalType.EyeTracking, SignalType.HeartRate], stopped: true);

        // Assert
        Assert.Equal(MonitoringStatus.Stopped, status);
    }
}
