using System.Globalization;
using System.Text.Json.Serialization;

namespace IPMax;

[JsonConverter(typeof(ErrorCodeConverter))]
public readonly record struct ErrorCode(int Value)
{
    public static ErrorCode InvalidRequest { get; } = new(1000);
    public static ErrorCode InvalidPayload { get; } = new(1001);
    public static ErrorCode InvalidIp { get; } = new(1002);
    public static ErrorCode IpNotFound { get; } = new(1003);
    public static ErrorCode RouteNotFound { get; } = new(1004);
    public static ErrorCode InvalidApiKey { get; } = new(1010);
    public static ErrorCode RateLimitExceeded { get; } = new(1011);
    public static ErrorCode ServiceUnavailable { get; } = new(1400);
    public static ErrorCode InternalError { get; } = new(1401);
    public static ErrorCode InsufficientBalance { get; } = new(1500);
    public static ErrorCode IdempotencyConflict { get; } = new(1501);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

[JsonConverter(typeof(StringEnumConverter<Product>))]
public readonly record struct Product(string Value) : IStringEnum<Product>
{
    public static Product GeoIp { get; } = new("geoip");
    public static Product Intelligence { get; } = new("intelligence");

    public override string ToString() => Value;

    static Product IStringEnum<Product>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<LedgerEntryKind>))]
public readonly record struct LedgerEntryKind(string Value) : IStringEnum<LedgerEntryKind>
{
    public static LedgerEntryKind Credit { get; } = new("credit");
    public static LedgerEntryKind Charge { get; } = new("charge");
    public static LedgerEntryKind Adjustment { get; } = new("adjustment");

    public override string ToString() => Value;

    static LedgerEntryKind IStringEnum<LedgerEntryKind>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<AnonymizerExemptionReason>))]
public readonly record struct AnonymizerExemptionReason(string Value) : IStringEnum<AnonymizerExemptionReason>
{
    public static AnonymizerExemptionReason CloudflareCdnOrigin { get; } = new("cloudflare_cdn_origin");
    public static AnonymizerExemptionReason SearchEngineCrawler { get; } = new("search_engine_crawler");

    public override string ToString() => Value;

    static AnonymizerExemptionReason IStringEnum<AnonymizerExemptionReason>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<AsnExchangeType>))]
public readonly record struct AsnExchangeType(string Value) : IStringEnum<AsnExchangeType>
{
    public static AsnExchangeType Physical { get; } = new("physical");
    public static AsnExchangeType Virtual { get; } = new("virtual");

    public override string ToString() => Value;

    static AsnExchangeType IStringEnum<AsnExchangeType>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<CpePart>))]
public readonly record struct CpePart(string Value) : IStringEnum<CpePart>
{
    public static CpePart Application { get; } = new("application");
    public static CpePart OperatingSystem { get; } = new("operating_system");
    public static CpePart Hardware { get; } = new("hardware");

    public override string ToString() => Value;

    static CpePart IStringEnum<CpePart>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<DnsHostnameKind>))]
public readonly record struct DnsHostnameKind(string Value) : IStringEnum<DnsHostnameKind>
{
    public static DnsHostnameKind Fdns { get; } = new("fdns");
    public static DnsHostnameKind Ct { get; } = new("ct");

    public override string ToString() => Value;

    static DnsHostnameKind IStringEnum<DnsHostnameKind>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<LiveIntelligenceTag>))]
public readonly record struct LiveIntelligenceTag(string Value) : IStringEnum<LiveIntelligenceTag>
{
    public static LiveIntelligenceTag Hosting { get; } = new("hosting");
    public static LiveIntelligenceTag PublicDnsResolver { get; } = new("public_dns_resolver");
    public static LiveIntelligenceTag WebServer { get; } = new("web_server");
    public static LiveIntelligenceTag Vpn { get; } = new("vpn");
    public static LiveIntelligenceTag ResidentialProxy { get; } = new("residential_proxy");

    public override string ToString() => Value;

    static LiveIntelligenceTag IStringEnum<LiveIntelligenceTag>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<NetworkClass>))]
public readonly record struct NetworkClass(string Value) : IStringEnum<NetworkClass>
{
    public static NetworkClass Mobile { get; } = new("mobile");
    public static NetworkClass Hosting { get; } = new("hosting");
    public static NetworkClass Residential { get; } = new("residential");
    public static NetworkClass Business { get; } = new("business");
    public static NetworkClass Organization { get; } = new("organization");
    public static NetworkClass Government { get; } = new("government");
    public static NetworkClass Military { get; } = new("military");
    public static NetworkClass Education { get; } = new("education");
    public static NetworkClass Library { get; } = new("library");
    public static NetworkClass Dedicated { get; } = new("dedicated");
    public static NetworkClass Satellite { get; } = new("satellite");
    public static NetworkClass Ixp { get; } = new("ixp");
    public static NetworkClass SearchEngine { get; } = new("search_engine");
    public static NetworkClass AiCrawler { get; } = new("ai_crawler");
    public static NetworkClass Unknown { get; } = new("unknown");

    public override string ToString() => Value;

    static NetworkClass IStringEnum<NetworkClass>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<NetworkClassConfidence>))]
public readonly record struct NetworkClassConfidence(string Value) : IStringEnum<NetworkClassConfidence>
{
    public static NetworkClassConfidence High { get; } = new("high");
    public static NetworkClassConfidence Medium { get; } = new("medium");
    public static NetworkClassConfidence Low { get; } = new("low");
    public static NetworkClassConfidence Unknown { get; } = new("unknown");

    public override string ToString() => Value;

    static NetworkClassConfidence IStringEnum<NetworkClassConfidence>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<NetworkClassSource>))]
public readonly record struct NetworkClassSource(string Value) : IStringEnum<NetworkClassSource>
{
    public static NetworkClassSource DomesticIsp { get; } = new("domestic_isp");
    public static NetworkClassSource UsageType { get; } = new("usage_type");
    public static NetworkClassSource MobileCarrier { get; } = new("mobile_carrier");
    public static NetworkClassSource ConnectionType { get; } = new("connection_type");
    public static NetworkClassSource NetworkTag { get; } = new("network_tag");
    public static NetworkClassSource ThreatIntelligence { get; } = new("threat_intelligence");
    public static NetworkClassSource AsnTag { get; } = new("asn_tag");
    public static NetworkClassSource Ixp { get; } = new("ixp");
    public static NetworkClassSource None { get; } = new("none");

    public override string ToString() => Value;

    static NetworkClassSource IStringEnum<NetworkClassSource>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<NetworkClassScope>))]
public readonly record struct NetworkClassScope(string Value) : IStringEnum<NetworkClassScope>
{
    public static NetworkClassScope Ip { get; } = new("ip");
    public static NetworkClassScope Prefix { get; } = new("prefix");
    public static NetworkClassScope Asn { get; } = new("asn");

    public override string ToString() => Value;

    static NetworkClassScope IStringEnum<NetworkClassScope>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<PtrConfidenceLevel>))]
public readonly record struct PtrConfidenceLevel(string Value) : IStringEnum<PtrConfidenceLevel>
{
    public static PtrConfidenceLevel High { get; } = new("high");
    public static PtrConfidenceLevel Medium { get; } = new("medium");
    public static PtrConfidenceLevel Low { get; } = new("low");

    public override string ToString() => Value;

    static PtrConfidenceLevel IStringEnum<PtrConfidenceLevel>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<PtrDatasetName>))]
public readonly record struct PtrDatasetName(string Value) : IStringEnum<PtrDatasetName>
{
    public static PtrDatasetName Geonames { get; } = new("geonames");
    public static PtrDatasetName OurAirports { get; } = new("ourairports");
    public static PtrDatasetName OperatorOverrides { get; } = new("operator-overrides");
    public static PtrDatasetName Unlocode { get; } = new("unlocode");

    public override string ToString() => Value;

    static PtrDatasetName IStringEnum<PtrDatasetName>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<PtrHintKind>))]
public readonly record struct PtrHintKind(string Value) : IStringEnum<PtrHintKind>
{
    public static PtrHintKind Iata { get; } = new("iata");
    public static PtrHintKind Unlocode { get; } = new("unlocode");
    public static PtrHintKind Clli { get; } = new("clli");
    public static PtrHintKind CityName { get; } = new("city_name");
    public static PtrHintKind OperatorCode { get; } = new("operator_code");

    public override string ToString() => Value;

    static PtrHintKind IStringEnum<PtrHintKind>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<RevokedNetworkTag>))]
public readonly record struct RevokedNetworkTag(string Value) : IStringEnum<RevokedNetworkTag>
{
    public static RevokedNetworkTag BusinessBroadband { get; } = new("business_broadband");
    public static RevokedNetworkTag HomeIsp { get; } = new("home_isp");
    public static RevokedNetworkTag MobileIsp { get; } = new("mobile_isp");

    public override string ToString() => Value;

    static RevokedNetworkTag IStringEnum<RevokedNetworkTag>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<RpkiSource>))]
public readonly record struct RpkiSource(string Value) : IStringEnum<RpkiSource>
{
    public static RpkiSource RtrLive { get; } = new("RTR Live");
    public static RpkiSource LocalCache { get; } = new("Local Cache");
    public static RpkiSource LocalDb { get; } = new("Local DB");

    public override string ToString() => Value;

    static RpkiSource IStringEnum<RpkiSource>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<RpkiStatus>))]
public readonly record struct RpkiStatus(string Value) : IStringEnum<RpkiStatus>
{
    public static RpkiStatus Valid { get; } = new("valid");
    public static RpkiStatus InvalidAsn { get; } = new("invalid_asn");
    public static RpkiStatus InvalidLength { get; } = new("invalid_length");
    public static RpkiStatus Unknown { get; } = new("unknown");
    public static RpkiStatus NotRouted { get; } = new("not_routed");

    public override string ToString() => Value;

    static RpkiStatus IStringEnum<RpkiStatus>.From(string value) => new(value);
}

[JsonConverter(typeof(StringEnumConverter<RpkiRtrSource>))]
public readonly record struct RpkiRtrSource(string Value) : IStringEnum<RpkiRtrSource>
{
    public static RpkiRtrSource Cloudflare { get; } = new("Cloudflare");
    public static RpkiRtrSource Mfeed { get; } = new("MFEED");
    public static RpkiRtrSource Rtr { get; } = new("RTR");

    public override string ToString() => Value;

    static RpkiRtrSource IStringEnum<RpkiRtrSource>.From(string value) => new(value);
}
