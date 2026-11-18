using System.Text.Json.Serialization;

namespace Kaizen.StaticSite;

// kaizen-manifest.json shape. Property names and order are pinned with [JsonPropertyName] to the pre-0.1.1 anonymous-type output
// (note the mixed casing: "Path"/"Template"/"Reason" were the original member names; `check` reads pages[].Path back).

internal sealed record ExportManifest(
    [property: JsonPropertyName("site")] string Site,
    [property: JsonPropertyName("pages")] IReadOnlyList<ManifestPage> Pages,
    [property: JsonPropertyName("assets")] IReadOnlyList<string> Assets,
    [property: JsonPropertyName("excluded")] IReadOnlyList<ManifestExclusion> Excluded);

internal sealed record ManifestPage(
    [property: JsonPropertyName("Path")] string Path,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("Template")] string Template,
    [property: JsonPropertyName("origin")] string Origin);

internal sealed record ManifestExclusion(
    [property: JsonPropertyName("Template")] string Template,
    [property: JsonPropertyName("Reason")] string Reason);

/// <summary>Source-generated (trim/AOT-safe) serializer for the manifest; same settings as before: indented, default encoder.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ExportManifest))]
internal sealed partial class ManifestJsonContext : JsonSerializerContext;
