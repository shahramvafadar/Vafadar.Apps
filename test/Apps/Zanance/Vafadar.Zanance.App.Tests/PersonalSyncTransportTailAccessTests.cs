using System.Net;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class PersonalSyncSessionLifetimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_refusal_can_retire_credentials_only_while_original_access_is_current(bool retire)
    {
        await using var host = await ProtocolHost.StartAsync();
        var session = new PersonalSyncSession
        {
            AccessToken = "fictitious-expired-access", RefreshToken = "fictitious-expired-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        var credentialKey = host.Peer.BindingKey("session");
        await host.Tokens.WriteAsync(credentialKey, session, Ct);
        var retired = 0;
        host.BeforeRefreshRefusal = () => Interlocked.Exchange(ref retired, retire ? 1 : 0);
        using var transport = new PersonalSyncTransport(host.Peer, host.Tokens, verifyAccess: () =>
        {
            if (Volatile.Read(ref retired) != 0) throw new OperationCanceledException("Original synthetic access retired.");
        });
        var initialRequests = host.Requests;

        var error = await Record.ExceptionAsync(() => transport.BillingIdentityAsync(Ct));

        Assert.Equal(initialRequests + 1, host.Requests);
        Assert.Equal(1, host.Tokens.Writes);
        if (retire)
        {
            Assert.IsAssignableFrom<OperationCanceledException>(error);
            Assert.Equal(0, host.Tokens.Removals);
            Assert.Same(session, await host.Tokens.ReadAsync(credentialKey, Ct));
        }
        else
        {
            Assert.Equal(HttpStatusCode.Unauthorized, Assert.IsType<PersonalSyncHttpException>(error).Status);
            Assert.Equal(1, host.Tokens.Removals);
            Assert.Null(await host.Tokens.ReadAsync(credentialKey, Ct));
        }
    }

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
