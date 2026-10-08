using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Core.Settings;

namespace Vafadar.Backup;

/// <summary>Backs up an explicit allowlist of portable string preferences; credentials and device keys are excluded.</summary>
public sealed class SettingsBackupSource(ISettingsStore settings, string name, IReadOnlyList<string> keys) : IBackupSource
{
    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public Task WriteAsync(Stream destination, CancellationToken cancellationToken) =>
        JsonSerializer.SerializeAsync(destination, keys.ToDictionary(key => key, settings.Get), PortableSettingsJsonContext.Default.DictionaryStringString, cancellationToken);

    /// <inheritdoc />
    public async Task RestoreAsync(Stream source, CancellationToken cancellationToken)
    {
        var values = await JsonSerializer.DeserializeAsync(source, PortableSettingsJsonContext.Default.DictionaryStringString, cancellationToken)
            ?? throw new BackupException(BackupError.InvalidFormat, "Portable preferences are missing.");
        // An imported package cannot write arbitrary preferences or overwrite any device security state.
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                settings.Set(key, value);
            }
        }
    }
}

[JsonSerializable(typeof(Dictionary<string, string?>))]
internal sealed partial class PortableSettingsJsonContext : JsonSerializerContext;
