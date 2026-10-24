using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Exercises the actual benchmark comparator against original migrated SQLite and backup sources.</summary>
public sealed class WorkloadSnapshotVerifierTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() { SqliteTestPools.Clear(directory); directory.Dispose(); }

    [Theory, Trait("AT", "AT-82")]
    [InlineData("round-trip", true)]
    [InlineData("read-only", true)]
    [InlineData("unaccepted-write", false)]
    [InlineData("missing-write", false)]
    [InlineData("financial-edit", false)]
    [InlineData("schema-edit", false)]
    [InlineData("header-edit", false)]
    [InlineData("invalid-revision", false)]
    public async Task Only_an_explicit_round_trip_revision_change_is_allowed(string scenario, bool accepted)
    {
        var folder = directory.Combine("original");
        await PerformanceFixture.CreateAsync(folder, new(10, 2, 1), Ct);
        using var services = new ServiceCollection().AddZananceData(Path.Combine(folder, "fixture.db")).BuildServiceProvider();
        var source = services.GetRequiredService<IBackupSource>();
        using var beforeBytes = new MemoryStream();
        await source.WriteAsync(beforeBytes, Ct); beforeBytes.Position = 0;
        await using var before = await SqliteMemorySnapshot.OpenAsync(beforeBytes, 16 * 1024 * 1024, Ct);
        var writes = scenario is not ("read-only" or "missing-write");
        if (writes)
        {
            var store = services.GetRequiredService<ZananceStore>();
            var entry = new LedgerEntry { AccountId = PerformanceFixture.Id(1, 0), Amount = 1234,
                Date = PerformanceFixture.Today, Kind = EntryKind.Expense, Title = "Independent round-trip" };
            Assert.True((await store.SaveEntryAsync(entry, Ct)).Succeeded);
            Assert.Single(await store.DeleteEntryAsync(entry.Id, Ct));
        }
        await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var sql = scenario switch
            {
                "financial-edit" => "UPDATE Entries SET Title='Changed original row' WHERE Id=(SELECT Id FROM Entries LIMIT 1);",
                "schema-edit" => "CREATE INDEX IndependentUnexpectedIndex ON Entries(Title);",
                "header-edit" => "PRAGMA user_version=123;",
                "invalid-revision" => "UPDATE HomeFinancialRevision SET Revision='zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz';",
                _ => null
            };
            if (sql is not null) await db.Database.ExecuteSqlRawAsync(sql, Ct);
        }
        using var afterBytes = new MemoryStream();
        await source.WriteAsync(afterBytes, Ct); afterBytes.Position = 0;
        await using var after = await SqliteMemorySnapshot.OpenAsync(afterBytes, 16 * 1024 * 1024, Ct);
        var acceptedWrites = scenario != "read-only" && scenario != "unaccepted-write";
        if (accepted)
        {
            if (writes)
                await Assert.ThrowsAsync<InvalidDataException>(() => SqliteSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, Ct));
            await WorkloadSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, acceptedWrites, Ct);
        }
        else await Assert.ThrowsAsync<InvalidDataException>(() =>
            WorkloadSnapshotVerifier.VerifyAsync(before.Connection, after.Connection, acceptedWrites, Ct));
    }
}
