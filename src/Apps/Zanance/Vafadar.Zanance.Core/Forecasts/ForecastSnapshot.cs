using System.Globalization;
using Vafadar.Core.Domain;

namespace Vafadar.Zanance.Core.Forecasts;

/// <summary>
/// A saved forecast (ZEX-P22, ZEX-S0803): the path of one currency for the usable accounts as it was computed on the base
/// date, with its scope and assumptions. Read-only after saving – later plan changes never touch it (ZEX-AT40), so it
/// can be compared with what really happened.
/// </summary>
public sealed class ForecastSnapshot : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the name the user gave it, e.g. "Before the move".</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the day the forecast starts from (today when saved).</summary>
    public DateOnly BaseDate { get; set; }

    /// <summary>Gets or sets the last day of the path.</summary>
    public DateOnly Horizon { get; set; }

    /// <summary>Gets or sets the currency of the path.</summary>
    public required string CurrencyCode { get; set; }

    /// <summary>Gets or sets the accounts of the scope, comma-separated ids.</summary>
    public required string AccountIds { get; set; }

    /// <summary>Gets or sets the daily balances from the base date to the horizon, comma-separated minor units.</summary>
    public required string Path { get; set; }

    /// <summary>Gets or sets the lowest balance of the path.</summary>
    public long Minimum { get; set; }

    /// <summary>Gets or sets the first day of the lowest balance.</summary>
    public DateOnly MinimumDate { get; set; }

    /// <summary>Gets or sets the number of plan items with an unknown amount (the path was incomplete).</summary>
    public int UnknownCount { get; set; }

    /// <summary>Gets or sets the what-if assumptions as the user saw them, e.g. "Rent excluded"; empty without any.</summary>
    public string? Assumptions { get; set; }

    /// <summary>Gets or sets the app version that saved it.</summary>
    public string? AppVersion { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Returns the account ids of the scope.</summary>
    public IReadOnlyList<Guid> Scope() => [.. AccountIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse)];

    /// <summary>Returns the saved path as points.</summary>
    public IReadOnlyList<ForecastPoint> Points() =>
        [.. Path.Split(',', StringSplitOptions.RemoveEmptyEntries).Select((value, i) => new ForecastPoint(BaseDate.AddDays(i), long.Parse(value, CultureInfo.InvariantCulture)))];

    /// <summary>Creates a snapshot of one currency's forecast.</summary>
    public static ForecastSnapshot From(string name, CurrencyForecast forecast, IEnumerable<Guid> scope, DateOnly baseDate, string? assumptions, string? appVersion)
    {
        ArgumentNullException.ThrowIfNull(forecast);
        ArgumentNullException.ThrowIfNull(scope);
        return new ForecastSnapshot
        {
            Name = name,
            BaseDate = baseDate,
            Horizon = forecast.Path.Count == 0 ? baseDate : forecast.Path[^1].Date,
            CurrencyCode = forecast.CurrencyCode,
            AccountIds = string.Join(',', scope.Select(id => id.ToString("D", CultureInfo.InvariantCulture))),
            Path = string.Join(',', forecast.Path.Select(p => p.Balance.ToString(CultureInfo.InvariantCulture))),
            Minimum = forecast.Minimum,
            MinimumDate = forecast.MinimumDate,
            UnknownCount = forecast.UnknownCount,
            Assumptions = string.IsNullOrWhiteSpace(assumptions) ? null : assumptions,
            AppVersion = appVersion,
        };
    }
}
