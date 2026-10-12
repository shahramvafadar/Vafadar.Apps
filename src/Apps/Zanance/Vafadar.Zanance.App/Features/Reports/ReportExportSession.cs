using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Reports;

/// <summary>Native authentication, owned cache and platform sharing effects of report export.</summary>
public interface IReportExportRuntime
{
    /// <summary>Requests the existing app-lock export confirmation.</summary>
    Task<bool> ConfirmAsync();
    /// <summary>Writes a distinct application-owned portable PDF and returns its path.</summary>
    Task<string> WriteAsync(byte[] document);
    /// <summary>Opens the platform share sheet for the reviewed PDF.</summary>
    Task ShareAsync(string path);
    /// <summary>Removes only this operation's failed, unpublished owned cache file.</summary>
    Task RemoveAsync(string path);
}

/// <summary>Single-flight export bound to the original reviewed file, data, rights and explicit form scope.</summary>
public sealed class ReportExportSession(ReportWorkStore store)
{
    /// <summary>Gets whether authentication, rendering, cache writing or native sharing is still active.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Exports a detached original projection without changing the live report selection.</summary>
    public async Task ExportAsync(ReportReadReview review, bool advanced, Action validateScope,
        Func<Task<byte[]>> render, IReportExportRuntime runtime, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(validateScope);
        ArgumentNullException.ThrowIfNull(render); ArgumentNullException.ThrowIfNull(runtime);
        if (IsBusy) return;
        IsBusy = true; string? path = null; var shareStarted = false;
        try
        {
            review.DemandPdf(advanced); validateScope(); await store.ValidateAsync(review, cancellationToken);
            if (!await runtime.ConfirmAsync()) return;
            await ValidateAsync();
            var bytes = await render(); await ValidateAsync();
            path = await runtime.WriteAsync(bytes); await ValidateAsync();
            // Native sharing can outlive this await. Only failed files that were never offered are removed here.
            shareStarted = true; await runtime.ShareAsync(path);
        }
        finally
        {
            try { if (path is not null && !shareStarted) await runtime.RemoveAsync(path); }
            finally { IsBusy = false; }
        }
        async Task ValidateAsync()
        { validateScope(); review.DemandPdf(advanced); await store.ValidateAsync(review, cancellationToken); validateScope(); }
    }
}
