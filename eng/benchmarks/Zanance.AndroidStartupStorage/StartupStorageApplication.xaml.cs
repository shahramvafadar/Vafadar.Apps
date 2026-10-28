using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Startup;

namespace Vafadar.Zanance.App;

/// <summary>Hosts unmodified production startup XAML/VM with an explicitly synthetic bounded-space fault port.</summary>
public partial class StartupStorageApplication : Application
{
    private readonly ILocalizationService localization;
    private readonly HashSet<string> completed = [];

    /// <summary>Loads original styles without constructing App, database services, accounts or reminders.</summary>
    public StartupStorageApplication(IServiceProvider services)
    {
        InitializeComponent();
        localization = services.GetRequiredService<ILocalizationService>();
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState) => new(new ContentPage());

    /// <summary>Renders one distinct finite language/theme case; repeated accepted cases are refused.</summary>
    internal async void Show(Intent? intent)
    {
        var language = intent?.GetStringExtra("review_language");
        var theme = intent?.GetStringExtra("review_theme");
        if (language is not ("en" or "fa" or "de") || theme is not ("light" or "dark")) return;
        var name = language + "-" + theme;
        if (!completed.Add(name)) throw new IOException("Accepted notice layout cannot be replayed.");
        localization.SetLanguage(localization.SupportedLanguages.Single(value => value.CultureName == language));
        Resources["AppFont"] = language == "fa" ? "Vazirmatn" : "Figtree";
        Resources["DisplayFont"] = language == "fa" ? "VazirmatnBold" : "UrbanistBold";
        UserAppTheme = theme == "dark" ? AppTheme.Dark : AppTheme.Light;
        Palette.Apply(Resources, theme == "dark");
        var vm = new StartupViewModel(() => Task.FromException(new InsufficientDatabaseStorageException()),
            () => throw new IOException("Unexpected key recovery."), () => throw new IOException("Unexpected backup recovery."));
        Windows[0].Page = new StartupPage(vm).WithFlowDirection(localization);
        await vm.InitializeAsync();
        if (!vm.ShowStorageFailure || vm.ShowGenericFailure || vm.RecoveryAvailable || vm.BackupRecoveryAvailable)
            throw new IOException("Original startup storage state refused.");
        await Task.Delay(750);
        var root = Platform.CurrentActivity?.Window?.DecorView ?? throw new IOException("Owned window missing.");
        var texts = Descendants(root).OfType<Android.Widget.TextView>().Where(view => view.Visibility == Android.Views.ViewStates.Visible).ToArray();
        var notice = texts.Single(view => view.Text == Translator.Instance["Startup_StorageLow"]);
        var retry = texts.Single(view => view.Text == Translator.Instance["Startup_Retry"]);
        var layout = notice.Layout ?? throw new IOException("Native notice not arranged.");
        if (layout.LineCount < 1 || layout.GetLineEnd(layout.LineCount - 1) != notice.Text!.Length
            || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0)
            || layout.GetLineBottom(layout.LineCount - 1) > notice.Height - notice.TotalPaddingTop - notice.TotalPaddingBottom
            || retry.Width / DeviceDisplay.Current.MainDisplayInfo.Density < 44
            || retry.Height / DeviceDisplay.Current.MainDisplayInfo.Density < 44)
            throw new IOException("Native complete caption or retry target refused.");
        if (texts.Any(view => view.Text == Translator.Instance["Recovery_UseCode"] || view.Text == Translator.Instance["Recovery_FromBackup"]))
            throw new IOException("Recovery caption remains visible.");
        Android.Util.Log.Info("ZananceStorageNotice", name + "|synthetic-fault|original-xaml-vm|no-financial-services");
        Android.Util.Log.Info("ZananceStorageLayout", System.Text.Json.JsonSerializer.Serialize(new
        {
            Case = name, Lines = layout.LineCount, NoticeWidth = notice.Width, NoticeHeight = notice.Height,
            RetryWidth = retry.Width, RetryHeight = retry.Height, CompleteCaption = true, HiddenRecovery = true,
            Density = DeviceDisplay.Current.MainDisplayInfo.Density, NoFinancialServices = true
        }));
    }

    private static IEnumerable<Android.Views.View> Descendants(Android.Views.View view)
    {
        yield return view;
        if (view is Android.Views.ViewGroup group)
            for (var index = 0; index < group.ChildCount; index++)
                if (group.GetChildAt(index) is { } child)
                    foreach (var descendant in Descendants(child)) yield return descendant;
    }
}
