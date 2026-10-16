using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Actual migrated SQLite commit barriers for linked cash and complete household allocations.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdCostPersistenceTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <inheritdoc />
    public void Dispose() { SqliteTestPools.Clear(directory); directory.Dispose(); }

    /// <summary>Archival retains historical shares, but editing an old cost cannot assign a new share to an archived contact.</summary>
    [Fact]
    public async Task Archived_contact_keeps_history_but_cannot_be_added_to_an_existing_cost()
    {
        await using var services = new ServiceCollection().AddZananceData(directory.Combine("archived.db")).BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var first = new HouseholdContact { Name = "Fictitious original participant" };
        var archived = new HouseholdContact { Name = "Fictitious archived participant" };
        var cost = new HouseholdCost { Title = "Fictitious original cost", Kind = HouseholdCostKind.Expense, Date = new(2026, 10, 16),
            Amount = 100, CurrencyCode = "EUR", PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = first.Id };
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            db.HouseholdContacts.AddRange(first, archived); db.HouseholdCosts.Add(cost);
            db.HouseholdAllocations.Add(new() { CostId = cost.Id, ContactId = first.Id, Amount = 100 }); await db.SaveChangesAsync(Ct);
        }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        { (await db.HouseholdContacts.SingleAsync(row => row.Id == archived.Id, Ct)).IsArchived = true; await db.SaveChangesAsync(Ct); }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            (await db.HouseholdCosts.SingleAsync(Ct)).Amount = 200;
            db.HouseholdAllocations.Add(new() { CostId = cost.Id, ContactId = archived.Id, Amount = 100 });
            var error = await Assert.ThrowsAsync<HouseholdCostRejectedException>(() => db.SaveChangesAsync(Ct));
            Assert.Contains(error.Issues, issue => issue.Code == "HouseholdContactArchived");
        }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            (await db.HouseholdContacts.SingleAsync(row => row.Id == first.Id, Ct)).IsArchived = true; await db.SaveChangesAsync(Ct);
            (await db.HouseholdCosts.SingleAsync(Ct)).Note = "Fictitious historical correction"; await db.SaveChangesAsync(Ct);
            Assert.Equal(100L, (await db.HouseholdCosts.SingleAsync(Ct)).Amount);
            Assert.Equal(first.Id, (await db.HouseholdAllocations.SingleAsync(Ct)).ContactId);
        }
    }

    /// <summary>Original ledger edits cannot silently invalidate a household annotation or create a second expense.</summary>
    [Fact]
    public async Task Linked_cash_edit_is_rolled_back_when_it_breaks_the_saved_cost_and_allocations()
    {
        await using var services = new ServiceCollection().AddZananceData(directory.Combine("linked.db")).BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var person = new HouseholdContact { Name = "Fictitious payer" };
        var account = new Account { Name = "Fictitious shared cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 16), Amount = 2100 };
        var cost = new HouseholdCost { Title = "Fictitious actual cost", Kind = HouseholdCostKind.Expense, Date = entry.Date,
            Amount = entry.Amount, CurrencyCode = "EUR", PaymentKind = HouseholdPaymentKind.SharedAccount, EntryId = entry.Id };
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            db.Accounts.Add(account); db.Entries.Add(entry); db.HouseholdContacts.Add(person); db.HouseholdCosts.Add(cost);
            db.HouseholdAllocations.Add(new() { CostId = cost.Id, ContactId = person.Id, Amount = cost.Amount });
            await db.SaveChangesAsync(Ct);
        }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            (await db.Entries.SingleAsync(Ct)).Amount = 2101;
            var error = await Assert.ThrowsAsync<HouseholdCostRejectedException>(() => db.SaveChangesAsync(Ct));
            Assert.Contains(error.Issues, issue => issue.Code == "SharedHouseholdMovementMismatch");
        }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            Assert.Equal(2100L, (await db.Entries.SingleAsync(Ct)).Amount);
            Assert.Equal(2100m, Assert.Single(HouseholdCostLedger.Totals(await db.HouseholdCosts.ToArrayAsync(Ct))).NetCost);
            Assert.Equal(1, await db.Entries.CountAsync(Ct));
        }
    }

    /// <summary>Complete replacements may span SaveChanges calls, but an invalid outer commit publishes none of them.</summary>
    [Fact]
    public async Task Outer_transaction_accepts_complete_shares_then_rejects_partial_replacement_without_losing_originals()
    {
        await using var services = new ServiceCollection().AddZananceData(directory.Combine("compound.db")).BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var person = new HouseholdContact { Name = "Fictitious private payer" };
        var cost = new HouseholdCost { Title = "Fictitious private cost", Kind = HouseholdCostKind.Expense, Date = new(2026, 10, 16),
            Amount = 101, CurrencyCode = "KWD", PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = person.Id };
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Ct);
            db.HouseholdContacts.Add(person); db.HouseholdCosts.Add(cost); await db.SaveChangesAsync(Ct);
            db.HouseholdAllocations.Add(new() { CostId = cost.Id, ContactId = person.Id, Amount = 101 }); await db.SaveChangesAsync(Ct);
            await transaction.CommitAsync(Ct);
        }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(Ct);
            (await db.HouseholdCosts.SingleAsync(Ct)).Amount = 102; await db.SaveChangesAsync(Ct);
            var error = await Assert.ThrowsAsync<HouseholdCostRejectedException>(() => transaction.CommitAsync(Ct));
            Assert.Contains(error.Issues, issue => issue.Code == "HouseholdAllocationSumMismatch");
        }
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            Assert.Equal(101L, (await db.HouseholdCosts.SingleAsync(Ct)).Amount);
            Assert.Equal(101L, (await db.HouseholdAllocations.SingleAsync(Ct)).Amount);
            Assert.Empty(await db.Entries.ToArrayAsync(Ct));
        }
    }
}
