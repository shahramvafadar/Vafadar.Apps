using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data.Auditing;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-641: complete original budget history, retained raw drafts and actual native continuation ownership.</summary>
[Collection("Entry validation units")]
public sealed class BudgetEditorContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal bool Refused;
        public CommercialWriteAccess Capture(string path) => Refused
            ? throw new InvalidOperationException("Original cached budget access retired.") : CommercialWriteAccess.Inactive;
    }
    private static async Task<Budget> SeedAsync(FlowFixture f)
    {
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 10000 };
        await f.Store.SaveBudgetAsync(budget, Ct); return budget;
    }
    private static BudgetEditorViewModel Editor(FlowFixture f, ZananceStore? store = null) =>
        new(store ?? f.Store, f.Translator, f.Localization, f.Platform);
    private static ZananceStore Controlled(FlowFixture f, AccountDetailOriginalBoundaryTests.ControlledFactory factory) =>
        new(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
    private static Task OpenAsync(BudgetEditorViewModel vm, Budget budget) =>
        vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = budget.Id });

    /// <summary>The complete reader/validator never creates Settings, categories or financial rows.</summary>
    [Fact]
    public async Task Empty_original_source_and_validation_never_initialize_storage()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.GetBudgetEditorSnapshotAsync(cancellationToken: Ct);
        Assert.Null(source.Review.Settings); Assert.Empty(source.Entries);
        await f.Store.ValidateBudgetEditorSnapshotAsync(source, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Nonpersisted default preferences cannot be mutated and mistaken for the reviewed source.</summary>
    [Fact]
    public async Task Mutated_nonpersisted_defaults_refuse_validation_without_a_write()
    {
        using var f = new FlowFixture(); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.GetBudgetEditorSnapshotAsync(cancellationToken: Ct);
        source.Settings.MonthStartDay = 15;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateBudgetEditorSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Full-field detached history includes tags even outside the displayed suggestion interval.</summary>
    [Fact]
    public async Task Detached_outside_period_ledger_tags_cannot_validate_as_original()
    {
        using var f = new FlowFixture(); await SeedAsync(f);
        var account = new Account { Name = "D641 tag source", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { db.Entries.Add(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 100, Date = new(2020, 1, 2), Tags = ["original"] }); await db.SaveChangesAsync(Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await f.Store.GetBudgetEditorSnapshotAsync(cancellationToken: Ct);
        Assert.Single(source.Entries).Tags.Add("changed detached tag");
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateBudgetEditorSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>The same actual writer refuses a complete history change before committing the original budget.</summary>
    [Fact]
    public async Task Changed_original_ledger_refuses_budget_writer_and_preserves_configuration()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f);
        var account = new Account { Name = "D641 changed source", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var source = await f.Store.GetBudgetEditorSnapshotAsync(cancellationToken: Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { db.Entries.Add(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Amount = 123, Date = new(2026, 10, 1) }); await db.SaveChangesAsync(Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f); budget.TotalLimit = 23456;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveBudgetAsync(budget, Ct, review: source.Review, originalSource: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>A failed explicit reload keeps raw total/category inputs and Retry never replays a writer.</summary>
    [Fact]
    public async Task Failed_refresh_and_visible_retry_preserve_raw_total_and_category_drafts()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f);
        var category = new Category { Name = "D641 original limit", Kind = CategoryKind.Expense };
        await f.Store.SaveCategoryAsync(category, Ct); var refuse = false;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = _ => refuse ? Task.FromException(new IOException("D641 original read refusal.")) : Task.CompletedTask };
        var vm = Editor(f, Controlled(f, factory)); await OpenAsync(vm, budget);
        vm.TotalText = " 123.4500 "; vm.Limits.Single(row => row.CategoryId == category.Id).Text = " 8.900 ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        refuse = true; await vm.ReloadCommand.ExecuteAsync(null); Assert.False(vm.IsReady); Assert.False(vm.CanSave);
        refuse = false; await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsReady);
        Assert.Equal(" 123.4500 ", vm.TotalText); Assert.Equal(" 8.900 ", vm.Limits.Single(row => row.CategoryId == category.Id).Text);
        Assert.True(vm.IsDirty); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Native appearance cannot hide a completed read failure by starting another acquisition.</summary>
    [Fact]
    public async Task Initial_appearance_requires_explicit_retry_after_fast_failure()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var refuse = true;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = _ => refuse ? Task.FromException(new IOException("D641 fast source failure.")) : Task.CompletedTask };
        var vm = Editor(f, Controlled(f, factory)); await OpenAsync(vm, budget);
        refuse = false; vm.SetViewActive(true); await vm.EnsureLoadedAsync(); Assert.False(vm.IsReady);
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsReady); Assert.Equal("100.00", vm.TotalText);
    }

    /// <summary>The actual same form resumes only its cancelled original reader, never the departed continuation.</summary>
    [Fact]
    public async Task Same_budget_form_resumes_cancelled_read_without_a_second_source_preparation()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var first = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.SetResult(); await wait.Task.WaitAsync(ct); } };
        var vm = Editor(f, Controlled(f, factory)); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var read = OpenAsync(vm, budget); await entered.Task.WaitAsync(Ct); vm.SetViewActive(false); await read;
        vm.SetViewActive(true); await vm.EnsureLoadedAsync(); Assert.True(vm.IsReady); Assert.Equal("100.00", vm.TotalText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Root replacement during an original read cannot publish a ready form in the replacement root.</summary>
    [Fact]
    public async Task Root_replacement_during_read_cannot_publish_original_total()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var root = true; var first = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.SetResult(); await release.Task.WaitAsync(ct); } };
        var vm = Editor(f, Controlled(f, factory)); vm.BindRootOwner(() => root);
        var read = OpenAsync(vm, budget); await entered.Task.WaitAsync(Ct); root = false; release.SetResult(); await read;
        Assert.False(vm.IsReady); Assert.Empty(vm.TotalText); Assert.False(vm.CanSave); Assert.False(vm.CanReload);
    }

    /// <summary>Reentrant departure from the original currency notification stops all later raw field publication.</summary>
    [Fact]
    public async Task Reentrant_retirement_during_currency_publication_never_sets_total_or_ready()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var vm = Editor(f);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.CurrencyCode)) vm.SetViewActive(false); };
        await OpenAsync(vm, budget); Assert.False(vm.IsReady); Assert.Empty(vm.TotalText); Assert.False(vm.CanSave);
    }

    /// <summary>The parent's narrow original file cannot be replaced by another initial editor context.</summary>
    [Fact]
    public async Task Original_parent_origin_refuses_a_foreign_first_budget_context()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var budget = await SeedAsync(f);
        var parent = await f.Store.GetBudgetEditorReviewAsync(Ct); var before = await GoalChooserFlowTests.SnapshotAsync(other);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => AccountDetailOriginalBoundaryTests.Factory(other) };
        var vm = Editor(f, Controlled(f, factory));
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = budget.Id, ["budgetEditorOrigin"] = parent.CreateEditorOrigin() });
        Assert.False(vm.IsReady); Assert.Empty(vm.TotalText); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other));
    }

    /// <summary>A native discard response cannot close raw text edited after its original request.</summary>
    [Fact]
    public async Task Late_discard_response_cannot_close_newer_budget_input()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var vm = Editor(f); await OpenAsync(vm, budget);
        vm.TotalText = "123.45"; f.Platform.PendingConfirmation = () => { vm.TotalText = "678.90"; return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal("678.90", vm.TotalText); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>The writer's accepted receipt precedes notification failure and any later native input.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Accepted_budget_retains_later_draft_and_never_replays_save(bool notificationFails)
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var vm = Editor(f); await OpenAsync(vm, budget);
        vm.TotalText = "123.45";
        f.Store.Changed += (_, _) => { vm.TotalText = "678.90"; if (notificationFails) throw new IOException("D641 accepted notification failure."); };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(12345, (await f.Store.GetBudgetsAsync(Ct)).Single().TotalLimit);
        Assert.False(vm.CanSave); Assert.True(vm.IsDirty); Assert.Contains(f.Translator["Budget_SavedDraftChanged"], vm.Error!);
        Assert.Empty(f.Platform.Routes); var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        f.Platform.Confirmations.Enqueue(false); await vm.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        f.Platform.Confirmations.Enqueue(true); await vm.CancelCommand.ExecuteAsync(null); Assert.Single(f.Platform.Routes);
    }

    /// <summary>An accepted original writer cannot alert or navigate after its native root has retired.</summary>
    [Fact]
    public async Task Accepted_budget_after_root_retirement_has_no_late_native_effect()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var vm = Editor(f); var root = true;
        vm.BindRootOwner(() => root); await OpenAsync(vm, budget); vm.TotalText = "123.45";
        f.Store.Changed += (_, _) => { root = false; throw new IOException("D641 retired notification."); };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(12345, (await f.Store.GetBudgetsAsync(Ct)).Single().TotalLimit);
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures); Assert.Empty(f.Platform.Alerts); Assert.False(vm.CanSave);
    }

    /// <summary>Retirement at the real auditing interceptor rolls back the same original writer transaction.</summary>
    [Fact]
    public async Task Original_audit_retirement_rolls_back_budget_without_losing_raw_draft()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var vm = Editor(f); await OpenAsync(vm, budget);
        vm.TotalText = "123.45"; var before = await GoalChooserFlowTests.SnapshotAsync(f); var reached = false;
        f.Time.BeforeRead = () =>
        {
            if (!new StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(AuditingSaveChangesInterceptor))) return;
            reached = true; f.Time.BeforeRead = null; vm.SetViewActive(false);
        };
        try { await vm.SaveCommand.ExecuteAsync(null); } finally { f.Time.BeforeRead = null; }
        Assert.True(reached); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal("123.45", vm.TotalText); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Retired cached rights disable stale Save until the same original source is explicitly reloaded.</summary>
    [Fact]
    public async Task Cached_access_retirement_disables_save_and_preserves_raw_draft_until_retry()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var access = new Access();
        var store = new ZananceStore(AccountDetailOriginalBoundaryTests.Factory(f), f.Time, access);
        var vm = Editor(f, store); await OpenAsync(vm, budget); vm.TotalText = " 123.4500 ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); access.Refused = true; await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.True(vm.CanReload); Assert.Equal(" 123.4500 ", vm.TotalText);
        access.Refused = false; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.Equal(" 123.4500 ", vm.TotalText); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>A display change retires a pending original publication and shares only the new current read.</summary>
    [Fact]
    public async Task Display_change_during_pending_read_resumes_only_current_budget_publication()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var first = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct => { if (!first) return; first = false; entered.SetResult(); await wait.Task.WaitAsync(ct); } };
        var vm = Editor(f, Controlled(f, factory)); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var read = OpenAsync(vm, budget); await entered.Task.WaitAsync(Ct); vm.RefreshDisplay(); await read;
        await vm.EnsureLoadedAsync(); Assert.True(vm.IsReady); Assert.Equal("100.00", vm.TotalText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Native retirement during the original raw total callback prevents later account/limit publication.</summary>
    [Fact]
    public async Task Reentrant_total_publication_stops_later_original_collections()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f);
        await f.Store.SaveAccountAsync(new Account { Name = "D641 unpublished later account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, Ct);
        var vm = Editor(f);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.TotalText)) vm.SetViewActive(false); };
        await OpenAsync(vm, budget); Assert.False(vm.IsReady); Assert.Empty(vm.ScopeAccounts); Assert.Empty(vm.Limits);
    }

    /// <summary>A paused reload must retain the preexisting raw draft even after saved values started publishing.</summary>
    [Fact]
    public async Task Interrupted_reload_resumes_the_original_dirty_total_instead_of_its_partial_saved_publication()
    {
        using var f = new FlowFixture(); var budget = await SeedAsync(f); var vm = Editor(f); await OpenAsync(vm, budget);
        vm.TotalText = " 123.4500 "; var retireOnce = true;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(vm.TotalText) || vm.TotalText != "100.00" || !retireOnce) return;
            retireOnce = false; vm.SetViewActive(false);
        };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.False(vm.IsReady); vm.SetViewActive(true); await vm.EnsureLoadedAsync();
        Assert.True(vm.IsReady); Assert.Equal(" 123.4500 ", vm.TotalText); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
