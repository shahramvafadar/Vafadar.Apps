using Android.OS;
using Android.Provider;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Security;

/// <summary>Provides the Android PIN gate's coarse boot generation and sleep-inclusive clock without new permissions.</summary>
internal sealed class AndroidPinAttemptClock : IPinAttemptClock
{
    /// <inheritdoc />
    public PinAttemptClockReading? Read()
    {
        try
        {
            var boot = Settings.Global.GetInt(Android.App.Application.Context.ContentResolver, Settings.Global.BootCount, -1);
            var elapsed = SystemClock.ElapsedRealtime();
            return boot >= 0 && elapsed >= 0 ? new(boot, elapsed) : null;
        }
        catch (Java.Lang.SecurityException) { return null; }
    }
}
