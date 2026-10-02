using System.Text.Json.Serialization;

namespace IPMax;

public sealed record GeoIpData
{
    public required GeoIpAs As { get; init; }
    public required string Ip { get; init; }
    public required Geolocation Geo { get; init; }
    public required string Netmask { get; init; }
    public required int? PrefixLength { get; init; }
    public required bool IsBogon { get; init; }
    public Currency? Currency { get; init; }
    public TimeZone? TimeZone { get; init; }
    public string? CallingCode { get; init; }
}

public sealed record GeoIpAs
{
    public required string Asn { get; init; }
    public required string Name { get; init; }
    public string? NameEn { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    public required string Domain { get; init; }
}

public sealed record Geolocation
{
    public required string City { get; init; }
    public required string Region { get; init; }
    public required string District { get; init; }
    public required string RegionCode { get; init; }
    public required string Adcode { get; init; }
    public required string Country { get; init; }
    public required string CountryCode { get; init; }
    public required string Continent { get; init; }
    public required string ContinentCode { get; init; }
    public required double? Latitude { get; init; }
    public required double? Longitude { get; init; }
    public required string Timezone { get; init; }
    public required string PostalCode { get; init; }
    public required double? Radius { get; init; }
    public string? AreaCode { get; init; }
    public string? PlusCode { get; init; }
    public string? CountryEn { get; init; }
    public string? CountryZh { get; init; }
    public string? RegionEn { get; init; }
    public string? RegionZh { get; init; }
    public string? CityEn { get; init; }
    public string? CityZh { get; init; }
    public string? DistrictEn { get; init; }
    public string? DistrictZh { get; init; }
}

public sealed record Currency
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string Symbol { get; init; }
    public required string Native { get; init; }
    public required string Plural { get; init; }
}

public sealed record TimeZone
{
    public required string Name { get; init; }
    public required string Abbr { get; init; }
    public required string Offset { get; init; }
    public required bool IsDst { get; init; }
    public required string CurrentTime { get; init; }
}
