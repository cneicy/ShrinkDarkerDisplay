using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using ShrinkDarkerDisplay.Core;

namespace ShrinkDarkerDisplay.App;

public sealed partial class MainWindow : Window
{
    private readonly Session session = new();
    private readonly SettingsStore store;
    private Settings settings = new();
    private readonly Dictionary<string, Slider> sliders = new();
    private readonly Shell.Subclass callback;
    private readonly nint hwnd;
    private readonly uint taskbarCreated;
    private Shell.NotifyData icon;
    private PopupWatcher? watcher;
    private bool ready, exiting, visible, hiding, canSave = true, wasPaused;
    private DateTime lastDismiss;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer saveTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer refreshTimer;
    private readonly LocalizedText text = new(Path.Combine(AppContext.BaseDirectory, "Localization"), CultureInfo.CurrentUICulture.Name);
    public bool StartMinimized => settings.StartMinimized;
    public MainWindow(string? settingsPath = null)
    {
        store = new(settingsPath ?? SettingsStore.DefaultPath);
        InitializeComponent();
        Title = "ShrinkDarkerDisplay";
        Root.ActualThemeChanged += (_, _) => { if (settings.FollowSystemTheme) NightTile.IsChecked = Root.ActualTheme == ElementTheme.Dark; UpdateWindowTheme(); };
        try { settings = store.Load(); } catch (Exception e) { canSave = false; Status.Text = text["SettingsLoadError"] + e.Message; }
        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        callback = WindowMessage;
        Shell.SetWindowSubclass(hwnd, callback, 1, 0);
        taskbarCreated = Shell.RegisterWindowMessageW("TaskbarCreated");
        var presenter = OverlappedPresenter.Create();
        AppWindow.SetPresenter(presenter);
        presenter.SetBorderAndTitleBar(false, false); presenter.IsResizable = false; presenter.IsMaximizable = false; presenter.IsMinimizable = false;
        var style = Shell.GetWindowLongPtrW(hwnd, -16).ToInt64();
        Shell.SetWindowLongPtrW(hwnd, -16, (nint)(style & ~0x00C40000L)); // WS_CAPTION | WS_THICKFRAME
        Shell.SetWindowPos(hwnd, 0, 0, 0, 0, 0, 0x0037); // FRAMECHANGED, without activation/move/resize/z-order
        AppWindow.IsShownInSwitchers = false;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        int corner = 2; Shell.DwmSetWindowAttribute(hwnd, 33, ref corner, 4);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ShrinkDarkerDisplay.ico");
        AppWindow.SetIcon(iconPath);
        var trayIcon = Shell.LoadImageW(0, iconPath, 1, 32, 32, 0x10);
        if (trayIcon == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        icon = new Shell.NotifyData { Size = (uint)Marshal.SizeOf<Shell.NotifyData>(), Window = hwnd, Id = 1, Flags = 7, Callback = 0x8001, Icon = trayIcon, Tip = "ShrinkDarkerDisplay", Info = "", Title = "" };
        Shell.Shell_NotifyIconW(0, ref icon);
        saveTimer = DispatcherQueue.CreateTimer(); saveTimer.Interval = TimeSpan.FromMilliseconds(400); saveTimer.IsRepeating = false; saveTimer.Tick += (_, _) => Save();
        refreshTimer = DispatcherQueue.CreateTimer(); refreshTimer.Interval = TimeSpan.FromMilliseconds(300); refreshTimer.IsRepeating = false; refreshTimer.Tick += (_, _) => Refresh();
        Activated += (_, e) => { if (e.WindowActivationState == WindowActivationState.Deactivated && visible && !exiting) Dismiss(); };
        AppWindow.Closing += (_, e) => { if (!exiting) { e.Cancel = true; Dismiss(); } };
        Closed += (_, _) => Cleanup();
        try { watcher = new PopupWatcher(() => { if (!exiting) Run(session.EnsureAbovePopups); }); } catch (Exception e) { Status.Text = e.Message; }
        ApplyLanguage(); Refresh(); ApplyTheme();
        if (!AccessStatus.RegisterEmergencyHotkey(hwnd)) Status.Text = text["HotkeyUnavailable"];
        ready = true;
    }
    private void Run(Action action) { try { action(); } catch (Exception e) { session.EmergencyReset(); Status.Text = text["BrightnessRestored"] + e.Message; } }
    private void Refresh()
    {
        var previous = ready; ready = false;
        Run(() =>
        {
            session.Refresh(); DisplayRows.Children.Clear(); sliders.Clear();
            foreach (var d in session.Displays)
            {
                var value = settings.ForDisplay(d.Id).Percent;
                var primary = text["PrimaryDisplay"];
                var enabled = new CheckBox { Content = d.Name + (d.Primary ? primary : ""), IsChecked = !settings.DisabledDisplays.Contains(d.Id) };
                var row = new StackPanel { Spacing = 4 };
                var label = new TextBlock { Text = $"{value}%", HorizontalAlignment = HorizontalAlignment.Right };
                var slider = new Slider { Minimum = 10, Maximum = 100, Value = value, IsEnabled = enabled.IsChecked == true };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(slider, d.Name + text["DisplayBrightness"]);
                sliders.Add(d.Id, slider);
                session.Set(d.Id, new(slider.IsEnabled ? value : 100));
                slider.ValueChanged += (_, _) => { if (!ready) return; int v = (int)Math.Round(slider.Value); label.Text = $"{v}%"; settings.Displays[d.Id] = v; Run(() => session.Set(d.Id, new(slider.IsEnabled ? v : 100))); QueueSave(); };
                enabled.Click += (_, _) => { slider.IsEnabled = enabled.IsChecked == true; settings.DisabledDisplays.Remove(d.Id); if (!slider.IsEnabled) settings.DisabledDisplays.Add(d.Id); Run(() => session.Set(d.Id, new(slider.IsEnabled ? (int)slider.Value : 100))); QueueSave(); };
                row.Children.Add(enabled); row.Children.Add(label); row.Children.Add(slider); DisplayRows.Children.Add(row);
            }
            Master.Value = sliders.Values.FirstOrDefault()?.Value ?? 100;
            MasterValue.Text = $"{Master.Value:0}%";
        });
        ready = previous;
    }
    private void QueueSave() { saveTimer.Stop(); if (canSave) saveTimer.Start(); }
    private void Save() { if (!canSave) return; try { store.Save(settings); } catch (Exception e) { Status.Text = text["SettingsSaveError"] + e.Message; } }
    private void MasterChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (MasterValue is not null) MasterValue.Text = $"{e.NewValue:0}%";
        if (!ready) return;
        foreach (var slider in sliders.Values) slider.Value = Math.Round(e.NewValue);
    }
    private void EnabledClick(object sender, RoutedEventArgs e) { Run(() => session.SetPaused(EnabledTile.IsChecked != true)); ApplyTheme(); }
    private void NightClick(object sender, RoutedEventArgs e) { settings.FollowSystemTheme = false; settings.NightMode = NightTile.IsChecked == true; ApplyTheme(); QueueSave(); }
    private void SystemClick(object sender, RoutedEventArgs e) { settings.FollowSystemTheme = SystemTile.IsChecked == true; ApplyTheme(); QueueSave(); }
    private void Restore()
    {
        session.EmergencyReset(); foreach (var id in settings.Displays.Keys.ToArray()) settings.Displays[id] = 100;
        EnabledTile.IsChecked = true; Refresh(); QueueSave();
    }
    private void ExitClick(object sender, RoutedEventArgs e) { exiting = true; Close(); Application.Current.Exit(); }
    private nint WindowMessage(nint window, uint msg, nint wp, nint lp, nuint id, nuint data)
    {
        // A borderless presenter can retain non-client insets. Let XAML occupy the full window.
        if (msg == 0x0083 && wp != 0) return 0; // WM_NCCALCSIZE
        if (msg == 0x8001 && ((int)lp == 0x202 || (int)lp == 0x205))
        {
            if (visible) Dismiss(); else if ((DateTime.UtcNow - lastDismiss).TotalMilliseconds > 250) Open();
            return 0;
        }
        if (msg == taskbarCreated) Shell.Shell_NotifyIconW(0, ref icon);
        if (msg == 0x312 && wp == 1) Restore();
        if (msg == 0x7e || msg == 0x219) { refreshTimer.Stop(); refreshTimer.Start(); }
        if (msg == 0x218 && wp == 4) { wasPaused = session.Paused; Run(() => session.SetPaused(true)); }
        if (msg == 0x218 && (wp == 7 || wp == 18)) { Run(() => session.SetPaused(wasPaused)); Refresh(); }
        if (msg == 0x1a) DispatcherQueue.TryEnqueue(ApplyTheme);
        return Shell.DefSubclassProc(window, msg, wp, lp);
    }
    private void Cleanup()
    {
        exiting = true; motion?.Stop(); saveTimer.Stop(); refreshTimer.Stop(); Save(); watcher?.Dispose(); session.Dispose();
        AccessStatus.UnregisterEmergencyHotkey(hwnd); Shell.Shell_NotifyIconW(2, ref icon); Shell.DestroyIcon(icon.Icon); Shell.RemoveWindowSubclass(hwnd, callback, 1);
    }
}
