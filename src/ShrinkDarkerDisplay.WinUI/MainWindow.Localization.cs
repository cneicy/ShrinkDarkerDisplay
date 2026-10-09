using Microsoft.UI.Xaml.Automation;

namespace ShrinkDarkerDisplay.App;

public sealed partial class MainWindow
{
    private void ApplyLanguage()
    {
        TitleText.Text = text["Title"];
        EnabledLabel.Text = text["Dim"];
        NightLabel.Text = text["DarkMode"];
        SystemLabel.Text = text["FollowSystem"];
        MasterLabel.Text = text["Brightness"];
        AboutButton.Content = text["About"];
        ExitButton.Content = text["Exit"];
        AutomationProperties.SetName(DismissButton, text["Close"]);
        AutomationProperties.SetName(Master, text["Brightness"]);
        AutomationProperties.SetName(EnabledTile, text["Dim"]);
        AutomationProperties.SetName(NightTile, text["DarkMode"]);
        AutomationProperties.SetName(SystemTile, text["FollowSystem"]);
    }
}
