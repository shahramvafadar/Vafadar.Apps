#if WINDOWS
using Vafadar.Core.Settings;
using Vafadar.Data;

namespace Vafadar.Maui.Hosting;

/// <summary>Synchronizes the exact unpackaged Windows preference file written by the supported MAUI backend.</summary>
/// <remarks>Covered storage publication excludes concurrent preference writers. Packaged Windows storage needs its own acceptance.</remarks>
internal sealed class WindowsSettingsPersistence(IPreferences preferences) : ISettingsPersistence
{
    private const string PersistenceMarker = "vafadar.settings.persistence-generation";

    /// <inheritdoc />
    public Task PersistAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ReferenceEquals(preferences, Preferences.Default)
            || AppInfo.Current.PackagingModel != AppPackagingModel.Unpackaged)
            throw new NotSupportedException("Persistence requires the supported unpackaged default Windows preferences.");
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            // MAUI 10.0.110 writes this exact file synchronously but does not flush the actual file/directory handles.
            var marker = Guid.NewGuid().ToString("N");
            preferences.Set(PersistenceMarker, marker);
            var directory = Path.GetFullPath(Path.Combine(FileSystem.AppDataDirectory, "..", "Settings"));
            var durability = new WindowsGenerationFileDurability(directory);
            await durability.SynchronizeFileAsync(Path.Combine(directory, "preferences.dat"), cancellationToken).ConfigureAwait(false);
            await durability.SynchronizeDirectoryAsync(cancellationToken).ConfigureAwait(false);
            if (preferences.Get(PersistenceMarker, string.Empty) != marker)
                throw new IOException("Windows preference publication changed during persistence.");
        }, cancellationToken);
    }
}
#endif
