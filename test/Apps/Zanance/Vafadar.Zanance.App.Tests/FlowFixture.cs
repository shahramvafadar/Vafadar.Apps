using System.Resources;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Testing;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Isolated profile data, actual application texts and explicit platform ports for application flow tests.</summary>
internal sealed class FlowFixture : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
    private readonly bool _nativeDigits = Vafadar.Localization.Formatting.NativeDigits.IsEnabled;
    private readonly bool _preserveSeparators = Vafadar.Localization.Formatting.NativeDigits.PreserveSeparators;
    internal readonly TemporaryDirectory Directory = new();
    internal readonly ServiceProvider Services;
    internal readonly InMemorySettingsStore Preferences = new();
    internal readonly Translator Translator = new();
    internal readonly FlowPlatform Platform = new();
    internal readonly ThemePlatform ThemeHost = new();
    internal readonly ThemeService Theme;
    internal readonly LocalizationService Localization;
    internal readonly FixedTime Time = new();
    internal ZananceStore Store => Services.GetRequiredService<ZananceStore>();

    internal FlowFixture()
    {
        Services = new ServiceCollection().AddSingleton<TimeProvider>(Time).AddZananceData(Directory.Combine("zanance.db")).BuildServiceProvider();
        Services.MigrateLocalDatabase<ZananceDbContext>();
        Translator.AddResources(new ResourceManager("Vafadar.Zanance.App.Tests.Resources.AppResources", typeof(FlowFixture).Assembly));
        Localization = new LocalizationService(new LocalizationOptions(), Preferences, Translator);
        Localization.SetLanguage(Localization.SupportedLanguages.First(l => l.CultureName == "en"));
        Theme = new ThemeService(Preferences, ThemeHost);
        Theme.Initialize();
    }

    public void Dispose()
    {
        Services.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Dispose();
        Vafadar.Localization.Formatting.NativeDigits.IsEnabled = _nativeDigits;
        Vafadar.Localization.Formatting.NativeDigits.PreserveSeparators = _preserveSeparators;
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}

/// <summary>A mutable clock which never changes the machine's system time.</summary>
internal sealed class FixedTime : TimeProvider
{
    internal DateTimeOffset Now { get; set; } = new(2026, 10, 9, 9, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>Explicit dialog/navigation answers; no mock application, Shell or MAUI controls.</summary>
internal sealed class FlowPlatform : IAppInteraction, IAppFlowHost
{
    internal int Completed, Restored, ProfileOpened;
    internal readonly List<Exception> Failures = [];
    internal readonly List<(string Title, string Message)> Alerts = [];
    internal readonly Queue<bool> Confirmations = [];
    internal readonly Queue<string?> Inputs = [];
    internal readonly Queue<string?> Choices = [];
    internal readonly List<(string Route, IDictionary<string, object>? Parameters)> Routes = [];
    internal readonly List<string[]> OfferedActions = [];
    internal Func<Task<string?>>? PendingInput;
    public Task AlertAsync(string title, string message, string cancel) { Alerts.Add((title, message)); return Task.CompletedTask; }
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(Confirmations.Count > 0 && Confirmations.Dequeue());
    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => PendingInput?.Invoke() ?? Task.FromResult(Inputs.Count > 0 ? Inputs.Dequeue() : null);
    public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) { OfferedActions.Add(actions); return Task.FromResult(Choices.Count > 0 ? Choices.Dequeue() : null); }
    public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) { Routes.Add((route, parameters)); return Task.CompletedTask; }
    public Task ShowFailureAsync(Exception exception) { Failures.Add(exception); return Task.CompletedTask; }
    public Task RestoreOnboardingAsync() { Restored++; return Task.CompletedTask; }
    public void CompleteOnboarding() => Completed++;
    public void ShowCurrentProfile() => ProfileOpened++;
}

/// <summary>Fake system theme events and native application calls, without a GUI or semantic-color replacement.</summary>
internal sealed class ThemePlatform : IThemeHost
{
    public event EventHandler? SystemThemeChanged;
    public bool IsAvailable { get; set; } = true;
    public bool SystemIsDark { get; set; }
    internal bool RaiseWhileApplying;
    internal readonly List<(ThemeChoice Choice, bool Dark)> Applied = [];
    public void Apply(ThemeChoice choice, bool dark) { Applied.Add((choice, dark)); if (RaiseWhileApplying) { Raise(); } }
    internal void Raise() => SystemThemeChanged?.Invoke(this, EventArgs.Empty);
}
