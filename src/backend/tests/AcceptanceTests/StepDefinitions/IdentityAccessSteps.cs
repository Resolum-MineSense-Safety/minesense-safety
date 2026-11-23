using System.Net;
using System.Text.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// TS01 and US18 (EP08, Identity &amp; Access Service). Every scenario signs up its own user with a
/// unique username; US18 signs in as the bootstrap administrator of the in-memory host.
/// </summary>
[Binding]
public class IdentityAccessSteps(ApiContext api)
{
    private const string Password = "Turno-Noche-2026";

    private string _username = string.Empty;
    private Guid _userId;
    private Guid _miningUnitId;
    private string? _accessToken;
    private string? _refreshToken;
    private string? _previousRefreshToken;

    [Given("un usuario registrado con el rol {string}")]
    public async Task GivenARegisteredUser(string role)
    {
        _username = $"acc-{api.Suffix.ToLowerInvariant()}";
        await PublicAsync("/api/v1/authentication/sign-up", new { username = _username, fullName = "Carmen Salas", password = Password, role });
        api.AssertStatus(HttpStatusCode.Created);
        _userId = api.JsonBody.GetProperty("id").GetGuid();
    }

    [Given("el usuario inicia sesión con su contraseña")]
    [When("el usuario inicia sesión con su contraseña")]
    public async Task WhenTheUserSignsIn()
    {
        await SignInAsync(_username, Password);
        if (api.Status != HttpStatusCode.OK) return;
        _accessToken = api.JsonBody.GetProperty("accessToken").GetString();
        _refreshToken = api.JsonBody.GetProperty("refreshToken").GetString();
    }

    [When("se inicia sesión con una contraseña incorrecta")]
    public Task WhenSigningInWithAWrongPassword() => SignInAsync(_username, Password + "x");

    [When("se inicia sesión con un usuario que no existe")]
    public Task WhenSigningInWithAnUnknownUser() => SignInAsync($"nadie-{api.Suffix.ToLowerInvariant()}", Password);

    [When("se inicia sesión con una contraseña vacía")]
    public Task WhenSigningInWithAnEmptyPassword() => SignInAsync(_username, string.Empty);

    [When("el usuario renueva su sesión")]
    public async Task WhenTheUserRefreshes()
    {
        _previousRefreshToken = _refreshToken;
        await PublicAsync("/api/v1/auth/refresh", new { refreshToken = _refreshToken });
        api.AssertStatus(HttpStatusCode.OK);
        var session = api.JsonBody;
        _accessToken = session.GetProperty("accessToken").GetString();
        _refreshToken = session.GetProperty("refreshToken").GetString();
    }

    [When("el usuario cierra su sesión")]
    public async Task WhenTheUserSignsOut()
    {
        await PublicAsync("/api/v1/authentication/sign-out", new { refreshToken = _refreshToken });
        api.AssertStatus(HttpStatusCode.NoContent);
    }

    [When("alguien se registra por su cuenta con el rol {string}")]
    public Task WhenSomeoneSignsUpAs(string role) => WhenSomeoneSignsUpWithPassword(role, Password);

    [When("alguien se registra por su cuenta con el rol {string} y la contraseña {string}")]
    public async Task WhenSomeoneSignsUpWithPassword(string role, string password) =>
        await PublicAsync("/api/v1/authentication/sign-up",
            new { username = $"acc-{api.Suffix.ToLowerInvariant()}", fullName = "Pedro Ramos", password, role });

    [Given("el administrador le concede acceso a una unidad minera")]
    [When("el administrador le concede acceso a una unidad minera")]
    public async Task WhenTheAdministratorGrantsAMiningUnit()
    {
        _miningUnitId = Guid.NewGuid();
        await SendAsAsync(await AdministratorTokenAsync(), HttpMethod.Post, $"/api/v1/users/{_userId}/mining-units",
            new { miningUnitId = _miningUnitId });
        api.AssertStatus(HttpStatusCode.OK);
    }

    [When("el administrador le revoca el acceso a esa unidad minera")]
    public async Task WhenTheAdministratorRevokesTheMiningUnit()
    {
        await SendAsAsync(await AdministratorTokenAsync(), HttpMethod.Delete, $"/api/v1/users/{_userId}/mining-units/{_miningUnitId}");
        api.AssertStatus(HttpStatusCode.OK);
    }

    [When("el administrador desactiva al usuario")]
    public async Task WhenTheAdministratorDeactivatesTheUser()
    {
        await SendAsAsync(await AdministratorTokenAsync(), HttpMethod.Post, $"/api/v1/users/{_userId}/deactivation");
        api.AssertStatus(HttpStatusCode.OK);
    }

    [When("el usuario intenta concederse acceso a una unidad minera")]
    public async Task WhenTheUserTriesToGrantItself() =>
        await SendAsAsync(_accessToken!, HttpMethod.Post, $"/api/v1/users/{_userId}/mining-units",
            new { miningUnitId = Guid.NewGuid() });

    [Then("recibe un token de acceso {string} que vence en {int} minutos como máximo")]
    public void ThenReceivesAnAccessToken(string tokenType, int minutes)
    {
        api.AssertStatus(HttpStatusCode.OK);
        var session = api.JsonBody;
        Assert.Equal(tokenType, session.GetProperty("tokenType").GetString());
        Assert.InRange(session.GetProperty("expiresIn").GetInt32(), 1, minutes * 60);
        Assert.Equal(3, session.GetProperty("accessToken").GetString()!.Split('.').Length);
        Assert.False(string.IsNullOrEmpty(session.GetProperty("refreshToken").GetString()));
    }

    [Then("su perfil muestra el rol {string}")]
    public async Task ThenTheProfileShowsTheRole(string role)
    {
        await SendAsAsync(_accessToken!, HttpMethod.Get, "/api/v1/users/me");
        api.AssertStatus(HttpStatusCode.OK);
        Assert.Equal(role, api.JsonBody.GetProperty("role").GetString());
        Assert.Equal(_username, api.JsonBody.GetProperty("username").GetString());
    }

    [Then("recibe un nuevo par de tokens")]
    public void ThenReceivesANewPair() => Assert.NotEqual(_previousRefreshToken, _refreshToken);

    [Then("el token de renovación anterior ya no es aceptado")]
    public async Task ThenThePreviousRefreshTokenIsRejected() => await AssertRefreshRejectedAsync(_previousRefreshToken!);

    [Then("su token de renovación ya no es aceptado")]
    public async Task ThenTheRefreshTokenIsRejected() => await AssertRefreshRejectedAsync(_refreshToken!);

    [Then("su sesión incluye esa unidad minera")]
    public void ThenTheSessionIncludesTheMiningUnit()
    {
        api.AssertStatus(HttpStatusCode.OK);
        Assert.Equal([_miningUnitId], MiningUnits());
    }

    [Then("su sesión no incluye ninguna unidad minera")]
    public void ThenTheSessionHasNoMiningUnit()
    {
        api.AssertStatus(HttpStatusCode.OK);
        Assert.Empty(MiningUnits());
    }

    private Guid[] MiningUnits() =>
        api.JsonBody.GetProperty("miningUnitIds").EnumerateArray().Select(item => item.GetGuid()).ToArray();

    private async Task AssertRefreshRejectedAsync(string refreshToken)
    {
        await PublicAsync("/api/v1/authentication/refresh", new { refreshToken });
        api.AssertStatus(HttpStatusCode.Unauthorized);
    }

    private Task SignInAsync(string username, string password) =>
        PublicAsync("/api/v1/authentication/sign-in", new { username, password });

    /// <summary>Public routes are called without credentials, as a person signing up or in would.</summary>
    private Task<HttpResponseMessage> PublicAsync(string path, object body) =>
        api.SendAsync(Service.IdentityAccess, HttpMethod.Post, path, body, api.Services.Anonymous(Service.IdentityAccess));

    private async Task<string> AdministratorTokenAsync()
    {
        await SignInAsync(MineSenseServices.AdministratorUsername, MineSenseServices.AdministratorPassword);
        api.AssertStatus(HttpStatusCode.OK);
        return api.JsonBody.GetProperty("accessToken").GetString()!;
    }

    /// <summary>Sends the request with the given user's token instead of the acceptance credentials.</summary>
    private async Task SendAsAsync(string accessToken, HttpMethod method, string path, object? body = null) =>
        await api.SendAsync(Service.IdentityAccess, method, path, body,
            api.Services.WithBearer(Service.IdentityAccess, accessToken));
}
