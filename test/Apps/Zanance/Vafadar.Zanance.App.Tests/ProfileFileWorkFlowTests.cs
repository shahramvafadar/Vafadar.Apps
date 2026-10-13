using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-159: original device choices composed with actual application access and real SQLite writers/readers.</summary>
[Trait("AT", "AT-159")]
public sealed class ProfileFileWorkFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Packet = "profiles.work.catalogue";

    [Fact]
    public void Inactive_registration_constructs_and_captures_without_device_or_preference_reads_or_a_cycle()
    {
        using var f = new Fixture(); var reads = f.Preferences.Reads; f.Preferences.FailRead = true; f.Device.Fail = true;
        var source = f.Services.GetRequiredService<ICommercialWriteAccessSource>();
        Assert.Same(CommercialWriteAccess.Inactive, source.Capture(f.MainPath));
        Assert.Equal(reads, f.Preferences.Reads); Assert.Equal(0, f.Device.Reads);
        Assert.Same(f.Profiles, f.Services.GetRequiredService<ProfileService>());
    }

    [Fact]
    public void Original_empty_main_id_keeps_the_verified_financial_scope_and_exact_resource_choices()
    {
        using var f = new Fixture(); f.Enable();
        var scope = new QuotaScope(QuotaScopeKind.PersonalProfile, f.Financial.Scope.Id);
        var accounts = new ResourceSelection(QuotaKind.FinancialAccounts, scope, [Guid.NewGuid()]);
        var plans = new ResourceSelection(QuotaKind.RecurringPlans, scope, [Guid.NewGuid()]);
        var goals = new ResourceSelection(QuotaKind.Goals, scope, [Guid.NewGuid()]);
        var budgets = new BudgetSelection(scope, [new(scope, "EUR", BudgetPeriod.Month, [])]);
        f.Financial.Main = new(f.MainPath, new(ProductPlan.Free, f.Financial.Scope), accounts, plans, goals, budgets);
        var result = f.Access.Capture(f.MainPath);
        Assert.Equal("", result.Context!.ProfileWork!.OriginalId); Assert.True(result.Context.ProfileWork.CanStartNewWork);
        Assert.Equal(f.Financial.Scope, result.Context.Scope); Assert.Equal(ProductPlan.Free, result.Context.PersonalPlan);
        Assert.Same(accounts, result.AccountSelection); Assert.Same(plans, result.PlanSelection);
        Assert.Same(goals, result.GoalSelection); Assert.Same(budgets, result.BudgetSelection);
        Assert.Null(f.Preferences.Get(Packet));
    }

    [Fact]
    public void Each_actual_file_uses_its_original_choice_even_after_the_current_preference_switches()
    {
        using var f = new Fixture(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        var otherPath = Path.Combine(Path.GetDirectoryName(f.MainPath)!, "zanance-" + other.Id + ".db");
        f.Financial.Others[otherPath] = new(otherPath, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.ParseExact(other.Id, "N"))));
        var main = f.Access.Capture(f.MainPath); var later = f.Access.Capture(otherPath);
        Assert.False(main.Context!.ProfileWork!.CanStartNewWork); Assert.True(later.Context!.ProfileWork!.CanStartNewWork);
        Assert.Equal("", main.Context.ProfileWork.OriginalId); Assert.Equal(other.Id, later.Context.ProfileWork.OriginalId);
        f.Preferences.Set("profiles.current", other.Id);
        Assert.Equal(main, f.Access.Capture(f.MainPath)); Assert.Equal(later, f.Access.Capture(otherPath));
        Assert.NotEqual(main.Context.Scope, later.Context.Scope);
    }

    [Theory, InlineData("missing"), InlineData("empty"), InlineData("foreign"), InlineData("malformed"), InlineData("unknown-file")]
    public void Unresolved_original_activity_blocks_only_new_work_and_never_guesses_main(string state)
    {
        using var f = new Fixture(); f.AddOther(); f.Enable(); var path = f.MainPath;
        if (state == "empty") f.Choose();
        if (state == "foreign")
        {
            f.Choose(""); var parts = f.Preferences.Get(Packet)!.Split('|'); parts[4] = Guid.NewGuid().ToString("N"); f.Preferences.Set(Packet, string.Join('|', parts));
        }
        if (state == "malformed") f.Preferences.Set(Packet, "unreadable original catalogue");
        if (state == "unknown-file")
        {
            path = Path.Combine(Path.GetDirectoryName(f.MainPath)!, "unlisted-original.db");
            f.Financial.Others[path] = new(path, new(ProductPlan.Free, f.Financial.Scope));
        }
        var result = f.Access.Capture(path); var work = result.Context!.ProfileWork!;
        Assert.False(work.CanStartNewWork); Assert.Equal(state is "malformed" or "unknown-file", work.OriginalId is null);
        Assert.Equal(FeaturePermission.RequiresProfileSelection, PlanPolicy.Check(CommercialFeature.Transactions, result.Context));
        foreach (var feature in new[] { CommercialFeature.History, CommercialFeature.Corrections, CommercialFeature.BackupRestore, CommercialFeature.BasicExport, CommercialFeature.DeleteData })
            Assert.Equal(FeaturePermission.Allowed, PlanPolicy.Check(feature, result.Context));
        Assert.Equal(f.Financial.Scope, result.Context.Scope);
    }

    [Theory, InlineData("entry"), InlineData("account"), InlineData("plan"), InlineData("goal"), InlineData("budget"), InlineData("template")]
    public async Task Actual_new_money_and_definitions_reject_unchosen_profiles_before_any_rows_change(string kind)
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow);
        Func<Task> operation = kind switch
        {
            "entry" => () => f.Store.SaveEntryAsync(new LedgerEntry { AccountId = f.Account.Id, Kind = EntryKind.Expense, Amount = 4321, Date = new(2026, 10, 9) }, cancellationToken: Ct),
            "account" => () => f.Store.SaveAccountAsync(new Account { Name = "Fictitious new", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 9) }, Ct),
            "plan" => () => f.Plans.SaveSchedulesAsync([new Schedule { Name = "Fictitious new", AccountId = f.Account.Id, Amount = 4321, Rule = new() { Start = new(2026, 10, 9) } }], Ct),
            "goal" => () => f.Goals.SaveGoalAsync(new Goal { Name = "Fictitious new", Type = GoalType.AccountBalance, AccountId = f.Account.Id, CurrencyCode = "EUR", TargetAmount = 54321 }, Ct),
            "budget" => () => f.Store.SaveBudgetAsync(new Budget { Year = 2026, Month = 10, CurrencyCode = "USD", TotalLimit = 12345 }, Ct),
            "template" => () => f.Store.SaveTemplateAsync(new EntryTemplate { Name = "Fictitious new", AccountId = f.Account.Id, Kind = EntryKind.Expense, Amount = 54321 }, Ct),
            _ => throw new InvalidOperationException(),
        };
        var rejection = await Assert.ThrowsAsync<CommercialWriteRejectedException>(operation);
        Assert.Equal(FeaturePermission.RequiresProfileSelection, rejection.Permission);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
    }

    [Fact]
    public async Task Complete_original_plan_goal_budget_and_template_inputs_remain_while_future_work_stops()
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow);
        var plans = await f.Plans.GetWorkSnapshotAsync(Ct); var goals = await f.Goals.GetWorkSnapshotAsync(Ct);
        var budgets = await f.Services.GetRequiredService<BudgetChoiceStore>().ReadAsync(Ct);
        var quick = await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        Assert.Equal(f.Plan.Id, Assert.Single(plans.Schedules).Id); Assert.False(plans.Work.CanGenerate(f.Plan.Id)); Assert.False(plans.Work.CanRemind(f.Plan.Id));
        Assert.Equal(f.Goal.Id, Assert.Single(goals.Goals).Id); Assert.False(goals.Work.CanUseGoal(f.Goal.Id)); Assert.True(goals.Work.CanCorrect);
        Assert.Equal(f.Budget.Id, Assert.Single(budgets.Items).Id); Assert.False(budgets.Work.CanUse(f.Budget.Id)); Assert.True(budgets.Work.CanCorrect);
        Assert.Equal(f.Template.Id, Assert.Single(quick.Templates).Id); Assert.False(quick.Work.CanApplyTemplate(f.Template.Id));
        var balance = f.Account.OpeningBalance - f.Entry.Amount;
        Assert.Equal(balance, Assert.Single(GoalProgressService.Evaluate(goals.Goals, goals.Allocations,
            [f.Account], [f.Entry], goals.ContributionPlans, new(2026, 10, 9))).Current);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
    }

    [Fact]
    public async Task Retained_ledger_account_plan_goal_budget_and_template_corrections_still_use_the_actual_writer()
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        f.Entry.Note = "Fictitious retained correction"; Assert.True((await f.Store.SaveEntryAsync(f.Entry, cancellationToken: Ct)).Succeeded);
        f.Account.Name = "Fictitious corrected original"; Assert.True(await f.Store.SaveAccountAsync(f.Account, Ct));
        f.Plan.Note = "Fictitious retained plan"; await f.Plans.SaveSchedulesAsync([f.Plan], Ct);
        f.Goal.Note = "Fictitious retained goal"; await f.Goals.SaveGoalAsync(f.Goal, Ct);
        f.Budget.TotalLimit = 23456; await f.Store.SaveBudgetAsync(f.Budget, Ct);
        f.Template.Name = "Fictitious corrected template"; await f.Store.SaveTemplateAsync(f.Template, Ct);
        Assert.False(f.Access.Capture(f.MainPath).Context!.ProfileWork!.CanStartNewWork);
        Assert.Equal(f.Entry.Note, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Note);
        Assert.Equal(f.Entry.Amount, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        Assert.Equal(f.Plan.Rule.Start, Assert.Single(await f.Plans.GetSchedulesAsync(Ct)).Rule.Start);
        Assert.Equal(f.Goal.Note, Assert.Single((await f.Goals.GetWorkSnapshotAsync(Ct)).Goals).Note);
        Assert.Equal(f.Budget.TotalLimit, Assert.Single(await f.Store.GetBudgetsAsync(Ct)).TotalLimit);
    }

    [Fact]
    public async Task Unchosen_actual_profile_backup_and_restore_keep_all_complete_original_rows()
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow); var backup = Assert.Single(f.Services.GetServices<IBackupSource>());
        using var stream = new MemoryStream(); await backup.WriteAsync(stream, Ct); Assert.True(stream.Length > 0);
        stream.Position = 0; await backup.RestoreAsync(stream, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
        Assert.False(f.Access.Capture(f.MainPath).Context!.ProfileWork!.CanStartNewWork);
    }

    [Theory, InlineData("choice"), InlineData("aba"), InlineData("name"), InlineData("legacy"), InlineData("device")]
    public async Task Complete_catalogue_and_provider_changes_retire_actual_reviews_even_when_activity_returns_to_the_same_value(string change)
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose("");
        var original = await f.Plans.GetWorkSnapshotAsync(Ct); var captured = f.Access.Capture(f.MainPath);
        switch (change)
        {
            case "choice": f.Choose(other.Id); break;
            case "aba": f.Choose(other.Id); f.Choose(""); break;
            case "name": f.Profiles.Rename(other, "Fictitious renamed original"); break;
            case "legacy": f.Preferences.Set("profiles.main.name", "Fictitious external main name"); break;
            case "device": f.Device.Current = new(f.MainPath, new(QuotaScopeKind.Device, Guid.NewGuid()), f.Device.Current.Context!); break;
        }
        Assert.NotEqual(captured, f.Access.Capture(f.MainPath));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Plans.ValidateWorkSnapshotAsync(original, Ct));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Retained_saved_queries_remain_viewable_and_correctable_without_a_new_definition_slot()
    {
        using var f = new Fixture(); await f.SeedAsync();
        var query = new SavedFilter { Name = "Fictitious original history query", AccountId = f.Account.Id, Period = 3, Search = "retained" };
        await f.Store.SaveSavedFilterAsync(query, Ct); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow);
        var review = await f.Store.GetQuickResourceReviewAsync(cancellationToken: Ct);
        Assert.True(review.Work.CanViewFilter(query.Id)); Assert.False(review.Work.CanCreateFilter);
        Assert.Equal(query.Search, Assert.Single(review.Filters).Search);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveSavedFilterAsync(
            new SavedFilter { Name = "Fictitious new history query", Period = 3 }, Ct, review));
        Assert.Equal(FeaturePermission.RequiresProfileSelection, rejected.Permission);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
        query.Search = "explicit retained correction"; await f.Store.SaveSavedFilterAsync(query, Ct, review);
        Assert.Equal(query.Search, Assert.Single(await f.Store.GetSavedFiltersAsync(Ct)).Search);
    }

    [Fact]
    public async Task Writer_captured_before_SQLite_ownership_rechecks_the_original_device_choice_and_rolls_back_new_money()
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose("");
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow); var captured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var captureCount = 0;
        // The adapter's second financial capture occurs after its complete catalogue stamp is calculated.
        f.Financial.AfterCapture = () => { if (Interlocked.Increment(ref captureCount) == 2) captured.TrySetResult(); };
        await using var blocker = await f.Factory.CreateDbContextAsync(Ct); await using var held = await blocker.Database.BeginTransactionAsync(Ct);
        var write = Task.Run(() => f.Store.SaveEntryAsync(new LedgerEntry { AccountId = f.Account.Id, Amount = 4321, Kind = EntryKind.Expense, Date = new(2026, 10, 9) }, cancellationToken: Ct), Ct);
        await captured.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); f.Financial.AfterCapture = null; f.Choose(other.Id);
        await held.RollbackAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => write);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
    }

    [Fact]
    public async Task Refund_and_original_overdue_settlement_keep_correction_rights_while_new_occurrence_money_stops()
    {
        using var f = new Fixture(); await f.SeedAsync(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id);
        var refund = new LedgerEntry { AccountId = f.Account.Id, Kind = EntryKind.Refund, Amount = 300,
            Date = new(2026, 10, 9), RefundOfId = f.Entry.Id };
        Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var amount = Assert.IsType<long>(f.Plan.Amount);
        var overdue = new LedgerEntry { AccountId = f.Account.Id, Kind = EntryKind.Expense, Amount = amount,
            Date = new(2026, 10, 9), ScheduleId = f.Plan.Id, OccurrenceDate = new(2026, 10, 2),
            Source = EntrySource.Schedule, Review = ReviewState.Confirmed };
        Assert.True((await f.Store.SaveEntryAsync(overdue, Ct)).Succeeded);
        var before = await GoalChooserFlowTests.SnapshotAsync(f.Flow);
        var future = new LedgerEntry { AccountId = f.Account.Id, Kind = EntryKind.Expense, Amount = amount,
            Date = new(2026, 11, 2), ScheduleId = f.Plan.Id, OccurrenceDate = new(2026, 11, 2),
            Source = EntrySource.Schedule, Review = ReviewState.Confirmed };
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(future, Ct));
        Assert.Equal(FeaturePermission.RequiresProfileSelection, rejected.Permission);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f.Flow));
    }

    [Fact]
    public void Shared_membership_never_inherits_personal_device_choices_or_requires_personal_catalogue_reads()
    {
        using var f = new Fixture(); var path = Path.Combine(Path.GetDirectoryName(f.MainPath)!, "shared.db");
        var space = Guid.NewGuid(); var original = new CommercialWriteAccess(path, new(ProductPlan.Free,
            new(EntitlementScopeKind.SharedSpace, space), new(space, true, true)));
        f.Financial.Others[path] = original; f.Preferences.FailRead = true; f.Device.Fail = true;
        Assert.Same(original, f.Access.Capture(path));
        Assert.Equal(FeaturePermission.Allowed, PlanPolicy.Check(CommercialFeature.Transactions, original.Context!));
        Assert.Throws<ArgumentException>(() => new CapabilityContext(ProductPlan.Free, original.Context!.Scope,
            original.Context.SharedAccess, new("", false, true, "fictitious-review")));
    }

    [Theory, InlineData(ProductPlan.Plus), InlineData(ProductPlan.Pro)]
    public void Unlimited_verified_personal_rights_release_old_choices_without_creating_new_financial_scopes(ProductPlan plan)
    {
        using var f = new Fixture(); var other = f.AddOther(); f.Enable(); f.Choose(other.Id); f.Enable(plan);
        var access = f.Access.Capture(f.MainPath); Assert.True(access.Context!.ProfileWork!.CanStartNewWork);
        Assert.Equal(f.Financial.Scope, access.Context.Scope); Assert.Equal("", access.Context.ProfileWork.OriginalId);
        Assert.Equal(plan, access.Context.PersonalPlan);
    }

    [Fact]
    public void Device_activity_cannot_grant_paid_tools_or_reuse_mismatched_or_foreign_verified_facts()
    {
        using var f = new Fixture(); f.Enable(); var context = f.Access.Capture(f.MainPath).Context!;
        Assert.Equal(FeaturePermission.RequiresPlus, PlanPolicy.Check(CommercialFeature.AdvancedGoals, context));
        f.Device.Current = new(f.MainPath, f.Device.Scope, new(ProductPlan.Plus, f.Device.Personal));
        Assert.Throws<InvalidOperationException>(() => f.Access.Capture(f.MainPath));
        f.Device.Current = new(Path.Combine(Path.GetDirectoryName(f.MainPath)!, "foreign.db"), f.Device.Scope, new(ProductPlan.Free, f.Device.Personal));
        Assert.Throws<InvalidOperationException>(() => f.Access.Capture(f.MainPath));
        f.Financial.Main = new(Path.Combine(Path.GetDirectoryName(f.MainPath)!, "foreign.db"), new(ProductPlan.Free, f.Financial.Scope));
        Assert.Throws<InvalidOperationException>(() => f.Access.Capture(f.MainPath));
    }

    [Theory, InlineData(false), InlineData(true)]
    public void Decoration_preserves_container_owned_factory_and_external_instance_lifetimes(bool external)
    {
        var source = new DisposableSource(); var services = new ServiceCollection(); var created = 0;
        if (external) services.AddSingleton<ICommercialWriteAccessSource>(source);
        else services.AddSingleton<ICommercialWriteAccessSource>(_ => { created++; return source; });
        services.DecorateZananceCommercialAccess((_, inner) => new IdentityAdapter(inner));
        using (var provider = services.BuildServiceProvider())
        {
            Assert.Same(provider.GetRequiredService<ICommercialWriteAccessSource>(), provider.GetRequiredService<ICommercialWriteAccessSource>());
            Assert.Same(CommercialWriteAccess.Inactive, provider.GetRequiredService<ICommercialWriteAccessSource>().Capture("fictitious.db"));
        }
        Assert.Equal(external ? 0 : 1, source.Disposals); Assert.Equal(external ? 0 : 1, created);
    }

    internal sealed class Fixture : IDisposable
    {
        internal readonly FlowFixture Flow = new(); internal readonly Preferences Preferences = new(); internal readonly DeviceSource Device = new();
        internal readonly FinancialSource Financial; internal readonly ServiceProvider Services;
        internal ProfileService Profiles => Services.GetRequiredService<ProfileService>();
        internal ZananceStore Store => Services.GetRequiredService<ZananceStore>(); internal PlanStore Plans => Services.GetRequiredService<PlanStore>();
        internal GoalStore Goals => Services.GetRequiredService<GoalStore>(); internal ICommercialWriteAccessSource Access => Services.GetRequiredService<ICommercialWriteAccessSource>();
        internal IDbContextFactory<ZananceDbContext> Factory => Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        internal string MainPath => Flow.Directory.Combine("zanance.db");
        internal readonly Account Account = new() { Name = "Fictitious retained account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 5000 };
        internal LedgerEntry Entry = null!; internal Schedule Plan = null!; internal Goal Goal = null!; internal Budget Budget = null!; internal EntryTemplate Template = null!;
        internal Fixture()
        {
            Financial = new();
            Services = new ServiceCollection().AddSingleton<TimeProvider>(Flow.Time).AddSingleton<ICommercialWriteAccessSource>(Financial)
                .AddSingleton<ISettingsStore>(Preferences).AddSingleton<IProfileWorkAccessSource>(Device).AddSingleton<IProfileRuntime, UnexpectedRuntime>()
                .AddSingleton(Flow.Translator).AddZananceData(MainPath).AddSingleton<ProfileService>()
                .DecorateZananceCommercialAccess((_, source) => new ProfileCommercialAccessSource(source, Preferences, Device, MainPath)).BuildServiceProvider();
        }
        internal LocalProfile AddOther() => Profiles.Create("Fictitious original later profile");
        internal void Enable(ProductPlan plan = ProductPlan.Free)
        {
            Financial.Main = new(MainPath, new(plan, Financial.Scope)); Device.Current = new(MainPath, Device.Scope, new(plan, Device.Personal));
        }
        internal void Choose(params string[] ids) => Profiles.SaveWorkChoice(Profiles.ReadWorkReview(), ids);
        internal async Task SeedAsync()
        {
            await Store.SaveAccountAsync(Account, Ct); Entry = new() { AccountId = Account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense, Amount = 1234, Title = "Fictitious retained expense" };
            Assert.True((await Store.SaveEntryAsync(Entry, cancellationToken: Ct)).Succeeded);
            Plan = new() { Name = "Fictitious retained monthly plan", AccountId = Account.Id, Amount = 1234, ReminderEnabled = true, Rule = new() { Start = new(2026, 10, 2), Frequency = Frequency.Monthly } };
            await Plans.SaveSchedulesAsync([Plan], Ct);
            Goal = new() { Name = "Fictitious retained balance goal", Type = GoalType.AccountBalance, AccountId = Account.Id, CurrencyCode = "EUR", TargetAmount = 12345 };
            await Goals.SaveGoalAsync(Goal, Ct); Budget = new() { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 12345 };
            await Store.SaveBudgetAsync(Budget, Ct); Template = EntryTemplate.From(Entry, "Fictitious retained template", true); await Store.SaveTemplateAsync(Template, Ct);
        }
        public void Dispose() { Services.Dispose(); Flow.Dispose(); }
    }
    internal sealed class Preferences : ISettingsStore
    {
        private readonly InMemorySettingsStore _inner = new(); internal int Reads; internal bool FailRead;
        public string? Get(string key) { Reads++; return FailRead ? throw new IOException("Fictitious preference failure.") : _inner.Get(key); }
        public void Set(string key, string? value) => _inner.Set(key, value); public void Remove(string key) => _inner.Remove(key);
    }
    internal sealed class DeviceSource : IProfileWorkAccessSource
    {
        internal ProfileWorkAccess Current = ProfileWorkAccess.Inactive; internal int Reads; internal bool Fail;
        internal readonly QuotaScope Scope = new(QuotaScopeKind.Device, Guid.NewGuid()); internal readonly EntitlementScope Personal = new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid());
        public ProfileWorkAccess Capture(string cataloguePath) { Reads++; return Fail ? throw new IOException("Fictitious device read.") : Current; }
    }
    internal sealed class FinancialSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Main = CommercialWriteAccess.Inactive; internal readonly Dictionary<string, CommercialWriteAccess> Others = new(StringComparer.Ordinal);
        internal readonly EntitlementScope Scope = new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid()); internal Action? AfterCapture;
        public CommercialWriteAccess Capture(string databasePath) { var value = Others.GetValueOrDefault(databasePath, Main); AfterCapture?.Invoke(); return value; }
    }
    internal sealed class UnexpectedRuntime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new InvalidOperationException("Unexpected native profile switch.");
        public Task MigrateAsync(Vafadar.Data.DatabaseAccessGate.ExclusiveLease owner) => throw new InvalidOperationException("Unexpected destination migration."); public Task<bool> AuthenticateAsync(string reason) => throw new InvalidOperationException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new InvalidOperationException("Unexpected lock reload."); public void DismissUndo() => throw new InvalidOperationException("Unexpected Undo dismissal.");
    }
    private sealed class DisposableSource : ICommercialWriteAccessSource, IDisposable
    {
        internal int Disposals; public CommercialWriteAccess Capture(string databasePath) => CommercialWriteAccess.Inactive; public void Dispose() => Disposals++;
    }
    private sealed class IdentityAdapter(ICommercialWriteAccessSource inner) : ICommercialWriteAccessSource
    { public CommercialWriteAccess Capture(string databasePath) => inner.Capture(databasePath); }
}
