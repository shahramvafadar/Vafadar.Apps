using Microsoft.AspNetCore.DataProtection;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>Protects personal labels with the same maintained persistent service key ring as financial payloads.</summary>
internal static class SyncLabelProtection
{
    /// <summary>Purpose separation prevents moving a name between identities, devices and spaces.</summary>
    internal static IDataProtector For(IDataProtectionProvider provider, string owner, string kind, Guid id) =>
        provider.CreateProtector("personal-sync-label-v1", owner, kind, id.ToString("N"));
}
