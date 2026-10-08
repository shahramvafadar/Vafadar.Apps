using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Security;

/// <summary>Stores only a PIN verifier and attempt state in the platform's secure storage (D-63).</summary>
public sealed class SecurePinStorage : IPinStorage
{
    private const string Key = "security.pin.v1";
    private const string RequiredKey = "security.pin.required";

    /// <summary>Gets the fail-closed marker used before the secure storage can be read at startup.</summary>
    public static bool IsRequired => Preferences.Default.Get(RequiredKey, false);

    /// <inheritdoc />
    public async Task<PinState?> ReadAsync()
    {
        var text = await SecureStorage.Default.GetAsync(Key);
        var state = text is null ? null : JsonSerializer.Deserialize(text, PinStorageJsonContext.Default.PinState);
        if (state is null && IsRequired)
        {
            throw new InvalidOperationException("The required PIN verifier is unavailable.");
        }

        return state;
    }

    /// <inheritdoc />
    public async Task WriteAsync(PinState? state)
    {
        if (state is null)
        {
            // Clear the marker first: a crash after removal must not leave a lock with no credential.
            Preferences.Default.Remove(RequiredKey);
            SecureStorage.Default.Remove(Key);
            return;
        }

        await SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(state, PinStorageJsonContext.Default.PinState));
        Preferences.Default.Set(RequiredKey, true);
    }
}

/// <summary>Generated JSON metadata keeps secure-state persistence compatible with trimmed mobile builds.</summary>
[JsonSerializable(typeof(PinState))]
internal sealed partial class PinStorageJsonContext : JsonSerializerContext;
