namespace IPMax;

public sealed record IPMaxClientOptions
{
    public Uri BaseAddress { get; init; } = new("https://api.ipm.ax");

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    public int MaxRetries { get; init; } = 2;

    public int CacheSize { get; init; } = 1024;

    public TimeSpan CacheTtl { get; init; } = TimeSpan.FromMinutes(5);
}
