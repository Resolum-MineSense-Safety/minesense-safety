using System.Net;
using System.Text.RegularExpressions;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>US20 and US21 (EP09, Privacy &amp; Audit Service). Each scenario uses a new operator.</summary>
[Binding]
public partial class PrivacyAuditSteps(ApiContext api)
{
    private readonly Guid _operatorId = Guid.NewGuid();

    [When("el operador consulta las finalidades del tratamiento de sus datos")]
    public async Task WhenTheOperatorQueriesThePurposes() =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.PrivacyAudit, HttpMethod.Get, "/api/v1/processing-purposes");

    [Then("la política vigente es {string} con las finalidades:")]
    public void ThenThePolicyHasThePurposes(string version, DataTable expected)
    {
        var policy = api.JsonBody;
        Assert.Equal(version, policy.GetProperty("policyVersion").GetString());
        var purposes = policy.GetProperty("purposes").EnumerateArray()
            .Select(purpose => $"{purpose.GetProperty("code").GetString()}:{purpose.GetProperty("isMandatory").GetBoolean()}")
            .ToList();
        Assert.Equal(expected.Rows.Select(row => $"{row["finalidad"]}:{bool.Parse(row["obligatoria"])}").ToList(), purposes);
    }

    [Given("el operador registró la decisión {string} para las finalidades {string}")]
    public async Task GivenTheOperatorDecided(string status, string purposes)
    {
        await WhenTheOperatorDecides(status, purposes);
        api.AssertStatus(HttpStatusCode.Created);
    }

    [When("el operador registra la decisión {string} para las finalidades {string}")]
    public async Task WhenTheOperatorDecides(string status, string purposes) =>
        await api.PostAsync(Service.PrivacyAudit, "/api/v1/consents", new
        {
            operatorId = _operatorId,
            policyVersion = "v1.0",
            purposes = ApiContext.List(purposes),
            status
        });

    [Then("su consentimiento vigente es {string}")]
    public async Task ThenTheCurrentConsentIs(string status)
    {
        var consent = await api.ExpectAsync(HttpStatusCode.OK, Service.PrivacyAudit, HttpMethod.Get,
            $"/api/v1/consents/operator/{_operatorId}/current");
        Assert.Equal(status, consent.GetProperty("status").GetString());
    }

    [Then("el consentimiento bloquea el monitoreo: {word}")]
    public void ThenTheConsentBlocksMonitoring(string blocks) =>
        Assert.Equal(bool.Parse(blocks), api.JsonBody.GetProperty("blocksMonitoring").GetBoolean());

    [Then("el historial de consentimientos del operador tiene {int} decisiones")]
    public async Task ThenTheHistoryHas(int count)
    {
        var history = await api.ExpectAsync(HttpStatusCode.OK, Service.PrivacyAudit, HttpMethod.Get,
            $"/api/v1/consents/operator/{_operatorId}/history");
        Assert.Equal(count, history.GetArrayLength());
    }

    [Given("se registraron los accesos a los datos biométricos de un operador:")]
    public async Task GivenAccessesWereRecorded(DataTable accesses)
    {
        foreach (var row in accesses.Rows)
            await api.ExpectAsync(HttpStatusCode.Created, Service.PrivacyAudit, HttpMethod.Post, "/api/v1/access-log/entries", new
            {
                userId = Guid.NewGuid(),
                resourceType = "FatigueAssessment",
                resourceId = _operatorId,
                fields = ApiContext.List(row["campos"]),
                purpose = row["finalidad"],
                decision = row["decision"]
            });
    }

    [When("el auditor consulta los accesos del operador con la decisión {string}")]
    public async Task WhenTheAuditorQueriesByDecision(string decision) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.PrivacyAudit, HttpMethod.Get,
            $"/api/v1/access-log?operatorId={_operatorId}&decision={decision}");

    [When("el auditor consulta todos los accesos del operador")]
    public Task WhenTheAuditorQueriesAll() => WhenTheAuditorQueriesByDecision(string.Empty);

    [When("el auditor consulta los accesos del operador con {int} resultados por página")]
    public async Task WhenTheAuditorQueriesWithPageSize(int pageSize) =>
        await api.GetAsync(Service.PrivacyAudit, $"/api/v1/access-log?operatorId={_operatorId}&pageSize={pageSize}");

    [Then("obtiene {int} accesos sellados con su huella SHA-256")]
    public void ThenGetsSealedEntries(int count)
    {
        var entries = api.JsonBody.EnumerateArray().ToList();
        Assert.Equal(count, entries.Count);
        Assert.All(entries, entry =>
        {
            Assert.Equal(_operatorId, entry.GetProperty("resourceId").GetGuid());
            Assert.Matches(Sha256(), entry.GetProperty("hash").GetString()!);
        });
    }

    [Then("cada acceso tiene una huella distinta")]
    public void ThenEveryHashIsDifferent()
    {
        var hashes = api.JsonBody.EnumerateArray().Select(entry => entry.GetProperty("hash").GetString()).ToList();
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }

    [Then("los accesos aparecen del más reciente al más antiguo")]
    public void ThenTheEntriesAreNewestFirst()
    {
        var times = api.JsonBody.EnumerateArray().Select(entry => entry.GetProperty("accessedAt").GetDateTimeOffset()).ToList();
        Assert.Equal(times.OrderByDescending(time => time).ToList(), times);
    }

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex Sha256();
}
