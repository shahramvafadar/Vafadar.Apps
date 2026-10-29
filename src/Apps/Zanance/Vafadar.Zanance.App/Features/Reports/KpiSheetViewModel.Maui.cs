namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>Maps original logical query delivery to the actual native routing contract.</summary>
public sealed partial class KpiSheetViewModel : IQueryAttributable;

/// <summary>Uses the original native modal Back without replacing authentication or data ownership.</summary>
internal sealed class MauiKpiSheetInteraction : IKpiSheetInteraction
{
    /// <inheritdoc />
    public Task CloseAsync() => Shell.Current.GoToAsync("..");
}
