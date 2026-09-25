using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Hosting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.More;

public sealed partial class MoreViewModel : ViewModelBase
{
    private readonly Translator _translator;
    private readonly IAppEnvironment _app;

    public MoreViewModel(Translator translator, IAppEnvironment app)
    {
        _translator = translator;
        _app = app;
        VersionText = string.Empty;
    }

    [ObservableProperty]
    public partial string VersionText { get; set; }

    public void Refresh() => VersionText = _translator.Format("Settings_Version", _app.Version);

    [RelayCommand]
    private Task OpenAsync(string route) => Shell.Current.GoToAsync(route);
}
