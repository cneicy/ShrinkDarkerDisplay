using System.Runtime.InteropServices;

namespace ShrinkDarkerDisplay.Core;

internal static class DisplayNames
{
    internal static Dictionary<string, string> Read()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        const uint activePaths = 2;
        // Hot-plug can change the buffer sizes between the two calls.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (GetDisplayConfigBufferSizes(activePaths, out var pathCount, out var modeCount) != 0) break;
            var paths = new DisplayPath[pathCount];
            var modes = new DisplayMode[modeCount];
            var error = QueryDisplayConfig(activePaths, ref pathCount, paths, ref modeCount, modes, 0);
            if (error == 122) continue; // ERROR_INSUFFICIENT_BUFFER
            if (error != 0) break;

            foreach (var path in paths.Take((int)pathCount))
            {
                var target = new TargetName
                {
                    Header = new DeviceHeader { Type = 2, Size = (uint)Marshal.SizeOf<TargetName>(), Adapter = path.Target.Adapter, Id = path.Target.Id },
                    FriendlyName = "", DevicePath = ""
                };
                if (GetTargetName(ref target) != 0 || string.IsNullOrWhiteSpace(target.FriendlyName)) continue;
                names[target.DevicePath] = target.FriendlyName.Trim();
            }
            break;
        }
        return names;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Source { public Luid Adapter; public uint Id, ModeIndex, Status; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Target
    {
        public Luid Adapter;
        public uint Id, ModeIndex, Technology, Rotation, Scaling;
        public uint RefreshNumerator, RefreshDenominator, ScanLineOrdering;
        public int Available;
        public uint Status;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayPath { public Source Source; public Target Target; public uint Flags; }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private struct DisplayMode { }
    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceHeader { public uint Type, Size; public Luid Adapter; public uint Id; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct TargetName
    {
        public DeviceHeader Header;
        public uint Flags, Technology;
        public ushort Manufacturer, Product;
        public uint Connector;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string FriendlyName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DevicePath;
    }
    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint paths, out uint modes);
    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DisplayPath[] paths, ref uint modeCount, [Out] DisplayMode[] modes, nint topology);
    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo", CharSet = CharSet.Unicode)]
    private static extern int GetTargetName(ref TargetName name);
}
