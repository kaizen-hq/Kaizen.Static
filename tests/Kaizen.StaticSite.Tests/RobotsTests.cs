using Xunit;

namespace Kaizen.StaticSite.Tests;

public sealed class RobotsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "kaizen-robots-" + Guid.NewGuid().ToString("N"));
    public RobotsTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Generated_robots_txt_is_allow_all_plus_sitemap()
    {
        Assert.Equal(
            "User-agent: *\nAllow: /\nSitemap: https://nucleics.org/sitemap.xml\n",
            StaticSiteExporter.BuildRobotsTxt("https://nucleics.org/"));
    }

    [Fact]
    public void Extra_lines_are_appended_after_the_sitemap_line()
    {
        Assert.Equal(
            "User-agent: *\nAllow: /\nSitemap: https://example.org/sitemap.xml\nDisallow: /draft/\n",
            StaticSiteExporter.BuildRobotsTxt("https://example.org", new[] { "Disallow: /draft/" }));
    }

    [Fact]
    public void No_custom_file_means_generate()
    {
        Assert.Null(StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions { PassthroughDirectory = _dir }));
        Assert.Null(StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions()));
    }

    [Fact]
    public void Passthrough_robots_txt_is_discovered()
    {
        var path = Path.Combine(_dir, "robots.txt");
        File.WriteAllText(path, "User-agent: *\nDisallow: /\n");
        var resolved = StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions { PassthroughDirectory = _dir });
        Assert.Equal(Path.GetFullPath(path), resolved);
    }

    [Fact]
    public void Wwwroot_robots_txt_is_discovered_when_the_root_file_is_absent()
    {
        var www = Path.Combine(_dir, "wwwroot");
        Directory.CreateDirectory(www);
        var path = Path.Combine(www, "robots.txt");
        File.WriteAllText(path, "User-agent: *\nDisallow: /tmp/\n");
        var resolved = StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions { PassthroughDirectory = _dir });
        Assert.Equal(Path.GetFullPath(path), resolved);
    }

    [Fact]
    public void Passthrough_robots_txt_wins_over_wwwroot()
    {
        var www = Path.Combine(_dir, "wwwroot");
        Directory.CreateDirectory(www);
        File.WriteAllText(Path.Combine(www, "robots.txt"), "wwwroot");
        var root = Path.Combine(_dir, "robots.txt");
        File.WriteAllText(root, "root");
        var resolved = StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions { PassthroughDirectory = _dir });
        Assert.Equal(Path.GetFullPath(root), resolved);
    }

    [Fact]
    public void Explicit_path_wins_and_is_resolved_against_the_passthrough_directory()
    {
        var custom = Path.Combine(_dir, "content", "robots.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(custom)!);
        File.WriteAllText(custom, "custom");
        File.WriteAllText(Path.Combine(_dir, "robots.txt"), "root");
        var resolved = StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions
        {
            PassthroughDirectory = _dir,
            RobotsTxtPath = Path.Combine("content", "robots.txt"),
        });
        Assert.Equal(Path.GetFullPath(custom), resolved);
    }

    [Fact]
    public void Explicit_path_is_returned_even_when_the_file_is_missing()
    {
        var resolved = StaticSiteExporter.ResolveCustomRobotsTxt(new StaticSiteOptions
        {
            PassthroughDirectory = _dir,
            RobotsTxtPath = "missing-robots.txt",
        });
        Assert.Equal(Path.GetFullPath(Path.Combine(_dir, "missing-robots.txt")), resolved);
        Assert.False(File.Exists(resolved));
    }

    [Fact]
    public void Custom_file_is_copied_byte_for_byte()
    {
        var src = Path.Combine(_dir, "robots.txt");
        // Unusual line endings and no trailing newline must be preserved.
        var bytes = "User-agent: *\r\nDisallow: /secret/\r\n"u8.ToArray();
        File.WriteAllBytes(src, bytes);
        var dest = Path.Combine(_dir, "out", "robots.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var options = new StaticSiteOptions { PassthroughDirectory = _dir, RobotsExtraLines = { "Disallow: /ignored/" } };
        var log = new StringWriter();
        StaticSiteExporter.WriteRobotsTxt(dest, "https://example.org", options, log);
        Assert.Equal(bytes, File.ReadAllBytes(dest));
        var logged = log.ToString();
        Assert.Contains("copied robots.txt (as-is)", logged);
        Assert.Contains("RobotsExtraLines is ignored", logged);
    }

    [Fact]
    public void Missing_explicit_path_fails_instead_of_generating()
    {
        var dest = Path.Combine(_dir, "out", "robots.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var options = new StaticSiteOptions { PassthroughDirectory = _dir, RobotsTxtPath = "missing-robots.txt" };
        var ex = Assert.Throws<InvalidOperationException>(() =>
            StaticSiteExporter.WriteRobotsTxt(dest, "https://example.org", options, new StringWriter()));
        Assert.Contains("robots.txt not found", ex.Message);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void Generated_file_is_written_when_no_custom_file_is_present()
    {
        var dest = Path.Combine(_dir, "out", "robots.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        StaticSiteExporter.WriteRobotsTxt(dest, "https://example.org", new StaticSiteOptions { PassthroughDirectory = _dir }, new StringWriter());
        Assert.Equal(StaticSiteExporter.BuildRobotsTxt("https://example.org"), File.ReadAllText(dest));
    }
}
