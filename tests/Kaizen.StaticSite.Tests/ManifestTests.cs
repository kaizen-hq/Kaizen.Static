using System.Text.Json;
using Kaizen.StaticSite;
using Xunit;

namespace Kaizen.StaticSite.Tests;

/// <summary>
/// 0.1.1 replaced the anonymous-type manifest (reflection JSON) with source-generated records. The file must be byte-identical:
/// the oracle below is the 0.1.0 code, verbatim.
/// </summary>
public class ManifestTests
{
    private static string Reflection010(string site, RouteInventory inv, Dictionary<string, string> pages, IEnumerable<string> assets)
    {
        static string FileFor(string routePath) => routePath == "/" ? "index.html" : routePath.TrimStart('/') + "/index.html";
        var manifest = new
        {
            site,
            pages = inv.Routes.Where(r => pages.ContainsKey(r.Path)).Select(r => new { r.Path, file = FileFor(r.Path), r.Template, origin = r.Origin.ToString() }),
            assets = assets.OrderBy(k => k, StringComparer.Ordinal),
            excluded = inv.Excluded.Select(e => new { e.Template, e.Reason }),
        };
        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    }

    private static RouteInventory Inventory()
    {
        var inv = new RouteInventory();
        inv.Routes.Add(new StaticRoute("/", "/", RouteOrigin.Direct, null));
        inv.Routes.Add(new StaticRoute("/why-nuclear", "/{slug}", RouteOrigin.Expanded, null));
        inv.Routes.Add(new StaticRoute("/items/b c", "/items/{id}", RouteOrigin.Expanded, null));
        inv.Routes.Add(new StaticRoute("/failed", "/failed", RouteOrigin.Direct, null));   // not in pages: must be left out
        inv.Routes.Add(new StaticRoute("/extra", "/extra", RouteOrigin.Extra, null));
        inv.Excluded.Add(new ExcludedRoute("/blog", "IStaticPageGate (no published blog pages)", null));
        inv.Excluded.Add(new ExcludedRoute("/not-found", "ExcludeFromStaticExport (404 page; exported as 404.html by the exporter)", null));
        inv.Excluded.Add(new ExcludedRoute("/é", "quotes \" and <tags> & ünïcode", null));
        return inv;
    }

    [Fact]
    public void Manifest_is_byte_identical_to_0_1_0()
    {
        var inv = Inventory();
        var pages = new Dictionary<string, string>(StringComparer.Ordinal) { ["/"] = "", ["/why-nuclear"] = "", ["/items/b c"] = "", ["/extra"] = "" };
        var assets = new[] { "styles.css", "assets/mark.svg", "favicon.ico", "Z.png", "a+b.css" };
        Assert.Equal(Reflection010("https://nucleics.org", inv, pages, assets), StaticSiteExporter.ManifestJson("https://nucleics.org", inv, pages.Keys, assets));
    }

    [Fact]
    public void Empty_manifest_is_byte_identical_to_0_1_0()
    {
        var inv = new RouteInventory();
        var pages = new Dictionary<string, string>(StringComparer.Ordinal);
        Assert.Equal(Reflection010("https://e.org", inv, pages, Array.Empty<string>()), StaticSiteExporter.ManifestJson("https://e.org", inv, pages.Keys, Array.Empty<string>()));
    }

    [Fact]
    public void Check_mode_can_read_pages_Path_back()
    {
        var json = StaticSiteExporter.ManifestJson("https://e.org", Inventory(), new[] { "/", "/why-nuclear" }, Array.Empty<string>());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(new[] { "/", "/why-nuclear" }, doc.RootElement.GetProperty("pages").EnumerateArray().Select(e => e.GetProperty("Path").GetString()));
    }
}
