using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShrinkDarkerDisplay.Core;

public sealed record Brightness
{
    public int Percent { get; }
    public byte OverlayAlpha => (byte)Math.Round(255 * (1 - Percent / 100.0));
    public Brightness(int percent)
    {
        if (percent is < 10 or > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        Percent = percent;
    }
}

public sealed class Settings
{
    public int Version { get; set; } = 1;
    public Dictionary<string, int> Displays { get; set; } = new(StringComparer.Ordinal);
    public List<string> DisabledDisplays { get; set; } = [];
    public bool StartMinimized { get; set; }
    public bool NightMode { get; set; }
    public bool FollowSystemTheme { get; set; } = true;
    public Brightness ForDisplay(string id) => new(Displays.TryGetValue(id, out var value) ? Math.Clamp(value, 10, 100) : 100);
}

public sealed class SettingsStore(string path)
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShrinkDarkerDisplay", "settings.json");
    public Settings Load()
    {
        if (!File.Exists(path)) return new();
        var settings = JsonSerializer.Deserialize(File.ReadAllText(path), SettingsJsonContext.Default.Settings) ?? throw new InvalidDataException("设置文件为空。");
        if (settings.Version != 1 || settings.Displays is null || settings.DisabledDisplays is null) throw new InvalidDataException("不支持的设置文件格式。");
        return settings;
    }
    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));
        File.Move(temporary, path, true);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext { }
