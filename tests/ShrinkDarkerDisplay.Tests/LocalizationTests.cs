using System.Text.Json;
using ShrinkDarkerDisplay.Core;

namespace ShrinkDarkerDisplay.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-SG", "zh-Hans")]
    [InlineData("zh-Hant", "zh-Hant")]
    [InlineData("zh-HK", "zh-Hant")]
    [InlineData("zh-MO", "zh-Hant")]
    [InlineData("ja-JP", "ja")]
    [InlineData("de-DE", "en")]
    public void SelectsLanguageAndFallsBack(string culture, string expected)
        => Assert.Equal(expected, LocalizedText.ResolveLanguage(culture));

    [Fact]
    public void TranslationFilesHaveMatchingNonemptyKeys()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Localization");
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "en.json")));
        var keys = baseline.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();
        foreach (var language in new[] { "en", "zh-Hans", "zh-Hant", "ja" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, language + ".json")));
            Assert.Equal(keys, document.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
            var text = new LocalizedText(directory, language);
            Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(text[key])));
        }
    }

    [Fact]
    public void MissingTranslationUsesEnglish()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ShrinkDarkerDisplay-locale-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "en.json"), "{\"Title\":\"Screen dimmer\",\"Exit\":\"Exit\"}");
            File.WriteAllText(Path.Combine(directory, "ja.json"), "{\"Exit\":\"終了\"}");
            var text = new LocalizedText(directory, "ja-JP");
            Assert.Equal("終了", text["Exit"]);
            Assert.Equal("Screen dimmer", text["Title"]);
            Assert.Equal("Exit", new LocalizedText(directory, "zh-TW")["Exit"]);
        }
        finally { Directory.Delete(directory, true); }
    }
}
