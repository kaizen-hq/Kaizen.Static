using System.Text.Json.Serialization;

namespace Kaizen.Seo;

/// <summary>
/// Source-generated System.Text.Json metadata for everything the <see cref="JsonLd"/> builders produce: nested
/// <c>Dictionary&lt;string, object?&gt;</c> values holding strings, ints, string arrays and dictionary arrays, plus the
/// common primitives and collections a site is likely to put in a hand-built JSON-LD dictionary.
/// Options match the previous reflection call (<c>new JsonSerializerOptions { WriteIndented = false }</c>), so output is unchanged.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, object?>[]))]
[JsonSerializable(typeof(List<Dictionary<string, object?>>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(List<object>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(Uri))]
internal sealed partial class JsonLdJsonContext : JsonSerializerContext;
