using System.Text.Json;

namespace ShrinkDarkerDisplay.Core;

public sealed class LocalizedText
{
    private readonly Dictionary<string, string> strings;

    public LocalizedText(string directory, string cultureName)
    {
        strings = Read(Path.Combine(directory, "en.json"));
        var language = ResolveLanguage(cultureName);
        var path = Path.Combine(directory, language + ".json");
        if (language != "en" && File.Exists(path))
        {
            foreach (var pair in Read(path)) strings[pair.Key] = pair.Value;
        }
    }

    public string this[string key] => strings.TryGetValue(key, out var value) ? value : key;

    public static string ResolveLanguage(string cultureName)
    {
        var parts = cultureName.ToLowerInvariant().Split('-');
        if (parts[0] == "ja") return "ja";
        if (parts[0] != "zh") return "en";
        if (parts.Contains("hant")) return "zh-Hant";
        if (parts.Contains("hans")) return "zh-Hans";
        return parts.Any(p => p is "tw" or "hk" or "mo") ? "zh-Hant" : "zh-Hans";
    }

    private static Dictionary<string, string> Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.GetString() ?? throw new InvalidDataException($"Empty translation: {property.Name}"),
            StringComparer.Ordinal);
    }
}
