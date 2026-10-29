namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>Original native close effect for the source-bound KPI explanation.</summary>
public interface IKpiSheetInteraction
{
    /// <summary>Closes only the current original native explanation through normal routing.</summary>
    Task CloseAsync();
}
