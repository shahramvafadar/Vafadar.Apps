using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>Independent portable formatting, calendar, digits and holiday choices shared by settings and onboarding.</summary>
public sealed partial class RegionalPreferencesViewModel(ILocalizationService localization, Translator translator, TimeProvider time) : ObservableObject
{
    private bool _refreshing;
    private string? _labelsLanguage;

    /// <summary>Gets the available formatting cultures, named independently of the UI language.</summary>
    [ObservableProperty]
    public partial FormattingOption[] Formats { get; set; } = [];

    /// <summary>Gets or sets the independent date/number format.</summary>
    [ObservableProperty]
    public partial FormattingOption? SelectedFormat { get; set; }

    /// <summary>Gets the digit-shape choices.</summary>
    [ObservableProperty]
    public partial string[] DigitNames { get; set; } = [];

    /// <summary>Gets or sets the digit-shape choice.</summary>
    [ObservableProperty]
    public partial int DigitIndex { get; set; }

    /// <summary>Gets regions whose public holiday rules are available.</summary>
    [ObservableProperty]
    public partial RegionOption[] HolidayRegions { get; set; } = [];

    /// <summary>Gets or sets the holiday region without changing numeric formats.</summary>
    [ObservableProperty]
    public partial RegionOption? SelectedHolidayRegion { get; set; }

    /// <summary>Gets the limitations of the selected holiday data.</summary>
    [ObservableProperty]
    public partial string HolidayHint { get; set; } = string.Empty;

    /// <summary>Gets week-start choices.</summary>
    [ObservableProperty]
    public partial WeekStartOption[] WeekStarts { get; set; } = [];

    /// <summary>Gets or sets an explicit week start or the regional default.</summary>
    [ObservableProperty]
    public partial WeekStartOption? SelectedWeekStart { get; set; }

    /// <summary>Gets the live sample date and number.</summary>
    [ObservableProperty]
    public partial string Preview { get; set; } = string.Empty;

    partial void OnSelectedFormatChanged(FormattingOption? value)
    {
        if (!_refreshing && value is not null && value.Name != localization.FormattingCultureName)
        {
            localization.SetFormattingCulture(value.Name);
            Refresh();
        }
    }

    partial void OnDigitIndexChanged(int value)
    {
        if (!_refreshing && Enum.IsDefined((DigitStyle)value) && (DigitStyle)value != localization.CurrentDigits)
        {
            localization.SetDigits((DigitStyle)value);
            Presentation.DigitPreferences.Apply(localization);
            Refresh();
        }
    }

    partial void OnSelectedHolidayRegionChanged(RegionOption? value)
    {
        if (!_refreshing && value is not null && value.Code != localization.CurrentRegion)
        {
            localization.SetRegion(value.Code);
            Refresh();
        }
    }

    partial void OnSelectedWeekStartChanged(WeekStartOption? value)
    {
        if (!_refreshing && value is not null && (value.Day is null ? !localization.IsFirstDayOfWeekAutomatic
                : localization.IsFirstDayOfWeekAutomatic || value.Day != localization.FirstDayOfWeek))
        {
            localization.SetFirstDayOfWeek(value.Day);
            Refresh();
        }
    }

    /// <summary>Refreshes translated labels and samples while retaining explicit choices.</summary>
    public void Refresh()
    {
        _refreshing = true;
        try
        {
            var uiCulture = CultureInfo.GetCultureInfo(localization.CurrentLanguage.CultureName);
            // Keep native picker items stable while a selection is being applied.
            if (_labelsLanguage != uiCulture.Name)
            {
                Formats =
                [
                    new(null, translator["Regional_FollowLanguage"]),
                    .. CultureInfo.GetCultures(CultureTypes.SpecificCultures).Where(c => c.Name.Length > 0)
                        .Select(c => new FormattingOption(c.Name, $"{c.NativeName} · {c.Name}"))
                        .OrderBy(c => c.Title, StringComparer.Create(uiCulture, true)),
                ];
                DigitNames = [translator["Regional_DigitsAutomatic"], translator["Regional_DigitsLatin"], translator["Regional_DigitsPersian"]];
                HolidayRegions =
                [
                    new(null, translator["Regional_NoHolidays"]),
                    .. PublicHolidays.Regions.Select(code => new RegionOption(code, Regions.DisplayName(code, uiCulture))),
                ];
                _labelsLanguage = uiCulture.Name;
            }

            SelectedFormat = Formats.FirstOrDefault(f => f.Name == localization.FormattingCultureName) ?? Formats[0];
            DigitIndex = (int)localization.CurrentDigits;
            // Preserve a legacy region even when its holidays are unavailable; never silently replace its week rules.
            if (localization.CurrentRegion is { } region && !HolidayRegions.Any(option => option.Code == region))
            {
                HolidayRegions = [.. HolidayRegions, new(region, Regions.DisplayName(region, uiCulture))];
            }

            SelectedHolidayRegion = HolidayRegions.FirstOrDefault(r => r.Code == localization.CurrentRegion) ?? HolidayRegions[0];
            HolidayHint = translator[PublicHolidays.IsNationwideOnly(localization.CurrentRegion) ? "Regional_HolidaysGermany"
                : PublicHolidays.HasApproximateDates(localization.CurrentRegion) ? "Regional_HolidaysIran" : "Regional_HolidaysHint"];
            var culture = localization.CurrentCulture;
            var automatic = Regions.FirstDayOfWeek(localization.CurrentRegion, culture);
            WeekStartOption[] weekStarts = [new(null, translator.Format("Settings_WeekStartAutomatic", culture.DateTimeFormat.GetDayName(automatic))),
                .. Enum.GetValues<DayOfWeek>().Select(day => new WeekStartOption(day, culture.DateTimeFormat.GetDayName(day)))];
            if (!WeekStarts.SequenceEqual(weekStarts)) WeekStarts = weekStarts;
            SelectedWeekStart = localization.IsFirstDayOfWeekAutomatic ? WeekStarts[0] : WeekStarts.First(w => w.Day == localization.FirstDayOfWeek);
            var date = new DateFormatter(localization).Format(DateOnly.FromDateTime(time.GetLocalNow().DateTime));
            // Numeric samples remain LTR within Persian prose, including Windows renderers that ignore isolates.
            Preview = translator.Format("Regional_Preview", PreviewValue(date), PreviewValue(1234.56m.ToString("N2", culture)));
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static string PreviewValue(string value) => $"\u2066\u200E{NativeDigits.Apply(value)}\u200E\u2069";
}

/// <summary>A regional formatting choice; null means the language's existing format.</summary>
public sealed record FormattingOption(string? Name, string Title)
{
    /// <inheritdoc />
    public override string ToString() => Title;
}
