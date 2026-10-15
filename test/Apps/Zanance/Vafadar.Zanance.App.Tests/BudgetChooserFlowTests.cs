using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-151: actual budget-definition chooser commands against complete original SQLite data.</summary>
[Trait("AT", "AT-151")]
public sealed class BudgetChooserFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("81818181-3434-4545-6767-898989898989");
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail ? throw new InvalidOperationException("Owned read failure.") : Current;
    }
    private sealed record Review(FlowFixture Fixture, AccessSource Source, BudgetChoiceStore Store, ActiveItemsViewModel ViewModel,
        Budget[] Budgets, Account[] Accounts);
    private static async Task<Review> CreateAsync(bool bounded = true)
    {
        var f = new FlowFixture();
        var accounts = new[] { new Account { Name = "Original archived household account 4", CurrencyCode = "EUR", IsArchived = true, OpeningDate = new(2026, 9, 1) },
            new Account { Name = "Original active account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1) } };
        foreach (var account in accounts) await f.Store.SaveAccountAsync(account, Ct);
        var budgets = new[] {
            new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 12345, Method = BudgetMethod.Envelopes,
                AccountIds = [accounts[0].Id, accounts[1].Id], CategoryLimits = [new() { CategoryId = Guid.NewGuid(), Limit = 2345 }] },
            new Budget { CurrencyCode = "eur", Year = 2026, Month = 11, TotalLimit = 54321, AccountIds = [accounts[1].Id, accounts[0].Id, accounts[0].Id] },
            new Budget { CurrencyCode = "USD", Year = 2026, Month = 10, TotalLimit = 99999 },
            new Budget { CurrencyCode = "GBP", Year = 2026, Month = 9, TotalLimit = 98765 } };
        foreach (var budget in budgets) await f.Store.SaveBudgetAsync(budget, Ct);
        var source = new AccessSource();
        if (bounded) source.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = new BudgetChoiceStore(factory, source, f.Time);
        var vm = new ActiveItemsViewModel(new(factory, source), f.Translator, f.Platform, store);
        await vm.LoadAsync(); vm.KindIndex = 3; await vm.PendingKindChange;
        return new(f, source, store, vm, budgets, accounts);
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
        Assert.Equal(33, tables.Count); var all = new SortedDictionary<string, List<string>>();
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
    private static ActiveItemRow Row(Review r, string currency) => r.ViewModel.Items.Single(item => item.Name.Contains(currency, StringComparison.Ordinal));

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    public async Task Persian_display_digits_format_the_period_count_without_rewriting_original_account_names(string language)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var before = await SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        Vafadar.Localization.Formatting.NativeDigits.IsEnabled = true; r.ViewModel.RefreshDisplay();
        Assert.True(Vafadar.Localization.Formatting.NativeDigits.IsEnabled);
        Assert.Equal("۴", Vafadar.Localization.Formatting.NativeDigits.Apply("4"));
        var euro = Row(r, "EUR");
        Assert.Contains(r.Accounts[0].Name, euro.DetailText, StringComparison.Ordinal);
        Assert.Contains(f.Translator.Format("ActiveItems_BudgetDetails", string.Join(", ", r.Accounts.OrderBy(account => account.Id).Select(account => account.Name)),
            f.Translator["Budget_MethodEnvelopes"], "۲"), euro.DetailText, StringComparison.Ordinal);
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_kind_groups_copied_periods_preserves_archived_scope_names_and_keeps_history_unselectable()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        Assert.Equal(6, vm.KindNames.Count); Assert.Equal(f.Translator["Budget_Title"], vm.KindNames[3]); Assert.Equal(3, vm.Items.Count);
        var euro = Row(r, "EUR"); Assert.Contains(r.Accounts[0].Name, euro.DetailText); Assert.Contains(r.Accounts[1].Name, euro.DetailText);
        Assert.Contains(f.Translator["Budget_MethodEnvelopes"], euro.DetailText); Assert.True(euro.IsEligible); Assert.False(euro.IsSelected);
        var history = Row(r, "GBP"); Assert.False(history.IsEligible); Assert.Equal(f.Translator["ActiveItems_History"], history.StateText);
        vm.ToggleCommand.Execute(history); Assert.False(vm.IsDirty); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Inactive_release_shows_two_current_definitions_without_choosing_period_copies_or_enabling_Save()
    {
        var r = await CreateAsync(false); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        Assert.False(vm.CanChoose); Assert.False(vm.CanSave); Assert.Equal(2, vm.Items.Count(item => item.IsSelected));
        Assert.All(vm.Items, row => Assert.False(row.CanToggle)); Assert.Equal(f.Translator["ActiveItems_Unlimited"], vm.AllowanceText);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Explicit_paired_period_choice_writes_only_its_definition_and_grants_no_paid_method()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var before = await SnapshotAsync(f, false); var vm = r.ViewModel;
        vm.ToggleCommand.Execute(Row(r, "EUR")); Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await SnapshotAsync(f, false)); Assert.Single(f.Platform.Routes); Assert.False(vm.CanSave); Assert.False(vm.IsDirty);
        var read = await r.Store.ReadAsync(Ct); Assert.Single(read.SelectedDefinitions); Assert.Equal("EUR", read.SelectedDefinitions[0].CurrencyCode);
        Assert.False(read.Work.CanUse(r.Budgets[0].Id)); Assert.False(read.Work.CanUse(r.Budgets[1].Id));
    }

    [Fact]
    public async Task Capacity_feedback_retains_the_full_unsaved_definition_set_and_writes_nothing()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        vm.ToggleCommand.Execute(Row(r, "EUR")); vm.ToggleCommand.Execute(Row(r, "USD"));
        Assert.True(vm.OverCapacity); Assert.False(vm.CanSave); Assert.NotNull(vm.CapacityError); Assert.True(vm.IsDirty);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(2, vm.Items.Count(item => item.IsSelected)); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("de")]
    public async Task Live_caption_changes_retain_original_scope_and_exact_explicit_selection(string language)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; vm.ToggleCommand.Execute(Row(r, "USD"));
        var ids = vm.Items.Where(row => row.IsSelected).Select(row => row.Id).ToArray(); var before = await SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language)); vm.RefreshDisplay();
        Assert.Equal(3, vm.KindIndex); Assert.Equal(ids, vm.Items.Where(row => row.IsSelected).Select(row => row.Id));
        Assert.Equal(f.Translator["Budget_Title"], vm.KindNames[3]); Assert.Contains(r.Accounts[0].Name, Row(r, "EUR").DetailText);
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Concurrent_reference_name_change_rejects_Save_without_discarding_the_original_draft()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; vm.ToggleCommand.Execute(Row(r, "EUR"));
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Accounts SET Name={"Owned concurrent name"} WHERE Id={r.Accounts[0].Id}", Ct);
        var before = await SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["ActiveItems_SaveFailed"], vm.Feedback); Assert.True(Row(r, "EUR").IsSelected);
        Assert.Contains(r.Accounts[0].Name, Row(r, "EUR").DetailText); Assert.True(vm.IsDirty); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Failed_SQL_preserves_choice_draft_and_committed_navigation_failure_cannot_resubmit_it()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; vm.ToggleCommand.Execute(Row(r, "USD"));
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_owned_choice BEFORE INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT,'owned fixture'); END;", Ct);
        var before = await SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null); Assert.True(vm.IsDirty); Assert.Equal(before, await SnapshotAsync(f));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_owned_choice;", Ct);
        f.Platform.PendingNavigation = () => throw new InvalidOperationException("Owned return failure."); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["ActiveItems_SavedReturnFailed"], vm.Feedback); Assert.False(vm.CanSave); Assert.False(vm.IsDirty);
        before = await SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Declining_a_kind_switch_keeps_the_budget_draft_and_approved_discard_posts_no_money()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; vm.ToggleCommand.Execute(Row(r, "EUR")); var before = await SnapshotAsync(f);
        f.Platform.Confirmations.Enqueue(false); vm.KindIndex = 0; await vm.PendingKindChange;
        Assert.Equal(3, vm.KindIndex); Assert.True(Row(r, "EUR").IsSelected); Assert.True(vm.IsDirty);
        f.Platform.Confirmations.Enqueue(true); vm.KindIndex = 0; await vm.PendingKindChange;
        Assert.Equal(0, vm.KindIndex); Assert.Null(vm.BudgetHint); Assert.False(vm.IsDirty); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Failed_covered_reload_retains_selected_data_until_explicit_retry_without_a_write()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        r.Source.Fail = true; await vm.LoadAsync(); Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanInteract);
        r.Source.Fail = false; await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.Equal(3, vm.Items.Count);
        Assert.Equal(before, await SnapshotAsync(f));
    }
}
