using System.Diagnostics;

namespace Vafadar.Zanance.App.Security;

/// <summary>Keeps original native access transitions independent of fallible lifetime observers.</summary>
internal static class NativeAccessLifecycle
{
    /// <summary>Retires/publishes lifetime state, then independently queues the original native cover transition.</summary>
    /// <returns>Whether lifetime publication and dispatch both succeeded; a failed publication must not start foreground work.</returns>
    internal static bool Queue(Action publishLifetime, Func<Func<Task>, bool> dispatch, Func<Task> updateAccess)
    {
        ArgumentNullException.ThrowIfNull(publishLifetime);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(updateAccess);
        var published = true;
        try { publishLifetime(); }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        {
            published = false;
            Report(failure);
        }

        // D-1037: a cancellation/subscriber failure must not skip Sleep/Resume's native access work.
        // The dispatched task owns its errors so they cannot escape through an async-void platform callback.
        var queued = dispatch(async () =>
        {
            try { await updateAccess(); }
            catch (Exception failure) when (failure is not OutOfMemoryException) { Report(failure); }
        });
        return published && queued;
    }

    /// <summary>Records only a bounded failure category, never exception payloads or captured private state.</summary>
    private static void Report(Exception failure) => Debug.WriteLine($"Native access transition failed: {failure.GetType().Name}");
}
