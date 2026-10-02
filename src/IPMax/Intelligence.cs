using System.Text.Json.Serialization;

namespace IPMax;

public sealed record IntelligenceData
{
    public required IntelligenceAs As { get; init; }
    public required LiveIntelligence Intelligence { get; init; }
    public required string Ip { get; init; }
    public NetworkInfo? Network { get; init; }
    public required NetworkClassification NetworkClass { get; init; }
    public MobileCarrier? Mobile { get; init; }
    public ThreatInfo? Threat { get; init; }
    public RpkiInfo? Rpki { get; init; }
    public Company? Company { get; init; }
    public AbuseContact? Abuse { get; init; }
    public DnsRecords? Dns { get; init; }
    public required bool IsBogon { get; init; }
    public required bool? IsAnycast { get; init; }
    public required bool? IsMobile { get; init; }
    public required bool? IsHosting { get; init; }
    public required bool? IsSatellite { get; init; }
}

public sealed record IntelligenceAs
{
    public required string Asn { get; init; }
    public required string Name { get; init; }
    public string? NameEn { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    public required string Domain { get; init; }
    public required string Type { get; init; }
    public string? Class { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public AsnConnectivity? Connectivity { get; init; }
}

public sealed record AsnConnectivity
{
    public required string DeclaredTraffic { get; init; }
    public int? ExchangesTotal { get; init; }
    public required AsnEstimatedCapacity EstimatedCapacity { get; init; }
    public required IReadOnlyList<AsnExchange> Exchanges { get; init; }
}

public sealed record AsnEstimatedCapacity
{
    public required double LowerGbps { get; init; }
    public double? UpperGbps { get; init; }
    public required string Label { get; init; }
}

public sealed record AsnExchange
{
    public required string Name { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    public required AsnExchangeType Type { get; init; }
    public required IReadOnlyList<AsnExchangePort> Ports { get; init; }
}

public sealed record AsnExchangePort
{
    public required double CapacityGbps { get; init; }
    public bool? Operational { get; init; }
    public bool? RsPeer { get; init; }
}

public sealed record NetworkInfo
{
    public required string ConnectionType { get; init; }
    public required string UsageType { get; init; }
    public string? AddressType { get; init; }
    public required string NetSpeed { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
}

public sealed record NetworkClassification
{
    public required NetworkClass Primary { get; init; }
    public required NetworkClassConfidence Confidence { get; init; }
    public required NetworkClassSource Source { get; init; }
    public required IReadOnlyList<NetworkClass> Conflicts { get; init; }
    public required IReadOnlyList<NetworkClassEvidence> Evidence { get; init; }
}

public sealed record NetworkClassEvidence
{
    public required NetworkClass Class { get; init; }
    public required NetworkClassSource Source { get; init; }
    public required NetworkClassScope Scope { get; init; }
    public required string Value { get; init; }
}

public sealed record MobileCarrier
{
    public required string Mcc { get; init; }
    public required string Mnc { get; init; }
    public required string Brand { get; init; }
}

public sealed record ThreatInfo
{
    public required bool? IsTor { get; init; }
    public required bool? IsVpn { get; init; }
    public required bool? IsIcloudRelay { get; init; }
    public required bool? IsCloudflareWarp { get; init; }
    public required bool? IsProxy { get; init; }
    public required bool? IsDatacenter { get; init; }
    public required bool? IsAnonymous { get; init; }
    public required bool? IsKnownAttacker { get; init; }
    public required bool? IsKnownAbuser { get; init; }
    public required bool? IsThreat { get; init; }
    public required bool IsBogon { get; init; }
    public required IReadOnlyList<Blocklist> Blocklists { get; init; }
    public required int BlocklistCount { get; init; }
    public required ThreatScores Scores { get; init; }
    public IReadOnlyList<AnonymizerExemptionReason>? AnonymizerExemptions { get; init; }
}

public sealed record Blocklist
{
    public required string Name { get; init; }
    public required string Site { get; init; }
    public required string Type { get; init; }
}

public sealed record ThreatScores
{
    public required double? VpnScore { get; init; }
    public required double? ProxyScore { get; init; }
    public required double? ThreatScore { get; init; }
    public required double? TrustScore { get; init; }
}

public sealed record RpkiInfo
{
    public required RpkiStatus Status { get; init; }
    public RpkiSource? Source { get; init; }
    public string? RouteSource { get; init; }
    public string? OriginAsn { get; init; }
    public string? RoutePrefix { get; init; }
    public string? RoaPrefix { get; init; }
    public string? RoaAsn { get; init; }
    public int? MaxLength { get; init; }
    public string? Ta { get; init; }
    public RpkiRtr? Rtr { get; init; }
    public RpkiValidator? Validator { get; init; }
}

public sealed record RpkiRtr
{
    public required RpkiRtrSource Source { get; init; }
    public string? Server { get; init; }

    [JsonPropertyName("lastUpdated")]
    public required long LastUpdated { get; init; }
}

public sealed record RpkiValidator
{
    public required string Source { get; init; }
    public required string Engine { get; init; }

    [JsonPropertyName("lastUpdated")]
    public required long LastUpdated { get; init; }
}

public sealed record Company
{
    public required string? Name { get; init; }
    public required string? Address { get; init; }
    public required string? Domain { get; init; }
    public required string? Phone { get; init; }
    public required string? Type { get; init; }
    public required string? Network { get; init; }
    public required string? Country { get; init; }
    public string? Source { get; init; }
}

public sealed record AbuseContact
{
    public required string? Name { get; init; }
    public required string? Address { get; init; }
    public required string? Country { get; init; }
    public required string? Email { get; init; }
    public required string? Network { get; init; }
    public required string? Phone { get; init; }
    public string? Source { get; init; }
}

public sealed record DnsRecords
{
    public required IReadOnlyList<string> Ptr { get; init; }
    public required IReadOnlyList<DnsPtrEvidence> PtrEvidence { get; init; }
    public required IReadOnlyList<DnsHostnameEvidence> Hostnames { get; init; }
    public required IReadOnlyList<string> Sources { get; init; }
    public required string? SnapshotId { get; init; }
    public required string? UpdatedAt { get; init; }
}

public sealed record DnsPtrEvidence
{
    public required string Hostname { get; init; }
    public required IReadOnlyList<string> Sources { get; init; }
    public required bool ForwardConfirmed { get; init; }
    public required string? FirstSeen { get; init; }
    public required string? LastSeen { get; init; }
}

public sealed record DnsHostnameEvidence
{
    public required string Hostname { get; init; }
    public required IReadOnlyList<DnsHostnameKind> Kinds { get; init; }
    public required IReadOnlyList<string> Sources { get; init; }
    public required bool ForwardConfirmed { get; init; }
    public required string? FirstSeen { get; init; }
    public required string? LastSeen { get; init; }
}

public sealed record LiveIntelligence
{
    public required IReadOnlyList<PtrIntelligence> Ptr { get; init; }
    public required IReadOnlyList<string> Nameservers { get; init; }
    public required IReadOnlyList<CpeObservation> Cpes { get; init; }
    public required IReadOnlyList<LiveIntelligenceTag> Tags { get; init; }
    public required IReadOnlyList<RevokedNetworkTag> RevokedTags { get; init; }
    public required string? UsageType { get; init; }
    public RpkiInfo? Rpki { get; init; }
    public AbuseReports? Abuse { get; init; }
    public IReadOnlyList<ResourceTransfer>? Transfers { get; init; }
    public SegmentProbe? SegmentProbe { get; init; }
}

public sealed record CpeObservation
{
    public required string Cpe { get; init; }
    public required CpePart Part { get; init; }
    public required string Vendor { get; init; }
    public required string Product { get; init; }
    public required string? Version { get; init; }
}

public sealed record AbuseReports
{
    public required int ConfidenceScore { get; init; }
    public required int TotalReports { get; init; }
    public required int DistinctReporters { get; init; }
    public required string? LastReportedAt { get; init; }
    public required bool? IsWhitelisted { get; init; }
    public required IReadOnlyList<AbuseCategoryCount> Categories { get; init; }
}

public sealed record AbuseCategoryCount
{
    public required int Id { get; init; }
    public required int Count { get; init; }
}

public sealed record ResourceTransfer
{
    public required string TransferTime { get; init; }
    public required string TransferType { get; init; }
    public required string SourceResource { get; init; }
    public required string RecipientResource { get; init; }
    public required string SourceRegistry { get; init; }
    public required string RecipientRegistry { get; init; }
    public required string SourceHolder { get; init; }
    public required string RecipientHolder { get; init; }
}

public sealed record SegmentProbe
{
    public required string Ip { get; init; }
    public required IReadOnlyList<int> Ports { get; init; }
    public required IReadOnlyList<LiveIntelligenceTag> Tags { get; init; }
}
