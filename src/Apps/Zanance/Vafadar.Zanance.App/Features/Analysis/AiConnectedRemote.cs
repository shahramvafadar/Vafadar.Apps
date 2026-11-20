using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>The app's connected analysis session, bound to the original identity and foreground access.</summary>
internal sealed class AiConnectedRemote(PersonalSyncTransport transport, IAiPrivateStorage storage, string user,
    string originalFile, AiUiAccess access, CancellationToken feature = default) : IAiRemote
{
    private readonly AiClient client = new(transport, storage, user, originalFile, access.Validate);
    public string Destination => transport.Peer.Address.AbsoluteUri;
    public Task<AiAvailability> AvailabilityAsync(CancellationToken ct) => Work(client.AvailabilityAsync, ct);
    public Task<AiClientRecord?> ReadAsync(CancellationToken ct) => Work(client.ReadAsync, ct);
    public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct) => Work(token => client.OfferAsync(purpose, token), ct);
    public Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct) =>
        Work(token => client.SubmitAsync(offer, payload, recheck, token), ct);
    public Task<AiRequestResult> ContinueAsync(CancellationToken ct) => Work(client.ContinueAsync, ct);
    public Task<AiRequestResult> CloseAsync(CancellationToken ct) => Work(client.CloseAsync, ct);

    private async Task<T> Work<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
    {
        access.Validate();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, access.Cancellation, feature);
        var result = await work(linked.Token);
        access.Validate();
        return result;
    }

    public void Dispose() => transport.Dispose();
}
