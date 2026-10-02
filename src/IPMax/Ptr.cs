using System.Text.Json.Serialization;

namespace IPMax;

[JsonConverter(typeof(PtrIntelligenceConverter))]
public abstract record PtrIntelligence
{
    private protected PtrIntelligence()
    {
    }

    public required string Hostname { get; init; }

    public abstract string Status { get; }
}

public sealed record PtrLocatedIntelligence : PtrIntelligence
{
    public override string Status => "located";

    public required PtrOperator Operator { get; init; }
    public required PtrHint Hint { get; init; }
    public required PtrMetroLocation Location { get; init; }
    public required PtrConfidence Confidence { get; init; }
    public required PtrEvidence Evidence { get; init; }
}

public sealed record PtrAsnMismatchIntelligence : PtrIntelligence
{
    public override string Status => "asn_mismatch";

    public required PtrOperator Operator { get; init; }
    public required string ObservedAsn { get; init; }
    public required PtrEvidence Evidence { get; init; }
}

public sealed record PtrUnmatchedIntelligence : PtrIntelligence
{
    public override string Status => "unmatched";
}

public sealed record PtrUnknownIntelligence(string Status) : PtrIntelligence
{
    public override string Status { get; } = Status;
}

public sealed record PtrOperator
{
    public required string Name { get; init; }
    public required string Asn { get; init; }
    public required string Suffix { get; init; }
}

public sealed record PtrHint
{
    public required string Value { get; init; }
    public required PtrHintKind Kind { get; init; }
}

public sealed record PtrMetroLocation
{
    public required string City { get; init; }
    public required string? Region { get; init; }
    public required string? RegionCode { get; init; }
    public required string Country { get; init; }
    public required string CountryCode { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required string Precision { get; init; }
}

public sealed record PtrConfidence
{
    public required PtrConfidenceLevel Level { get; init; }
    public required double Score { get; init; }
}

public sealed record PtrEvidence
{
    public required string RuleId { get; init; }
    public required string RulesetVersion { get; init; }
    public required IReadOnlyList<PtrEvidenceSource> Sources { get; init; }
}

public sealed record PtrEvidenceSource
{
    public required PtrDatasetName Dataset { get; init; }
    public required string Version { get; init; }
}
