using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Binds retained alias effects to authenticated independent Android Keystore entries.</summary>
internal sealed class AndroidRetainedKeyAliases : IRetainedKeyAliases
{
    /// <inheritdoc />
    public int MaximumEnvelopeBytes => 82;
    /// <inheritdoc />
    public bool HasIndependentAlias(int envelopeLength) => envelopeLength == 82;
    /// <inheritdoc />
    public Guid Capture(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding, bool recovered) =>
        AndroidDatabaseKeyProtector.CaptureIndependentAlias(envelope, binding, recovered);
    /// <inheritdoc />
    public async Task RemoveUnreferencedAsync(Guid alias, string root, Action validate, CancellationToken ct)
    {
        await AndroidAliasReferences.RequireUnreferencedAsync(alias, validate, ct, root).ConfigureAwait(false);
        AndroidDatabaseKeyProtector.RemoveCapturedIndependentAlias(alias, validate);
    }
}
