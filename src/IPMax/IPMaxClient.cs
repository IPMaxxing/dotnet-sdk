using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace IPMax;

public sealed class IPMaxClient : IDisposable
{
    private const string ApiKeyVariable = "IPMAX_API_KEY";
    private const string JsonMediaType = "application/json";
    private static readonly TimeSpan BackoffBase = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan BackoffCap = TimeSpan.FromSeconds(8);
    private static readonly string UserAgent =
        "ipmax-dotnet/" + typeof(IPMaxClient).Assembly.GetName().Version!.ToString(3);

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly string? _apiKey;
    private readonly string _apiRoot;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly LruCache? _cache;

    public IPMaxClient(string? apiKey = null, IPMaxClientOptions? options = null)
        : this(options, apiKey, null)
    {
    }

    public IPMaxClient(HttpClient httpClient, string? apiKey = null, IPMaxClientOptions? options = null)
        : this(options, apiKey, httpClient ?? throw new ArgumentNullException(nameof(httpClient)))
    {
    }

    private IPMaxClient(IPMaxClientOptions? options, string? apiKey, HttpClient? httpClient)
    {
        options ??= new IPMaxClientOptions();
        ArgumentNullException.ThrowIfNull(options.BaseAddress);
        if (!options.BaseAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("BaseAddress must be an absolute URI.", nameof(options));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.Timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MaxRetries);
        ArgumentOutOfRangeException.ThrowIfNegative(options.CacheSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.CacheTtl, TimeSpan.Zero);

        var resolvedApiKey = apiKey ?? Environment.GetEnvironmentVariable(ApiKeyVariable);
        _apiKey = string.IsNullOrEmpty(resolvedApiKey) ? null : resolvedApiKey;
        _http = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttp = httpClient is null;
        _apiRoot = options.BaseAddress.AbsoluteUri.TrimEnd('/') + "/api/v1/";
        _timeout = options.Timeout;
        _maxRetries = options.MaxRetries;
        _cache = options.CacheSize > 0 ? new LruCache(options.CacheSize, options.CacheTtl) : null;
    }

    public Task<Catalog> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Get, "catalog", null, null, IPMaxJsonContext.Default.CatalogResponse, static response => response.Data, cancellationToken);

    public Task<Account> GetAccountAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Get, "account", null, null, IPMaxJsonContext.Default.AccountResponse, static response => response.Data, cancellationToken);

    public Task<GeoIpData> LookupGeoIpAsync(string ip, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
        LookupAsync("geoip", ip, idempotencyKey, IPMaxJsonContext.Default.GeoIpResponse, static response => response.Data, cancellationToken);

    public Task<IntelligenceData> LookupIntelligenceAsync(string ip, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
        LookupAsync("intelligence", ip, idempotencyKey, IPMaxJsonContext.Default.IntelligenceResponse, static response => response.Data, cancellationToken);

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    private async Task<T> LookupAsync<TResponse, T>(
        string product,
        string ip,
        string? idempotencyKey,
        JsonTypeInfo<TResponse> typeInfo,
        Func<TResponse, T> data,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(ip);
        var address = ip.Trim();
        if (idempotencyKey is null && _cache is not null && _cache.TryGet<T>(product, address, out var cached))
        {
            return cached;
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(new LookupRequest { Ip = address }, IPMaxJsonContext.Default.LookupRequest);
        var key = idempotencyKey ?? Guid.NewGuid().ToString();
        var result = await SendAsync(HttpMethod.Post, product, body, key, typeInfo, data, cancellationToken).ConfigureAwait(false);
        _cache?.Set(product, address, result);
        return result;
    }

    private async Task<T> SendAsync<TResponse, T>(
        HttpMethod method,
        string path,
        byte[]? body,
        string? idempotencyKey,
        JsonTypeInfo<TResponse> typeInfo,
        Func<TResponse, T> data,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(_apiRoot + path);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var response = await AttemptAsync(method, uri, body, idempotencyKey, typeInfo, cancellationToken).ConfigureAwait(false);
                return data(response);
            }
            catch (IPMaxException error) when (attempt < _maxRetries && IsRetryable(error))
            {
                await Task.Delay(RetryDelay(error, attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<TResponse> AttemptAsync<TResponse>(
        HttpMethod method,
        Uri uri,
        byte[]? body,
        string? idempotencyKey,
        JsonTypeInfo<TResponse> typeInfo,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, uri, body, idempotencyKey);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout);
        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            var content = await response.Content.ReadAsByteArrayAsync(deadline.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw IPMaxApiException.From(ToFailure(response, content));
            }

            return Decode(content, typeInfo);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            var message = string.Create(CultureInfo.InvariantCulture, $"The request to {uri} timed out after {_timeout.TotalSeconds:0.###} s.");
            throw new IPMaxTimeoutException(message, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new IPMaxConnectionException($"Could not reach {uri}.", exception);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, byte[]? body, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        request.Headers.UserAgent.ParseAdd(UserAgent);
        if (_apiKey is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (body is not null)
        {
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType);
        }

        return request;
    }

    private static TResponse Decode<TResponse>(byte[] content, JsonTypeInfo<TResponse> typeInfo)
    {
        try
        {
            return JsonSerializer.Deserialize(content, typeInfo) ?? throw new JsonException("The response body is null.");
        }
        catch (JsonException exception)
        {
            throw new IPMaxException("The IP-Max API returned a response that could not be decoded.", exception);
        }
    }

    private static ApiFailure ToFailure(HttpResponseMessage response, byte[] content)
    {
        var status = response.StatusCode;
        var error = DecodeError(content);
        return new ApiFailure(
            error?.Message ?? $"The IP-Max API responded with HTTP {(int)status}.",
            status,
            error?.Code,
            error?.RequestId ?? Header(response, "X-Request-ID"),
            error?.Retryable ?? IsRetryable(status),
            RetryAfter(response));
    }

    private static ApiError? DecodeError(byte[] content)
    {
        try
        {
            return JsonSerializer.Deserialize(content, IPMaxJsonContext.Default.ErrorResponse)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    private static TimeSpan? RetryAfter(HttpResponseMessage response) =>
        double.TryParse(Header(response, "Retry-After"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds)
        && seconds < TimeSpan.MaxValue.TotalSeconds
            ? TimeSpan.FromSeconds(seconds)
            : null;

    private static bool IsRetryable(IPMaxException error) => error switch
    {
        IPMaxConnectionException or IPMaxTimeoutException => true,
        IPMaxApiException api => IsRetryable(api.StatusCode),
        _ => false,
    };

    private static bool IsRetryable(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static TimeSpan RetryDelay(IPMaxException error, int attempt)
    {
        if (error is IPMaxApiException { RetryAfter: { } retryAfter })
        {
            return retryAfter;
        }

        var ceiling = Math.Min(BackoffCap.TotalMilliseconds, BackoffBase.TotalMilliseconds * Math.Pow(2, attempt));
        return TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * ceiling);
    }
}
