namespace Vafadar.Zanance.Data.Sync;

/// <summary>Actual original personal source companion continuation after financial application, without financial/schema mutations.</summary>
internal static class PersonalMarketSourceCoordinator
{
    internal static async Task SynchronizeAsync(PersonalSyncStore store, IProfileSynchronizationTransport transport, string identity, CancellationToken ct)
    {
        var work = await store.CaptureMarketSourcesAsync(transport.Peer, identity, ProfileSynchronizationKind.Personal, ct);
        if (transport is not IPersonalMarketSourceTransport source)
            throw new InvalidOperationException("The original transport does not support accepted source history.");
        while (work.Outgoing.Length != 0)
        {
            var acknowledgment = await source.PublishSourcesAsync(identity, work.Authority.Profile, work.Authority.Device, work.Outgoing, ct);
            await store.AcknowledgeMarketSourcesAsync(work, acknowledgment, ct);
            work = await store.CaptureMarketSourcesAsync(transport.Peer, identity, ProfileSynchronizationKind.Personal, ct, work.ThroughRowId);
        }
        long? horizon = null;
        while (true)
        {
            var page = await source.ReadSourcesAsync(identity, work.Authority.Profile, work.Authority.Device, work.After, horizon, ct);
            if (horizon is { } captured && captured != page.Through) throw new InvalidDataException("Original source horizon changed.");
            horizon ??= page.Through; await store.ImportMarketSourcesAsync(work, page, ct);
            if (!page.More) break;
            work = await store.CaptureMarketSourcesAsync(transport.Peer, identity, ProfileSynchronizationKind.Personal, ct, work.ThroughRowId);
        }
    }
}
