using Vafadar.Finance.Core.Ledger;

namespace Vafadar.Finance.Core.Tests.Ledger;

public sealed class EntryTemplateTests
{
    [Fact]
    public void A_template_keeps_what_repeats_but_never_the_date_note_review_or_links()
    {
        var entry = new LedgerEntry
        {
            Kind = EntryKind.Expense,
            AccountId = Guid.CreateVersion7(),
            CategoryId = Guid.CreateVersion7(),
            Amount = 350,
            Date = new DateOnly(2026, 9, 1),
            Title = "Coffee",
            Payee = "Corner cafe",
            Note = "with Sara",
            Review = ReviewState.Unreviewed,
            ScheduleId = Guid.CreateVersion7(),
            OccurrenceDate = new DateOnly(2026, 9, 1),
        };

        var template = EntryTemplate.From(entry, " Coffee ", keepAmount: true);
        var created = template.CreateEntry(new DateOnly(2026, 10, 5));

        Assert.Equal("Coffee", template.Name);
        Assert.Equal(350, created.Amount);
        Assert.Equal(entry.CategoryId, created.CategoryId);
        Assert.Equal("Corner cafe", created.Payee);
        Assert.Equal(new DateOnly(2026, 10, 5), created.Date);
        Assert.Null(created.Note);
        Assert.Null(created.ScheduleId);
        Assert.Null(created.OccurrenceDate);
        Assert.NotEqual(entry.Id, created.Id);
        Assert.Null(EntryTemplate.From(entry, "Coffee", keepAmount: false).Amount);
    }

    [Fact]
    public void Refunds_and_adjustments_cannot_become_templates()
    {
        var refund = new LedgerEntry { Kind = EntryKind.Refund, AccountId = Guid.CreateVersion7(), Amount = 1, RefundOfId = Guid.CreateVersion7() };

        Assert.Throws<ArgumentException>(() => EntryTemplate.From(refund, "Refund", keepAmount: true));
    }
}