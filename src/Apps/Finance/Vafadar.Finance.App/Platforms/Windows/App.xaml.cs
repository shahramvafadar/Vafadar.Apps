namespace Vafadar.Finance.App.WinUI;

/// <summary>
/// The WinUI application that hosts the MAUI app on Windows.
/// </summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
