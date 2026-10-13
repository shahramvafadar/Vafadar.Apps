#if ANDROID
using Microsoft.Maui.Storage;
using Vafadar.Core.Settings;

namespace Vafadar.Maui.Hosting;

/// <summary>Persists the exact default Android preferences used by MAUI, away from the UI thread.</summary>
/// <remarks>The host must exclude concurrent preference writers until restore publication completes.</remarks>
internal sealed class AndroidSettingsPersistence(IPreferences preferences) : ISettingsPersistence
{
    // Device-local operational metadata, never a portable preference or credential.
    private const string PersistenceMarker = "vafadar.settings.persistence-generation";

    /// <inheritdoc />
    public Task PersistAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ReferenceEquals(preferences, Preferences.Default))
            throw new NotSupportedException("Persistence requires the default MAUI preference backend.");
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Match MAUI 10.0.110's default backend exactly; do not guess its private filename.
#pragma warning disable CA1422, CS0618
            using var native = Android.Preferences.PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context)
                ?? throw new IOException("Default Android preferences are unavailable.");
#pragma warning restore CA1422, CS0618
            using var editor = native.Edit() ?? throw new IOException("Default Android preferences cannot be edited.");
            // An empty editor can skip the write. A fresh marker forces a complete current snapshot to disk,
            // including prior MAUI Apply writes, so Commit's success belongs to an actual disk operation.
            editor.PutString(PersistenceMarker, Guid.NewGuid().ToString("N"));
            if (!editor.Commit()) throw new IOException("Android preference persistence failed.");
            cancellationToken.ThrowIfCancellationRequested();
        }, cancellationToken);
    }
}
#endif
