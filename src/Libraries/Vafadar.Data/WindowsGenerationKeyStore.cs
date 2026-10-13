using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Stores immutable CurrentUser DPAPI envelopes through native private file creation and flush.</summary>
/// <remarks>Caller owns the captured directory and exclusive application lease. Missing reads never create
/// a key; damaged entries never get overwritten. Recovery, reconciliation and power-loss acceptance remain host duties.</remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsGenerationKeyStore : IDatabaseGenerationKeyStore
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
    public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.Run(() =>
    {
        Check(ct); using var root = new WindowsPrivateFiles(directory);
        var path = PathFor(binding);
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
    public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.Run(() =>
    {
        Check(ct); using var root = new WindowsPrivateFiles(directory);
        var key = ReadCore(root, PathFor(binding), binding);
        try { Check(ct); return key; }
        catch { CryptographicOperations.ZeroMemory(key); throw; }
    }, ct);

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
