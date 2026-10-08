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
using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Verifies profile preferences, notification privacy and cancellation using the real stores and coordinator.</summary>
[Trait("AT", "AT-78")]
public sealed class ReviewReminderDeliveryTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly RecordingScheduler _scheduler = new();
    private readonly ReminderService _reminders;
    private readonly Translator _translator = new();
    private readonly LocalizationService _localization;

    /// <summary>Creates a fictitious SQLite profile and records requests at the platform boundary.</summary>
    public ReviewReminderDeliveryTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("review.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
        var preferences = new InMemorySettingsStore();
        _translator.AddResources(new ResourceManager("Vafadar.Zanance.Data.Tests.Resources.AppResources", typeof(ReviewReminderDeliveryTests).Assembly));
        _localization = new LocalizationService(new LocalizationOptions(), preferences, _translator);
        _localization.Initialize();
        _localization.SetLanguage(AppLanguages.All.Single(l => l.CultureName == "en"));
        _localization.SetCalendar(CalendarSystem.Gregorian);
        _reminders = new ReminderService(_scheduler, _store, _services.GetRequiredService<PlanStore>(),
            _services.GetRequiredService<GoalStore>(), _services.GetRequiredService<HoldingStore>(), preferences, _translator,
            new DateFormatter(_localization), _localization, new FixedTime());
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
    public async Task Opting_in_schedules_generic_reviews_without_posting_or_marking_steps(string language)
    {
        _translator.SetCulture(CultureInfo.GetCultureInfo(language));
        await SaveAccountAsync();
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        Assert.False((await _store.GetSettingsAsync(Ct)).ReviewReminderEnabled);
        await _store.UpdateSettingsAsync(s => s.ReviewReminderEnabled = true, Ct);
        await _reminders.RefreshAsync();
        Assert.NotEmpty(_scheduler.Pending);
        Assert.All(_scheduler.Pending, n =>
        {
            Assert.Equal(_translator["App_Name"], n.Title);
            Assert.Equal(_translator["Reminder_ReviewGeneric"], n.Body);
            Assert.Equal("review", n.Link);
            Assert.False(n.CanSnooze);
            Assert.Equal(9, n.NotifyAt!.Value.Hour);
        });
        Assert.Equal(0, _scheduler.PermissionRequests);
        Assert.Null((await _store.GetSettingsAsync(Ct)).ReviewProgress);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Financial_month_and_display_calendar_are_used_independently_of_budget_defaults()
    {
        await SaveAccountAsync();
        await _store.UpdateSettingsAsync(s => { s.ReviewReminderEnabled = true; s.MonthStartDay = 25; s.BudgetCalendar = PeriodCalendar.Hijri; }, Ct);
        await _reminders.RefreshAsync();
        Assert.Equal(new DateTime(2026, 2, 25, 9, 0, 0), _scheduler.Pending[0].NotifyAt);
        _localization.SetCalendar(CalendarSystem.Persian);
        await _reminders.RefreshAsync();
        var at = DateOnly.FromDateTime(_scheduler.Pending[0].NotifyAt!.Value);
        Assert.Equal(25, PeriodMath.DayOf(at, PeriodCalendar.Persian));
        Assert.Equal(9, _scheduler.Pending[0].NotifyAt!.Value.Hour);
    }

    [Fact]
    public async Task Completing_a_period_cancels_its_pending_reminder_and_off_cancels_all_reviews()
    {
        await SaveAccountAsync();
        await _store.UpdateSettingsAsync(s => s.ReviewReminderEnabled = true, Ct);
        await _reminders.RefreshAsync();
        var first = _scheduler.Pending[0];
        await _store.UpdateSettingsAsync(s => s.ReviewProgress = "2026-01", Ct);
        await _reminders.RefreshAsync();
        Assert.DoesNotContain(_scheduler.Pending, n => n.Id == first.Id);
        Assert.NotEmpty(_scheduler.Pending);
        await _store.UpdateSettingsAsync(s => s.ReviewReminderEnabled = false, Ct);
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        Assert.Equal("2026-01", (await _store.GetSettingsAsync(Ct)).ReviewProgress);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Denial_and_empty_profiles_do_not_prompt_on_refresh_and_details_need_explicit_opt_in()
    {
        await _store.UpdateSettingsAsync(s => s.ReviewReminderEnabled = true, Ct);
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        await SaveAccountAsync();
        _scheduler.Enabled = false;
        await _reminders.RefreshAsync();
        Assert.Empty(_scheduler.Pending);
        Assert.True((await _store.GetSettingsAsync(Ct)).ReviewReminderEnabled);
        Assert.Equal(0, _scheduler.PermissionRequests);
        _scheduler.Enabled = true;
        await _store.UpdateSettingsAsync(s => s.NotificationsShowDetails = true, Ct);
        await _reminders.RefreshAsync();
        Assert.All(_scheduler.Pending, n => Assert.NotEqual(_translator["Reminder_ReviewGeneric"], n.Body));
        Assert.Contains("January", _scheduler.Pending[0].Body);
    }

    private Task SaveAccountAsync() => _store.SaveAccountAsync(new Account
    {
        Name = "Private account", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = new DateOnly(2025, 1, 1),
    }, Ct);

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);
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
