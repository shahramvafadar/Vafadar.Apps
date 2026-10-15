using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

var root = Directory.GetCurrentDirectory();
if (!OperatingSystem.IsWindows() || !File.Exists(Path.Combine(root, "AGENTS.md")) ||
    (!Directory.Exists(Path.Combine(root, ".git")) && !File.Exists(Path.Combine(root, ".git"))))
    throw new InvalidOperationException("Run from the repository root.");
// Reuse the already qualified native package for metadata; no legacy bundle or application file is loaded.
SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
// Metadata only: this context is never opened, migrated or pointed at a financial file.
using var context = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite("Data Source=:memory:").Options);
var model = context.GetService<IDesignTimeModel>().Model.GetRelationalModel();
var excluded = new HashSet<string>(["ImportLinks", "ResourceSelections"], StringComparer.Ordinal);
var settings = new HashSet<string>(["ReportCurrencyCode", "DefaultCurrencyCode", "ValuationCurrencyEnabled",
    "RateFreshnessDays", "HomeBudgetCurrencyCode", "DisplayUnits", "HomeLayout", "EssentialEstimate",
    "EssentialEstimatePeriod", "EssentialEstimateCurrency", "ReviewProgress", "DefaultAccountId", "Mode",
    "BudgetCalendar", "MonthStartDay", "FortnightStart"], StringComparer.Ordinal);
var tables = new List<SyncTable>();
foreach (var table in model.Tables.OrderBy(value => value.Name, StringComparer.Ordinal))
{
    if (excluded.Contains(table.Name) || table.Name.StartsWith("Sync", StringComparison.Ordinal)) continue;
    var singleton = table.Name == "Settings";
    var columns = table.Columns.Where(column => singleton ? settings.Contains(column.Name) : column.Name != "UpdatedAt")
        .OrderBy(column => column.Name, StringComparer.Ordinal).Select(column =>
        {
            var property = column.PropertyMappings.First().Property;
            var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            var element = property.GetElementType()?.ClrType;
            var kind = element is not null ? element == typeof(Guid) ? "json-guid" : element == typeof(string) ? "json-string"
                : element == typeof(long) || element == typeof(int) ? "json-integer" : throw new InvalidOperationException("Unreviewed primitive collection element.")
                : type == typeof(Guid) ? "guid" : type == typeof(DateOnly) ? "date" : type == typeof(TimeOnly) ? "time"
                : type == typeof(DateTimeOffset) ? "ticks" : type == typeof(decimal) ? "decimal"
                : column.StoreType.ToUpperInvariant() switch { "INTEGER" => "integer", "REAL" => "real", "BLOB" => "blob", "TEXT" => "text", _ => throw new InvalidOperationException("Unknown stored scalar.") };
            var scalar = type.IsEnum ? Enum.GetUnderlyingType(type) : element ?? type;
            var (minimum, maximum) = scalar == typeof(int) ? ((long?)int.MinValue, (long?)int.MaxValue)
                : scalar == typeof(short) ? ((long?)short.MinValue, (long?)short.MaxValue)
                : scalar == typeof(byte) ? ((long?)byte.MinValue, (long?)byte.MaxValue) : (null, null);
            return new SyncColumn(column.Name, kind, column.IsNullable, property.GetMaxLength(),
                type == typeof(bool) ? [0, 1] : type.IsEnum ? Enum.GetValues(type).Cast<object>().Select(Convert.ToInt64).ToArray() : null,
                type.IsDefined(typeof(FlagsAttribute), false), minimum, maximum);
        }).ToArray();
    var allowed = columns.Select(column => column.Name).ToHashSet(StringComparer.Ordinal);
    var foreign = table.ForeignKeyConstraints.Where(key => key.Columns.All(column => allowed.Contains(column.Name)))
        .Select(key => new SyncForeignKey(key.Columns.Select(column => column.Name).ToArray(), key.PrincipalTable.Name,
            key.PrincipalColumns.Select(column => column.Name).ToArray())).ToArray();
    var unique = table.Indexes.Where(index => index.IsUnique).Select(index =>
    {
        var filter = index.MappedIndexes.First().GetFilter();
        if (filter is not null && filter != "\"ScheduleId\" IS NOT NULL AND \"IsPartialPayment\" = 0")
            throw new InvalidOperationException("A new conditional unique rule needs explicit protocol support.");
        return new SyncUniqueKey(index.Columns.Select(column => column.Name).ToArray(), filter is not null);
    }).ToArray();
    tables.Add(new(table.Name, singleton, columns,
        singleton ? [] : (table.PrimaryKey ?? throw new InvalidOperationException("Financial primary key required.")).Columns.Select(column => column.Name).ToArray(),
        foreign, unique));
}
var manifest = new SyncSchemaManifest(1, tables.ToArray());
var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, options);
var output = Path.Combine(root, "src/Apps/Zanance/Vafadar.Zanance.Core/Sync/schema-v1.json");
var migrations = Path.Combine(root, "src/Apps/Zanance/Vafadar.Zanance.Data/Migrations");
// Published migrations must never change when a later model gains a column. Require a new snapshot/migration instead.
RequireFrozen(Path.Combine(migrations, "PersonalSyncJournalUp.sql"), SyncTriggerSql.Create(manifest));
RequireFrozen(Path.Combine(migrations, "PersonalSyncJournalDown.sql"), SyncTriggerSql.Drop(manifest));
File.WriteAllBytes(output, bytes);
Console.WriteLine($"Financial contract: {tables.Count} tables, {tables.Sum(table => table.Columns.Length)} columns, canonical SHA-256 {SyncSchemaDigest.Compute(manifest)}.");

static void RequireFrozen(string path, string expected)
{
    if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != expected.Replace("\r\n", "\n"))
        throw new InvalidOperationException("Frozen capture SQL differs; review a new versioned migration instead of overwriting history.");
}
