using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-146: actual reviewed goal-form commands with real SQLite/localization and native-effect ports.</summary>
[Trait("AT", "AT-146")]
public sealed class GoalChooserFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("21212121-3434-4545-6767-898989898989");
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed record Review(FlowFixture Fixture, AccessSource Source, ResourceChoiceStore Store, ActiveItemsViewModel ViewModel);
    private static async Task<Review> CreateAsync(bool bounded = true)
    {
        var f = new FlowFixture(); var accounts = new List<Account>();
        for (var i = 0; i < 5; i++)
        {
            var account = new Account { Name = "Fictitious account " + i, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 12) };
            await f.Store.SaveAccountAsync(account, Ct); accounts.Add(account);
        }
        var goals = f.Services.GetRequiredService<GoalStore>();
        for (var i = 0; i < 5; i++) await goals.SaveGoalAsync(new Goal
        {
            Name = "Fictitious goal " + i, CurrencyCode = "EUR", TargetAmount = 5000, AccountId = accounts[i].Id,
            Type = i == 1 ? GoalType.Earmark : i == 2 ? GoalType.HoldingQuantity : GoalType.AccountBalance,
            State = i == 2 ? GoalState.Paused : i == 3 ? GoalState.Completed : i == 4 ? GoalState.Archived : GoalState.Active,
            Note = "Complete original history",
        }, Ct);
        var source = new AccessSource();
        if (bounded) source.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Scope)));
        var store = new ResourceChoiceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), source);
        var vm = new ActiveItemsViewModel(store, f.Translator, f.Platform); await vm.LoadAsync();
        vm.KindIndex = 2; await vm.PendingKindChange;
        return new(f, source, store, vm);
    }

    internal static async Task<string> SnapshotAsync(FlowFixture f, bool choices = true)
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
            .Append("HomeFinancialRevision").Append("__EFMigrationsHistory").Append("__EFMigrationsLock").Distinct().Order(StringComparer.Ordinal), tables.Order(StringComparer.Ordinal)); var all = new SortedDictionary<string, List<string>>();
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
    public async Task Inactive_goal_form_keeps_every_original_identity_state_and_requires_no_save()
    {
        var r = await CreateAsync(false); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        Assert.Equal(6, vm.KindNames.Count); Assert.Equal(f.Translator["Goals_Title"], vm.KindNames[2]);
        Assert.Equal(2, vm.KindIndex); Assert.Equal(5, vm.Items.Count); Assert.Equal(3, vm.Items.Count(row => row.IsSelected));
        Assert.False(vm.CanChoose); Assert.False(vm.CanSave); Assert.All(vm.Items, row => Assert.False(row.CanToggle));
        Assert.Equal(f.Translator["ActiveItems_Unlimited"], vm.AllowanceText);
        await vm.SaveCommand.ExecuteAsync(null); vm.ToggleCommand.Execute(vm.Items[0]);
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Goal_measurement_completed_and_paused_states_are_distinct_from_unsaved_selection()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f);
        var paused = vm.Items.Single(row => row.Name.EndsWith('2'));
        Assert.Equal(f.Translator["GoalState_Paused"], paused.StateText); Assert.True(paused.IsEligible);
        Assert.Equal(f.Translator["GoalType_HoldingQuantity"], paused.DetailText); Assert.DoesNotContain("EUR", paused.DetailText!);
        Assert.Contains("EUR", vm.Items.Single(row => row.Name.EndsWith('0')).DetailText!);
        var completed = vm.Items.Single(row => row.Name.EndsWith('3'));
        Assert.Equal(f.Translator["GoalState_Completed"], completed.StateText); Assert.False(completed.IsEligible);
        vm.ToggleCommand.Execute(completed); Assert.False(completed.IsSelected);
        vm.ToggleCommand.Execute(paused); Assert.True(paused.IsSelected); Assert.True(vm.IsDirty); Assert.True(vm.CanSave);
        Assert.Contains(paused.Name, paused.AccessibilityText); Assert.Contains(paused.StateText, paused.AccessibilityText);
        Assert.Equal(before, await SnapshotAsync(f));
        vm.ToggleCommand.Execute(vm.Items.Single(row => row.Name.EndsWith('1')));
        Assert.True(vm.OverCapacity); Assert.False(vm.CanSave); Assert.NotNull(vm.CapacityError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_goal_save_changes_only_the_choice_and_never_resumes_a_pause(bool selectPaused)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var before = await SnapshotAsync(f, false);
        if (selectPaused) vm.ToggleCommand.Execute(vm.Items.Single(row => row.Name.EndsWith('2')));
        await vm.SaveCommand.ExecuteAsync(null);
        var read = await r.Store.ReadAsync(QuotaKind.Goals, Ct);
        Assert.Equal(selectPaused ? 1 : 0, read.SelectedIds.Count); Assert.False(read.RequiresSelection);
        Assert.Equal(before, await SnapshotAsync(f, false)); Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        Assert.False(vm.CanChoose); Assert.False(vm.IsDirty);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public async Task Live_translation_keeps_the_goal_identity_index_and_exact_unsaved_choice(string language)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel; var rows = vm.Items.ToArray();
        var paused = vm.Items.Single(row => row.Name.EndsWith('2')); vm.ToggleCommand.Execute(paused); var before = await SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language)); vm.RefreshDisplay();
        Assert.Equal(2, vm.KindIndex); Assert.Equal(rows, vm.Items); Assert.True(paused.IsSelected); Assert.True(vm.IsDirty);
        Assert.Equal(f.Translator["Goals_Title"], vm.KindNames[2]); Assert.Equal(f.Translator["GoalState_Paused"], paused.StateText);
        Assert.Contains(paused.Name, paused.AccessibilityText); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData("rights")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("currency")]
    [InlineData("sql")]
    public async Task Failed_goal_save_retains_the_exact_draft_with_translated_feedback(string change)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel;
        var selected = vm.Items.Single(row => row.Name.EndsWith('2')); vm.ToggleCommand.Execute(selected);
        if (change == "rights") r.Source.Current = CommercialWriteAccess.Inactive;
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            if (change == "sql") await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FictitiousChoiceFailure BEFORE INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT, 'Owned failure'); END;", Ct);
            else
            {
                var goal = await db.Goals.SingleAsync(goal => goal.Id == selected.Id, Ct);
                if (change == "name") goal.Name += " changed";
                if (change == "type") goal.Type = GoalType.Earmark;
                if (change == "currency") goal.CurrencyCode = "GBP";
                await db.SaveChangesAsync(Ct);
            }
        }
        var before = await SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["ActiveItems_SaveFailed"], vm.Feedback); Assert.True(selected.IsSelected); Assert.True(vm.IsDirty);
        Assert.Same(selected, vm.Items.Single(row => row.Id == selected.Id)); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Leaving_the_goal_kind_requires_discard_without_financial_save(bool discard)
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel;
        vm.ToggleCommand.Execute(vm.Items.Single(row => row.Name.EndsWith('0'))); var rows = vm.Items.ToArray(); var before = await SnapshotAsync(f);
        f.Platform.Confirmations.Enqueue(discard); vm.KindIndex = 0; await vm.PendingKindChange;
        Assert.Equal(discard ? 0 : 2, vm.KindIndex); Assert.Single(f.Platform.ConfirmationDialogs);
        if (!discard) { Assert.Equal(rows, vm.Items); Assert.True(vm.IsDirty); }
        else { Assert.False(vm.IsDirty); Assert.Equal(5, vm.Items.Count); }
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task A_malformed_goal_choice_can_be_explicitly_repaired_without_mutating_goals()
    {
        var r = await CreateAsync(); using var f = r.Fixture; var vm = r.ViewModel;
        await r.Store.SaveAsync(await r.Store.ReadAsync(QuotaKind.Goals, Ct), [], Ct);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE ResourceSelections SET IdentitySet='Malformed owned choice'", Ct);
        await vm.ReloadCommand.ExecuteAsync(null); var before = await SnapshotAsync(f, false);
        Assert.DoesNotContain(vm.Items, row => row.IsSelected); vm.ToggleCommand.Execute(vm.Items.Single(row => row.Name.EndsWith('0')));
        await vm.SaveCommand.ExecuteAsync(null); Assert.False((await r.Store.ReadAsync(QuotaKind.Goals, Ct)).RequiresSelection);
        Assert.Equal(before, await SnapshotAsync(f, false));
    }
}
