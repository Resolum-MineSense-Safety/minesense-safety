using System.Net;
using System.Net.Http.Json;
using AlertService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AlertService.Tests.Interfaces;

public class AlertsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<AlertResource> IssueAlertAsync(Guid operatorId, string severity = "Critical")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/alerts",
            new IssueAlertResource(operatorId, Guid.NewGuid(), severity));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AlertResource>())!;
    }

    [Fact]
    public async Task PostAlert_WithValidResource_ReturnsCreatedAlert()
    {
        // Arrange
        var operatorId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/alerts",
            new IssueAlertResource(operatorId, Guid.NewGuid(), "Warning"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var alert = await response.Content.ReadFromJsonAsync<AlertResource>();
        Assert.Equal("Issued", alert!.Status);
        Assert.Equal("Warning", alert.Severity);
    }

    [Fact]
    public async Task PostAlert_WithUnknownSeverity_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/alerts",
            new IssueAlertResource(Guid.NewGuid(), Guid.NewGuid(), "Sleepy"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetAlerts_ByOperator_ReturnsOnlyThatOperator()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await IssueAlertAsync(operatorId);
        await IssueAlertAsync(Guid.NewGuid());

        // Act
        var alerts = await _client.GetFromJsonAsync<List<AlertResource>>($"/api/v1/alerts?operatorId={operatorId}");

        // Assert
        Assert.Single(alerts!);
        Assert.Equal(operatorId, alerts![0].OperatorId);
    }

    [Fact]
    public async Task GetAlertById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var alert = await IssueAlertAsync(Guid.NewGuid());

        // Act
        var found = await _client.GetAsync($"/api/v1/alerts/{alert.Id}");
        var missing = await _client.GetAsync($"/api/v1/alerts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Acknowledge_ThenEscalate_ReturnsOkThenUnprocessableEntity()
    {
        // Arrange
        var alert = await IssueAlertAsync(Guid.NewGuid());

        // Act
        var acknowledged = await _client.PostAsync($"/api/v1/alerts/{alert.Id}/acknowledgements", null);
        var escalated = await _client.PostAsync($"/api/v1/alerts/{alert.Id}/escalations", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, acknowledged.StatusCode);
        Assert.Equal("Acknowledged", (await acknowledged.Content.ReadFromJsonAsync<AlertResource>())!.Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, escalated.StatusCode);
    }

    [Fact]
    public async Task Escalate_IssuedAlert_ReturnsEscalatedAlert()
    {
        // Arrange
        var alert = await IssueAlertAsync(Guid.NewGuid());

        // Act
        var response = await _client.PostAsync($"/api/v1/alerts/{alert.Id}/escalations", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Escalated", (await response.Content.ReadFromJsonAsync<AlertResource>())!.Status);
    }

    [Fact]
    public async Task GetByStatus_SupervisorQueue_ReturnsEscalatedAlerts()
    {
        // Arrange
        var alert = await IssueAlertAsync(Guid.NewGuid());
        await _client.PostAsync($"/api/v1/alerts/{alert.Id}/escalations", null);

        // Act
        var escalated = await _client.GetFromJsonAsync<List<AlertResource>>("/api/v1/alerts/status/escalated");

        // Assert
        Assert.Contains(escalated!, item => item.Id == alert.Id);
        Assert.All(escalated!, item => Assert.Equal("Escalated", item.Status));
    }

    [Fact]
    public async Task GetByStatus_WithUnknownStatus_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/alerts/status/sleeping");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task OverdueEscalations_WithAlertsInTime_DoesNotEscalateThem()
    {
        // Arrange
        var alert = await IssueAlertAsync(Guid.NewGuid(), "Warning");

        // Act
        var response = await _client.PostAsync("/api/v1/alerts/overdue-escalations", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var escalated = await response.Content.ReadFromJsonAsync<List<AlertResource>>();
        Assert.DoesNotContain(escalated!, item => item.Id == alert.Id);
    }

    [Fact]
    public async Task Actions_OnUnknownAlert_ReturnNotFound()
    {
        // Act
        var acknowledge = await _client.PostAsync($"/api/v1/alerts/{Guid.NewGuid()}/acknowledgements", null);
        var escalate = await _client.PostAsync($"/api/v1/alerts/{Guid.NewGuid()}/escalations", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, acknowledge.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, escalate.StatusCode);
    }
}
