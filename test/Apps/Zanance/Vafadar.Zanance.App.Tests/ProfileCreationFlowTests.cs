using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual profile sources retain durable Add intent across explicit persistence/preparation interruptions.</summary>
public sealed class ProfileCreationFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory, InlineData(1), InlineData(2), InlineData(3), Trait("AT", "AT-237")]
    public async Task Failed_native_write_resumes_same_identity_without_replacing_catalogue_or_main(int failure)
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var persistence = new Persistence { FailAt = failure }; var ids = new List<string>();
        var host = new ProtectedProfileCreationHost(persistence, (id, path, owner, ct) =>
        { location.Access.RequireDrainedOwner(owner); ids.Add(id); Assert.EndsWith(id + ".db", path); return Task.CompletedTask; });
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator, profileCreation: host);
        await Assert.ThrowsAsync<IOException>(() => profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious durable work", Ct));
        var pending = f.Preferences.Get("profiles.creation.pending"); Assert.NotNull(pending);
        if (failure == 1) Assert.Empty(ids);
        var retainedId = pending.Split('|')[2]; Assert.True(profiles.Current.IsMain);
        // A new service and native host represent process-local state loss; only real device intent supplies the id.
        profiles = new(location, f.Store, f.Preferences, new Runtime(), f.Translator, profileCreation: host);
        persistence.FailAt = -1;
        using (var owner = await location.Access.AcquireExclusiveAsync(Ct)) await host.ResumePendingAsync(profiles, owner, Ct);
        Assert.Equal(2, profiles.All.Count); Assert.Equal(retainedId, profiles.All.Single(p => !p.IsMain).Id);
        Assert.All(ids, id => Assert.Equal(retainedId, id)); Assert.True(profiles.Current.IsMain);
        Assert.Null(f.Preferences.Get("profiles.creation.pending")); Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        var writes = persistence.Writes;
        using (var owner = await location.Access.AcquireExclusiveAsync(Ct)) await host.ResumePendingAsync(profiles, owner, Ct);
        Assert.Equal(writes, persistence.Writes);
    }

    [Fact, Trait("AT", "AT-237")]
    public async Task Failed_preparation_retains_request_and_unrelated_catalogue_change_refuses_replay()
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var host = new ProtectedProfileCreationHost(new Persistence(), (_, _, _, _) => throw new IOException("Injected preparation interruption."));
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator, profileCreation: host);
        await Assert.ThrowsAsync<IOException>(() => profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious pending", Ct));
        Assert.Single(profiles.All); var before = f.Preferences.Get("profiles.creation.pending");
        profiles.Rename(profiles.Current, "Later legitimate main name");
        var prepares = 0; var resumed = new ProtectedProfileCreationHost(new Persistence(), (_, _, _, _) => { prepares++; return Task.CompletedTask; });
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<IOException>(() => resumed.ResumePendingAsync(profiles, owner, Ct));
        Assert.Equal(0, prepares); Assert.Single(profiles.All); Assert.Equal(before, f.Preferences.Get("profiles.creation.pending"));
    }

    [Fact, Trait("AT", "AT-237")]
    public async Task Persistence_that_restores_original_catalogue_cannot_record_creation_completion()
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var persistence = new Persistence();
        persistence.OnWrite = count => { if (count == 2) f.Preferences.Remove("profiles.work.catalogue"); };
        var host = new ProtectedProfileCreationHost(persistence, (_, _, _, _) => Task.CompletedTask);
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator, profileCreation: host);
        await Assert.ThrowsAsync<IOException>(() => profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious native readback", Ct));
        Assert.Single(profiles.All); Assert.Equal("0", f.Preferences.Get("profiles.creation.pending")!.Split('|')[1]);
        persistence.OnWrite = null;
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        await host.ResumePendingAsync(profiles, owner, Ct); Assert.Equal(2, profiles.All.Count);
    }

    [Fact, Trait("AT", "AT-237")]
    public async Task Damaged_device_intent_never_authorizes_preparation_or_catalogue_write()
    {
        using var f = new FlowFixture(); var location = f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        f.Preferences.Set("profiles.creation.pending", "Malformed existing intent");
        var prepares = 0; var persistence = new Persistence();
        var host = new ProtectedProfileCreationHost(persistence, (_, _, _, _) => { prepares++; return Task.CompletedTask; });
        var profiles = new ProfileService(location, f.Store, f.Preferences, new Runtime(), f.Translator, profileCreation: host);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        await Assert.ThrowsAsync<IOException>(() => host.ResumePendingAsync(profiles, owner, Ct));
        Assert.Equal(0, prepares); Assert.Equal(0, persistence.Writes); Assert.Single(profiles.All);
    }
    private sealed class Persistence : ISettingsPersistence
    {
        internal int Writes, FailAt = -1;
        internal Action<int>? OnWrite;
        public Task PersistAsync(CancellationToken cancellationToken = default)
        { if (++Writes == FailAt) throw new IOException("Injected native persistence interruption."); OnWrite?.Invoke(Writes); return Task.CompletedTask; }
    }
    private sealed class Runtime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => action();
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new NotSupportedException();
        public Task<bool> AuthenticateAsync(string reason) => throw new NotSupportedException();
        public Task ReloadLockAsync() => throw new NotSupportedException();
        public void DismissUndo() { }
    }
}
