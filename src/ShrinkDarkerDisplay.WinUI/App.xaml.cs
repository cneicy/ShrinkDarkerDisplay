using Microsoft.UI.Xaml;
namespace ShrinkDarkerDisplay.App;

public partial class App : Application
{
    private Mutex? instance;
    private MainWindow? window;
    public App() { UnhandledException += (_, e) => Log(e.Exception); InitializeComponent(); }
    private static void Log(Exception e) { var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShrinkDarkerDisplay"); Directory.CreateDirectory(p); File.WriteAllText(Path.Combine(p, "winui-error.txt"), e.ToString()); }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        instance = new Mutex(true, @"Local\ShrinkDarkerDisplay.App", out var first);
        if (!first) { Exit(); return; }
        try
        {
            window = new MainWindow();
            if (!window.StartMinimized) window.Open();
        }
        catch (Exception e) { Log(e); Exit(); }
    }
}

