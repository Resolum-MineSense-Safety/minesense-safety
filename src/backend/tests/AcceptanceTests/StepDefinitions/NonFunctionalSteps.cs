using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// Non-functional user stories NFR01 (performance), NFR02 (security) and NFR03 (availability),
/// section 4.8. NFR04 (accessibility) runs in the browser: tests/e2e/features.
/// </summary>
[Binding]
public class NonFunctionalSteps(MineSenseApi api, SecuredApi secured, IReqnrollOutputHelper output)
{
    private readonly List<double> _latenciesMs = [];
    private readonly List<HttpStatusCode> _statuses = [];
    private HttpResponseMessage? _response;
    private string? _requestLine;
    private double _elapsedMs;
    private string? _metrics;

    private HttpClient Open(string service) => service switch
    {
        "Fatigue Detection" => api.Fatigue,
        "Alerts" => api.Alerts,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Unknown service")
    };

    private Task<HttpResponseMessage> CriticalPathRequest(string endpoint) => endpoint switch
    {
        "evaluación de fatiga" => api.AssessAsync(api.Signals(0.45, 8, 18)),
        "emisión de alerta" => api.Alerts.PostAsJsonAsync("/api/v1/alerts",
            new { operatorId = api.OperatorId, assessmentId = Guid.NewGuid(), severity = "Critical" }),
        "consulta de alertas" => api.Alerts.GetAsync($"/api/v1/alerts?operatorId={api.OperatorId}"),
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unknown endpoint")
    };

    // ---------- NFR01 ----------
    [Given("el servicio ya atendió peticiones de calentamiento")]
    public async Task GivenWarmedUp()
    {
        // JIT and first-request costs are not part of the steady-state SLO.
        for (var i = 0; i < 20; i++)
        {
            await CriticalPathRequest("evaluación de fatiga");
            await CriticalPathRequest("emisión de alerta");
            await CriticalPathRequest("consulta de alertas");
        }
    }

    [When("se envían {int} peticiones a {string}")]
    public async Task WhenRequestsAreSent(int count, string endpoint)
    {
        for (var i = 0; i < count; i++)
        {
            var watch = Stopwatch.StartNew();
            var response = await CriticalPathRequest(endpoint);
            watch.Stop();
            _latenciesMs.Add(watch.Elapsed.TotalMilliseconds);
            _statuses.Add(response.StatusCode);
        }
    }

    [Then("el percentil 95 de la latencia es menor a {int} ms")]
    public void ThenP95IsBelow(int limitMs)
    {
        var sorted = _latenciesMs.Order().ToList();
        var p95 = sorted[(int)Math.Ceiling(sorted.Count * 0.95) - 1];
        output.WriteLine($"P95 = {p95:F1} ms (max {sorted[^1]:F1} ms, {sorted.Count} requests)");
        Assert.True(p95 < limitMs, $"P95 {p95:F1} ms >= {limitMs} ms");
    }

    [Then("ninguna petición falla")]
    public void ThenNoRequestFails() => Assert.All(_statuses, status => Assert.True((int)status < 400, $"HTTP {(int)status}"));

    // ---------- NFR02 ----------
    [Given("los servicios exigen el token de Identity & Access")]
    public void GivenTokensAreRequired()
    {
        // SecuredApi starts both services with Authentication:IdentityUrl set.
    }

    [When("se llama a {string} de {string} con credencial {string}")]
    public async Task WhenCalledWithCredential(string requestLine, string service, string credential)
    {
        var (method, path) = (requestLine.Split(' ')[0], requestLine.Split(' ')[1]);
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST") request.Content = JsonContent.Create(new { });
        if (credential == "token falso")
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                "eyJhbGciOiJSUzI1NiIsImtpZCI6ImZha2UifQ.eyJzdWIiOiJhdGFjYW50ZSJ9.ZmlybWEtZmFsc2E");
        _requestLine = requestLine;
        _response = await secured.Client(service).SendAsync(request);
    }

    [Then("la respuesta es {int}")]
    public void ThenStatusIs(int status) => Assert.Equal(status, (int)_response!.StatusCode);

    [Then("el intento queda registrado con método, ruta, fecha y motivo")]
    public void ThenAttemptIsLogged()
    {
        var (method, path) = (_requestLine!.Split(' ')[0], _requestLine.Split(' ')[1]);
        var entry = secured.Logs.Messages.FirstOrDefault(message =>
            message.StartsWith("Access rejected 401") && message.Contains($"{method} {path}"));
        Assert.NotNull(entry);
        // "... at 2026-11-04T10:15:30.1234567+00:00 for anonymous: <reason>"
        Assert.Matches(@" at \d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", entry);
        Assert.Matches(@": \S+", entry![(entry.LastIndexOf(" for ", StringComparison.Ordinal))..]);
    }

    [Then("estas rutas responden sin token")]
    public async Task ThenRoutesArePublic(DataTable table)
    {
        foreach (var row in table.Rows)
        {
            var response = await secured.Client(row["servicio"]).GetAsync(row["ruta"]);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{row["servicio"]} {row["ruta"]} -> {(int)response.StatusCode}");
        }
    }

    // ---------- NFR03 ----------
    [When("Kubernetes consulta {string} en {string}")]
    public async Task WhenProbeIsCalled(string path, string service)
    {
        var watch = Stopwatch.StartNew();
        _response = await Open(service).GetAsync(path);
        _elapsedMs = watch.Elapsed.TotalMilliseconds;
    }

    [Then("responde 200 en menos de {int} ms")]
    public void ThenAnswersInTime(int limitMs)
    {
        Assert.Equal(HttpStatusCode.OK, _response!.StatusCode);
        Assert.True(_elapsedMs < limitMs, $"{_elapsedMs:F0} ms");
    }

    [Then("el contenido es de tipo {string}")]
    public void ThenContentTypeIs(string type) =>
        Assert.StartsWith(type, _response!.Content.Headers.ContentType?.MediaType ?? "");

    [Given("el servicio {string} atendió peticiones con distintos resultados")]
    public async Task GivenServiceHandledRequests(string service, DataTable table)
    {
        foreach (var row in table.Rows)
            for (var i = 0; i < int.Parse(row["veces"]); i++)
                await Open(service).GetAsync(row["ruta"]);
    }

    [When("Prometheus lee {string} de {string}")]
    public async Task WhenPrometheusScrapes(string path, string service) =>
        _metrics = await Open(service).GetStringAsync(path);

    [Then("la serie {string} existe para los códigos")]
    public void ThenSeriesExistsForCodes(string series, DataTable table)
    {
        foreach (var row in table.Rows)
            Assert.Contains(_metrics!.Split('\n'), line =>
                line.StartsWith(series) && line.Contains($"code=\"{row["codigo"]}\""));
    }
}
