namespace IPMax.Tests;

public sealed class CacheTests
{
    [Fact]
    public async Task RepeatedLookupHitsTheServerOnce()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")]);

        var first = await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        var second = await harness.Client.LookupGeoIpAsync(" 8.8.8.8 ", cancellationToken: Ct);

        Assert.Same(first, second);
        Assert.Single(harness.Requests);
    }

    [Fact]
    public async Task ProductsAreCachedSeparately()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8"), Replies.Fixture("intelligence-8-8-8-8")]);

        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal(2, harness.Requests.Count);
    }

    [Fact]
    public async Task ExpiredEntriesAreFetchedAgain()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")], Harness.Options with { CacheTtl = TimeSpan.FromMilliseconds(50) });

        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        await Task.Delay(TimeSpan.FromMilliseconds(120), Ct);
        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal(2, harness.Requests.Count);
    }

    [Fact]
    public async Task LeastRecentlyUsedEntryIsEvicted()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")], Harness.Options with { CacheSize = 2 });

        foreach (var ip in new[] { "1.1.1.1", "2.2.2.2", "1.1.1.1", "3.3.3.3", "1.1.1.1", "2.2.2.2" })
        {
            await harness.Client.LookupGeoIpAsync(ip, cancellationToken: Ct);
        }

        string[] fetched = ["1.1.1.1", "2.2.2.2", "3.3.3.3", "2.2.2.2"];
        Assert.Equal(fetched.Select(ip => $$"""{"ip":"{{ip}}"}"""), harness.Requests.Select(request => request.Body));
    }

    [Fact]
    public async Task SizeZeroDisablesTheCache()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")], Harness.Options with { CacheSize = 0 });

        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal(2, harness.Requests.Count);
    }

    [Fact]
    public async Task ErrorsAreNotCached()
    {
        using var harness = new Harness([Replies.Fixture("error-not-found"), Replies.Fixture("geoip-8-8-8-8")]);

        await Assert.ThrowsAsync<IPMaxNotFoundException>(() => harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct));
        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal(2, harness.Requests.Count);
    }

    [Fact]
    public async Task ExplicitIdempotencyKeyBypassesTheReadButStoresTheResult()
    {
        using var harness = new Harness([Replies.Fixture("intelligence-8-8-8-8")]);

        await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);
        await harness.Client.LookupIntelligenceAsync("8.8.8.8", "order-1042-intelligence", Ct);
        Assert.Equal(2, harness.Requests.Count);

        await harness.Client.LookupIntelligenceAsync("9.9.9.9", "order-1043-intelligence", Ct);
        await harness.Client.LookupIntelligenceAsync("9.9.9.9", cancellationToken: Ct);
        Assert.Equal(3, harness.Requests.Count);
    }

    [Fact]
    public async Task CatalogAndAccountAreNeverCached()
    {
        using var harness = new Harness(
            [Replies.Fixture("catalog"), Replies.Fixture("catalog"), Replies.Fixture("account"), Replies.Fixture("account")]);

        await harness.Client.GetCatalogAsync(Ct);
        await harness.Client.GetCatalogAsync(Ct);
        await harness.Client.GetAccountAsync(Ct);
        await harness.Client.GetAccountAsync(Ct);

        Assert.Equal(4, harness.Requests.Count);
    }

    [Fact]
    public async Task ConcurrentLookupsAreSafe()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")], Harness.Options with { CacheSize = 8 });

        await Task.WhenAll(Enumerable.Range(0, 200).Select(index =>
            harness.Client.LookupGeoIpAsync($"10.0.0.{index % 16}", cancellationToken: Ct)));

        Assert.InRange(harness.Requests.Count, 16, 200);
    }
}
