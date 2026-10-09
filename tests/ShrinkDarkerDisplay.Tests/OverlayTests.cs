using System.Runtime.InteropServices;
using ShrinkDarkerDisplay.Core;

namespace ShrinkDarkerDisplay.Tests;

public sealed class OverlayTests
{
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);
    [Fact]
    [Trait("Category", "Desktop")]
    public void OverlayLifecycleExcludesCaptureAndDestroysWindows()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var session = new Session();
                session.Refresh();
                Assert.NotEmpty(session.Displays);
                foreach (var display in session.Displays)
                {
                    using var overlay = new OverlayWindow(display);
                    Assert.True(overlay.CaptureExcluded);
                    var handle = overlay.Handle;
                    overlay.SetBrightness(new(100)); overlay.Dispose();
                    Assert.False(IsWindow(handle));
                }
                var handles = session.Handles.ToArray();
                foreach (var display in session.Displays) session.Set(display.Id, new(99));
                for (var i = 0; i < 20; i++) session.Refresh();
                Assert.Equal(handles, session.Handles.ToArray());
                foreach (var handle in handles)
                {
                    Assert.True(IsWindowVisible(handle));
                    Assert.True(GetLayeredWindowAttributes(handle, out _, out var alpha, out _));
                    Assert.Equal(new Brightness(99).OverlayAlpha, alpha);
                }
                session.SetPaused(true);
                session.Refresh();
                Assert.True(session.Paused);
                Assert.Equal(handles, session.Handles.ToArray());
                Assert.All(handles, h => Assert.False(IsWindowVisible(h)));
                handles = session.Handles.ToArray();
                session.EmergencyReset();
                Assert.False(session.Paused);
                Assert.All(handles, h => Assert.False(IsWindow(h)));
            }
            catch (Exception e) { failure = e; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        Assert.Null(failure);
    }
}
