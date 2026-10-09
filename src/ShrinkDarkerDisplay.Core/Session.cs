using System.Diagnostics;

namespace ShrinkDarkerDisplay.Core;

public sealed class Session : IDisposable
{
    private readonly Dictionary<string, OverlayWindow> windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Brightness> values = new(StringComparer.Ordinal);
    public IReadOnlyList<Display> Displays { get; private set; } = [];
    public IReadOnlyList<nint> Handles => windows.Values.Select(w => w.Handle).ToArray();
    public bool Paused { get; private set; }
    public void EnsureAbovePopups() { if (!Paused) foreach (var window in windows.Values) window.EnsureAbovePopups(); }
    public void Refresh()
    {
        var displays = DisplayCatalog.Enumerate();
        try
        {
            foreach (var display in displays)
            {
                // Device notifications also arrive for unrelated hardware. Preserve live surfaces.
                if (!windows.TryGetValue(display.Id, out var window))
                {
                    window = new OverlayWindow(display);
                    windows.Add(display.Id, window);
                }
                else window.UpdateDisplay(display);
                values.TryAdd(display.Id, new(100));
                window.SetBrightness(Paused ? new(100) : values[display.Id]);
            }
            var active = displays.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var id in windows.Keys.Where(id => !active.Contains(id)).ToArray())
            {
                windows[id].Dispose();
                windows.Remove(id);
            }
            Displays = displays;
        }
        catch { EmergencyReset(); throw; }
    }
    public void Set(string id, Brightness brightness)
    {
        if (!windows.TryGetValue(id, out var window)) throw new ArgumentException("显示器已断开。", nameof(id));
        window.SetBrightness(Paused ? new(100) : brightness);
        values[id] = brightness;
    }
    public Brightness Get(string id) => values.TryGetValue(id, out var value) ? value : new(100);
    public Brightness At(int x, int y)
    {
        if (Paused) return new(100);
        var display = Displays.FirstOrDefault(d => x >= d.X && x < d.X + d.Width && y >= d.Y && y < d.Y + d.Height);
        return display is null ? new(100) : Get(display.Id);
    }
    public void SetPaused(bool paused)
    {
        Paused = paused;
        foreach (var pair in windows) pair.Value.SetBrightness(paused ? new(100) : values[pair.Key]);
    }
    public void EmergencyReset()
    {
        foreach (var window in windows.Values) window.Dispose();
        windows.Clear();
        foreach (var id in values.Keys.ToArray()) values[id] = new(100);
        Paused = false;
    }
    public void Dispose() => EmergencyReset();
}

public static class AccessStatus
{
    public static bool HasUiAccess()
    {
        using var process = Process.GetCurrentProcess();
        if (!Native.OpenProcessToken(process.Handle, 8, out var token)) return false;
        try { return Native.GetTokenInformation(token, 26, out var value, 4, out _) && value != 0; }
        finally { Native.CloseHandle(token); }
    }
    public static bool RegisterEmergencyHotkey(nint window) => Native.RegisterHotKey(window, 1, 0x4007, 0x7B);
    public static void UnregisterEmergencyHotkey(nint window) => Native.UnregisterHotKey(window, 1);
}
