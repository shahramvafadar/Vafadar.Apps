using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SQLitePCL;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

/// <summary>Reads every persisted ledger field through typed code under the original EF connection and transaction.</summary>
internal static class CompleteHomeLedgerReader
{
    private static readonly ConditionalWeakTable<IModel, ReadContract> Contracts = new();
    private static readonly (string Name, Type Type)[] Columns =
    [
        (nameof(LedgerEntry.Id), typeof(Guid)),
        (nameof(LedgerEntry.AccountId), typeof(Guid)),
        (nameof(LedgerEntry.AggregatedFrom), typeof(DateOnly?)),
        (nameof(LedgerEntry.AggregatedTo), typeof(DateOnly?)),
        (nameof(LedgerEntry.Amount), typeof(long)),
        (nameof(LedgerEntry.CategoryId), typeof(Guid?)),
        (nameof(LedgerEntry.CreatedAt), typeof(DateTimeOffset)),
        (nameof(LedgerEntry.Date), typeof(DateOnly)),
        (nameof(LedgerEntry.Direction), typeof(AdjustmentDirection?)),
        (nameof(LedgerEntry.GroupId), typeof(Guid?)),
        (nameof(LedgerEntry.Icon), typeof(string)),
        (nameof(LedgerEntry.ImportBatchId), typeof(Guid?)),
        (nameof(LedgerEntry.IsAggregated), typeof(bool)),
        (nameof(LedgerEntry.IsPartialPayment), typeof(bool)),
        (nameof(LedgerEntry.Kind), typeof(EntryKind)),
        (nameof(LedgerEntry.Note), typeof(string)),
        (nameof(LedgerEntry.OccurrenceDate), typeof(DateOnly?)),
        (nameof(LedgerEntry.OriginalAmount), typeof(long?)),
        (nameof(LedgerEntry.OriginalCurrencyCode), typeof(string)),
        (nameof(LedgerEntry.Payee), typeof(string)),
        (nameof(LedgerEntry.RefundOfId), typeof(Guid?)),
        (nameof(LedgerEntry.ReimbursableAmount), typeof(long?)),
        (nameof(LedgerEntry.ReimbursedBy), typeof(string)),
        (nameof(LedgerEntry.ReimbursementDueDate), typeof(DateOnly?)),
        (nameof(LedgerEntry.Review), typeof(ReviewState)),
        (nameof(LedgerEntry.ScheduleId), typeof(Guid?)),
        (nameof(LedgerEntry.Source), typeof(EntrySource)),
        (nameof(LedgerEntry.Tags), typeof(List<string>)),
        (nameof(LedgerEntry.Title), typeof(string)),
        (nameof(LedgerEntry.ToAccountId), typeof(Guid?)),
        (nameof(LedgerEntry.ToAmount), typeof(long?)),
        (nameof(LedgerEntry.UpdatedAt), typeof(DateTimeOffset))
    ];
    // Derive both projections from the same guarded field order. The extra value is only a null bitmap;
    // every original financial value still travels through its complete original typed conversion.
    private static readonly string Sql = "SELECT " + string.Join(", ", Columns.Select(column => $"\"{column.Name}\""))
        + ", " + string.Join(" | ", Columns.Select((column, ordinal) =>
            $"((\"{column.Name}\" IS NULL) << {ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)})"))
        + " AS \"__OriginalNulls\" FROM \"Entries\" NOT INDEXED ORDER BY rowid DESC";

    /// <summary>Retains complete no-tracking rows, command interception, cancellation and the caller's captured transaction.</summary>
    internal static async Task<List<LedgerEntry>> ReadAsync(ZananceDbContext db, CancellationToken ct)
    {
        // D-305: execute through EF's relational command path, retaining its timeout, diagnostics,
        // interceptors and joined transaction while replacing only the per-row materialization.
        var contract = Contracts.GetValue(db.Model, CreateContract);
        var command = db.GetService<IRelationalCommandBuilderFactory>().Create().Append(Sql, sensitive: false).Build();
        var parameters = new RelationalCommandParameterObject(db.GetService<IRelationalConnection>(), null, null,
            db, db.GetService<IRelationalCommandDiagnosticsLogger>(), CommandSource.FromSqlQuery);
        await using var result = await command.ExecuteReaderAsync(parameters, ct);
        var reader = result.DbDataReader;
        var entries = new List<LedgerEntry>();
        var statement = reader.GetType() == typeof(SqliteDataReader) ? ((SqliteDataReader)reader).Handle : null;
        // Interceptors can rewrite the command or replace even a concrete SQLite reader. Trust the
        // derived bitmap only when its actual prepared statement is our exact complete projection.
        if (statement is not null && (Columns.Length > 32 || reader.FieldCount != Columns.Length + 1
            || !string.Equals(raw.sqlite3_sql(statement).utf8_to_string()?.Trim(), Sql, StringComparison.Ordinal)))
            statement = null;
        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            var row = new CompleteHomeLedgerRow(reader, statement, Columns.Length);
            entries.Add(new LedgerEntry(row.GetGuid(0))
            {
                AccountId = row.GetGuid(1),
                AggregatedFrom = row.IsDBNull(2) ? null : row.GetFieldValue<DateOnly>(2),
                AggregatedTo = row.IsDBNull(3) ? null : row.GetFieldValue<DateOnly>(3),
                Amount = row.GetInt64(4),
                CategoryId = row.IsDBNull(5) ? null : row.GetGuid(5),
                CreatedAt = (DateTimeOffset)contract.CreatedAt.ConvertFromProvider(row.GetInt64(6))!,
                Date = row.GetFieldValue<DateOnly>(7),
                Direction = row.IsDBNull(8) ? null : (AdjustmentDirection)row.GetInt32(8),
                GroupId = row.IsDBNull(9) ? null : row.GetGuid(9),
                Icon = row.IsDBNull(10) ? null : row.GetString(10),
                ImportBatchId = row.IsDBNull(11) ? null : row.GetGuid(11),
                IsAggregated = row.GetBoolean(12),
                IsPartialPayment = row.GetBoolean(13),
                Kind = (EntryKind)row.GetInt32(14),
                Note = row.IsDBNull(15) ? null : row.GetString(15),
                OccurrenceDate = row.IsDBNull(16) ? null : row.GetFieldValue<DateOnly>(16),
                OriginalAmount = row.IsDBNull(17) ? null : row.GetInt64(17),
                OriginalCurrencyCode = row.IsDBNull(18) ? null : row.GetString(18),
                Payee = row.IsDBNull(19) ? null : row.GetString(19),
                RefundOfId = row.IsDBNull(20) ? null : row.GetGuid(20),
                ReimbursableAmount = row.IsDBNull(21) ? null : row.GetInt64(21),
                ReimbursedBy = row.IsDBNull(22) ? null : row.GetString(22),
                ReimbursementDueDate = row.IsDBNull(23) ? null : row.GetFieldValue<DateOnly>(23),
                Review = (ReviewState)row.GetInt32(24),
                ScheduleId = row.IsDBNull(25) ? null : row.GetGuid(25),
                Source = (EntrySource)row.GetInt32(26),
                Tags = contract.ReadTags(row.GetString(27)),
                Title = row.IsDBNull(28) ? null : row.GetString(28),
                ToAccountId = row.IsDBNull(29) ? null : row.GetGuid(29),
                ToAmount = row.IsDBNull(30) ? null : row.GetInt64(30),
                UpdatedAt = (DateTimeOffset)contract.UpdatedAt.ConvertFromProvider(row.GetInt64(31))!,
            });
        }
        return entries;
    }

    /// <summary>Refuses model drift rather than silently losing fields, conversions or future query filters.</summary>
    private static ReadContract CreateContract(IModel model)
    {
        var entity = model.FindEntityType(typeof(LedgerEntry))
            ?? throw new InvalidOperationException("The complete ledger mapping is unavailable.");
        var table = StoreObjectIdentifier.Table("Entries", null);
        if (entity.GetTableName() != "Entries" || entity.BaseType is not null || entity.GetSchema() is not null
            || entity.GetProperties().Count() != Columns.Length
            || entity.GetDeclaredQueryFilters().Any()
            || Columns.Any(column => entity.FindProperty(column.Name) is not { } property
                || property.ClrType != column.Type || property.GetColumnName(table) != column.Name
                || !HasOriginalScalarConversion(property)))
            throw new InvalidOperationException("The complete Home ledger reader must be refreshed for the current mapping.");
        var tags = entity.FindProperty(nameof(LedgerEntry.Tags))!.GetTypeMapping();
        Func<string, List<string>> readTags;
        if (tags.Converter is { } converter)
            readTags = json => (List<string>)converter.ConvertFromProvider(json)!;
        else if (tags.JsonValueReaderWriter is { } jsonReader)
            readTags = json => (List<string>)jsonReader.FromJsonString(json)!;
        else throw new InvalidOperationException("The original ledger tag conversion is unavailable.");
        var created = entity.FindProperty(nameof(LedgerEntry.CreatedAt))!.GetTypeMapping().Converter
            ?? throw new InvalidOperationException("The original creation timestamp conversion is unavailable.");
        var updated = entity.FindProperty(nameof(LedgerEntry.UpdatedAt))!.GetTypeMapping().Converter
            ?? throw new InvalidOperationException("The original update timestamp conversion is unavailable.");
        return new(readTags, created, updated);
    }

    /// <summary>Rejects a new scalar conversion instead of interpreting it with obsolete provider getters.</summary>
    private static bool HasOriginalScalarConversion(IProperty property)
    {
        if (property.Name is nameof(LedgerEntry.Tags))
            return property.GetTypeMapping().Converter is not { } converter || converter.ProviderClrType == typeof(string);
        if (property.Name is nameof(LedgerEntry.CreatedAt) or nameof(LedgerEntry.UpdatedAt))
            return property.GetTypeMapping().Converter?.ProviderClrType == typeof(long);
        if (property.GetValueConverter() is not null) return false;
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return type.IsEnum ? property.GetTypeMapping().Converter?.ProviderClrType == typeof(int)
            : property.GetTypeMapping().Converter is null;
    }

    /// <summary>Original model-owned conversions, retained only as long as their immutable model.</summary>
    private sealed record ReadContract(Func<string, List<string>> ReadTags, ValueConverter CreatedAt, ValueConverter UpdatedAt);
}
