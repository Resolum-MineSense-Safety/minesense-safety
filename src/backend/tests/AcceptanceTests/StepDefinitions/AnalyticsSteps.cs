using System.Globalization;
using System.Net;
using System.Text.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US24 and US25 (EP11, Analytics Service). The observations of each scenario belong to a mine site
/// named with the scenario suffix, and every query filters by it. AnalyticsPolicy asks for at least
/// 10 observations before a trend or an evaluation is representative.
/// </summary>
[Binding]
public class AnalyticsSteps(ApiContext api)
{
    private string MineSite => $"UM-{api.Suffix}";
    private object? _lastEvent;
    private Guid _measureId;
    private DateTimeOffset _measureAppliedAt;

    [Given("se registraron alertas de fatiga en una unidad minera:")]
    public async Task GivenFatigueAlertsWereRecorded(DataTable alerts)
    {
        var occurredAt = DateTimeOffset.UtcNow.AddDays(-1);
        foreach (var row in alerts.Rows)
            for (var index = 0; index < int.Parse(row["cantidad"], CultureInfo.InvariantCulture); index++)
            {
                occurredAt = occurredAt.AddMinutes(-7);
                await RecordAsync(row["severidad"], occurredAt, row["turno"], row["ubicacion"], row["flota"]);
            }
    }

    [When("la última alerta se registra de nuevo")]
    public async Task WhenTheLastAlertIsRecordedAgain() =>
        await api.ExpectAsync(HttpStatusCode.Created, Service.Analytics, HttpMethod.Post, "/api/v1/analytics/fatigue-events", _lastEvent!);

    [Given("una medida preventiva {string} aplicada hace {int} días en una unidad minera")]
    public async Task GivenAPreventiveMeasure(string type, int days)
    {
        _measureAppliedAt = DateTimeOffset.UtcNow.AddDays(-days);
        var measure = await api.ExpectAsync(HttpStatusCode.Created, Service.Analytics, HttpMethod.Post,
            "/api/v1/analytics/preventive-measures", new
            {
                name = $"Pausa activa cada 2 horas ({api.Suffix})",
                measureType = type,
                description = "Pausas obligatorias en el turno de noche",
                appliedAt = _measureAppliedAt,
                registeredBy = Guid.NewGuid(),
                mineSite = MineSite
            });
        _measureId = measure.GetProperty("id").GetGuid();
    }

    [Given("se registraron alertas en esa unidad minera alrededor de la medida:")]
    public async Task GivenAlertsAroundTheMeasure(DataTable periods)
    {
        foreach (var row in periods.Rows)
        {
            var direction = row["periodo"] == "antes" ? -1 : 1;
            for (var index = 1; index <= int.Parse(row["cantidad"], CultureInfo.InvariantCulture); index++)
                await RecordAsync("Warning", _measureAppliedAt.AddHours(direction * index * 12), "Night", "Tajo Norte", "Acarreo");
        }
    }

    [When("el analista consulta la tendencia agrupada por {string}")]
    public async Task WhenTheAnalystQueriesTheTrend(string dimension) =>
        await api.GetAsync(Service.Analytics,
            $"/api/v1/analytics/fatigue-trends?groupBy={dimension}&mineSite={Uri.EscapeDataString(MineSite)}");

    [When("el analista evalúa la medida con una ventana de {int} días")]
    public async Task WhenTheAnalystEvaluatesTheMeasure(int windowDays) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.Analytics, HttpMethod.Get,
            $"/api/v1/analytics/preventive-measures/{_measureId}/evaluation?windowDays={windowDays}");

    [Then("la tendencia suma {int} alertas y es representativa")]
    public void ThenTheTrendIsRepresentative(int total)
    {
        ThenTheTrendAdds(total);
        Assert.True(api.JsonBody.GetProperty("isRepresentative").GetBoolean());
    }

    [Then("la tendencia suma {int} alertas")]
    public void ThenTheTrendAdds(int total)
    {
        api.AssertStatus(HttpStatusCode.OK);
        Assert.Equal(total, api.JsonBody.GetProperty("totalAlerts").GetInt32());
    }

    [Then("el grupo {string} tiene {int} alertas, {int} críticas y una proporción de {double}")]
    public void ThenTheGroupHas(string key, int alerts, int critical, double proportion)
    {
        var group = api.JsonBody.GetProperty("groups").EnumerateArray()
            .Single(item => item.GetProperty("key").GetString() == key);
        Assert.Equal(alerts, group.GetProperty("alerts").GetInt32());
        Assert.Equal(critical, group.GetProperty("criticalAlerts").GetInt32());
        Assert.Equal(proportion, group.GetProperty("proportion").GetDouble(), precision: 4);
    }

    [Then("la tendencia no es representativa y advierte que se necesitan al menos {int} observaciones")]
    public void ThenTheTrendIsNotRepresentative(int minimum)
    {
        api.AssertStatus(HttpStatusCode.OK);
        Assert.False(api.JsonBody.GetProperty("isRepresentative").GetBoolean());
        Assert.Contains($"at least {minimum}", api.JsonBody.GetProperty("warning").GetString());
    }

    [Then("la evaluación es comparable con {int} alertas antes y {int} después")]
    public void ThenTheEvaluationIsComparable(int before, int after)
    {
        var evaluation = api.JsonBody;
        Assert.True(evaluation.GetProperty("isComparable").GetBoolean(), api.Body);
        Assert.Equal(before, evaluation.GetProperty("before").GetProperty("alerts").GetInt32());
        Assert.Equal(after, evaluation.GetProperty("after").GetProperty("alerts").GetInt32());
    }

    [Then("la variación de alertas por día es de {int} %")]
    public void ThenTheVariationIs(int percent) =>
        Assert.Equal(percent, api.JsonBody.GetProperty("alertsPerDayVariationPercent").GetDouble(), precision: 2);

    [Then("la conclusión aclara que es una variación observada")]
    public void ThenTheConclusionIsCautious() =>
        Assert.Contains("does not prove that accidents were avoided", api.JsonBody.GetProperty("conclusion").GetString());

    [Then("la evaluación no es comparable porque {string}")]
    public void ThenTheEvaluationIsNotComparable(string reason)
    {
        Assert.False(api.JsonBody.GetProperty("isComparable").GetBoolean());
        Assert.Equal(JsonValueKind.Null, api.JsonBody.GetProperty("alertsPerDayVariationPercent").ValueKind);
        Assert.Contains(reason, api.JsonBody.GetProperty("conclusion").GetString());
    }

    private async Task RecordAsync(string severity, DateTimeOffset occurredAt, string shift, string location, string fleet)
    {
        _lastEvent = new
        {
            operatorId = Guid.NewGuid(),
            alertId = Guid.NewGuid(),
            severity,
            occurredAt,
            shift,
            mineSite = MineSite,
            location,
            fleet
        };
        await api.ExpectAsync(HttpStatusCode.Created, Service.Analytics, HttpMethod.Post, "/api/v1/analytics/fatigue-events", _lastEvent);
    }
}
