using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// RNF01 performance, RNF02 security and RNF03 availability. Latency is measured on the client with
/// a <see cref="Stopwatch"/> around each HTTP request, after a few warm-up requests that are not counted.
/// </summary>
[Binding]
public partial class NonFunctionalSteps(ApiContext api)
{
    private const int WarmUpRequests = 5;

    /// <summary>A token with a valid shape and an invented signature.</summary>
    private const string ForgedToken =
        "eyJhbGciOiJSUzI1NiIsImtpZCI6ImZvcmdlZCJ9.eyJzdWIiOiI3ZDFmOGE1Mi0yYTBjLTRjNDMtOWE4ZS0xZjJiM2M0ZDVlNmYiLCJyb2xlIjoiQWRtaW5pc3RyYXRvciJ9.c2lnbmF0dXJh";

    private readonly ConcurrentBag<double> _latencies = [];
    private readonly ConcurrentBag<string> _failures = [];
    private readonly string _fleet = $"Flota-perf-{Guid.NewGuid():N}"[..20];
    private readonly Guid _operatorId = Guid.NewGuid();
    private readonly string _mineSite = $"UM-perf-{Guid.NewGuid():N}"[..16];
    private string _username = string.Empty;
    private TimeSpan _elapsed;
    private (HttpStatusCode Status, string Body)? _firstSignIn;

    private const string Password = "Turno-Noche-2026";

    // ---------- RNF01: performance ----------

    [Given("la operación {string} tiene datos de prueba")]
    public async Task GivenTheOperationHasTestData(string operation)
    {
        switch (operation)
        {
            case "estado de la flota":
                for (var index = 0; index < 20; index++)
                    await api.ExpectAsync(HttpStatusCode.Created, Service.FleetMonitoring, HttpMethod.Post, "/api/v1/fleet-operators", new
                    {
                        operatorId = Guid.NewGuid(), fullName = $"Operador {index:00}", vehicleCode = $"CA-{index:000}",
                        fleet = _fleet, shift = index % 2 == 0 ? "Day" : "Night", location = "Tajo Norte"
                    });
                break;
            case "bandeja de incidentes":
                for (var index = 0; index < 20; index++)
                    await api.ExpectAsync(HttpStatusCode.Created, Service.IncidentManagement, HttpMethod.Post, "/api/v1/incidents",
                        new { operatorId = _operatorId, alertId = Guid.NewGuid(), severity = index % 3 == 0 ? "Critical" : "Warning" });
                break;
            case "tendencia de fatiga":
                for (var index = 0; index < 20; index++)
                    await api.ExpectAsync(HttpStatusCode.Created, Service.Analytics, HttpMethod.Post, "/api/v1/analytics/fatigue-events", new
                    {
                        operatorId = Guid.NewGuid(), alertId = Guid.NewGuid(), severity = "Warning",
                        occurredAt = DateTimeOffset.UtcNow.AddHours(-index - 1), shift = index % 2 == 0 ? "Day" : "Night",
                        mineSite = _mineSite, location = "Tajo Norte", fleet = "Acarreo"
                    });
                break;
            case "inicio de sesión":
                _username = $"perf-{Guid.NewGuid():N}"[..20];
                await SignUpAsync("Usuario de carga");
                break;
        }

        for (var index = 0; index < WarmUpRequests; index++)
            await ExecuteAsync(operation, measure: false);
    }

    [When("se ejecutan {int} solicitudes secuenciales de {string}")]
    public async Task WhenSequentialRequestsRun(int requests, string operation)
    {
        for (var index = 0; index < requests; index++)
            await ExecuteAsync(operation, measure: true);
    }

    [When("se ejecutan {int} solicitudes de {string} con {int} clientes en paralelo")]
    public async Task WhenConcurrentRequestsRun(int requests, string operation, int clients)
    {
        var remaining = requests;
        var workers = Enumerable.Range(0, clients).Select(async _ =>
        {
            while (Interlocked.Decrement(ref remaining) >= 0)
                await ExecuteAsync(operation, measure: true);
        });
        await Task.WhenAll(workers);
    }

    [Then("todas las solicitudes responden con éxito")]
    public void ThenAllRequestsSucceed() => Assert.True(_failures.IsEmpty, string.Join(Environment.NewLine, _failures.Take(5)));

    [Then("el percentil 95 de la latencia es menor a {int} ms en memoria y a {int} ms en el entorno desplegado")]
    public void ThenTheP95IsBelow(int inMemoryMs, int deployedMs)
    {
        var threshold = MineSenseServices.IsRemote ? deployedMs : inMemoryMs;
        var p95 = Percentile(_latencies.ToArray(), 0.95);
        Console.WriteLine($"p95 = {p95:0.0} ms over {_latencies.Count} requests (threshold {threshold} ms)");
        Assert.True(p95 < threshold, $"p95 {p95:0.0} ms is not below {threshold} ms ({_latencies.Count} requests).");
    }

    private async Task ExecuteAsync(string operation, bool measure)
    {
        var (service, request) = Operation(operation);
        var client = api.Services.Client(service);
        var stopwatch = Stopwatch.StartNew();
        using var response = await client.SendAsync(request);
        stopwatch.Stop();
        request.Dispose();
        if (!measure) return;

        _latencies.Add(stopwatch.Elapsed.TotalMilliseconds);
        if (!response.IsSuccessStatusCode)
            _failures.Add($"{operation}: HTTP {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }

    private (Service, HttpRequestMessage) Operation(string operation) => operation switch
    {
        "evaluación de fatiga" => (Service.FatigueDetection, Post("/api/v1/fatigue-assessments", new
        {
            operatorId = Guid.NewGuid(), monitoringSessionId = Guid.NewGuid(),
            perclos = 0.20, blinkRatePerMinute = 18.0, heartRateVariabilityMs = 45.0
        })),
        "emisión de alerta" => (Service.Alerts, Post("/api/v1/alerts",
            new { operatorId = Guid.NewGuid(), assessmentId = Guid.NewGuid(), severity = "Warning" })),
        "estado de la flota" => (Service.FleetMonitoring,
            new HttpRequestMessage(HttpMethod.Get, $"/api/v1/fleet/status?fleet={_fleet}")),
        "bandeja de incidentes" => (Service.IncidentManagement,
            new HttpRequestMessage(HttpMethod.Get, $"/api/v1/incidents?status=Pending&operatorId={_operatorId}")),
        "tendencia de fatiga" => (Service.Analytics,
            new HttpRequestMessage(HttpMethod.Get, $"/api/v1/analytics/fatigue-trends?groupBy=Shift&mineSite={_mineSite}")),
        "inicio de sesión" => (Service.IdentityAccess, Post("/api/v1/authentication/sign-in",
            new { username = _username, password = Password })),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation.")
    };

    private async Task SignUpAsync(string fullName)
    {
        await api.SendAsync(Service.IdentityAccess, HttpMethod.Post, "/api/v1/authentication/sign-up",
            new { username = _username, fullName, password = Password, role = "Operator" },
            api.Services.Anonymous(Service.IdentityAccess));
        api.AssertStatus(HttpStatusCode.Created);
    }

    private static HttpRequestMessage Post(string path, object body) =>
        new(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: ApiContext.Json) };

    private static double Percentile(double[] values, double percentile)
    {
        Assert.NotEmpty(values);
        Array.Sort(values);
        var rank = (int)Math.Ceiling(percentile * values.Length) - 1;
        return values[Math.Clamp(rank, 0, values.Length - 1)];
    }

    // ---------- RNF02: security ----------

    [When("se consulta la ruta {string} de {string} sin token de acceso")]
    public async Task WhenTheRouteIsCalledWithoutToken(string path, string service) =>
        await api.SendAsync(Parse(service), HttpMethod.Get, path, client: api.Services.SecuredAnonymous(Parse(service)));

    [When("se consulta la ruta {string} de {string} con un token falsificado")]
    public async Task WhenTheRouteIsCalledWithAForgedToken(string path, string service) =>
        await api.SendAsync(Parse(service), HttpMethod.Get, path,
            client: api.Services.SecuredWithBearer(Parse(service), ForgedToken));

    [Then("la respuesta publica una clave RSA para verificar las firmas {string}")]
    public void ThenTheJwksPublishesAnRsaKey(string algorithm)
    {
        var key = Assert.Single(api.JsonBody.GetProperty("keys").EnumerateArray(),
            item => item.GetProperty("kty").GetString() == "RSA");
        Assert.Equal(algorithm, key.GetProperty("alg").GetString());
        Assert.False(key.TryGetProperty("d", out _), "The JWKS must not publish the private exponent.");
    }

    [When("se inicia sesión con un usuario inexistente y luego con una contraseña incorrecta de un usuario real")]
    public async Task WhenSigningInWithUnknownUserAndWrongPassword()
    {
        _username = $"real-{Guid.NewGuid():N}"[..20];
        await SignUpAsync("Usuario real");

        var anonymous = api.Services.Anonymous(Service.IdentityAccess);
        await api.SendAsync(Service.IdentityAccess, HttpMethod.Post, "/api/v1/authentication/sign-in",
            new { username = $"nadie-{Guid.NewGuid():N}"[..20], password = Password }, anonymous);
        _firstSignIn = (api.Status, api.Body);
        await api.SendAsync(Service.IdentityAccess, HttpMethod.Post, "/api/v1/authentication/sign-in",
            new { username = _username, password = Password + "x" }, anonymous);
    }

    [Then("ambas respuestas son 401 con el mismo mensaje")]
    public void ThenBothAnswersAreEqual()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, _firstSignIn!.Value.Status);
        api.AssertStatus(HttpStatusCode.Unauthorized);
        Assert.Equal(_firstSignIn.Value.Body, api.Body);
    }

    [When("se envía un cuerpo JSON malformado a {string} de {string}")]
    public async Task WhenAMalformedBodyIsSent(string path, string service) =>
        await api.PostAsync(Parse(service), path, ApiContext.RawJson("{\"operatorId\": \"7d1f8a52\", \"severity\": "));

    [Then("la respuesta no expone trazas de la pila ni nombres de excepciones")]
    public void ThenTheErrorHidesInternals()
    {
        Assert.DoesNotMatch(StackFrame(), api.Body);
        Assert.DoesNotContain("Exception", api.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", api.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(":line ", api.Body, StringComparison.Ordinal);
    }

    /// <summary>A .NET stack frame such as "at Namespace.Type.Method(".</summary>
    [GeneratedRegex(@"\bat [A-Za-z_][\w.`<>]+\(")]
    private static partial Regex StackFrame();

    // ---------- RNF03: availability ----------

    [When("se consultan las sondas de salud de {string}")]
    public void WhenTheHealthProbesAreQueried(string service) => _probeService = Parse(service);

    private Service _probeService;

    [Then("la sonda {string} responde {string} sin token de acceso")]
    public async Task ThenTheProbeAnswers(string path, string status)
    {
        // The host has authentication on: the probes must stay public for Kubernetes and the ALB.
        await api.SendAsync(_probeService, HttpMethod.Get, path, client: api.Services.SecuredAnonymous(_probeService));
        api.AssertStatus(HttpStatusCode.OK);
        Assert.Equal(status, api.Body);
    }

    [When("se consulta la ruta {string} de {string} a través del punto de entrada")]
    public async Task WhenTheRouteIsCalledThroughTheEntryPoint(string path, string service)
    {
        var stopwatch = Stopwatch.StartNew();
        await api.GetAsync(Parse(service), path);
        _elapsed = stopwatch.Elapsed;
    }

    [Then("el microservicio responde sin error de servidor en menos de {int} ms")]
    public void ThenTheServiceAnswers(int milliseconds)
    {
        Assert.True((int)api.Status < 500, $"HTTP {(int)api.Status}: {api.Body}");
        // An unmatched path would fall through the gateway to the dashboard (HTML).
        Assert.NotEqual("text/html", api.Response!.Content.Headers.ContentType?.MediaType);
        Assert.True(_elapsed.TotalMilliseconds < milliseconds, $"{_elapsed.TotalMilliseconds:0} ms");
    }

    private static Service Parse(string service) => Enum.Parse<Service>(service);
}
