using ShrinkDarkerDisplay.Core;

namespace ShrinkDarkerDisplay.Tests;

public sealed class SettingsTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(101)]
    public void InvalidBrightnessIsRejected(int value) => Assert.Throws<ArgumentOutOfRangeException>(() => new Brightness(value));

    [Fact]
    public void FullBrightnessHasNoOverlay() => Assert.Equal(0, new Brightness(100).OverlayAlpha);


    [Fact]
    public void ReconnectedMonitorRetainsSettingByStableId()
    {
        var settings = new Settings { Displays = new() { ["monitor-instance-A"] = 37, ["monitor-instance-B"] = 82 } };
        Assert.Equal(82, settings.ForDisplay("monitor-instance-B").Percent);
        Assert.Equal(37, settings.ForDisplay("monitor-instance-A").Percent);
        Assert.Equal(100, settings.ForDisplay("new-monitor").Percent);
    }

    [Theory]
    [InlineData(-10, 10)]
    [InlineData(400, 100)]
    public void OutOfRangeStoredValuesAreClamped(int stored, int expected)
    {
        var settings = new Settings { Displays = new() { ["monitor"] = stored } };
        Assert.Equal(expected, settings.ForDisplay("monitor").Percent);
    }

    [Fact]
    public void SettingsRoundTripAndOverwriteAtomically()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ShrinkDarkerDisplay-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "settings.json");
            var store = new SettingsStore(path);
            Assert.Empty(store.Load().Displays);
            store.Save(new Settings { Displays = new() { ["screen"] = 24 } });
            Assert.Equal(24, store.Load().ForDisplay("screen").Percent);
            store.Save(new Settings { Displays = new() { ["screen"] = 65 } });
            Assert.Equal(65, store.Load().ForDisplay("screen").Percent);
            Assert.False(File.Exists(path + ".tmp"));
            File.WriteAllText(path, "broken-json");
            Assert.Throws<System.Text.Json.JsonException>(() => store.Load());
            Assert.Equal("broken-json", File.ReadAllText(path));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
