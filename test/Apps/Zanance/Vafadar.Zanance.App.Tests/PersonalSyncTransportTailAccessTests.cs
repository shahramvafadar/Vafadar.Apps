using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    /// <summary>Authenticated custom sends revalidate their original access after session reads, retaining sign-out cleanup.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Original_custom_authenticated_sends_refuse_retired_access_before_network(bool signOut, bool retire)
    {
        await using var host = await ProtocolHost.StartAsync(); host.TailAccessFixture = true;
        await host.Tokens.WriteAsync(host.Peer.BindingKey("session"), new()
        {
            AccessToken = "fictitious-tail-access", RefreshToken = "fictitious-tail-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        }, Ct);
        var allowed = true; var reads = 0;
        using var transport = new PersonalSyncTransport(host.Peer, host.Tokens,
            verifyAccess: () => { if (!allowed) throw new OperationCanceledException("Original synthetic access retired."); });
        host.Tokens.BeforeRead = () => { if (++reads == (signOut ? 2 : 1) && retire) allowed = false; };
        byte[]? payload = null;
        var error = await Record.ExceptionAsync(async () =>
        {
            if (signOut) await transport.SignOutAsync(Ct);
            else payload = await transport.ReadBlobAsync(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), 0, Ct);
        });
        Assert.Equal(retire ? 0 : 1, host.TailRequests);
        Assert.Equal(signOut ? 2 : 1, reads);
        if (retire) { Assert.IsAssignableFrom<OperationCanceledException>(error); Assert.Null(payload); }
        else { Assert.Null(error); if (!signOut) Assert.Equal(new byte[] { 11, 23, 47 }, payload); }
        Assert.Equal(signOut ? 1 : 0, host.Tokens.Removals);
        Assert.Equal(signOut, await host.Tokens.ReadAsync(host.Peer.BindingKey("session"), Ct) is null);
    }
}
