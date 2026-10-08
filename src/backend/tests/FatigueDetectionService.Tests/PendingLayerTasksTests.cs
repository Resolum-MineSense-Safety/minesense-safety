using FatigueDetectionService.Application.Internal.QueryServices;
using FatigueDetectionService.Domain.Model.Queries;
using FatigueDetectionService.Infrastructure.Persistence.InMemory;

namespace FatigueDetectionService.Tests;

/// <summary>
/// Guards the contracts left for the layer tasks (docs: reparto por capas).
/// When you implement your task, delete its test here and add the real tests in your layer folder.
/// </summary>
public class PendingLayerTasksTests
{
    private readonly InMemoryFatigueAssessmentRepository _repository = new();

    [Fact]
    public async Task F2_QueryBySession_IsPending()
    {
        // Arrange
        var service = new FatigueAssessmentQueryService(_repository);
        var query = new GetFatigueAssessmentsByMonitoringSessionIdQuery(Guid.NewGuid());

        // Act
        var exception = await Assert.ThrowsAsync<NotImplementedException>(() => service.Handle(query));

        // Assert
        Assert.StartsWith("F2:", exception.Message);
    }

    [Fact]
    public async Task F3_FindByMonitoringSessionId_IsPending()
    {
        // Arrange
        var monitoringSessionId = Guid.NewGuid();

        // Act
        var exception = await Assert.ThrowsAsync<NotImplementedException>(
            () => _repository.FindByMonitoringSessionIdAsync(monitoringSessionId));

        // Assert
        Assert.StartsWith("F3:", exception.Message);
    }
}
