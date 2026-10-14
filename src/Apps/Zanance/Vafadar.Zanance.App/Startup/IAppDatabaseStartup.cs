using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Core;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Prepares all required storage before any financial page, app-lock database read or foreground writer starts.</summary>
public interface IAppDatabaseStartup
{
    /// <summary>Runs under the covered startup worker; protected hosts must register their complete bootstrap.</summary>
    Task PrepareAsync(CancellationToken cancellationToken = default);
}

/// <summary>Existing plaintext migration host; a configured protected session cannot fall back to it.</summary>
internal sealed class LocalDatabaseStartup(IServiceProvider services) : IAppDatabaseStartup
{
    /// <inheritdoc />
    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        void RequirePlaintextHost()
        {
            if (services.GetService<SqlCipherProfileSession<ZananceDbContext>>() is not null ||
                location.RequiresProtection || location.IsProtected)
                throw new InvalidOperationException("Protected storage requires its complete startup bootstrap.");
        }
        RequirePlaintextHost();
        using var owner = await location.Access.AcquireExclusiveAsync(cancellationToken).ConfigureAwait(false);
        RequirePlaintextHost(); owner.Validate(); cancellationToken.ThrowIfCancellationRequested();
        MoveLegacyDatabase(Path.GetDirectoryName(location.Path)!);
        await services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Preserves the original one-time brand rename only after ordinary database work drains.</summary>
    private static void MoveLegacyDatabase(string directory)
    {
        var path = Path.Combine(directory, ZananceApp.DatabaseFileName);
        var legacy = Path.Combine(directory, ZananceApp.LegacyDatabaseFileName);
        if (File.Exists(path) || !File.Exists(legacy)) return;
        // Preflight every destination before moving any member of the existing SQLite family.
        // Encrypted generations use their recorded original identity and must never enter this rename.
        var suffixes = new[] { string.Empty, "-wal", "-shm", "-journal" };
        if (suffixes.Any(suffix => File.Exists(path + suffix) || Directory.Exists(path + suffix)) ||
            Directory.EnumerateFileSystemEntries(directory, ZananceApp.LegacyDatabaseFileName + ".*")
                .Any(item => Path.GetFileName(item).Contains("cipher", StringComparison.Ordinal) ||
                    item.EndsWith(".generation.sqlite", StringComparison.Ordinal) || item.EndsWith(".restore-history.sqlite", StringComparison.Ordinal)))
            throw new IOException("Legacy storage has a conflicting destination or protected identity.");
        foreach (var suffix in suffixes)
            if (File.Exists(legacy + suffix)) File.Move(legacy + suffix, path + suffix);
    }
}
