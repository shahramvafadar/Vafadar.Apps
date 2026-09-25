using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Maui.Localization;

namespace Vafadar.Finance.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var localization = _services.GetRequiredService<ILocalizationService>();
        var shell = _services.GetRequiredService<AppShell>().WithFlowDirection(localization);

        return new Window(shell) { Title = Translator.Instance["App_Name"] };
    }
}
