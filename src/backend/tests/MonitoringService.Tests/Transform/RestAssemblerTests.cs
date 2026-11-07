using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Interfaces.REST.Transform;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Tests.Transform;

/// <summary>
/// Pure unit tests of the REST assemblers (no TestHost, so they also run locally).
/// </summary>
public class RestAssemblerTests
{
    [Theory]
    [InlineData(MonitoringStatus.Active, "Monitoreo activo")]
    [InlineData(MonitoringStatus.Unavailable, "Monitoreo no disponible")]
    [InlineData(MonitoringStatus.Stopped, "Monitoreo detenido")]
    public void ToOperatorMessage_NonPartialStatus_ReturnsSpanishMessage(MonitoringStatus status, string expected)
    {
        // Act
        var message = MonitoringStatusResourceFromEntityAssembler.ToOperatorMessage(status, []);

        // Assert
        Assert.Equal(expected, message);
    }

    [Fact]
    public void ToOperatorMessage_Partial_NamesMissingSignal()
    {
        // Act
        var message = MonitoringStatusResourceFromEntityAssembler.ToOperatorMessage(
            MonitoringStatus.Partial, [SignalType.HeartRate]);

        // Assert
        Assert.Equal("Monitoreo parcial: falta frecuencia cardiaca (wearable)", message);
    }

    [Theory]
    [InlineData("eyetracking", SignalType.EyeTracking)]
    [InlineData("HeartRate", SignalType.HeartRate)]
    public void ToSignalType_KnownValue_Parses(string value, SignalType expected)
    {
        // Act & Assert
        Assert.Equal(expected, SignalTypeFromResourceAssembler.ToSignalType(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("Temperature")]
    public void ToSignalType_UnknownValue_ThrowsDomainException(string? value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => SignalTypeFromResourceAssembler.ToSignalType(value));
    }

    [Fact]
    public void ToMonitoringStatus_EmptyKnownAndUnknown_ParsesOrThrows()
    {
        // Act & Assert
        Assert.Null(SignalTypeFromResourceAssembler.ToMonitoringStatus(null));
        Assert.Equal(MonitoringStatus.Partial, SignalTypeFromResourceAssembler.ToMonitoringStatus("partial"));
        Assert.Throws<DomainException>(() => SignalTypeFromResourceAssembler.ToMonitoringStatus("Sleeping"));
    }
}
