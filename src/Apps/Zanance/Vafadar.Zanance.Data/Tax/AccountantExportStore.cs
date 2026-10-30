using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tax;

/// <summary>Coherent read-only accountant packets, independently bound to the original file and consent.</summary>
public sealed class AccountantExportStore(IDbContextFactory<ZananceDbContext> factory,
    ICommercialWriteAccessSource source, TimeProvider time)
{
    /// <summary>Technical output bound, independent of paid resource quotas.</summary>
    public const long MaxArchiveBytes = 128 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    /// <summary>Lists actual archived and active identities without opening unrelated service/private metadata.</summary>
    public async Task<IReadOnlyList<AccountantAccount>> AccountsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Accounts.AsNoTracking().OrderBy(a => a.Name)
            .Select(a => new AccountantAccount(a.Id, a.Name, a.CurrencyCode, a.IsArchived)).ToListAsync(ct);
    }

    /// <summary>Reads original disclosure choices under one file-bound worker transaction, before any review or native output.</summary>
    public async Task<AccountantWorkspaceSnapshot> ReadWorkspaceAsync(CancellationToken ct = default, Func<string, bool>? acceptsFile = null)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original disclosure file changed.");
        var access = new CommercialFileAccess(source, path); access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var accounts = await ReadAccountsAsync(db, ct);
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original disclosure file changed.");
            return new AccountantWorkspaceSnapshot(path, access, accounts);
        }, ct);
    }
    private static async Task<IReadOnlyList<AccountantAccount>> ReadAccountsAsync(ZananceDbContext db, CancellationToken ct) =>
        await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ThenBy(a => a.Id)
            .Select(a => new AccountantAccount(a.Id, a.Name, a.CurrencyCode, a.IsArchived)).ToArrayAsync(ct);

    /// <summary>Reviews exact selected sources without writing a readable cache file.</summary>
    public async Task<AccountantExportReview> ReviewAsync(AccountantExportRequest request, Action validateScope,
        CancellationToken ct = default, AccountantWorkspaceSnapshot? originalWorkspace = null)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(validateScope);
        if (request.Year > time.GetLocalNow().Year) throw new ArgumentOutOfRangeException(nameof(request));
        await using var db = await factory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        originalWorkspace?.RequireFile(path);
        var access = new CommercialFileAccess(source, path);
        void Validate() { ct.ThrowIfCancellationRequested(); validateScope(); access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.AccountantExport); }
        Validate();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        if (originalWorkspace is not null) originalWorkspace.RequireAccounts(await ReadAccountsAsync(db, ct));
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var result = await ScanAsync(db, request, null, Validate, ct);
        Validate();
        return new(request, path, access, result.Digest, result.Entries, result.Documents, result.Bytes, result.Linked, day);
    }

    /// <summary>Revalidates file, rights, day and all disclosed bytes after native awaits, without writing.</summary>
    public Task ValidateAsync(AccountantExportReview review, Action validateScope, CancellationToken ct = default)
        => BuildCoreAsync(review, null, validateScope, ct);

    /// <summary>Streams a ZIP from one coherent reader; a changed review fails before native publication.</summary>
    public Task BuildAsync(AccountantExportReview review, Stream destination, Action validateScope, CancellationToken ct = default)
        => BuildCoreAsync(review, destination, validateScope, ct);

    private async Task BuildCoreAsync(AccountantExportReview review, Stream? destination, Action validateScope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(validateScope);
        void Validate()
        {
            ct.ThrowIfCancellationRequested(); validateScope(); review.Access.EnsureSnapshotCurrent();
            review.Access.DemandFeature(CommercialFeature.AccountantExport);
            if (review.Day != DateOnly.FromDateTime(time.GetLocalNow().DateTime)) throw new InvalidOperationException("Review the current day again.");
        }
        Validate();
        await using var db = await factory.CreateDbContextAsync(ct);
        if (DatabaseFileIdentity.ConfiguredPath(db) != review.Path) throw new InvalidOperationException("The reviewed profile changed.");
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        using var bounded = destination is null ? null : new BoundedOutput(destination);
        using var archive = bounded is null ? null : new ZipArchive(bounded, ZipArchiveMode.Create, leaveOpen: true);
        var current = await ScanAsync(db, review.Request, archive, Validate, ct);
        if (current.Digest != review.Digest) throw new InvalidOperationException("The source changed; review it again.");
        if (archive is not null)
        {
            using var manifest = archive.CreateEntry("manifest.json").Open();
            using var json = new Utf8JsonWriter(manifest, new JsonWriterOptions { Indented = true });
            json.WriteStartObject(); json.WriteString("format", "zanance-accountant-data/1"); json.WriteString("country", "DE");
            json.WriteNumber("calendarYear", review.Request.Year); json.WriteString("createdUtc", time.GetUtcNow());
            json.WriteBoolean("includesNotesAndPayees", review.Request.IncludeNotes);
            json.WriteBoolean("includesStoredDocumentCopies", review.Request.IncludeDocuments);
            json.WriteStartArray("selectedAccountIds"); foreach (var id in review.Request.AccountIds) json.WriteStringValue(id); json.WriteEndArray();
            json.WriteNumber("ledgerRows", current.Entries); json.WriteNumber("storedDocumentCopies", current.Documents);
            json.WriteStartObject("sha256"); foreach (var file in current.Files) json.WriteString(file.Key, file.Value); json.WriteEndObject();
            json.WriteEndObject();
        }
        Validate();
    }

    private async Task<ScanResult> ScanAsync(ZananceDbContext db, AccountantExportRequest request,
        ZipArchive? archive, Action validate, CancellationToken ct)
    {
        var selected = request.AccountIds.ToArray();
        var first = new DateOnly(request.Year, 1, 1); var end = first.AddYears(1);
        var entries = db.Entries.AsNoTracking().Where(e => e.Date >= first && e.Date < end &&
            (selected.Contains(e.AccountId) || (e.Kind == EntryKind.Transfer && e.ToAccountId != null && selected.Contains(e.ToAccountId.Value))));
        var accounts = await db.Accounts.AsNoTracking().Where(a => selected.Contains(a.Id) || entries.Any(e => e.AccountId == a.Id || e.ToAccountId == a.Id))
            .OrderBy(a => a.Id).ToListAsync(ct);
        if (accounts.Count > 10000 || selected.Any(id => accounts.All(a => a.Id != id))) throw new InvalidOperationException("The selected accounts changed or exceed the packet bound.");
        var lookup = accounts.ToDictionary(a => a.Id);
        var categories = await db.Categories.AsNoTracking().Where(c => entries.Any(e => e.CategoryId == c.Id)).OrderBy(c => c.Id).ToListAsync(ct);
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        long textBytes = 0, documentBytes = 0; int count = 0, documentCount = 0;
        var totals = new SortedDictionary<string, (int Count, BigInteger Minor)>(StringComparer.Ordinal);
        void Check() { validate(); if (textBytes > 512L * 1024 * 1024 || documentBytes > MaxArchiveBytes) throw new InvalidOperationException("The packet exceeds its safe output bound."); }
        PacketFile Open(string name) => new(name, archive, files, size => { textBytes += size; Check(); });
        using (var file = Open("accounts.csv"))
        {
            file.Row("id", "name", "currency", "type", "archived", "explicitly_selected");
            foreach (var a in accounts) file.Row(a.Id, Safe(a.Name), a.CurrencyCode, a.Type, a.IsArchived, selected.Contains(a.Id));
        }
        using (var file = Open("categories.csv"))
        {
            file.Row("id", "name", "system_key", "parent_id", "kind", "tax_classification");
            foreach (var c in categories) file.Row(c.Id, Safe(c.Name), Safe(c.SystemKey), c.ParentId, c.Kind, "");
        }
        using (var file = Open("transactions.csv"))
        {
            file.Row("id", "recorded_date", "kind", "amount_minor", "currency", "amount_decimal", "account_id", "to_account_id", "to_amount_minor", "to_currency",
                "category_id", "title", "payee", "note", "original_amount_minor", "original_currency", "refund_of", "group_id", "review", "source", "direction",
                "reimbursable_minor", "reimbursed_by", "reimbursement_due", "aggregated", "aggregated_from", "aggregated_to", "tags", "created_utc", "updated_utc");
            await foreach (var e in entries.OrderBy(e => e.Id).AsAsyncEnumerable().WithCancellation(ct))
            {
                Check(); count++;
                if (!lookup.TryGetValue(e.AccountId, out var account)) throw new InvalidOperationException("An entry has no source account.");
                var currency = account.CurrencyCode;
                var destination = e.ToAccountId is { } to && lookup.TryGetValue(to, out var target) ? target : null;
                file.Row(e.Id, e.Date, e.Kind, e.Amount, currency, DecimalAmount(e.Amount, currency), e.AccountId, e.ToAccountId,
                    e.Kind == EntryKind.Transfer ? e.ToAmount ?? e.Amount : null, destination?.CurrencyCode,
                    e.CategoryId, Safe(e.Title), request.IncludeNotes ? Safe(e.Payee) : "", request.IncludeNotes ? Safe(e.Note) : "",
                    e.OriginalAmount, e.OriginalCurrencyCode, e.RefundOfId, e.GroupId, e.Review, e.Source, e.Direction,
                    e.ReimbursableAmount, request.IncludeNotes ? Safe(e.ReimbursedBy) : "", e.ReimbursementDueDate, e.IsAggregated,
                    e.AggregatedFrom, e.AggregatedTo, Safe(EntryTags.Format(e.Tags)), e.CreatedAt, e.UpdatedAt);
                var key = currency + "/" + e.Kind;
                var total = totals.GetValueOrDefault(key); totals[key] = (total.Count + 1, total.Minor + e.Amount);
            }
        }
        using (var file = Open("accountant-review.csv"))
        {
            file.Row("entry_id", "review_flags", "tax_category_to_complete", "business_share_to_confirm", "vat_treatment_to_confirm", "accountant_comment");
            await foreach (var e in entries.OrderBy(e => e.Id).AsAsyncEnumerable().WithCancellation(ct))
            {
                Check(); var flags = new List<string> { "tax_treatment_not_assessed" };
                if (e.Review != ReviewState.Confirmed) flags.Add("unreviewed_record");
                if (e.IsAggregated) flags.Add("aggregate_period_requires_review");
                if (e.RefundOfId is not null) flags.Add("refund_reference_requires_review");
                if (e.GroupId is not null) flags.Add("related_group_requires_review");
                if (e.Date > DateOnly.FromDateTime(time.GetLocalNow().DateTime)) flags.Add("future_dated_stored_record");
                if (!Currencies.TryGet(lookup[e.AccountId].CurrencyCode, out _)) flags.Add("unknown_currency_precision");
                file.Row(e.Id, string.Join(';', flags), "", "", "", "");
            }
        }
        using (var file = Open("recorded-totals.csv"))
        {
            file.Row("currency_and_recorded_kind", "row_count", "source_amount_minor_sum", "meaning");
            foreach (var pair in totals) file.Row(pair.Key, pair.Value.Count, pair.Value.Minor, "Recorded source amounts only; not taxable income, profit or a converted total.");
        }
        if (request.IncludeDocuments)
        {
            var documents = db.Attachments.AsNoTracking().Where(a => entries.Any(e => e.Id == a.EntryId));
            if (await documents.AnyAsync(a => a.Data.Length > EntryAttachment.MaxBytes, ct)) throw new InvalidOperationException("A stored document exceeds its allowed size.");
            await foreach (var a in documents.OrderBy(a => a.Id).AsAsyncEnumerable().WithCancellation(ct))
            {
                documentCount++; documentBytes += a.Data.LongLength; Check();
                if (documentCount > 10000) throw new InvalidOperationException("Too many stored document copies for one packet.");
                var extension = a.ContentType switch { "application/pdf" => ".pdf", "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".bin" };
                var name = "documents/" + a.Id.ToString("N") + extension;
                var hash = Convert.ToHexStringLower(SHA256.HashData(a.Data)); files.Add(name, hash);
                if (archive is not null)
                {
                    // Stored copies stay byte-exact; filenames never become archive paths or executable suffixes.
                    using var output = archive.CreateEntry(name, CompressionLevel.Fastest).Open();
                    for (var offset = 0; offset < a.Data.Length; offset += 65536)
                    { Check(); await output.WriteAsync(a.Data.AsMemory(offset, Math.Min(65536, a.Data.Length - offset)), ct); }
                }
            }
            using var index = Open("documents.csv");
            index.Row("attachment_id", "entry_id", "stored_file_name", "stored_media_type", "archive_path", "bytes", "sha256", "created_utc", "updated_utc", "status");
            // A second metadata-only enumeration avoids retaining document names or blobs while ZIP members are open.
            await foreach (var a in documents.OrderBy(a => a.Id).Select(a => new { a.Id, a.EntryId, a.FileName, a.ContentType, Size = a.Data.Length, a.CreatedAt, a.UpdatedAt }).AsAsyncEnumerable().WithCancellation(ct))
            {
                Check();
                var extension = a.ContentType switch { "application/pdf" => ".pdf", "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".bin" };
                var name = "documents/" + a.Id.ToString("N") + extension;
                index.Row(a.Id, a.EntryId, Safe(a.FileName), Safe(a.ContentType), name, a.Size, files[name], a.CreatedAt, a.UpdatedAt, "stored_copy_not_verified_original");
            }
        }
        using (var file = Open("README.txt")) file.Text(Instructions);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Utf8.GetBytes(string.Join('\n', files.Select(p => p.Key + ":" + p.Value)))));
        Check(); return new(digest, count, documentCount, documentBytes, accounts.Count(a => !selected.Contains(a.Id)), files);
    }

    private static string Safe(string? value)
    {
        if (value?.Length > 65536) throw new InvalidOperationException("A source field exceeds the safe export bound.");
        var safe = Csv.Text(value);
        var trimmed = value.AsSpan().TrimStart();
        return safe == value && trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' ? "'" + value : safe;
    }
    private static string DecimalAmount(long value, string currency) => Currencies.TryGet(currency, out var known)
        ? MoneyAmount.ToDecimal(value, known).ToString("F" + known.MinorDigits, CultureInfo.InvariantCulture) : "";

    private const string Instructions = """
        Zanance accountant data packet / Germany / version 1
        This is a readable, unencrypted ZIP. Protect all copies and select the recipient yourself.
        CSV headers and these instructions are English; original user text is preserved in UTF-8.
        Scope: stored ledger dates in the selected Gregorian calendar year and explicit accounts.
        A transfer touching either selected account stays ONE row with both account identities and currencies.
        Linked counterpart account names may therefore be included; their unrelated history and opening balances are not.
        All amounts ending _minor are exact integer minor units. Decimal amount is blank for unknown precision.
        Recorded totals separate currency and kind; transfers, loans, adjustments and asset trades are NOT tax profit.
        Refunds and income reversals retain their recorded kind and original references. References/groups may lie outside this packet.
        Categories, tags, review flags and dates do NOT determine deductibility, VAT, business share or recognition year.
        No inferred tax classification, exchange conversion, depreciation, accrual, EÜR, DATEV or ELSTER filing is produced.
        Accountant-review columns are intentionally blank. An employee or freelancer must review completeness with their accountant.
        Plans, estimates, unposted obligations, profile settings, device keys, passwords and private service history are excluded.
        Documents, when explicitly included, are exact STORED COPIES, not verified legal originals. Photos may have been compressed.
        Keep original invoices and any original structured electronic invoice data separately. This is not a GoBD archive.
        Files have SHA-256 integrity hashes, not signatures or authenticity certification. manifest.json does not hash itself.
        CSV text beginning with spreadsheet formula markers is prefixed with an apostrophe for safe opening.
        This packet is not a Zanance backup and cannot restore the app. Retain a separate portable Zanance export.
        No tax submission or automatic payment is performed. Other country/rule packages are not implied by display settings.
        """;

    private sealed record ScanResult(string Digest, int Entries, int Documents, long Bytes, int Linked, SortedDictionary<string, string> Files);

    /// <summary>Streams each text member with a bounded working row and its actual byte hash.</summary>
    private sealed class PacketFile(string name, ZipArchive? archive, IDictionary<string, string> hashes, Action<int> count) : IDisposable
    {
        private readonly Stream? _output = archive?.CreateEntry(name, CompressionLevel.Fastest).Open();
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        public void Row(params object?[] values) => Text(Csv.Write([values.Select(v => v switch
        {
            null => "", string text => Safe(text), DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeOffset date => date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false", IFormattable f => f.ToString(null, CultureInfo.InvariantCulture), _ => v.ToString() ?? ""
        }).ToArray()]));
        public void Text(string text)
        {
            var bytes = Utf8.GetBytes(text); count(bytes.Length); _hash.AppendData(bytes); _output?.Write(bytes);
        }
        public void Dispose() { _output?.Dispose(); hashes.Add(name, Convert.ToHexStringLower(_hash.GetHashAndReset())); _hash.Dispose(); }
    }

    /// <summary>Prevents ZIP growth beyond the cache budget, including central-directory bytes.</summary>
    private sealed class BoundedOutput(Stream destination) : Stream
    {
        private long _written;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _written;
        public override long Position { get => _written; set => throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        { if (buffer.Length > MaxArchiveBytes - _written) throw new InvalidOperationException("The archive exceeds its safe output bound."); destination.Write(buffer); _written += buffer.Length; }
        public override void Flush() => destination.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
