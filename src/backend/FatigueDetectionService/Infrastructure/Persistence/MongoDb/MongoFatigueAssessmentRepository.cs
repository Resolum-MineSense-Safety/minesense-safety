using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace FatigueDetectionService.Infrastructure.Persistence.MongoDb;

public class FatigueAssessmentDocument : MongoAggregateDocument
{
    public string OperatorId { get; set; } = string.Empty;
    public string MonitoringSessionId { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; }
}

/// <summary>Fatigue assessments in MongoDB (ADR-005), indexed by operator and monitoring session.</summary>
public class MongoFatigueAssessmentRepository(IMongoDatabase database)
    : MongoRepository<FatigueAssessment, FatigueAssessmentDocument>(database, "fatigue_assessments"),
      IFatigueAssessmentRepository
{
    protected override FatigueAssessmentDocument CreateDocument(FatigueAssessment assessment) => new()
    {
        OperatorId = assessment.OperatorId.ToString(),
        MonitoringSessionId = assessment.MonitoringSessionId.ToString(),
        RiskLevel = assessment.RiskLevel.ToString(),
        AssessedAt = Utc(assessment.AssessedAt)
    };

    protected override IEnumerable<CreateIndexModel<FatigueAssessmentDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.OperatorId).Descending(item => item.AssessedAt)),
        new(Keys.Ascending(item => item.MonitoringSessionId).Ascending(item => item.AssessedAt))
    ];

    public async Task<IEnumerable<FatigueAssessment>> FindByOperatorIdAsync(Guid operatorId) =>
        await ToEntitiesAsync(Collection.Find(item => item.OperatorId == operatorId.ToString())
            .SortByDescending(item => item.AssessedAt).ThenBy(item => item.Id));

    public async Task<IEnumerable<FatigueAssessment>> FindByMonitoringSessionIdAsync(Guid monitoringSessionId) =>
        await ToEntitiesAsync(Collection.Find(item => item.MonitoringSessionId == monitoringSessionId.ToString())
            .SortBy(item => item.AssessedAt).ThenBy(item => item.Id));
}
