using FatigueDetectionService.Application.Internal.CommandServices;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Infrastructure.Persistence.InMemory;

namespace FatigueDetectionService.Tests.Application;

public class FatigueAssessmentCommandServiceTests
{
    private readonly InMemoryFatigueAssessmentRepository _repository = new();
    private readonly FatigueAssessmentCommandService _service;

    public FatigueAssessmentCommandServiceTests()
    {
        _service = new FatigueAssessmentCommandService(_repository, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_AssessFatigueCommand_PersistsAssessment()
    {
        // Arrange
        var command = new AssessFatigueCommand(Guid.NewGuid(), Guid.NewGuid(), 0.20, 18, 35);

        // Act
        var assessment = await _service.Handle(command);

        // Assert
        Assert.NotNull(await _repository.FindByIdAsync(assessment.Id));
    }
}
