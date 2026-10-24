using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Retains the original financial source boundary until Home's covered publication is validated.</summary>
[Trait("AT", "AT-50")]
public sealed class HomeLedgerRefreshFlowTests
{
    [Fact]
    public async Task A_committed_expense_after_the_read_retires_the_original_home_packet()
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        var day = new DateOnly(2026, 10, 9);
        var account = new Account { Name = "Concurrent cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await fixture.Store.SaveAccountAsync(account, ct);
        var original = await fixture.Store.GetHomeSnapshotAsync(day, 0, ct);
        Assert.Empty(original.Entries);
        var saved = await fixture.Store.SaveEntryAsync(new LedgerEntry
        {
            AccountId = account.Id, Date = day, Kind = EntryKind.Expense, Amount = 1000, Title = "Later original expense"
        }, ct);
        Assert.True(saved.Succeeded);
        await Assert.ThrowsAsync<HomeSnapshotSupersededException>(() => fixture.Store.ValidateHomeSnapshotAsync(original, ct));
    }

    [Theory]
    [InlineData("Entries", "INSERT")]
    [InlineData("Entries", "UPDATE")]
    [InlineData("Entries", "DELETE")]
    [InlineData("Budgets", "INSERT")]
    [InlineData("Budgets", "UPDATE")]
    [InlineData("Budgets", "DELETE")]
    [InlineData("BudgetCategoryLimits", "INSERT")]
    [InlineData("BudgetCategoryLimits", "UPDATE")]
    [InlineData("BudgetCategoryLimits", "DELETE")]
    [InlineData("ExchangeRates", "INSERT")]
    [InlineData("ExchangeRates", "UPDATE")]
    [InlineData("ExchangeRates", "DELETE")]
    [InlineData("AssetTypes", "INSERT")]
    [InlineData("AssetTypes", "UPDATE")]
    [InlineData("AssetEvents", "INSERT")]
    [InlineData("AssetEvents", "UPDATE")]
    [InlineData("AssetEvents", "DELETE")]
    public async Task A_raw_financial_writer_retires_the_captured_home_packet(string table, string operation)
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var day = new DateOnly(2026, 10, 9);
        var extraCategory = new Category { Name = "Extra category", Kind = CategoryKind.Expense };
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = day };
            var category = new Category { Name = "Original category", Kind = CategoryKind.Expense };
            var type = new AssetType { Name = "Original type", PriceCurrencyCode = "EUR" };
            var location = new AssetLocation { Name = "Original location" };
            db.AddRange(account, category, extraCategory, type, location);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, Date = day, Kind = EntryKind.Expense, Amount = 100 });
            db.Budgets.Add(new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 1000,
                CategoryLimits = [new BudgetCategoryLimit { CategoryId = category.Id, Limit = 500 }] });
            db.ExchangeRates.Add(new ExchangeRate { FromCurrencyCode = "EUR", ToCurrencyCode = "USD", Date = day, Rate = 1.1m });
            if (table != "AssetTypes" || operation != "DELETE")
                db.AssetEvents.Add(new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Date = day, Kind = AssetEventKind.Opening, Quantity = 1000 });
            await db.SaveChangesAsync(ct);
        }
        var packet = await fixture.Store.GetHomeSnapshotAsync(day, 0, ct);
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            // Fixed theory identifiers exercise real SQLite triggers, including writers outside EF tracking.
            string sql;
            if (operation == "INSERT")
            {
                await db.Database.OpenConnectionAsync(ct);
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"PRAGMA table_info(\"{table}\")";
                var columns = new List<string>();
                await using (var reader = await command.ExecuteReaderAsync(ct))
                    while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(1));
                var key = table == "BudgetCategoryLimits" ? "CategoryId" : "Id";
                var value = table == "BudgetCategoryLimits" ? extraCategory.Id : Guid.NewGuid();
                sql = $"INSERT INTO \"{table}\" ({string.Join(",", columns.Select(c => $"\"{c}\""))}) SELECT " +
                    string.Join(",", columns.Select(c => c == key ? $"'{value:D}'"
                        : table == "ExchangeRates" && c == "FromCurrencyCode" ? "'GBP'"
                        : table == "Budgets" && c == "Month" ? "9"
                        : table == "AssetTypes" && c == "Name" ? "'Other type'"
                        : $"\"{c}\"")) + $" FROM \"{table}\" LIMIT 1";
            }
            else if (operation == "UPDATE")
            {
                var change = table switch
                {
                    "Entries" => "Amount = Amount + 1", "Budgets" => "TotalLimit = TotalLimit + 1",
                    "BudgetCategoryLimits" => "\"Limit\" = \"Limit\" + 1", "ExchangeRates" => "IsEstimate = 1 - IsEstimate",
                    "AssetTypes" => "Name = Name || ' changed'", "AssetEvents" => "Quantity = Quantity + 1",
                    _ => throw new InvalidOperationException()
                };
                sql = $"UPDATE \"{table}\" SET {change}";
            }
            else sql = $"DELETE FROM \"{table}\"";
            Assert.Equal(1, await db.Database.ExecuteSqlRawAsync(sql, ct));
        }
        await Assert.ThrowsAsync<HomeSnapshotSupersededException>(() => fixture.Store.ValidateHomeSnapshotAsync(packet, ct));
    }

    [Fact]
    public Task Deletion_of_an_unreferenced_asset_type_retires_home() =>
        A_raw_financial_writer_retires_the_captured_home_packet("AssetTypes", "DELETE");

    [Fact]
    public async Task A_rolled_back_financial_write_does_not_retire_the_original_packet()
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        var day = new DateOnly(2026, 10, 9);
        var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = day };
        await fixture.Store.SaveAccountAsync(account, ct);
        var packet = await fixture.Store.GetHomeSnapshotAsync(day, 0, ct);
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, Date = day, Amount = 100, Kind = EntryKind.Expense });
            await db.SaveChangesAsync(ct);
            await transaction.RollbackAsync(ct);
        }
        await fixture.Store.ValidateHomeSnapshotAsync(packet, ct);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
    }

    [Fact]
    public async Task Covered_refresh_rereads_the_new_ledger_without_repeating_its_accepted_write()
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        var day = new DateOnly(2026, 10, 9);
        var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await fixture.Store.SaveAccountAsync(account, ct);
        var reads = 0; var writes = 0;
        await HomeRefreshSession.RunAsync(async refresh =>
        {
            var packet = await fixture.Store.GetHomeSnapshotAsync(day, 0, ct); reads++;
            refresh.RequireOriginalFile(packet.DatabasePath);
            if (reads == 1)
            {
                Assert.True((await fixture.Store.SaveEntryAsync(new LedgerEntry
                    { AccountId = account.Id, Date = day, Amount = 1000, Kind = EntryKind.Expense }, ct)).Succeeded);
                writes++;
            }
            await fixture.Store.ValidateHomeSnapshotAsync(packet, ct);
            Assert.Equal(9000, LedgerCalculator.Balance(account, packet.Entries, day));
        }, () => ct.ThrowIfCancellationRequested());
        Assert.Equal(2, reads); Assert.Equal(1, writes);
        Assert.Single(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
    }
}
