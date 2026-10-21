using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Runs the original native amount prompt without giving a display change financial authority.</summary>
public static class ForecastAmountPrompt
{
    /// <summary>Returns only a positive explicit assumption; cancellation or invalid input keeps the scenario unchanged.</summary>
    public static async Task<long?> ReadAsync(string currency, CultureInfo culture, long? initial,
        Func<string, string, Task<string?>> prompt)
    {
        ArgumentNullException.ThrowIfNull(culture); ArgumentNullException.ThrowIfNull(prompt);
        // A native prompt keeps its original caption until dismissed; parse its response in that same unit/format.
        var original = MoneyInputContext.Capture(currency, culture);
        var raw = await prompt(original.UnitName, initial is { } amount ? original.ForInput(amount) : string.Empty);
        return raw is not null && original.TryParse(raw, out var assumed) && assumed > 0 ? assumed : null;
    }
}
