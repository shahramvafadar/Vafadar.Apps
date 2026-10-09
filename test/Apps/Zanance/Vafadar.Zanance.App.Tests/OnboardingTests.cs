using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Real onboarding commands and SQLite writes with explicit window-transition ports.</summary>
public sealed class OnboardingTests
{
    [Fact, Trait("AT", "AT-80")]
    public async Task Back_and_next_keep_the_account_draft_without_writing_before_finish()
    {
        using var f = new FlowFixture(); var vm = Create(f);
        vm.BackCommand.Execute(null); Assert.Equal(1, vm.Step);
        await vm.NextCommand.ExecuteAsync(null); vm.ReportCurrency = "USD";
        await vm.NextCommand.ExecuteAsync(null); vm.Account.Name = "Fictitious wallet"; vm.Account.OpeningText = "25";
        vm.BackCommand.Execute(null); Assert.Equal(2, vm.Step); await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal("Fictitious wallet", vm.Account.Name); Assert.Equal("25", vm.Account.OpeningText);
        Assert.Equal("USD", vm.Account.CurrencyCode); Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.False((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).OnboardingCompleted); Assert.Equal(0, f.Platform.Completed);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Restore_opens_without_an_extra_account_or_committing_draft_financial_preferences()
    {
        using var f = new FlowFixture(); var vm = Create(f); vm.ReportCurrency = "USD"; vm.ModeIndex = 1;
        await vm.RestoreBackupCommand.ExecuteAsync(null);
        Assert.Equal(1, f.Platform.Restored); Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken));
        var settings = await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken); Assert.False(settings.OnboardingCompleted);
        Assert.Equal("EUR", settings.ReportCurrencyCode); Assert.Equal(ExperienceMode.Simple, settings.Mode);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Finish_saves_one_account_and_the_selected_financial_preferences()
    {
        using var f = new FlowFixture(); var vm = Create(f); vm.ReportCurrency = "USD"; vm.ModeIndex = 1;
        vm.SelectedCalendar = vm.Calendars.First(c => c.Calendar == CalendarSystem.Hijri);
        await vm.NextCommand.ExecuteAsync(null); await vm.NextCommand.ExecuteAsync(null);
        vm.Account.Name = "Fictitious wallet"; vm.Account.OpeningText = "25.50";
        await vm.NextCommand.ExecuteAsync(null);
        var account = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken)); Assert.Equal(2550, account.OpeningBalance);
        var settings = await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken); Assert.True(settings.OnboardingCompleted);
        Assert.Equal(account.Id, settings.DefaultAccountId); Assert.Equal("USD", settings.DefaultCurrencyCode);
        Assert.Equal("USD", settings.ReportCurrencyCode); Assert.Equal(ExperienceMode.Advanced, settings.Mode);
        Assert.Equal(PeriodCalendar.Hijri, settings.BudgetCalendar); Assert.Equal(1, f.Platform.Completed);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken)); Assert.False(vm.IsBusy);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Invalid_account_fields_leave_the_draft_and_database_unchanged()
    {
        using var f = new FlowFixture(); var vm = Create(f);
        await vm.NextCommand.ExecuteAsync(null); await vm.NextCommand.ExecuteAsync(null);
        vm.Account.Name = ""; vm.Account.OpeningText = "not money"; await vm.NextCommand.ExecuteAsync(null);
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken)); Assert.False((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).OnboardingCompleted);
        Assert.Equal("not money", vm.Account.OpeningText); Assert.Equal(0, f.Platform.Completed); Assert.False(vm.IsBusy);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Retrying_after_an_account_write_failure_reuses_the_first_account_identity()
    {
        using var f = new FlowFixture(); var vm = Create(f);
        await vm.NextCommand.ExecuteAsync(null); await vm.NextCommand.ExecuteAsync(null); vm.Account.Name = "Fictitious wallet";
        var failed = false;
        f.Store.Changed += (_, _) =>
        {
            if (!failed && f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken).GetAwaiter().GetResult().Count > 0)
            { failed = true; throw new IOException("Fictitious post-commit receiver failure."); }
        };
        await vm.NextCommand.ExecuteAsync(null);
        var first = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken)); Assert.Single(f.Platform.Failures);
        Assert.False((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).OnboardingCompleted); Assert.Equal(0, f.Platform.Completed);
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal(first.Id, Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken)).Id);
        Assert.True((await f.Store.GetSettingsAsync(TestContext.Current.CancellationToken)).OnboardingCompleted); Assert.Equal(1, f.Platform.Completed);
    }

    [Fact, Trait("AT", "AT-80")]
    public void Language_and_theme_changes_preserve_an_open_account_and_independent_regional_choices()
    {
        using var f = new FlowFixture(); var vm = Create(f); vm.Account.Name = "Fictitious draft"; vm.Account.OpeningText = "123";
        vm.ModeIndex = 1; vm.ThemeIndex = (int)ThemeChoice.Dark;
        vm.Regional.SelectedFormat = vm.Regional.Formats.First(x => x.Name == "de-DE");
        vm.Regional.DigitIndex = (int)DigitStyle.Latin;
        vm.Regional.SelectedHolidayRegion = vm.Regional.HolidayRegions.First(x => x.Code == "DE");
        vm.SelectedLanguage = vm.Languages.First(x => x.CultureName == "fa");
        Assert.Equal("Fictitious draft", vm.Account.Name); Assert.Equal("123", vm.Account.OpeningText);
        Assert.Equal(1, vm.ModeIndex); Assert.Equal((int)ThemeChoice.Dark, vm.ThemeIndex);
        Assert.Equal("de-DE", f.Localization.FormattingCultureName); Assert.Equal("DE", f.Localization.CurrentRegion);
        Assert.Equal(DigitStyle.Latin, f.Localization.CurrentDigits); Assert.Equal(ThemeChoice.Dark, f.Theme.Choice);
        Assert.Contains("1.234,56", vm.Regional.Preview, StringComparison.Ordinal);
    }

    private static OnboardingViewModel Create(FlowFixture f) => new(f.Store, f.Localization, f.Translator, f.Time, f.Theme, f.Platform, f.Platform);
}
