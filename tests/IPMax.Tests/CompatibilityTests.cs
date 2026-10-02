using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IPMax.Tests;

public sealed class CompatibilityTests
{
    [Fact]
    public async Task UnknownEnumValuesAndFieldsDecode()
    {
        var fixture = Fixture.Load("intelligence-rich");
        var data = fixture.Data;
        data["brand_new_field"] = new JsonObject { ["nested"] = 1 };
        data["network_class"]!["primary"] = "quantum_link";
        data["network_class"]!["confidence"] = "certain";
        data["network_class"]!["source"] = "oracle";
        data["network_class"]!["conflicts"] = new JsonArray("satellite", "telepathy");
        data["network_class"]!["evidence"]![0]!["scope"] = "planet";
        data["rpki"]!["status"] = "mystery";
        data["rpki"]!["source"] = "Carrier Pigeon";
        data["rpki"]!["rtr"]!["source"] = "NEWRTR";
        data["threat"]!["anonymizer_exemptions"] = new JsonArray("cloudflare_cdn_origin", "future_reason");
        data["as"]!["connectivity"]!["exchanges"]![0]!["type"] = "orbital";
        var live = data["intelligence"]!;
        live["tags"] = new JsonArray("public_dns_resolver", "quantum_relay");
        live["revoked_tags"] = new JsonArray("satellite_isp");
        live["cpes"]![0]!["part"] = "firmware";
        live["segment_probe"]!["tags"] = new JsonArray("honeypot");
        var located = live["ptr"]![0]!;
        located["hint"]!["kind"] = "geohash";
        located["confidence"]!["level"] = "certain";
        located["evidence"]!["sources"]![0]!["dataset"] = "newdb";
        located["location"]!["altitude"] = 40;
        live["ptr"]!.AsArray().Add(new JsonObject { ["hostname"] = "edge.example", ["status"] = "teleported", ["via"] = true });
        using var harness = new Harness([Replies.From(fixture)]);

        var result = await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal(new NetworkClass("quantum_link"), result.NetworkClass.Primary);
        Assert.NotEqual(NetworkClass.Hosting, result.NetworkClass.Primary);
        Assert.Equal("certain", result.NetworkClass.Confidence.Value);
        Assert.Equal("oracle", result.NetworkClass.Source.Value);
        Assert.Equal([NetworkClass.Satellite, new NetworkClass("telepathy")], result.NetworkClass.Conflicts);
        Assert.Equal("planet", result.NetworkClass.Evidence[0].Scope.Value);
        Assert.Equal("mystery", result.Rpki!.Status.Value);
        Assert.Equal("Carrier Pigeon", result.Rpki.Source!.Value.Value);
        Assert.Equal("NEWRTR", result.Rpki.Rtr!.Source.Value);
        Assert.Equal([AnonymizerExemptionReason.CloudflareCdnOrigin, new AnonymizerExemptionReason("future_reason")], result.Threat!.AnonymizerExemptions!);
        Assert.Equal("orbital", result.As.Connectivity!.Exchanges[0].Type.Value);
        Assert.Equal([LiveIntelligenceTag.PublicDnsResolver, new LiveIntelligenceTag("quantum_relay")], result.Intelligence.Tags);
        Assert.Equal("satellite_isp", Assert.Single(result.Intelligence.RevokedTags).Value);
        Assert.Equal("firmware", Assert.Single(result.Intelligence.Cpes).Part.Value);
        Assert.Equal("honeypot", Assert.Single(result.Intelligence.SegmentProbe!.Tags).Value);

        var ptr = Assert.IsType<PtrLocatedIntelligence>(result.Intelligence.Ptr[0]);
        Assert.Equal("geohash", ptr.Hint.Kind.Value);
        Assert.Equal("certain", ptr.Confidence.Level.Value);
        Assert.Equal("newdb", Assert.Single(ptr.Evidence.Sources).Dataset.Value);

        var unknown = Assert.IsType<PtrUnknownIntelligence>(result.Intelligence.Ptr[^1]);
        Assert.Equal("teleported", unknown.Status);
        Assert.Equal("edge.example", unknown.Hostname);
    }

    [Fact]
    public async Task UnknownProductDecodes()
    {
        var fixture = Fixture.Load("catalog");
        fixture.Data["prices"]![0]!["product"] = "satellite";
        fixture.Data["discount"] = 0.1;
        using var harness = new Harness([Replies.From(fixture)]);

        var catalog = await harness.Client.GetCatalogAsync(Ct);

        Assert.Equal(new Product("satellite"), catalog.Prices[0].Product);
        Assert.Equal("satellite", catalog.Prices[0].Product.ToString());
    }

    [Fact]
    public async Task UnknownErrorCodeIsKeptAsAnOpenValue()
    {
        using var harness = new Harness([Replies.Error(HttpStatusCode.BadRequest, 1999, false)], Harness.Options with { MaxRetries = 0 });

        var error = await Assert.ThrowsAsync<IPMaxInvalidRequestException>(() => harness.Client.GetCatalogAsync(Ct));

        Assert.Equal(new ErrorCode(1999), error.Code);
        Assert.Equal("1999", error.Code.ToString());
        Assert.Equal(ErrorCode.InvalidRequest, new ErrorCode(1000));
    }

    [Fact]
    public async Task PtrVariantsSerializeWithTheirStatus()
    {
        using var harness = new Harness([Replies.Fixture("intelligence-rich")]);
        var result = await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);

        var json = JsonSerializer.Serialize(result.Intelligence.Ptr[0]);

        var node = JsonNode.Parse(json)!;
        Assert.Equal("located", node["status"]!.GetValue<string>());
        Assert.Equal("ae-1.r01.tokyjp05.jp.bb.gin.ntt.net", node["hostname"]!.GetValue<string>());
        Assert.Equal("clli", node["hint"]!["kind"]!.GetValue<string>());
    }
}
