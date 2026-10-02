# IPMax

.NET client for the [IP-Max](https://ipm.ax) GeoIP and IP intelligence API. Targets .NET 8 and .NET 10, has no dependencies beyond the BCL, and is trimming and Native AOT friendly.

## Install

```sh
dotnet add package IPMax
```

## Quickstart

```csharp
using IPMax;

using var ipmax = new IPMaxClient(Environment.GetEnvironmentVariable("IPMAX_API_KEY"));

var geo = await ipmax.LookupGeoIpAsync("8.8.8.8");
Console.WriteLine($"{geo.Geo.City} {geo.TimeZone?.Name}");

var intel = await ipmax.LookupIntelligenceAsync("8.8.8.8");
Console.WriteLine($"{intel.NetworkClass.Primary} {intel.Threat?.IsVpn}");

foreach (var ptr in intel.Intelligence.Ptr)
{
    if (ptr is PtrLocatedIntelligence located)
    {
        Console.WriteLine($"{located.Hostname} is in {located.Location.City}");
    }
}

var catalog = await ipmax.GetCatalogAsync();
var account = await ipmax.GetAccountAsync();
```

Every method accepts a `CancellationToken`, and lookups accept your own idempotency key:

```csharp
await ipmax.LookupGeoIpAsync("8.8.8.8", idempotencyKey: "order-1042-geoip", cancellationToken: token);
```

To bring your own `HttpClient` (proxies, handlers, `IHttpClientFactory`), pass it first. The client never disposes an `HttpClient` it did not create.

```csharp
var ipmax = new IPMaxClient(httpClient, apiKey, new IPMaxClientOptions { MaxRetries = 4 });
```

## Configuration

| Option        | Default              | Description                                                             |
| ------------- | -------------------- | ----------------------------------------------------------------------- |
| `apiKey`      | `IPMAX_API_KEY` env  | API key sent as a bearer token. `GetCatalogAsync` works without one.    |
| `BaseAddress` | `https://api.ipm.ax` | API origin.                                                             |
| `Timeout`     | 10 s                 | Per-attempt timeout.                                                    |
| `MaxRetries`  | `2`                  | Retries on network errors, timeouts, 408, 429 and 5xx; honours `Retry-After`. |
| `CacheSize`   | `1024`               | Successful lookups kept in memory; `0` disables the cache.              |
| `CacheTtl`    | 5 min                | Cache lifetime.                                                         |
| `HttpClient`  | a private instance   | Constructor overload for custom transports.                             |

Each lookup carries a fresh UUID v4 `Idempotency-Key` that is reused on every retry, so a retried request is never billed twice. Cached lookups make no request and cost nothing; passing an explicit `idempotencyKey` skips the cache read. The client is thread-safe and meant to be reused.

String enums such as `NetworkClass` and `RpkiStatus` are open: unknown values from newer servers decode without error and keep their raw `Value`, so compare against the known members (`NetworkClass.Hosting`) rather than switching exhaustively.

## Error handling

```csharp
try
{
    await ipmax.LookupGeoIpAsync("192.0.2.1");
}
catch (IPMaxNotFoundException)
{
    Console.WriteLine("not in the database");
}
catch (IPMaxInsufficientBalanceException)
{
    Console.WriteLine("top up the geoip wallet");
}
catch (IPMaxRateLimitException error)
{
    Console.WriteLine($"retry in {error.RetryAfter}");
}
catch (IPMaxApiException error)
{
    Console.WriteLine($"{error.StatusCode} {error.Code} {error.Message} {error.RequestId}");
}
```

| Exception                           | When                                   |
| ----------------------------------- | -------------------------------------- |
| `IPMaxException`                    | Base class of every exception below    |
| `IPMaxApiException`                 | Any non-2xx response                   |
| `IPMaxInvalidRequestException`      | 400, 413, 415                          |
| `IPMaxAuthenticationException`      | 401                                    |
| `IPMaxInsufficientBalanceException` | 402                                    |
| `IPMaxNotFoundException`            | 404                                    |
| `IPMaxConflictException`            | 409                                    |
| `IPMaxRateLimitException`           | 429                                    |
| `IPMaxServerException`              | 5xx                                    |
| `IPMaxConnectionException`          | The request never got a response       |
| `IPMaxTimeoutException`             | An attempt exceeded `Timeout`          |

`IPMaxApiException` exposes `StatusCode`, `Code` (an `ErrorCode` such as `ErrorCode.IpNotFound`), `Message`, `RequestId`, `Retryable` and `RetryAfter`. Cancelling your own token throws `OperationCanceledException` as usual.

## License

[Apache-2.0](LICENSE)
