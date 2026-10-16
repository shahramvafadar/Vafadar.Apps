using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>Owns one explicit source review across the original profile transition, never across process termination.</summary>
public sealed class HouseholdProjectionHandoff : IDisposable
{
    private readonly object gate = new();
    private Pending? current;

    /// <summary>Retains only the reviewed destination, actual identity and exact service; another action cannot replace it silently.</summary>
    public void Retain(string destinationId, LocalSyncPeer peer, string identity, HouseholdProjectionReview review)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationId); ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        lock (gate)
        {
            if (current is not null) throw new InvalidOperationException("Finish or cancel the original selected household copy first.");
            current = new(destinationId, peer.BindingKey(identity), review);
        }
    }

    /// <summary>Returns the same owned immutable source only to its explicitly chosen destination and authenticated identity.</summary>
    public HouseholdProjectionReview? Read(string destinationId, LocalSyncPeer peer, string identity)
    {
        lock (gate) return current is { } pending && pending.DestinationId == destinationId && pending.PeerIdentityBinding == peer.BindingKey(identity)
            ? pending.Review : null;
    }

    /// <summary>Clears only an exactly owned original; cancellation never deletes a durable draft or copied financial rows.</summary>
    public void Finish(HouseholdProjectionReview review)
    {
        lock (gate)
        {
            if (current?.Review != review) throw new InvalidOperationException("The original household selection changed.");
            current = null; review.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose() { lock (gate) { current?.Review.Dispose(); current = null; } }
    private sealed record Pending(string DestinationId, string PeerIdentityBinding, HouseholdProjectionReview Review);
}
