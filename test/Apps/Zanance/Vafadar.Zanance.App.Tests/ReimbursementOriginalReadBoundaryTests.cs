using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original reimbursement read composition against actual separate SQLite files and committed changes.</summary>
public sealed class ReimbursementOriginalReadBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static async Task<Guid> SeedAsync(FlowFixture f, string currency, long amount, Guid? existing = null)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original reimbursement account", CurrencyCode = currency,
            Type = AccountType.Checking, OpeningDate = new(2026, 10, 1) };
        db.Accounts.Add(account);
        if (existing is { } id) db.Entry(account).Property(item => item.Id).CurrentValue = id;
        db.Entries.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 2),
            Kind = EntryKind.Expense, Title = "Original owed expense", Amount = amount,
            ReimbursableAmount = amount, ReimbursedBy = "Fictitious payer" });
        await db.SaveChangesAsync(Ct); return account.Id;
    }

    // The saved pre-correction witness used the original three-call composition. The corrected
    // path retains the original calculator, and validates the actual packet before returning it.
    private static async Task<(string Currency, long Open)> ReadOriginalAsync(ZananceStore store)
    {
        var snapshot = await store.ReadReimbursementsAsync(Ct);
        var open = Assert.Single(EntryActions.OpenReimbursements(snapshot.Inputs.Entries));
        await store.ValidateReimbursementsAsync(snapshot, Ct);
        return (snapshot.Inputs.Accounts.Single(item => item.Id == open.Expense.AccountId).CurrencyCode, open.Open);
    }

    [Fact]
    public async Task File_replacement_between_original_reads_cannot_mix_another_files_amount_with_the_original_currency()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var id = await SeedAsync(f, "EUR", 1000); await SeedAsync(other, "USD", 2500, id);
        var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Select = () => Interlocked.Increment(ref calls) == 1
            ? AccountDetailOriginalBoundaryTests.Factory(f) : AccountDetailOriginalBoundaryTests.Factory(other) };
        var reader = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadOriginalAsync(reader));
    }

    [Fact]
    public async Task A_real_committed_pair_cannot_publish_an_old_currency_with_the_new_reimbursement_amount()
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f, "EUR", 1000); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(f))
        { Before = async ct =>
            {
                if (Interlocked.Increment(ref calls) != 2) return;
                await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(ct);
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await db.Accounts.Where(item => item.Id == id).ExecuteUpdateAsync(set => set.SetProperty(item => item.CurrencyCode, "USD"), ct);
                await db.Entries.ExecuteUpdateAsync(set => set.SetProperty(item => item.Amount, 2500).SetProperty(item => item.ReimbursableAmount, (long?)2500), ct);
                await transaction.CommitAsync(ct);
            }
        };
        var reader = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadOriginalAsync(reader));
    }

    [Theory]
    [InlineData("account")]
    [InlineData("category")]
    [InlineData("ledger")]
    [InlineData("input")]
    public async Task Final_validation_refuses_each_changed_original_source_without_repair_or_initializing_settings(string change)
    {
        using var f = new FlowFixture(); await SeedAsync(f, "EUR", 1000);
        var snapshot = await f.Store.ReadReimbursementsAsync(Ct);
        if (change == "input") snapshot.Inputs.Entries[0].ReimbursedBy = "Changed only in caller memory";
        else
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
            if (change == "account") await db.Accounts.ExecuteUpdateAsync(set => set.SetProperty(item => item.Name, "New original name"), Ct);
            if (change == "ledger") await db.Entries.ExecuteUpdateAsync(set => set.SetProperty(item => item.ReimbursableAmount, (long?)900), Ct);
            if (change == "category")
            {
                db.Categories.Add(new() { Name = "New original category", Kind = Vafadar.Zanance.Core.Categories.CategoryKind.Expense });
                await db.SaveChangesAsync(Ct);
            }
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateReimbursementsAsync(snapshot, Ct));
        await using var final = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        Assert.Empty(await final.Settings.ToListAsync(Ct)); Assert.Single(await final.Entries.ToListAsync(Ct));
    }

    [Fact]
    public async Task A_retry_bound_to_the_first_actual_file_refuses_before_reading_the_replacement()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f, "EUR", 1000);
        var snapshot = await f.Store.ReadReimbursementsAsync(Ct);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.ReadReimbursementsAsync(Ct, snapshot.DatabasePath));
        Assert.Contains("another file", exception.Message, StringComparison.Ordinal);
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(other).CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }

    [Fact]
    public async Task An_unchanged_read_preserves_original_order_and_all_refund_fields_without_new_history()
    {
        using var f = new FlowFixture(); await SeedAsync(f, "EUR", 1000);
        var original = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            db.Entries.Add(new LedgerEntry { Kind = EntryKind.Refund, AccountId = original.AccountId, Date = original.Date.AddDays(1),
                Amount = 300, RefundOfId = original.Id, Title = "Original partial reimbursement", Note = "Preserved original note" });
            await db.SaveChangesAsync(Ct);
        }
        var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var snapshot = await f.Store.ReadReimbursementsAsync(Ct); await f.Store.ValidateReimbursementsAsync(snapshot, Ct);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(snapshot.Inputs.Entries));
        Assert.Equal(700, Assert.Single(EntryActions.OpenReimbursements(snapshot.Inputs.Entries)).Open);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
    }
}
