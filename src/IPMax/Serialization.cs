using System.Text.Json;
using System.Text.Json.Serialization;

namespace IPMax;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CatalogResponse))]
[JsonSerializable(typeof(AccountResponse))]
[JsonSerializable(typeof(GeoIpResponse))]
[JsonSerializable(typeof(IntelligenceResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(LookupRequest))]
[JsonSerializable(typeof(PtrLocatedIntelligence))]
[JsonSerializable(typeof(PtrAsnMismatchIntelligence))]
[JsonSerializable(typeof(PtrUnmatchedIntelligence))]
[JsonSerializable(typeof(PtrUnknownIntelligence))]
internal sealed partial class IPMaxJsonContext : JsonSerializerContext;

internal interface IStringEnum<TSelf>
    where TSelf : struct, IStringEnum<TSelf>
{
    string Value { get; }

    static abstract TSelf From(string value);
}

internal sealed class StringEnumConverter<T> : JsonConverter<T>
    where T : struct, IStringEnum<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? T.From(reader.GetString()!)
            : throw new JsonException($"Expected a string for {typeof(T).Name}.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

internal sealed class ErrorCodeConverter : JsonConverter<ErrorCode>
{
    public override ErrorCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, ErrorCode value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}

internal sealed class PtrIntelligenceConverter : JsonConverter<PtrIntelligence>
{
    public override PtrIntelligence Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var element = document.RootElement;
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Expected an object for PtrIntelligence.");
        }

        var status = element.TryGetProperty("status", out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
        var context = IPMaxJsonContext.Default;
        PtrIntelligence? value = status switch
        {
            "located" => element.Deserialize(context.PtrLocatedIntelligence),
            "asn_mismatch" => element.Deserialize(context.PtrAsnMismatchIntelligence),
            "unmatched" => element.Deserialize(context.PtrUnmatchedIntelligence),
            _ => element.Deserialize(context.PtrUnknownIntelligence),
        };
        return value ?? throw new JsonException("Expected an object for PtrIntelligence.");
    }

    public override void Write(Utf8JsonWriter writer, PtrIntelligence value, JsonSerializerOptions options)
    {
        var context = IPMaxJsonContext.Default;
        switch (value)
        {
            case PtrLocatedIntelligence located:
                JsonSerializer.Serialize(writer, located, context.PtrLocatedIntelligence);
                break;
            case PtrAsnMismatchIntelligence mismatch:
                JsonSerializer.Serialize(writer, mismatch, context.PtrAsnMismatchIntelligence);
                break;
            case PtrUnmatchedIntelligence unmatched:
                JsonSerializer.Serialize(writer, unmatched, context.PtrUnmatchedIntelligence);
                break;
            case PtrUnknownIntelligence unknown:
                JsonSerializer.Serialize(writer, unknown, context.PtrUnknownIntelligence);
                break;
            default:
                throw new JsonException($"Cannot serialize {value.GetType().Name}.");
        }
    }
}
