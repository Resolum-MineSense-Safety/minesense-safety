using System.Globalization;
using System.Net;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US14 (Edge Synchronization Service). The batchId is the idempotency key. The events use a type
/// that no consumer handles, so a deployed environment stores them without publishing anything.
/// </summary>
[Binding]
public class EdgeSynchronizationSteps(ApiContext api)
{
    private readonly Guid _batchId = Guid.NewGuid();
    private readonly Guid _edgeNodeId = Guid.NewGuid();
    private readonly Guid _deviceId = Guid.NewGuid();
    private List<object> _events = [];

    [Given("la cabina acumuló sin conexión los eventos:")]
    public void GivenTheCabinCapturedEvents(DataTable events) =>
        _events = events.Rows.Select(row => Event(row["tipo"],
            DateTimeOffset.UtcNow.AddMinutes(-int.Parse(row["minutos_atras"], CultureInfo.InvariantCulture)))).ToList();

    [Given("el gateway de la cabina envía el lote al recuperar la conexión")]
    [When("el gateway de la cabina envía el lote al recuperar la conexión")]
    public async Task WhenTheGatewaySendsTheBatch() => await SubmitAsync(_events);

    [When("el gateway reenvía el mismo lote con otro contenido")]
    public async Task WhenTheGatewayResendsTheBatch() =>
        await SubmitAsync([Event("DeviceHeartbeat", DateTimeOffset.UtcNow), Event("DeviceHeartbeat", DateTimeOffset.UtcNow)]);

    [When("el gateway de la cabina envía un lote sin eventos")]
    public Task WhenTheGatewaySendsAnEmptyBatch() => SubmitAsync([]);

    [When("el gateway de la cabina envía un lote con eventos repetidos")]
    public Task WhenTheGatewaySendsRepeatedEvents() => SubmitAsync([_events[0], _events[0]]);

    [When("el gateway de la cabina envía un lote con una secuencia negativa")]
    public Task WhenTheGatewaySendsANegativeSequence() => SubmitAsync(_events, sequenceNumber: -1);

    [When("el gateway de la cabina envía un lote con un evento sin tipo")]
    public Task WhenTheGatewaySendsAnEventWithoutType() =>
        SubmitAsync([_events[0], Event(" ", DateTimeOffset.UtcNow)]);

    [Then("la nube acepta el lote con sus {int} eventos")]
    public async Task ThenTheCloudAcceptsTheBatch(int count)
    {
        api.AssertStatus(HttpStatusCode.Accepted);
        await ThenTheCloudKeepsTheOriginalBatch(count);
    }

    [Then("la nube conserva el lote original con {int} eventos")]
    public async Task ThenTheCloudKeepsTheOriginalBatch(int count)
    {
        var batch = await api.ExpectAsync(HttpStatusCode.OK, Service.EdgeSynchronization, HttpMethod.Get,
            $"/api/v1/synchronization-batches/{_batchId}");
        Assert.Equal("Accepted", batch.GetProperty("status").GetString());
        Assert.Equal(_edgeNodeId, batch.GetProperty("edgeNodeId").GetGuid());
        Assert.Equal(count, batch.GetProperty("events").GetArrayLength());
    }

    private static object Event(string type, DateTimeOffset occurredAt) => new
    {
        eventId = Guid.NewGuid(),
        occurredAt,
        eventType = type,
        payload = new { batteryLevel = 64, source = "acceptance-tests" }
    };

    private async Task SubmitAsync(IReadOnlyCollection<object> events, long sequenceNumber = 7) =>
        await api.PostAsync(Service.EdgeSynchronization, "/api/v1/synchronization-batches", new
        {
            batchId = _batchId,
            edgeNodeId = _edgeNodeId,
            deviceId = _deviceId,
            sequenceNumber,
            capturedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            events
        });
}
