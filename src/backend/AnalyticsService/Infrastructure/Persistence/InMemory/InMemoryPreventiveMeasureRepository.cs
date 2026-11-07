using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace AnalyticsService.Infrastructure.Persistence.InMemory;

public class InMemoryPreventiveMeasureRepository : InMemoryRepository<PreventiveMeasure>, IPreventiveMeasureRepository;
