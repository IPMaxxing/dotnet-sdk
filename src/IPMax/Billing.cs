using System.Text.Json.Serialization;

namespace IPMax;

public sealed record Catalog
{
    public required string Currency { get; init; }
    public required IReadOnlyList<Price> Prices { get; init; }

    [JsonPropertyName("purchaseEmail")]
    public required string PurchaseEmail { get; init; }

    public required bool Available { get; init; }
}

public sealed record Price
{
    public required Product Product { get; init; }
    public required string Name { get; init; }

    [JsonPropertyName("unitMicros")]
    public required double UnitMicros { get; init; }
}

public sealed record Account
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    [JsonPropertyName("keyPrefix")]
    public required string KeyPrefix { get; init; }

    public required IReadOnlyList<Wallet> Wallets { get; init; }
    public required IReadOnlyList<LedgerEntry> Entries { get; init; }
}

public sealed record Wallet
{
    public required Product Product { get; init; }

    [JsonPropertyName("balanceMicros")]
    public required double BalanceMicros { get; init; }
}

public sealed record LedgerEntry
{
    public required string Id { get; init; }
    public required Product Product { get; init; }
    public required LedgerEntryKind Kind { get; init; }

    [JsonPropertyName("amountMicros")]
    public required double AmountMicros { get; init; }

    [JsonPropertyName("balanceMicros")]
    public required double BalanceMicros { get; init; }

    public required string Reference { get; init; }

    [JsonPropertyName("createdAt")]
    public required string CreatedAt { get; init; }
}
