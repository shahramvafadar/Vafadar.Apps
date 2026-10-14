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
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested(); ValidateDirectory();
                var final = GetPath(binding) + ".recovered"; var stage = final + ".creating";
                if (HasEntry(final))
                {
                    VerifyRecovered(final, binding, owned);
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
                ct.ThrowIfCancellationRequested(); File.Move(stage, final, overwrite: false); FlushDirectory();
                VerifyRecovered(final, binding, owned); ct.ThrowIfCancellationRequested();
            }, ct).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(owned); }
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
