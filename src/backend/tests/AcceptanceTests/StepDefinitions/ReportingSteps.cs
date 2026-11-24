using System.Net;
using System.Security.Cryptography;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>US22 (EP10, Reporting Service). Each scenario defines its own indicator code.</summary>
[Binding]
public class ReportingSteps(ApiContext api)
{
    private string _definitionCode = string.Empty;
    private string[] _dataSources = [];
    private Guid _reportId;

    [Given("un indicador de reporte versión {string} con las fuentes de datos:")]
    public async Task GivenAReportIndicator(string version, DataTable sources)
    {
        _definitionCode = $"FATIGA-{api.Suffix}";
        _dataSources = sources.Rows.Select(row => row["fuente"]).ToArray();
        await WhenTheIndicatorIsDefinedAgain(version);
        api.AssertStatus(HttpStatusCode.Created);
    }

    [When("se define otra vez la versión {string} del indicador")]
    public async Task WhenTheIndicatorIsDefinedAgain(string version) =>
        await api.PostAsync(Service.Reporting, "/api/v1/report-definitions", new
        {
            code = _definitionCode,
            name = "Alertas de fatiga e incidentes por turno",
            version,
            dataSources = _dataSources
        });

    [When("el jefe de seguridad solicita el reporte en formato {string} de los últimos {int} días")]
    public async Task WhenAReportIsRequested(string format, int days)
    {
        var to = DateTimeOffset.UtcNow;
        await RequestAsync(_definitionCode, format, to.AddDays(-days), to);
        api.AssertStatus(HttpStatusCode.Created);
        _reportId = api.JsonBody.GetProperty("id").GetGuid();
    }

    [When("el jefe de seguridad solicita un reporte de un indicador que no existe")]
    public Task WhenAReportOfAnUnknownIndicatorIsRequested() =>
        RequestAsync($"NO-EXISTE-{api.Suffix}", "Csv", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);

    [When("el jefe de seguridad solicita un reporte con el período invertido")]
    public Task WhenAReportWithAnInvertedPeriodIsRequested() =>
        RequestAsync(_definitionCode, "Csv", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-7));

    [When("el jefe de seguridad solicita un reporte en un formato no soportado")]
    public Task WhenAReportInAnUnsupportedFormatIsRequested() =>
        RequestAsync(_definitionCode, "Xlsx", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);

    [Then("el reporte queda en estado {string} con la versión {string} del indicador")]
    public async Task ThenTheReportIs(string status, string version)
    {
        var report = await api.ExpectAsync(HttpStatusCode.OK, Service.Reporting, HttpMethod.Get, $"/api/v1/reports/{_reportId}");
        Assert.Equal(status, report.GetProperty("status").GetString());
        Assert.Equal(version, report.GetProperty("definitionVersion").GetString());
        Assert.Equal(_definitionCode, report.GetProperty("definitionCode").GetString());
    }

    [Then("el reporte queda aceptado para su generación")]
    public async Task ThenTheReportIsAccepted()
    {
        var report = await api.ExpectAsync(HttpStatusCode.OK, Service.Reporting, HttpMethod.Get, $"/api/v1/reports/{_reportId}");
        Assert.Contains(report.GetProperty("status").GetString(), new[] { "Rendering", "Ready" });
    }

    [Then("el archivo descargado coincide con su suma de verificación SHA-256")]
    public async Task ThenTheDownloadMatchesItsChecksum()
    {
        var response = await api.Services.Client(Service.Reporting).GetAsync($"/api/v1/reports/{_reportId}/download");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(content);
        var checksum = Assert.Single(response.Headers.GetValues("X-Checksum"));
        Assert.Equal($"sha256:{Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()}", checksum);
    }

    private async Task RequestAsync(string definitionCode, string format, DateTimeOffset from, DateTimeOffset to) =>
        await api.PostAsync(Service.Reporting, "/api/v1/reports/safety", new
        {
            from,
            to,
            siteId = $"UM-{api.Suffix}",
            definitionCode,
            format,
            requestedBy = Guid.NewGuid()
        });
}
