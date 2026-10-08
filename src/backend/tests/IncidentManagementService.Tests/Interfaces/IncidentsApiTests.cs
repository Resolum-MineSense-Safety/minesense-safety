using System.Net;
using System.Net.Http.Json;
using IncidentManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentManagementService.Tests.Interfaces;

public class IncidentsApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/api/v1/incidents";
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<IncidentResource> OpenAsync()
    {
        var response = await _client.PostAsJsonAsync(Route, new OpenIncidentResource(Guid.NewGuid(), Guid.NewGuid()));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentResource>())!;
    }

    private static async Task<IncidentResource> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<IncidentResource>())!;

    [Fact]
    public async Task FullLifecycle_OpenAssignActClose_EndsClosedWithResolution()
    {
        // Arrange
        var incident = await OpenAsync();
        var supervisorId = Guid.NewGuid();

        // Act
        var assigned = await _client.PostAsJsonAsync($"{Route}/{incident.Id}/assignment", new AssignIncidentResource(supervisorId));
        var acted = await _client.PostAsJsonAsync($"{Route}/{incident.Id}/actions",
            new RegisterIncidentActionResource(supervisorId, "Se relevó al operador.", "Operador en descanso"));
        var closed = await _client.PostAsJsonAsync($"{Route}/{incident.Id}/closure",
            new CloseIncidentResource("El operador retomó la jornada tras el descanso."));

        // Assert
        Assert.Equal("Assigned", (await ReadAsync(assigned)).Status);
        Assert.Single((await ReadAsync(acted)).Actions);
        var final = await ReadAsync(closed);
        Assert.Equal("Closed", final.Status);
        Assert.NotNull(final.ClosedAt);
    }

    [Fact]
    public async Task Close_WithoutAssignment_ReturnsUnprocessableEntity()
    {
        // Arrange
        var incident = await OpenAsync();

        // Act
        var response = await _client.PostAsJsonAsync($"{Route}/{incident.Id}/closure", new CloseIncidentResource("Sin acciones"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Escalate_PendingIncident_ReturnsEscalated()
    {
        // Arrange
        var incident = await OpenAsync();

        // Act
        var response = await _client.PostAsync($"{Route}/{incident.Id}/escalation", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Escalated", (await ReadAsync(response)).Status);
    }

    [Fact]
    public async Task GetByStatus_AndById_ReturnMatchingIncidents()
    {
        // Arrange
        var incident = await OpenAsync();

        // Act
        var pending = await _client.GetFromJsonAsync<List<IncidentResource>>($"{Route}?status=Pending");
        var all = await _client.GetFromJsonAsync<List<IncidentResource>>(Route);
        var found = await _client.GetAsync($"{Route}/{incident.Id}");

        // Assert
        Assert.Contains(pending!, i => i.Id == incident.Id);
        Assert.Contains(all!, i => i.Id == incident.Id);
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
    }

    [Fact]
    public async Task GetByStatus_WithUnknownStatus_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.GetAsync($"{Route}?status=Lost");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Operations_OnUnknownIncident_ReturnNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var responses = new[]
        {
            await _client.GetAsync($"{Route}/{id}"),
            await _client.PostAsJsonAsync($"{Route}/{id}/assignment", new AssignIncidentResource(Guid.NewGuid())),
            await _client.PostAsJsonAsync($"{Route}/{id}/actions", new RegisterIncidentActionResource(Guid.NewGuid(), "a", "b")),
            await _client.PostAsync($"{Route}/{id}/escalation", null),
            await _client.PostAsJsonAsync($"{Route}/{id}/closure", new CloseIncidentResource("x")),
        };

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }
}
