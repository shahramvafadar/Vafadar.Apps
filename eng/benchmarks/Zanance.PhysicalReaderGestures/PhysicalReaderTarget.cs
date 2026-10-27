using Android.Content;
using Android.Content.PM;
using Android.OS;
using System.Security.Cryptography;

namespace Vafadar.Zanance.PhysicalReaderGestures;

/// <summary>Reads only installed public APK metadata and bytes, never Main databases, keys or preferences.</summary>
internal static class PhysicalReaderTarget
{
    /// <summary>Capture package identity using the fixed owner, never a path or package supplied in an Intent.</summary>
    internal static ReaderOwnerInstall ReadInstall(Context context)
    {
        var manager = context.PackageManager ?? throw new InvalidOperationException("Package manager unavailable.");
        using var info = manager.GetPackageInfo(PhysicalReaderPolicy.Owner, (PackageInfoFlags)0)
            ?? throw new InvalidOperationException("Owned application unavailable.");
        var app = info.ApplicationInfo ?? throw new InvalidOperationException("Owned application identity unavailable.");
        return new(app.Uid, (app.Flags & ApplicationInfoFlags.Debuggable) != 0,
            app.SourceDir ?? "", info.LastUpdateTime);
    }

    /// <summary>Hash off the native service thread; require the same installed generation on both sides.</summary>
    internal static async Task<ReaderOwnerInstall?> AdmitAsync(Context context, CancellationToken cancellationToken)
    {
        var before = ReadInstall(context);
        var device = new PhysicalReaderDevice(Build.Manufacturer ?? "", Build.Model ?? "", (int)Build.VERSION.SdkInt,
            Build.SupportedAbis?.FirstOrDefault() ?? "", (int)(context.Resources?.DisplayMetrics?.DensityDpi ?? 0));
        // Check all public metadata before opening even the public APK; actual digest is still mandatory afterward.
        if (!PhysicalReaderPolicy.PublicTargetMatches(device, before)) return null;
        if (new FileInfo(before.SourceApk).Length != 97075103) return null;
        await using var source = File.OpenRead(before.SourceApk);
        var digest = Convert.ToHexString(await SHA256.HashDataAsync(source, cancellationToken));
        return PhysicalReaderPolicy.SameInstall(before, ReadInstall(context))
            && PhysicalReaderPolicy.Admits(device, before, digest) ? before : null;
    }
}
