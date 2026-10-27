using Microsoft.Data.Sqlite;
using Vafadar.Backup;
using Vafadar.Core.Settings;

namespace Vafadar.Data;

/// <summary>Stages database and portable preferences, selects with rollback, then replays before publishing access.</summary>
/// <remarks>One receiver is captured for one restore identity. Caller owns the exclusive covered application lease,
/// trusted paths, actual migrations and final catalogue/key publication. Existing selected restores resume without
/// the portable package or old connection. Originals, rollback and preference capsule remain referenced/retained.</remarks>
public sealed class SqlCipherPackageRestoreReceiver : IBackupPackageRestoreReceiver
{
    private readonly DatabaseGenerationCheckpoint captured;
    private readonly SqliteDatabaseGenerationJournal journal;
    private readonly string candidate;
    private readonly DatabaseKeyBinding binding;
    private readonly IDatabaseGenerationKeyStore keys;
    private readonly IDatabaseGenerationFileDurability durability;
    private readonly SqlCipherRestorePreferences preferences;
    private readonly ISettingsStore settings;
    private readonly Func<CancellationToken, Task<SqliteConnection>> openCurrent;
    private readonly Func<SqliteConnection, CancellationToken, Task> upgrade;
    private readonly Func<CancellationToken, Task> publish;
    private readonly Action validate;
    private readonly string version;
    private readonly int maximumBytes;
    private readonly SqliteProfileRestoreHistory? history;
    private readonly Func<CancellationToken, Task> persistPreferences;

    /// <summary>The immutable device binding used to validate retained restore publication.</summary>
    internal DatabaseKeyBinding Binding => binding;
    /// <summary>The exact captured endpoint; never inferred from a directory listing.</summary>
    internal string CandidatePath => candidate;
    /// <summary>The actual captured transition used by this package.</summary>
    internal DatabaseGenerationCheckpoint Captured => captured;
    /// <summary>Rejects untracked or differently captured history before resuming any platform effects.</summary>
    internal void RequireHistory(SqliteProfileRestoreHistory expected)
    {
        if (history is null || !history.IsSameStore(expected))
            throw new InvalidOperationException("Retained generation requires the receiver's exact restore history.");
    }

    /// <summary>Captures the actual generation, current connection owner and covered final publication effects.</summary>
    /// <remarks>The openCurrent callback transfers a registered open connection for receiver disposal. The
    /// publish callback owns the selected key/endpoint lifetime beyond this call; it must not expose access before replay.
    /// persistPreferences must actually persist the just-applied platform values and fail when that cannot be verified;
    /// an in-memory readback or completed placeholder task is not a durability implementation.</remarks>
    public SqlCipherPackageRestoreReceiver(DatabaseGenerationCheckpoint captured, SqliteDatabaseGenerationJournal journal,
        string candidate, DatabaseKeyBinding binding, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        ISettingsStore settings, IReadOnlyList<string> portableKeys, Func<CancellationToken, Task<SqliteConnection>> openCurrent,
        Func<SqliteConnection, CancellationToken, Task> upgrade, Func<CancellationToken, Task> publish,
        Action validate, int maximumBytes, string version, Func<CancellationToken, Task> persistPreferences,
        SqliteProfileRestoreHistory? history = null)
    {
        ArgumentNullException.ThrowIfNull(captured); ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(settings); ArgumentNullException.ThrowIfNull(openCurrent);
        ArgumentNullException.ThrowIfNull(upgrade); ArgumentNullException.ThrowIfNull(publish); ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(persistPreferences);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (!Path.IsPathFullyQualified(candidate) || captured.ProfileId != binding.ProfileId || captured.CandidateGeneration != binding.GenerationId)
            throw new ArgumentException("Package restore requires matching captured generation and absolute path.");
        this.captured = captured; this.journal = journal; this.candidate = Path.GetFullPath(candidate); this.binding = binding;
        this.keys = keys; this.durability = durability; this.settings = settings; this.openCurrent = openCurrent;
        this.upgrade = upgrade; this.publish = publish; this.validate = validate; this.maximumBytes = maximumBytes; this.version = version;
        this.history = history;
        this.persistPreferences = persistPreferences;
        preferences = new(this.candidate + ".preferences.cipher.db", binding, keys, durability, version, portableKeys);
    }

    /// <inheritdoc />
    public async Task RestoreAsync(BackupManifest manifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest); ArgumentNullException.ThrowIfNull(entries); Check(ct);
        if (history is not null) await history.RequireAsync(captured, ct).ConfigureAwait(false);
        // Zanance has two sources. Attachment bytes are inside its database, not a separate sequential restore.
        if (!entries.TryGetValue("database.sqlite", out var database) || entries.Keys.Any(name => name is not "database.sqlite" and not "display-settings.json"))
            throw new InvalidDataException("Protected restore package has missing or unsupported sources.");
        if (database.Length > maximumBytes) throw new InvalidDataException("Portable database exceeds restore bound.");
        var packet = preferences.CapturePackage(entries, settings);
        Check(ct);
        var state = await journal.ReadAsync(ct).ConfigureAwait(false); Check(ct);
        if (state != captured || state.Phase != DatabaseGenerationPhase.Original)
            throw new InvalidOperationException("Existing restore must resume from its durable state, not consume a new package.");
        var owned = database.ToArray();
        using (var input = new MemoryStream(owned, writable: false))
        {
            try { await SqlCipherRestoreStaging.PrepareAsync(input, candidate, binding, keys, durability, upgrade, validate, maximumBytes, version, ct).ConfigureAwait(false); }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(owned); }
        }
        await preferences.CreateNewAsync(packet, validate, ct).ConfigureAwait(false); Check(ct);
        await ResumeAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Resumes an already prepared package; reapplies durable allowlisted preferences before publishing the selected database.</summary>
    public async Task ResumeAsync(CancellationToken ct = default)
        => await ResumeCoreAsync(true, ct).ConfigureAwait(false);

    /// <summary>Finishes actual replay without invoking an earlier host publication callback.</summary>
    internal Task ResumeForGenerationAsync(CancellationToken ct) => ResumeCoreAsync(false, ct);

    private async Task ResumeCoreAsync(bool publishAccess, CancellationToken ct)
    {
        Check(ct);
        // Recorded ancestry is authority for key/capsule access, including a cold Resume call.
        if (history is not null) await history.RequireAsync(captured, ct).ConfigureAwait(false);
        Check(ct); var packet = await preferences.ReadAsync(validate, ct).ConfigureAwait(false); Check(ct);
        var state = await journal.ReadAsync(ct).ConfigureAwait(false); Check(ct);
        // Before selection the actual current factory connection remains open/registered; after selection no old key is needed.
        await using var current = state.CandidateSelected ? new SqliteConnection() : await openCurrent(ct).ConfigureAwait(false);
        await SqlCipherRestoreSelection.SelectAsync(captured, journal, candidate, binding, current, keys, durability, validate,
            maximumBytes, version, ct).ConfigureAwait(false); Check(ct);
        if (!packet.Applied)
        {
            preferences.Apply(packet, settings, validate, ct); Check(ct);
            await persistPreferences(ct).ConfigureAwait(false); Check(ct);
            await preferences.MarkAppliedAsync(validate, ct).ConfigureAwait(false); Check(ct);
        }
        if (history is not null) await history.RecordSelectedAsync(captured, journal, ct).ConfigureAwait(false);
        // Selection/replay no longer needs the old registered connection. Release it before the
        // session's drained publication callback can retire its previous key owner.
        await current.DisposeAsync().ConfigureAwait(false); Check(ct);
        if (publishAccess) await publish(ct).ConfigureAwait(false);
        // Publication may retire the previous access endpoint. The host keeps the cover until this task returns.
        // Do not mark migration Completed: original/capsule/rollback retention belongs to final package lifecycle.
    }

    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
}
