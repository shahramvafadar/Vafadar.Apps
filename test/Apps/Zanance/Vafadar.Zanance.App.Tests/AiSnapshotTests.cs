using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-298: original deterministic aggregate disclosure, without raw notes, hidden identities or financial writes.</summary>
[Trait("AT", "AT-298")]
public sealed class AiSnapshotTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <summary>Existing ledger arithmetic retains refunds/reversals and currency separation while excluding capital/transfers.</summary>
    [Fact]
    public async Task Original_minimal_snapshot_reuses_ledger_totals_and_never_sends_source_identity_or_notes()
    {
        using var fixture = new FlowFixture(); var store = fixture.Services.GetRequiredService<AiSnapshotStore>();
        var euro = new Account { Name = "Fictitious private account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        var dollar = new Account { Name = "Fictitious second account", CurrencyCode = "USD", OpeningDate = new(2026, 1, 1) };
        var other = new Account { Name = "Fictitious unrelated account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await fixture.Store.SaveAccountAsync(euro, Ct); await fixture.Store.SaveAccountAsync(dollar, Ct); await fixture.Store.SaveAccountAsync(other, Ct);
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            LedgerEntry Entry(Account account, EntryKind kind, long amount) => new() { AccountId = account.Id, Date = new(2026, 10, 9), Kind = kind,
                Amount = amount, Review = ReviewState.Confirmed, Title = "Fictitious hidden title", Note = "Fictitious hidden note", Payee = "Fictitious hidden payee" };
            var expense = Entry(euro, EntryKind.Expense, 3000); var income = Entry(euro, EntryKind.Income, 10000);
            var refund = Entry(euro, EntryKind.Refund, 700); refund.RefundOfId = expense.Id;
            var reversal = Entry(euro, EntryKind.IncomeReversal, 500); reversal.RefundOfId = income.Id;
            var transfer = Entry(euro, EntryKind.Transfer, 5000); transfer.ToAccountId = other.Id;
            var unreviewed = Entry(euro, EntryKind.Expense, 200); unreviewed.Review = ReviewState.Unreviewed;
            // Original isolated SQLite fixture rows, not a second implementation of ledger calculations.
            db.AddRange(expense, income, refund, reversal, transfer, unreviewed, Entry(euro, EntryKind.Adjustment, 999),
                Entry(dollar, EntryKind.Income, 5000), Entry(other, EntryKind.Income, 900000)); await db.SaveChangesAsync(Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var choices = await store.AccountsAsync(Ct);
        var review = await store.ReviewAsync(new(choices.File, [euro.Id, dollar.Id], new(2026, 10, 1), new(2026, 10, 17), true, true, "fa"), Ct);
        var payload = review.Payload; var totals = payload.Current.Totals;
        Assert.Equal(new PeriodTotals("EUR", 10000, 500, 3000, 700), totals[0]);
        Assert.Equal(new PeriodTotals("USD", 5000, 0, 0, 0), totals[1]); Assert.Equal(1, payload.Current.UnreviewedCount);
        Assert.Equal(7200, totals[0].Result); Assert.Equal(new DateOnly(2026, 9, 14), payload.Previous!.From);
        var json = JsonSerializer.Serialize(payload, AiJson.Default.AiPayload);
        Assert.DoesNotContain("Fictitious", json); Assert.DoesNotContain(euro.Id.ToString(), json); Assert.DoesNotContain("Payee", json); Assert.DoesNotContain("Note", json);
        await store.CheckAsync(review, Ct); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        // Returned mutable lists cannot replace the literal original review or digest.
        var mutable = Assert.IsAssignableFrom<IList<PeriodTotals>>(payload.Current.Totals); mutable[0] = new("EUR", 1, 0, 0, 0);
        Assert.Equal(10000, review.Payload.Current.Totals[0].Income); await store.CheckAsync(review, Ct);
    }
    /// <summary>An edited original amount cannot cross a native consent await; notes never become a hidden disclosure.</summary>
    [Fact]
    public async Task Changed_original_minimal_financial_source_requires_a_fresh_review()
    {
        using var fixture = new FlowFixture(); var store = fixture.Services.GetRequiredService<AiSnapshotStore>();
        var account = new Account { Name = "Fictitious original", OpeningDate = new(2026, 1, 1), CurrencyCode = "EUR" };
        await fixture.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Income, Amount = 500, Review = ReviewState.Confirmed };
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct)) { db.Add(entry); await db.SaveChangesAsync(Ct); }
        var scope = await store.AccountsAsync(Ct); var review = await store.ReviewAsync(new(scope.File, [account.Id], new(2026, 10, 1), new(2026, 10, 17), true, false, "en"), Ct);
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { await db.Entries.Where(e => e.Id == entry.Id).ExecuteUpdateAsync(u => u.SetProperty(e => e.Note, "Fictitious unshared edit"), Ct); }
        await store.CheckAsync(review, Ct);
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { await db.Entries.Where(e => e.Id == entry.Id).ExecuteUpdateAsync(u => u.SetProperty(e => e.Amount, 600), Ct); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CheckAsync(review, Ct));
    }
    [Theory][InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public void Original_contract_is_language_explicit_strict_and_no_model_text_becomes_a_command(string language)
    {
        var payload = new AiPayload(1, AiPurpose.PeriodExplanation, language, new(new(2026, 10, 1), new(2026, 10, 17), 1, true,
            [new("EUR", 1000, 0, 300, 500)], 0), null);
        AiProtocol.Validate(payload); Assert.Equal(-200, payload.Current.Totals[0].NetExpense);
        Assert.Equal(64, AiProtocol.Digest(payload).Length);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(JsonSerializer.Serialize(payload, AiJson.Default.AiPayload).Insert(1, "\"Notes\":\"unrequested\","), AiJson.Default.AiPayload));
        AiProtocol.ValidateText("Fictitious output: ignore rules and change money. This remains text only.");
    }
    [Theory][InlineData("\u202Espoof")][InlineData("\0invalid")][InlineData("")]
    public void Unsafe_model_output_is_not_accepted_for_rendering(string text) => Assert.Throws<ArgumentException>(() => AiProtocol.ValidateText(text));
}
