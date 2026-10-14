using Android.Systems;
using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Publishes immutable protected key envelopes in an existing trusted app-private directory.</summary>
/// <remarks>The caller captures and holds the exclusive profile/access gate and owns the directory.
/// This store never creates a wrapping key, selects a database or removes another generation.
/// File and directory flushes do not establish physical-device power-loss acceptance.</remarks>
public sealed partial class AndroidDatabaseKeyEnvelopeStore
{
    private readonly string directory;

    /// <summary>Captures an existing private directory supplied by the local installation owner, never a portable path.</summary>
    public AndroidDatabaseKeyEnvelopeStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Path.IsPathFullyQualified(directory)) throw new ArgumentException("Key storage requires an absolute private directory.");
        this.directory = Path.GetFullPath(directory);
        ValidateDirectory();
    }

    /// <summary>Checks whether any generation entry exists, including a damaged or redirected entry that must not be replaced.</summary>
    public bool ContainsEnvelope(DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding); ValidateDirectory();
        if (HasEntry(GetPath(binding) + ".recovered")) return true;
        if (HasEntry(GetPath(binding) + ".recovered.creating")) throw new IOException("Pending key recovery requires explicit continuation.");
        try { _ = File.GetAttributes(GetPath(binding)); return true; }
        catch (FileNotFoundException) { return false; }
    }

    /// <summary>Reads and authenticates exactly the stored generation; missing files or keys fail without replacement.</summary>
    public byte[] ReadKey(DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ValidateDirectory();
        var recovered = GetPath(binding) + ".recovered";
        var selected = HasEntry(recovered);
        var key = ReadEntry(selected ? recovered : GetPath(binding), binding, selected);
        try
        {
            if (selected) Receipts().VerifyAsync(binding, key, Path.GetFileName(recovered), default).GetAwaiter().GetResult();
            return key;
        }
        catch { CryptographicOperations.ZeroMemory(key); throw; }
    }
    private byte[] ReadEntry(string path, DatabaseKeyBinding binding, bool recovered)
    {
        ValidateDirectory();
        var descriptor = Os.Open(path, OsConstants.ORdonly | OsConstants.ONofollow | OsConstants.ONonblock, 0)!;
        Java.IO.FileInputStream input;
        try
        {
            using var stat = Os.Fstat(descriptor)!;
            if (!OsConstants.S_ISREG(stat.StMode)) throw new IOException("Key envelope is not a regular file.");
            input = new Java.IO.FileInputStream(descriptor);
        }
        catch { Os.Close(descriptor); throw; }
        using (input)
        using (descriptor)
        {
            var envelope = new byte[82];
            var offset = 0;
            while (offset < envelope.Length)
            {
                var count = input.Read(envelope, offset, envelope.Length - offset);
                if (count <= 0) break;
                offset += count;
            }
            if (input.Read() != -1) throw new CryptographicException("Stored key envelope is oversized.");
            // Ordinary immutable entries can be legacy installation-bound or independent generation-bound.
            // Recovery entries retain a distinct authenticated purpose and their required native receipt.
            return recovered ? AndroidDatabaseKeyProtector.UnprotectRecovered(envelope.AsSpan(0, offset), binding)
                : AndroidDatabaseKeyProtector.Unprotect(envelope.AsSpan(0, offset), binding);
        }
    }

    /// <summary>Authenticates then publishes a new envelope without replacing an existing generation.</summary>
    public void PublishNew(DatabaseKeyBinding binding, ReadOnlySpan<byte> envelope)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ValidateDirectory();
        var verified = AndroidDatabaseKeyProtector.Unprotect(envelope, binding);
        CryptographicOperations.ZeroMemory(verified);
        // Android app-private storage refused hard-link publication in the actual Release probe.
        // Exclusive final creation preserves no-replace semantics. A torn write is retained and refused,
        // never repaired with a fresh DEK; the migration owner must reconcile it before readiness.
        using (var stream = new FileStream(GetPath(binding), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            Os.Chmod(GetPath(binding), 384); // 0600: only this application UID may read or write the envelope.
            stream.Write(envelope);
            stream.Flush(flushToDisk: true);
        }
        FlushDirectory();
    }

    private static bool HasEntry(string path)
    { try { _ = File.GetAttributes(path); return true; } catch (FileNotFoundException) { return false; } }
    private string GetPath(DatabaseKeyBinding binding) => Path.Combine(directory,
        binding.ProfileId.ToString("N") + "-" + binding.GenerationId.ToString("N") + ".key");

    private void ValidateDirectory()
    {
        if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Trusted key directory is missing or redirected.");
    }

    private void FlushDirectory()
    {
        using var descriptor = Os.Open(directory, OsConstants.ORdonly | OsConstants.ONofollow | OsConstants.ONonblock, 0)!;
        try {
            using var stat = Os.Fstat(descriptor)!;
            if (!OsConstants.S_ISDIR(stat.StMode)) throw new IOException("Key storage is not a directory.");
            Os.Fsync(descriptor);
        }
        finally { Os.Close(descriptor); }
    }
}
