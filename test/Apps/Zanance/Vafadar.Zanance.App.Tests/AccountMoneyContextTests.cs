using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-434: original account money must survive changes to the displayed number format and unit.</summary>
[Trait("AT", "AT-434")]
[Collection("Entry validation units")]
public sealed class AccountMoneyContextTests
{
    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Current.CreateDbContextAsync(cancellationToken);
    }
    private sealed class InactiveAccess : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Capture(string databasePath) => CommercialWriteAccess.Inactive;
    }
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static AccountEditorViewModel Editor(FlowFixture f) => new(f.Store, f.Services.GetRequiredService<PlanStore>(),
        f.Translator, f.Localization, f.Time, f.Platform);

    /// <summary>The actual account form must not reinterpret a grouping separator after a display change.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_opening_and_installment_keep_their_input_format(bool debt)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
        var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Form.Name = "Original account money";
        if (debt) vm.Form.TypeIndex = (int)AccountType.Loan;
        vm.Form.OpeningText = "1,234"; if (debt) vm.Form.InstallmentText = "1,234";
        f.Localization.SetFormattingCulture("de-DE"); await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal(debt ? -123400 : 123400, saved.OpeningBalance);
        Assert.Equal(debt ? 123400L : (long?)null, saved.Installment);
    }

    /// <summary>Changing a display unit requires complete reentry, not silent multiplication at Save.</summary>
    [Fact]
    public async Task Changed_unit_cannot_reinterpret_existing_opening_amount()
    {
        var originalUnits = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Form.Name = "Original euro opening"; vm.Form.OpeningText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            await vm.SaveCommand.ExecuteAsync(null);
            Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
            Assert.Equal("12.34", vm.Form.OpeningText); Assert.NotNull(vm.Form.AmountError);
        }
        finally { DisplayUnits.Set(originalUnits); }
    }

    /// <summary>A currency change cannot relabel an already entered opening amount.</summary>
    [Fact]
    public async Task Changed_currency_cannot_relabel_existing_opening_amount()
    {
        using var f = new FlowFixture(); var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Form.Name = "Original euro opening"; vm.Form.OpeningText = "12.34"; vm.Form.CurrencyCode = "USD";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal("12.34", vm.Form.OpeningText); Assert.NotNull(vm.Form.AmountError);
    }

    /// <summary>Partial edits retain the old meaning; only clearing allows a new currency to be entered.</summary>
    [Fact]
    public async Task Complete_reentry_is_required_after_currency_change()
    {
        using var f = new FlowFixture(); var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Form.Name = "Explicit new currency"; vm.Form.OpeningText = "12.34"; vm.Form.CurrencyCode = "USD";
        vm.Form.OpeningText += "0";
        Assert.Equal("EUR", vm.Form.OpeningUnitName); Assert.False(vm.Form.MoneyContextsReady);
        vm.Form.OpeningText = string.Empty; vm.Form.OpeningText = "56.78";
        Assert.Equal("USD", vm.Form.OpeningUnitName); Assert.True(vm.Form.MoneyContextsReady);
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal("USD", saved.CurrencyCode); Assert.Equal(5678, saved.OpeningBalance);
    }

    /// <summary>Unknown principal ignores its hidden amount but cannot hide a stale applicable installment.</summary>
    [Fact]
    public async Task Unknown_principal_does_not_hide_installment_currency_review()
    {
        using var f = new FlowFixture(); var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Form.Name = "Unknown principal"; vm.Form.TypeIndex = (int)AccountType.Loan;
        vm.Form.OpeningText = "12.34"; vm.Form.InstallmentText = "10";
        vm.Form.CurrencyCode = "USD"; vm.Form.OpeningUnknown = true;
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Null(vm.Form.AmountError); Assert.NotNull(vm.Form.TermsError);
        vm.Form.InstallmentText = string.Empty; vm.Form.InstallmentText = "20"; await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.False(saved.OpeningBalanceKnown); Assert.Equal(0, saved.OpeningBalance); Assert.Equal(2000, saved.Installment);
    }

    /// <summary>Actual balance comparison uses the original grouping parser and keeps explicit negative direction.</summary>
    [Theory]
    [InlineData("1,234", 123400)]
    [InlineData("-1,234", -123400)]
    [InlineData("−1,234", -123400)]
    public async Task Observed_balance_retains_original_parser_and_sign(string raw, long expected)
    {
        using var f = new FlowFixture(); var day = new DateOnly(2026, 10, 1);
        var account = new Account { Name = "Original comparison", CurrencyCode = "EUR", OpeningDate = day };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        var session = new AccountReconciliationSession(f.Store);
        session.ObservedMoney.ChangeText(raw, "EUR", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        f.Localization.SetFormattingCulture("de-DE");
        var review = await session.CompareObservedAsync(account.Id, "EUR", day);
        Assert.Equal(expected, review!.Result.Difference);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>A unit change during native confirmation cannot turn the reviewed amount into accepted money.</summary>
    [Fact]
    public async Task Changed_observed_unit_during_confirmation_refuses_adjustment()
    {
        var originalUnits = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var day = new DateOnly(2026, 10, 1);
            var account = new Account { Name = "Original pending comparison", CurrencyCode = "EUR", OpeningDate = day };
            Assert.True(await f.Store.SaveAccountAsync(account, Ct));
            var session = new AccountReconciliationSession(f.Store);
            session.ObservedMoney.ChangeText("12.34", "EUR", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
            await session.CompareObservedAsync(account.Id, "EUR", day);
            Assert.Null(await session.AdjustAsync(() =>
            {
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); return Task.FromResult(true);
            }, () => "Original reason"));
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.False(session.Committed);
            Assert.Equal("12.34", session.ObservedMoney.Text);
        }
        finally { DisplayUnits.Set(originalUnits); }
    }

    /// <summary>The real writer rolls back if the draft edits and returns, changes unit, or departs during Save.</summary>
    [Theory]
    [InlineData("edit-return")]
    [InlineData("unit")]
    [InlineData("departure")]
    [InlineData("rate-edit-return")]
    public async Task Pending_account_writer_rechecks_original_monetary_and_native_owner(string change)
    {
        var originalUnits = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = Editor(f);
            await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Form.Name = "Original pending account"; vm.Form.OpeningText = "12.34";
            if (change == "rate-edit-return") { vm.Form.TypeIndex = (int)AccountType.Loan; vm.Form.RateText = "1.25"; }
            var reached = false;
            f.Time.BeforeRead = () =>
            {
                f.Time.BeforeRead = null; reached = true;
                if (change == "edit-return") { vm.Form.OpeningText = "99"; vm.Form.OpeningText = "12.34"; }
                else if (change == "rate-edit-return") { vm.Form.RateText = "2.5"; vm.Form.RateText = "1.25"; }
                else if (change == "unit") DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                else vm.SetPresentationActive(false);
            };
            await vm.SaveCommand.ExecuteAsync(null);
            Assert.True(reached); Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct));
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.NotNull(vm.SaveError);
            Assert.Equal("12.34", vm.Form.OpeningText);
        }
        finally { DisplayUnits.Set(originalUnits); }
    }

    /// <summary>Inactive commerce is not permission to use another file for the original account form.</summary>
    [Fact]
    public async Task Original_account_work_refuses_another_actual_file_before_account_creation()
    {
        using var original = new FlowFixture(); using var other = new FlowFixture();
        var work = await original.Store.ReadEditorWorkAsync(Ct);
        var proposal = new Account { Name = "Foreign account writer", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.SaveAccountAsync(proposal, Ct, originalWork: work));
        Assert.Empty(await original.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Empty(await other.Store.GetAccountsAsync(cancellationToken: Ct));
    }

    /// <summary>Loaded original debts keep their historical sign and stored installment through display changes.</summary>
    [Theory]
    [InlineData(AccountType.Loan, -123400)]
    [InlineData(AccountType.Loan, 123400)]
    [InlineData(AccountType.Lent, 123400)]
    [InlineData(AccountType.Lent, -123400)]
    public async Task Loaded_debt_keeps_original_signed_principal_and_installment(AccountType type, long principal)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
        var original = new Account { Name = "Original historical debt", Type = type, CurrencyCode = "EUR",
            OpeningDate = new(2020, 1, 1), OpeningBalance = principal, Installment = 1234, InterestRate = 1.25m };
        Assert.True(await f.Store.SaveAccountAsync(original, Ct));
        var vm = Editor(f); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
        var raw = (vm.Form.OpeningText, vm.Form.InstallmentText);
        f.Localization.SetFormattingCulture("de-DE"); vm.Form.RefreshTexts(); vm.Form.Name += " renamed";
        Assert.NotNull(vm.Form.OpeningFormatNote); Assert.NotNull(vm.Form.InstallmentFormatNote);
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal(principal, saved.OpeningBalance); Assert.Equal(1234, saved.Installment); Assert.Equal(1.25m, saved.InterestRate);
        Assert.Equal(type, saved.Type); Assert.Equal(raw, (vm.Form.OpeningText, vm.Form.InstallmentText));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Known account acceptance cannot send its later default preference into another file or departed page.</summary>
    [Theory]
    [InlineData("file")]
    [InlineData("departure")]
    public async Task Accepted_account_default_keeps_original_file_and_native_owner(string change)
    {
        using var first = new FlowFixture(); using var other = new FlowFixture();
        await first.Store.GetSettingsAsync(Ct); await other.Store.GetSettingsAsync(Ct);
        var otherBefore = await GoalChooserFlowTests.SnapshotAsync(other);
        var factory = new SwitchingFactory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var access = new InactiveAccess(); var store = new ZananceStore(factory, first.Time, access);
        var vm = new AccountEditorViewModel(store, new PlanStore(factory, store, access), first.Translator, first.Localization, first.Time, first.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Form.Name = "Original accepted account"; vm.Form.OpeningText = "12.34"; vm.IsDefault = true;
        Guid? defaultAtOriginalNotification = null;
        store.Changed += (_, _) =>
        {
            using var original = first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext();
            defaultAtOriginalNotification = original.Settings.AsNoTracking().Single().DefaultAccountId;
            if (change == "file") factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            else vm.SetPresentationActive(false);
        };
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await first.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Equal(1234, saved.OpeningBalance);
        Assert.Equal(saved.Id, defaultAtOriginalNotification);
        Assert.Equal(otherBefore, await GoalChooserFlowTests.SnapshotAsync(other));
        Assert.Equal(saved.Id, (await first.Store.GetSettingsAsync(Ct)).DefaultAccountId);
        Assert.Equal(change == "file" ? first.Translator["Account_SavedNeedsReview"] : null, vm.SaveError);
        Assert.False(vm.CanSave); Assert.Equal(first.Translator["Common_Close"], vm.CloseText); Assert.Empty(first.Platform.Routes);
        Assert.Empty(await first.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>A different stored currency is not permission to reinterpret an already entered observed balance.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("12.34")]
    public async Task Observed_comparison_refuses_a_currency_changed_before_its_original_read(string raw)
    {
        using var f = new FlowFixture(); var day = new DateOnly(2026, 10, 1);
        var account = new Account { Name = "Original observed currency", CurrencyCode = "EUR", OpeningDate = day };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        var session = new AccountReconciliationSession(f.Store);
        session.ObservedMoney.ChangeText(raw, "EUR", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        account.CurrencyCode = "USD"; Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.CompareObservedAsync(account.Id, "EUR", day));
        Assert.Null(session.Review); Assert.False(session.Committed); Assert.Equal(raw, session.ObservedMoney.Text);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal("USD", saved.CurrencyCode); Assert.Null(saved.LastReconciledOn);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
