namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Maps the actual history effect to the current native semantic palette.</summary>
public sealed partial record HoldingEventRow
{
    /// <summary>Gets the original quantity color for the current theme without moving finance into native code.</summary>
    public Color QuantityColor => QuantitySign > 0 ? Presentation.Palette.IncomeText
        : QuantitySign < 0 ? Presentation.Palette.ExpenseText : Presentation.Palette.SecondaryText;
}

/// <summary>Adapts original detail query delivery to the native routing interface.</summary>
public sealed partial class HoldingDetailViewModel : IQueryAttributable;
