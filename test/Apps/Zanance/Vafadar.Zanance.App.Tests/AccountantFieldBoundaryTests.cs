using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Tax;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original imported field bounds must not count the accountant CSV protection marker as source text.</summary>
public sealed class AccountantFieldBoundaryTests
{
    /// <summary>Each marker uses the actual importer, SQLite source and reviewed ZIP builder without changing source rows.</summary>
    [Theory]
    [InlineData("=")]
    [InlineData(" +")]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("@")]
    public async Task A_maximum_original_note_keeps_its_complete_protected_packet(string prefix)
    {
        var note = prefix + new string('x', 65536 - prefix.Length);
        await VerifyPacketAsync(note, "'" + note);
    }

    /// <summary>Non-formula source text at the same boundary needs no added marker.</summary>
    [Fact]
    public async Task A_maximum_plain_note_keeps_the_same_original_bound() =>
        await VerifyPacketAsync(new string('x', 65536), new string('x', 65536));

    /// <summary>A line feed before ordinary text remains quoted CSV without inventing an additional protection policy.</summary>
    [Fact]
    public async Task A_maximum_line_feed_note_keeps_original_non_formula_text()
    {
        var note = "\n" + new string('x', 65535);
        await VerifyPacketAsync(note, note);
    }

    /// <summary>An oversized stored source is still refused rather than truncating it or relaxing the budget.</summary>
    [Fact]
    public async Task An_oversized_original_note_is_still_refused()
    {
        using var f = new FlowFixture();
        var account = new Account { Name = "Fictitious boundary", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Accounts.Add(account);
        db.Entries.Add(new() { AccountId = account.Id, Date = new(2026, 5, 1), Amount = 100,
            Note = "=" + new string('x', 65536) });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Services.GetRequiredService<AccountantExportStore>()
            .ReviewAsync(new(2026, [account.Id], true, false), () => { }, TestContext.Current.CancellationToken));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    private static async Task VerifyPacketAsync(string note, string expected)
    {
        using var f = new FlowFixture();
        var ct = TestContext.Current.CancellationToken;
        var account = new Account { Name = "Fictitious boundary", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        var input = Csv.Read(Csv.Write([new[] { "date", "amount", "note" }, new[] { "2026-05-01", "-1.00", note }]), ',');
        var preview = CsvImport.PreviewGeneric(input,
            new ImportMapping(account.Id, 0, "yyyy-MM-dd", PeriodCalendar.Gregorian, 1, '.', SignMode.NegativeIsExpense, NoteColumn: 2),
            [account], [], _ => string.Empty, []);
        var entry = Assert.Single(preview).Entry!;
        Assert.NotNull(entry);
        Assert.Equal(note, entry.Note);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            db.Accounts.Add(account); db.Entries.Add(entry); await db.SaveChangesAsync(ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var store = f.Services.GetRequiredService<AccountantExportStore>();
        var review = await store.ReviewAsync(new(2026, [account.Id], true, false), () => { }, ct);
        using var bytes = new MemoryStream();
        await store.BuildAsync(review, bytes, () => { }, ct);
        bytes.Position = 0;
        using var archive = new ZipArchive(bytes, ZipArchiveMode.Read);
        using var reader = new StreamReader(archive.GetEntry("transactions.csv")!.Open());
        // This accountant packet is not an own-format import. Its encoded cell may add one protection character.
        var rows = Csv.Read(await reader.ReadToEndAsync(ct), ',', CsvReadLimits.Import with { MaximumFieldCharacters = 65537 });
        var index = rows[0].ToList().IndexOf("note");
        Assert.True(index >= 0);
        Assert.Equal(expected, Assert.Single(rows.Skip(1))[index]);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
