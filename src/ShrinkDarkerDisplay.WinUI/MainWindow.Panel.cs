using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace ShrinkDarkerDisplay.App;

public sealed partial class MainWindow
{
    private PointInt32 restingPosition;
    private int edgeX;
    public void Open()
    {
        if (exiting) return;
        motion?.Stop(); hiding = false; visible = true;
        Shell.GetCursorPos(out var point);
        var info = new Shell.MonitorInfo { Size = (uint)Marshal.SizeOf<Shell.MonitorInfo>() };
        Shell.GetMonitorInfoW(Shell.MonitorFromPoint(point, 2), ref info);
        AppWindow.Move(new PointInt32(info.Work.Right - 400, info.Work.Top + 10));
        var scale = Shell.GetDpiForWindow(hwnd) / 96.0;
        int w = (int)(360 * scale), h = Math.Min((int)(500 * scale), info.Work.Bottom - info.Work.Top - 20);
        restingPosition = new PointInt32(info.Work.Right - w - 10, info.Work.Bottom - h - 10);
        edgeX = info.Work.Right;
        AppWindow.MoveAndResize(new RectInt32(edgeX, restingPosition.Y, w, h));
        Activate(); Shell.SetForegroundWindow(hwnd); Animate(false);
    }
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? motion;
    private void Animate(bool closing)
    {
        motion?.Stop();
        int from = AppWindow.Position.X, to = closing ? edgeX : restingPosition.X;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var timer = DispatcherQueue.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(16);
        motion = timer;
        timer.Tick += (_, _) =>
        {
            double t = Math.Min(1, watch.Elapsed.TotalMilliseconds / (closing ? 160 : 200));
            double ease = closing ? t * t : 1 - Math.Pow(1 - t, 3);
            AppWindow.Move(new PointInt32((int)Math.Round(from + (to - from) * ease), restingPosition.Y));
            if (t < 1) return;
            timer.Stop();
            if (closing && hiding && !exiting) { AppWindow.Hide(); visible = false; hiding = false; }
        };
        timer.Start();
    }

    private void Dismiss() { if (!visible || hiding) return; hiding = true; lastDismiss = DateTime.UtcNow; Animate(true); }
    private void DismissClick(object sender, RoutedEventArgs e) => Dismiss();
}
