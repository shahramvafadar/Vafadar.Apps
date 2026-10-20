#if DEBUG && WINDOWS
using System.Text.Json;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Continues only changed quantity field captions against the exact original completed Windows cases.</summary>
    private static async Task<HoldingRefundNativeReview.Prepared> ContinueOriginalQuantityCaptionsAsync(IServiceProvider services, string folder, string originalRun)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(originalRun, "^[0-9a-f]{32}$")
            || !AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Exact independent original quantity run required.");
        var original = Path.Combine(Path.GetDirectoryName(folder)!, originalRun);
        using var completed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(original, "complete.json")));
        if (completed.RootElement.GetProperty("Package").GetString() != "pro.vafadar.zanance.storepreview." + originalRun[..12]
            || !completed.RootElement.GetProperty("OriginalBaseStoreUnchanged").GetBoolean()
            || !completed.RootElement.GetProperty("ReadOnlyPresentationAfterTwoQuantityCases").GetBoolean())
            throw new IOException("Original completed quantity proof required.");
        return await HoldingQuantityNativeReview.ContinueAsync(services, Path.Combine(original, "financial"), completed.RootElement, CancellationToken.None);
    }
}
#endif
