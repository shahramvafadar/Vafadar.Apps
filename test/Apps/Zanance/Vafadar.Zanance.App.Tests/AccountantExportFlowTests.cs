using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Tax;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-296: actual coherent accountant packets and original application commands, not duplicated projections.</summary>
[Trait("AT", "AT-296")]
public sealed class AccountantExportFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Access : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class Native : IAccountantExportRuntime
    {
        public int Confirms, Writes, Shares, Removes;
        public byte[] Bytes = [];
        public bool Available = true, Confirm = true;
        public Func<Task>? DuringConfirm, AfterWrite;
        public void EnsureAvailable() { if (!Available) throw new InvalidOperationException("Fictitious covered host."); }
        public async Task<bool> ConfirmAsync() { Confirms++; if (DuringConfirm is not null) await DuringConfirm(); return Confirm; }
        public async Task<string> WriteAsync(Func<Stream, Task> write)
        { Writes++; using var memory = new MemoryStream(); await write(memory); Bytes = memory.ToArray(); if (AfterWrite is not null) await AfterWrite(); return "owned-packet.zip"; }
        public Task ShareAsync(string path) { Assert.Equal("owned-packet.zip", path); Shares++; return Task.CompletedTask; }
        public void Remove(string path) { Assert.Equal("owned-packet.zip", path); Removes++; }
    }
    private static AccountantExportStore Store(FlowFixture f) => f.Services.GetRequiredService<AccountantExportStore>();
    private static async Task<(Account First, Account Linked, LedgerEntry Expense)> SeedAsync(FlowFixture f)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var first = new Account { Name = "=Fictitious private salary", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1), OpeningBalance = 987654321, IsArchived = true };
        var linked = new Account { Name = "Fictitious other-currency transfer", CurrencyCode = "USD", OpeningDate = new(2020, 1, 1) };
        var excluded = new Account { Name = "NEVER_DISCLOSE_ACCOUNT", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        db.Accounts.AddRange(first, linked, excluded);
        var expense = new LedgerEntry { Date = new(2026, 1, 2), AccountId = first.Id, Amount = 12345, Kind = EntryKind.Expense,
            Title = "+invoice", Payee = "PRIVATE_PAYEE", Note = "PRIVATE_NOTE", Review = ReviewState.Unreviewed, Tags = ["=tag"] };
        var transfer = new LedgerEntry { Date = new(2026, 2, 3), AccountId = linked.Id, ToAccountId = first.Id, Amount = 3500, ToAmount = 3200, Kind = EntryKind.Transfer };
        var refund = new LedgerEntry { Date = new(2026, 3, 4), AccountId = first.Id, Amount = 5000, Kind = EntryKind.Refund, RefundOfId = expense.Id };
        db.Entries.AddRange(expense, transfer, refund,
            new() { Date = new(2025, 12, 31), AccountId = first.Id, Amount = 90000, Kind = EntryKind.Income, Title = "NEVER_DISCLOSE_PRIOR_YEAR" },
            new() { Date = new(2026, 1, 1), AccountId = excluded.Id, Amount = 90000, Kind = EntryKind.Income, Title = "NEVER_DISCLOSE_OTHER_ACCOUNT" });
        db.Attachments.Add(new() { EntryId = expense.Id, FileName = "../../=private-original.pdf", ContentType = "application/pdf", Data = "%PDF-fictitious\n"u8.ToArray() });
        db.Attachments.Add(new() { EntryId = Guid.NewGuid(), FileName = "NEVER_DISCLOSE_ORPHAN", ContentType = "text/html", Data = "orphan"u8.ToArray() });
        await db.SaveChangesAsync(Ct); return (first, linked, expense);
    }
    private static async Task<byte[]> PacketAsync(FlowFixture f, AccountantExportRequest request)
    {
        var review = await Store(f).ReviewAsync(request, () => { }, Ct);
        using var bytes = new MemoryStream(); await Store(f).BuildAsync(review, bytes, () => { }, Ct); return bytes.ToArray();
    }
    private static string Text(ZipArchive zip, string path)
    { using var reader = new StreamReader(zip.GetEntry(path)!.Open(), Encoding.UTF8); return reader.ReadToEnd(); }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task Coherent_exact_rows_preserve_transfer_refund_scope_and_opt_in_document_bytes(bool documents)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var bytes = await PacketAsync(f, new(2026, [seed.First.Id], false, documents));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        var csv = Text(zip, "transactions.csv"); var rows = Csv.Read(csv, ','); var columns = rows[0];
        Assert.Equal(4, rows.Count); Assert.Single(rows.Skip(1), row => row[2] == "Transfer");
        var transfer = Assert.Single(rows.Skip(1), row => row[2] == "Transfer");
        Assert.Equal("3500", transfer[3]); Assert.Equal("USD", transfer[4]); Assert.Equal("3200", transfer[8]); Assert.Equal("EUR", transfer[9]);
        var refund = Assert.Single(rows.Skip(1), row => row[2] == "Refund"); Assert.Equal(seed.Expense.Id.ToString(), refund[16]);
        Assert.DoesNotContain("PRIVATE_NOTE", csv); Assert.DoesNotContain("PRIVATE_PAYEE", csv); Assert.Contains("'+invoice", csv);
        Assert.DoesNotContain("987654321", Text(zip, "accounts.csv")); Assert.Contains("'=Fictitious", Text(zip, "accounts.csv"));
        Assert.DoesNotContain("NEVER_DISCLOSE", string.Join('\n', zip.Entries.Where(e => !e.FullName.StartsWith("documents/", StringComparison.Ordinal)).Select(e => Text(zip, e.FullName))));
        using var manifest = JsonDocument.Parse(Text(zip, "manifest.json")); Assert.Equal(3, manifest.RootElement.GetProperty("ledgerRows").GetInt32());
        Assert.Equal(documents ? 1 : 0, manifest.RootElement.GetProperty("storedDocumentCopies").GetInt32());
        foreach (var hash in manifest.RootElement.GetProperty("sha256").EnumerateObject())
        { using var entry = zip.GetEntry(hash.Name)!.Open(); Assert.Equal(hash.Value.GetString(), Convert.ToHexStringLower(SHA256.HashData(entry))); }
        var attachment = zip.Entries.SingleOrDefault(e => e.FullName.StartsWith("documents/", StringComparison.Ordinal));
        if (documents) { Assert.NotNull(attachment); Assert.EndsWith(".pdf", attachment.FullName); Assert.DoesNotContain("..", attachment.FullName); using var data = attachment.Open(); Assert.Equal(SHA256.HashData("%PDF-fictitious\n"u8), SHA256.HashData(data)); }
        else { Assert.Null(attachment); Assert.Null(zip.GetEntry("documents.csv")); }
        Assert.Contains("not taxable income", Text(zip, "recorded-totals.csv"));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory][InlineData("entry")][InlineData("document")][InlineData("account")][InlineData("day")][InlineData("scope")]
    public async Task A_changed_original_review_never_publishes_a_different_packet(string change)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var store = Store(f);
        var review = await store.ReviewAsync(new(2026, [seed.First.Id], true, true), () => { }, Ct);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            if (change == "entry") (await db.Entries.SingleAsync(e => e.Id == seed.Expense.Id, Ct)).Amount++;
            if (change == "document") (await db.Attachments.FirstAsync(a => a.EntryId == seed.Expense.Id, Ct)).Data = "changed"u8.ToArray();
            if (change == "account") (await db.Accounts.FirstAsync(a => a.Id == seed.First.Id, Ct)).Name += " changed";
            await db.SaveChangesAsync(Ct);
        }
        if (change == "day") f.Time.Now = f.Time.Now.AddDays(1);
        using var destination = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.BuildAsync(review, destination,
            () => { if (change == "scope") throw new InvalidOperationException("Fictitious retired form."); }, Ct));
    }

    [Theory][InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Real_form_requires_explicit_selection_review_and_native_confirmation_without_saving(string language)
    {
        using var f = new FlowFixture(); await SeedAsync(f); f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var native = new Native(); var vm = new AccountantExportViewModel(Store(f), f.Translator, f.Time, f.Platform, native);
        await vm.LoadAsync(); Assert.All(vm.Accounts, a => Assert.False(a.Selected)); Assert.False(vm.IncludeDocuments); Assert.False(vm.IncludeNotes);
        vm.Year = "bad"; await vm.ReviewAsync(); Assert.NotNull(vm.YearError); Assert.NotNull(vm.AccountsError);
        vm.Year = "۲۰۲۶"; vm.Accounts[0].Selected = true; await vm.ReviewAsync(); Assert.True(vm.CanShare); Assert.NotNull(vm.Summary);
        f.Platform.Confirmations.Enqueue(false); await vm.ShareAsync(); Assert.Equal(0, native.Writes);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Platform.Confirmations.Enqueue(true); await vm.ShareAsync(); Assert.Equal((1, 1, 1), (native.Confirms, native.Writes, native.Shares)); Assert.Equal(0, native.Removes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        vm.IncludeNotes = true; Assert.False(vm.CanShare); Assert.Null(vm.Summary);
    }

    [Theory][InlineData("retire")][InlineData("selection")][InlineData("data")][InlineData("lock")]
    public async Task Native_await_changes_refuse_share_and_remove_only_unpublished_files(string change)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var native = new Native();
        var vm = new AccountantExportViewModel(Store(f), f.Translator, f.Time, f.Platform, native);
        await vm.LoadAsync(); vm.Accounts.First(a => a.Id == seed.First.Id).Selected = true; await vm.ReviewAsync();
        f.Platform.Confirmations.Enqueue(true);
        if (change == "retire") native.DuringConfirm = () => { vm.Retire(); return Task.CompletedTask; };
        if (change == "selection") native.DuringConfirm = () => { vm.IncludeDocuments = true; return Task.CompletedTask; };
        if (change == "lock") native.AfterWrite = () => { native.Available = false; return Task.CompletedTask; };
        if (change == "data") native.AfterWrite = async () =>
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            (await db.Entries.FirstAsync(e => e.Id == seed.Expense.Id, Ct)).Amount++; await db.SaveChangesAsync(Ct);
        };
        await vm.ShareAsync(); Assert.Equal(0, native.Shares);
        Assert.Equal(change is "data" or "lock" ? 1 : 0, native.Removes); Assert.False(vm.IsBusy);
    }

    [Theory][InlineData(ProductPlan.Free, false)][InlineData(ProductPlan.Plus, true)][InlineData(ProductPlan.Pro, true)]
    public async Task Separate_level_one_plus_right_is_rechecked_without_activating_commercial_limits(ProductPlan plan, bool allowed)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f); var access = new Access();
        using var p = new ServiceCollection().AddSingleton<TimeProvider>(f.Time).AddSingleton<ICommercialWriteAccessSource>(access)
            .AddZananceData(f.Directory.Combine("zanance.db")).BuildServiceProvider();
        access.Current = new(f.Directory.Combine("zanance.db"), new(plan, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        var store = p.GetRequiredService<AccountantExportStore>(); var request = new AccountantExportRequest(2026, [seed.First.Id], false, false);
        Assert.Equal(FeaturePermission.RequiresAddOn, PlanPolicy.Check(CommercialFeature.TaxAddOn, access.Current.Context!));
        if (!allowed) { await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.ReviewAsync(request, () => { }, Ct)); return; }
        var review = await store.ReviewAsync(request, () => { }, Ct);
        access.Current = CommercialWriteAccess.Inactive;
        using var destination = new MemoryStream(); await Assert.ThrowsAsync<InvalidOperationException>(() => store.BuildAsync(review, destination, () => { }, Ct));
    }

    [Fact]
    public async Task Unknown_currency_keeps_exact_minor_units_and_never_guesses_precision_or_executes_indented_text()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            (await db.Accounts.SingleAsync(a => a.Id == seed.First.Id, Ct)).CurrencyCode = "ZZZ";
            var entry = await db.Entries.SingleAsync(e => e.Id == seed.Expense.Id, Ct);
            entry.Amount = long.MaxValue; entry.Title = " \n=SUM(1,2)"; entry.Note = "Approved exact optional note";
            await db.SaveChangesAsync(Ct);
        }
        var bytes = await PacketAsync(f, new(2026, [seed.First.Id], true, false)); using var zip = new ZipArchive(new MemoryStream(bytes));
        var row = Assert.Single(Csv.Read(Text(zip, "transactions.csv"), ',').Skip(1), r => r[0] == seed.Expense.Id.ToString());
        Assert.Equal("9223372036854775807", row[3]); Assert.Equal("ZZZ", row[4]); Assert.Empty(row[5]); Assert.StartsWith("' \n=", row[11]);
        Assert.Equal("Approved exact optional note", row[13]); Assert.Contains("unknown_currency_precision", Text(zip, "accountant-review.csv"));
    }

    [Fact]
    public async Task Oversized_stored_document_is_refused_before_allocating_its_export_blob()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var attachment = await db.Attachments.FirstAsync(a => a.EntryId == seed.Expense.Id, Ct);
            attachment.Data = new byte[EntryAttachment.MaxBytes + 1]; await db.SaveChangesAsync(Ct);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store(f).ReviewAsync(new(2026, [seed.First.Id], false, true), () => { }, Ct));
        // Excluding documents is a real disclosure choice, not a silent partial-document packet.
        var review = await Store(f).ReviewAsync(new(2026, [seed.First.Id], false, false), () => { }, Ct); Assert.Equal(0, review.Documents);
    }

    [Fact]
    public async Task Future_year_and_missing_selected_account_are_refused_without_creating_financial_rows()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Store(f).ReviewAsync(new(2027, [Guid.NewGuid()], false, false), () => { }, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store(f).ReviewAsync(new(2026, [Guid.NewGuid()], false, false), () => { }, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
