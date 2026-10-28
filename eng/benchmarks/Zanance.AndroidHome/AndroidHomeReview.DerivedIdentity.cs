#if DERIVED_LEDGER_IDENTITY_REVIEW
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
    private const string DerivedIdentityPackage = "pro.vafadar.zanance.ledgeridentityreview";
    private static int derivedIdentityStarted;
    private static int derivedIdentityInspected;
    private static int derivedIdentityResumed;

    /// <summary>Starts only the newly admitted original API36 emulator fixture; never a historical financial phase.</summary>
    internal static void StartDerivedLedgerIdentity(MainActivity activity)
    {
        var run = activity.Intent?.GetStringExtra("review_identity_run");
        if (activity.PackageName != DerivedIdentityPackage || !Guid.TryParseExact(run, "N", out _)) return;
        if (activity.Intent?.GetBooleanExtra("review_identity_resume", false) == true)
        {
            if (Interlocked.Exchange(ref derivedIdentityResumed, 1) == 0)
                _ = ResumeDerivedIdentityAsync(activity, run);
            return;
        }
        if (activity.Intent?.GetBooleanExtra("review_identity_inspect", false) == true)
        {
            if (Interlocked.Exchange(ref derivedIdentityInspected, 1) == 0)
                _ = InspectDerivedIdentityAsync(activity, run);
            return;
        }
        if (Interlocked.Exchange(ref derivedIdentityStarted, 1) != 0) return;
        _ = ReviewDerivedIdentityAsync(activity, run);
    }

    /// <summary>Continues only the already damaged original cache; never replays seed or cache mutation.</summary>
    private static async Task ResumeDerivedIdentityAsync(MainActivity activity, string run)
    {
        var stage = "resume-admission";
        void Emit(string phase, object value) => Android.Util.Log.Info("ZananceDerivedId", JsonSerializer.Serialize(new
        { Run = run, Stage = phase, PID = Android.OS.Process.MyPid(), Report = value }));
        using var memory = new MemoryStream();
        try
        {
            if (AppInfo.Current.PackageName != DerivedIdentityPackage || (int)Android.OS.Build.VERSION.SdkInt != 36
                || Android.OS.Build.Hardware != "ranchu" || Environment.SystemPageSize != 4096)
                throw new InvalidDataException("Same owned emulator required.");
            var root = activity.FilesDir?.AbsolutePath ?? throw new IOException("Original owned source required.");
            using var admitted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "derived-identity-admitted.json")));
            using var planned = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "derived-identity-fixture-plan.json")));
            if (admitted.RootElement.GetProperty("Run").GetString() != run)
                throw new InvalidDataException("Original accepted run required.");
            var ids = planned.RootElement.GetProperty("EntryIds").EnumerateArray().Select(value => value.GetGuid()).Order().ToArray();
            var expected = activity.Intent?.GetStringExtra("review_identity_source_sha256");
            if (expected is null || expected.Length != 64 || expected.Any(character => !char.IsAsciiHexDigit(character)))
                throw new InvalidDataException("Actual post-observer complete-source receipt required.");
            using (var checkpoint = new FileStream(Path.Combine(root, "derived-identity-resume-request.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(checkpoint, new { Run = run, ExpectedRowsSHA256 = expected, NoSeedOrCacheWrite = true });
            var app = (App)Microsoft.Maui.Controls.Application.Current!;
            await app.WaitForStartupAsync();
            var services = IPlatformApplication.Current!.Services;
            var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            var time = services.GetRequiredService<TimeProvider>();
            var store = services.GetRequiredService<ZananceStore>();
            stage = "original-source-read";
            var original = await store.GetEntriesAsync();
            var accounts = await store.GetAccountsAsync();
            var source = JsonSerializer.Serialize(original.OrderBy(entry => entry.Id));
            var digest = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source)));
            if (!original.Select(entry => entry.Id).Order().SequenceEqual(ids) || digest != expected)
                throw new InvalidDataException("Retain changed original source; never prepare a replacement.");
            await using var captured = await factory.CreateDbContextAsync();
            await captured.Database.OpenConnectionAsync();
            var connection = (SqliteConnection)captured.Database.GetDbConnection();
            stage = "inspect-existing-cache-damage";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Payload, Digest FROM HomeLedgerReadBlocks ORDER BY BlockId LIMIT 1";
                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new InvalidDataException("Preserve missing original cache; do not recreate it.");
                var payload = (byte[])reader.GetValue(0);
                if (payload.Length <= 24 || !SHA256.HashData(payload).AsSpan().SequenceEqual((byte[])reader.GetValue(1))
                    || BinaryPrimitives.ReadInt32LittleEndian(payload) != 0x31424C48
                    || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4)) != 1
                    || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(8)) != 32
                    || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(12)) != 2
                    || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(16)) is < 24 or > 32 * 1024 * 1024
                    || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(20)) != payload.Length - 24)
                    throw new InvalidDataException("Original complete damaged-cache envelope differs.");
                var raw = new byte[BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(16))];
                try
                {
                    if (!BrotliDecoder.TryDecompress(payload.AsSpan(24), raw, out var expanded) || expanded != raw.Length
                        || new Guid(raw.AsSpan(sizeof(long), 16)) != Guid.Empty)
                        throw new InvalidDataException("Original cache damage is not established; never repeat its write.");
                }
                finally { CryptographicOperations.ZeroMemory(raw); }
            }
            Emit("prior-cache-damage-present", new { NoCacheMutationRepeated = true, OriginalRowsSHA256 = digest });
            stage = "original-bounded-memory-snapshot";
            // SQLCipher's existing portable exporter handles encrypted-to-plain memory snapshots; raw backup is not substituted.
            await SqlCipherPortableSnapshot.WriteAsync(connection, memory, 32 * 1024 * 1024);
            memory.Position = 0;
            await using var baseline = await SqliteMemorySnapshot.OpenAsync(memory, 32 * 1024 * 1024);
            stage = "original-native-home";
            using var observed = new IsolatedHomeReadFactory(factory, time, true);
            var nativeStore = new ZananceStore(observed, time, services.GetRequiredService<ICommercialWriteAccessSource>());
            var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, nativeStore, new SeparateHomeReadOperationSource(nativeStore));
            var page = new HomePage(vm);
            app.OpenDerivedIdentityHome(page);
            await AwaitDerivedIdentityHomeAttachmentAsync(page);
            await page.PendingPublication;
            if (!vm.Loading.IsReady || !vm.HasEntries || page.Handler?.PlatformView is not Android.Views.View native)
                throw new InvalidDataException("Original native Home must publish complete fallback rows.");
            await WaitLayoutAsync(native, CancellationToken.None);
            if (Platform.CurrentActivity != activity || !activity.HasWindowFocus)
                throw new InvalidDataException("Original native Home window required.");
            var texts = NativeTexts(native).ToHashSet(StringComparer.Ordinal);
            var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
            var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
            foreach (var balance in LedgerCalculator.TotalBalances(accounts, original, day))
                if (!texts.Contains(MoneyText.Format(balance.Value, balance.Key, culture)))
                    throw new InvalidDataException("Complete original native balance differs.");
            if (observed.DerivedBlockReads != 1 || observed.CompleteScalarReads != 1)
                throw new InvalidDataException("One actual failed derived read and complete scalar fallback required.");
            if (JsonSerializer.Serialize((await store.GetEntriesAsync()).OrderBy(entry => entry.Id)) != source)
                throw new InvalidDataException("Original complete source rows changed.");
            stage = "original-full-snapshot-comparison";
            await SqliteSnapshotVerifier.VerifyAsync(baseline.Connection, connection);
            var result = new { OriginalNativeHomePublished = true, AllOriginalTypedTablesAndSchemaUnchangedAfterExistingDamage = true,
                OriginalRowsSHA256 = digest, BaselineCapturedAfterReadOnlyObserver = true, InitialFullSourceBaselineNotClaimed = true,
                OriginalEntryCount = 2, observed.DerivedBlockReads, observed.CompleteScalarReads,
                PriorAcceptedCacheDamageWriteNotRepeated = true, MalformedCacheNotRepaired = true, NoFinancialSeedReplay = true,
                OriginalPortableExporterAndFullTypedVerifier = true, NoPlaintextFileOrKeyExport = true,
                NoMainOrPhoneOperation = true, NoPerformanceOrWholeDeviceAcceptance = true };
            using (var output = new FileStream(Path.Combine(root, "derived-identity-complete.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(output, result);
            Emit("complete", result);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Emit("resume-refused", new { Stage = stage, ExceptionType = error.GetType().Name,
                SqliteCode = error is SqliteException sqlite ? sqlite.SqliteErrorCode : (int?)null,
                SqliteExtendedCode = error is SqliteException extended ? extended.SqliteExtendedErrorCode : (int?)null,
                NoExceptionMessageSQLPathOrKeyExport = true, NoPrepareOrCacheWriteReplay = true });
        }
        finally { CryptographicOperations.ZeroMemory(memory.GetBuffer().AsSpan()); }
    }

    /// <summary>Waits for the original replacement root to attach before awaiting its original appearance publication.</summary>
    private static async Task AwaitDerivedIdentityHomeAttachmentAsync(HomePage page)
    {
        for (var attempt = 0; attempt < 1200; attempt++)
        {
            if (page.IsLoaded && page.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true }) return;
            await Task.Delay(50);
        }
        throw new TimeoutException("Observe the same original native root; never repeat fixture preparation.");
    }

    /// <summary>Reads only the accepted source/schema and bounded numeric failures after an interrupted native observation.</summary>
    private static async Task InspectDerivedIdentityAsync(MainActivity activity, string run)
    {
        var stage = "admission";
        void Emit(string phase, object value) => Android.Util.Log.Info("ZananceDerivedId", JsonSerializer.Serialize(new
        { Run = run, Stage = phase, PID = Android.OS.Process.MyPid(), Report = value }));
        try
        {
            if (AppInfo.Current.PackageName != DerivedIdentityPackage || (int)Android.OS.Build.VERSION.SdkInt != 36
                || Android.OS.Build.Hardware != "ranchu" || Environment.SystemPageSize != 4096)
                throw new InvalidDataException("Same owned emulator required.");
            var root = activity.FilesDir?.AbsolutePath ?? throw new IOException("Original owned source required.");
            using var admitted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "derived-identity-admitted.json")));
            if (admitted.RootElement.GetProperty("Run").GetString() != run)
                throw new InvalidDataException("Original accepted run required.");
            using var planned = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "derived-identity-fixture-plan.json")));
            var ids = planned.RootElement.GetProperty("EntryIds").EnumerateArray().Select(value => value.GetGuid()).Order().ToArray();
            using (var checkpoint = new FileStream(Path.Combine(root, "derived-identity-readonly-inspection.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(checkpoint, new { Run = run, NoPrepareWriterOrRearm = true });
            await ((App)Microsoft.Maui.Controls.Application.Current!).WaitForStartupAsync();
            var services = IPlatformApplication.Current!.Services;
            var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            stage = "schema-read";
            await db.Database.OpenConnectionAsync();
            var names = new List<string>();
            using (var query = db.Database.GetDbConnection().CreateCommand())
            {
                query.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Entries','HomeLedgerReadBlocks','HomeLedgerReadState','HomeLedgerDirtyBlocks','__EFMigrationsHistory') ORDER BY name";
                await using var reader = await query.ExecuteReaderAsync();
                while (await reader.ReadAsync()) names.Add(reader.GetString(0));
            }
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            var known = db.Database.GetMigrations().ToArray();
            Emit("inspection-schema", new { Tables = names, AppliedCount = applied.Length, KnownCount = known.Length,
                LastApplied = applied.LastOrDefault(), LastKnown = known.LastOrDefault(), NoSourceMutation = true });
            stage = "original-ef-rows";
            var entries = await db.Entries.AsNoTracking().ToListAsync();
            if (!entries.Select(entry => entry.Id).Order().SequenceEqual(ids))
                throw new InvalidDataException("Retain different existing rows; never reseed.");
            var encoded = JsonSerializer.Serialize(entries.OrderBy(entry => entry.Id));
            Emit("inspection-original-rows", new { Entries = entries.Count,
                OriginalRowsSHA256AfterObserverUpgrade = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(encoded))),
                InitialFullSourceBaselineNotClaimed = true, NoSourceMutation = true });
            stage = "original-store-read";
            var original = await services.GetRequiredService<ZananceStore>().GetEntriesAsync();
            if (JsonSerializer.Serialize(original.OrderBy(entry => entry.Id)) != encoded)
                throw new InvalidDataException("Original EF/store rows differ.");
            Emit("inspection-complete", new { NoSourceMutation = true, OriginalEFAndStoreRowsMatch = true,
                OnlyOriginalAcceptedFixture = true, NoFinancialReplay = true });
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Emit("inspection-refused", new { Stage = stage, ExceptionType = error.GetType().Name,
                SqliteCode = error is SqliteException sqlite ? sqlite.SqliteErrorCode : (int?)null,
                SqliteExtendedCode = error is SqliteException extended ? extended.SqliteExtendedErrorCode : (int?)null,
                NoExceptionMessageSQLPathOrKeyExport = true, NoPrepareOrFinancialReplay = true });
        }
    }

    /// <summary>Retains one accepted two-row fixture and one malformed-cache native Home publication without repair.</summary>
    private static async Task ReviewDerivedIdentityAsync(MainActivity activity, string run)
    {
        const string tag = "ZananceDerivedId";
        using var memory = new MemoryStream();
        void Emit(string stage, object report) => Android.Util.Log.Info(tag, JsonSerializer.Serialize(new
        { Run = run, Stage = stage, PID = Android.OS.Process.MyPid(), Report = report }));
        try
        {
            if (AppInfo.Current.PackageName != DerivedIdentityPackage || (int)Android.OS.Build.VERSION.SdkInt != 36
                || Android.OS.Build.Hardware != "ranchu" || Environment.SystemPageSize != 4096)
                throw new InvalidDataException("Fresh owned API36/x64/4096 emulator required before any fixture write.");
            var root = activity.FilesDir?.AbsolutePath ?? throw new IOException("Owned files directory required.");
            var journal = Path.Combine(root, "derived-identity-admitted.json");
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
                using (var plan = new FileStream(Path.Combine(root, "derived-identity-fixture-plan.json"), FileMode.CreateNew))
                    JsonSerializer.Serialize(plan, new { AccountId = accountId, EntryIds = entryIds, Day = day, Created = now });
                await using var write = await seed.Database.BeginTransactionAsync();
                if (accounts.Count == 0)
                    seed.Accounts.Add(new Account(accountId) { Name = "Native identity fallback", Type = AccountType.Cash,
                        CurrencyCode = "EUR", OpeningDate = day, CreatedAt = now, UpdatedAt = now });
                seed.Entries.AddRange(new LedgerEntry(entryIds[0]) { AccountId = accountId, Date = day, Kind = EntryKind.Income,
                    Amount = 10000, Title = "Fictitious identity income", Tags = ["original", "متن"], CreatedAt = now, UpdatedAt = now },
                    new LedgerEntry(entryIds[1]) { AccountId = accountId, Date = day, Kind = EntryKind.Expense,
                        Amount = 1000, Title = "Fictitious identity expense", Note = "Retain complete original fields", CreatedAt = now, UpdatedAt = now });
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
            long blockId; byte[] payload;
            using (var select = connection.CreateCommand())
            {
                select.CommandText = "SELECT BlockId, Payload FROM HomeLedgerReadBlocks ORDER BY BlockId LIMIT 1";
                await using var reader = await select.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new InvalidDataException("Original complete derived block required.");
                blockId = reader.GetInt64(0); payload = (byte[])reader.GetValue(1);
            }
            if (payload.Length <= 24 || BinaryPrimitives.ReadInt32LittleEndian(payload) != 0x31424C48
                || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4)) != 1
                || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(8)) != 32
                || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(12)) != 2
                || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(16)) is < 1 or > 32 * 1024 * 1024
                || BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(20)) != payload.Length - 24)
                throw new InvalidDataException("Exact original two-row complete transport required.");
            var raw = new byte[BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(16))];
            if (!BrotliDecoder.TryDecompress(payload.AsSpan(24), raw, out var expanded) || expanded != raw.Length
                || !entryIds.Contains(new Guid(raw.AsSpan(sizeof(long), 16))))
                throw new InvalidDataException("Original nonempty first identity required before synthetic cache damage.");
            raw.AsSpan(sizeof(long), 16).Clear();
            var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(raw.Length)];
            if (!BrotliEncoder.TryCompress(raw, compressed, out var written, quality: 1, window: 22))
                throw new InvalidDataException("Complete recompression required.");
            var damaged = new byte[24 + written]; payload.AsSpan(0, 24).CopyTo(damaged);
            BinaryPrimitives.WriteInt32LittleEndian(damaged.AsSpan(20), written);
            compressed.AsSpan(0, written).CopyTo(damaged.AsSpan(24));
            await captured.Database.ExecuteSqlRawAsync("UPDATE HomeLedgerReadBlocks SET Payload={0}, Digest={1} WHERE BlockId={2}",
                [damaged, SHA256.HashData(damaged), blockId]);
            await SqlCipherPortableSnapshot.WriteAsync(connection, memory, 32 * 1024 * 1024);
            memory.Position = 0;
            await using var baseline = await SqliteMemorySnapshot.OpenAsync(memory, 32 * 1024 * 1024);
            Emit("malformed-cache-bound", new { MatchingDigest = true, OriginalIdentityUnchanged = true, NoSourceWriteAfterSeed = true });
            using var observed = new IsolatedHomeReadFactory(factory, time, true);
            var nativeStore = new ZananceStore(observed, time, services.GetRequiredService<ICommercialWriteAccessSource>());
            var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, nativeStore, new SeparateHomeReadOperationSource(nativeStore));
            var page = new HomePage(vm);
            app.OpenDerivedIdentityHome(page);
            await AwaitDerivedIdentityHomeAttachmentAsync(page);
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
            if (observed.DerivedBlockReads != 1 || observed.CompleteScalarReads != 1)
                throw new InvalidDataException("One actual failed derived read and complete scalar fallback required.");
            if (JsonSerializer.Serialize((await store.GetEntriesAsync()).OrderBy(entry => entry.Id)) != originalRows)
                throw new InvalidDataException("Authoritative original rows changed.");
            await SqliteSnapshotVerifier.VerifyAsync(baseline.Connection, connection);
            var result = new { OriginalNativeHomePublished = true, AllOriginalTypedTablesAndSchemaUnchangedAfterDamage = true,
                OriginalEntryRowsSHA256 = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(originalRows))),
                OriginalEntryCount = 2, observed.DerivedBlockReads, observed.CompleteScalarReads,
                MalformedCacheNotRepaired = true, ExistingFinancialReplay = 0, NoMainKeyOrDatabaseRead = true,
                NoRuntimeClockPermissionSettingChange = true, NoFullDevicePerformanceOrReleaseAcceptance = true,
                DataModule = typeof(ZananceStore).Module.ModuleVersionId };
            using (var output = new FileStream(Path.Combine(root, "derived-identity-complete.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(output, result);
            Emit("complete", result);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Emit("refused", new { ExceptionType = error.GetType().Name, PreserveSourceAndAcceptedCheckpoints = true });
        }
        finally { CryptographicOperations.ZeroMemory(memory.GetBuffer().AsSpan()); }
    }
}

public partial class App
{
    /// <summary>Uses original root replacement only for the independently owned malformed-identity fixture.</summary>
    internal void OpenDerivedIdentityHome(HomePage page)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.ledgeridentityreview")
            throw new InvalidDataException("Fresh derived-identity review package required.");
        ReplaceRoot(Windows[0], new NavigationPage(page));
    }
}
#endif
