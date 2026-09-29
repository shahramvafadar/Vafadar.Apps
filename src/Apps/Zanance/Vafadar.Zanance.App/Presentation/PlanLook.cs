using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>The colours of plan rows: the future is violet, overdue is red, expected income green (D-27).</summary>
internal static class PlanLook
{
    /// <summary>Adds the date tile of an occurrence to a row.</summary>
    public static PlanRow WithDate(this PlanRow row, PlanText text, DateOnly due, bool overdue, EntryKind kind)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(text);
        var (day, month) = text.DayAndMonth(due);
        var (fore, back, line) = overdue ? Danger : kind == EntryKind.Income ? Income : Future;
        return row with { DayText = day, MonthText = month, TileText = fore, TileBackground = back, TileStroke = line };
    }

    /// <summary>Gets the colours of an overdue item.</summary>
    public static (Color Text, Color Background, Color Line) Danger => (Palette.ExpenseText, Palette.ExpenseBackground, Palette.DangerLine);

    /// <summary>Gets the colours of something that is still to come.</summary>
    public static (Color Text, Color Background, Color Line) Future => (Palette.PlanText, Palette.PlanBackground, Palette.PlanLine);

    /// <summary>Gets the colours of expected income.</summary>
    public static (Color Text, Color Background, Color Line) Income => (Palette.IncomeText, Palette.IncomeBackground, Palette.IncomeLine);

    /// <summary>Gets the colours of a paused plan or an estimate.</summary>
    public static (Color Text, Color Background, Color Line) Warning => (Palette.WarningText, Palette.WarningBackground, Palette.WarningLine);

    /// <summary>Gets the colours of an ended plan or a transfer.</summary>
    public static (Color Text, Color Background, Color Line) Neutral => (Palette.TransferText, Palette.TransferBackground, Palette.TransferLine);
}
