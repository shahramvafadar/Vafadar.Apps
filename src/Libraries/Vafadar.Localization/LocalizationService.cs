using System.Globalization;
using Vafadar.Core.Settings;

namespace Vafadar.Localization;

/// <summary>
/// Default <see cref="ILocalizationService"/>: stores the choice in <see cref="ISettingsStore"/> and applies it to the
/// process cultures and the <see cref="Translator"/>.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    /// <summary>The portable display choices; never includes tokens, PINs or device security preferences.</summary>
    public static IReadOnlyList<string> PortableKeys { get; } =
        [LanguageKey, CalendarKey, RegionKey, FirstDayKey, FormattingKey, DigitsKey];

    internal const string LanguageKey = "localization.language";
    internal const string CalendarKey = "localization.calendar";
    internal const string RegionKey = "localization.region";
    internal const string FirstDayKey = "localization.firstDayOfWeek";
    internal const string FormattingKey = "localization.formattingCulture";
    internal const string DigitsKey = "localization.digits";

    private readonly LocalizationOptions _options;
    private readonly ISettingsStore _settings;
    private readonly Translator _translator;
    private readonly CultureInfo _deviceCulture;

    /// <summary>Creates the service and registers the app resources with the translator.</summary>
    public LocalizationService(LocalizationOptions options, ISettingsStore settings, Translator translator)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(translator);

        if (options.SupportedLanguages.Count == 0)
        {
            throw new ArgumentException("At least one language must be supported.", nameof(options));
        }

        _options = options;
        _settings = settings;
        _translator = translator;
        _deviceCulture = CultureInfo.CurrentUICulture;

        SupportedLanguages = [.. options.SupportedLanguages];
        CurrentLanguage = options.DefaultLanguage;
        CurrentCalendar = options.DefaultCalendar(options.DefaultLanguage);
        CurrentCulture = CultureFactory.Create(CurrentLanguage, CurrentCalendar);

        // Registered in reverse so that the first configured resource has the highest priority.
        foreach (var resources in options.Resources.Reverse())
        {
            translator.AddResources(resources);
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public IReadOnlyList<AppLanguage> SupportedLanguages { get; }

    /// <inheritdoc />
    public AppLanguage CurrentLanguage { get; private set; }

    /// <inheritdoc />
    public CalendarSystem CurrentCalendar { get; private set; }

    /// <inheritdoc />
    public CultureInfo CurrentCulture { get; private set; }

    /// <inheritdoc />
    public string? FormattingCultureName { get; private set; }

    /// <inheritdoc />
    public DigitStyle CurrentDigits { get; private set; }

    /// <inheritdoc />
    public bool IsRightToLeft => CurrentLanguage.IsRightToLeft;

    /// <inheritdoc />
    public string? CurrentRegion { get; private set; }

    /// <inheritdoc />
    public string? SuggestedRegion
    {
        get
        {
            try
            {
                var code = new RegionInfo(_deviceCulture.Name).TwoLetterISORegionName;
                return Regions.IsKnown(code) ? code.ToUpperInvariant() : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public DayOfWeek FirstDayOfWeek => CurrentCulture.DateTimeFormat.FirstDayOfWeek;

    /// <inheritdoc />
    public bool IsFirstDayOfWeekAutomatic => SavedFirstDay() is null;

    /// <inheritdoc />
    public void Initialize()
    {
        var language = FindLanguage(_settings.Get(LanguageKey))
            ?? FindLanguage(_deviceCulture.Name)
            ?? FindLanguage(_deviceCulture.TwoLetterISOLanguageName)
            ?? FindLanguage(_options.DefaultLanguage.CultureName)
            ?? SupportedLanguages[0];

        var region = _settings.Get(RegionKey);
        CurrentRegion = Regions.IsKnown(region) ? region!.ToUpperInvariant() : null;
        FormattingCultureName = ValidFormattingCulture(_settings.Get(FormattingKey));
        CurrentDigits = Enum.TryParse<DigitStyle>(_settings.Get(DigitsKey), out var digits) && Enum.IsDefined(digits)
            ? digits : DigitStyle.LanguageDefault;
        Apply(language, SavedCalendar() ?? _options.DefaultCalendar(language));
    }

    /// <inheritdoc />
    public void SetLanguage(AppLanguage language)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (!SupportedLanguages.Contains(language))
        {
            throw new ArgumentException($"Language '{language.CultureName}' is not supported by this app.", nameof(language));
        }

        _settings.Set(LanguageKey, language.CultureName);

        // Until the user picks a calendar explicitly, the calendar follows the language.
        Apply(language, SavedCalendar() ?? _options.DefaultCalendar(language));
    }

    /// <inheritdoc />
    public void SetCalendar(CalendarSystem calendar)
    {
        if (!Enum.IsDefined(calendar))
        {
            throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "Unknown calendar system.");
        }

        _settings.Set(CalendarKey, calendar.ToString());
        Apply(CurrentLanguage, calendar);
    }

    /// <inheritdoc />
    public void SetRegion(string? region)
    {
        if (!string.IsNullOrEmpty(region) && !Regions.IsKnown(region))
        {
            throw new ArgumentException($"Unknown region '{region}'.", nameof(region));
        }

        CurrentRegion = string.IsNullOrEmpty(region) ? null : region.ToUpperInvariant();
        _settings.Set(RegionKey, CurrentRegion);
        Apply(CurrentLanguage, CurrentCalendar);
    }

    /// <inheritdoc />
    public void SetFirstDayOfWeek(DayOfWeek? day)
    {
        if (day is { } value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "Unknown day of the week.");
        }

        _settings.Set(FirstDayKey, day?.ToString());
        Apply(CurrentLanguage, CurrentCalendar);
    }

    /// <inheritdoc />
    public void SetFormattingCulture(string? cultureName)
    {
        var valid = ValidFormattingCulture(cultureName);
        if (!string.IsNullOrEmpty(cultureName) && valid is null)
        {
            throw new ArgumentException("A supported specific culture is required.", nameof(cultureName));
        }

        FormattingCultureName = valid;
        _settings.Set(FormattingKey, valid);
        Apply(CurrentLanguage, CurrentCalendar);
    }

    /// <inheritdoc />
    public void SetDigits(DigitStyle digits)
    {
        if (!Enum.IsDefined(digits))
        {
            throw new ArgumentOutOfRangeException(nameof(digits));
        }

        CurrentDigits = digits;
        _settings.Set(DigitsKey, digits.ToString());
        Apply(CurrentLanguage, CurrentCalendar);
    }

    private static string? ValidFormattingCulture(string? name) =>
        string.IsNullOrEmpty(name) ? null : CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Name;

    private void Apply(AppLanguage language, CalendarSystem calendar)
    {
        var culture = CultureFactory.Create(language, calendar, FormattingCultureName);
        culture.DateTimeFormat.FirstDayOfWeek = SavedFirstDay() ?? Regions.FirstDayOfWeek(CurrentRegion, culture);

        CurrentLanguage = language;
        CurrentCalendar = calendar;
        CurrentCulture = culture;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        var uiCulture = CultureInfo.GetCultureInfo(language.CultureName);
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;

        _translator.SetCulture(culture);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private AppLanguage? FindLanguage(string? cultureName) =>
        string.IsNullOrEmpty(cultureName)
            ? null
            : SupportedLanguages.FirstOrDefault(l => string.Equals(l.CultureName, cultureName, StringComparison.OrdinalIgnoreCase));

    private DayOfWeek? SavedFirstDay() =>
        Enum.TryParse<DayOfWeek>(_settings.Get(FirstDayKey), ignoreCase: true, out var day) && Enum.IsDefined(day) ? day : null;

    private CalendarSystem? SavedCalendar() =>
        Enum.TryParse<CalendarSystem>(_settings.Get(CalendarKey), ignoreCase: true, out var calendar) && Enum.IsDefined(calendar)
            ? calendar
            : null;
}
