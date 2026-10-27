namespace Vafadar.Zanance.PhysicalReaderGestures;

/// <summary>Public device facts; never contains a serial, account, credential or private application path.</summary>
internal sealed record PhysicalReaderDevice(string Manufacturer, string Model, int Api, string Abi, int DensityDpi);

/// <summary>Package-manager identity of the installed, explicitly owned public APK.</summary>
internal sealed record ReaderOwnerInstall(int Uid, bool Debuggable, string SourceApk, long UpdatedAt);

/// <summary>D-572: finite, current-candidate-only admission for a separate inactive physical review helper.</summary>
internal static class PhysicalReaderPolicy
{
    /// <summary>Only this application's original foreground window may be observed or receive a gesture.</summary>
    internal const string Owner = "pro.vafadar.zanance";
    /// <summary>UID captured after the explicit Samsung handback; a reinstall or another device must refuse.</summary>
    internal const int OwnerUid = 10380;
    /// <summary>The complete signed D-578 product; a future upgrade requires fresh admission (D-579).</summary>
    internal const string OwnerApkSha256 = "510a3197cb6d77bf69df03d10f3d4079286726a82fecce557f8a4bebeef3b879";
    /// <summary>Public APK byte count of the same candidate; retain the exact pre-hash admission check.</summary>
    internal const long OwnerApkBytes = 97058719;
    /// <summary>Maximum lifetime of one explicitly enabled review connection.</summary>
    internal const long LifetimeMilliseconds = 5 * 60 * 1000;
    /// <summary>Finite command budget; successful callbacks alone never establish application acceptance.</summary>
    internal const int MaximumCommands = 96;

    /// <summary>Refuses other hardware, OS/ABI/density, UID, debug package, unreadable identity or candidate bytes.</summary>
    internal static bool Admits(PhysicalReaderDevice device, ReaderOwnerInstall install, string digest) =>
        PublicTargetMatches(device, install)
        && string.Equals(digest, OwnerApkSha256, StringComparison.OrdinalIgnoreCase);

    /// <summary>Rejects a prior or incomplete public APK before hashing; length alone never grants admission.</summary>
    internal static bool PublicApkLengthMatches(long bytes) => bytes == OwnerApkBytes;

    /// <summary>Reject off-target public metadata before opening the fixed package-manager APK path.</summary>
    internal static bool PublicTargetMatches(PhysicalReaderDevice device, ReaderOwnerInstall install) =>
        string.Equals(device.Manufacturer, "samsung", StringComparison.OrdinalIgnoreCase)
        && device.Model == "SM-A176B" && device.Api == 36 && device.Abi == "arm64-v8a"
        && device.DensityDpi == 450 && install.Uid == OwnerUid && !install.Debuggable
        && install.UpdatedAt > 0 && install.SourceApk.StartsWith("/data/app/", StringComparison.Ordinal)
        && install.SourceApk.EndsWith("/base.apk", StringComparison.Ordinal)
        && !install.SourceApk.Contains("..", StringComparison.Ordinal) && !install.SourceApk.Contains('\0')
        && install.SourceApk.Length <= 4096;

    /// <summary>A package update, changed public path/UID or debug flag invalidates the admitted file handle.</summary>
    internal static bool SameInstall(ReaderOwnerInstall before, ReaderOwnerInstall after) => before == after;

    /// <summary>Expiry applies to observations as well as input; an inactive or retired service emits no owner nodes.</summary>
    internal static bool SessionOpen(long now, long connectedAt, int commands, bool connected) =>
        connected && now >= connectedAt && now - connectedAt < LifetimeMilliseconds
        && commands is >= 0 and <= MaximumCommands;

    /// <summary>A late callback cannot revive a disconnected or replaced native service owner.</summary>
    internal static bool CurrentCallback(int requestedBinding, int actualBinding, bool connected) =>
        connected && requestedBinding == actualBinding;

    /// <summary>Use actual density with less than one pixel of rounding tolerance; never expand a viewport.</summary>
    internal static int MinimumTargetPixels(float density)
    {
        if (!float.IsFinite(density) || density <= 0 || density > 8)
            throw new ArgumentOutOfRangeException(nameof(density));
        return (int)Math.Floor(44d * density);
    }
}
