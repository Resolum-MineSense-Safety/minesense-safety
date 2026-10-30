using FatigueDetectionService.Application.Internal.CommandServices;
using FatigueDetectionService.Application.Internal.QueryServices;
using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Model.Queries;

namespace FatigueDetectionService.Tests.Application;

public class FatigueAssessmentServicesTests
{
    private readonly FakeFatigueAssessmentRepository _repository = new();
    private readonly FatigueAssessmentCommandService _commandService;
    private readonly FatigueAssessmentQueryService _queryService;

    public FatigueAssessmentServicesTests()
    {
        _commandService = new FatigueAssessmentCommandService(_repository, TimeProvider.System);
        _queryService = new FatigueAssessmentQueryService(_repository);
    }

    private Task<FatigueAssessment> AssessAsync(Guid operatorId, Guid sessionId) =>
        _commandService.Handle(new AssessFatigueCommand(operatorId, sessionId, 0.20, 18, 35));

    [Fact]
    public async Task Handle_AssessFatigueCommand_PersistsAssessment()
    {
        // Act
        var assessment = await AssessAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.Same(assessment, await _queryService.Handle(new GetFatigueAssessmentByIdQuery(assessment.Id)));
    }

    [Fact]
    public async Task Handle_ByOperatorAndLatest_ReturnHistoryAndNewest()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await AssessAsync(operatorId, Guid.NewGuid());
        await AssessAsync(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var history = await _queryService.Handle(new GetFatigueAssessmentsByOperatorIdQuery(operatorId));
        var latest = await _queryService.Handle(new GetLatestFatigueAssessmentByOperatorIdQuery(operatorId));

        // Assert
        Assert.Single(history);
        Assert.Equal(operatorId, latest!.OperatorId);
    }

    [Fact]
    public async Task Handle_BySession_ReturnsOnlyThatSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        await AssessAsync(Guid.NewGuid(), sessionId);
        await AssessAsync(Guid.NewGuid(), sessionId);
        await AssessAsync(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var assessments = await _queryService.Handle(new GetFatigueAssessmentsByMonitoringSessionIdQuery(sessionId));

        // Assert
        Assert.Equal(2, assessments.Count());
    }

    [Fact]
    public async Task Handle_BySessionWithEmptyId_ReturnsEmptyWithoutQueryingRepository()
    {
        // Act
        var assessments = await _queryService.Handle(new GetFatigueAssessmentsByMonitoringSessionIdQuery(Guid.Empty));

        // Assert
        Assert.Empty(assessments);
        Assert.Equal(0, _repository.SessionQueries);
    }
}
