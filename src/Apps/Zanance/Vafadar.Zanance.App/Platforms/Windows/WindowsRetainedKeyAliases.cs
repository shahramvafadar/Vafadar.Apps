using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Keeps DPAPI system master keys outside application retirement ownership.</summary>
internal sealed class WindowsRetainedKeyAliases : IRetainedKeyAliases
{
    /// <inheritdoc />
    public int MaximumEnvelopeBytes => WindowsDatabaseKeyProtector.MaximumEnvelopeBytes;
    /// <inheritdoc />
    public bool HasIndependentAlias(int envelopeLength) => false;
    /// <inheritdoc />
    public Guid Capture(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding, bool recovered) =>
        throw new InvalidOperationException("DPAPI master keys are not independently owned application aliases.");
    /// <inheritdoc />
    public Task RemoveUnreferencedAsync(Guid alias, string root, Action validate, CancellationToken ct) =>
        throw new InvalidOperationException("Application retention cannot remove DPAPI master keys.");
}
