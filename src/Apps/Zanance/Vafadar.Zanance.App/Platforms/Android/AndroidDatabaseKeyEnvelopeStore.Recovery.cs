using Android.Systems;
using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

public sealed partial class AndroidDatabaseKeyEnvelopeStore
{
    /// <summary>Explicitly verifies an owned recovered DEK against existing finance, then publishes a fresh native recovery entry.</summary>
    public async Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> recoveredKey,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyExistingGeneration, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(verifyExistingGeneration);
        if (recoveredKey.Length != 32) throw new CryptographicException("Recovery requires the exact 256-bit key.");
        var owned = recoveredKey.ToArray();
        try
        {
            ct.ThrowIfCancellationRequested(); ValidateDirectory();
            await verifyExistingGeneration(owned, ct).ConfigureAwait(false);
            await Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested(); ValidateDirectory();
                var final = GetPath(binding) + ".recovered"; var stage = final + ".creating";
                if (HasEntry(final))
                {
                    VerifyRecovered(final, binding, owned);
                    await Receipts().EnsureAsync(binding, owned, Path.GetFileName(final), CreateReceipt, ct).ConfigureAwait(false);
                    SynchronizeEntry(final); FlushDirectory(); return;
                }
                if (!HasEntry(stage))
                {
                    var envelope = AndroidDatabaseKeyProtector.ProtectRecovered(owned, binding);
                    ct.ThrowIfCancellationRequested();
                    using var output = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    Os.Chmod(stage, 384); output.Write(envelope); output.Flush(true);
                }
                VerifyRecovered(stage, binding, owned); SynchronizeEntry(stage); FlushDirectory();
                await Receipts().EnsureAsync(binding, owned, Path.GetFileName(stage), CreateReceipt, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested(); File.Move(stage, final, overwrite: false); FlushDirectory();
                VerifyRecovered(final, binding, owned);
                await Receipts().VerifyAsync(binding, owned, Path.GetFileName(final), ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
            }, ct).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(owned); }
    }
    private KeyRecoveryReceiptStore Receipts() => new(directory,
        new AndroidProfileRetirementFiles(new Dictionary<string, string> { ["keys"] = directory }, ValidateDirectory),
        new AndroidGenerationFileDurability(directory));
    private Task CreateReceipt(string name, ReadOnlyMemory<byte> bytes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); ValidateDirectory();
        var path = Path.Combine(directory, name);
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        Os.Chmod(path, 384); output.Write(bytes.Span); output.Flush(true); return Task.CompletedTask;
    }
    private void VerifyRecovered(string path, DatabaseKeyBinding binding, ReadOnlySpan<byte> expected)
    {
        var read = ReadEntry(path, binding, recovered: true);
        try { if (!CryptographicOperations.FixedTimeEquals(read, expected)) throw new CryptographicException("Recovery entry differs from verified data key."); }
        finally { CryptographicOperations.ZeroMemory(read); }
    }
    private static void SynchronizeEntry(string path)
    {
        using var descriptor = Os.Open(path, OsConstants.ORdwr | OsConstants.ONofollow | OsConstants.ONonblock, 0)!;
        try
        {
            using var stat = Os.Fstat(descriptor)!;
            if (!OsConstants.S_ISREG(stat.StMode)) throw new IOException("Recovery entry is not regular.");
            Os.Fsync(descriptor);
        }
        finally { Os.Close(descriptor); }
    }
}
