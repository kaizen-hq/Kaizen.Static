using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Kaizen.Seo;

/// <summary>Tiny schema.org JSON-LD builders. Each returns a plain dictionary; <see cref="Serialize"/> makes it script-safe.</summary>
public static class JsonLd
{
    private const string Ctx = "https://schema.org";

    public static Dictionary<string, object?> Organization(string name, string url, string? logoUrl = null, IEnumerable<string>? sameAs = null, string? description = null)
        => Clean(new()
        {
            ["@context"] = Ctx, ["@type"] = "Organization", ["name"] = name, ["url"] = url,
            ["logo"] = logoUrl, ["description"] = description, ["sameAs"] = sameAs?.ToArray() is { Length: > 0 } s ? s : null,
        });

    public static Dictionary<string, object?> WebSite(string name, string url)
        => new() { ["@context"] = Ctx, ["@type"] = "WebSite", ["name"] = name, ["url"] = url };

    public static Dictionary<string, object?> BlogPosting(string headline, string url, DateTimeOffset published,
        string? description = null, DateTimeOffset? modified = null, string? authorName = null, string? publisherName = null,
        string? image = null, IEnumerable<string>? keywords = null)
        => Clean(new()
        {
            ["@context"] = Ctx, ["@type"] = "BlogPosting", ["headline"] = headline,
            ["mainEntityOfPage"] = new Dictionary<string, object?> { ["@type"] = "WebPage", ["@id"] = url },
            ["url"] = url, ["datePublished"] = published.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ["dateModified"] = (modified ?? published).ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ["description"] = description, ["image"] = image,
            ["author"] = authorName is null ? null : new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = authorName },
            ["publisher"] = publisherName is null ? null : new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = publisherName },
            ["keywords"] = keywords?.ToArray() is { Length: > 0 } k ? string.Join(", ", k) : null,
        });

    /// <summary>An evergreen web page with its published/modified dates (modified falls back to published).</summary>
    public static Dictionary<string, object?> WebPage(string name, string url, DateTimeOffset published,
        string? description = null, DateTimeOffset? modified = null, string? authorName = null, string? publisherName = null)
        => Clean(new()
        {
            ["@context"] = Ctx, ["@type"] = "WebPage", ["name"] = name, ["url"] = url,
            ["datePublished"] = published.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ["dateModified"] = (modified ?? published).ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ["description"] = description,
            ["author"] = authorName is null ? null : new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = authorName },
            ["publisher"] = publisherName is null ? null : new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = publisherName },
        });

    public static Dictionary<string, object?> BreadcrumbList(params (string Name, string Url)[] items)
        => new()
        {
            ["@context"] = Ctx, ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items.Select((it, i) => new Dictionary<string, object?>
                { ["@type"] = "ListItem", ["position"] = i + 1, ["name"] = it.Name, ["item"] = it.Url }).ToArray(),
        };

    /// <summary>JSON safe to inline in a script element: the default encoder escapes &lt; &gt; &amp; (no "&lt;/script&gt;" break-out).</summary>
    /// <remarks>
    /// Serialization uses the source-generated <see cref="JsonLdJsonContext"/> (trim/AOT-safe) for the dictionaries the builders return.
    /// Any other runtime type (e.g. an anonymous object or a site's own record) still works in a normal app through a reflection fallback;
    /// in a trimmed/AOT app with reflection-based serialization disabled it throws <see cref="NotSupportedException"/> naming the type.
    /// </remarks>
    public static string Serialize(object data) => JsonSerializer.Serialize(data, ObjectTypeInfo);

    private static readonly JsonSerializerOptions Options = CreateOptions();
    private static readonly JsonTypeInfo<object> ObjectTypeInfo = (JsonTypeInfo<object>)Options.GetTypeInfo(typeof(object));

    private static JsonSerializerOptions CreateOptions()
    {
        // Same settings as before 0.1.1 (default encoder, not indented); only the metadata source changed.
        var options = new JsonSerializerOptions { WriteIndented = false };
        options.TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? JsonTypeInfoResolver.Combine(JsonLdJsonContext.Default, ReflectionFallback())
            : JsonLdJsonContext.Default;
        options.MakeReadOnly();
        return options;
    }

    // Only reached when JsonSerializer.IsReflectionEnabledByDefault is true. Trimmed and native AOT apps default that feature switch
    // (JsonSerializerIsReflectionEnabledByDefault) to false, and the trimmer removes this branch, so the reflection resolver is never
    // used where it is unsafe; there, types outside JsonLdJsonContext fail with a clear NotSupportedException instead.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault (false when trimming/AOT).")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault (false when trimming/AOT).")]
    private static IJsonTypeInfoResolver ReflectionFallback() => new DefaultJsonTypeInfoResolver();

    private static Dictionary<string, object?> Clean(Dictionary<string, object?> d)
    {
        foreach (var k in d.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList()) d.Remove(k);
        return d;
    }
}
