using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Backup;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original personal sync commands with real isolated SQLite and explicit native lifetime effects.</summary>
public sealed class PersonalSyncFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal sealed class EnvironmentPort : IPersonalSyncEnvironment, IDisposable
    {
        internal readonly CancellationTokenSource Lifetime = new();
        internal bool FailCapture;
        internal int Reads, Captures, ConnectionsChanged, Opens;
        internal Exception? OpenError;
        internal Func<PersonalSyncAttachmentContent, int, CancellationToken, Task<PersonalSyncRenderedPage>>? RenderPage;
        internal string Fingerprint = new('1', 64);
        internal string Scope = "fictitious-current-profile";
        public event EventHandler? StatusChanged;
        public bool Available { get; set; } = true;
        internal void PublishStatus() => StatusChanged?.Invoke(this, EventArgs.Empty);
        public PersonalSyncConnection ReadConnection() { Reads++; return new("https://127.0.0.1:5048/", Fingerprint); }
        public void RememberPeer(LocalSyncPeer peer) => throw new InvalidOperationException("No peer may be remembered by a local read.");
        public PersonalSyncAccess Capture()
        {
            Captures++;
            if (FailCapture) throw new InvalidOperationException("Fictitious secure-state failure.");
            return new(() => Lifetime.Token.ThrowIfCancellationRequested(), Lifetime.Token, Scope);
        }
        public PersonalSyncTransport Open(LocalSyncPeer peer, Action verifyAccess)
        { Opens++; throw OpenError ?? new InvalidOperationException("This local flow must not open HTTP or secure credentials."); }
        public Task PublishAppliedAsync(CancellationToken ct) => throw new InvalidOperationException("No synchronization was performed.");
        public void ConnectionChanged() => ConnectionsChanged++;
        public Guid CreationOperation(string requestDigest) => throw new InvalidOperationException("No remote creation was requested.");
        public void CompleteCreation(string requestDigest, Guid operation) => throw new InvalidOperationException("No remote creation was requested.");
        public Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int pageIndex, CancellationToken ct) =>
            RenderPage?.Invoke(content, pageIndex, ct) ?? throw new InvalidOperationException("No native attachment rendering was requested.");
        public void Dispose() => Lifetime.Dispose();
    }

    private static PersonalSyncViewModel Create(FlowFixture fixture, EnvironmentPort environment) => new(
        fixture.Services.GetRequiredService<PersonalSyncStore>(), fixture.Services.GetRequiredService<PersonalSyncCoordinator>(),
        environment, fixture.Platform, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);

    [Fact]
    public void Construction_cannot_expose_a_default_connection_or_open_native_effects()
    {
        using var f = new FlowFixture(); using var environment = new EnvironmentPort();
        var vm = Create(f, environment);
        Assert.False(vm.IsReady); Assert.False(vm.CanInteract); Assert.False(vm.SignedIn); Assert.False(vm.CanSync);
        Assert.Null(vm.Error); Assert.Empty(vm.Profiles); Assert.Empty(vm.Devices);
        Assert.Equal(0, environment.Reads); Assert.Equal(0, environment.Captures); Assert.Equal(0, environment.Opens);
    }

    [Fact]
    public async Task Failed_initial_access_keeps_the_form_covered_and_retry_reads_only_local_state()
    {
        using var f = new FlowFixture(); using var environment = new EnvironmentPort { FailCapture = true };
        var vm = Create(f, environment);
        await vm.ActivateAsync();
        Assert.False(vm.IsReady); Assert.False(vm.CanInteract); Assert.True(vm.CanRead);
        Assert.Equal(f.Translator["Sync_ActionFailed"], vm.Error);
        environment.FailCapture = false; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.True(vm.CanInteract); Assert.Null(vm.Error); Assert.False(vm.SignedIn);
        Assert.Equal(0, environment.Opens); Assert.Equal(0, environment.ConnectionsChanged);
        Assert.False((await f.Services.GetRequiredService<PersonalSyncStore>().StatusAsync(Ct)).Connected);
        vm.Deactivate(); Assert.False(vm.CanInteract);
    }

    [Fact]
    public async Task A_connection_changed_during_confirmation_cannot_be_paused_by_an_old_review()
    {
        using var f = new FlowFixture(); using var environment = new EnvironmentPort();
        var vm = Create(f, environment); await vm.ActivateAsync();
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        var peer = new LocalSyncPeer("https://127.0.0.1:5048/", new('1', 64));
        f.Platform.PendingConfirmation = async () =>
        {
            await store.ConnectAsync(await store.ReviewAsync(Ct), peer, "fictitious-owner", Guid.NewGuid(),
                new(Guid.NewGuid(), "Fictitious new connection", SyncSchema.Hash, 0, false), Ct);
            return true;
        };
        await vm.PauseCommand.ExecuteAsync(null);
        Assert.True((await store.StatusAsync(Ct)).Connected);
        Assert.Equal(f.Translator["Sync_ActionFailed"], vm.Error);
        Assert.Equal(0, environment.ConnectionsChanged); Assert.Equal(0, environment.Opens);
        vm.Deactivate();
    }

    [Fact]
    public async Task Retiring_foreground_authority_while_confirming_cannot_publish_a_late_pause()
    {
        using var f = new FlowFixture(); using var environment = new EnvironmentPort();
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await store.ConnectAsync(await store.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious copy", SyncSchema.Hash, 0, false), Ct);
        var vm = Create(f, environment); await vm.ActivateAsync();
        f.Platform.PendingConfirmation = () => { environment.Lifetime.Cancel(); return Task.FromResult(true); };
        await vm.PauseCommand.ExecuteAsync(null);
        Assert.True((await store.StatusAsync(Ct)).Connected);
        Assert.Equal(0, environment.ConnectionsChanged); Assert.Equal(0, environment.Opens);
        vm.Deactivate();
    }

    [Fact]
    public async Task Explicit_pause_preserves_money_and_the_existing_offline_change_queue()
    {
        using var f = new FlowFixture(); using var environment = new EnvironmentPort();
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious cash", CurrencyCode = "EUR", OpeningBalance = 12345,
            OpeningDate = new(2026, 10, 15) }, Ct);
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await store.ConnectAsync(await store.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious copy", SyncSchema.Hash, 0, false), Ct);
        var before = await store.StatusAsync(Ct); Assert.True(before.PendingChanges > 0);
        var vm = Create(f, environment); await vm.ActivateAsync(); f.Platform.Confirmations.Enqueue(true);
        await vm.PauseCommand.ExecuteAsync(null);
        var after = await store.StatusAsync(Ct);
        Assert.False(after.Connected); Assert.Equal(before.PendingChanges, after.PendingChanges);
        Assert.Equal(12345, Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).OpeningBalance);
        Assert.Null(vm.Error); Assert.Equal(1, environment.ConnectionsChanged); Assert.Equal(0, environment.Opens);
        vm.Deactivate();
    }

    [Fact]
    public async Task Portable_export_keeps_finances_but_removes_device_replay_state_without_touching_the_original()
    {
        using var f = new FlowFixture();
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious portable cash", CurrencyCode = "EUR",
            OpeningBalance = 24680, OpeningDate = new(2026, 10, 15) }, Ct);
        await store.ConnectAsync(await store.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-private-owner-not-for-export", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious private space", SyncSchema.Hash, 0, false), Ct);
        var before = await store.StatusAsync(Ct);
        var source = f.Services.GetServices<IBackupSource>().Single(row => row.Name == "database.sqlite");
        using var output = new MemoryStream(); await source.WriteAsync(output, Ct);
        var bytes = output.ToArray();
        Assert.DoesNotContain("fictitious-private-owner-not-for-export", System.Text.Encoding.UTF8.GetString(bytes));
        var exported = f.Directory.Combine("fictitious-portable.sqlite"); await File.WriteAllBytesAsync(exported, bytes, Ct);
        await using (var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = exported, Pooling = false }.ToString()))
        {
            await db.OpenAsync(Ct);
            foreach (var table in new[] { "SyncBinding", "SyncOutbox", "SyncKnown", "SyncClientRound", "SyncIncoming", "SyncConflicts", "SyncBlobTransfers", "SyncBlobParts" })
            {
                await using var command = db.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM \"" + table + "\"";
                Assert.Equal(0L, await command.ExecuteScalarAsync(Ct));
            }
            await using var amount = db.CreateCommand(); amount.CommandText = "SELECT OpeningBalance FROM Accounts";
            Assert.Equal(24680L, await amount.ExecuteScalarAsync(Ct));
        }
        Assert.Equal(before, await store.StatusAsync(Ct));
    }

    [Fact]
    public async Task Restoring_an_older_copy_containing_sync_metadata_cannot_reconnect_or_replay_its_queue()
    {
        using var f = new FlowFixture();
        var store = f.Services.GetRequiredService<PersonalSyncStore>();
        await f.Store.SaveAccountAsync(new Account { Name = "Fictitious restored cash", CurrencyCode = "EUR",
            OpeningBalance = -54321, OpeningDate = new(2026, 10, 15) }, Ct);
        await store.ConnectAsync(await store.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-old-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious old space", SyncSchema.Hash, 0, false), Ct);
        var oldCopy = f.Directory.Combine("fictitious-older-copy.sqlite");
        await using (var context = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await context.Database.OpenConnectionAsync(Ct);
            await using var copy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = oldCopy, Pooling = false }.ToString());
            await copy.OpenAsync(Ct); ((SqliteConnection)context.Database.GetDbConnection()).BackupDatabase(copy);
        }
        var source = f.Services.GetServices<IBackupSource>().Single(row => row.Name == "database.sqlite");
        await using (var input = File.OpenRead(oldCopy)) await source.RestoreAsync(input, Ct);
        var restored = await store.StatusAsync(Ct);
        Assert.False(restored.Connected); Assert.Equal(0, restored.PendingChanges); Assert.Null(restored.RemoteName);
        Assert.Equal(-54321, Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).OpeningBalance);
    }
}
