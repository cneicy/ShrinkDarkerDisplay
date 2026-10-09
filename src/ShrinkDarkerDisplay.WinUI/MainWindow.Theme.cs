using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace ShrinkDarkerDisplay.App;

public sealed partial class MainWindow
{
    private void ApplyTheme()
    {
        Root.RequestedTheme = settings.FollowSystemTheme ? ElementTheme.Default : settings.NightMode ? ElementTheme.Dark : ElementTheme.Light;
        UpdateWindowTheme();
        SystemTile.IsChecked = settings.FollowSystemTheme;
        NightTile.IsChecked = settings.FollowSystemTheme ? Root.ActualTheme == ElementTheme.Dark : settings.NightMode;
        UpdateWindowTheme();
    }
    private void UpdateWindowTheme()
    {
        if (hwnd == 0) return;
        bool dark = Root.ActualTheme == ElementTheme.Dark;
        int mode = dark ? 1 : 0, border = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE
        Shell.DwmSetWindowAttribute(hwnd, 20, ref mode, 4);
        Shell.DwmSetWindowAttribute(hwnd, 34, ref border, 4);
        SolidColorBrush Brush(byte r, byte g, byte b) => new(Windows.UI.Color.FromArgb(255, r, g, b));
        foreach (var button in new ButtonBase[] { EnabledTile, NightTile, SystemTile, AboutButton, ExitButton, DismissButton })
        {
            bool selected = button is ToggleButton toggle && toggle.IsChecked == true;
            button.Background = selected ? (dark ? Brush(96, 205, 255) : Brush(0, 103, 192)) : (dark ? Brush(48, 48, 48) : Brush(250, 250, 250));
            button.Foreground = selected ? (dark ? Brush(0, 0, 0) : Brush(255, 255, 255)) : (dark ? Brush(245, 245, 245) : Brush(25, 25, 25));
            button.BorderBrush = selected ? button.Background : (dark ? Brush(65, 65, 65) : Brush(222, 222, 222));
        }
    }
}
