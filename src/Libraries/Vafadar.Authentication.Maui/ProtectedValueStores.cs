#if IOS || WINDOWS
using System.Text;
using Vafadar.Authentication.OAuth;

namespace Vafadar.Authentication.Maui;

#if IOS
/// <summary>
/// Keeps a value in the iOS keychain through MAUI's <c>SecureStorage</c> (access group of the app's bundle id, see the
/// <c>keychain-access-groups</c> entitlement). iOS encrypts the keychain; an item can reach another device only inside an
/// encrypted device backup, never through the app's own data or the Zanance backups.
/// </summary>
internal sealed class KeychainValueStore(string key) : IProtectedValueStore
{
    public async ValueTask<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync(key);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // An unreadable keychain entry (for example after a restore to another device) means "not signed in".
            System.Diagnostics.Debug.WriteLine($"Keychain read failed: {ex.GetType().Name}");
            return null;
        }
    }

    public async ValueTask WriteAsync(string value, CancellationToken cancellationToken = default) =>
        await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync(key, value);

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        Microsoft.Maui.Storage.SecureStorage.Default.Remove(key);
        return ValueTask.CompletedTask;
    }
}
#endif

#if WINDOWS
/// <summary>
/// Keeps a value in a file protected with DPAPI for the current Windows user – the same protection as the MSAL token
/// cache (<c>Microsoft.Identity.Client.Extensions.Msal.Storage</c>). The file is useless on another computer or for
/// another user.
/// </summary>
internal sealed class DpapiFileValueStore(string folder, string fileName) : IProtectedValueStore
{
    private Microsoft.Identity.Client.Extensions.Msal.Storage? _storage;

    public ValueTask<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = Storage().ReadData();
            return ValueTask.FromResult<string?>(data is { Length: > 0 } ? Encoding.UTF8.GetString(data) : null);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A damaged or foreign file means "not signed in".
            System.Diagnostics.Debug.WriteLine($"Protected file read failed: {ex.GetType().Name}");
            return ValueTask.FromResult<string?>(null);
        }
    }

    public ValueTask WriteAsync(string value, CancellationToken cancellationToken = default)
    {
        Storage().WriteData(Encoding.UTF8.GetBytes(value));
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        Storage().Clear(ignoreExceptions: true);
        return ValueTask.CompletedTask;
    }

    private Microsoft.Identity.Client.Extensions.Msal.Storage Storage()
    {
        if (_storage is null)
        {
            Directory.CreateDirectory(folder);
            var properties = new Microsoft.Identity.Client.Extensions.Msal.StorageCreationPropertiesBuilder(fileName, folder).Build();
            _storage = Microsoft.Identity.Client.Extensions.Msal.Storage.Create(properties, null);
        }

        return _storage;
    }
}
#endif
#endif
