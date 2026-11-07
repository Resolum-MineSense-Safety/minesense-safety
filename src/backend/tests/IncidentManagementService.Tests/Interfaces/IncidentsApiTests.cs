using System.Net;
using System.Net.Http.Json;
using IncidentManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentManagementService.Tests.Interfaces;

public class IncidentsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid Supervisor = Guid.NewGuid();
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<IncidentResource> OpenIncidentAsync(string severity = "Critical", Guid? operatorId = null)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/incidents",
            new OpenIncidentResource(operatorId ?? Guid.NewGuid(), Guid.NewGuid(), severity));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<IncidentResource>())!;
    }

    private Task<HttpResponseMessage> RecordActionAsync(Guid incidentId) =>
        _client.PostAsJsonAsync($"/api/v1/incidents/{incidentId}/actions",
            new RecordIncidentActionResource("PreventivePause", "Stop at the next bay", "Operator rested",
                Supervisor, DateTimeOffset.UtcNow));

    [Fact]
    public async Task PostIncident_WithValidResource_ReturnsCreatedPendingIncident()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/incidents",
            new OpenIncidentResource(Guid.NewGuid(), Guid.NewGuid(), "Critical"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var incident = await response.Content.ReadFromJsonAsync<IncidentResource>();
        Assert.Equal("Pending", incident!.Status);
        Assert.Equal("Critical", incident.Severity);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostIncident_WithUnknownSeverity_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/incidents",
            new OpenIncidentResource(Guid.NewGuid(), Guid.NewGuid(), "Sleepy"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetIncidents_ByStatusAndOperator_ReturnsMatchingIncidents()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await OpenIncidentAsync(operatorId: operatorId);
        await OpenIncidentAsync();

        // Act
        var incidents = await _client.GetFromJsonAsync<List<IncidentResource>>(
            $"/api/v1/incidents?status=pending&operatorId={operatorId}");

        // Assert
        Assert.Equal(operatorId, Assert.Single(incidents!).OperatorId);
    }

    [Fact]
    public async Task GetIncidents_WithUnknownStatus_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/incidents?status=Lost");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UnknownIncident_EveryEndpoint_ReturnsNotFound()
    {
        // Arrange
        var unknown = Guid.NewGuid();

        // Act
        var responses = new[]
        {
            await _client.GetAsync($"/api/v1/incidents/{unknown}"),
            await _client.PatchAsJsonAsync($"/api/v1/incidents/{unknown}/assignment", new AssignIncidentResource(Supervisor)),
            await RecordActionAsync(unknown),
            await _client.PatchAsJsonAsync($"/api/v1/incidents/{unknown}/closure", new CloseIncidentResource("Done", null)),
            await _client.GetAsync($"/api/v1/incidents/{unknown}/escalation"),
            await _client.GetAsync($"/api/v1/incidents/{unknown}/history")
        };

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task PatchAssignment_PendingIncident_ReturnsResponsibleAndDate()
    {
        // Arrange
        var incident = await OpenIncidentAsync();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/incidents/{incident.Id}/assignment",
            new AssignIncidentResource(Supervisor));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var assigned = await response.Content.ReadFromJsonAsync<IncidentResource>();
        Assert.Equal("Assigned", assigned!.Status);
        Assert.Equal(Supervisor, assigned.AssigneeId);
        Assert.NotNull(assigned.AssignedAt);
    }

    [Fact]
    public async Task PatchAssignment_TakenByAnotherSupervisor_ReturnsConflict()
    {
        // Arrange
        var incident = await OpenIncidentAsync();
        await _client.PatchAsJsonAsync($"/api/v1/incidents/{incident.Id}/assignment", new AssignIncidentResource(Supervisor));

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/incidents/{incident.Id}/assignment",
            new AssignIncidentResource(Guid.NewGuid()));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostAction_WithCompleteBody_ReturnsCreated()
    {
        // Arrange
        var incident = await OpenIncidentAsync();

        // Act
        var response = await RecordActionAsync(incident.Id);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var action = await response.Content.ReadFromJsonAsync<IncidentActionResource>();
        Assert.Equal("PreventivePause", action!.ActionType);
    }

    [Fact]
    public async Task PostAction_WithoutResult_ReturnsBadRequest()
    {
        // Arrange
        var incident = await OpenIncidentAsync();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/incidents/{incident.Id}/actions", new
        {
            actionType = "OperatorContact",
            description = "Radio call",
            performedBy = Supervisor,
            performedAt = DateTimeOffset.UtcNow
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAction_WithUnknownType_ReturnsUnprocessableEntity()
    {
        // Arrange
        var incident = await OpenIncidentAsync();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/incidents/{incident.Id}/actions",
            new RecordIncidentActionResource("Dance", "x", "y", Supervisor, DateTimeOffset.UtcNow));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PatchClosure_WithoutActions_ReturnsConflict()
    {
        // Arrange
        var incident = await OpenIncidentAsync();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/incidents/{incident.Id}/closure",
            new CloseIncidentResource("Done", null));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_ClosesIncidentAndExposesChronologicalHistory()
    {
        // Arrange
        var incident = await OpenIncidentAsync();
        await _client.PatchAsJsonAsync($"/api/v1/incidents/{incident.Id}/assignment", new AssignIncidentResource(Supervisor));
        await RecordActionAsync(incident.Id);

        // Act
        var closure = await _client.PatchAsJsonAsync($"/api/v1/incidents/{incident.Id}/closure",
            new CloseIncidentResource("Operator recovered", null));
        var actionAfterClosure = await RecordActionAsync(incident.Id);
        var history = await _client.GetFromJsonAsync<List<IncidentEventResource>>($"/api/v1/incidents/{incident.Id}/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, closure.StatusCode);
        var closed = await closure.Content.ReadFromJsonAsync<IncidentResource>();
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.AttentionTimeSeconds);
        Assert.Equal(HttpStatusCode.Conflict, actionAfterClosure.StatusCode);
        Assert.Equal(["Opened", "Assigned", "ActionRecorded", "Closed"], history!.Select(e => e.Type));
    }

    [Theory]
    [InlineData("Critical", "NotRequired")]
    [InlineData("Warning", "NotApplicable")]
    public async Task GetEscalation_NewIncident_ReportsWhetherEscalationApplies(string severity, string expected)
    {
        // Arrange
        var incident = await OpenIncidentAsync(severity);

        // Act
        var escalation = await _client.GetFromJsonAsync<IncidentEscalationResource>(
            $"/api/v1/incidents/{incident.Id}/escalation");

        // Assert
        Assert.Equal(expected, escalation!.Status);
        Assert.Equal(0, escalation.Level);
        Assert.Empty(escalation.NotifiedRoles);
    }

    [Fact]
    public async Task PostOverdueEscalations_WithFreshIncidents_EscalatesNothing()
    {
        // Arrange
        await OpenIncidentAsync();

        // Act
        var escalated = await (await _client.PostAsync("/api/v1/incidents/overdue-escalations", null))
            .Content.ReadFromJsonAsync<List<IncidentResource>>();

        // Assert
        Assert.Empty(escalated!);
    }
}
