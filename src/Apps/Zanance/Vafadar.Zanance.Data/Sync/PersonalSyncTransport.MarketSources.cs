using System.Globalization;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Separate source companion capability; it never changes a delivered financial payload or schema digest.</summary>
internal interface IPersonalMarketSourceTransport
{
    Task<MarketSourceAcknowledgment> PublishSourcesAsync(string identity, Guid profile, Guid device, MarketReferenceDocument[] documents, CancellationToken ct);
    Task<MarketSourcePage> ReadSourcesAsync(string identity, Guid profile, Guid device, long after, long? through, CancellationToken ct);
}

public sealed partial class PersonalSyncTransport : IPersonalMarketSourceTransport
{
    Task<MarketSourceAcknowledgment> IPersonalMarketSourceTransport.PublishSourcesAsync(string identity, Guid profile, Guid device,
        MarketReferenceDocument[] documents, CancellationToken ct) =>
        MarketAsync(identity, HttpMethod.Put, SourcePath(profile, device), new MarketSourceUpload(1, device, documents),
            MarketSourceJson.Default.MarketSourceUpload, MarketSourceJson.Default.MarketSourceAcknowledgment, ct);
    Task<MarketSourcePage> IPersonalMarketSourceTransport.ReadSourcesAsync(string identity, Guid profile, Guid device, long after, long? through, CancellationToken ct) =>
        MarketAsync<MarketSourceUpload, MarketSourcePage>(identity, HttpMethod.Get,
            SourcePath(profile, device) + "&after=" + after.ToString(CultureInfo.InvariantCulture) +
                (through is null ? "" : "&through=" + through.Value.ToString(CultureInfo.InvariantCulture)),
            null, null, MarketSourceJson.Default.MarketSourcePage, ct);
    private static string SourcePath(Guid profile, Guid device)
    {
        if (profile == Guid.Empty || device == Guid.Empty) throw new InvalidDataException("Original source transport authority required.");
        return $"sync/profiles/{profile:D}/source-documents?deviceId={device:D}";
    }
}
