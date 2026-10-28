#if DERIVED_LEDGER_SCHEMA_REVIEW
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    private const string DerivedSchemaPackage = "pro.vafadar.zanance.ledgerschemareview";
    private static int derivedSchemaStarted;

    /// <summary>Admits one new fictitious schema-fault cohort, never an existing source or accepted preparation.</summary>
    internal static void StartDerivedLedgerSchema(MainActivity activity)
    {
        var run = activity.Intent?.GetStringExtra("review_schema_run");
        if (activity.PackageName != DerivedSchemaPackage || !Guid.TryParseExact(run, "N", out _)
            || Interlocked.Exchange(ref derivedSchemaStarted, 1) != 0) return;
        _ = ReviewDerivedSchemaAsync(activity, run);
    }

    /// <summary>Accepts one fresh fixture and verifies original native Home and full source after optional schema damage.</summary>
    private static async Task ReviewDerivedSchemaAsync(MainActivity activity, string run)
    {
        const string tag = "ZananceDerivedSchema";
        using var memory = new MemoryStream();
        void Emit(string stage, object report) => Android.Util.Log.Info(tag, JsonSerializer.Serialize(new
        { Run = run, Stage = stage, PID = Android.OS.Process.MyPid(), Report = report }));
        try
        {
            if (AppInfo.Current.PackageName != DerivedSchemaPackage || (int)Android.OS.Build.VERSION.SdkInt != 36
                || Android.OS.Build.Hardware != "ranchu" || Environment.SystemPageSize != 4096)
                throw new InvalidDataException("Fresh owned API36/x64/4096 emulator required before any fixture write.");
            var root = activity.FilesDir?.AbsolutePath ?? throw new IOException("Owned files directory required.");
            var journal = Path.Combine(root, "derived-schema-admitted.json");
            using (var admitted = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                JsonSerializer.Serialize(admitted, new { Run = run, NativePID = Android.OS.Process.MyPid() });
            var app = (App)Microsoft.Maui.Controls.Application.Current!;
            await app.WaitForStartupAsync();
            if (Platform.CurrentActivity != activity || !activity.HasWindowFocus)
                throw new InvalidDataException("Original ready owned Activity required.");
            var services = IPlatformApplication.Current!.Services;
            var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            var time = services.GetRequiredService<TimeProvider>();
            var store = services.GetRequiredService<ZananceStore>();
            var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
            var now = time.GetUtcNow();
            Guid accountId;
            var entryIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            await using (var seed = await factory.CreateDbContextAsync())
            {
                if (await seed.Entries.AnyAsync()) throw new InvalidDataException("Never replace an existing source.");
                var accounts = await seed.Accounts.ToListAsync();
                if (accounts.Count > 1 || accounts.Any(account => account.OpeningBalance != 0 || account.IsArchived))
                    throw new InvalidDataException("Only the fresh empty source may receive this fixture.");
                accountId = accounts.FirstOrDefault()?.Id ?? Guid.NewGuid();
                using (var plan = new FileStream(Path.Combine(root, "derived-schema-fixture-plan.json"), FileMode.CreateNew))
                    JsonSerializer.Serialize(plan, new { AccountId = accountId, EntryIds = entryIds, Day = day, Created = now });
                await using var write = await seed.Database.BeginTransactionAsync();
                if (accounts.Count == 0)
                    seed.Accounts.Add(new Account(accountId) { Name = "Native schema fallback", Type = AccountType.Cash,
                        CurrencyCode = "EUR", OpeningDate = day, CreatedAt = now, UpdatedAt = now });
                seed.Entries.AddRange(new LedgerEntry(entryIds[0]) { AccountId = accountId, Date = day, Kind = EntryKind.Income,
                    Amount = 10000, Title = "Fictitious schema income", Tags = ["original", "متن"], CreatedAt = now, UpdatedAt = now },
                    new LedgerEntry(entryIds[1]) { AccountId = accountId, Date = day, Kind = EntryKind.Expense,
                        Amount = 1000, Title = "Fictitious schema expense", Note = "Retain complete original fields", CreatedAt = now, UpdatedAt = now });
                await seed.SaveChangesAsync(); await write.CommitAsync();
            }
            Emit("fixture-accepted", new { Entries = 2, SeedAcceptedOnce = true, ExistingFinancialReplay = 0 });
            var original = await store.GetEntriesAsync();
            var originalAccounts = await store.GetAccountsAsync();
            if (!original.Select(entry => entry.Id).Order().SequenceEqual(entryIds.Order()))
                throw new InvalidDataException("Complete original fixture identity required.");
            var originalRows = JsonSerializer.Serialize(original.OrderBy(entry => entry.Id));
            await using var captured = await factory.CreateDbContextAsync();
            await captured.Database.OpenConnectionAsync();
            var connection = (SqliteConnection)captured.Database.GetDbConnection();
            // Damage only this fresh optional contract once; original financial rows remain authoritative.
            using (var damagePlan = new FileStream(Path.Combine(root, "schema-damage-plan.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(damagePlan, new { Run = run, Table = "HomeLedgerReadState", Column = "CodecVersion" });
            await captured.Database.ExecuteSqlRawAsync("ALTER TABLE HomeLedgerReadState RENAME COLUMN CodecVersion TO UnsupportedColumn");
            await SqlCipherPortableSnapshot.WriteAsync(connection, memory, 32 * 1024 * 1024);
            memory.Position = 0;
            await using var baseline = await SqliteMemorySnapshot.OpenAsync(memory, 32 * 1024 * 1024);
            Emit("unsupported-schema-bound", new { RenamedOptionalColumn = true, OriginalIdentityUnchanged = true, NoSourceWriteAfterSeed = true });
            using var observed = new IsolatedHomeReadFactory(factory, time, true);
            var nativeStore = new ZananceStore(observed, time, services.GetRequiredService<ICommercialWriteAccessSource>());
            var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, nativeStore, new SeparateHomeReadOperationSource(nativeStore));
            var page = new HomePage(vm);
            app.OpenDerivedSchemaHome(page);
            await AwaitDerivedSchemaHomeAttachmentAsync(page);
            await page.PendingPublication;
            if (!vm.Loading.IsReady || !vm.HasEntries || page.Handler?.PlatformView is not Android.Views.View native)
                throw new InvalidDataException("Original native Home must publish complete fallback rows.");
            await WaitLayoutAsync(native, CancellationToken.None);
            if (Platform.CurrentActivity != activity || !activity.HasWindowFocus)
                throw new InvalidDataException("Native Home lost its original owned window.");
            var texts = NativeTexts(native).ToHashSet(StringComparer.Ordinal);
            var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
            foreach (var balance in LedgerCalculator.TotalBalances(originalAccounts, original, day))
                if (!texts.Contains(MoneyText.Format(balance.Value, balance.Key, culture)))
                    throw new InvalidDataException("Complete original native balance differs.");
            if (observed.DerivedBlockReads != 0 || observed.CompleteScalarReads != 1)
                throw new InvalidDataException("Optional schema must decline before a block query and use complete scalar fallback.");
            if (JsonSerializer.Serialize((await store.GetEntriesAsync()).OrderBy(entry => entry.Id)) != originalRows)
                throw new InvalidDataException("Authoritative original rows changed.");
            await SqliteSnapshotVerifier.VerifyAsync(baseline.Connection, connection);
            var result = new { OriginalNativeHomePublished = true, AllOriginalTypedTablesAndSchemaUnchangedAfterDamage = true,
                OriginalEntryRowsSHA256 = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(originalRows))),
                OriginalEntryCount = 2, observed.DerivedBlockReads, observed.CompleteScalarReads,
                UnsupportedSchemaNotRepaired = true, ExistingFinancialReplay = 0, NoMainKeyOrDatabaseRead = true,
                NoRuntimeClockPermissionSettingChange = true, NoFullDevicePerformanceOrReleaseAcceptance = true,
                DataModule = typeof(ZananceStore).Module.ModuleVersionId };
            using (var output = new FileStream(Path.Combine(root, "derived-schema-complete.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(output, result);
            Emit("complete", result);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Emit("refused", new { ExceptionType = error.GetType().Name, PreserveSourceAndAcceptedCheckpoints = true });
        }
        finally { CryptographicOperations.ZeroMemory(memory.GetBuffer().AsSpan()); }
    }
    private static async Task AwaitDerivedSchemaHomeAttachmentAsync(HomePage page)
    {
        for (var attempt = 0; attempt < 1200; attempt++)
        {
            if (page.IsLoaded && page.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true }) return;
            await Task.Delay(50);
        }
        throw new TimeoutException("Observe the same original native root; never repeat fixture preparation.");
    }

}

public partial class App
{
    /// <summary>Uses original root replacement only for the independently owned unsupported-schema fixture.</summary>
    internal void OpenDerivedSchemaHome(HomePage page)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.ledgerschemareview")
            throw new InvalidDataException("Fresh derived-schema review package required.");
        ReplaceRoot(Windows[0], new NavigationPage(page));
    }
}
#endif
