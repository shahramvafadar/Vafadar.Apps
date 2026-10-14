using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Owns only platform wrapping aliases separately recorded by authenticated generation envelopes.</summary>
internal interface IRetainedKeyAliases
{
    /// <summary>Bounds the platform envelope read before native interpretation.</summary>
    int MaximumEnvelopeBytes { get; }
    /// <summary>Reports whether this envelope format carries an independently owned wrapping alias.</summary>
    bool HasIndependentAlias(int envelopeLength);
    /// <summary>Authenticates the wrapping identity of one exact generation envelope.</summary>
    Guid Capture(ReadOnlySpan<byte> envelope, DatabaseKeyBinding binding, bool recovered);
    /// <summary>Removes a captured alias only after all retained installation references are absent.</summary>
    Task RemoveUnreferencedAsync(Guid alias, string root, Action validate, CancellationToken ct);
}
