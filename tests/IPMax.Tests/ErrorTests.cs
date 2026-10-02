using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IPMax.Tests;

public sealed class ErrorTests
{
    private static readonly IPMaxClientOptions NoRetries = Harness.Options with { MaxRetries = 0 };

    [Fact]
    public async Task InvalidRequestFixture()
    {
        using var harness = new Harness([Replies.Fixture("error-invalid-request")], NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxInvalidRequestException>(
            () => harness.Client.LookupGeoIpAsync("not-an-ip", cancellationToken: Ct));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal(ErrorCode.InvalidRequest, error.Code);
        Assert.Equal("A valid IP and 16-120 character Idempotency-Key are required", error.Message);
        Assert.Equal("ENT-9b178079-212d-4052-ae72-8edf564be6e1", error.RequestId);
        Assert.False(error.Retryable);
        Assert.Null(error.RetryAfter);
        Assert.Single(harness.Requests);
    }

    [Fact]
    public async Task UnauthorizedFixture()
    {
        using var harness = new Harness([Replies.Fixture("error-unauthorized")], NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxAuthenticationException>(() => harness.Client.GetAccountAsync(Ct));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Equal(ErrorCode.InvalidApiKey, error.Code);
        Assert.Equal("Invalid API key", error.Message);
        Assert.Equal("ENT-706ff971-cb85-4933-8b6a-c4e42e0ecce1", error.RequestId);
    }

    [Fact]
    public async Task NotFoundFixture()
    {
        using var harness = new Harness([Replies.Fixture("error-not-found")], NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxNotFoundException>(
            () => harness.Client.LookupIntelligenceAsync("192.0.2.1", cancellationToken: Ct));

        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Equal(ErrorCode.IpNotFound, error.Code);
        Assert.Equal("ENT-f1b619f1-4da9-48c6-bb41-8ac819ea863a", error.RequestId);
    }

    [Theory]
    [InlineData(400, typeof(IPMaxInvalidRequestException))]
    [InlineData(401, typeof(IPMaxAuthenticationException))]
    [InlineData(402, typeof(IPMaxInsufficientBalanceException))]
    [InlineData(404, typeof(IPMaxNotFoundException))]
    [InlineData(409, typeof(IPMaxConflictException))]
    [InlineData(413, typeof(IPMaxInvalidRequestException))]
    [InlineData(415, typeof(IPMaxInvalidRequestException))]
    [InlineData(429, typeof(IPMaxRateLimitException))]
    [InlineData(500, typeof(IPMaxServerException))]
    [InlineData(503, typeof(IPMaxServerException))]
    [InlineData(418, typeof(IPMaxApiException))]
    public async Task StatusMapsToExceptionType(int status, Type expected)
    {
        using var harness = new Harness([Replies.Error((HttpStatusCode)status, 1000, false)], NoRetries);

        var error = await Assert.ThrowsAnyAsync<IPMaxException>(
            () => harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct));

        Assert.IsType(expected, error);
        var api = Assert.IsAssignableFrom<IPMaxApiException>(error);
        Assert.Equal((HttpStatusCode)status, api.StatusCode);
        Assert.Equal($"req-{status}", api.RequestId);
    }

    [Fact]
    public async Task RateLimitCarriesRetryAfter()
    {
        using var harness = new Harness(
            [Replies.Error(HttpStatusCode.TooManyRequests, 1011, true, ("Retry-After", "7"))],
            NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxRateLimitException>(() => harness.Client.GetCatalogAsync(Ct));

        Assert.Equal(ErrorCode.RateLimitExceeded, error.Code);
        Assert.True(error.Retryable);
        Assert.Equal(TimeSpan.FromSeconds(7), error.RetryAfter);
    }

    [Fact]
    public async Task NonJsonErrorBodyStillProducesAnApiError()
    {
        using var harness = new Harness(
            [Replies.Text(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>", "text/html", ("X-Request-ID", "edge-42"))],
            NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxServerException>(() => harness.Client.GetCatalogAsync(Ct));

        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.Null(error.Code);
        Assert.Equal("edge-42", error.RequestId);
        Assert.True(error.Retryable);
        Assert.Contains("502", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedJsonErrorBodyStillProducesAnApiError()
    {
        using var harness = new Harness(
            [Replies.Json(HttpStatusCode.Conflict, new JsonObject { ["message"] = "conflict" })],
            NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxConflictException>(
            () => harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct));

        Assert.Null(error.Code);
        Assert.Null(error.RequestId);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task UndecodableSuccessBodyIsAnIPMaxException()
    {
        using var harness = new Harness([Replies.Text(HttpStatusCode.OK, "{\"status\":true}", "application/json")], NoRetries);

        var error = await Assert.ThrowsAsync<IPMaxException>(() => harness.Client.GetCatalogAsync(Ct));

        Assert.IsType<JsonException>(error.InnerException);
    }

    [Fact]
    public async Task ConnectionFailureIsAConnectionException()
    {
        using var harness = new Harness([Replies.Drop()], Harness.Options with { MaxRetries = 1 });

        var error = await Assert.ThrowsAsync<IPMaxConnectionException>(() => harness.Client.GetCatalogAsync(Ct));

        Assert.IsType<HttpRequestException>(error.InnerException);
        Assert.Equal(2, harness.Requests.Count);
    }

    [Fact]
    public async Task SlowAttemptIsATimeoutException()
    {
        using var harness = new Harness([Replies.Hang()], NoRetries with { Timeout = TimeSpan.FromMilliseconds(100) });

        await Assert.ThrowsAsync<IPMaxTimeoutException>(() => harness.Client.GetCatalogAsync(Ct));
    }

    [Fact]
    public async Task CallerCancellationIsNotWrapped()
    {
        using var harness = new Harness([Replies.Hang()]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: cancellation.Token));
        Assert.Single(harness.Requests);
    }

    [Fact]
    public void EveryExceptionSharesTheRoot()
    {
        Assert.True(typeof(IPMaxException).IsAssignableFrom(typeof(IPMaxApiException)));
        Assert.True(typeof(IPMaxException).IsAssignableFrom(typeof(IPMaxConnectionException)));
        Assert.True(typeof(IPMaxException).IsAssignableFrom(typeof(IPMaxTimeoutException)));
        Assert.False(typeof(IPMaxApiException).IsAssignableFrom(typeof(IPMaxConnectionException)));
        Assert.False(typeof(IPMaxApiException).IsAssignableFrom(typeof(IPMaxTimeoutException)));
    }
}
