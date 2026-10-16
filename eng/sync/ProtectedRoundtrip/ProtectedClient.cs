using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Owns one original protected factory and actual device-bound key lifecycle over an isolated fictional root.</summary>
internal sealed class ProtectedClient : IDisposable
{
    /// <summary>Original scoped production services owned by this fictional client.</summary>
    internal ServiceProvider Services { get; }
    /// <summary>Original pinned transport with this client's device-protected session port.</summary>
    internal PersonalSyncTransport Transport { get; }
    private readonly SqlCipherProfileGeneration generation;
    /// <summary>Original financial store over the current protected generation.</summary>
    internal ZananceStore Store => Services.GetRequiredService<ZananceStore>();
    /// <summary>Original captured-round journal and review operations.</summary>
    internal PersonalSyncStore Sync => Services.GetRequiredService<PersonalSyncStore>();
    /// <summary>Original network/transaction coordinator without replacement algorithms.</summary>
    internal PersonalSyncCoordinator Coordinator => Services.GetRequiredService<PersonalSyncCoordinator>();

    private ProtectedClient(ServiceProvider services, SqlCipherProfileGeneration current, PersonalSyncTransport transport)
    { Services = services; generation = current; Transport = transport; }

    /// <summary>Creates only a fresh fictional input or reopens its original retained key/generation.</summary>
    internal static async Task<ProtectedClient> OpenAsync(string root, LocalSyncPeer peer, bool create, CancellationToken ct, TimeProvider? clock = null)
    {
        var original = Path.Combine(root, "original.db");
        if (create)
        {
            if (Directory.Exists(root)) throw new IOException("A fresh fictional client root is required.");
            Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "client.marker"), "Fictitious protected sync client v1", ct);
            using var initial = new ServiceCollection().AddSingleton(TimeProvider.System).AddZananceData(original).BuildServiceProvider();
            initial.MigrateLocalDatabase<ZananceDbContext>();
            await initial.GetRequiredService<ZananceStore>().GetSettingsAsync(ct);
        }
        void Validate()
        {
            if (!File.Exists(Path.Combine(root, "client.marker")) || File.ReadAllText(Path.Combine(root, "client.marker")) != "Fictitious protected sync client v1")
                throw new IOException("Original client ownership marker required.");
            for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
                if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected client roots are refused.");
        }
        Validate();
        var services = new ServiceCollection().AddSingleton(clock ?? TimeProvider.System).AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        try
        {
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var keyRoot = Path.Combine(root, "keys"); if (create) Directory.CreateDirectory(keyRoot);
            var keys = new WindowsGenerationKeyStore(keyRoot, Validate); var durability = new WindowsGenerationFileDurability(root);
            var identityPath = Path.Combine(root, "identities.sqlite");
            using var owner = await location.Access.AcquireExclusiveAsync(ct);
            await SqlCipherProfileBootstrap.PrepareAsync(location, owner, identityPath, new Dictionary<string, string> { [""] = original },
                create, keys, durability, (_, _) => throw new IOException("No restore intent belongs to this initial fictional client."), Validate,
                256 * 1024 * 1024, "4.19.0", ct);
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Validate); var identity = (await identities.ReadAsync(ct)).Single();
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, Validate);
            var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                _ => throw new IOException("Unexpected restored generation."), keys, durability, owner, Validate, 256 * 1024 * 1024, "4.19.0", ct);
            try
            {
                generation.Publish(location, owner);
                using var file = File.OpenRead(generation.Path); var header = new byte[16]; file.ReadExactly(header);
                if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8) || File.Exists(original) || !location.IsProtected)
                    throw new IOException("The published financial generation is not protected.");
                return new(services, generation, new PersonalSyncTransport(peer, new ProtectedSessions(root, Validate)));
            }
            catch { generation.Dispose(); throw; }
        }
        catch { services.Dispose(); throw; }
    }

    /// <inheritdoc />
    public void Dispose() { Transport.Dispose(); generation.Dispose(); Services.Dispose(); }
}
