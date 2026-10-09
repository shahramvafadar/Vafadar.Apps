using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>A region choice; <see cref="Code"/> is <see langword="null"/> for "not set".</summary>
public sealed record RegionOption(string? Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>A first-day-of-week choice; <see cref="Day"/> is <see langword="null"/> for "automatic".</summary>
public sealed record WeekStartOption(DayOfWeek? Day, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>A calendar choice with its display name in the current language.</summary>
public sealed record CalendarOption(CalendarSystem Calendar, string DisplayName)
{
    public override string ToString() => DisplayName;
}
