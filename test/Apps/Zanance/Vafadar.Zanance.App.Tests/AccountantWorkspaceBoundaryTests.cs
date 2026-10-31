using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Tax;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original accountant page/file and retained disclosure input with real isolated SQLite and no output side effects.</summary>
public sealed class AccountantWorkspaceBoundaryTests
{
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Original = original;
        internal TaskCompletionSource? Held;
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationToken Token;
        internal bool Fail;
        internal int Reads;
        public ZananceDbContext CreateDbContext() => Original.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            Reads++; Token = ct; Entered.TrySetResult();
            if (Held is not null) await Held.Task.WaitAsync(ct);
            if (Fail) throw new IOException("Actual original account read refused");
            return await Original.CreateDbContextAsync(ct);
        }
    }
    private sealed class Native : IAccountantExportRuntime
    {
        public void EnsureAvailable() => throw new InvalidOperationException("No export authority is requested");
        public Task<bool> ConfirmAsync() => throw new InvalidOperationException("No authentication is requested");
        public Task<string> WriteAsync(Func<Stream, Task> write) => throw new InvalidOperationException("No output is requested");
        public Task ShareAsync(string path) => throw new InvalidOperationException("No share is requested");
        public void Remove(string path) => throw new InvalidOperationException("No output exists");
    }
    private static AccountantExportViewModel Create(FlowFixture f, Factory factory) => new(
        new AccountantExportStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Time),
        f.Translator, f.Time, f.Platform, new Native());
    private static async Task AddOriginalAccountAsync(FlowFixture f)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Accounts.Add(new Account { Name = "Fictitious original disclosure", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
    private static async Task RenameOriginalAsync(FlowFixture f)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Accounts.SingleAsync(TestContext.Current.CancellationToken)).Name = "Changed original disclosure";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
    [Fact]
    public async Task Actual_accountant_page_departure_cancels_the_original_pending_database_acquisition()
    {
        using var f = new FlowFixture(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()) { Held = new() };
        var model = Create(f, factory); var read = model.LoadAsync(); await factory.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        model.Retire(); var cancelled = factory.Token.IsCancellationRequested; factory.Held.SetResult(); await read;
        Assert.True(cancelled); Assert.False(model.Loaded); Assert.False(model.CanShare); Assert.False(model.SourceReadFailed);
    }
    [Fact]
    public async Task An_old_public_Load_cannot_activate_a_departed_original_accountant_form()
    {
        using var f = new FlowFixture(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); model.Retire(); await model.LoadAsync(); Assert.False(model.Loaded); Assert.Equal(1, factory.Reads); Assert.False(model.CanShare);
    }
    [Fact]
    public async Task Original_disclosure_form_cannot_replace_its_actual_file_on_reload()
    {
        using var first = new FlowFixture(); using var other = new FlowFixture(); var factory = new Factory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(first, factory);
        await model.LoadAsync(); factory.Original = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); await model.LoadAsync();
        Assert.False(model.Loaded); Assert.True(model.SourceReadFailed); Assert.Null(model.Error); Assert.False(model.CanShare);
    }
    [Fact]
    public async Task Read_only_failure_and_Retry_preserve_unchanged_explicit_account_disclosure_choices()
    {
        using var f = new FlowFixture();
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Accounts.Add(new Account { Name = "Original fictitious disclosure", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); Assert.Single(model.Accounts).Selected = true; model.Year = "2025"; model.IncludeNotes = true;
        factory.Fail = true; await model.LoadAsync(); Assert.False(model.Loaded); Assert.True(Assert.Single(model.Accounts).Selected); Assert.Equal("2025", model.Year);
        factory.Fail = false; await model.LoadAsync(); Assert.True(model.Loaded); Assert.True(Assert.Single(model.Accounts).Selected); Assert.True(model.IncludeNotes); Assert.Equal("2025", model.Year);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken)); Assert.False(model.CanShare);
    }

    /// <summary>A failed account-choice read must not report an output failure before any packet was requested.</summary>
    [Fact]
    public async Task Source_failure_does_not_report_unrequested_packet_creation_and_Retry_retains_disclosure()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()) { Fail = true };
        var model = Create(f, factory); model.Year = "2025"; model.IncludeNotes = true;
        await model.LoadAsync();
        Assert.True(model.SourceReadFailed);
        Assert.Null(model.Error); Assert.False(model.Loaded); Assert.False(model.CanReview); Assert.False(model.CanShare);
        Assert.Equal(1, factory.Reads); Assert.Equal("2025", model.Year); Assert.True(model.IncludeNotes);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
        factory.Fail = false; await model.LoadCommand.ExecuteAsync(null);
        Assert.True(model.Loaded); Assert.False(model.SourceReadFailed); Assert.Null(model.Error); Assert.Equal(2, factory.Reads);
        Assert.Empty(model.Accounts); Assert.Equal("2025", model.Year); Assert.True(model.IncludeNotes);
        Assert.False(model.CanShare); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); model.Retire();
    }
    [Fact]
    public async Task A_terminal_original_root_cannot_grant_a_later_appearance_disclosure_authority()
    {
        using var f = new FlowFixture(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); model.RetireRoot(); model.SetPresentationActive(true); await model.LoadAsync();
        Assert.Equal(1, factory.Reads); Assert.False(model.Loaded); Assert.False(model.CanShare); Assert.False(model.CanReview);
    }
    [Fact]
    public async Task Genuine_original_page_return_uses_a_new_token_and_retains_raw_year_and_opt_ins()
    {
        using var f = new FlowFixture(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); model.Year = "2025"; model.IncludeNotes = true; model.IncludeDocuments = true; var old = factory.Token;
        model.Retire(); Assert.True(old.IsCancellationRequested); model.SetPresentationActive(true); await model.LoadAsync();
        Assert.True(model.Loaded); Assert.NotEqual(old, factory.Token); Assert.Equal("2025", model.Year); Assert.True(model.IncludeNotes); Assert.True(model.IncludeDocuments); Assert.False(model.CanShare);
    }
    [Fact]
    public async Task Original_minimal_source_read_does_not_initialize_preferences_or_accounts()
    {
        using var f = new FlowFixture(); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); Assert.True(model.Loaded); Assert.Empty(model.Accounts);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, await db.Settings.CountAsync(TestContext.Current.CancellationToken)); Assert.Equal(0, await db.Accounts.CountAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Changed_account_captions_require_explicit_new_disclosure_after_read_only_reload()
    {
        using var f = new FlowFixture(); await AddOriginalAccountAsync(f); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); Assert.Single(model.Accounts).Selected = true; model.Year = "2025"; await RenameOriginalAsync(f); await model.LoadAsync();
        Assert.True(model.Loaded); Assert.False(Assert.Single(model.Accounts).Selected); Assert.Contains("Changed original disclosure", model.Accounts[0].Caption); Assert.Equal("2025", model.Year); Assert.False(model.CanShare);
    }
    [Fact]
    public async Task An_old_account_caption_cannot_review_changed_actual_metadata_before_Reload()
    {
        using var f = new FlowFixture(); await AddOriginalAccountAsync(f); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); Assert.Single(model.Accounts).Selected = true; await RenameOriginalAsync(f); await model.ReviewAsync();
        Assert.False(model.CanShare); Assert.Null(model.Summary); Assert.NotNull(model.Error); Assert.True(model.Accounts[0].Selected);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task A_retired_choice_callback_cannot_erase_a_new_current_read_only_review()
    {
        using var f = new FlowFixture(); await AddOriginalAccountAsync(f); var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Create(f, factory);
        await model.LoadAsync(); var old = Assert.Single(model.Accounts); old.Selected = true; await model.LoadAsync(); Assert.True(Assert.Single(model.Accounts).Selected);
        await model.ReviewAsync(); Assert.True(model.CanShare); var summary = model.Summary; old.Selected = false;
        Assert.True(model.CanShare); Assert.Equal(summary, model.Summary); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
