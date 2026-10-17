using System.Text;
using Vafadar.Zanance.Data.Analysis;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Device-private bounded analysis continuation, outside all portable preferences and financial synchronization.</summary>
internal sealed class SecureAiStorage : IAiPrivateStorage
{
    private const string Prefix = "analysis.private.v1.";
    /// <inheritdoc />
    public async Task<string?> ReadAsync(string scope, CancellationToken ct)
    {
        var marker = Marker(scope); ct.ThrowIfCancellationRequested(); string? value;
        try { value = await SecureStorage.Default.GetAsync(Prefix + scope); }
        catch (System.Security.Cryptography.CryptographicException) { throw new IOException("Original private analysis state unavailable."); }
#if ANDROID
        catch (Java.Security.GeneralSecurityException) { throw new IOException("Original private analysis state unavailable."); }
#endif
        ct.ThrowIfCancellationRequested();
        if (value is null && File.Exists(marker)) throw new IOException("Original private analysis state was lost; never assume safe resend.");
        if (value is not null && Encoding.UTF8.GetByteCount(value) > 65536) throw new IOException("Original private analysis state bound exceeded.");
        return value;
    }
    /// <inheritdoc />
    public async Task WriteAsync(string scope, string value, CancellationToken ct)
    {
        var marker = Marker(scope); ct.ThrowIfCancellationRequested();
        if (Encoding.UTF8.GetByteCount(value) > 65536) throw new IOException("Private analysis state bound exceeded.");
        await SecureStorage.Default.SetAsync(Prefix + scope, value);
        if (await SecureStorage.Default.GetAsync(Prefix + scope) != value) throw new IOException("Original private analysis state publication was not confirmed.");
        if (!File.Exists(marker))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            await using var file = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128, FileOptions.WriteThrough);
            await file.WriteAsync("Zanance private analysis state v1"u8.ToArray(), ct); file.Flush(flushToDisk: true);
        }
        ct.ThrowIfCancellationRequested();
    }
    private static string Marker(string scope)
    {
        if (scope.Length != 64 || !scope.All(char.IsAsciiHexDigit)) throw new ArgumentException("Original private analysis scope required.");
        var root = Path.Combine(FileSystem.AppDataDirectory, "analysis-state");
        if (File.Exists(root) || Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Private analysis directory refused.");
        var path = Path.Combine(root, scope + ".initialized");
        if (Directory.Exists(path) || File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Private analysis marker refused.");
        return path;
    }
}
