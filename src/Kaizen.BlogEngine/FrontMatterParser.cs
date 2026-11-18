using System.ComponentModel;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kaizen.BlogEngine;

/// <summary>The typed model of a post's YAML front matter. Property names map to camelCase keys (title, date, updated, ...).</summary>
public sealed class FrontMatter
{
    public string? Title { get; set; }
    /// <summary>ISO date (2026-10-01) or date-time (2026-10-01T09:30:00Z). Kept as text so no time-zone shifting happens while reading.</summary>
    public string? Date { get; set; }
    public string? Updated { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? Slug { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool Draft { get; set; }
    /// <summary>Optional layout hint, e.g. "page" for an evergreen web page. Null means the default (post) layout.</summary>
    public string? Layout { get; set; }
}

/// <summary>
/// Source-generated (Vecc.YamlDotNet.Analyzers.StaticGenerator) YamlDotNet type metadata for <see cref="FrontMatter"/>, used by
/// <see cref="FrontMatterParser"/>. Infrastructure only: public because the generator emits a public partial class.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[YamlStaticContext]
[YamlSerializable(typeof(FrontMatter))]
public partial class FrontMatterYamlContext : StaticContext { }

/// <summary>
/// Reads the YAML found by Markdig's YamlFrontMatter extension into <see cref="FrontMatter"/> with YamlDotNet.
/// Standard YAML is supported (plain/quoted scalars, inline [a, b] and dash lists, | and > multi-line text, comments).
/// Policy: STRICT. An unknown key (typo such as "dratf"), malformed YAML, a wrong type (draft: maybe, tags: scalar) or a duplicate key
/// throws an <see cref="InvalidOperationException"/> that names the file and the line; nothing is ever skipped silently.
/// </summary>
public static class FrontMatterParser
{
    // StaticDeserializerBuilder + the source-generated FrontMatterYamlContext: no reflection, so the library is trim/AOT-safe.
    private static readonly IDeserializer Deserializer = new StaticDeserializerBuilder(new FrontMatterYamlContext())
        .WithNamingConvention(CamelCaseNamingConvention.Instance)   // Title <-> title, and e.g. PublishedAt <-> publishedAt
        .WithDuplicateKeyChecking()                                  // a repeated key is an error, not "last one wins"
        .Build();                                                    // IgnoreUnmatchedProperties NOT set: unknown keys are errors

    /// <param name="yaml">The YAML between the --- fences (without them).</param>
    /// <param name="source">File path used in error messages.</param>
    /// <param name="fenceLine">1-based line of the opening --- in the file, so YAML line k is reported as file line fenceLine + k.</param>
    public static FrontMatter Parse(string yaml, string source = "front matter", int fenceLine = 0)
    {
        if (string.IsNullOrWhiteSpace(yaml)) return new FrontMatter();   // empty block: required-field checks then say what is missing
        try
        {
            return Deserializer.Deserialize<FrontMatter?>(yaml) ?? new FrontMatter();
        }
        catch (YamlException e)
        {
            var line = e.Start.Line > 0 ? $", line {fenceLine + (int)e.Start.Line}" : "";
            throw new InvalidOperationException($"{source}{line}: invalid front matter: {Friendly(e)}", e);
        }
        catch (InvalidCastException e)
        {
            // The static (source-generated) deserializer reports a top-level non-mapping (e.g. "just text") as a bare cast error with
            // no position; the reflection deserializer used before 0.1.1 wrapped it in a YamlException. Same message as before.
            var first = FirstContentLine(yaml);
            var line = first > 0 ? $", line {fenceLine + first}" : "";
            throw new InvalidOperationException($"{source}{line}: invalid front matter: {WrongShape}", e);
        }
    }

    /// <summary>1-based line of the first line that is not blank or a comment (0 if none).</summary>
    private static int FirstContentLine(string yaml)
    {
        var lines = yaml.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Trim() is { Length: > 0 } t && !t.StartsWith('#')) return i + 1;
        return 0;
    }

    private static string Friendly(YamlException e)
    {
        var m = e.InnerException?.Message ?? e.Message;
        m = Regex.Replace(m, @"^\(Line:.*?\):\s*", "");   // YamlDotNet's own position prefix; ours is already in the message
        var unknown = Regex.Match(m, @"^Property '([^']+)' not found");
        if (unknown.Success)
            return $"unknown key '{unknown.Groups[1].Value}' (allowed, lowercase camelCase: {Allowed}); keys are case-sensitive and typos are errors, not ignored.";
        // "Invalid cast"/"No node deserializer": reflection-era wording; "Unable to cast"/"not registered in the YamlDotNet static context":
        // what the static deserializer says for the same mistakes (a scalar where a list is required, a map where text is required).
        if (m.StartsWith("Invalid cast") || m.StartsWith("No node deserializer") || m.StartsWith("Unable to cast")
            || m.Contains("is not registered in the YamlDotNet static context"))
            return WrongShape;
        return m;
    }

    private const string WrongShape =
        "value has the wrong shape or type for its key (tags must be a list, e.g. [a, b] or '- a' lines; text keys take one text value; draft is true/false)";

    private const string Allowed = "title, date, updated, description, author, slug, tags, draft, layout";
}
