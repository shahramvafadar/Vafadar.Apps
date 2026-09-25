using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Home;

public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly Translator _translator;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;

    public HomeViewModel(Translator translator, IDateFormatter dates, TimeProvider time)
    {
        _translator = translator;
        _dates = dates;
        _time = time;
        Today = string.Empty;
    }

    [ObservableProperty]
    public partial string Today { get; set; }

    public void Refresh()
    {
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        Today = _translator.Format("Home_Today", _dates.Format(today, DateFormatStyle.Long));
    }
}
