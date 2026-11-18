using System.Text.Json;
using Kaizen.Seo;
using Xunit;

namespace Kaizen.Seo.Tests;

/// <summary>
/// 0.1.1 switched JsonLd.Serialize from reflection to the source-generated JsonLdJsonContext. Output must be byte-identical to the
/// 0.1.0 call, <c>JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false })</c>, which these tests run as the oracle.
/// </summary>
public class JsonLdSerializeTests
{
    private static readonly DateTimeOffset Pub = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Mod = new(2026, 10, 10, 9, 30, 0, TimeSpan.FromHours(-5));

    // The 0.1.0 implementation, verbatim.
    private static string Reflection(object data) => JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });

    public static TheoryData<string, object> Cases() => new()
    {
        { "Organization", JsonLd.Organization("Nucleics", "https://nucleics.org", "https://nucleics.org/assets/favicon-64.png",
            new[] { "https://x.com/a", "https://github.com/b" }, "Advocacy for nuclear abundance: 100 MWh <per> person & year.") },
        { "Organization minimal", JsonLd.Organization("N", "https://n.org") },
        { "WebSite", JsonLd.WebSite("Nucleics", "https://nucleics.org/") },
        { "BlogPosting", JsonLd.BlogPosting("Why \"nuclear\"? </script><b>", "https://n.org/blog/why/", Pub, "Déjà vu — ünïcode + emoji 🚀",
            Mod, "Kijana Woodard", "Nucleics", "https://n.org/a.png", new[] { "nuclear", "energy" }) },
        { "BlogPosting minimal", JsonLd.BlogPosting("H", "https://n.org/p/", Pub) },
        { "WebPage", JsonLd.WebPage("Why nuclear? Start with energy density", "https://nucleics.org/why-nuclear/", Pub,
            "A fingertip-sized pellet.", Mod, "Kijana Woodard", "Nucleics") },
        { "BreadcrumbList", JsonLd.BreadcrumbList(("Home", "https://n.org/"), ("Why nuclear?", "https://n.org/why-nuclear/")) },
        { "BreadcrumbList empty", JsonLd.BreadcrumbList() },
        { "hand-built dictionary", new Dictionary<string, object?>
            { ["@type"] = "Thing", ["n"] = 3L, ["d"] = 1.5, ["b"] = true, ["none"] = null, ["list"] = new List<string> { "a" }, ["when"] = Pub } },
    };

    [Theory, MemberData(nameof(Cases))]
    public void Output_is_byte_identical_to_the_reflection_serializer(string name, object data)
    {
        _ = name;
        Assert.Equal(Reflection(data), JsonLd.Serialize(data));
    }

    [Fact]
    public void Golden_breadcrumbs_and_webpage() // literal strings from the 0.1.0 nucleics.org export
    {
        Assert.Equal("""{"@context":"https://schema.org","@type":"BreadcrumbList","itemListElement":[{"@type":"ListItem","position":1,"name":"Home","item":"https://nucleics.org/"},{"@type":"ListItem","position":2,"name":"Why nuclear? Start with energy density","item":"https://nucleics.org/why-nuclear/"}]}""",
            JsonLd.Serialize(JsonLd.BreadcrumbList(("Home", "https://nucleics.org/"), ("Why nuclear? Start with energy density", "https://nucleics.org/why-nuclear/"))));
        Assert.Equal("""{"@context":"https://schema.org","@type":"WebPage","name":"Why nuclear? Start with energy density","url":"https://nucleics.org/why-nuclear/","datePublished":"2026-10-01T00:00:00\u002B00:00","dateModified":"2026-10-10T00:00:00\u002B00:00","description":"A fingertip-sized uranium pellet holds as much energy as a ton of coal. Density is most of the argument, and land use is the rest.","author":{"@type":"Person","name":"Kijana Woodard"},"publisher":{"@type":"Organization","name":"Nucleics"}}""",
            JsonLd.Serialize(JsonLd.WebPage("Why nuclear? Start with energy density", "https://nucleics.org/why-nuclear/", Pub,
                "A fingertip-sized uranium pellet holds as much energy as a ton of coal. Density is most of the argument, and land use is the rest.",
                new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero), "Kijana Woodard", "Nucleics")));
    }

    [Fact]
    public void Script_breakout_characters_stay_escaped()
    {
        var json = JsonLd.Serialize(JsonLd.WebSite("</script><script>alert(1)</script>", "https://n.org/?a=1&b=2"));
        Assert.DoesNotContain("<", json);
        Assert.DoesNotContain(">", json);
        Assert.DoesNotContain("&", json);
    }

    private sealed record Custom(string Name, int Count);

    [Fact]
    public void Types_outside_the_context_still_serialize_when_reflection_is_enabled()
    {
        object anon = new { name = "x", n = 1 };
        Assert.Equal(Reflection(anon), JsonLd.Serialize(anon));
        Assert.Equal(Reflection(new Custom("y", 2)), JsonLd.Serialize(new Custom("y", 2)));
    }
}
