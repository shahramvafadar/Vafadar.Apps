using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>A fixed, fictitious Q-02 workload. Tenfold scales all three dimensions, not only transactions.</summary>
public sealed record Workload(int Entries, int Accounts, int Schedules)
{
    /// <summary>The specification's reference workload.</summary>
    public static Workload Reference { get; } = new(10_000, 20, 100);

    /// <summary>A stress workload ten times the reference in every dimension.</summary>
    public static Workload Tenfold { get; } = new(100_000, 200, 1_000);

    /// <summary>Rejects unusable fixtures before any database is opened.</summary>
    public void Validate()
    {
        if (Entries < 10 || Accounts < 2 || Schedules < 1) { throw new ArgumentOutOfRangeException(nameof(Entries)); }
    }
}

/// <summary>Fresh owned directories only; never accepts an existing database or an app data directory.</summary>
public static class FixtureDirectory
{
    /// <summary>Creates a new leaf after rejecting existing paths and redirected ancestors.</summary>
    public static string Create(string requested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requested);
        var path = Path.GetFullPath(requested);
        if (File.Exists(path) || Directory.Exists(path)) { throw new IOException("The fixture directory must be new."); }
        var parent = new DirectoryInfo(Path.GetDirectoryName(path)!);
        if (!parent.Exists) { throw new DirectoryNotFoundException("The fixture parent must already exist."); }
        for (var ancestor = parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) { throw new IOException("Redirected fixture paths are not allowed."); }
        }
        Directory.CreateDirectory(path);
        using var marker = new StreamWriter(new FileStream(Path.Combine(path, "fictitious-performance.marker"), FileMode.CreateNew));
        marker.Write("Zanance Q-02 fictitious fixture v1\n");
        return path;
    }
}

/// <summary>Migration duration and legacy account/entry/settings preservation proof, excluding fixture generation.</summary>
public sealed record MigrationProof(double Milliseconds, string BeforeFingerprint, string AfterFingerprint);

/// <summary>Builds an old-schema fixture with prepared SQL, then uses the application's real migrations and stores.</summary>
public static class PerformanceFixture
{
    /// <summary>Fixed date independent of execution time; native measurements record their actual device date separately.</summary>
    public static DateOnly Today { get; } = new(2026, 10, 9);

    /// <summary>A reproducible id for fictitious rows; kinds occupy independent id ranges.</summary>
    public static Guid Id(int kind, int index) => new($"{kind:x8}-0000-0000-0000-{index:x12}");

    /// <summary>Creates legacy/current files in a new directory; any preservation failure aborts the run.</summary>
    public static async Task<MigrationProof> CreateAsync(string directory, Workload workload, CancellationToken ct)
    {
        workload.Validate();
        var folder = FixtureDirectory.Create(directory);
        var path = Path.Combine(folder, "fixture.db");
        // A concurrent writer must never turn the fresh-directory check into an existing-database migration.
        using (new FileStream(path, FileMode.CreateNew)) { }
        await using var services = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().First(), ct);
        }
        await SeedLegacyAsync(path, workload, ct);
        var before = await FingerprintAsync(path, ct);
        // VACUUM produces a standalone legacy fixture, with no dependence on WAL sidecars.
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite }.ToString()))
        {
            await connection.OpenAsync(ct);
            using var export = connection.CreateCommand();
            export.CommandText = "VACUUM INTO $destination";
            export.Parameters.AddWithValue("$destination", Path.Combine(folder, "legacy.db"));
            await export.ExecuteNonQueryAsync(ct);
        }
        var watch = Stopwatch.StartNew();
        services.MigrateLocalDatabase<ZananceDbContext>();
        watch.Stop();
        var after = await FingerprintAsync(path, ct);
        if (before != after) { throw new InvalidOperationException("Migration changed a legacy row."); }

        var store = services.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync(ct);
        await store.UpdateSettingsAsync(settings =>
        {
            settings.DefaultAccountId = Id(1, 0);
            settings.Mode = ExperienceMode.Advanced;
            settings.DefaultCurrencyCode = "EUR";
        }, ct);
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            db.Schedules.AddRange(Enumerable.Range(0, workload.Schedules).Select(i => new Schedule
            {
                Name = $"QA06 plan {i:D4}", AccountId = Id(1, i % workload.Accounts), Amount = 1_000,
                Rule = new RecurrenceRule { Frequency = i % 2 == 0 ? Frequency.Monthly : Frequency.Weekly,
                    Start = Today.AddDays(-60 - i % 28) },
                // Opening a benchmark profile must never post money or schedule notifications.
                AutoPost = false, ReminderEnabled = false,
            }));
            await db.SaveChangesAsync(ct);
            using var checkpoint = db.Database.GetDbConnection().CreateCommand();
            await db.Database.OpenConnectionAsync(ct);
            checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
            await checkpoint.ExecuteNonQueryAsync(ct);
        }
        return new(watch.Elapsed.TotalMilliseconds, before, after);
    }

    private static async Task SeedLegacyAsync(string path, Workload workload, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite }.ToString());
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();
        using var account = connection.CreateCommand();
        account.Transaction = transaction;
        account.CommandText = "INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ($id,$name,1,'EUR',100000,'2024-01-01',1,1,0,$order,0,0)";
        account.Parameters.AddWithValue("$id", ""); account.Parameters.AddWithValue("$name", ""); account.Parameters.AddWithValue("$order", 0);
        for (var i = 0; i < workload.Accounts; i++)
        {
            account.Parameters["$id"].Value = Id(1, i).ToString().ToUpperInvariant();
            account.Parameters["$name"].Value = $"QA06 account {i:D3}"; account.Parameters["$order"].Value = i;
            await account.ExecuteNonQueryAsync(ct);
        }
        using var entry = connection.CreateCommand();
        entry.Transaction = transaction;
        entry.CommandText = "INSERT INTO Entries (Id,Kind,Date,AccountId,Amount,ToAccountId,ToAmount,Title,Payee,Note,Review,Source,CreatedAt,UpdatedAt) VALUES ($id,$kind,$date,$account,$amount,$to,$toAmount,$title,'Fictitious shop','Q-02 generated data',0,0,$created,$created)";
        foreach (var name in new[] { "$id", "$kind", "$date", "$account", "$amount", "$to", "$toAmount", "$title", "$created" })
        {
            entry.Parameters.AddWithValue(name, "");
        }
        var random = new Random(42);
        for (var i = 0; i < workload.Entries; i++)
        {
            var kind = (i % 10) switch { 0 => EntryKind.Income, 1 => EntryKind.Refund, 2 => EntryKind.Transfer, _ => EntryKind.Expense };
            var owner = random.Next(workload.Accounts);
            var amount = random.Next(1, 50_001);
            var date = Today.AddDays(-random.Next(0, 1_000));
            entry.Parameters["$id"].Value = Id(2, i).ToString().ToUpperInvariant(); entry.Parameters["$kind"].Value = (int)kind;
            entry.Parameters["$date"].Value = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            entry.Parameters["$account"].Value = Id(1, owner).ToString().ToUpperInvariant(); entry.Parameters["$amount"].Value = amount;
            entry.Parameters["$to"].Value = kind == EntryKind.Transfer ? Id(1, (owner + 1) % workload.Accounts).ToString().ToUpperInvariant() : DBNull.Value;
            entry.Parameters["$toAmount"].Value = kind == EntryKind.Transfer ? amount : DBNull.Value;
            entry.Parameters["$title"].Value = $"QA06 item {i:D6}"; entry.Parameters["$created"].Value = i;
            await entry.ExecuteNonQueryAsync(ct);
        }
        using var settings = connection.CreateCommand();
        settings.Transaction = transaction;
        settings.CommandText = "INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ($id,'EUR',1,0,1,3,'09:00:00',0,0,1,0,0)";
        settings.Parameters.AddWithValue("$id", Id(4, 0).ToString().ToUpperInvariant());
        await settings.ExecuteNonQueryAsync(ct);
        transaction.Commit();
    }

    /// <summary>Hashes unchanged legacy fields in id order; the retired WeekStart column is deliberately excluded.</summary>
    public static async Task<string> FingerprintAsync(string path, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly }.ToString());
        await connection.OpenAsync(ct);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var sql in new[]
        {
            "SELECT Id,Name,Type,CurrencyCode,OpeningBalance,OpeningDate,OpeningBalanceKnown,Icon,IncludeInTotals,IsArchived,SortOrder,CreatedAt,UpdatedAt FROM Accounts ORDER BY Id",
            "SELECT Id,Kind,Date,AccountId,Amount,ToAccountId,ToAmount,Direction,CategoryId,Title,Payee,Note,Icon,Review,Source,OriginalAmount,OriginalCurrencyCode,RefundOfId,GroupId,ScheduleId,OccurrenceDate,ImportBatchId,CreatedAt,UpdatedAt FROM Entries ORDER BY Id",
            "SELECT Id,ReportCurrencyCode,DefaultAccountId,Mode,BudgetCalendar,ReminderDaysBefore,ReminderTime,NotificationsShowDetails,AppLockEnabled,OnboardingCompleted,CreatedAt,UpdatedAt FROM Settings ORDER BY Id",
        })
        {
            using var command = connection.CreateCommand(); command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "<null>" : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)!;
                    var bytes = Encoding.UTF8.GetBytes(value);
                    hash.AppendData(BitConverter.GetBytes(bytes.Length)); hash.AppendData(bytes);
                }
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
