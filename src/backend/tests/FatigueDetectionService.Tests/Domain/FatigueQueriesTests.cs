using FatigueDetectionService.Domain.Model.Queries;

namespace FatigueDetectionService.Tests.Domain;

public class FatigueQueriesTests
{
    [Fact]
    public void Queries_KeepTheRequestedIdentifiers()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var byId = new GetFatigueAssessmentByIdQuery(id);
        var byOperator = new GetFatigueAssessmentsByOperatorIdQuery(id);
        var latest = new GetLatestFatigueAssessmentByOperatorIdQuery(id);
        var bySession = new GetFatigueAssessmentsByMonitoringSessionIdQuery(id);

        // Assert
        Assert.Equal(id, byId.FatigueAssessmentId);
        Assert.Equal(id, byOperator.OperatorId);
        Assert.Equal(id, latest.OperatorId);
        Assert.Equal(id, bySession.MonitoringSessionId);
    }
}
