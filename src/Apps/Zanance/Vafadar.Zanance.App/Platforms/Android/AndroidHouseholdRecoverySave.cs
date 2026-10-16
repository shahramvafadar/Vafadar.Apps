using Android.App;
using Android.Content;

namespace Vafadar.Zanance.App.Interaction;

/// <summary>Storage Access Framework create-document flow; a cancelled/recreated activity never confirms or reuses a later Save destination.</summary>
internal static class AndroidHouseholdRecoverySave
{
    private sealed record Pending(Activity Owner, int Code, TaskCompletionSource<Android.Net.Uri?> Completion);
    private static Pending? pending;
    private static int request = 0x5B00;

    /// <summary>Writes only the exact caller-owned encrypted bytes after explicit native destination selection; no persistent document access is retained.</summary>
    internal static async Task<bool> SaveAsync(string name, byte[] encrypted, CancellationToken ct)
    {
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("The current activity is unavailable.");
        if (pending is not null || request >= 0x7FFF) throw new InvalidOperationException("The original document picker must finish first.");
        var own = new Pending(activity, ++request, new(TaskCreationOptions.RunContinuationsAsynchronously)); pending = own;
        using var registration = ct.Register(() => own.Completion.TrySetCanceled(ct));
        var launched = false;
        try
        {
            using var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable); intent.SetType("application/octet-stream"); intent.PutExtra(Intent.ExtraTitle, name);
            // The app's existing activity-result bridge also hosts camera/OAuth. Keep a distinct
            // request code and exact activity owner; document results never enter those other flows.
            ct.ThrowIfCancellationRequested(); activity.StartActivityForResult(intent, own.Code); launched = true;
            using var uri = await own.Completion.Task; ct.ThrowIfCancellationRequested(); if (uri is null) return false;
            await using (var stream = activity.ContentResolver?.OpenOutputStream(uri, "w") ?? throw new IOException("The selected destination is unavailable."))
            { await stream.WriteAsync(encrypted, ct); await stream.FlushAsync(ct); }
            ct.ThrowIfCancellationRequested(); return true;
        }
        catch
        {
            // After cancellation the result still belongs to this original code/activity. It must
            // be consumed before another picker starts, rather than redirected to a new Save.
            if ((!launched || own.Completion.Task.IsCompletedSuccessfully) && ReferenceEquals(pending, own)) pending = null;
            throw;
        }
    }

    /// <summary>Consumes only this original activity's outstanding document selection.</summary>
    internal static bool OnActivityResult(Activity activity, int code, Result result, Intent? data)
    {
        if (pending is not { } own || own.Code != code || !ReferenceEquals(own.Owner, activity)) return false;
        pending = null; own.Completion.TrySetResult(result == Result.Ok ? data?.Data : null); return true;
    }

    /// <summary>Activity recreation abandons only the old native picker wait; the protected original kit remains resumable.</summary>
    internal static void OnDestroyed(Activity activity)
    {
        if (pending is not { } own || !ReferenceEquals(own.Owner, activity)) return;
        pending = null; own.Completion.TrySetCanceled();
    }
}
