namespace Vafadar.Data;

/// <summary>Publishes or verifies bounded native recovery receipts under the host's real private-root/access effects.</summary>
public sealed class KeyRecoveryReceiptStore(string directory, IProfileRetirementReferenceFiles files,
    IDatabaseGenerationFileDurability durability)
{
    /// <summary>Creates/resumes an immutable receipt before recovered-key publication, using exact native file identities.</summary>
    public async Task EnsureAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key, string recoveredName,
        Func<string, ReadOnlyMemory<byte>, CancellationToken, Task> createPrivateFile, CancellationToken ct)
    {
        RequireName(binding, recoveredName); ArgumentNullException.ThrowIfNull(createPrivateFile);
        var receiptName = Name(binding) + ".recovered.receipt";
        var expected = await CaptureAsync(binding, key, recoveredName, ct).ConfigureAwait(false);
        var names = await files.ListNamesAsync("keys", ct).ConfigureAwait(false);
        if (!names.Contains(receiptName, StringComparer.Ordinal))
        {
            var stage = receiptName + ".creating";
            if (!names.Contains(stage, StringComparer.Ordinal)) await createPrivateFile(stage, expected, ct).ConfigureAwait(false);
            var stored = await files.ReadAsync("keys", stage, KeyRecoveryReceipt.MaximumBytes, prefix: false, ct).ConfigureAwait(false);
            if (!stored.AsSpan().SequenceEqual(expected))
            {
                // Explicit native recovery already verified the owned DEK against retained finance.
                // Preserve an interrupted receipt object; a fresh no-replace attempt cannot overwrite
                // either that object or an already published receipt. Reads still require final authority.
                stage = receiptName + ".creating." + Guid.NewGuid().ToString("N");
                await createPrivateFile(stage, expected, ct).ConfigureAwait(false);
                stored = await files.ReadAsync("keys", stage, KeyRecoveryReceipt.MaximumBytes, prefix: false, ct).ConfigureAwait(false);
                if (!stored.AsSpan().SequenceEqual(expected)) throw new IOException("New recovery receipt readback differs.");
            }
            await durability.SynchronizeFileAsync(Path.Combine(directory, stage), ct).ConfigureAwait(false);
            await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); ct.ThrowIfCancellationRequested();
            File.Move(Path.Combine(directory, stage), Path.Combine(directory, receiptName), overwrite: false);
        }
        await VerifyAsync(binding, key, recoveredName, ct).ConfigureAwait(false);
        await durability.SynchronizeFileAsync(Path.Combine(directory, receiptName), ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        await VerifyAsync(binding, key, recoveredName, ct).ConfigureAwait(false);
    }

    /// <summary>Reopens existing receipt and verifies exact original/recovered native objects without manufacturing authority.</summary>
    public async Task VerifyAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key, string recoveredName, CancellationToken ct)
    {
        RequireName(binding, recoveredName);
        var actual = await files.ReadAsync("keys", Name(binding) + ".recovered.receipt", KeyRecoveryReceipt.MaximumBytes, prefix: false, ct).ConfigureAwait(false);
        var expected = await CaptureAsync(binding, key, recoveredName, ct).ConfigureAwait(false);
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(actual, expected))
            throw new System.Security.Cryptography.CryptographicException("Native recovery receipt authority differs.");
    }
    private async Task<byte[]> CaptureAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key, string recoveredName, CancellationToken ct)
    {
        var names = await files.ListNamesAsync("keys", ct).ConfigureAwait(false);
        var original = names.Contains(Name(binding), StringComparer.Ordinal) ? await files.CaptureAsync("keys", Name(binding), ct).ConfigureAwait(false) : null;
        var recovered = await files.CaptureAsync("keys", recoveredName, ct).ConfigureAwait(false);
        recovered = new(recovered.Root, Name(binding) + ".recovered", recovered.NativeIdentity, recovered.Length, recovered.Sha256, recovered.RootIdentity);
        return KeyRecoveryReceipt.Create(binding, original, recovered, key.Span);
    }
    private void RequireName(DatabaseKeyBinding binding, string recoveredName)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (!Path.IsPathFullyQualified(directory) || recoveredName != Name(binding) + ".recovered" && recoveredName != Name(binding) + ".recovered.creating")
            throw new IOException("Recovery receipts require captured private paths and bound native names.");
    }
    private static string Name(DatabaseKeyBinding binding) => $"{binding.ProfileId:N}-{binding.GenerationId:N}.key";
}
