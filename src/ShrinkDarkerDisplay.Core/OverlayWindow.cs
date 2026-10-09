using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ShrinkDarkerDisplay.Core;

public sealed class OverlayWindow : IDisposable
{
    private static readonly Native.WindowProc Procedure = WndProc;
    private const string ClassName = "ShrinkDarkerDisplay.Overlay";
    private static bool registered;
    public nint Handle { get; private set; }
    public Display Display { get; private set; }
    private Brightness? appliedBrightness;
    public bool CaptureExcluded => Native.GetWindowDisplayAffinity(Handle, out var value) && value == 0x11;
    public OverlayWindow(Display display)
    {
        Display = display;
        EnsureClass();
        Handle = Native.CreateWindowExW(0x080800A8, ClassName, "ShrinkDarkerDisplay dimming surface", 0x80000000,
            display.X, display.Y, display.Width, display.Height, 0, 0, Native.GetModuleHandleW(null), 0);
        if (Handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            // Keep the dimming surface visible while another window is being peeked.
            int enabled = 1;
            Marshal.ThrowExceptionForHR(Native.DwmSetWindowAttribute(Handle, 12, ref enabled, 4));
        }
        catch { Dispose(); throw; }
        if (!Native.SetWindowDisplayAffinity(Handle, 0x11))
        {
            var error = Marshal.GetLastWin32Error(); Dispose();
            throw new Win32Exception(error, "无法排除调暗窗口的截图捕获，已停止调暗。");
        }
    }
    internal static void EnsureClass()
    {
        if (registered) return;
        var wc = new Native.WindowClass { Proc = Procedure, Name = ClassName, Instance = Native.GetModuleHandleW(null), Background = Native.GetStockObject(4) };
        if (Native.RegisterClassW(ref wc) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        registered = true;
    }
    private static nint WndProc(nint hwnd, uint msg, nint wp, nint lp) => msg switch
    {
        0x0084 => -1, // HTTRANSPARENT
        0x0021 => 3, // MA_NOACTIVATE
        _ => Native.DefWindowProcW(hwnd, msg, wp, lp)
    };
    public void SetBrightness(Brightness brightness)
    {
        ObjectDisposedException.ThrowIf(Handle == 0, this);
        if (appliedBrightness == brightness) return;
        if (!Native.SetLayeredWindowAttributes(Handle, 0, brightness.OverlayAlpha, 2)) throw new Win32Exception();
        if (brightness.Percent == 100) Native.ShowWindow(Handle, 0);
        else if (!Native.SetWindowPos(Handle, -1, Display.X, Display.Y, Display.Width, Display.Height, 0x0050)) throw new Win32Exception();
        appliedBrightness = brightness;
    }
    internal void UpdateDisplay(Display display)
    {
        ObjectDisposedException.ThrowIf(Handle == 0, this);
        if (display.Id != Display.Id) throw new ArgumentException("显示设备标识不能改变。", nameof(display));
        if ((Display.X != display.X || Display.Y != display.Y || Display.Width != display.Width || Display.Height != display.Height)
            && !Native.SetWindowPos(Handle, 0, display.X, display.Y, display.Width, display.Height, 0x0014))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        Display = display;
    }
    public void Dispose() { if (Handle != 0) { Native.DestroyWindow(Handle); Handle = 0; } }
    public void EnsureAbovePopups()
    {
        if (Handle == 0 || !Native.IsWindowVisible(Handle)) return;
        // Skip our own surfaces. Reorder only when a visible foreign window is actually above us.
        for (var above = Native.GetWindow(Handle, 3); above != 0; above = Native.GetWindow(above, 3))
        {
            if (!Native.IsWindowVisible(above)) continue;
            Native.GetWindowThreadProcessId(above, out var pid);
            if (pid == Environment.ProcessId) continue;
            // Reorder without asking the candidate window to repaint; this avoids a visible flash.
            if (!Native.SetWindowPos(Handle, -1, 0, 0, 0, 0, 0x001B)) throw new Win32Exception();
            break;
        }
    }
}

