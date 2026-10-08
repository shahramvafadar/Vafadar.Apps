using System.Globalization;
using System.Resources;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Testing;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Exercises goal notification privacy and cancellation through the real coordinator and SQLite stores.</summary>
[Trait("AT", "AT-77")]
public sealed class GoalReminderDeliveryTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly GoalStore _goals;
    private readonly RecordingScheduler _scheduler = new();
    private readonly ReminderService _reminders;
    private readonly Translator _translator = new();

    /// <summary>Creates only a fictitious database and records platform-bound notification requests.</summary>
    public GoalReminderDeliveryTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("reminders.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
        _goals = _services.GetRequiredService<GoalStore>();
        var preferences = new InMemorySettingsStore();
        _translator.AddResources(new ResourceManager("Vafadar.Zanance.Data.Tests.Resources.AppResources", typeof(GoalReminderDeliveryTests).Assembly));
        var localization = new LocalizationService(new LocalizationOptions(), preferences, _translator);
        localization.Initialize();
        localization.SetLanguage(AppLanguages.All.Single(l => l.CultureName == "en"));
        _reminders = new ReminderService(_scheduler, _store, _services.GetRequiredService<PlanStore>(),
            _goals, _services.GetRequiredService<HoldingStore>(), preferences, _translator,
            new DateFormatter(localization), localization, new FixedTime());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <inheritdoc />
    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    public async Task A_saved_reminder_is_generic_by_default_and_never_posts_money(string language)
    {
        _translator.SetCulture(CultureInfo.GetCultureInfo(language));
        var (goal, _) = await SaveGoalAsync();
        await _reminders.RefreshAsync();

        Assert.NotEmpty(_scheduler.Pending);
        Assert.All(_scheduler.Pending, n =>
        {
            Assert.Equal(_translator["App_Name"], n.Title);
            Assert.Equal(_translator["Reminder_GoalGeneric"], n.Body);
            Assert.DoesNotContain(goal.Name, n.Body);
            Assert.Equal($"goal|{goal.Id}", n.Link);
            Assert.False(n.CanSnooze);
            Assert.Equal(9, n.NotifyAt!.Value.Hour);
        });
        Assert.Equal(0, _scheduler.PermissionRequests);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(await _goals.GetAllocationsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Details_require_opt_in_and_same_time_goals_share_one_notification()
    {
        var (first, _) = await SaveGoalAsync("First private goal");
        var (second, _) = await SaveGoalAsync("Second private goal");
        await _reminders.RefreshAsync();
        Assert.All(_scheduler.Pending, n => Assert.Equal(_translator["Reminder_GoalGeneric"], n.Body));
        Assert.All(_scheduler.Pending, n => Assert.Equal("goals", n.Link));

        var settings = await _store.GetSettingsAsync(Ct);
        settings.NotificationsShowDetails = true;
        await _store.SaveSettingsAsync(settings, Ct);
        await _reminders.RefreshAsync();
        Assert.All(_scheduler.Pending, n =>
        {
            Assert.Equal(_translator.Format("Reminder_GoalSummary", 2), n.Title);
            Assert.Contains(first.Name, n.Body);
            Assert.Contains(second.Name, n.Body);
        });
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Pausing_reaching_archiving_and_disabling_cancel_pending_goal_reminders()
    {
        var (goal, account) = await SaveGoalAsync();
        await _reminders.RefreshAsync();
        var originalIds = _scheduler.Pending.Select(n => n.Id).ToArray();

        goal.State = GoalState.Paused;
        await _goals.SaveGoalAsync(goal, Ct);
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        goal.State = GoalState.Active;
        await _goals.SaveGoalAsync(goal, Ct);
        await _reminders.RefreshAsync();
        Assert.Equal(originalIds, _scheduler.Pending.Select(n => n.Id));

        account.OpeningBalance = goal.TargetAmount;
        await _store.SaveAccountAsync(account, Ct);
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        account.OpeningBalance = 0;
        await _store.SaveAccountAsync(account, Ct);
        await _reminders.RefreshAsync();
        Assert.Equal(originalIds, _scheduler.Pending.Select(n => n.Id));

        account.IsArchived = true;
        await _store.SaveAccountAsync(account, Ct);
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        account.IsArchived = false;
        await _store.SaveAccountAsync(account, Ct);
        var plan = Assert.Single(await _goals.GetContributionPlansAsync(Ct));
        plan.ReminderEnabled = false;
        await _goals.SaveContributionPlanAsync(goal.Id, plan, Ct);
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        Assert.False(Assert.Single(await _goals.GetContributionPlansAsync(Ct)).ReminderEnabled);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Denied_permission_keeps_the_saved_choice_without_prompting_during_refresh()
    {
        await SaveGoalAsync();
        _scheduler.Enabled = false;
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        Assert.Equal(0, _scheduler.PermissionRequests);
        Assert.True(Assert.Single(await _goals.GetContributionPlansAsync(Ct)).ReminderEnabled);
        Assert.False(await _reminders.EnsurePermissionAsync());
        Assert.Equal(1, _scheduler.PermissionRequests);
        _scheduler.Enabled = true;
        await _reminders.RefreshAsync();
        Assert.NotEmpty(_scheduler.Pending);
        Assert.Equal(1, _scheduler.PermissionRequests);
    }

    [Fact]
    public async Task Busy_plan_and_goal_queues_share_the_device_limit_and_keep_the_earliest_items()
    {
        var (goal, account) = await SaveGoalAsync();
        await _services.GetRequiredService<PlanStore>().SaveScheduleAsync(new Schedule
        {
            Name = "Daily bill", AccountId = account.Id, AmountMode = AmountMode.Unknown,
            ReminderEnabled = true, ReminderDaysBefore = 0, ReminderTime = new TimeOnly(10, 0),
            Rule = new RecurrenceRule { Frequency = Frequency.Daily, Start = new DateOnly(2026, 1, 31) },
        }, Ct);
        await _store.UpdateSettingsAsync(s => s.ReviewReminderEnabled = true, Ct);
        await _reminders.RefreshAsync();

        Assert.Equal(Vafadar.Zanance.Core.Reminders.ReminderPlanner.MaxPending, _scheduler.Pending.Count);
        Assert.Equal($"goal|{goal.Id}", _scheduler.Pending[0].Link);
        Assert.Equal(_scheduler.Pending.OrderBy(n => n.NotifyAt).ThenBy(n => n.Id), _scheduler.Pending);
        Assert.Contains(_scheduler.Pending, n => n.Link.StartsWith("occurrence|", StringComparison.Ordinal));
        Assert.Contains(_scheduler.Pending, n => n.Link == "review");
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    private async Task<(Goal Goal, Account Account)> SaveGoalAsync(string name = "Private savings goal")
    {
        var account = new Account { Name = "Private account", CurrencyCode = "EUR", Type = AccountType.Savings, OpeningDate = new DateOnly(2026, 1, 1) };
        await _store.SaveAccountAsync(account, Ct);
        var goal = new Goal { Name = name, CurrencyCode = "EUR", Type = GoalType.AccountBalance, AccountId = account.Id, TargetAmount = 100_00 };
        await _goals.SaveGoalAsync(goal, Ct);
        await _goals.SaveContributionPlanAsync(goal.Id, new ContributionPlan
        {
            ReminderEnabled = true,
            Amount = 25_00,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 1, 31) },
        }, Ct);
        return (goal, account);
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 30, 8, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class RecordingScheduler : IReminderScheduler
    {
        public event EventHandler<string>? Tapped { add { } remove { } }
        public event EventHandler<SnoozeRequest>? SnoozeRequested { add { } remove { } }
        public bool IsSupported => true;
        public bool Enabled { get; set; } = true;
        public int PermissionRequests { get; private set; }
        public IReadOnlyList<ReminderNotification> Pending { get; private set; } = [];
        public void SetSnoozeActions(string oneHour, string tomorrow) { }
        public Task<bool> AreEnabledAsync() => Task.FromResult(Enabled);
        public Task<bool> RequestPermissionAsync() { PermissionRequests++; return Task.FromResult(Enabled); }
        public Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders) { Pending = reminders; return Task.CompletedTask; }
        public Task ShowAsync(ReminderNotification notification) => Task.CompletedTask;
    }
}
