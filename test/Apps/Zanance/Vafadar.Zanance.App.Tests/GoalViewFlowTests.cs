using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-148: actual list/Home/detail sources, complete financial inputs and explicitly reviewed SQLite writes.</summary>
[Trait("AT", "AT-148")]
public sealed class GoalViewFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("41414141-5252-6363-7474-858585858585");
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail
            ? throw new InvalidOperationException("Fictitious failed read.") : Current;
    }
    private sealed record Seed(Goal[] Items, Account Account, Account Retained, AssetType Type, AccessSource Access,
        GoalStore Goals, ResourceChoiceStore Choices, GoalPresenter Presenter, GoalDetailViewModel Detail);

    private static async Task<Seed> SeedAsync(FlowFixture f)
    {
        var day = DateOnly.FromDateTime(f.Time.Now.DateTime);
        var account = new Account { Name = "Owned funding", CurrencyCode = "EUR", OpeningDate = day.AddDays(-90), OpeningBalance = 10000 };
        var retained = new Account { Name = "Owned archived funding", CurrencyCode = "EUR", OpeningDate = day.AddDays(-90), OpeningBalance = 5000, IsArchived = true };
        await f.Store.SaveAccountAsync(account, Ct); await f.Store.SaveAccountAsync(retained, Ct);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Owned counted item", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", Divisible = true };
        Assert.True(await holdings.SaveTypeAsync(type, Ct));
        var writer = f.Services.GetRequiredService<GoalStore>();
        var items = Enumerable.Range(0, 6).Select(i => new Goal
        {
            Name = "Owned goal " + i, CurrencyCode = "EUR", TargetAmount = 50000, TargetDate = day.AddDays(30),
            Type = i == 0 ? GoalType.AccountBalance : i == 5 ? GoalType.HoldingQuantity : GoalType.Earmark,
            AccountId = i == 0 ? account.Id : null, AssetTypeId = i == 5 ? type.Id : null,
            Priority = i == 1 ? GoalPriority.High : GoalPriority.Low,
            HomePin = i is 0 or 1 ? i + 1 : null,
            State = i == 3 ? GoalState.Completed : i == 4 ? GoalState.Paused : GoalState.Active,
        }).ToArray();
        foreach (var goal in items) await writer.SaveGoalWithContributionPlanAsync(goal, new ContributionPlan
        { GoalId = goal.Id, Amount = 1000, AssumedPricePerUnitMilli = goal.Type == GoalType.HoldingQuantity ? 1000000 : null,
            Rule = new() { Start = day, Frequency = Frequency.Monthly } }, Ct);
        foreach (var goal in items.Skip(1).Take(2)) await writer.AddAllocationAsync(new()
        { GoalId = goal.Id, AccountId = account.Id, Amount = 8000, Date = day }, Ct);
        await writer.AddAllocationAsync(new() { GoalId = items[3].Id, AccountId = retained.Id, Amount = 2400, Date = day }, Ct);
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced; await f.Store.SaveSettingsAsync(settings, Ct);
        var access = new AccessSource(); var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var goals = new GoalStore(factory, access); var dates = new DateFormatter(f.Localization);
        var presenter = new GoalPresenter(f.Translator, dates, f.Localization, holdings, new HoldingText(f.Translator, f.Localization, dates));
        return new(items, account, retained, type, access, goals, new(factory, access), presenter,
            new(f.Store, goals, f.Services.GetRequiredService<PlanStore>(), holdings, presenter, f.Translator, dates, f.Localization, f.Time, f.Platform));
    }
    private static void Enable(FlowFixture f, Seed s, ProductPlan plan = ProductPlan.Free) =>
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(plan, new(EntitlementScopeKind.PersonalProfile, Scope)));
    private static async Task ChooseAsync(Seed s, params Guid[] ids) =>
        await s.Choices.SaveAsync(await s.Choices.ReadAsync(QuotaKind.Goals, Ct), ids, Ct);
    private static Task<string> RowsAsync(FlowFixture f) => GoalChooserFlowTests.SnapshotAsync(f);
    /// <summary>Home's single-reader packet preserves original financial goal rows and separate new-work availability.</summary>
    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), Trait("AT", "AT-283")]
    public async Task Captured_home_projects_the_same_complete_goal_packets_without_additional_reads(string language)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); Enable(f, seed); await ChooseAsync(seed, seed.Items[0].Id);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        var day = DateOnly.FromDateTime(f.Time.Now.DateTime); var before = await RowsAsync(f);
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, seed.Access);
        var captured = await store.GetHomeSnapshotAsync(day, 0, Ct);
        var expected = await GoalHomeProjection.ReadAsync(seed.Goals, seed.Presenter, f.Translator,
            captured.QuickResources.Accounts, captured.Entries, day);
        var actual = GoalHomeProjection.Project(captured, seed.Presenter, f.Translator, day);
        Assert.Equal(expected.Pinned, actual.Pinned); Assert.Equal(expected.Attention, actual.Attention);
        await store.ValidateHomeSnapshotAsync(captured, Ct);
        Assert.Equal(before, await RowsAsync(f));
    }

    /// <summary>The covered asynchronous Home projection retains all original financial rows and localized output.</summary>
    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), Trait("AT", "AT-283")]
    public async Task Worker_home_projection_preserves_original_goal_packets_and_complete_source(string language)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); Enable(f, seed);
        await ChooseAsync(seed, seed.Items[0].Id);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(item => item.CultureName == language));
        var day = DateOnly.FromDateTime(f.Time.Now.DateTime); var before = await RowsAsync(f);
        var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, seed.Access);
        var captured = await store.GetHomeSnapshotAsync(day, 0, Ct);
        var expected = GoalHomeProjection.Project(captured, seed.Presenter, f.Translator, day);
        var actual = await GoalHomeProjection.ProjectAsync(captured, seed.Presenter, f.Translator, day, Ct);
        Assert.Equal(expected.Pinned, actual.Pinned); Assert.Equal(expected.Attention, actual.Attention);
        await store.ValidateHomeSnapshotAsync(captured, Ct);
        Assert.Equal(before, await RowsAsync(f));
    }

    /// <summary>An already cancelled Home calculation cannot produce a new presentation or write original rows.</summary>
    [Fact, Trait("AT", "AT-283")]
    public async Task Cancelled_worker_home_projection_returns_no_presentation_and_keeps_original_source()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); Enable(f, seed);
        var day = DateOnly.FromDateTime(f.Time.Now.DateTime); var before = await RowsAsync(f);
        var captured = await f.Store.GetHomeSnapshotAsync(day, 0, Ct);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GoalHomeProjection.ProjectAsync(captured, seed.Presenter, f.Translator, day, cancelled.Token));
        Assert.Equal(before, await RowsAsync(f));
    }

    /// <summary>The original goal evaluator actually starts on a worker, independently of its calling presentation thread.</summary>
    [Fact, Trait("AT", "AT-283")]
    public async Task Home_goal_financial_evaluation_runs_outside_its_original_calling_thread()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f);
        var day = DateOnly.FromDateTime(f.Time.Now.DateTime);
        var captured = await f.Store.GetHomeSnapshotAsync(day, 0, Ct);
        using var starts = new GoalTaskStarts();
        var dispatched = new TaskCompletionSource<Task<IReadOnlyList<GoalProgress>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callerId = 0;
        var caller = new Thread(() =>
        {
            try
            {
                callerId = Environment.CurrentManagedThreadId;
                dispatched.SetResult(seed.Presenter.EvaluateHomeAsync(captured, day, Ct));
            }
            catch (Exception error) { dispatched.SetException(error); }
        }) { IsBackground = true };
        caller.Start();
        var calculation = await dispatched.Task.WaitAsync(Ct);
        var actual = await calculation;
        Assert.True(caller.Join(TimeSpan.FromSeconds(5)));
        Assert.True(starts.Started.TryGetValue(calculation.Id, out var workerId));
        Assert.NotEqual(callerId, workerId);
        Assert.Equal(GoalProgressService.Evaluate(captured.Goals.Goals, captured.Goals.Allocations,
            captured.QuickResources.Accounts, captured.Entries, captured.Goals.ContributionPlans,
            day, captured.AssetEvents, captured.AssetTypes), actual);
    }

    /// <summary>Observes public task-start identities only; it captures no application values or stack/memory state.</summary>
    private sealed class GoalTaskStarts : EventListener
    {
        internal ConcurrentDictionary<int, int> Started { get; } = new();
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName != "TaskStarted" || eventData.PayloadNames is not { } names
                || eventData.Payload is not { } values)
                return;
            var index = names.IndexOf("TaskID");
            if (index >= 0 && values[index] is int id && Started is { } started)
                started.TryAdd(id, Environment.CurrentManagedThreadId);
        }
    }

    /// <summary>A row from the previous original list publication cannot open a new detail route after refresh.</summary>
    [Fact]
    public async Task Goal_list_refuses_the_retained_row_after_its_source_was_republished()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); var previous = vm.Active.Single(row => row.Id == s.Items[0].Id);
        s.Items[0].Name = "Changed original list name"; await s.Goals.SaveGoalAsync(s.Items[0], Ct);
        await vm.LoadAsync(); var current = vm.Active.Single(row => row.Id == previous.Id);
        Assert.NotSame(previous, current); Assert.NotEqual(previous.Name, current.Name);
        await vm.OpenCommand.ExecuteAsync(previous); Assert.Empty(f.Platform.Routes);
        await vm.OpenCommand.ExecuteAsync(current); Assert.Equal(current.Id, Assert.Single(f.Platform.Routes).Parameters!["id"]);
    }

    /// <summary>Native collection callbacks cannot navigate while the original complete list is still being published.</summary>
    [Fact]
    public async Task Goal_list_keeps_navigation_disabled_during_partial_native_collection_publication()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); var current = vm.Active[0]; var observed = new List<bool>();
        vm.Active.CollectionChanged += (_, _) => observed.Add(vm.OpenCommand.CanExecute(current));
        await vm.LoadAsync(); Assert.NotEmpty(observed); Assert.All(observed, allowed => Assert.False(allowed));
        Assert.True(vm.OpenCommand.CanExecute(vm.Active[0]));
    }

    /// <summary>A failed source/rights read cannot leave the old Add/detail actions enabled over previous data.</summary>
    [Fact]
    public async Task Goal_list_refuses_old_actions_after_its_original_read_failed()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); var previous = vm.Active[0]; s.Access.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.False(vm.AddCommand.CanExecute(null)); Assert.False(vm.OpenCommand.CanExecute(previous));
        Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Old native commands remain retired even when the same page later publishes another valid source.</summary>
    [Fact]
    public async Task Goal_list_old_commands_stay_retired_after_another_appearance()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); var oldAdd = vm.AddCommand; var oldOpen = vm.OpenCommand; var row = vm.Active[0];
        vm.Retire(); await vm.LoadAsync();
        Assert.NotSame(oldAdd, vm.AddCommand); Assert.NotSame(oldOpen, vm.OpenCommand);
        await oldAdd.ExecuteAsync(null); await oldOpen.ExecuteAsync(row); Assert.Empty(f.Platform.Routes);
        await vm.AddCommand.ExecuteAsync(null); Assert.Equal(AppRoutes.GoalEditorRoute, Assert.Single(f.Platform.Routes).Route);
    }

    /// <summary>Departure from an actual native reset retires the unfinished packet instead of enabling partial rows.</summary>
    [Fact]
    public async Task Goal_list_departure_during_publication_keeps_the_new_lifetime_covered()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        System.Collections.Specialized.NotifyCollectionChangedEventHandler leave = (_, _) => vm.Retire();
        vm.Active.CollectionChanged += leave;
        await vm.LoadAsync(); Assert.False(vm.Loading.IsReady); Assert.False(vm.AddCommand.CanExecute(null));
        vm.Active.CollectionChanged -= leave; await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(5, vm.Active.Count); Assert.Single(vm.Finished); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>A rights failure triggered by a real native reset cannot uncover the partially published packet.</summary>
    [Fact]
    public async Task Goal_list_reentrant_rights_failure_stays_covered_until_explicit_retry()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        var before = await RowsAsync(f);
        System.Collections.Specialized.NotifyCollectionChangedEventHandler fail = (_, _) => s.Access.Fail = true;
        vm.Active.CollectionChanged += fail;
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.False(vm.AddCommand.CanExecute(null));
        vm.Active.CollectionChanged -= fail; s.Access.Fail = false; await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(before, await RowsAsync(f));
    }

    /// <summary>Retry only reads the original source and restores complete actions without another definition/payment.</summary>
    [Fact]
    public async Task Goal_list_explicit_retry_reads_the_same_original_data_after_failure()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        var before = await RowsAsync(f); s.Access.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync); s.Access.Fail = false;
        await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.False(vm.Loading.HasFailed);
        Assert.Equal(5, vm.Active.Count); Assert.Equal(before, await RowsAsync(f));
    }

    /// <summary>Actual list data retains one original file and refuses another factory before publication or navigation.</summary>
    [Fact]
    public async Task Goal_list_snapshot_refuses_another_actual_profile_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var original = await s.Goals.ReadListSnapshotAsync(Ct); var before = await RowsAsync(f); var otherBefore = await RowsAsync(other);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Services.GetRequiredService<GoalStore>().ValidateListSnapshotAsync(original, Ct));
        Assert.Equal(before, await RowsAsync(f)); Assert.Equal(otherBefore, await RowsAsync(other));
    }

    /// <summary>Detached account units cannot be rewritten in a captured original list packet and then blessed.</summary>
    [Fact]
    public async Task Goal_list_snapshot_refuses_mutated_original_account_references()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s); var before = await RowsAsync(f);
        var original = await s.Goals.ReadListSnapshotAsync(Ct); original.Accounts[0].CurrencyCode = "USD";
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Goals.ValidateListSnapshotAsync(original, Ct));
        Assert.Equal(before, await RowsAsync(f));
    }

    /// <summary>An empty goal list is an actual complete read and never initializes unrelated preferences.</summary>
    [Fact]
    public async Task Goal_list_empty_read_does_not_create_settings_or_financial_records()
    {
        using var f = new FlowFixture(); var goals = f.Services.GetRequiredService<GoalStore>();
        var original = await goals.ReadListSnapshotAsync(Ct); await goals.ValidateListSnapshotAsync(original, Ct);
        Assert.Empty(original.Accounts); Assert.Empty(original.Entries); Assert.Empty(original.Goals.Goals);
        var dates = new DateFormatter(f.Localization);
        var presenter = new GoalPresenter(f.Translator, dates, f.Localization, f.Services.GetRequiredService<HoldingStore>(),
            new HoldingText(f.Translator, f.Localization, dates));
        var vm = new GoalsViewModel(f.Store, goals, presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.True(vm.IsEmpty); Assert.True(vm.AddCommand.CanExecute(null));
        Assert.Empty(vm.Active); Assert.Empty(vm.Finished); Assert.Empty(vm.Earmarks);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.False(await db.Settings.AnyAsync(Ct));
    }

    /// <summary>Equal values do not revive a row object from an earlier native publication.</summary>
    [Fact]
    public async Task Goal_list_refuses_equal_old_rows_after_a_read_only_refresh()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s);
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); var previous = vm.Active[0]; await vm.LoadAsync(); var current = vm.Active[0];
        Assert.Equal(previous, current); Assert.NotSame(previous, current);
        await vm.OpenCommand.ExecuteAsync(previous); Assert.Empty(f.Platform.Routes);
        await vm.OpenCommand.ExecuteAsync(current); Assert.Single(f.Platform.Routes);
    }

    /// <summary>Explicit retry in the same page cannot silently adopt another selected profile's list.</summary>
    [Fact]
    public async Task Goal_list_retains_its_actual_file_until_the_page_lifetime_is_retired()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var first = await SeedAsync(f); var second = await SeedAsync(other);
        var factory = new GoalListFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var goals = new GoalStore(factory, new AccessSource());
        var vm = new GoalsViewModel(f.Store, goals, first.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync); Assert.False(vm.Loading.IsReady);
        vm.Retire(); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady);
        Assert.Contains(vm.Active, row => row.Id == second.Items[0].Id); Assert.DoesNotContain(vm.Active, row => row.Id == first.Items[0].Id);
    }

    /// <summary>A selected-file change after context creation cannot mix the first reader with later profile data.</summary>
    [Fact]
    public async Task Goal_list_switch_after_original_context_creation_refuses_publication_and_same_frame_retry()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var first = await SeedAsync(f); var second = await SeedAsync(other);
        var factory = new GoalListFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        factory.AfterCreate = () => factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var goals = new GoalStore(factory, new AccessSource());
        var vm = new GoalsViewModel(f.Store, goals, first.Presenter, f.Translator, f.Time, f.Platform);
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.Empty(vm.Active); Assert.False(vm.Loading.IsReady);
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        vm.Retire(); await vm.LoadAsync(); Assert.Contains(vm.Active, row => row.Id == second.Items[0].Id);
    }

    /// <summary>The actual original reader and validator each open their real SQLite connection away from the calling thread.</summary>
    [Fact]
    public async Task Goal_list_reader_and_validation_open_their_keyed_connections_on_workers()
    {
        using var f = new FlowFixture(); await SeedAsync(f);
        var factory = new GoalListFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var goals = new GoalStore(factory, new AccessSource());
        var read = await GoalListFromThreadAsync(() => goals.ReadListSnapshotAsync(Ct));
        var validated = await GoalListFromThreadAsync(async () => { await goals.ValidateListSnapshotAsync(read.Value, Ct); return true; });
        var opens = factory.Opened.ToArray(); Assert.Equal(2, opens.Length);
        Assert.NotEqual(read.Caller, opens[0]); Assert.NotEqual(validated.Caller, opens[1]);
    }

    private static async Task<(T Value, int Caller)> GoalListFromThreadAsync<T>(Func<Task<T>> action)
    {
        var started = new TaskCompletionSource<(Task<T> Work, int Caller)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var caller = new Thread(() =>
        {
            try { started.SetResult((action(), Environment.CurrentManagedThreadId)); }
            catch (Exception error) { started.SetException(error); }
        }) { IsBackground = true };
        caller.Start(); var work = await started.Task.WaitAsync(Ct); var value = await work.Work;
        Assert.True(caller.Join(TimeSpan.FromSeconds(5))); return (value, work.Caller);
    }

    /// <summary>Delegates every query to actual SQLite; only selected-factory timing and public connection-open thread IDs are observed.</summary>
    private sealed class GoalListFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal Action? AfterCreate;
        internal ConcurrentQueue<int> Opened { get; } = new();
        public ZananceDbContext CreateDbContext() => Track(Current.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Track(await Current.CreateDbContextAsync(cancellationToken));
        private ZananceDbContext Track(ZananceDbContext context)
        {
            context.Database.GetDbConnection().StateChange += (_, change) =>
            { if (change.CurrentState == System.Data.ConnectionState.Open) Opened.Enqueue(Environment.CurrentManagedThreadId); };
            var next = AfterCreate; AfterCreate = null; next?.Invoke(); return context;
        }
    }

    private static async Task OpenAsync(Seed s, int index)
    {
        s.Detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = s.Items[index].Id }); await s.Detail.LoadAsync();
        Assert.False(s.Detail.NotFound); Assert.True(s.Detail.CanInteract);
    }

    [Theory]
    [InlineData("en"), InlineData("fa"), InlineData("de")]
    public async Task Complete_list_and_original_Home_pins_keep_financial_packets_when_new_work_changes(string language)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var vm = new GoalsViewModel(f.Store, s.Goals, s.Presenter, f.Translator, f.Time, f.Platform);
        await vm.LoadAsync(); var original = vm.Active.ToDictionary(row => row.Id); var earmarks = vm.Earmarks.ToArray();
        Enable(f, s); await ChooseAsync(s, s.Items[0].Id); var before = await RowsAsync(f);
        await vm.LoadAsync(); Assert.Equal(5, vm.Active.Count); Assert.Single(vm.Finished);
        Assert.Equal(earmarks, vm.Earmarks);
        foreach (var row in vm.Active)
        {
            var prior = original[row.Id];
            Assert.Equal((prior.FundedText, prior.Progress, prior.RemainingText, prior.PlanText, prior.WarningText, prior.Tone, prior.StateText),
                (row.FundedText, row.Progress, row.RemainingText, row.PlanText, row.WarningText, row.Tone, row.StateText));
            Assert.Null(row.SuggestionText);
            Assert.Equal(row.Id == s.Items[0].Id ? null : f.Translator["ActiveItems_ReadOnly"], row.WorkText);
            if (row.WorkText is { } work) Assert.Contains(work, row.Description);
        }
        var accounts = await f.Store.GetAccountsAsync(cancellationToken: Ct); var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        var home = await GoalHomeProjection.ReadAsync(s.Goals, s.Presenter, f.Translator, accounts, entries, DateOnly.FromDateTime(f.Time.Now.DateTime));
        Assert.Equal(s.Items.Take(2).Select(goal => goal.Id), home.Pinned.Select(row => row.Id));
        Assert.Equal(f.Translator["ActiveItems_ReadOnly"], home.Pinned[1].WorkText); Assert.Null(home.Attention);
        Assert.Equal(before, await RowsAsync(f));
        await vm.OpenCommand.ExecuteAsync(vm.Active[0]);
        Assert.Equal(vm.Active[0].Id, f.Platform.Routes.Single().Parameters!["id"]);
    }

    [Theory]
    [InlineData(GoalState.Completed), InlineData(GoalState.Archived), InlineData(GoalState.Paused)]
    public async Task Retained_release_on_original_archived_account_is_available_without_new_paid_work(GoalState state)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        s.Items[3].State = state; await s.Goals.SaveGoalAsync(s.Items[3], Ct);
        Enable(f, s); await ChooseAsync(s, s.Items[0].Id); await OpenAsync(s, 3); var vm = s.Detail;
        Assert.True(vm.CanSetAside); Assert.False(vm.CanContribute);
        Assert.Equal(1, vm.DirectionIndex); Assert.Equal(s.Retained.Id, vm.SelectedAccount!.Id);
        vm.SelectedAccount = vm.Accounts.Single(account => account.Id == s.Retained.Id);
        Assert.False(vm.SelectedAccount.CanFund); vm.AmountText = "1.23"; vm.DirectionIndex = 0;
        Assert.False(vm.CanAllocate); var before = await RowsAsync(f); await vm.AllocateCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.Equal("1.23", vm.AmountText);
        vm.DirectionIndex = 1; Assert.True(vm.CanAllocate); await vm.AllocateCommand.ExecuteAsync(null);
        var allocations = await s.Goals.GetAllocationsAsync(s.Items[3].Id, Ct);
        Assert.Equal(2277, allocations.Sum(allocation => allocation.Amount)); Assert.Single(allocations, allocation => allocation.Amount == -123);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(string.Empty, vm.AmountText);
        Assert.Empty(f.Platform.Failures); Assert.Equal(state, (await s.Goals.GetGoalAsync(s.Items[3].Id, Ct))!.State);
    }

    [Fact]
    public async Task Excess_release_keeps_direction_account_and_exact_amount_draft_without_writing()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s, 3); var vm = s.Detail;
        vm.SelectedAccount = vm.Accounts.Single(account => account.Id == s.Retained.Id); vm.DirectionIndex = 1; vm.AmountText = "25.00";
        var before = await RowsAsync(f); await vm.AllocateCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.NotNull(vm.AmountError);
        Assert.Equal("25.00", vm.AmountText); Assert.Equal(1, vm.DirectionIndex); Assert.Equal(s.Retained.Id, vm.SelectedAccount.Id);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task One_busy_guard_spans_native_confirmation_and_preserves_unsaved_input(bool confirm)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s, 1); var vm = s.Detail;
        vm.AmountText = "7.25"; vm.DirectionIndex = 1; var before = await RowsAsync(f);
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task;
        var command = vm.ToggleCompleteCommand.ExecuteAsync(null);
        Assert.True(vm.IsBusy); Assert.False(vm.CanInteract); Assert.False(vm.CanAllocate); Assert.False(vm.CanTogglePause);
        await vm.TogglePauseCommand.ExecuteAsync(null); await vm.AllocateCommand.ExecuteAsync(null); await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.Single(f.Platform.ConfirmationDialogs);
        answer.SetResult(confirm); await command;
        Assert.False(vm.IsBusy); Assert.Equal("7.25", vm.AmountText); Assert.Equal(1, vm.DirectionIndex);
        Assert.Equal(confirm ? GoalState.Completed : GoalState.Active, (await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!.State);
        Assert.Empty(f.Platform.Failures); if (!confirm) Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("choice"), InlineData("rights"), InlineData("goal"), InlineData("note"), InlineData("pin"), InlineData("account"), InlineData("price-currency")]
    public async Task Intervening_context_change_rejects_explicit_completion_and_keeps_original_local_draft(string changed)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s, ProductPlan.Plus); await OpenAsync(s, 1);
        var vm = s.Detail; vm.AmountText = "7.25"; vm.DirectionIndex = 1;
        f.Platform.PendingConfirmation = async () =>
        {
            if (changed == "choice") await ChooseAsync(s);
            else if (changed == "rights") Enable(f, s);
            else if (changed == "goal") { var goal = (await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!; goal.TargetAmount++; await s.Goals.SaveGoalAsync(goal, Ct); }
            else if (changed is "note" or "pin")
            {
                var goal = (await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!; var stamp = goal.UpdatedAt;
                if (changed == "note") goal.Note = "Concurrent original retained note"; else goal.HomePin = null;
                await s.Goals.SaveGoalAsync(goal, Ct);
                Assert.Equal(stamp, (await s.Goals.GetGoalAsync(goal.Id, Ct))!.UpdatedAt);
            }
            else if (changed == "account")
            {
                s.Account.IsArchived = true; await f.Store.SaveAccountAsync(s.Account, Ct);
                Assert.True((await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single(account => account.Id == s.Account.Id).IsArchived);
            }
            else { s.Type.PriceCurrencyCode = "USD"; Assert.True(await f.Services.GetRequiredService<HoldingStore>().SaveTypeAsync(s.Type, Ct)); }
            return true;
        };
        await vm.ToggleCompleteCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.Failures); Assert.Null(vm.SaveError); Assert.Equal("7.25", vm.AmountText); Assert.Equal(1, vm.DirectionIndex);
        Assert.True(vm.IsActive); Assert.Equal(f.Translator["Goal_Complete"], vm.CompleteText);
        Assert.Equal(GoalState.Active, (await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!.State);
        if (changed == "note") Assert.Equal("Concurrent original retained note", (await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!.Note);
        if (changed == "pin") Assert.Null((await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!.HomePin);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Failed_reload_disables_writes_until_success_and_retains_explicit_money_input()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s, 1); var vm = s.Detail;
        vm.AmountText = "7.25"; vm.DirectionIndex = 1; var before = await RowsAsync(f); s.Access.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.False(vm.CanInteract); await vm.AllocateCommand.ExecuteAsync(null); Assert.Equal(before, await RowsAsync(f));
        Assert.Equal("7.25", vm.AmountText); s.Access.Fail = false; await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.CanInteract);
        Assert.Equal("7.25", vm.AmountText); Assert.Equal(1, vm.DirectionIndex);
    }

    [Fact]
    public async Task Committed_release_followed_by_reload_failure_reports_saved_without_repeating_the_write()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s, 1); var vm = s.Detail;
        vm.AmountText = "1.23"; vm.DirectionIndex = 1; s.Goals.Changed += (_, _) => s.Access.Fail = true;
        await vm.AllocateCommand.ExecuteAsync(null);
        Assert.Equal(f.Translator["Occurrence_SavedRefreshFailed"], vm.SaveError); Assert.False(vm.CanInteract);
        Assert.Empty(f.Platform.Failures); Assert.Equal(string.Empty, vm.AmountText);
        var before = await RowsAsync(f); await vm.AllocateCommand.ExecuteAsync(null); Assert.Equal(before, await RowsAsync(f));
        s.Access.Fail = false; await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.CanInteract); Assert.Null(vm.SaveError);
        Assert.Single(await s.Goals.GetAllocationsAsync(s.Items[1].Id, Ct), item => item.Amount == -123);
    }

    [Fact]
    public async Task Retained_price_correction_preserves_saved_rule_and_never_changes_valuation_or_ledger()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); Enable(f, s); await ChooseAsync(s, s.Items[0].Id); await OpenAsync(s, 5);
        var vm = s.Detail; Assert.True(vm.CanUseAssumedPrice); Assert.True(vm.CanEditAssumedPrice);
        var original = (await s.Goals.GetContributionPlansAsync(Ct)).Single(plan => plan.GoalId == s.Items[5].Id);
        vm.AssumedPriceText = "12.34"; await vm.SaveAssumedPriceCommand.ExecuteAsync(null);
        var saved = (await s.Goals.GetContributionPlansAsync(Ct)).Single(plan => plan.GoalId == s.Items[5].Id);
        Assert.Equal(original.Id, saved.Id); Assert.Equal(1234000, saved.AssumedPricePerUnitMilli);
        Assert.Equal(original.Amount, saved.Amount); Assert.Equal(original.Rule.Start, saved.Rule.Start); Assert.Equal(original.Rule.Frequency, saved.Rule.Frequency);
        Assert.Empty(await f.Services.GetRequiredService<HoldingStore>().GetValuationsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Readonly_quantity_without_recorded_price_cannot_start_a_new_price()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        var plan = (await s.Goals.GetContributionPlansAsync(Ct)).Single(plan => plan.GoalId == s.Items[5].Id);
        plan.AssumedPricePerUnitMilli = null; await s.Goals.SaveContributionPlanAsync(s.Items[5].Id, plan, Ct);
        Enable(f, s); await ChooseAsync(s, s.Items[0].Id); await OpenAsync(s, 5); var vm = s.Detail;
        Assert.True(vm.CanUseAssumedPrice); Assert.False(vm.CanEditAssumedPrice); Assert.False(vm.CanSaveAssumedPrice);
        vm.AssumedPriceText = "12.34"; var before = await RowsAsync(f); await vm.SaveAssumedPriceCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); Assert.Equal("12.34", vm.AssumedPriceText);
    }

    [Fact]
    public async Task Formatting_reload_keeps_dirty_price_and_amount_in_their_original_parse_culture()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); f.Localization.SetFormattingCulture("de-DE"); await OpenAsync(s, 5);
        s.Detail.AssumedPriceText = "1,23"; f.Localization.SetFormattingCulture("en-US"); await s.Detail.LoadAsync();
        Assert.Equal("1,23", s.Detail.AssumedPriceText); await s.Detail.SaveAssumedPriceCommand.ExecuteAsync(null);
        Assert.Equal(123000, (await s.Goals.GetContributionPlansAsync(Ct)).Single(plan => plan.GoalId == s.Items[5].Id).AssumedPricePerUnitMilli);
        await OpenAsync(s, 1); f.Localization.SetFormattingCulture("de-DE"); s.Detail.AmountText = "1,23"; s.Detail.DirectionIndex = 1;
        f.Localization.SetFormattingCulture("en-US"); await s.Detail.LoadAsync(); Assert.Equal("1,23", s.Detail.AmountText);
        await s.Detail.AllocateCommand.ExecuteAsync(null); Assert.Single(await s.Goals.GetAllocationsAsync(s.Items[1].Id, Ct), item => item.Amount == -123);
        Assert.Empty(f.Platform.Failures);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Changed_currency_preserves_original_draft_but_requires_explicit_reentry_before_save(bool price)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await OpenAsync(s, price ? 5 : 1); var vm = s.Detail;
        if (price) { vm.AssumedPriceText = "1.23"; s.Type.PriceCurrencyCode = "USD"; Assert.True(await f.Services.GetRequiredService<HoldingStore>().SaveTypeAsync(s.Type, Ct)); }
        else { vm.AmountText = "1.23"; vm.DirectionIndex = 1; var goal = (await s.Goals.GetGoalAsync(s.Items[1].Id, Ct))!; goal.CurrencyCode = "USD"; await s.Goals.SaveGoalAsync(goal, Ct); }
        await vm.LoadAsync(); Assert.Equal("1.23", price ? vm.AssumedPriceText : vm.AmountText);
        Assert.False(price ? vm.CanSaveAssumedPrice : vm.CanAllocate); Assert.Contains("USD", (price ? vm.PriceError : vm.AmountError)!);
        var before = await RowsAsync(f); if (price) await vm.SaveAssumedPriceCommand.ExecuteAsync(null); else await vm.AllocateCommand.ExecuteAsync(null);
        Assert.Equal(before, await RowsAsync(f)); if (price) vm.AssumedPriceText = "2.34"; else vm.AmountText = "2.34";
        Assert.False(price ? vm.CanSaveAssumedPrice : vm.CanAllocate);
        // Currency changes require complete reentry; a partial edit cannot renew the old raw amount's meaning.
        if (price) { vm.AssumedPriceText = ""; vm.AssumedPriceText = "2.34"; }
        else { vm.AmountText = ""; vm.AmountText = "2.34"; }
        Assert.True(price ? vm.CanSaveAssumedPrice : vm.CanAllocate);
    }

    [Theory]
    [InlineData("save"), InlineData("allocation"), InlineData("delete-allocation"), InlineData("plan"), InlineData("delete")]
    public async Task Every_detail_writer_rejects_a_review_from_another_actual_file_before_mutation(string action)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var review = await s.Goals.GetWorkSnapshotAsync(Ct);
        using var other = new FlowFixture(); var restored = await SeedAsync(other); var before = await RowsAsync(other);
        Task Write() => action switch
        {
            "save" => restored.Goals.SaveGoalAsync(s.Items[1], Ct, review),
            "allocation" => restored.Goals.AddAllocationAsync(new() { GoalId = restored.Items[1].Id, AccountId = restored.Account.Id, Amount = -1, Date = new(2026, 10, 9) }, Ct, review),
            "delete-allocation" => restored.Goals.DeleteAllocationAsync((review.Allocations[0]).Id, Ct, review),
            "plan" => restored.Goals.SaveContributionPlanAsync(restored.Items[5].Id, review.ContributionPlans[0], Ct, review),
            _ => restored.Goals.DeleteGoalAsync(restored.Items[1].Id, Ct, review),
        };
        await Assert.ThrowsAsync<InvalidOperationException>(Write); Assert.Equal(before, await RowsAsync(other));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Paused_basic_goal_resumes_only_when_it_is_the_actual_chosen_identity(bool selected)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        s.Items[4].Type = GoalType.AccountBalance; s.Items[4].AccountId = s.Retained.Id;
        await s.Goals.SaveGoalAsync(s.Items[4], Ct); Enable(f, s); await ChooseAsync(s, s.Items[selected ? 4 : 0].Id);
        await OpenAsync(s, 4); var vm = s.Detail; Assert.Equal(selected, vm.CanTogglePause);
        Assert.Equal(f.Translator["Goal_Resume"], vm.PauseText); var before = await RowsAsync(f);
        await vm.TogglePauseCommand.ExecuteAsync(null);
        Assert.Equal(selected ? GoalState.Active : GoalState.Paused, (await s.Goals.GetGoalAsync(s.Items[4].Id, Ct))!.State);
        Assert.Empty(f.Platform.Failures); if (!selected) Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("free-slot"), InlineData("full"), InlineData("paid-rule"), InlineData("reminder"), InlineData("advanced-type")]
    public async Task Historical_reopening_reflects_free_capacity_and_the_complete_saved_contribution(string condition)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        s.Items[3].Type = condition == "advanced-type" ? GoalType.Earmark : GoalType.AccountBalance;
        s.Items[3].AccountId = condition == "advanced-type" ? null : s.Retained.Id;
        await s.Goals.SaveGoalAsync(s.Items[3], Ct);
        var plan = (await s.Goals.GetContributionPlansAsync(Ct)).Single(plan => plan.GoalId == s.Items[3].Id);
        plan.Amount = condition == "paid-rule" ? 1000 : null; plan.ReminderEnabled = condition == "reminder";
        await s.Goals.SaveContributionPlanAsync(s.Items[3].Id, plan, Ct);
        Enable(f, s); await ChooseAsync(s, condition == "full" ? [s.Items[0].Id] : []); await OpenAsync(s, 3);
        var allowed = condition == "free-slot"; Assert.Equal(allowed, s.Detail.CanToggleComplete); var before = await RowsAsync(f);
        await s.Detail.ToggleCompleteCommand.ExecuteAsync(null);
        Assert.Equal(allowed ? GoalState.Active : GoalState.Completed, (await s.Goals.GetGoalAsync(s.Items[3].Id, Ct))!.State);
        Assert.Empty(f.Platform.Failures); if (!allowed) Assert.Equal(before, await RowsAsync(f));
        else Assert.Contains(s.Items[3].Id, (await s.Choices.ReadAsync(QuotaKind.Goals, Ct)).SelectedIds);
    }

    [Fact]
    public async Task Original_overdue_Home_attention_remains_visible_when_the_goal_is_unavailable_for_new_work()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        foreach (var goal in s.Items) { goal.HomePin = null; goal.TargetDate = new(2026, 10, 8); await s.Goals.SaveGoalAsync(goal, Ct); }
        Enable(f, s); await ChooseAsync(s); var before = await RowsAsync(f);
        var view = await GoalHomeProjection.ReadAsync(s.Goals, s.Presenter, f.Translator, await f.Store.GetAccountsAsync(cancellationToken: Ct),
            await f.Store.GetEntriesAsync(cancellationToken: Ct), new(2026, 10, 9));
        Assert.Empty(view.Pinned); Assert.NotNull(view.Attention); Assert.Equal(before, await RowsAsync(f));
    }
}
