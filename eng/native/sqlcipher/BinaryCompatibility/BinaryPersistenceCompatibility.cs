using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;


namespace Vafadar.SqlCipher.BinaryCompatibility;

/// <summary>Exercises actual production persistence assemblies through caller-owned native effects.</summary>
internal static class BinaryPersistenceCompatibility
{
    /// <summary>Creates or reopens only an independently owned fictitious encrypted fixture.</summary>
    internal static async Task RunAsync(string root, bool create, Func<string, IDatabaseGenerationKeyStore> createKeys, Func<string, IDatabaseGenerationFileDurability> createDurability)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (typeof(LocalDbContext).Assembly.GetName().Name != "Vafadar.Data" || typeof(ZananceDbContext).Assembly.GetName().Name != "Vafadar.Zanance.Data")
            throw new IOException("The caller must load actual production assemblies, not source copies.");
        var original = Path.Combine(root, "zanance.db");
        using var services = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        void Validate() { if (!Directory.Exists(root)) throw new IOException("Owned binary fixture root was retired."); }
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(root, "identities.sqlite"), Validate);
        var keys = createKeys(root); var durability = createDurability(root);
        SqlCipherProfileGeneration generation;
        // D-253: actual protected host registration must refuse pre-bootstrap reads without creating plaintext.
        var unpublishedFactory = services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>();
        var refusedBeforePublication = false;
        try { await using var premature = await unpublishedFactory.CreateDbContextAsync(); }
        catch (InvalidOperationException) { refusedBeforePublication = true; }
        if (!refusedBeforePublication || File.Exists(original)) throw new IOException("Protected registration allowed a plaintext endpoint.");
        using (var owner = await location.Access.AcquireExclusiveAsync())
        {
            if (create) await identities.CreateNewAsync([""], durability);
            generation = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, create,
                create ? keys : new ExistingKeys(keys), durability, BuildSchema, owner, Validate, 16 * 1024 * 1024, "4.19.0");
            generation.Publish(location, owner);
            if (create)
            {
                await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner);
                db.Accounts.Add(new Account { Name = "Fictitious binary assembly بانک Ü", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = -321 });
                await db.SaveChangesAsync();
            }
        }
        using (generation)
        {
            // D-274: time the original complete operation, not a copied query or a reduced security check.
            var store = services.GetRequiredService<ZananceStore>();
            var quickReadMilliseconds = new List<double>();
            for (var sample = 0; sample < 5; sample++)
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var review = await store.GetQuickResourceReviewAsync();
                clock.Stop();
                if (review.Accounts.Count != 1 || review.Accounts[0].OpeningBalance != -321)
                    throw new IOException("Timed original quick-resource read lost financial state.");
                quickReadMilliseconds.Add(clock.Elapsed.TotalMilliseconds);
            }
            await File.WriteAllTextAsync(Path.Combine(root, create ? "quick-review-create.json" : "quick-review-reopen.json"),
                System.Text.Json.JsonSerializer.Serialize(new { FirstThenFourWarmMilliseconds = quickReadMilliseconds,
                    ActualProductionOperation = true, CipherVersion = "4.19.0", KeyDerivationUnchanged = true }));
            // D-276: each timed operation includes its actual original packet and later-action stale/access recheck.
            var workMilliseconds = new Dictionary<string, List<double>>();
            var plans = services.GetRequiredService<PlanStore>(); var goals = services.GetRequiredService<GoalStore>();
            async Task MeasureWork(string name, Func<Task> originalOperation)
            {
                var times = new List<double>();
                for (var sample = 0; sample < 4; sample++)
                { var clock = System.Diagnostics.Stopwatch.StartNew(); await originalOperation(); clock.Stop(); times.Add(clock.Elapsed.TotalMilliseconds); }
                workMilliseconds.Add(name, times);
            }
            await MeasureWork("PlanReadAndValidate", async () =>
            { var packet = await plans.GetWorkSnapshotAsync(); await plans.ValidateWorkSnapshotAsync(packet); });
            await MeasureWork("GoalReadAndValidate", async () =>
            { var packet = await goals.GetWorkSnapshotAsync(); await goals.ValidateWorkSnapshotAsync(packet); });
            await MeasureWork("BudgetFormReadAndValidate", async () =>
            { var packet = await store.GetBudgetEditorReviewAsync(); await store.ValidateBudgetEditorReviewAsync(packet); });
            await MeasureWork("BudgetAlertReadAndValidate", async () =>
            { var packet = await store.GetBudgetAlertSnapshotAsync(); await store.ValidateBudgetAlertSnapshotAsync(packet); });
            await MeasureWork("QuickReadAndValidate", async () =>
            { var packet = await store.GetQuickResourceReviewAsync(); await store.ValidateQuickResourceReviewAsync(packet); });
            await File.WriteAllTextAsync(Path.Combine(root, create ? "work-read-create.json" : "work-read-reopen.json"),
                System.Text.Json.JsonSerializer.Serialize(new { FirstThenThreeWarmMilliseconds = workMilliseconds,
                    ActualProductionReadAndValidation = true, NoGlobalPool = true, KeyDerivationUnchanged = true }));
            await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync())
            {
                if ((await db.Accounts.SingleAsync()).OpeningBalance != -321 || (await db.Database.GetAppliedMigrationsAsync()).Count() != 32)
                    throw new IOException("Actual production model/financial packet differs.");
            }
            using var portable = new MemoryStream();
            await services.GetRequiredService<IBackupSource>().WriteAsync(portable, default); portable.Position = 0;
            try
            {
                await using var snapshot = await SqliteMemorySnapshot.OpenAsync(portable, 16 * 1024 * 1024);
                await using var imported = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>()
                    .UseSqlite(snapshot.Connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options);
                if ((await imported.Accounts.SingleAsync()).OpeningBalance != -321) throw new IOException("Binary portable snapshot differs.");
            }
            finally { CryptographicOperations.ZeroMemory(portable.GetBuffer().AsSpan()); }
            var identity = (await identities.ReadAsync()).Single(); var key = await keys.ReadAsync(identity.InitialCandidateKey, default);
            try
            {
                var wrong = key.ToArray(); wrong[0] ^= 1;
                try
                {
                    await using var invalid = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = generation.Path,
                        Password = Convert.ToBase64String(wrong), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
                    var refused = false;
                    try { await invalid.OpenAsync(); using var command = invalid.CreateCommand(); command.CommandText = "SELECT count(*) FROM sqlite_schema"; await command.ExecuteScalarAsync(); }
                    catch (SqliteException) { refused = true; }
                    if (!refused) throw new IOException("Wrong native key was accepted.");
                }
                finally { CryptographicOperations.ZeroMemory(wrong); }
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        Console.WriteLine("{\"ActualPersistenceAssemblies\":true,\"Original32Migrations\":true,\"NegativeMoneyPreserved\":true,\"ActualBinaryPortableSource\":true,\"WrongKeyRefused\":true}");

        static async Task BuildSchema(SqliteConnection connection, CancellationToken ct)
        {
            await using var db = new ZananceDbContext(new DbContextOptionsBuilder<ZananceDbContext>()
                .UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options);
            await db.Database.MigrateAsync(ct);
        }

    }
}

/// <summary>Cold binary reopen may only read an existing native key.</summary>
internal sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
{
    /// <inheritdoc />
    public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold binary reopen cannot create keys.");
    /// <inheritdoc />
    public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
}
