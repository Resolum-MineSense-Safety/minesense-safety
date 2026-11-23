using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>
/// HTTP calls of one scenario: sends a request to a microservice, keeps the last response and its
/// JSON body for the Then steps, and remembers the ids created along the way.
/// </summary>
public sealed class ApiContext(MineSenseServices services)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MineSenseServices Services => services;

    public HttpResponseMessage? Response { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>Short suffix that keeps codes and names unique, so scenarios can share a deployed database.</summary>
    public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    public JsonElement JsonBody => JsonDocument.Parse(Body).RootElement;

    public HttpStatusCode Status => Response?.StatusCode ?? throw new InvalidOperationException("No request was sent.");

    public Task<HttpResponseMessage> GetAsync(Service service, string path) =>
        SendAsync(service, HttpMethod.Get, path);

    public Task<HttpResponseMessage> PostAsync(Service service, string path, object? body = null) =>
        SendAsync(service, HttpMethod.Post, path, body);

    public Task<HttpResponseMessage> PutAsync(Service service, string path, object body) =>
        SendAsync(service, HttpMethod.Put, path, body);

    public Task<HttpResponseMessage> PatchAsync(Service service, string path, object? body = null) =>
        SendAsync(service, HttpMethod.Patch, path, body);

    public async Task<HttpResponseMessage> SendAsync(Service service, HttpMethod method, string path, object? body = null,
        HttpClient? client = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = body as HttpContent ?? JsonContent.Create(body, body.GetType(), options: Json);
        Response = await (client ?? services.Client(service)).SendAsync(request);
        Body = await Response.Content.ReadAsStringAsync();
        return Response;
    }

    /// <summary>Sends the request and fails with the body when the status is not the expected one.</summary>
    public async Task<JsonElement> ExpectAsync(HttpStatusCode expected, Service service, HttpMethod method, string path,
        object? body = null)
    {
        await SendAsync(service, method, path, body);
        AssertStatus(expected);
        return string.IsNullOrWhiteSpace(Body) ? default : JsonBody;
    }

    public void AssertStatus(HttpStatusCode expected) =>
        Assert.True(Status == expected,
            $"Expected HTTP {(int)expected} but got {(int)Status} from {Response!.RequestMessage?.RequestUri}: {Body}");

    public T Read<T>() => JsonSerializer.Deserialize<T>(Body, Json)!;

    public static StringContent RawJson(string json) => new(json, Encoding.UTF8, "application/json");

    /// <summary>Splits a Gherkin list such as "EyeTracking, HeartRate" (an empty cell is an empty list).</summary>
    public static string[] List(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
