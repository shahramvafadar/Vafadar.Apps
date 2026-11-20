namespace Vafadar.Zanance.App.Interaction;

internal static partial class HouseholdRecoveryFiles
{
    /// <summary>Owns the selected stream through the encrypted write, flush and close.</summary>
    internal static async Task<bool> WriteSelectedAsync(Stream destination, ReadOnlyMemory<byte> encrypted, CancellationToken ct)
    {
        await using (destination)
        {
            ct.ThrowIfCancellationRequested();
            await destination.WriteAsync(encrypted, ct);
            await destination.FlushAsync(ct);
        }
        // A late deadline cannot revoke a file that the provider successfully flushed and closed.
        return true;
    }
}
