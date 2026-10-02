using System.Diagnostics;
using System.Net;

namespace IPMax.Tests;

public sealed class RetryTests
{
    [Fact]
    public async Task ServiceUnavailableThenSuccessReusesTheIdempotencyKey()
    {
        using var harness = new Harness([Replies.Unavailable("0"), Replies.Fixture("geoip-8-8-8-8")]);

        var geo = await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal("8.8.8.8", geo.Ip);
        Assert.Equal(2, harness.Requests.Count);
        var first = harness.Requests[0].Header("Idempotency-Key");
        Assert.NotNull(first);
        Assert.Equal(first, harness.Requests[1].Header("Idempotency-Key"));
        Assert.Equal(harness.Requests[0].Body, harness.Requests[1].Body);
    }

    [Fact]
    public async Task RetryAfterIsHonoured()
    {
        using var harness = new Harness([Replies.Unavailable("0.2"), Replies.Fixture("catalog")]);
        var stopwatch = Stopwatch.StartNew();

        await harness.Client.GetCatalogAsync(Ct);

        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(190), $"Retried after {stopwatch.Elapsed}.");
        Assert.Equal(2, harness.Requests.Count);
    }

    [Fact]
    public async Task RetriesStopAfterMaxRetries()
    {
        using var harness = new Harness([Replies.Unavailable("0")], Harness.Options with { MaxRetries = 2 });

        var error = await Assert.ThrowsAsync<IPMaxServerException>(
            () => harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal(3, harness.Requests.Count);
        Assert.Single(harness.Requests.Select(request => request.Header("Idempotency-Key")).Distinct());
    }

    [Fact]
    public async Task ZeroMaxRetriesMakesOneAttempt()
    {
        using var harness = new Harness([Replies.Unavailable("0")], Harness.Options with { MaxRetries = 0 });

        await Assert.ThrowsAsync<IPMaxServerException>(() => harness.Client.GetCatalogAsync(Ct));

        Assert.Single(harness.Requests);
    }

    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    public async Task RetryableStatusesAreRetried(int status)
    {
        using var harness = new Harness(
            [Replies.Error((HttpStatusCode)status, 1400, true, ("Retry-After", "0")), Replies.Fixture("catalog")]);

        await harness.Client.GetCatalogAsync(Ct);

        Assert.Equal(2, harness.Requests.Count);
    }

    [Theory]
    [InlineData("error-not-found")]
    [InlineData("error-invalid-request")]
    [InlineData("error-unauthorized")]
    public async Task ClientErrorsAreNotRetried(string fixture)
    {
        using var harness = new Harness([Replies.Fixture(fixture), Replies.Fixture("geoip-8-8-8-8")]);

        await Assert.ThrowsAnyAsync<IPMaxApiException>(() => harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct));

        Assert.Single(harness.Requests);
    }

    [Fact]
    public async Task DroppedConnectionIsRetriedWithTheSameKey()
    {
        using var harness = new Harness([Replies.Drop(), Replies.Fixture("geoip-8-8-8-8")], Harness.Options with { MaxRetries = 1 });

        var geo = await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal("USD", geo.Currency!.Code);
        Assert.Equal(2, harness.Requests.Count);
        Assert.Equal(harness.Requests[0].Header("Idempotency-Key"), harness.Requests[1].Header("Idempotency-Key"));
    }

    [Fact]
    public async Task TimedOutAttemptIsRetriedWithTheSameKey()
    {
        using var harness = new Harness(
            [Replies.Hang(), Replies.Fixture("intelligence-8-8-8-8")],
            Harness.Options with { MaxRetries = 1, Timeout = TimeSpan.FromMilliseconds(100) });

        var data = await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal("8.8.8.8", data.Ip);
        Assert.Equal(2, harness.Requests.Count);
        Assert.Equal(harness.Requests[0].Header("Idempotency-Key"), harness.Requests[1].Header("Idempotency-Key"));
    }
}
