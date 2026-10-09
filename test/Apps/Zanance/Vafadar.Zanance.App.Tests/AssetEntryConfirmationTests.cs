using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The editor's actual valued-asset gate with real SQLite writes and translated native dialog requests.</summary>
public sealed class AssetEntryConfirmationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);

    [Theory, InlineData(EntryKind.Income), InlineData(EntryKind.Expense), Trait("AT", "AT-81")]
    public async Task Cancelling_a_new_asset_entry_keeps_the_draft_ledger_and_asset_value_unchanged(EntryKind kind)
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var draft = Entry(asset, kind, 1234);
        f.Platform.Confirmations.Enqueue(false);
        await Gate(f).RunAsync(kind, asset, async () => { draft.Note = "Committed"; Assert.True((await f.Store.SaveEntryAsync(draft, Ct)).Succeeded); });
        Assert.Null(draft.Note); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(800000, LedgerCalculator.Balance(asset, [], Day));
        Assert.Equal(800000, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalance);
        Assert.Empty(await f.Services.GetRequiredService<Vafadar.Zanance.Data.HoldingStore>().GetTypesAsync(cancellationToken: Ct));
        Assert.Single(f.Platform.ConfirmationDialogs);
    }

    [Theory, InlineData(EntryKind.Income), InlineData(EntryKind.Expense), Trait("AT", "AT-81")]
    public async Task Accepting_an_asset_entry_saves_its_original_identity_and_kind_once(EntryKind kind)
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var draft = Entry(asset, kind, 1234);
        f.Platform.Confirmations.Enqueue(true);
        await Gate(f).RunAsync(kind, asset, async () => Assert.True((await f.Store.SaveEntryAsync(draft, Ct)).Succeeded));
        var saved = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal((draft.Id, asset.Id, kind, 1234L), (saved.Id, saved.AccountId, saved.Kind, saved.Amount));
        Assert.Equal(800000 + (kind == EntryKind.Income ? 1234 : -1234), LedgerCalculator.Balance(asset, [saved], Day));
        Assert.Equal(800000, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalance);
        Assert.Single(f.Platform.ConfirmationDialogs);
    }

    [Theory, InlineData(EntryKind.Income), InlineData(EntryKind.Expense), Trait("AT", "AT-81")]
    public async Task Cancelling_an_edit_preserves_the_persisted_record_then_accepting_updates_the_same_identity(EntryKind kind)
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var original = Entry(asset, kind, 1000);
        original.Note = "Original"; Assert.True((await f.Store.SaveEntryAsync(original, Ct)).Succeeded);
        var gate = Gate(f); var calls = 0;
        async Task Save() { calls++; original.Amount = 2000; original.Note = "Updated"; Assert.True((await f.Store.SaveEntryAsync(original, Ct)).Succeeded); }
        f.Platform.Confirmations.Enqueue(false); await gate.RunAsync(kind, asset, Save);
        var kept = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal((original.Id, 1000L, "Original"), (kept.Id, kept.Amount, kept.Note)); Assert.Equal(0, calls);
        f.Platform.Confirmations.Enqueue(true); await gate.RunAsync(kind, asset, Save);
        var saved = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal((original.Id, 2000L, "Updated"), (saved.Id, saved.Amount, saved.Note)); Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(AccountType.Cash, EntryKind.Income), InlineData(AccountType.Cash, EntryKind.Expense)]
    [InlineData(AccountType.Checking, EntryKind.Income), InlineData(AccountType.Checking, EntryKind.Expense)]
    [InlineData(AccountType.Savings, EntryKind.Income), InlineData(AccountType.Savings, EntryKind.Expense)]
    [InlineData(AccountType.CreditCard, EntryKind.Income), InlineData(AccountType.CreditCard, EntryKind.Expense)]
    [InlineData(AccountType.Loan, EntryKind.Income), InlineData(AccountType.Loan, EntryKind.Expense)]
    [InlineData(AccountType.Lent, EntryKind.Income), InlineData(AccountType.Lent, EntryKind.Expense)]
    [Trait("AT", "AT-81")]
    public async Task Other_account_types_keep_their_existing_save_path_without_an_asset_prompt(AccountType type, EntryKind kind)
    {
        using var f = new FlowFixture(); var account = await AccountAsync(f, type); var entry = Entry(account, kind, 1234);
        await Gate(f).RunAsync(kind, account, async () => Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded));
        Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(entry.Id, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Id);
    }

    [Theory]
    [InlineData(EntryKind.Transfer), InlineData(EntryKind.Refund), InlineData(EntryKind.IncomeReversal)]
    [InlineData(EntryKind.Adjustment), InlineData(EntryKind.AssetPurchase), InlineData(EntryKind.AssetSale)]
    [Trait("AT", "AT-81")]
    public async Task Non_income_expense_kinds_do_not_acquire_the_legacy_asset_prompt(EntryKind kind)
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var calls = 0;
        await Gate(f).RunAsync(kind, asset, () => { calls++; return Task.CompletedTask; });
        Assert.Equal(1, calls); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), InlineData("es"), InlineData("fr"), InlineData("it"), Trait("AT", "AT-81")]
    public async Task Consent_uses_the_actual_localized_title_explanation_and_explicit_buttons(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var asset = await AccountAsync(f); var calls = 0;
        await Gate(f).RunAsync(EntryKind.Expense, asset, () => { calls++; return Task.CompletedTask; });
        var dialog = Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.Equal((f.Translator["Entry_AssetAccountTitle"], f.Translator["Entry_AssetAccountMessage"],
            f.Translator["Entry_AssetAccountYes"], f.Translator["Common_Cancel"]), dialog);
        Assert.All(new[] { dialog.Title, dialog.Message, dialog.Accept, dialog.Cancel }, text => Assert.False(string.IsNullOrWhiteSpace(text)));
        Assert.NotEqual(dialog.Accept, dialog.Cancel); Assert.Equal(0, calls);
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-81")]
    public async Task Pending_consent_rejects_a_second_save_and_never_posts_before_the_answer(bool accept)
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var entry = Entry(asset, EntryKind.Expense, 1234);
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task; var gate = Gate(f); var calls = 0;
        async Task Save() { calls++; Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded); }
        var pending = gate.RunAsync(entry.Kind, asset, Save); Assert.False(pending.IsCompleted);
        await gate.RunAsync(entry.Kind, asset, Save);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(0, calls); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        answer.SetResult(accept); await pending;
        Assert.Equal(accept ? 1 : 0, calls); Assert.Equal(accept ? 1 : 0, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact, Trait("AT", "AT-81")]
    public async Task Failed_native_confirmation_posts_nothing_and_a_later_retry_can_request_consent()
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var gate = Gate(f); var calls = 0;
        Task Save() { calls++; return Task.CompletedTask; }
        f.Platform.PendingConfirmation = () => Task.FromException<bool>(new InvalidOperationException("Fictitious missing window"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RunAsync(EntryKind.Expense, asset, Save));
        Assert.Equal(0, calls); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        f.Platform.PendingConfirmation = null; f.Platform.Confirmations.Enqueue(true); await gate.RunAsync(EntryKind.Expense, asset, Save);
        Assert.Equal(1, calls); Assert.Equal(2, f.Platform.ConfirmationDialogs.Count);
    }

    [Fact, Trait("AT", "AT-81")]
    public async Task Failed_save_does_not_reuse_consent_or_leave_the_gate_busy()
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var gate = Gate(f);
        f.Platform.Confirmations.Enqueue(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RunAsync(EntryKind.Income, asset,
            () => Task.FromException(new InvalidOperationException("Fictitious rejected save"))));
        var calls = 0; f.Platform.Confirmations.Enqueue(false);
        await gate.RunAsync(EntryKind.Income, asset, () => { calls++; return Task.CompletedTask; });
        Assert.Equal(0, calls); Assert.Equal(2, f.Platform.ConfirmationDialogs.Count); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, InlineData(EntryKind.Income), InlineData(EntryKind.Expense), Trait("AT", "AT-81")]
    public async Task Consent_does_not_bypass_the_financial_validator(EntryKind kind)
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var invalid = Entry(asset, kind, -1);
        f.Platform.Confirmations.Enqueue(true);
        await Gate(f).RunAsync(kind, asset, async () => Assert.False((await f.Store.SaveEntryAsync(invalid, Ct)).Succeeded));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(800000, LedgerCalculator.Balance(asset, [], Day));
    }

    [Fact, Trait("AT", "AT-81")]
    public async Task A_transfer_to_an_asset_stays_one_transfer_and_never_income_or_spending()
    {
        using var f = new FlowFixture(); var asset = await AccountAsync(f); var cash = await AccountAsync(f, AccountType.Cash);
        var transfer = Entry(cash, EntryKind.Transfer, 500); transfer.ToAccountId = asset.Id;
        await Gate(f).RunAsync(transfer.Kind, cash, async () => Assert.True((await f.Store.SaveEntryAsync(transfer, Ct)).Succeeded));
        var rows = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(EntryKind.Transfer, Assert.Single(rows).Kind);
        Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(1600000, LedgerCalculator.Balance(cash, rows, Day) + LedgerCalculator.Balance(asset, rows, Day));
        var totals = LedgerCalculator.Totals([cash, asset], rows, new LedgerFilter(Day, Day));
        Assert.All(totals, t => { Assert.Equal(0, t.NetIncome); Assert.Equal(0, t.NetExpense); });
    }

    private static AssetEntryConfirmation Gate(FlowFixture f) => new(f.Translator, f.Platform);
    private static LedgerEntry Entry(Account account, EntryKind kind, long amount) => new() { AccountId = account.Id, Kind = kind, Amount = amount, Date = Day };
    private static async Task<Account> AccountAsync(FlowFixture f, AccountType type = AccountType.Asset)
    {
        var account = new Account { Name = "Fictitious car", Type = type, CurrencyCode = "EUR", OpeningDate = Day.AddDays(-30), OpeningBalance = 800000 };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct)); return account;
    }
}
