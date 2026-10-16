using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Sync.Tests;

/// <summary>Actual immutable-file/PostgreSQL retirement isolation; synthetic opaque rows do not claim cryptographic or HTTP acceptance.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdRetirementIsolationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The changed inspection contract still merges the same valid immutable intent and preserves its original time.</summary>
    [Fact]
    public async Task Known_original_marker_transfers_unchanged_through_raw_inspection()
    {
        var root = FixtureRoot("valid-transfer"); var target = root + "-target";
        Directory.CreateDirectory(root); Directory.CreateDirectory(target);
        var ledger = Ledger(root); var id = Guid.NewGuid();
        var original = new HouseholdDeletionMarker(1, id, Guid.NewGuid(), new('a', 64), new('b', 64), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), HouseholdDeletionReason.Owner);
        await ledger.RecordAsync(original, Ct);
        Assert.Equal(1, await HouseholdDeletionTransfer.MergeAsync(root, target, Ct));
        Assert.Equal(original, await Ledger(target).ReadAsync(id, Ct));
        Assert.Equal(original, await ledger.ReadAsync(id, Ct));
    }

    /// <summary>Raw unknown names advance bounded discovery without becoming authority, while offline transfer still refuses them.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(41)]
    public async Task Unknown_names_do_not_pin_discovery_or_silently_weaken_offline_restore(int unknown)
    {
        var root = FixtureRoot("scan-" + unknown); Directory.CreateDirectory(root);
        var privateRoot = Path.Combine(root, "household-deletions"); Directory.CreateDirectory(privateRoot);
        var original = Encoding.UTF8.GetBytes("Fictitious unrecognized private entry; preserve exactly.");
        for (var index = 0; index < unknown; index++) await File.WriteAllBytesAsync(Path.Combine(privateRoot, "unknown-" + index + ".json"), original, Ct);
        var ledger = Ledger(root); var id = Guid.NewGuid();
        await ledger.RecordAsync(new(1, id, Guid.NewGuid(), new('a', 64), new('b', 64), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), HouseholdDeletionReason.Owner), Ct);
        var seen = new List<Guid>(); var scanned = 0;
        while (true)
        {
            var page = ledger.Inspect(scanned, 20); Assert.InRange(page.Scanned, 0, 20); seen.AddRange(page.Spaces); scanned += page.Scanned;
            if (page.Scanned < 20) break;
            Assert.True(scanned <= unknown + 1);
        }
        Assert.Equal(unknown + 1, scanned); Assert.Equal(id, Assert.Single(seen));
        var target = root + "-target"; Directory.CreateDirectory(target);
        await Assert.ThrowsAsync<IOException>(() => HouseholdDeletionTransfer.MergeAsync(root, target, Ct));
        for (var index = 0; index < unknown; index++) Assert.Equal(original, await File.ReadAllBytesAsync(Path.Combine(privateRoot, "unknown-" + index + ".json"), Ct));
    }

    /// <summary>Twenty invalid/missing/directory markers stay closed and unchanged while the next real expiry is eventually retired atomically.</summary>
    [Fact]
    public async Task Failed_first_page_cannot_starve_later_expiry_or_erase_unrelated_ciphertext()
    {
        var root = FixtureRoot("worker"); Directory.CreateDirectory(root);
        var connection = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB") ?? throw new InvalidOperationException("Explicit isolated marker-review PostgreSQL required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("zanance_identity_household_marker_isolation_d294", settings.Database);
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" });
        var ledger = Ledger(root); var factory = new Factory(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options, ledger);
        var now = DateTimeOffset.UtcNow; var bad = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray(); var due = Guid.NewGuid(); var control = Guid.NewGuid();
        var users = Enumerable.Range(0, 3).Select(_ => new ServiceUser { Id = Guid.NewGuid().ToString("N") }).ToArray();
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            await db.Database.MigrateAsync(Ct); Assert.False(db.Database.HasPendingModelChanges());
            Assert.False(await db.Set<HouseholdSpaceRow>().AnyAsync(Ct));
            db.Users.AddRange(users);
            foreach (var id in bad) db.Add(Room(id, users[0].Id, now.AddDays(-2), true, null));
            db.Add(Room(due, users[1].Id, now.AddDays(-1), false, now.AddDays(-121)));
            db.Add(Room(control, users[2].Id, now, false, null));
            foreach (var id in bad.Append(due).Append(control)) db.Add(new HouseholdRecordRow
                { SpaceId = id, RecordId = new('c', 64), Version = 1, KeyEpoch = 1, MembershipVersion = 1, DeviceId = Guid.NewGuid(), OperationId = Guid.NewGuid(),
                    Ciphertext = "fictitious-opaque-preservation-sentinel", Digest = new('d', 64), Bytes = 41 });
            await db.SaveChangesAsync(Ct);
        }
        var privateRoot = Path.Combine(root, "household-deletions"); Directory.CreateDirectory(privateRoot);
        var invalid = Encoding.UTF8.GetBytes("{fictitious-invalid-json");
        // Keep one missing marker and one directory-shaped marker among the same
        // first twenty original rows; neither may be invented or overwritten.
        Directory.CreateDirectory(Path.Combine(privateRoot, bad[0].ToString("N") + ".json"));
        foreach (var id in bad.Skip(2)) await File.WriteAllBytesAsync(Path.Combine(privateRoot, id.ToString("N") + ".json"), invalid, Ct);
        var worker = new HouseholdRetirement(factory, ledger, TimeProvider.System);
        await worker.RunOnceAsync(Ct);
        await using (var db = await factory.CreateDbContextAsync(Ct)) Assert.False((await db.Set<HouseholdSpaceRow>().SingleAsync(row => row.Id == due, Ct)).StorageRetired);
        // The same live worker/cursors continue, rather than restarting or renewing
        // any original intent. The due room was behind a full failed first page.
        await worker.RunOnceAsync(Ct);
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            var retired = await db.Set<HouseholdSpaceRow>().SingleAsync(row => row.Id == due, Ct);
            Assert.True(retired.Deleted && retired.StorageRetired); Assert.Empty(retired.OwnerSigningKey); Assert.Empty(retired.OwnerWrappingKey);
            Assert.False(await db.Set<HouseholdRecordRow>().AnyAsync(row => row.SpaceId == due, Ct));
            Assert.Equal(20, await db.Set<HouseholdSpaceRow>().CountAsync(row => bad.Contains(row.Id) && row.Deleted && !row.StorageRetired, Ct));
            Assert.Equal(21, await db.Set<HouseholdRecordRow>().CountAsync(Ct));
            Assert.Equal("fictitious-opaque-preservation-sentinel", (await db.Set<HouseholdRecordRow>().SingleAsync(row => row.SpaceId == control, Ct)).Ciphertext);
            Assert.False((await db.Set<HouseholdSpaceRow>().SingleAsync(row => row.Id == control, Ct)).Deleted);
        }
        Assert.Equal(HouseholdDeletionReason.RetentionEnded, (await ledger.ReadAsync(due, Ct))!.Reason);
        Assert.True(Directory.Exists(Path.Combine(privateRoot, bad[0].ToString("N") + ".json")));
        await Assert.ThrowsAsync<IOException>(() => ledger.ReadAsync(bad[0], Ct));
        Assert.Null(await ledger.ReadAsync(bad[1], Ct));
        foreach (var id in bad.Skip(2))
        {
            Assert.Equal(invalid, await File.ReadAllBytesAsync(Path.Combine(privateRoot, id.ToString("N") + ".json"), Ct));
            await Assert.ThrowsAsync<IOException>(() => ledger.ReadAsync(id, Ct));
        }
        await File.WriteAllTextAsync(Path.Combine(root, "completed.txt"), "Two bounded real PostgreSQL passes; original malformed/missing/directory markers preserved; later expiry retired; unrelated opaque records unchanged.", Ct);
    }

    /// <summary>Explicit new fictitious root only; existing failure/success fixtures are preserved instead of reset.</summary>
    private static string FixtureRoot(string kind)
    {
        var basis = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_MARKER_TEST_ROOT") ?? throw new InvalidOperationException("Explicit owned marker fixture root required.");
        Assert.True(Path.IsPathFullyQualified(basis)); var root = basis + "-" + kind; Assert.False(Directory.Exists(root)); return root;
    }

    /// <summary>Uses the actual private ledger implementation without a replacement marker port.</summary>
    private static HouseholdDeletionLedger Ledger(string root) => new(new ServiceStorage { Root = root, LocalMailbox = false });

    /// <summary>Opaque rows isolate worker behavior, without claiming they are valid signed household membership.</summary>
    private static HouseholdSpaceRow Room(Guid id, string owner, DateTimeOffset created, bool deleted, DateTimeOffset? expires) => new()
    {
        Id = id, OwnerId = owner, CreationOperationId = Guid.NewGuid(), SchemaHash = new('a', 64), OwnerSigningKey = "fictitious-retained-public-signing",
        OwnerWrappingKey = "fictitious-retained-public-recipient", CreatedAt = created, Deleted = deleted, HostExpiresAt = expires,
    };

    /// <summary>Independent real PostgreSQL contexts share only the explicit owned marker root.</summary>
    private sealed class Factory(DbContextOptions<ServiceDbContext> options, IHouseholdDeletionLedger ledger) : IDbContextFactory<ServiceDbContext>
    {
        /// <inheritdoc />
        public ServiceDbContext CreateDbContext() => new(options, ledger);
        /// <inheritdoc />
        public Task<ServiceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    }
}
