using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace IPMax.Tests;

internal static class Ambient
{
    public static CancellationToken Ct => TestContext.Current.CancellationToken;
}

internal sealed record Fixture(string Name, HttpStatusCode Status, IReadOnlyDictionary<string, string> Headers, JsonObject Body)
{
    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static IReadOnlyList<string> Names { get; } =
        [.. Directory.GetFiles(FixtureDirectory, "*.json").Select(path => Path.GetFileNameWithoutExtension(path)).Order(StringComparer.Ordinal)];

    public JsonObject Data => Body["data"]!.AsObject();

    public static Fixture Load(string name)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureDirectory, name + ".json")))!.AsObject();
        var headers = root["headers"]!.AsObject().ToDictionary(header => header.Key, header => header.Value!.GetValue<string>());
        return new Fixture(name, (HttpStatusCode)root["status"]!.GetValue<int>(), headers, root["body"]!.AsObject());
    }
}

internal delegate Task<HttpResponseMessage> Reply(HttpRequestMessage request, CancellationToken cancellationToken);

internal static class Replies
{
    public static Reply Fixture(string name) => From(Tests.Fixture.Load(name));

    public static Reply From(Fixture fixture) => Json(fixture.Status, fixture.Body, [.. fixture.Headers.Select(header => (header.Key, header.Value))]);

    public static Reply Json(HttpStatusCode status, JsonNode body, params (string Name, string Value)[] headers) =>
        Text(status, body.ToJsonString(), "application/json", headers);

    public static Reply Text(HttpStatusCode status, string body, string mediaType, params (string Name, string Value)[] headers) =>
        (_, _) =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
            foreach (var (name, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return Task.FromResult(response);
        };

    public static Reply Error(HttpStatusCode status, int code, bool retryable, params (string Name, string Value)[] headers) =>
        Json(
            status,
            new JsonObject
            {
                ["status"] = false,
                ["data"] = null,
                ["error"] = new JsonObject
                {
                    ["code"] = code,
                    ["message"] = $"HTTP {(int)status}",
                    ["request_id"] = $"req-{(int)status}",
                    ["retryable"] = retryable,
                },
            },
            headers);

    public static Reply Unavailable(string retryAfter) =>
        Error(HttpStatusCode.ServiceUnavailable, 1400, true, ("Retry-After", retryAfter));

    public static Reply Drop() =>
        (_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection reset by peer."));

    public static Reply Hang() =>
        async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        };
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType,
    string? Body)
{
    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}

internal sealed class FakeHandler(IReadOnlyList<Reply> replies) : HttpMessageHandler
{
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => string.Join(", ", header.Value),
            StringComparer.OrdinalIgnoreCase);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, headers, request.Content?.Headers.ContentType?.ToString(), body);
        int index;
        lock (_requests)
        {
            index = _requests.Count;
            _requests.Add(recorded);
        }

        return await replies[Math.Min(index, replies.Count - 1)](request, cancellationToken);
    }
}

internal sealed class Harness : IDisposable
{
    public const string ApiKey = "sk_test_key";

    public static readonly IPMaxClientOptions Options = new() { BaseAddress = new Uri("https://api.test") };

    private readonly HttpClient _http;

    public Harness(IReadOnlyList<Reply> replies, IPMaxClientOptions? options = null, string? apiKey = ApiKey)
    {
        Handler = new FakeHandler(replies);
        _http = new HttpClient(Handler);
        Client = new IPMaxClient(_http, apiKey, options ?? Options);
    }

    public FakeHandler Handler { get; }

    public IPMaxClient Client { get; }

    public IReadOnlyList<RecordedRequest> Requests => Handler.Requests;

    public void Dispose()
    {
        Client.Dispose();
        _http.Dispose();
    }
}
