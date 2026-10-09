using System.Globalization;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>Fresh translated display choices; indexes and stored preferences belong to the form, never these labels.</summary>
/// <param name="Modes">Simple and Advanced, in their existing enum order.</param>
/// <param name="Themes">System, Light and Dark, in their existing enum order.</param>
/// <param name="Freshness">Existing 7/30/90/never choices.</param>
/// <param name="StartDays">Calendar month and the supported pay-cycle days.</param>
/// <param name="EssentialPeriods">Day, week and month, in their existing enum order.</param>
internal sealed record SettingsChoiceLabels(IReadOnlyList<string> Modes, IReadOnlyList<string> Themes,
    IReadOnlyList<string> Freshness, IReadOnlyList<string> StartDays, IReadOnlyList<string> EssentialPeriods)
{
    /// <summary>Recreates every choice caption from the current language/number display without saving a selection.</summary>
    internal static SettingsChoiceLabels Create(Translator translator, CultureInfo culture) => new(
        [translator["Mode_Simple"], translator["Mode_Advanced"]],
        [translator["Theme_System"], translator["Theme_Light"], translator["Theme_Dark"]],
        [.. new[] { 7, 30, 90, 0 }.Select(d => d == 0 ? translator["Settings_FreshnessNever"]
            : NativeDigits.Apply(translator.Format("Settings_FreshnessDays", d.ToString(culture)))!)],
        [translator["Settings_MonthStartCalendar"], .. Enumerable.Range(2, Core.Budgets.PeriodMath.MaxStartDay - 1)
            .Select(d => NativeDigits.Apply(translator.Format("Settings_MonthStartDay", d.ToString(culture)))!)],
        [translator["Settings_PerDay"], translator["Settings_PerWeek"], translator["Settings_PerMonth"]]);
}
