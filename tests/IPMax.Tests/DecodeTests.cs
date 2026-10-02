using System.Text.Json.Nodes;

namespace IPMax.Tests;

public sealed class DecodeTests
{
    public static TheoryData<string> FixtureNames => [.. Fixture.Names];

    [Fact]
    public void EveryFixtureIsPresent()
    {
        string[] expected =
        [
            "account",
            "catalog",
            "error-invalid-request",
            "error-not-found",
            "error-unauthorized",
            "geoip-10-1-2-3",
            "geoip-240e-390-a1-3cd0-be24-11ff-fe46-aca3",
            "geoip-8-8-8-8",
            "intelligence-10-1-2-3",
            "intelligence-240e-390-a1-3cd0-be24-11ff-fe46-aca3",
            "intelligence-8-8-8-8",
            "intelligence-rich",
        ];
        Assert.Equal(expected, Fixture.Names);
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public async Task EveryFixtureReplays(string name)
    {
        var fixture = Fixture.Load(name);
        using var harness = new Harness([Replies.From(fixture)], Harness.Options with { MaxRetries = 0 });

        if (name.StartsWith("error-", StringComparison.Ordinal))
        {
            var error = fixture.Body["error"]!;
            var exception = await Assert.ThrowsAnyAsync<IPMaxApiException>(
                () => harness.Client.LookupGeoIpAsync("192.0.2.1", cancellationToken: Ct));
            Assert.Equal(fixture.Status, exception.StatusCode);
            Assert.Equal(new ErrorCode(error["code"]!.GetValue<int>()), exception.Code);
            Assert.Equal(error["message"]!.GetValue<string>(), exception.Message);
            Assert.Equal(error["request_id"]!.GetValue<string>(), exception.RequestId);
            Assert.Equal(error["retryable"]!.GetValue<bool>(), exception.Retryable);
            return;
        }

        var ip = fixture.Data["ip"]?.GetValue<string>() ?? "";
        object result = name switch
        {
            "catalog" => await harness.Client.GetCatalogAsync(Ct),
            "account" => await harness.Client.GetAccountAsync(Ct),
            _ when name.StartsWith("geoip-", StringComparison.Ordinal) => await harness.Client.LookupGeoIpAsync(ip, cancellationToken: Ct),
            _ => await harness.Client.LookupIntelligenceAsync(ip, cancellationToken: Ct),
        };

        var expectedType = name switch
        {
            "catalog" => typeof(Catalog),
            "account" => typeof(Account),
            _ when name.StartsWith("geoip-", StringComparison.Ordinal) => typeof(GeoIpData),
            _ => typeof(IntelligenceData),
        };
        Assert.IsType(expectedType, result);
    }

    [Fact]
    public async Task CatalogKeepsFractionalMicros()
    {
        using var harness = new Harness([Replies.Fixture("catalog")]);

        var catalog = await harness.Client.GetCatalogAsync(Ct);

        Assert.Equal("CNY", catalog.Currency);
        Assert.True(catalog.Available);
        Assert.Equal("sales@ipmax.example", catalog.PurchaseEmail);
        Assert.Collection(
            catalog.Prices,
            price =>
            {
                Assert.Equal(Product.GeoIp, price.Product);
                Assert.Equal("GeoIP 定位", price.Name);
                Assert.Equal(437.5, price.UnitMicros);
            },
            price =>
            {
                Assert.Equal(Product.Intelligence, price.Product);
                Assert.Equal(1920, price.UnitMicros);
            });
    }

    [Fact]
    public async Task AccountDecodesWalletsAndLedger()
    {
        var fixture = Fixture.Load("account");
        fixture.Data["wallets"]![0]!["balanceMicros"] = 437.5;
        fixture.Data["entries"] = new JsonArray(
            new JsonObject
            {
                ["id"] = "entry-1",
                ["product"] = "geoip",
                ["kind"] = "charge",
                ["amountMicros"] = 437.5,
                ["balanceMicros"] = 999562.5,
                ["reference"] = "receipt-1",
                ["createdAt"] = "2026-09-29T08:30:00.000Z",
            });
        using var harness = new Harness([Replies.From(fixture)]);

        var account = await harness.Client.GetAccountAsync(Ct);

        Assert.Equal("00000000-0000-4000-8000-000000000042", account.Id);
        Assert.Equal("Fixture", account.Name);
        Assert.Equal("ipmax_test", account.KeyPrefix);
        Assert.Equal(Product.GeoIp, account.Wallets[0].Product);
        Assert.Equal(437.5, account.Wallets[0].BalanceMicros);
        Assert.Equal(Product.Intelligence, account.Wallets[1].Product);
        Assert.Equal(1_000_000, account.Wallets[1].BalanceMicros);
        var entry = Assert.Single(account.Entries);
        Assert.Equal(LedgerEntryKind.Charge, entry.Kind);
        Assert.Equal(437.5, entry.AmountMicros);
        Assert.Equal(999562.5, entry.BalanceMicros);
        Assert.Equal("2026-09-29T08:30:00.000Z", entry.CreatedAt);
    }

    [Fact]
    public async Task AccountFixtureDecodes()
    {
        using var harness = new Harness([Replies.Fixture("account")]);

        var account = await harness.Client.GetAccountAsync(Ct);

        Assert.Equal([1_000_000d, 1_000_000d], account.Wallets.Select(wallet => wallet.BalanceMicros));
        Assert.Empty(account.Entries);
    }

    [Fact]
    public async Task GeoIpCarriesCountryEnrichment()
    {
        using var harness = new Harness([Replies.Fixture("geoip-8-8-8-8")]);

        var geo = await harness.Client.LookupGeoIpAsync("8.8.8.8", cancellationToken: Ct);

        Assert.Equal("8.8.8.8", geo.Ip);
        Assert.Equal("8.8.8.0/24", geo.Netmask);
        Assert.Equal(24, geo.PrefixLength);
        Assert.False(geo.IsBogon);
        Assert.Equal("USD", geo.Currency!.Code);
        Assert.Equal("$", geo.Currency.Symbol);
        Assert.Equal("PDT", geo.TimeZone!.Abbr);
        Assert.Equal("-0700", geo.TimeZone.Offset);
        Assert.True(geo.TimeZone.IsDst);
        Assert.Equal("1", geo.CallingCode);
        Assert.Equal("山景城", geo.Geo.City);
        Assert.Equal("US", geo.Geo.CountryCode);
        Assert.Equal(37.4056, geo.Geo.Latitude);
        Assert.Equal(-122.0775, geo.Geo.Longitude);
        Assert.Equal(25, geo.Geo.Radius);
        Assert.Equal("美国", geo.Geo.CountryZh);
        Assert.Null(geo.Geo.CountryEn);
        Assert.Equal("AS15169", geo.As.Asn);
        Assert.Equal("Google LLC", geo.As.Name);
        Assert.Null(geo.As.CountryCode);
    }

    [Fact]
    public async Task GeoIpForIpv6()
    {
        using var harness = new Harness([Replies.Fixture("geoip-240e-390-a1-3cd0-be24-11ff-fe46-aca3")]);

        var geo = await harness.Client.LookupGeoIpAsync("240e:390:a1:3cd0:be24:11ff:fe46:aca3", cancellationToken: Ct);

        Assert.Equal("CNY", geo.Currency!.Code);
        Assert.Equal("GMT+8", geo.TimeZone!.Abbr);
        Assert.False(geo.TimeZone.IsDst);
        Assert.Equal("广州", geo.Geo.City);
        Assert.Equal(32, geo.PrefixLength);
    }

    [Fact]
    public async Task GeoIpForReservedAddressHasNullEnrichment()
    {
        using var harness = new Harness([Replies.Fixture("geoip-10-1-2-3")]);

        var geo = await harness.Client.LookupGeoIpAsync("10.1.2.3", cancellationToken: Ct);

        Assert.True(geo.IsBogon);
        Assert.Equal(8, geo.PrefixLength);
        Assert.Null(geo.Currency);
        Assert.Null(geo.TimeZone);
        Assert.Null(geo.CallingCode);
        Assert.Equal("", geo.As.Asn);
        Assert.Equal(5000, geo.Geo.Radius);
    }

    [Fact]
    public async Task RichIntelligenceDecodesEveryVariant()
    {
        using var harness = new Harness([Replies.Fixture("intelligence-rich")]);

        var data = await harness.Client.LookupIntelligenceAsync("8.8.8.8", cancellationToken: Ct);
        var live = data.Intelligence;

        Assert.Equal(3, live.Ptr.Count);
        var located = Assert.IsType<PtrLocatedIntelligence>(live.Ptr[0]);
        Assert.Equal("located", located.Status);
        Assert.Equal("ae-1.r01.tokyjp05.jp.bb.gin.ntt.net", located.Hostname);
        Assert.Equal("Tokyo", located.Location.City);
        Assert.Equal("13", located.Location.RegionCode);
        Assert.Equal("JP", located.Location.CountryCode);
        Assert.Equal(35.6895, located.Location.Latitude);
        Assert.Equal("metro", located.Location.Precision);
        Assert.Equal("NTT", located.Operator.Name);
        Assert.Equal(PtrHintKind.Clli, located.Hint.Kind);
        Assert.Equal("tokyjp", located.Hint.Value);
        Assert.Equal(PtrConfidenceLevel.High, located.Confidence.Level);
        Assert.Equal(0.92, located.Confidence.Score);
        Assert.Equal("ntt-clli", located.Evidence.RuleId);
        Assert.Equal(PtrDatasetName.Geonames, Assert.Single(located.Evidence.Sources).Dataset);

        var mismatch = Assert.IsType<PtrAsnMismatchIntelligence>(live.Ptr[1]);
        Assert.Equal("asn_mismatch", mismatch.Status);
        Assert.Equal("AS64500", mismatch.Operator.Asn);
        Assert.Equal("AS64501", mismatch.ObservedAsn);
        Assert.Empty(mismatch.Evidence.Sources);

        var unmatched = Assert.IsType<PtrUnmatchedIntelligence>(live.Ptr[2]);
        Assert.Equal("dns.google", unmatched.Hostname);

        var transfer = Assert.Single(live.Transfers!);
        Assert.Equal("Level 3", transfer.SourceHolder);
        Assert.Equal("Google LLC", transfer.RecipientHolder);
        Assert.Equal("resourceTransfer", transfer.TransferType);

        var probe = live.SegmentProbe!;
        Assert.Equal("8.8.8.1", probe.Ip);
        Assert.Equal([53, 443], probe.Ports);
        Assert.Equal([LiveIntelligenceTag.PublicDnsResolver], probe.Tags);

        var abuse = live.Abuse!;
        Assert.Equal(12, abuse.ConfidenceScore);
        Assert.Equal(34, abuse.TotalReports);
        Assert.Equal(7, abuse.DistinctReporters);
        Assert.False(abuse.IsWhitelisted);
        Assert.Equal(new AbuseCategoryCount { Id = 18, Count = 20 }, Assert.Single(abuse.Categories));

        var cpe = Assert.Single(live.Cpes);
        Assert.Equal(CpePart.Application, cpe.Part);
        Assert.Equal("1.25.3", cpe.Version);
        Assert.Equal([LiveIntelligenceTag.PublicDnsResolver, LiveIntelligenceTag.WebServer], live.Tags);
        Assert.Equal([RevokedNetworkTag.HomeIsp], live.RevokedTags);
        Assert.Equal("DCH", live.UsageType);
        Assert.Null(live.Rpki);

        var rpki = data.Rpki!;
        Assert.Equal(RpkiStatus.Valid, rpki.Status);
        Assert.Equal(RpkiSource.RtrLive, rpki.Source);
        Assert.Equal("Local DB", rpki.RouteSource);
        Assert.Equal(24, rpki.MaxLength);
        Assert.Equal(RpkiRtrSource.Cloudflare, rpki.Rtr!.Source);
        Assert.Equal(1_790_000_000L, rpki.Rtr.LastUpdated);
        Assert.Null(rpki.Rtr.Server);
        Assert.Null(rpki.Validator);

        var connectivity = data.As.Connectivity!;
        Assert.Equal("1 Tbps+", connectivity.DeclaredTraffic);
        Assert.Null(connectivity.ExchangesTotal);
        Assert.Equal(3000, connectivity.EstimatedCapacity.LowerGbps);
        Assert.Null(connectivity.EstimatedCapacity.UpperGbps);
        Assert.Equal(30, connectivity.Exchanges.Count);
        Assert.Equal(AsnExchangeType.Physical, connectivity.Exchanges[0].Type);
        Assert.Equal("US", connectivity.Exchanges[0].CountryCode);
        var port = Assert.Single(connectivity.Exchanges[0].Ports);
        Assert.Equal(100, port.CapacityGbps);
        Assert.True(port.Operational);
        Assert.True(port.RsPeer);

        Assert.Equal(NetworkClass.Hosting, data.NetworkClass.Primary);
        Assert.Equal(NetworkClassConfidence.Medium, data.NetworkClass.Confidence);
        Assert.Equal(NetworkClassSource.NetworkTag, data.NetworkClass.Source);
        Assert.Empty(data.NetworkClass.Conflicts);
        Assert.Equal(NetworkClassScope.Prefix, data.NetworkClass.Evidence[0].Scope);

        var threat = data.Threat!;
        Assert.True(threat.IsKnownAbuser);
        Assert.False(threat.IsTor);
        Assert.Equal("known_abuser", Assert.Single(threat.Blocklists).Type);
        Assert.Equal(35, threat.Scores.ThreatScore);
        Assert.Equal(70, threat.Scores.TrustScore);
        Assert.Null(threat.AnonymizerExemptions);

        Assert.Equal(["cdn"], data.Network!.Tags);
        Assert.Null(data.Network.AddressType);
        Assert.Null(data.Mobile);
        Assert.Null(data.Company);
        Assert.Null(data.Abuse);
        Assert.Null(data.Dns);
        Assert.True(data.IsHosting);
    }

    [Fact]
    public async Task IntelligenceForIpv6()
    {
        using var harness = new Harness([Replies.Fixture("intelligence-240e-390-a1-3cd0-be24-11ff-fe46-aca3")]);

        var data = await harness.Client.LookupIntelligenceAsync("240e:390:a1:3cd0:be24:11ff:fe46:aca3", cancellationToken: Ct);

        Assert.Equal(["eui64_autoconfig"], data.Network!.Tags);
        Assert.Equal(NetworkClassSource.ThreatIntelligence, data.NetworkClass.Source);
        Assert.Equal(2, data.NetworkClass.Evidence.Count);
        Assert.Equal(0, data.Threat!.Scores.VpnScore);
        Assert.IsType<PtrUnmatchedIntelligence>(Assert.Single(data.Intelligence.Ptr));
        Assert.Equal(
            [RevokedNetworkTag.BusinessBroadband, RevokedNetworkTag.HomeIsp, RevokedNetworkTag.MobileIsp],
            data.Intelligence.RevokedTags);
        Assert.Equal("数据中心", data.Intelligence.UsageType);
        Assert.Null(data.Rpki);
    }

    [Fact]
    public async Task IntelligenceForReservedAddressHasNullSignals()
    {
        using var harness = new Harness([Replies.Fixture("intelligence-10-1-2-3")]);

        var data = await harness.Client.LookupIntelligenceAsync("10.1.2.3", cancellationToken: Ct);

        Assert.True(data.IsBogon);
        Assert.False(data.IsAnycast);
        Assert.Null(data.Network);
        Assert.Null(data.Rpki);
        Assert.Null(data.Threat!.IsVpn);
        Assert.Null(data.Threat.Scores.TrustScore);
        Assert.Equal(0, data.Threat.BlocklistCount);
        Assert.Equal(["anycast_network", "cdn"], data.As.Tags!);
        Assert.Null(data.As.Connectivity);
        Assert.Null(data.Intelligence.UsageType);
        Assert.Null(data.Intelligence.Abuse);
        Assert.Null(data.Intelligence.Transfers);
        Assert.Null(data.Intelligence.SegmentProbe);
        Assert.Empty(data.Intelligence.Ptr);
    }
}
