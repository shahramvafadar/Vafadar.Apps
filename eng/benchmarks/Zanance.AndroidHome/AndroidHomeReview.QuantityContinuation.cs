using System.Text.Json;
using Vafadar.Zanance.App.Diagnostics;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Reads the exact original completed quantity result and reuses its unchanged stores without financial commands.</summary>
    private static async Task<HoldingRefundNativeReview.Prepared> ContinueOriginalQuantityCaptionsAsync(IServiceProvider services, string root, CancellationToken ct)
    {
        var original = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetStringExtra("review_baseline");
        if (original is null || !System.Text.RegularExpressions.Regex.IsMatch(original, "^[0-9a-f]{32}$") || RedirectedAncestors(root).Length != 0)
            throw new ReviewCheckException("Exact original completed quantity comparison required.");
        using var completed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "holding-quantity-input-" + original + "-result.json"), ct));
        if (completed.RootElement.GetProperty("Comparison").GetString() != original
            || !completed.RootElement.GetProperty("ReadOnlyPresentationAfterTwoQuantityCases").GetBoolean()
            || completed.RootElement.TryGetProperty("OriginalQuantityReceiptContinuation", out var priorContinuation) && priorContinuation.GetBoolean())
            throw new ReviewCheckException("Original completed quantity proof required.");
        return await HoldingQuantityNativeReview.ContinueAsync(services, Path.Combine(root, "financial"), completed.RootElement, ct);
    }
}
