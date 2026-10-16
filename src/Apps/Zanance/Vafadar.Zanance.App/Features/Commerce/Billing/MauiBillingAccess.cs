using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Original native foreground access and read-only elapsed time; no system settings are changed.</summary>
internal sealed class MauiBillingAccess(Action validate) : IBillingNativeAccess
{
    private static readonly string ProcessSession = "process:" + Guid.NewGuid().ToString("N");
    /// <inheritdoc />
    public void Validate() => validate();
    /// <inheritdoc />
    public BillingMonotonicInstant ReadMonotonic()
    {
#if ANDROID
        try
        {
            var boot = global::Android.Provider.Settings.Global.GetInt(global::Android.App.Application.Context.ContentResolver,
                global::Android.Provider.Settings.Global.BootCount, -1);
            return new(boot < 0 ? "" : "android:" + boot.ToString(System.Globalization.CultureInfo.InvariantCulture), global::Android.OS.SystemClock.ElapsedRealtime());
        }
        catch (Java.Lang.SecurityException) { return new("", 0); }
#else
        // A new process deliberately requires online subscription time again. It never guesses an OS boot identity.
        return new(ProcessSession, checked((long)System.Diagnostics.Stopwatch.GetElapsedTime(0, System.Diagnostics.Stopwatch.GetTimestamp()).TotalMilliseconds));
#endif
    }
}
