using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ShrinkDarkerDisplay.Core;

public sealed record Display(string Id, string Name, int X, int Y, int Width, int Height, bool Primary);

public static class DisplayCatalog
{
    public static IReadOnlyList<Display> Enumerate()
    {
        var displays = new List<Display>();
        var names = DisplayNames.Read();
        Native.MonitorProc callback = (nint monitor, nint dc, ref Native.Rect rect, nint data) =>
        {
            var info = new Native.MonitorInfo { Size = (uint)Marshal.SizeOf<Native.MonitorInfo>(), Device = "" };
            if (!Native.GetMonitorInfoW(monitor, ref info)) throw new Win32Exception();
            var device = new Native.DisplayDevice { Size = (uint)Marshal.SizeOf<Native.DisplayDevice>(), Name = "", Description = "", Id = "", Key = "" };
            var found = Native.EnumDisplayDevicesW(info.Device, 0, ref device, 1);
            // EDD_GET_DEVICE_INTERFACE_NAME returns a monitor instance path, unlike DISPLAY1's transient numbering.
            var id = found && !string.IsNullOrWhiteSpace(device.Id) ? device.Id : info.Device;
            var name = names.TryGetValue(id, out var friendlyName) ? friendlyName : found ? device.Description : info.Device;
            displays.Add(new(id, name, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, (info.Flags & 1) != 0));
            return true;
        };
        if (!Native.EnumDisplayMonitors(0, 0, callback, 0)) throw new Win32Exception();
        return displays;
    }
}
