using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-144: actual choice-form commands against isolated SQLite and explicit native-effect ports.</summary>
[Trait("AT", "AT-144")]
public sealed class ActiveItemsFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("21212121-3434-4545-6767-898989898989");

    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail ? throw new InvalidOperationException("Fictitious read failure.") : Current;
    }

    private sealed record Review(FlowFixture Fixture, AccessSource Source, ResourceChoiceStore Store, ActiveItemsViewModel ViewModel);

    private static async Task<Review> CreateAsync(bool bounded = true)
    {
        var f = new FlowFixture();
        for (var index = 0; index < 5; index++)
            await f.Store.SaveAccountAsync(new Account { Name = "Fictitious account " + index, CurrencyCode = "EUR",
                OpeningDate = new(2026, 10, 12), IsArchived = index == 4 }, Ct);
        var account = (await f.Store.GetAccountsAsync(cancellationToken: Ct)).First();
        await f.Services.GetRequiredService<PlanStore>().SaveSchedulesAsync(Enumerable.Range(0, 8).Select(index => new Schedule
        {
            Name = "Fictitious plan " + index, AccountId = account.Id, Amount = 100,
            State = index == 6 ? ScheduleState.Paused : index == 7 ? ScheduleState.Ended : ScheduleState.Active,
            PausedFrom = index == 6 ? new DateOnly(2026, 10, 12) : null,
            Rule = new() { Start = new(2026, 10, 12), Frequency = Frequency.Monthly },
        }), Ct);
        var source = new AccessSource();
        if (bounded) source.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Scope)));
        var store = new ResourceChoiceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), source);
        return new(f, source, store, new(store, f.Translator, f.Platform));
    }

    private static async Task<string> SnapshotAsync(FlowFixture f, bool choices = true)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        // Keep the complete no-write comparison, including every newly migrated private table.
        Assert.Equal(db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).OfType<string>()
            .Append("__EFMigrationsHistory").Append("__EFMigrationsLock").Distinct().Order(StringComparer.Ordinal), tables.Order(StringComparer.Ordinal)); var all = new SortedDictionary<string, List<string>>();
        foreach (var table in tables.Where(table => choices || table != "ResourceSelections"))
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); all[table] = rows;
        }
        return JsonSerializer.Serialize(all);
    }

    [Fact]
    public async Task Construction_is_covered_and_does_not_write_defaults()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var before = await SnapshotAsync(f);
        Assert.False(r.ViewModel.Loading.IsReady); Assert.Empty(r.ViewModel.Items); Assert.False(r.ViewModel.CanInteract);
        Assert.Equal(before, await SnapshotAsync(f));
        await r.ViewModel.EnsureLoadedAsync(); Assert.True(r.ViewModel.Loading.IsReady);
        Assert.Equal(5, r.ViewModel.Items.Count); Assert.DoesNotContain(r.ViewModel.Items, row => row.IsSelected);
        Assert.True(r.ViewModel.CanSave); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Inactive_release_shows_all_eligible_items_without_a_choice_or_write()
    {
        var r = await CreateAsync(false); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        await vm.LoadAsync(); Assert.False(vm.CanChoose); Assert.False(vm.CanSave);
        Assert.Equal(4, vm.Items.Count(row => row.IsSelected)); Assert.All(vm.Items, row => Assert.False(row.CanToggle));
        vm.ToggleCommand.Execute(vm.Items[0]); await vm.SaveCommand.ExecuteAsync(null);
        vm.KindIndex = 1; await vm.PendingKindChange;
        Assert.Equal(7, vm.Items.Count(row => row.IsSelected)); Assert.Contains(vm.Items, row => row.StateText == f.Translator["Plan_Paused"] && row.IsSelected);
        Assert.Equal(f.Translator["ActiveItems_Unlimited"], vm.AllowanceText); Assert.Equal(before, await SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Draft_selection_reports_capacity_and_never_changes_original_states_or_money()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync(); var before = await SnapshotAsync(f);
        foreach (var row in vm.Items) vm.ToggleCommand.Execute(row);
        Assert.Equal(4, vm.Items.Count(row => row.IsSelected)); Assert.True(vm.OverCapacity); Assert.NotNull(vm.CapacityError); Assert.False(vm.CanSave);
        Assert.False(vm.Items.Single(row => !row.IsEligible).IsSelected);
        vm.ToggleCommand.Execute(vm.Items.First(row => row.IsEligible)); Assert.True(vm.CanSave); Assert.Null(vm.CapacityError);
        Assert.True(vm.IsDirty); Assert.Equal(before, await SnapshotAsync(f));
        await vm.EnsureLoadedAsync(); Assert.Equal(3, vm.Items.Count(row => row.IsSelected)); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_save_persists_only_the_reviewed_choice_and_leaves_once(bool empty)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync(); var before = await SnapshotAsync(f, false);
        if (!empty) vm.ToggleCommand.Execute(vm.Items.First(row => row.IsEligible));
        var ids = vm.Items.Where(row => row.IsSelected).Select(row => row.Id).Order().ToArray();
        await vm.SaveCommand.ExecuteAsync(null); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(ids, (await r.Store.ReadAsync(QuotaKind.FinancialAccounts, Ct)).SelectedIds.Order());
        Assert.Equal(before, await SnapshotAsync(f, false)); Assert.Single(f.Platform.Routes); Assert.Equal("..", f.Platform.Routes[0].Route);
        Assert.False(vm.CanSave); Assert.False(vm.IsDirty);
    }

    [Theory]
    [InlineData("rights")]
    [InlineData("name")]
    [InlineData("currency")]
    [InlineData("type")]
    [InlineData("sql")]
    public async Task Failed_save_preserves_the_exact_draft_and_does_not_navigate_or_write(string change)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        vm.ToggleCommand.Execute(vm.Items.First(row => row.IsEligible)); var selected = vm.Items.Where(row => row.IsSelected).Select(row => row.Id).ToArray();
        if (change == "rights") r.Source.Current = CommercialWriteAccess.Inactive;
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            if (change == "sql") await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FictitiousChoiceFailure BEFORE INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT, 'Fictitious write failure'); END;", Ct);
            else
            {
                var account = await db.Accounts.FirstAsync(Ct);
                if (change == "name") account.Name += " changed";
                if (change == "currency") account.CurrencyCode = "GBP";
                if (change == "type") account.Type = account.Type == AccountType.Cash ? AccountType.Checking : AccountType.Cash;
                await db.SaveChangesAsync(Ct);
            }
        }
        var before = await SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(selected, vm.Items.Where(row => row.IsSelected).Select(row => row.Id)); Assert.True(vm.IsDirty);
        Assert.Equal(f.Translator["ActiveItems_SaveFailed"], vm.Feedback); Assert.Empty(f.Platform.Routes); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public async Task Live_translation_retains_identity_choice_kind_and_complete_stored_rows(string language)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        vm.KindIndex = 1; await vm.PendingKindChange;
        var paused = vm.Items.Single(row => row.StateText == f.Translator["Plan_Paused"]); vm.ToggleCommand.Execute(paused);
        var rows = vm.Items.ToArray(); var before = await SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language)); vm.RefreshDisplay();
        Assert.Equal(1, vm.KindIndex); Assert.Equal(rows, vm.Items); Assert.True(paused.IsSelected); Assert.True(vm.IsDirty);
        Assert.Equal(f.Translator["Plan_Paused"], paused.StateText); Assert.Contains(paused.Name, paused.AccessibilityText);
        Assert.Equal(f.Translator["Tab_Plans"], vm.KindNames[1]); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Changing_kind_requires_discard_and_never_saves_a_draft(bool accept)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        vm.ToggleCommand.Execute(vm.Items.First(row => row.IsEligible)); var rows = vm.Items.ToArray(); var before = await SnapshotAsync(f);
        f.Platform.Confirmations.Enqueue(accept); vm.KindIndex = 1; await vm.PendingKindChange;
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(accept ? 1 : 0, vm.KindIndex);
        if (accept) { Assert.Equal(8, vm.Items.Count); Assert.False(vm.IsDirty); }
        else { Assert.Equal(rows, vm.Items); Assert.True(vm.IsDirty); }
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reload_keeps_or_discards_only_the_unsaved_choice(bool accept)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        vm.ToggleCommand.Execute(vm.Items.First(row => row.IsEligible)); var before = await SnapshotAsync(f);
        f.Platform.Confirmations.Enqueue(accept); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(accept ? 0 : 1, vm.Items.Count(row => row.IsSelected)); Assert.Equal(!accept, vm.IsDirty);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(before, await SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Pending_discard_and_navigation_block_other_commands_through_the_entire_transition()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        var row = vm.Items.First(item => item.IsEligible); vm.ToggleCommand.Execute(row); var before = await SnapshotAsync(f);
        var consent = new TaskCompletionSource<bool>(); var navigation = new TaskCompletionSource();
        f.Platform.PendingConfirmation = () => consent.Task; f.Platform.PendingNavigation = () => navigation.Task;
        var leaving = vm.LeaveCommand.ExecuteAsync(null); Assert.True(vm.IsBusy); Assert.False(vm.CanLeave);
        vm.ToggleCommand.Execute(row); await vm.SaveCommand.ExecuteAsync(null); await vm.ReloadCommand.ExecuteAsync(null);
        vm.KindIndex = 1; Assert.Equal(0, vm.KindIndex); Assert.True(row.IsSelected); Assert.Single(f.Platform.ConfirmationDialogs);
        consent.SetResult(true); Assert.True(vm.IsBusy); Assert.Single(f.Platform.Routes);
        navigation.SetResult(); await leaving; Assert.False(vm.IsBusy); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Committed_choice_with_failed_navigation_cannot_be_saved_again_until_reload()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        var row = vm.Items.First(item => item.IsEligible); vm.ToggleCommand.Execute(row);
        f.Platform.PendingNavigation = () => Task.FromException(new InvalidOperationException("Fictitious navigation failure."));
        await vm.SaveCommand.ExecuteAsync(null); var saved = await SnapshotAsync(f);
        Assert.Equal(f.Translator["ActiveItems_SavedReturnFailed"], vm.Feedback); Assert.False(vm.CanChoose); Assert.False(vm.IsDirty);
        vm.ToggleCommand.Execute(row); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(saved, await SnapshotAsync(f)); Assert.Single(f.Platform.Routes);
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.CanChoose); Assert.True(vm.Items.Single(item => item.Id == row.Id).IsSelected);
        Assert.Equal(saved, await SnapshotAsync(f)); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    /// <summary>Delays only the real context creation, without replacing reads, policy or application publication.</summary>
    private sealed class DelayedContexts(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        internal readonly TaskCompletionSource Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Reads;
        public ZananceDbContext CreateDbContext() => inner.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            Reads++; await Ready.Task.WaitAsync(cancellationToken);
            return await inner.CreateDbContextAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task A_pending_real_read_keeps_all_input_covered_and_coalesces_appearance()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var before = await SnapshotAsync(f);
        var delayed = new DelayedContexts(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = new ActiveItemsViewModel(new(delayed, r.Source), f.Translator, f.Platform);
        Assert.Equal(0, delayed.Reads); var loading = vm.LoadAsync(); await vm.EnsureLoadedAsync();
        Assert.Equal(1, delayed.Reads); Assert.True(vm.IsBusy); Assert.False(vm.CanInteract); Assert.False(vm.CanLeave);
        vm.KindIndex = 1; Assert.Equal(0, vm.KindIndex); await vm.ReloadCommand.ExecuteAsync(null); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1, delayed.Reads); Assert.Empty(vm.Items); Assert.Empty(f.Platform.Routes);
        delayed.Ready.SetResult(); await loading;
        Assert.True(vm.Loading.IsReady); Assert.Equal(5, vm.Items.Count); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Changed_plan_kind_rejects_the_old_display_review_without_losing_its_choice()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; await vm.LoadAsync();
        vm.KindIndex = 1; await vm.PendingKindChange; var row = vm.Items.First(item => item.IsEligible); vm.ToggleCommand.Execute(row);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var plan = await db.Schedules.SingleAsync(item => item.Id == row.Id, Ct);
            plan.Kind = Vafadar.Zanance.Core.Ledger.EntryKind.Income; await db.SaveChangesAsync(Ct);
        }
        var before = await SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(row.IsSelected); Assert.True(vm.IsDirty); Assert.Equal(f.Translator["ActiveItems_SaveFailed"], vm.Feedback);
        Assert.Empty(f.Platform.Routes); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Failed_read_keeps_the_form_covered_and_explicit_retry_publishes_without_a_write()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        r.Source.Fail = true; await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract);
        Assert.Equal(f.Translator["ActiveItems_LoadFailed"], vm.Feedback); Assert.Empty(vm.Items);
        r.Source.Fail = false; await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.Null(vm.Feedback);
        Assert.Equal(5, vm.Items.Count); Assert.Equal(before, await SnapshotAsync(f));
    }
}
