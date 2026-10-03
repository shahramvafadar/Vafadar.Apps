namespace Vafadar.Authentication.OAuth;

/// <summary>
/// Keeps one small secret value (for example a refresh token) in the protected storage of the platform: the keychain on
/// iOS, DPAPI for the current user on Windows. Values never go to preferences, backups or logs.
/// </summary>
public interface IProtectedValueStore
{
    /// <summary>Returns the stored value, or <see langword="null"/> when nothing is stored or it cannot be read.</summary>
    ValueTask<string?> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="value"/>, replacing an earlier value.</summary>
    ValueTask WriteAsync(string value, CancellationToken cancellationToken = default);

    /// <summary>Removes the stored value; nothing happens when none is stored.</summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
