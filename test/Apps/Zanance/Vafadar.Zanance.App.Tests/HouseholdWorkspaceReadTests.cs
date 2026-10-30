using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original local household source/worker boundaries, without synthesizing private household authority.</summary>
public sealed class HouseholdWorkspaceReadTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Pre_sign_in_household_source_uses_one_captured_context_and_original_joined_worker_transaction()
    {
        using var f = new FlowFixture(); var access = new ReadAccess();
        var factory = new ObservedFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var journal = new HouseholdJournal(factory, f.Time); var original = new PersonalSyncStore(factory, access);
        var caller = 0;
        var packet = await Task.Factory.StartNew(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            return journal.ReadWorkspaceAsync(original, null, null, Ct).GetAwaiter().GetResult();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Assert.Equal(1, factory.Created); Assert.True(access.Joined); Assert.NotEqual(caller, access.Worker);
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        Assert.Equal(await store.ReviewAsync(Ct), packet.Connection); Assert.Equal(await store.StatusAsync(Ct), packet.Status);
        Assert.Null(packet.State); Assert.Null(packet.Membership); Assert.Null(packet.Access); Assert.Null(packet.Ownership);
        Assert.Null(packet.OwnershipOffer); Assert.Null(packet.SourceReceipt); Assert.Null(packet.Deletion);
        Assert.False(packet.PendingAdministration); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_household_source_refuses_when_its_actual_final_source_access_retires()
    {
        using var f = new FlowFixture(); var access = new ReadAccess { RetireAtFinalGuard = true };
        var factory = new ObservedFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var journal = new HouseholdJournal(factory, f.Time); var original = new PersonalSyncStore(factory, access);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => journal.ReadWorkspaceAsync(original, null, null, Ct));
        Assert.Equal("Original household source access retired.", error.Message);
        Assert.Equal(1, factory.Created); Assert.True(access.Joined);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Retired_household_first_run_owner_cannot_open_storage_or_publish_a_completion_receipt()
    {
        using var f = new FlowFixture(); var access = new ReadAccess();
        var factory = new ObservedFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var journal = new HouseholdJournal(factory, f.Time);
        var originalSettings = await f.Store.GetSettingsAsync(Ct);
        var before = System.Text.Json.JsonSerializer.Serialize(originalSettings);
        var accepted = 0; var changed = 0; f.Store.Changed += (_, _) => changed++;
        var authority = new HouseholdJournalAuthority(f.Directory.Combine("zanance.db"), Guid.NewGuid(), "fictitious-unverified-peer", "fictitious-identity");
        await Assert.ThrowsAsync<OperationCanceledException>(() => f.Store.CompleteHouseholdOnboardingAsync(journal, authority,
            () => throw new OperationCanceledException("Fictitious retired first-run page."), () => accepted++, Ct));
        Assert.Equal(0, factory.Created); Assert.Equal(0, accepted); Assert.Equal(0, changed);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
    }

    [Fact]
    public async Task Unregistered_household_first_run_factory_cannot_change_settings_or_publish_a_false_acceptance()
    {
        using var f = new FlowFixture(); var access = new ReadAccess();
        var factory = new ObservedFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var journal = new HouseholdJournal(factory, f.Time);
        var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
        var accepted = 0; var changed = 0; f.Store.Changed += (_, _) => changed++;
        var authority = new HouseholdJournalAuthority(f.Directory.Combine("zanance.db"), Guid.NewGuid(), "fictitious-unverified-peer", "fictitious-identity");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.CompleteHouseholdOnboardingAsync(journal, authority,
            () => { }, () => accepted++, Ct));
        Assert.Equal("Protected storage requires captured factory access.", error.Message);
        Assert.Equal(1, factory.Created); Assert.Equal(0, accepted); Assert.Equal(0, changed);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
    }

    [Fact]
    public async Task Original_household_source_reads_existing_first_run_completion_without_rewriting_settings()
    {
        using var f = new FlowFixture(); var settings = await f.Store.GetSettingsAsync(Ct);
        settings.OnboardingCompleted = true; f.Store.SaveSettings(settings);
        var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
        var journal = f.Services.GetRequiredService<HouseholdJournal>();
        var packet = await journal.ReadWorkspaceAsync(f.Services.GetRequiredService<PersonalSyncStore>(), null, null, Ct);
        Assert.True(packet.OnboardingCompleted); Assert.Null(packet.State); Assert.Null(packet.Access);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Identity_hint_cannot_open_private_household_material_through_an_unregistered_factory()
    {
        using var f = new FlowFixture(); var access = new ReadAccess();
        var factory = new ObservedFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var journal = new HouseholdJournal(factory, f.Time); var original = new PersonalSyncStore(factory, access);
        var peer = new LocalSyncPeer("https://127.0.0.1:5048/", new('1', 64));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => journal.ReadWorkspaceAsync(original, peer, "fictitious-unverified-identity", Ct));
        Assert.Equal("Protected storage requires captured factory access.", error.Message);
        Assert.Equal(1, factory.Created); Assert.False(access.Joined);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Counts only real factory creation and retains a weak reference for actual transaction observation.</summary>
    private sealed class ObservedFactory(IDbContextFactory<ZananceDbContext> original, ReadAccess access) : IDbContextFactory<ZananceDbContext>
    {
        internal int Created;
        public ZananceDbContext CreateDbContext()
        {
            var db = original.CreateDbContext(); Created++; access.Context = new(db); return db;
        }
    }

    /// <summary>Records the real final guard/worker and can retire only that fixture's cached access.</summary>
    private sealed class ReadAccess : ICommercialWriteAccessSource
    {
        internal WeakReference<ZananceDbContext>? Context;
        internal bool Joined, RetireAtFinalGuard;
        internal int Worker;
        public CommercialWriteAccess Capture(string databasePath)
        {
            if (Context?.TryGetTarget(out var db) == true && db.Database.CurrentTransaction is not null)
            {
                Joined = true; Worker = Environment.CurrentManagedThreadId;
                if (RetireAtFinalGuard) throw new InvalidOperationException("Original household source access retired.");
            }
            return CommercialWriteAccess.Inactive;
        }
    }
}
