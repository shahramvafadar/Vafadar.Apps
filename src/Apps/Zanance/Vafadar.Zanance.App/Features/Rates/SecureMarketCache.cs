using System.Text;
using Vafadar.Zanance.Data.Rates;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>Device-encrypted dispensable reference cache; accepted source history lives in the financial profile instead.</summary>
internal sealed class SecureMarketCache : IMarketPrivateCache
{
    private static string Key(string scope)
    {
        if (scope.Length != 64 || !scope.All(char.IsAsciiHexDigit)) throw new ArgumentException("Original market cache scope required.");
        return "market.reference.v1." + scope;
    }
    /// <inheritdoc />
    public async Task<string?> ReadAsync(string scope, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); string? value;
        try { value = await SecureStorage.Default.GetAsync(Key(scope)); }
        catch (System.Security.Cryptography.CryptographicException) { throw new IOException("Original market cache unavailable."); }
#if ANDROID
        catch (Java.Security.GeneralSecurityException) { throw new IOException("Original market cache unavailable."); }
#endif
        ct.ThrowIfCancellationRequested();
        if (value is not null && Encoding.UTF8.GetByteCount(value) > 65536) throw new IOException("Market cache bound exceeded.");
        return value;
    }
    /// <inheritdoc />
    public async Task WriteAsync(string scope, string value, CancellationToken ct)
    {
        var key = Key(scope); ct.ThrowIfCancellationRequested();
        if (Encoding.UTF8.GetByteCount(value) > 65536) throw new IOException("Market cache bound exceeded.");
        await SecureStorage.Default.SetAsync(key, value);
        if (await SecureStorage.Default.GetAsync(key) != value) throw new IOException("Original market cache publication was not confirmed.");
        ct.ThrowIfCancellationRequested();
    }
}
