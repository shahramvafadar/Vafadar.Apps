namespace Vafadar.Zanance.App.Startup;

/// <summary>Non-financial startup content retained until database and secure-access initialization succeed.</summary>
public partial class StartupPage : ContentPage
{
    /// <summary>Creates the real loading/retry surface around the host's startup operation.</summary>
    public StartupPage(StartupViewModel viewModel)
    {
        InitializeComponent(); BindingContext = viewModel;
    }
}
