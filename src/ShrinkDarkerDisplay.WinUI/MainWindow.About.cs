using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace ShrinkDarkerDisplay.App;

public sealed partial class MainWindow
{
    private async void AboutClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = text["AboutTitle"],
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = text["AboutBody"].Replace("{version}", typeof(App).Assembly.GetName().Version?.ToString(3) ?? ""), TextWrapping = TextWrapping.Wrap },
                    new HyperlinkButton { Content = "github.com/cneicy/ShrinkDarkerDisplay", Tag = "https://github.com/cneicy/ShrinkDarkerDisplay" }
                }
            },
            CloseButtonText = text["Close"],
            XamlRoot = Content.XamlRoot
        };
        if (dialog.Content is StackPanel panel && panel.Children[1] is HyperlinkButton link)
            link.Click += async (_, _) => await Launcher.LaunchUriAsync(new Uri((string)link.Tag));
        await dialog.ShowAsync();
    }
}
