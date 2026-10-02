namespace IPMax;

public sealed record LookupRequest
{
    public required string Ip { get; init; }
}

public sealed record CatalogResponse
{
    public required bool Status { get; init; }
    public required Catalog Data { get; init; }
}

public sealed record AccountResponse
{
    public required bool Status { get; init; }
    public required Account Data { get; init; }
}

public sealed record GeoIpResponse
{
    public required bool Status { get; init; }
    public required GeoIpData Data { get; init; }
}

public sealed record IntelligenceResponse
{
    public required bool Status { get; init; }
    public required IntelligenceData Data { get; init; }
}

public sealed record ErrorResponse
{
    public required bool Status { get; init; }
    public required ApiError Error { get; init; }
}

public sealed record ApiError
{
    public required ErrorCode Code { get; init; }
    public required string Message { get; init; }
    public required string RequestId { get; init; }
    public required bool Retryable { get; init; }
}
