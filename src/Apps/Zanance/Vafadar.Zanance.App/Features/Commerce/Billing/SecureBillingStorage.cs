using System.Text;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Private platform-protected billing slots, with public loss markers outside financial and portable storage.</summary>
internal sealed class SecureBillingStorage : IBillingPrivateStorage
{
    private const string Prefix = "billing.private.v1.";
    /// <inheritdoc />
    public async Task<string?> ReadAsync(string scope, CancellationToken ct)
    {
        var marker = Marker(scope); ct.ThrowIfCancellationRequested();
        string? value;
        try { value = await SecureStorage.Default.GetAsync(Prefix + scope); }
        catch (System.Security.Cryptography.CryptographicException) { throw new BillingPrivateStateUnavailableException(); }
#if ANDROID
        catch (Java.Security.GeneralSecurityException) { throw new BillingPrivateStateUnavailableException(); }
#endif
        ct.ThrowIfCancellationRequested();
        if (value is null && File.Exists(marker)) throw new BillingPrivateStateUnavailableException();
        if (value is not null && Encoding.UTF8.GetByteCount(value) > 524288) throw new BillingPrivateStateUnavailableException();
        return value;
    }
    /// <inheritdoc />
    public async Task WriteAsync(string scope, string value, CancellationToken ct)
    {
        var marker = Marker(scope); ct.ThrowIfCancellationRequested();
        if (Encoding.UTF8.GetByteCount(value) > 524288) throw new InvalidDataException("Private billing state bound exceeded.");
        await SecureStorage.Default.SetAsync(Prefix + scope, value);
        if (await SecureStorage.Default.GetAsync(Prefix + scope) != value) throw new IOException("Original private billing publication was not confirmed.");
        // No server request can start until this write returns. A cut before this marker therefore has no accepted registration.
        if (!File.Exists(marker))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            await using var file = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128, FileOptions.WriteThrough);
            await file.WriteAsync("Zanance private billing state v1"u8.ToArray(), ct); file.Flush(flushToDisk: true);
        }
        ct.ThrowIfCancellationRequested();
    }
    /// <summary>Only this installation's exact billing markers; no path or arbitrary preference comes from external input.</summary>
    private static string Marker(string scope)
    {
        if (scope.Length != 64 || !scope.All(char.IsAsciiHexDigit)) throw new ArgumentException("Private billing scope required.");
        var root = Path.Combine(FileSystem.AppDataDirectory, "billing-state");
        if (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new IOException("Private billing directory refused.");
        var path = Path.Combine(root, scope + ".initialized");
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Private billing marker refused.");
        return path;
    }
}
