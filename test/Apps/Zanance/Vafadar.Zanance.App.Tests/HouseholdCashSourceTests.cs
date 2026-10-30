using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Complete original cash selection facts; SQL materialization here grants no protected household role.</summary>
public sealed class HouseholdCashSourceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("entry-title")]
    [InlineData("entry-note")]
    [InlineData("entry-tags")]
    [InlineData("account-name")]
    public async Task Original_SQLite_cash_selection_refuses_changed_complete_source_metadata(string field)
    {
        using var f = new FlowFixture();
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original cash account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = new(2026, 10, 30), Amount = 1234,
            Title = "Original selected expense", Note = "Original note", Tags = ["original-tag"] };
        db.Accounts.Add(account); db.Entries.Add(entry); await db.SaveChangesAsync(Ct);
        var selected = Assert.Single(await HouseholdCostStore.ReadMovementsInContextAsync(db, entry.Date, entry.Kind, "selected", Ct));
        Assert.Equal(entry.Id, selected.Id); Assert.Equal("EUR", selected.CurrencyCode); Assert.NotNull(selected.OriginalCashToken);
        await HouseholdCostStore.RequireMovementInContextAsync(db, selected, entry.Id, Ct);
        switch (field)
        {
            case "entry-title": entry.Title = "Changed selected expense"; break;
            case "entry-note": entry.Note = "Changed source note"; break;
            case "entry-tags": entry.Tags = ["changed-source-tag"]; break;
            default: account.Name = "Changed source account"; break;
        }
        await db.SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => HouseholdCostStore.RequireMovementInContextAsync(db, selected, entry.Id, Ct));
        Assert.Empty(await db.HouseholdCosts.ToListAsync(Ct)); Assert.Empty(await db.HouseholdSettlements.ToListAsync(Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_cash_selection_requires_its_opaque_source_and_exact_target(bool missingToken)
    {
        using var f = new FlowFixture();
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var account = new Account { Name = "Unpersisted source facts", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var entry = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 30), Kind = EntryKind.Expense, Amount = 1234 };
        var selected = missingToken ? new HouseholdCashMovement(entry.Id, entry.Date, entry.Kind, entry.Amount, "EUR", null, null, null)
            : HouseholdCostStore.CaptureMovement(entry, account, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => HouseholdCostStore.RequireMovementInContextAsync(db, selected,
            missingToken ? entry.Id : Guid.NewGuid(), Ct));
        Assert.Empty(await db.Entries.ToListAsync(Ct)); Assert.Empty(await db.Accounts.ToListAsync(Ct));
    }

    [Fact]
    public async Task Original_transfer_lookup_retains_both_real_account_sources_and_destination_amount()
    {
        using var f = new FlowFixture();
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var source = new Account { Name = "Original EUR side", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var target = new Account { Name = "Original GBP side", CurrencyCode = "GBP", OpeningDate = new(2026, 10, 1) };
        var entry = new LedgerEntry { AccountId = source.Id, ToAccountId = target.Id, Kind = EntryKind.Transfer,
            Date = new(2026, 10, 30), Amount = 1234, ToAmount = 2345, Title = "Original selected transfer" };
        db.Accounts.AddRange(source, target); db.Entries.Add(entry); await db.SaveChangesAsync(Ct);
        var selected = Assert.Single(await HouseholdCostStore.ReadMovementsInContextAsync(db, entry.Date, EntryKind.Transfer, "selected", Ct));
        Assert.Equal(1234, selected.Amount); Assert.Equal("EUR", selected.CurrencyCode);
        Assert.Equal(2345, selected.DestinationAmount); Assert.Equal("GBP", selected.DestinationCurrencyCode);
        await HouseholdCostStore.RequireMovementInContextAsync(db, selected, entry.Id, Ct);
        target.Name = "Changed original destination label"; await db.SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => HouseholdCostStore.RequireMovementInContextAsync(db, selected, entry.Id, Ct));
        Assert.Empty(await db.HouseholdSettlements.ToListAsync(Ct));
    }

    [Fact]
    public async Task Original_cash_fingerprint_covers_each_of_the_32_mapped_ledger_fields()
    {
        using var f = new FlowFixture();
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var properties = db.Model.FindEntityType(typeof(LedgerEntry))!.GetProperties().ToArray();
        Assert.Equal(32, properties.Length);
        var account = new Account { Name = "Original complete source", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234, Date = new(2026, 10, 30),
            Title = "Original complete cash facts", Tags = ["original-tag"] };
        var original = HouseholdCostState.CashFingerprint([entry], [account]);
        var node = JsonNode.Parse(JsonSerializer.Serialize(entry, TransactionsReadJson.Default.LedgerEntry))!.AsObject();
        foreach (var property in properties)
        {
            Assert.NotNull(property.PropertyInfo); Assert.True(node.ContainsKey(property.Name), property.Name);
            var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            var previous = property.PropertyInfo.GetValue(entry);
            JsonNode? replacement = type == typeof(Guid) ? JsonValue.Create(Guid.NewGuid())
                : type == typeof(string) ? JsonValue.Create("Changed complete source field")
                : type == typeof(long) ? JsonValue.Create(previous is long longValue ? longValue + 1 : 1L)
                : type == typeof(int) ? JsonValue.Create(previous is int intValue ? intValue + 1 : 1)
                : type == typeof(bool) ? JsonValue.Create(previous is not true)
                : type == typeof(DateOnly) ? JsonValue.Create(new DateOnly(2026, 10, 31))
                : type == typeof(DateTimeOffset) ? JsonValue.Create(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero))
                : type.IsEnum ? JsonValue.Create(Convert.ToInt32(previous, System.Globalization.CultureInfo.InvariantCulture) + 1)
                : type == typeof(List<string>) ? new JsonArray(JsonValue.Create("changed-complete-tag"))
                : throw new InvalidOperationException("Explicit original source-field mutation required: " + property.Name);
            var changed = node.DeepClone().AsObject(); changed[property.Name] = replacement;
            var candidate = changed.Deserialize(TransactionsReadJson.Default.LedgerEntry)!;
            Assert.NotEqual(original, HouseholdCostState.CashFingerprint([candidate], [account]));
        }
        Assert.Equal(["original-tag"], entry.Tags); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }
}
