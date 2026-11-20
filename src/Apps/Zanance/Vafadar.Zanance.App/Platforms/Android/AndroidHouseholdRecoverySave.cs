using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;

namespace Vafadar.Zanance.App.Interaction;

/// <summary>Storage Access Framework create-document flow; a cancelled/recreated activity never confirms or reuses a later Save destination.</summary>
internal static class AndroidHouseholdRecoverySave
{
    private sealed class Pending(Activity owner, int code, CancellationTokenSource lifetime)
    {
        internal Activity Owner { get; } = owner;
        internal int Code { get; } = code;
        internal TaskCompletionSource<Android.Net.Uri?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationTokenSource? Lifetime { get; set; } = lifetime;
        internal bool NativeFinished { get; set; }
    }
    private static Pending? pending;
    private static int request = 0x5B00;

    /// <summary>Writes only the exact caller-owned encrypted bytes after explicit native destination selection; no persistent document access is retained.</summary>
    internal static async Task<bool> SaveAsync(string name, byte[] encrypted, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("The current activity is unavailable.");
        if (pending is not null || request >= 0x7FFF) throw new InvalidOperationException("The original document picker must finish first.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = lifetime.Token;
        var own = new Pending(activity, ++request, lifetime); pending = own;
        using var registration = token.Register(() => own.Completion.TrySetCanceled(token));
        var launched = false;
        try
        {
            using var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable); intent.SetType("application/octet-stream"); intent.PutExtra(Intent.ExtraTitle, name);
            // The app's existing activity-result bridge also hosts camera/OAuth. Keep a distinct
            // request code and exact activity owner; document results never enter those other flows.
            token.ThrowIfCancellationRequested(); activity.StartActivityForResult(intent, own.Code); launched = true;
            using var uri = await own.Completion.Task; token.ThrowIfCancellationRequested(); if (uri is null) return false;
            var destination = await Task.Run(() => OpenDestination(activity, uri, token), token);
            using var asset = destination.Asset;
            return await HouseholdRecoveryFiles.WriteSelectedAsync(destination.Stream, encrypted, token);
        }
        catch (Android.OS.OperationCanceledException error)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException("The original recovery save retired.", error, token);
            throw new IOException("The provider cancelled the selected recovery destination.", error);
        }
        finally
        {
            own.Lifetime = null;
            // A deadline can retire the caller while the chooser is still open.
            // That callback remains reserved, and a returned URI retains ownership through close.
            if ((!launched || own.NativeFinished) && ReferenceEquals(pending, own)) pending = null;
        }
    }

    private static (AssetFileDescriptor Asset, Stream Stream) OpenDestination(Activity activity, Android.Net.Uri uri, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var authority = uri.Authority;
        var provider = string.IsNullOrEmpty(authority) ? null : activity.PackageManager?.ResolveContentProvider(authority, 0);
        if (uri.Scheme != ContentResolver.SchemeContent || string.IsNullOrEmpty(authority) ||
            provider?.ApplicationInfo?.Uid == Android.OS.Process.MyUid() ||
            activity.CheckUriPermission(uri, Android.OS.Process.MyPid(), Android.OS.Process.MyUid(),
                ActivityFlags.GrantWriteUriPermission) != Permission.Granted)
            throw new UnauthorizedAccessException("The selected recovery destination has no external write grant.");

        using var signal = new Android.OS.CancellationSignal();
        using var registration = token.Register(signal.Cancel);
        // OpenOutputStream uses the same mode and asset descriptor, without the cancellation overload.
        var asset = activity.ContentResolver?.OpenAssetFileDescriptor(uri, "w", signal)
            ?? throw new IOException("The selected destination is unavailable.");
        try
        {
            token.ThrowIfCancellationRequested();
            return (asset, asset.CreateOutputStream() ?? throw new IOException("The selected destination is unavailable."));
        }
        catch
        {
            using (asset) asset.Close();
            throw;
        }
    }

    /// <summary>Consumes only this original activity's outstanding document selection.</summary>
    internal static bool OnActivityResult(Activity activity, int code, Result result, Intent? data)
    {
        if (pending is not { } own || own.Code != code || !ReferenceEquals(own.Owner, activity)) return false;
        if (own.NativeFinished) return true;
        own.NativeFinished = true;
        var uri = result == Result.Ok ? data?.Data : null;
        if (!own.Completion.TrySetResult(uri)) uri?.Dispose();
        if (own.Lifetime is null) pending = null;
        return true;
    }

    /// <summary>Activity recreation abandons only the old native picker wait; the protected original kit remains resumable.</summary>
    internal static void OnDestroyed(Activity activity)
    {
        if (pending is not { } own || !ReferenceEquals(own.Owner, activity)) return;
        own.NativeFinished = true;
        if (own.Lifetime is { } lifetime) lifetime.Cancel();
        else pending = null;
    }
}
