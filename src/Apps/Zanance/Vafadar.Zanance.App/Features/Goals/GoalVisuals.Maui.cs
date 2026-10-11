using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Native semantic goal color, resolved from the current real palette.</summary>
public sealed partial record GoalRow
{
    /// <summary>Gets the original semantic progress color in the current theme.</summary>
    public Color ProgressColor => Tone switch
    {
        GoalTone.Historical => Palette.Muted,
        GoalTone.Reached => Palette.IncomeText,
        GoalTone.Warning => Palette.NearLimit,
        _ => Palette.SavingText,
    };
}

/// <summary>Native semantic allocation color, with releases kept visually secondary.</summary>
public sealed partial record AllocationRow
{
    /// <summary>Gets the original semantic history color in the current theme.</summary>
    public Color AmountColor => IsRelease ? Palette.SecondaryText : Palette.IncomeText;
}

/// <summary>The native query interface; the actual query application remains in the non-UI source.</summary>
public sealed partial class GoalDetailViewModel : IQueryAttributable
{
}
