using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ShrinkDarkerDisplay.Core;

/// <summary>Notify the owning UI thread when a foreign popup or foreground window changes.</summary>
public sealed class PopupWatcher : IDisposable
{
    private readonly Native.WinEventProc callback;
    private readonly List<nint> hooks = [];
    public PopupWatcher(Action windowChanged)
    {
        callback = (_, _, hwnd, objectId, _, _, _) =>
        {
            // Candidate updates may be reported on the client or caret, not OBJID_WINDOW.
            if (hwnd == 0 || objectId == -9 || !Native.IsWindowVisible(hwnd)) return;
            Native.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == Environment.ProcessId) return;
            windowChanged();
        };
        try
        {
            Add(3, 3); // EVENT_SYSTEM_FOREGROUND
            Add(0x8002, 0x8002); // EVENT_OBJECT_SHOW
            Add(0x8004, 0x8004); // EVENT_OBJECT_REORDER
            Add(0x800B, 0x800C); // LOCATIONCHANGE, NAMECHANGE (reused candidate windows)
            Add(0x800E, 0x800E); // VALUECHANGE (composition updates)
            Add(0x8027, 0x8029); // IME_SHOW, IME_HIDE, IME_CHANGE
        }
        catch { Dispose(); throw; }
    }
    private void Add(uint first, uint last)
    {
        var hook = Native.SetWinEventHook(first, last, 0, callback, 0, 0, 2); // OUTOFCONTEXT | SKIPOWNPROCESS
        if (hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        hooks.Add(hook);
    }
    public void Dispose() { foreach (var hook in hooks) Native.UnhookWinEvent(hook); hooks.Clear(); }
}
