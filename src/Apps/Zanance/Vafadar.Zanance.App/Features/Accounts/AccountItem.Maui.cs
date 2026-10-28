using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Native semantic account tile colors; original list data and commands remain platform independent.</summary>
public sealed partial record AccountItem
{
    /// <summary>Gets the icon colour.</summary>
    public Color TileText => Look.Text;

    /// <summary>Gets the tile background.</summary>
    public Color TileBackground => Look.Background;

    /// <summary>Gets the tile outline.</summary>
    public Color TileStroke => Look.Line;

    private (Color Text, Color Background, Color Line) Look => Type switch
    {
        AccountType.Savings => (Palette.SavingText, Palette.SavingBackground, Palette.SavingLine),
        AccountType.Loan => PlanLook.Danger,
        AccountType.Lent => (Palette.RefundText, Palette.RefundBackground, Palette.RefundLine),
        _ => (Palette.SecondaryText, Palette.Get("SurfaceMuted"), Palette.DividerColor),
    };
}
