using System.Net;

namespace IPMax.Tests;

public sealed class RequestTests
{
    private const string UuidV4Pattern = "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$";

    [Fact]
    public async Task CatalogIsAPlainGet()
    {
        using var harness = new Harness([Replies.Fixture("catalog")]);

        await harness.Client.GetCatalogAsync(Ct);

        var request = Assert.Single(harness.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri("https://api.test/api/v1/catalog"), request.Uri);
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal("ipmax-dotnet/0.1.0", request.Header("User-Agent"));
        Assert.Equal($"Bearer {Harness.ApiKey}", request.Header("Authorization"));
        Assert.Null(request.Header("Idempotency-Key"));
        Assert.Null(request.Body);
    }

    [Fact]
    public async Task AccountIsAnAuthenticatedGet()
    {
        using var harness = new Harness([Replies.Fixture("account")]);

        await harness.Client.GetAccountAsync(Ct);

        var request = Assert.Single(harness.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri("https://api.test/api/v1/account"), request.Uri);
        Assert.Equal($"Bearer {Harness.ApiKey}", request.Header("Authorization"));
        Assert.Null(request.Header("Idempotency-Key"));
    }

    [Theory]
    [InlineData("geoip", "geoip-8-8-8-8")]
    [InlineData("intelligence", "intelligence-8-8-8-8")]
    public async Task LookupsPostJsonWithAFreshUuidV4IdempotencyKey(string product, string fixture)
    {
        using var harness = new Harness([Replies.Fixture(fixture)]);

        _ = product == "geoip"
            ? (object)await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct)
            : await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);

        var request = Assert.Single(harness.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri($"https://api.test/api/v1/{product}"), request.Uri);
        Assert.Equal("application/json", request.ContentType);
        Assert.Equal("""{"ip":"8.8.8.8"}""", request.Body);
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal("ipmax-dotnet/0.1.0", request.Header("User-Agent"));
        Assert.Equal($"Bearer {Harness.ApiKey}", request.Header("Authorization"));
        Assert.Matches(UuidV4Pattern, request.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task LookupTrimsTheAddress()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")]);

        await harness.Client.LookupGeoIpAsync("  8.8.8.8\n", cancellationToken: Ct);

        Assert.Equal("""{"ip":"8.8.8.8"}""", Assert.Single(harness.Requests).Body);
    }

    [Fact]
    public async Task EachLookupGetsItsOwnIdempotencyKey()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")], Harness.Options with { CacheSize = 0 });

        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        var keys = harness.Requests.Select(request => request.Header("Idempotency-Key")).ToArray();
        Assert.Equal(2, keys.Length);
        Assert.All(keys, key => Assert.Matches(UuidV4Pattern, key));
        Assert.NotEqual(keys[0], keys[1]);
    }

    [Fact]
    public async Task ExplicitIdempotencyKeyIsSentVerbatim()
    {
        using var harness = new Harness([Replies.Fixture("intelligence-8-8-8-8")]);

        await harness.Client.LookupIntelligenceAsync("8.8.8.8", "order-1042-intelligence", Ct);

        Assert.Equal("order-1042-intelligence", Assert.Single(harness.Requests).Header("Idempotency-Key"));
    }

    [Theory]
    [InlineData("https://proxy.test/ipmax", "https://proxy.test/ipmax/api/v1/catalog")]
    [InlineData("https://proxy.test/ipmax/", "https://proxy.test/ipmax/api/v1/catalog")]
    [InlineData("http://127.0.0.1:8080", "http://127.0.0.1:8080/api/v1/catalog")]
    public async Task BaseAddressIsHonoured(string baseAddress, string expected)
    {
        using var harness = new Harness([Replies.Fixture("catalog")], Harness.Options with { BaseAddress = new Uri(baseAddress) });

        await harness.Client.GetCatalogAsync(Ct);

        Assert.Equal(new Uri(expected), Assert.Single(harness.Requests).Uri);
    }

    [Fact]
    public void DefaultOptionsMatchTheContract()
    {
        var options = new IPMaxClientOptions();

        Assert.Equal(new Uri("https://api.ipm.ax"), options.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(10), options.Timeout);
        Assert.Equal(2, options.MaxRetries);
        Assert.Equal(1024, options.CacheSize);
        Assert.Equal(TimeSpan.FromMinutes(5), options.CacheTtl);

        using var client = new IPMaxClient("sk_test_key");
    }

    [Fact]
    public void InvalidOptionsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IPMaxClient("k", new IPMaxClientOptions { MaxRetries = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IPMaxClient("k", new IPMaxClientOptions { CacheSize = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IPMaxClient("k", new IPMaxClientOptions { Timeout = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IPMaxClient("k", new IPMaxClientOptions { CacheTtl = TimeSpan.FromSeconds(-1) }));
        Assert.Throws<ArgumentException>(() => new IPMaxClient("k", new IPMaxClientOptions { BaseAddress = new Uri("/relative", UriKind.Relative) }));
        Assert.Throws<ArgumentNullException>(() => new IPMaxClient((HttpClient)null!));
    }
}

[CollectionDefinition(nameof(SharedEnvironment), DisableParallelization = true)]
public sealed class SharedEnvironment;

[Collection(nameof(SharedEnvironment))]
public sealed class EnvironmentTests
{
    private const string Variable = "IPMAX_API_KEY";

    [Fact]
    public async Task ApiKeyFallsBackToTheEnvironment()
    {
        using var scope = new EnvironmentScope("sk_from_env");
        using var harness = new Harness([Replies.Fixture("account")], apiKey: null);

        await harness.Client.GetAccountAsync(Ct);

        Assert.Equal("Bearer sk_from_env", Assert.Single(harness.Requests).Header("Authorization"));
    }

    [Fact]
    public async Task ExplicitApiKeyWinsOverTheEnvironment()
    {
        using var scope = new EnvironmentScope("sk_from_env");
        using var harness = new Harness([Replies.Fixture("account")]);

        await harness.Client.GetAccountAsync(Ct);

        Assert.Equal($"Bearer {Harness.ApiKey}", Assert.Single(harness.Requests).Header("Authorization"));
    }

    [Fact]
    public async Task CatalogWorksWithoutAnApiKey()
    {
        using var scope = new EnvironmentScope(null);
        using var harness = new Harness([Replies.Fixture("catalog")], apiKey: null);

        var catalog = await harness.Client.GetCatalogAsync(Ct);

        Assert.True(catalog.Available);
        Assert.Null(Assert.Single(harness.Requests).Header("Authorization"));
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly string? _previous = Environment.GetEnvironmentVariable(Variable);

        public EnvironmentScope(string? value) => Environment.SetEnvironmentVariable(Variable, value);

        public void Dispose() => Environment.SetEnvironmentVariable(Variable, _previous);
    }
}
