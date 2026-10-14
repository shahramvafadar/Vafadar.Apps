using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Stores immutable CurrentUser DPAPI envelopes through native private file creation and flush.</summary>
/// <remarks>Caller owns the captured directory and exclusive application lease. Missing reads never create
/// a key; damaged entries never get overwritten. Recovery, reconciliation and power-loss acceptance remain host duties.</remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsGenerationKeyStore : IDatabaseGenerationKeyStore, IDatabaseGenerationKeyRecovery
{
    private readonly string directory;
    private readonly Action validate;

    /// <summary>Captures an existing app-private local directory and the caller's access/lifetime check.</summary>
    public WindowsGenerationKeyStore(string directory, Action validateAccess)
    {
        ArgumentNullException.ThrowIfNull(validateAccess);
        if (!Path.IsPathFullyQualified(directory)) throw new ArgumentException("Key storage requires an absolute private directory.", nameof(directory));
        this.directory = Path.GetFullPath(directory); validate = validateAccess;
        using var root = new WindowsPrivateFiles(this.directory);
    }

    /// <inheritdoc />
    public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.Run(async () =>
    {
        Check(ct); using var root = new WindowsPrivateFiles(directory);
        var path = RecoveryPath(binding);
        if (HasEntry(path))
        {
            var retained = ReadCore(root, path, binding);
            try { await Receipts().VerifyAsync(binding, retained, Path.GetFileName(path), ct).ConfigureAwait(false); }
            finally { CryptographicOperations.ZeroMemory(retained); }
            return;
        }
        if (HasEntry(path + ".creating")) throw new IOException("Pending key recovery requires its explicit continuation.");
        path = PathFor(binding);
        try
        {
            _ = File.GetAttributes(path);
            var existing = ReadCore(root, path, binding); CryptographicOperations.ZeroMemory(existing); return;
        }
        catch (FileNotFoundException) { }
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var envelope = WindowsDatabaseKeyProtector.Protect(key, binding); Check(ct);
            // Exclusive final creation retains a torn write for reconciliation; it is never replaced by a fresh DEK.
            using (var handle = root.CreateNewFile(path))
            using (var output = new FileStream(handle, FileAccess.Write))
            { output.Write(envelope); output.Flush(flushToDisk: true); }
            root.FlushDirectory(); Check(ct);
            var reopened = ReadCore(root, path, binding);
            try { if (!CryptographicOperations.FixedTimeEquals(key, reopened)) throw new CryptographicException("Stored generation key differs."); }
            finally { CryptographicOperations.ZeroMemory(reopened); }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }, ct);

    /// <inheritdoc />
    public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.Run(async () =>
    {
        Check(ct); using var root = new WindowsPrivateFiles(directory);
        var recovered = RecoveryPath(binding);
        var key = ReadCore(root, HasEntry(recovered) ? recovered : PathFor(binding), binding);
        try
        {
            if (HasEntry(recovered)) await Receipts().VerifyAsync(binding, key, Path.GetFileName(recovered), ct).ConfigureAwait(false);
            Check(ct); return key;
        }
        catch { CryptographicOperations.ZeroMemory(key); throw; }
    }, ct);

    /// <inheritdoc />
    public async Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> recoveredKey,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyExistingGeneration, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(verifyExistingGeneration);
        if (recoveredKey.Length != 32) throw new CryptographicException("Recovery requires the exact 256-bit key.");
        var owned = recoveredKey.ToArray();
        try
        {
            Check(ct); await verifyExistingGeneration(owned, ct).ConfigureAwait(false); Check(ct);
            using var root = new WindowsPrivateFiles(directory);
            var final = RecoveryPath(binding); var stage = final + ".creating";
            if (HasEntry(final))
            {
                var retained = ReadCore(root, final, binding);
                try { if (!CryptographicOperations.FixedTimeEquals(owned, retained)) throw new CryptographicException("Existing recovery entry differs."); }
                finally { CryptographicOperations.ZeroMemory(retained); }
                await Receipts().EnsureAsync(binding, owned, Path.GetFileName(final), CreateReceipt, ct).ConfigureAwait(false);
                using var handle = root.OpenFile(final, writable: true);
                using var flush = new FileStream(handle, FileAccess.Write); flush.Flush(true); root.FlushDirectory(); return;
            }
            if (!HasEntry(stage))
            {
                var envelope = WindowsDatabaseKeyProtector.Protect(owned, binding); Check(ct);
                using var handle = root.CreateNewFile(stage);
                using var output = new FileStream(handle, FileAccess.Write); output.Write(envelope); output.Flush(true);
            }
            var staged = ReadCore(root, stage, binding);
            try { if (!CryptographicOperations.FixedTimeEquals(owned, staged)) throw new CryptographicException("Staged recovery entry differs."); }
            finally { CryptographicOperations.ZeroMemory(staged); }
            await Receipts().EnsureAsync(binding, owned, Path.GetFileName(stage), CreateReceipt, ct).ConfigureAwait(false);
            root.FlushDirectory(); Check(ct); File.Move(stage, final, overwrite: false); root.FlushDirectory(); Check(ct);
            var reopened = ReadCore(root, final, binding);
            try { if (!CryptographicOperations.FixedTimeEquals(owned, reopened)) throw new CryptographicException("Recovered key readback differs."); }
            finally { CryptographicOperations.ZeroMemory(reopened); }
            await Receipts().VerifyAsync(binding, owned, Path.GetFileName(final), ct).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(owned); }
    }
    private KeyRecoveryReceiptStore Receipts() => new(directory,
        new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["keys"] = directory }, validate),
        new WindowsGenerationFileDurability(directory));
    private Task CreateReceipt(string name, ReadOnlyMemory<byte> bytes, CancellationToken ct)
    {
        Check(ct); using var root = new WindowsPrivateFiles(directory);
        using var handle = root.CreateNewFile(Path.Combine(directory, name));
        using var output = new FileStream(handle, FileAccess.Write); output.Write(bytes.Span); output.Flush(true);
        return Task.CompletedTask;
    }
    private static bool HasEntry(string path)
    { try { _ = File.GetAttributes(path); return true; } catch (FileNotFoundException) { return false; } }
    private string RecoveryPath(DatabaseKeyBinding binding) => PathFor(binding) + ".recovered";

    private static byte[] ReadCore(WindowsPrivateFiles root, string path, DatabaseKeyBinding binding)
    {
        using var handle = root.OpenFile(path, writable: false);
        using var input = new FileStream(handle, FileAccess.Read);
        if (input.Length is <= 0 or > WindowsDatabaseKeyProtector.MaximumEnvelopeBytes)
            throw new CryptographicException("Stored key envelope size is invalid.");
        var envelope = new byte[checked((int)input.Length)]; input.ReadExactly(envelope);
        if (input.ReadByte() != -1) throw new CryptographicException("Stored key envelope grew unexpectedly.");
        return WindowsDatabaseKeyProtector.Unprotect(envelope, binding);
    }
    private string PathFor(DatabaseKeyBinding binding) => Path.Combine(directory, $"{binding.ProfileId:N}-{binding.GenerationId:N}.key");
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
}
