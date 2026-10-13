using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Creates owned first-schema financial fixtures through the actual migrations for native lifecycle checks.</summary>
internal static class OriginalProfileFixture
{
    /// <summary>Seeds fictitious first-schema rows, then applies the actual complete migration baseline.</summary>
    internal static async Task CreateOriginalAsync(string original)
    {
        // Actual original registration and compiled model (D-44), not a duplicate financial schema or mapper.
        _ = new ServiceCollection().AddZananceData(original);
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(new SqliteConnectionStringBuilder
        { DataSource = original, Pooling = false }.ToString()).UseModel(ZananceDbContextModel.Instance).Options;
        await using var db = new ZananceDbContext(options);
        var migrations = db.Database.GetMigrations().ToList();
        if (migrations.Count != 32) throw new IOException("Review the actual migration fixture baseline.");
        await db.GetService<IMigrator>().MigrateAsync(migrations[0]);
        var account = Guid.NewGuid(); var entry = Guid.NewGuid(); var settings = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"INSERT INTO Settings (Id, ReportCurrencyCode, Mode, BudgetCalendar, WeekStart, ReminderDaysBefore, ReminderTime, NotificationsShowDetails, AppLockEnabled, OnboardingCompleted, CreatedAt, UpdatedAt) VALUES ({settings}, 'EUR', 0, 0, 1, 3, '09:00:00', 0, 0, 1, 0, 0)");
        await db.Database.ExecuteSqlAsync($"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({account}, 'Fictitious بانک Ü', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)");
        await db.Database.ExecuteSqlAsync($"INSERT INTO Entries (Id, Kind, Date, AccountId, Amount, Review, Source, CreatedAt, UpdatedAt) VALUES ({entry}, 1, '2026-02-01', {account}, 250, 0, 0, 0, 0)");
        await db.Database.MigrateAsync();
        await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "CREATE TABLE fictitious_packets(amount INTEGER, label TEXT, bytes BLOB); INSERT INTO fictitious_packets VALUES(-9223372036854775808,'Unicode بانک Ü',zeroblob(2097152)); PRAGMA user_version=73; PRAGMA application_id=123456";
        await command.ExecuteNonQueryAsync();
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(original)!, "unrelated.txt"), "Keep this fictitious unrelated file");
    }

}
