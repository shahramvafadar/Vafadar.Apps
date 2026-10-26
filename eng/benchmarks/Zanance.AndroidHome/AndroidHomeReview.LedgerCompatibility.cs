using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    /// <summary>One new-platform fixture plan, durable before its single atomic synthetic write; never a private key.</summary>
    private sealed record CodecRuntimeFixture(Guid AccountId, bool CreateAccount, Guid[] EntryIds, DateOnly Day, DateTimeOffset Created);

    /// <summary>Qualifies only the outstanding nonempty original codec/native fallback path on owned compatibility runtimes.</summary>
    private static async Task<object> ReviewLedgerCodecCompatibilityAsync(App app, IServiceProvider services, string root,
        string comparison, CancellationToken ct)
    {
        var api = (int)Android.OS.Build.VERSION.SdkInt;
        var nativePage = Android.Systems.Os.Sysconf(Android.Systems.OsConstants.ScPagesize);
        var managedPage = Environment.SystemPageSize;
        if (Package != "pro.vafadar.zanance.ledgercodeccompatibility" || AppInfo.Current.PackageName != Package
            || api is not (24 or 35) || nativePage != (api == 24 ? 4096 : 16384) || managedPage != nativePage)
            throw new ReviewCheckException("Exact owned API24/4096 or simulated API35/16384 in-process page size required before fixture writes.");
        var finalPath = Path.Combine(root, "codec-runtime-complete.json");
        if (File.Exists(finalPath)) throw new ReviewCheckException("Native codec runtime already complete; retain its original receipt instead of replay.");
        await AwaitOriginalMoneyWindowAsync(ct);
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var time = services.GetRequiredService<TimeProvider>();
        var store = services.GetRequiredService<ZananceStore>();
        var marker = Path.Combine(root, "codec-runtime-fixture.json");
        CodecRuntimeFixture fixture;
        if (File.Exists(marker)) fixture = JsonSerializer.Deserialize<CodecRuntimeFixture>(await File.ReadAllTextAsync(marker, ct))
            ?? throw new ReviewCheckException("Retain the original unreadable codec fixture; never reseed it.");
        else
        {
            await using var inspect = await factory.CreateDbContextAsync(ct);
            if (await inspect.Entries.AnyAsync(ct)) throw new ReviewCheckException("Fresh owned compatibility source required; never replace existing financial rows.");
            var accounts = await inspect.Accounts.AsNoTracking().ToListAsync(ct);
            if (accounts.Count > 1 || accounts.Any(account => account.OpeningBalance != 0 || account.IsArchived))
                throw new ReviewCheckException("Only the new empty default compatibility profile may receive this fixture.");
            var now = time.GetUtcNow();
            fixture = new(accounts.FirstOrDefault()?.Id ?? Guid.NewGuid(), accounts.Count == 0,
                Enumerable.Range(0, 1031).Select(_ => Guid.NewGuid()).ToArray(), DateOnly.FromDateTime(time.GetLocalNow().DateTime), now);
            await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(fixture), ct);
        }
        await using (var seed = await factory.CreateDbContextAsync(ct))
        {
            var ids = await seed.Entries.AsNoTracking().Select(entry => entry.Id).ToListAsync(ct);
            if (ids.Count == 0)
            {
                // D-549: new platform coverage needs one owned atomic fixture; its durable planned ids prevent re-posting after a lost observer.
                await using var write = await seed.Database.BeginTransactionAsync(ct);
                if (fixture.CreateAccount)
                    seed.Accounts.Add(new Account(fixture.AccountId) { Name = "Native codec compatibility", CurrencyCode = "EUR",
                        Type = AccountType.Cash, OpeningDate = fixture.Day, CreatedAt = fixture.Created, UpdatedAt = fixture.Created });
                seed.Entries.AddRange(fixture.EntryIds.Select((id, index) => new LedgerEntry(id) { AccountId = fixture.AccountId,
                    Kind = index == 0 ? EntryKind.Income : EntryKind.Expense, Amount = index == 0 ? 10000 : 1,
                    Date = fixture.Day, Title = "Native codec " + index, Payee = index % 2 == 0 ? null : "Owned fixture",
                    Note = index % 2 == 0 ? string.Concat(Enumerable.Repeat("Unicode fixture فارسی 中文 EUR ", 16)) : null,
                    Tags = index % 2 == 0 ? [] : ["codec", "فارسی"], CreatedAt = fixture.Created, UpdatedAt = fixture.Created }));
                await seed.SaveChangesAsync(ct); await write.CommitAsync(ct);
            }
            else if (!ids.Order().SequenceEqual(fixture.EntryIds.Order()))
                throw new ReviewCheckException("Existing fixture ids differ; never seed or replace accepted rows.");
        }
        var original = await store.GetEntriesAsync(cancellationToken: ct);
        if (!original.Select(entry => entry.Id).Order().SequenceEqual(fixture.EntryIds.Order()))
            throw new ReviewCheckException("Complete original native fixture read differs.");
        var source = JsonSerializer.Serialize(original.OrderBy(entry => entry.Id));
        var digest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source)));
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        async Task<(HomeReadSnapshot Snapshot, int Scalar, int Blocks)> Read(bool derived)
        {
            using var owned = new IsolatedHomeReadFactory(factory, time, derived);
            var reader = new ZananceStore(owned, time, services.GetRequiredService<ICommercialWriteAccessSource>());
            var snapshot = await reader.GetHomeSnapshotAsync(day, 0, ct);
            return (snapshot, owned.CompleteScalarReads, owned.DerivedBlockReads);
        }
        void Complete(HomeReadSnapshot snapshot)
        {
            if (JsonSerializer.Serialize(snapshot.Entries.OrderBy(entry => entry.Id)) != source)
                throw new ReviewCheckException("Original complete native ledger fields or tag relationships differ.");
        }
        long blockId; byte[] payload; byte[] originalDigest;
        await using (var inspect = await factory.CreateDbContextAsync(ct))
        {
            await inspect.Database.OpenConnectionAsync(ct);
            using var command = inspect.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT BlockId, Payload, Digest FROM HomeLedgerReadBlocks ORDER BY BlockId DESC LIMIT 1 OFFSET 1";
            await using var row = await command.ExecuteReaderAsync(ct);
            if (!await row.ReadAsync(ct)) throw new ReviewCheckException("Multiple real payload blocks required before native fallback qualification.");
            blockId = row.GetInt64(0); payload = (byte[])row.GetValue(1); originalDigest = (byte[])row.GetValue(2);
        }
        async Task SetPayload(byte[] bytes, byte[] hash)
        {
            await using var context = await factory.CreateDbContextAsync(ct);
            await context.Database.ExecuteSqlRawAsync("UPDATE HomeLedgerReadBlocks SET Payload={0}, Digest={1} WHERE BlockId={2}", [bytes, hash, blockId], ct);
        }
        var recoveryPath = Path.Combine(root, "codec-cache-recovery.json");
        var payloadBackup = Path.Combine(root, "codec-cache-original.bin");
        var digestBackup = Path.Combine(root, "codec-cache-original-digest.bin");
        if (File.Exists(recoveryPath))
        {
            using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(recoveryPath, ct));
            if (saved.RootElement.GetProperty("SourceDigest").GetString() != digest
                || saved.RootElement.GetProperty("BlockId").GetInt64() != blockId)
                throw new ReviewCheckException("Owned original codec recovery source changed; never overwrite another source.");
            payload = await File.ReadAllBytesAsync(payloadBackup, ct); originalDigest = await File.ReadAllBytesAsync(digestBackup, ct);
            if (Convert.ToHexString(SHA256.HashData(payload)) != saved.RootElement.GetProperty("PayloadHash").GetString()
                || !SHA256.HashData(payload).AsSpan().SequenceEqual(originalDigest))
                throw new ReviewCheckException("Retain incomplete original private fixture recovery copies; never guess or reseed.");
            await SetPayload(payload, originalDigest);
        }
        else
        {
            await File.WriteAllBytesAsync(payloadBackup, payload, ct); await File.WriteAllBytesAsync(digestBackup, originalDigest, ct);
            await File.WriteAllTextAsync(recoveryPath, JsonSerializer.Serialize(new { SourceDigest = digest, BlockId = blockId,
                PayloadHash = Convert.ToHexString(SHA256.HashData(payload)), OnlyOwnedFictitiousCacheNoKeys = true }), ct);
        }
        async Task<bool> HasAccepted(string name)
        {
            var path = Path.Combine(root, name);
            if (!File.Exists(path)) return false;
            using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
            if (saved.RootElement.GetProperty("SourceDigest").GetString() != digest
                || saved.RootElement.GetProperty("API").GetInt32() != api
                || saved.RootElement.GetProperty("NativePageBytes").GetInt64() != nativePage)
                throw new ReviewCheckException("Accepted native checkpoint source/runtime changed; never replace it.");
            return true;
        }
        Task Accept(string name) => File.WriteAllTextAsync(Path.Combine(root, name), JsonSerializer.Serialize(new
        { SourceDigest = digest, API = api, NativePageBytes = nativePage, DataModule = typeof(ZananceStore).Module.ModuleVersionId }), ct);
        if (!await HasAccepted("codec-valid-read-complete.json"))
        {
            var scalar = await Read(false); Complete(scalar.Snapshot);
            var derived = await Read(true); Complete(derived.Snapshot);
            if (scalar.Scalar != 1 || scalar.Blocks != 0 || derived.Scalar != 0 || derived.Blocks != 1)
                throw new ReviewCheckException("Actual scalar and nonempty block routes must be observed, never inferred.");
            await Accept("codec-valid-read-complete.json");
        }
        if (!await HasAccepted("codec-fallback-complete.json"))
        {
        try
        {
            var damaged = payload[..^1]; BinaryPrimitives.WriteInt32LittleEndian(damaged.AsSpan(20), damaged.Length - 24);
            await SetPayload(damaged, SHA256.HashData(damaged));
            var fallback = await Read(true); Complete(fallback.Snapshot);
            if (fallback.Scalar != 1 || fallback.Blocks != 1)
                throw new ReviewCheckException("Digest-valid malformed native payload did not use complete original scalar fallback.");
        }
        finally { await SetPayload(payload, originalDigest); }
        var restored = await Read(true); Complete(restored.Snapshot);
        if (restored.Scalar != 0 || restored.Blocks != 1)
            throw new ReviewCheckException("Restored original native payload is not the healthy original route.");
        await Accept("codec-fallback-complete.json");
        }
        if (JsonSerializer.Serialize((await store.GetEntriesAsync(cancellationToken: ct)).OrderBy(entry => entry.Id)) != source)
            throw new ReviewCheckException("Native cache qualification changed authoritative financial rows.");

        if (!await HasAccepted("codec-native-home-complete.json"))
        {
        var originalAccounts = await store.GetAccountsAsync(cancellationToken: ct);
        app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        using (var observed = new IsolatedHomeReadFactory(factory, time, true))
        {
            var nativeStore = new ZananceStore(observed, time, services.GetRequiredService<ICommercialWriteAccessSource>());
            var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, nativeStore, new SeparateHomeReadOperationSource(nativeStore));
            var page = new HomePage(vm); var shell = Shell.Current;
            await shell.Navigation.PushAsync(page, false);
            try
            {
                await page.PendingPublication;
                if (!vm.Loading.IsReady || !vm.HasEntries || page.Handler?.PlatformView is not Android.Views.View native)
                    throw new ReviewCheckException("Original nonempty native Home did not publish.");
                await WaitLayoutAsync(native, ct); RequireHomeWindowFocus();
                var texts = NativeTexts(native).ToHashSet(StringComparer.Ordinal);
                var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
                foreach (var balance in LedgerCalculator.TotalBalances(originalAccounts, original, day))
                    if (!texts.Contains(MoneyText.Format(balance.Value, balance.Key, culture)))
                        throw new ReviewCheckException("Original native nonempty Home balance differs.");
            }
            finally { await shell.Navigation.PopAsync(false); }
        }
        await Accept("codec-native-home-complete.json");
        }
        var result = new { Comparison = comparison, API = api, NativePageBytes = nativePage, ManagedPageBytes = managedPage,
            Entries = fixture.EntryIds.Length, SourceDigest = digest, AllOriginalEntryFieldsPreserved = true,
            ActualNonemptyBlockRead = true, DigestValidMalformedPayloadScalarFallback = true, OriginalPayloadRestored = true,
            OriginalNativeHomeAndFinancialBalanceMatched = true, SyntheticLedgerFixtureRowsPreparedOnce = fixture.EntryIds.Length,
            AcceptedExistingPhoneFinancialWritesReplayed = 0, FinancialCommands = 0, ExistingMainPackageTouched = false,
            NoClockOSAccountOrAuthenticationChange = true, NoKeyOrPrivateSourceExport = true, NoPerformanceOrHardware16KiBAcceptance = true,
            DataModule = typeof(ZananceStore).Module.ModuleVersionId, CoreModule = typeof(LedgerCalculator).Module.ModuleVersionId };
        await File.WriteAllTextAsync(finalPath, JsonSerializer.Serialize(result), ct); return result;
    }
}
