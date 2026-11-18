using Kaizen.StaticSite;
using Xunit;

namespace Kaizen.StaticSite.Tests;

public class SitemapTests
{
    private static readonly System.Xml.Linq.XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    [Fact]
    public void Lastmod_is_written_only_where_a_date_is_supplied()
    {
        var doc = StaticSiteExporter.BuildSitemap("https://example.org/", new (string, DateTimeOffset?)[]
        {
            ("/why", new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero)),
            ("/", null),
        });
        var urls = doc.Root!.Elements(Ns + "url").ToList();
        Assert.Equal("https://example.org/", urls[0].Element(Ns + "loc")!.Value);
        Assert.Null(urls[0].Element(Ns + "lastmod"));
        Assert.Equal("https://example.org/why/", urls[1].Element(Ns + "loc")!.Value);
        Assert.Equal("2026-10-10", urls[1].Element(Ns + "lastmod")!.Value);
    }

    [Fact]
    public void Lastmod_uses_the_supplied_calendar_date_not_utc_conversion()
    {
        var doc = StaticSiteExporter.BuildSitemap("https://example.org", new (string, DateTimeOffset?)[]
        {
            ("/p", new DateTimeOffset(2026, 10, 10, 23, 0, 0, TimeSpan.FromHours(-5))),
        });
        Assert.Equal("2026-10-10", doc.Root!.Element(Ns + "url")!.Element(Ns + "lastmod")!.Value);
    }

    [Theory]
    [InlineData("<meta name=\"robots\" content=\"noindex,follow\" />", true)]
    [InlineData("<META NAME=\"ROBOTS\" CONTENT=\"NOINDEX, FOLLOW\">", true)]
    [InlineData("<meta content='noindex' name='robots'>", true)]
    [InlineData("<meta name=\"robots\" content=\"none\" />", true)]
    [InlineData("<meta name=\"robots\" content=\"index,follow\" />", false)]
    [InlineData("<p>noindex is mentioned in the body</p>", false)]
    [InlineData("<meta name=\"description\" content=\"noindex this\">", false)]
    public void HasNoIndex_reads_the_robots_meta(string html, bool expected) =>
        Assert.Equal(expected, StaticSiteExporter.HasNoIndex(html));

    [Fact]
    public void ExcludeNoIndexFromSitemap_omits_noindex_pages_only()
    {
        var pages = new (string Path, string Html, DateTimeOffset? LastModified)[]
        {
            ("/", "<title>Home</title>", null),
            ("/sms/confirmed", "<meta name=\"robots\" content=\"noindex,follow\" />", null),
            ("/about", "<title>About</title>", new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero)),
        };
        var routes = pages.Where(p => !StaticSiteExporter.HasNoIndex(p.Html)).Select(p => (p.Path, p.LastModified));
        var doc = StaticSiteExporter.BuildSitemap("https://www.5obowling.com", routes);
        var locs = doc.Root!.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value).ToList();
        Assert.Equal(new[] { "https://www.5obowling.com/", "https://www.5obowling.com/about/" }, locs);
        Assert.Equal("2026-11-18", doc.Root!.Elements(Ns + "url").Last().Element(Ns + "lastmod")!.Value);
    }
}
