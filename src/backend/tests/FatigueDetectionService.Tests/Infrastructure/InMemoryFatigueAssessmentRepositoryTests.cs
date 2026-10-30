using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Infrastructure.Persistence.InMemory;

namespace FatigueDetectionService.Tests.Infrastructure;

public class InMemoryFatigueAssessmentRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private readonly InMemoryFatigueAssessmentRepository _repository = new();

    private static FatigueAssessment Assessment(Guid operatorId, Guid sessionId, int minutes) =>
        new(new AssessFatigueCommand(operatorId, sessionId, 0.20, 18, 35), Start.AddMinutes(minutes));

    [Fact]
    public async Task AddAndUpdate_StoreTheAssessment()
    {
        // Arrange
        var assessment = Assessment(Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        await _repository.AddAsync(assessment);
        await _repository.UpdateAsync(assessment);

        // Assert
        Assert.Same(assessment, await _repository.FindByIdAsync(assessment.Id));
        Assert.Single(await _repository.ListAsync());
        Assert.Null(await _repository.FindByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FindByOperatorIdAsync_ReturnsNewestFirst()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _repository.AddAsync(Assessment(operatorId, Guid.NewGuid(), 1));
        await _repository.AddAsync(Assessment(operatorId, Guid.NewGuid(), 5));
        await _repository.AddAsync(Assessment(Guid.NewGuid(), Guid.NewGuid(), 9));

        // Act
        var assessments = (await _repository.FindByOperatorIdAsync(operatorId)).ToList();

        // Assert
        Assert.Equal(2, assessments.Count);
        Assert.Equal(Start.AddMinutes(5), assessments[0].AssessedAt);
    }

    [Fact]
    public async Task FindByMonitoringSessionIdAsync_ReturnsOldestFirst()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        await _repository.AddAsync(Assessment(Guid.NewGuid(), sessionId, 7));
        await _repository.AddAsync(Assessment(Guid.NewGuid(), sessionId, 2));
        await _repository.AddAsync(Assessment(Guid.NewGuid(), Guid.NewGuid(), 4));

        // Act
        var assessments = (await _repository.FindByMonitoringSessionIdAsync(sessionId)).ToList();

        // Assert
        Assert.Equal(2, assessments.Count);
        Assert.Equal(Start.AddMinutes(2), assessments[0].AssessedAt);
    }
}
