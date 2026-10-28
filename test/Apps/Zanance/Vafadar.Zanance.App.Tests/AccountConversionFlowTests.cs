using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original conversion source, actual SQLite writer and application confirmation ownership, without a fake native page.</summary>
public sealed class AccountConversionFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);

    private static IDbContextFactory<ZananceDbContext> Factory(FlowFixture f) => f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
    private static HoldingStore Holdings(FlowFixture f) => f.Services.GetRequiredService<HoldingStore>();
    private static async Task<Account> AssetAsync(FlowFixture f, long amount = 10000, Guid? id = null)
    {
        var account = new Account(id ?? Guid.NewGuid()) { Name = "Original legacy asset", Type = AccountType.Asset,
            CurrencyCode = "EUR", OpeningBalance = amount, OpeningDate = Day.AddMonths(-1) };
        await f.Store.SaveAccountAsync(account, Ct); return account;
    }
    private static AccountConversionSession Session(FlowFixture f, Account account, HoldingStore? holdings = null)
    {
        var result = new AccountConversionSession(holdings ?? Holdings(f), f.Translator, f.Localization, f.Time, f.Platform);
        result.SetOwner(account.Id, true); return result;
    }
    private static async Task NoConversionAsync(FlowFixture f)
    {
        Assert.False(Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).IsArchived);
        Assert.Empty(await Holdings(f).GetTypesAsync(Ct)); Assert.Empty(await Holdings(f).GetLocationsAsync(Ct));
        Assert.Empty(await Holdings(f).GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await Holdings(f).GetValuationsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_preview_reads_complete_source_without_creating_settings_or_holdings()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f);
        var review = await Holdings(f).ReadAccountConversionAsync(account.Id, Day, Ct);
        Assert.NotNull(review); Assert.Equal(account.Id, review.AccountId); Assert.Equal(10000, review.Balance);
        Assert.Equal(account.Name, review.Inputs.Account.Name); Assert.Empty(review.Inputs.Entries);
        await using var db = await Factory(f).CreateDbContextAsync(Ct); Assert.Empty(await db.Settings.ToListAsync(Ct));
        await NoConversionAsync(f);
    }

    [Fact]
    public async Task Confirmation_cancel_leaves_original_account_and_all_holding_tables_unchanged()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var session = Session(f, account);
        f.Platform.Confirmations.Enqueue(false);
        Assert.Null(await session.ConvertAsync(account.Id, () => true));
        Assert.Contains(account.Name, Assert.Single(f.Platform.ConfirmationDialogs).Message, StringComparison.Ordinal);
        Assert.Contains("100.00", f.Platform.ConfirmationDialogs[0].Message, StringComparison.Ordinal);
        Assert.Null(session.AcceptedOutcome); Assert.Null(session.Result); Assert.Empty(f.Platform.Routes);
        await NoConversionAsync(f);
    }

    [Theory]
    [InlineData("departure")]
    [InlineData("account")]
    [InlineData("day")]
    [InlineData("language")]
    public async Task An_original_confirmation_cannot_outlive_its_actual_owner_or_display(string change)
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var session = Session(f, account);
        var consent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => consent.Task;
        var pending = session.ConvertAsync(account.Id, () => true);
        while (f.Platform.ConfirmationDialogs.Count == 0) await Task.Delay(5, Ct);
        switch (change)
        {
            case "departure": session.SetOwner(account.Id, false); break;
            case "account": session.SetOwner(Guid.NewGuid(), true); break;
            case "day": f.Time.Now = f.Time.Now.AddDays(1); break;
            case "language": f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x => x.CultureName == "de")); break;
        }
        consent.SetResult(true); Assert.Null(await pending); Assert.Null(session.AcceptedOutcome);
        Assert.Empty(f.Platform.Routes); await NoConversionAsync(f);
    }

    [Fact]
    public async Task Concurrent_double_tap_shares_no_second_confirmation_or_financial_writer()
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var session = Session(f, account);
        var consent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => consent.Task;
        var first = session.ConvertAsync(account.Id, () => true);
        while (f.Platform.ConfirmationDialogs.Count == 0) await Task.Delay(5, Ct);
        Assert.Null(await session.ConvertAsync(account.Id, () => true)); Assert.Single(f.Platform.ConfirmationDialogs);
        consent.SetResult(true); var id = await first; Assert.NotNull(id);
        Assert.Equal(id, Assert.Single(await Holdings(f).GetTypesAsync(Ct)).Id);
        Assert.Single(await Holdings(f).GetEventsAsync(cancellationToken: Ct)); Assert.Single(f.Platform.Routes);
        Assert.Equal("../holdingdetail", f.Platform.Routes[0].Route);
    }

    [Theory]
    [InlineData("metadata")]
    [InlineData("ledger")]
    public async Task Persisted_original_source_changes_during_consent_refuse_every_conversion_mutation(string change)
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var session = Session(f, account);
        f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await Factory(f).CreateDbContextAsync(Ct);
            if (change == "metadata") await db.Database.ExecuteSqlRawAsync("UPDATE Accounts SET Name='Changed after original preview'", Ct);
            else
            {
                db.Entries.Add(new LedgerEntry(Guid.NewGuid()) { AccountId = account.Id, Kind = EntryKind.Adjustment,
                    Amount = 500, Date = Day, Title = "Original concurrent adjustment" });
                await db.SaveChangesAsync(Ct);
            }
            return true;
        };
        Assert.Null(await session.ConvertAsync(account.Id, () => true)); Assert.NotNull(session.Error);
        Assert.Null(session.AcceptedOutcome); Assert.Empty(f.Platform.Routes); await NoConversionAsync(f);
        Assert.Equal(change == "ledger" ? 1 : 0, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task A_copied_account_id_in_another_file_cannot_receive_the_original_review()
    {
        using var original = new FlowFixture(); using var other = new FlowFixture();
        var account = await AssetAsync(original); await AssetAsync(other, id: account.Id);
        var factory = new SwitchingFactory(Factory(original));
        var writer = new HoldingStore(factory, original.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var review = await writer.ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        factory.Current = Factory(other);
        await Assert.ThrowsAsync<AccountConversionChangedException>(() => writer.ConvertAccountAsync(review, "Original place", () => true, Ct));
        await NoConversionAsync(original); await NoConversionAsync(other);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("entry")]
    public async Task Caller_mutation_never_changes_the_original_calculated_preview_or_grants_a_writer(string change)
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f);
        var review = await Holdings(f).ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        if (change == "account") review.Inputs.Account.OpeningBalance = 777;
        else review.Inputs.Entries.Add(new LedgerEntry(Guid.NewGuid()) { AccountId = account.Id, Kind = EntryKind.Adjustment, Amount = 9, Date = Day, Title = "Caller value" });
        Assert.Equal(10000, review.Balance);
        await Assert.ThrowsAsync<AccountConversionChangedException>(() => Holdings(f).ConvertAccountAsync(review, "Original place", () => true, Ct));
        await NoConversionAsync(f); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Original_nonpositive_balance_remains_unknown_value_without_an_invented_price(long balance)
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f, balance);
        var review = await Holdings(f).ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        var type = await Holdings(f).ConvertAccountAsync(review, "Original place", () => true, Ct);
        var item = Assert.Single(await Holdings(f).GetEventsAsync(cancellationToken: Ct));
        Assert.Equal(type.Id, item.AssetTypeId); Assert.Equal(Quantities.PerGramOrUnit, item.Quantity); Assert.Null(item.BasisAmount);
        Assert.Empty(await Holdings(f).GetValuationsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("notification")]
    [InlineData("navigation")]
    public async Task Accepted_conversion_keeps_original_receipt_when_follow_through_fails_and_never_reposts(string failure)
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var session = Session(f, account);
        var notices = 0; Holdings(f).Changed += (_, _) => { notices++; if (failure == "notification") throw new IOException("Owned notification failure."); };
        if (failure == "navigation") f.Platform.PendingNavigation = () => Task.FromException(new IOException("Owned native route failure."));
        f.Platform.Confirmations.Enqueue(true);
        var result = await session.ConvertAsync(account.Id, () => true); Assert.NotNull(result);
        Assert.Equal(result, session.AcceptedOutcome!.TypeId); Assert.Equal(account.Id, session.AcceptedOutcome.AccountId);
        Assert.NotNull(session.Result); Assert.NotNull(session.Error); Assert.Equal(1, notices); var retainedError = session.Error;
        Assert.Equal(result, await session.ConvertAsync(account.Id, () => true));
        Assert.Equal(retainedError, session.Error);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Single(f.Platform.Routes); Assert.Equal(1, notices);
        Assert.Single(await Holdings(f).GetTypesAsync(Ct)); Assert.Single(await Holdings(f).GetEventsAsync(cancellationToken: Ct));
        Assert.Single(await Holdings(f).GetValuationsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("source")]
    public async Task A_change_after_save_inside_the_actual_writer_rolls_back_archive_and_every_new_holding_row(string change)
    {
        using var f = new FlowFixture(); var account = await AssetAsync(f); var current = true;
        var factory = new TriggerFactory(Factory(f), () => current = false);
        var writer = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var review = await writer.ReadAccountConversionAsync(account.Id, Day, Ct); Assert.NotNull(review);
        await using (var db = await Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync(change == "owner"
                ? "CREATE TRIGGER RetireConversion AFTER UPDATE OF IsArchived ON Accounts WHEN NEW.IsArchived=1 BEGIN SELECT RetireOriginalConversionOwner(); END;"
                : "CREATE TRIGGER ChangeConversion AFTER UPDATE OF IsArchived ON Accounts WHEN NEW.IsArchived=1 BEGIN UPDATE Accounts SET Name='Unexpected source change' WHERE Id=NEW.Id; END;", Ct);
        await Assert.ThrowsAsync<AccountConversionChangedException>(() => writer.ConvertAccountAsync(review, "Original place", () => current, Ct));
        await NoConversionAsync(f);
        Assert.Equal(account.Name, Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
    }

    /// <summary>Changes only actual factory selection, retaining the original real SQLite contexts.</summary>
    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> current) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = current;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Current.CreateDbContextAsync(cancellationToken);
    }

    /// <summary>Registers one owned SQLite callback so retirement occurs after the real archive UPDATE inside its writer.</summary>
    private sealed class TriggerFactory(IDbContextFactory<ZananceDbContext> actual, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        private ZananceDbContext Bind(ZananceDbContext db)
        { ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireOriginalConversionOwner", () => { retire(); return 1; }); return db; }
        public ZananceDbContext CreateDbContext() => Bind(actual.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Bind(await actual.CreateDbContextAsync(cancellationToken));
    }
}
