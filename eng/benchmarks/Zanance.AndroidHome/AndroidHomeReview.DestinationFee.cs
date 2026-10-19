using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original native successful transfer edits on independently owned fictitious SQLite data.</summary>
    private static async Task<object> ReviewDestinationFeeAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>();
        var theme = services.GetRequiredService<ThemeService>();
        var translator = services.GetRequiredService<Translator>();
        var originalLanguage = local.CurrentLanguage.CultureName;
        var originalTheme = theme.Choice;
        var mainStore = services.GetRequiredService<ZananceStore>();
        async Task<string> MainAsync() => JsonSerializer.Serialize(new { Accounts = await mainStore.GetAccountsAsync(cancellationToken: ct),
            Entries = await mainStore.GetEntriesAsync(cancellationToken: ct), Settings = await mainStore.GetSettingsAsync(ct) });
        var mainBefore = await MainAsync();
        var results = new List<JsonElement>();
        var mvid = typeof(AndroidHomeReview).Module.ModuleVersionId;
        var baselineMvid = Platform.CurrentActivity?.Intent?.GetStringExtra("review_fee_baseline_mvid");
        var passedMvid = Platform.CurrentActivity?.Intent?.GetStringExtra("review_fee_passed_mvid");
        var inputMvid = Platform.CurrentActivity?.Intent?.GetStringExtra("review_fee_input_mvid");
        var passedContexts = (Platform.CurrentActivity?.Intent?.GetStringExtra("review_fee_passed_contexts") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var resumeEmpty = Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_fee_resume_empty", false) == true;
        var formerProcessDead = Platform.CurrentActivity?.Intent?.GetIntExtra("review_fee_dead_pid", 0) == 10391;
        if (passedContexts.Any(value => value is not ("en-Light-Simple" or "en-Light-Advanced" or "fa-Light-Simple")))
            throw new ReviewCheckException("Only the actual terminal process's three successful financial cohorts may be retained.");
        await AwaitReimbursementFocusAsync(ct);
        app.OpenReviewShell();
        try
        {
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await ThemeAsync(choice);
                foreach (var language in new[] { "en", "fa", "de" })
                {
                    await SetLanguageAsync(app, local, language, ct);
                    foreach (var mode in new[] { ExperienceMode.Simple, ExperienceMode.Advanced })
                    {
                        var name = language + "-" + choice + "-" + mode;
                        var folder = Path.Combine(root, "destination-fee-" + comparison + "-" + name);
                        var completed = Path.Combine(folder, "complete.json");
                        if (File.Exists(completed))
                        {
                            using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(completed, ct));
                            var priorMvid = prior.RootElement.GetProperty("SourceMvid").GetGuid();
                            if ((priorMvid != mvid && (!passedContexts.Contains(name) || !Guid.TryParse(passedMvid, out var passed) || priorMvid != passed))
                                || prior.RootElement.GetProperty("Context").GetString() != name
                                || prior.RootElement.GetProperty("NativeSaves").GetInt32() != 3)
                                throw new ReviewCheckException("Retained transfer case identity differs; never replay.");
                            // The three successful original Saves precede the separate native finalizer failure.
                            // Read their existing committed rows again; never recreate the fixture or repeat those Saves.
                            using var retained = new ServiceCollection().AddZananceData(Path.Combine(folder, "fictitious-transfer.db")).BuildServiceProvider();
                            var retainedStore = retained.GetRequiredService<ZananceStore>();
                            using var original = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "input.json"), ct));
                            var initial = original.RootElement.GetProperty("Entries").Deserialize<List<LedgerEntry>>()!;
                            var actual = await retainedStore.GetEntriesAsync(cancellationToken: ct);
                            var originalTransfer = initial.Single(value => value.Kind == EntryKind.Transfer);
                            var originalSource = EntryActions.FindTransferFee(originalTransfer, initial)!;
                            var originalDestination = EntryActions.FindDestinationFee(originalTransfer, initial)!;
                            var currentTransfer = actual.Single(value => value.Id == originalTransfer.Id);
                            var currentSource = EntryActions.FindTransferFee(currentTransfer, actual)!;
                            if (currentTransfer.Note != "D369 " + name + " remove" || currentSource.Id != originalSource.Id || currentSource.Amount != 150
                                || actual.Any(value => value.Id == originalDestination.Id)) throw new ReviewCheckException("Completed original fee outcome changed.");
                            currentTransfer.Note = originalTransfer.Note; currentTransfer.UpdatedAt = originalTransfer.UpdatedAt;
                            currentSource.UpdatedAt = originalSource.UpdatedAt;
                            if (JsonSerializer.Serialize(initial.Where(value => value.Id != originalDestination.Id).OrderBy(value => value.Id))
                                != JsonSerializer.Serialize(actual.OrderBy(value => value.Id))
                                || original.RootElement.GetProperty("Stable").GetString() != JsonSerializer.Serialize(new {
                                    Accounts = await retainedStore.GetAccountsAsync(cancellationToken: ct), Settings = await retainedStore.GetSettingsAsync(ct),
                                    Categories = await retainedStore.GetCategoriesAsync(ct) })) throw new ReviewCheckException("Completed case no longer matches original fields.");
                            results.Add(prior.RootElement.Clone());
                            LogReport("destination-fee-retained-" + name + "-" + mvid.ToString("N"), Path.GetFileName(root), new { Context = name, OriginalSourceMvid = priorMvid,
                                SourceMvid = mvid, OriginalCompleteFieldsReadbackPassed = true, OriginalSuccessfulSavesReplayed = false });
                            continue;
                        }
                        var retainedFixture = Directory.Exists(folder);
                        Directory.CreateDirectory(folder);
                        var path = Path.Combine(folder, "fictitious-transfer.db");
                        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
                        var store = fixture.GetRequiredService<ZananceStore>();
                        if (!retainedFixture) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
                        LedgerEntry transfer;
                        string stable;
                        string[]? pendingSchemaTarget = null;
                        if (retainedFixture && !File.Exists(Path.Combine(folder, "input.json")))
                        {
                            // The terminal process partially migrated this exact file but never persisted preparation data.
                            // Inspect under query-only, restore its connection state, then continue only the missing inserts.
                            await using var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                            await db.Database.OpenConnectionAsync(ct);
                            await using var queryOnly = db.Database.GetDbConnection().CreateCommand();
                            queryOnly.CommandText = "PRAGMA query_only";
                            var previousQueryOnly = Convert.ToInt64(await queryOnly.ExecuteScalarAsync(ct));
                            queryOnly.CommandText = "PRAGMA query_only=ON"; await queryOnly.ExecuteNonQueryAsync(ct);
                            try
                            {
                                var accounts = await db.Set<Account>().AsNoTracking().ToListAsync(ct);
                                var entries = await db.Set<LedgerEntry>().AsNoTracking().ToListAsync(ct);
                                var settings = await db.Set<ZananceSettings>().AsNoTracking().ToListAsync(ct);
                                var categories = await db.Set<Category>().CountAsync(ct);
                                var applied = (await db.Database.GetAppliedMigrationsAsync(ct)).ToArray();
                                var expectedMigrations = db.Database.GetMigrations().ToArray();
                                LogReport("destination-fee-migration-state-" + mvid.ToString("N"), Path.GetFileName(root), new {
                                    Context = name, ResumeRequested = resumeEmpty, Applied = applied.Length, Expected = expectedMigrations.Length,
                                    AppliedIsExactPrefix = applied.SequenceEqual(expectedMigrations.Take(applied.Length)), EmptyFinancialPreparation = accounts.Count == 0 && entries.Count == 0 && settings.Count == 0 && categories == 0 });
                                if (name != "fa-Light-Advanced" || !resumeEmpty || !formerProcessDead
                                    || Path.GetFileName(root) != "4594c95b77a647d2b4f783cbea717c61" || comparison != "6f3c7a1691cf4d46b4371dc5cb5e847f"
                                    || accounts.Count != 0 || entries.Count != 0 || settings.Count != 0 || categories != 0
                                    || applied.Length != 43 || expectedMigrations.Length != 50 || !applied.SequenceEqual(expectedMigrations.Take(43)) || Directory.GetFiles(folder, "*.json").Length != 0)
                                    throw new ReviewCheckException("Interrupted preparation differs from the original query-only empty-schema inspection.");
                                queryOnly.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsLock'";
                                var lockRows = 0L; string? lockTimestamp = null;
                                if (Convert.ToInt64(await queryOnly.ExecuteScalarAsync(ct)) == 1)
                                {
                                    queryOnly.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsLock\""; lockRows = Convert.ToInt64(await queryOnly.ExecuteScalarAsync(ct));
                                    if (lockRows == 1)
                                    {
                                        queryOnly.CommandText = "SELECT \"Timestamp\" FROM \"__EFMigrationsLock\" WHERE \"Id\"=1";
                                        lockTimestamp = (await queryOnly.ExecuteScalarAsync(ct))?.ToString();
                                    }
                                    if (lockRows > 1 || (lockRows == 1 && lockTimestamp is null)) throw new ReviewCheckException("Unexpected original migration lock shape.");
                                }
                                if (previousQueryOnly != 0) throw new ReviewCheckException("Original connection was already read-only; preserve it.");
                                queryOnly.CommandText = "PRAGMA query_only=OFF"; await queryOnly.ExecuteNonQueryAsync(ct);
                                if (lockRows == 1)
                                {
                                    // This exact fictitious run's sole former migrator is authoritatively terminal.
                                    // No new migrator has started; remove only its unchanged lock row, never a table or financial row.
                                    queryOnly.CommandText = "DELETE FROM \"__EFMigrationsLock\" WHERE \"Id\"=1 AND \"Timestamp\"=@timestamp";
                                    var parameter = queryOnly.CreateParameter(); parameter.ParameterName = "@timestamp"; parameter.Value = lockTimestamp!; queryOnly.Parameters.Add(parameter);
                                    if (await queryOnly.ExecuteNonQueryAsync(ct) != 1) throw new ReviewCheckException("Original migration lock changed during exclusive diagnostic continuation.");
                                    queryOnly.Parameters.Clear();
                                }
                                LogReport("destination-fee-resumed-preparation-" + mvid.ToString("N"), Path.GetFileName(root), new {
                                    Context = name, OriginalMigratedFileRetained = true, PreviouslyAppliedMigrations = applied.Length, PendingMigrations = expectedMigrations.Length - applied.Length,
                                    ExistingAccounts = accounts.Count, ExistingEntries = entries.Count, ExistingSettings = settings.Count, ExistingCategories = categories,
                                    CompletedMigrationReplayed = false, AbandonedOriginalMigrationLockRowsRemoved = lockRows,
                                    FormerProcessTerminal = 10391, OriginalDataDeletedOrReplaced = false, OnlyPreviouslyUnwrittenPreparationContinues = true });
                                retainedFixture = false;
                                pendingSchemaTarget = expectedMigrations;
                            }
                            finally
                            {
                                queryOnly.CommandText = previousQueryOnly == 0 ? "PRAGMA query_only=OFF" : "PRAGMA query_only=ON";
                                await queryOnly.ExecuteNonQueryAsync(CancellationToken.None);
                            }
                        }
                        if (pendingSchemaTarget is not null)
                        {
                            // GetPendingMigrations skips all original committed history; apply only the remaining seven in the interrupted fixture.
                            await fixture.MigrateLocalDatabaseAsync<ZananceDbContext>(ct);
                            await using var verified = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                            var actualHistory = (await verified.Database.GetAppliedMigrationsAsync(ct)).ToArray();
                            if (!actualHistory.SequenceEqual(pendingSchemaTarget)) throw new ReviewCheckException("Original migration continuation did not reach the exact current history.");
                            LogReport("destination-fee-migration-complete-" + mvid.ToString("N"), Path.GetFileName(root), new {
                                Context = name, Applied = actualHistory.Length, OriginalCommittedPrefixRetained = true, NoDatabaseReset = true });
                        }
                        if (retainedFixture)
                        {
                            if (!Guid.TryParse(baselineMvid, out var oldMvid) || !File.Exists(Path.Combine(folder, "input.json"))
                                || Directory.GetFiles(folder, "*.json").Any(value => Path.GetFileName(value) is not ("input.json" or "retain.json" or "edit.json" or "remove.json")))
                                throw new ReviewCheckException("Only original input and verified stage receipts may continue.");
                            using var input = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "input.json"), ct));
                            var packet = input.RootElement;
                            stable = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
                                Settings = await store.GetSettingsAsync(ct), Categories = await store.GetCategoriesAsync(ct) });
                            var rows = await store.GetEntriesAsync(cancellationToken: ct);
                            var originalMvid = packet.GetProperty("SourceMvid").GetGuid();
                            if ((originalMvid != oldMvid && (!Guid.TryParse(passedMvid, out var passed) || originalMvid != passed)
                                && (name != "fa-Light-Advanced" || !Guid.TryParse(inputMvid, out var priorInput) || originalMvid != priorInput)) || packet.GetProperty("Context").GetString() != name
                                || packet.GetProperty("Stable").GetString() != stable)
                                throw new ReviewCheckException("Original source, context or stable financial fixture changed.");
                            transfer = rows.Single(value => value.Kind == EntryKind.Transfer);
                            var names = new[] { "retain", "edit", "remove" };
                            var last = Array.FindIndex(names, value => transfer.Note == "D369 " + name + " " + value);
                            if (last < 0)
                            {
                                if (Directory.GetFiles(folder, "*.json").Length != 1 || packet.GetProperty("Entries").GetRawText() != JsonSerializer.Serialize(rows))
                                    throw new ReviewCheckException("Original pre-Save fixture changed without a complete committed stage.");
                            }
                            else
                            {
                                var initial = packet.GetProperty("Entries").Deserialize<List<LedgerEntry>>()!;
                                var initialTransfer = initial.Single(value => value.Id == transfer.Id);
                                var initialSource = EntryActions.FindTransferFee(initialTransfer, initial)!;
                                var initialDestination = EntryActions.FindDestinationFee(initialTransfer, initial)!;
                                var sourceFee = EntryActions.FindTransferFee(transfer, rows)!;
                                var destinationFee = EntryActions.FindDestinationFee(transfer, rows);
                                var fee = last == 0 ? 75L : last == 1 ? 125L : 0L;
                                if (sourceFee.Id != initialSource.Id || sourceFee.Amount != 150 || rows.Count != initial.Count - (last == 2 ? 1 : 0)
                                    || (last == 2 ? destinationFee is not null : destinationFee?.Id != initialDestination.Id || destinationFee.Amount != fee))
                                    throw new ReviewCheckException("Committed original stage fee/row identity differs.");
                                transfer.Note = initialTransfer.Note; transfer.UpdatedAt = initialTransfer.UpdatedAt;
                                sourceFee.UpdatedAt = initialSource.UpdatedAt;
                                if (destinationFee is not null) { destinationFee.Amount = initialDestination.Amount; destinationFee.UpdatedAt = initialDestination.UpdatedAt; }
                                if (JsonSerializer.Serialize(initial.Where(value => last != 2 || value.Id != initialDestination.Id).OrderBy(value => value.Id))
                                    != JsonSerializer.Serialize(rows.OrderBy(value => value.Id)))
                                    throw new ReviewCheckException("Committed stage changed an unrelated serialized financial field.");
                                for (var index = 0; index < names.Length; index++)
                                {
                                    var stagePath = Path.Combine(folder, names[index] + ".json");
                                    if (index > last) { if (File.Exists(stagePath)) throw new ReviewCheckException("A stage receipt exceeds actual committed state."); continue; }
                                    if (!File.Exists(stagePath))
                                    {
                                        // A persisted note/fee can prove data state but not a native activation or returned navigation.
                                        // Preserve the original file and refuse acceptance when that original runtime receipt is missing.
                                        throw new ReviewCheckException("Original native stage receipt is missing; committed rows alone do not qualify input/navigation.");
                                    }
                                    using var proof = JsonDocument.Parse(await File.ReadAllTextAsync(stagePath, ct)); var value = proof.RootElement;
                                    if (value.GetProperty("Stage").GetString() != names[index] || !value.GetProperty("ActualNativeSave").GetBoolean()
                                        || !value.GetProperty("ActualNativeInputBinding").GetBoolean() || value.GetProperty("TransferId").GetGuid() != transfer.Id
                                        || value.GetProperty("SourceFeeId").GetGuid() != initialSource.Id || value.GetProperty("ExpectedDestinationFee").GetInt64() != (index == 0 ? 75 : index == 1 ? 125 : 0)
                                        || !value.GetProperty("EveryOtherSerializedLedgerFieldPreserved").GetBoolean() || !value.GetProperty("AccountsCategoriesSettingsPreserved").GetBoolean())
                                        throw new ReviewCheckException("Original successful stage receipt differs from its committed source/input.");
                                }
                            }
                        }
                        else
                        {
                            await store.EnsureDefaultCategoriesAsync(ct);
                            var source = new Account { Name = "D369 fictitious source", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1), OpeningBalance = 100000 };
                            var destination = new Account { Name = "D369 fictitious destination", CurrencyCode = "EUR", OpeningDate = source.OpeningDate };
                            await store.SaveAccountAsync(source, ct); await store.SaveAccountAsync(destination, ct);
                            await store.UpdateSettingsAsync(value => { value.Mode = mode; value.DefaultAccountId = source.Id; }, ct);
                            var feeCategory = (await store.GetCategoriesAsync(ct)).Single(value => value.SystemKey == DefaultCategories.Fees).Id;
                            transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = source.Id, ToAccountId = destination.Id,
                                Amount = 20000, Date = source.OpeningDate, Title = "D369 original transfer", Note = "Original note", Tags = ["fictitious", "preserve"] };
                            var sourceFee = EntryActions.SyncTransferFee(transfer, null, 150, feeCategory)!;
                            var destinationFee = EntryActions.SyncDestinationFee(transfer, null, 75, feeCategory)!;
                            var unrelated = new LedgerEntry { Kind = EntryKind.Expense, AccountId = source.Id, Amount = 321, Date = source.OpeningDate,
                                CategoryId = feeCategory, Title = "D369 unrelated row", Note = "Must remain exact", Tags = ["unrelated"] };
                            if (!(await store.SaveEntriesAsync([transfer, sourceFee, destinationFee, unrelated], [], ct)).Succeeded)
                                throw new ReviewCheckException("Fictitious transfer preparation failed; preserve its files.");
                            stable = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
                                Settings = await store.GetSettingsAsync(ct), Categories = await store.GetCategoriesAsync(ct) });
                            await File.WriteAllTextAsync(Path.Combine(folder, "input.json"), JsonSerializer.Serialize(new { SourceMvid = mvid,
                                DataMvid = typeof(ZananceStore).Module.ModuleVersionId, Context = name, Stable = stable,
                                Entries = await store.GetEntriesAsync(cancellationToken: ct) }), ct);
                        }
                        var accountRows = (await store.GetAccountsAsync(cancellationToken: ct)).ToDictionary(value => value.Id);
                        var sourceAccount = accountRows[transfer.AccountId]; var destinationAccount = accountRows[transfer.ToAccountId!.Value];
                        var stages = new List<object>();
                        foreach (var stage in new[] { "retain", "edit", "remove" })
                        {
                            var retainedStage = Path.Combine(folder, stage + ".json");
                            if (File.Exists(retainedStage))
                            {
                                using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(retainedStage, ct)); stages.Add(retained.RootElement.Clone());
                                LogReport("destination-fee-stage-retained-" + name + "-" + stage + "-" + mvid.ToString("N"), Path.GetFileName(root),
                                    new { Context = name, OriginalInputSourceBound = true, ActualCompleteCommittedStateMatched = true, Stage = retained.RootElement.Clone(), NativeSaveReplayed = false });
                                continue;
                            }
                            await AwaitReimbursementFocusAsync(ct);
                            var before = await store.GetEntriesAsync(cancellationToken: ct);
                            var originalTransfer = before.Single(value => value.Id == transfer.Id);
                            var originalDestination = EntryActions.FindDestinationFee(originalTransfer, before)!;
                            var originalSource = EntryActions.FindTransferFee(originalTransfer, before)!;
                            var page = await OpenAsync(store, fixture.GetRequiredService<ReceiptWorkStore>(), new() { ["id"] = transfer.Id });
                            var vm = (EntryEditorViewModel)page.BindingContext;
                            if (!vm.IsTransfer || vm.Kind != EntryKind.Transfer || !vm.ShowDestinationFee || !vm.ShowDetails)
                                throw new ReviewCheckException("Original stored destination fee must be visible in both modes.");
                            var field = page.FindByName<Microsoft.Maui.Controls.Entry>("DestinationFeeInput");
                            await UntilAsync(() => field.Handler?.PlatformView is Android.Widget.EditText { IsAttachedToWindow: true }, ct);
                            var nativeField = (Android.Widget.EditText)field.Handler!.PlatformView!;
                            await RevealAsync(nativeField);
                            if (EditorNativeDescription(nativeField) != translator["Entry_DestinationFee"]
                                || nativeField.Text != vm.DestinationFeeText || !nativeField.Enabled)
                                throw new ReviewCheckException("Original native destination fee label/value differs.");
                            CheckEffect(originalDestination.Amount);
                            var targetFee = stage == "retain" ? 75L : stage == "edit" ? 125L : 0L;
                            if (stage != "retain")
                            {
                                var input = stage == "remove" ? (language == "fa" ? "0" : "") : MoneyText.ForInput(targetFee, "EUR", local.CurrentCulture);
                                nativeField.Text = input;
                                await UntilAsync(() => vm.DestinationFeeText == input, ct);
                            }
                            CheckEffect(targetFee);
                            var note = "D369 " + name + " " + stage;
                            var noteField = page.FindByName<Editor>("NoteEditor");
                            await UntilAsync(() => noteField.Handler?.PlatformView is Android.Widget.EditText, ct);
                            var nativeNote = (Android.Widget.EditText)noteField.Handler!.PlatformView!;
                            await RevealAsync(nativeNote); nativeNote.Text = note;
                            await UntilAsync(() => vm.Note == note && vm.CanSave, ct);
                            var button = ReimbursementViews(page).OfType<Button>().Single(value => ReferenceEquals(value.Command, vm.SaveCommand));
                            if (button.Handler?.PlatformView is not Android.Views.View nativeSave) throw new ReviewCheckException("Original native Save is absent.");
                            await RevealAsync(nativeSave);
                            if ((EditorNativeDescription(nativeSave) ?? (nativeSave as Android.Widget.TextView)?.Text) != translator["Common_Save"] || !nativeSave.Enabled)
                                throw new ReviewCheckException("Original native Save name/enablement differs.");
                            if (!nativeSave.PerformClick() || vm.SaveCommand.ExecutionTask is not { } saving)
                                throw new ReviewCheckException("Original native Save did not begin.");
                            await saving.WaitAsync(ct);
                            await UntilAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
                            var after = await store.GetEntriesAsync(cancellationToken: ct);
                            var saved = after.Single(value => value.Id == transfer.Id);
                            var savedSource = EntryActions.FindTransferFee(saved, after);
                            var savedDestination = EntryActions.FindDestinationFee(saved, after);
                            if (vm.SaveError is not null || saved.Note != note || savedSource?.Id != originalSource.Id || savedSource.Amount != 150
                                || after.Count(value => value.Kind == EntryKind.Transfer) != 1
                                || after.Count != before.Count - (targetFee == 0 ? 1 : 0)
                                || (targetFee == 0 ? savedDestination is not null : savedDestination?.Id != originalDestination.Id || savedDestination.Amount != targetFee))
                                throw new ReviewCheckException("Original successful edit changed fee identity/value or transfer count.");
                            // Compare every serialized ledger field, allowing only the explicitly edited note/fee and their audit advances.
                            saved.Note = originalTransfer.Note; saved.UpdatedAt = originalTransfer.UpdatedAt;
                            savedSource.UpdatedAt = originalSource.UpdatedAt;
                            if (savedDestination is not null) { savedDestination.Amount = originalDestination.Amount; savedDestination.UpdatedAt = originalDestination.UpdatedAt; }
                            if (JsonSerializer.Serialize(before.Where(value => targetFee != 0 || value.Id != originalDestination.Id).OrderBy(value => value.Id))
                                != JsonSerializer.Serialize(after.OrderBy(value => value.Id))
                                || stable != JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
                                    Settings = await store.GetSettingsAsync(ct), Categories = await store.GetCategoriesAsync(ct) }))
                                throw new ReviewCheckException("Unrelated original financial/settings fields changed.");
                            var proof = new { Stage = stage, ActualNativeSave = true, ActualNativeInputBinding = true, ExpectedDestinationFee = targetFee,
                                TransferId = transfer.Id, SourceFeeId = originalSource.Id, DestinationFeeId = targetFee == 0 ? (Guid?)null : originalDestination.Id,
                                EveryOtherSerializedLedgerFieldPreserved = true, AccountsCategoriesSettingsPreserved = true, OriginalNavigationReturned = true };
                            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(proof), ct);
                            LogReport("destination-fee-stage-" + name + "-" + stage, Path.GetFileName(root), proof);
                            stages.Add(proof);

                            void CheckEffect(long fee)
                            {
                                var expected = translator.Format("Entry_EffectFrom", MoneyText.Format(-20150, "EUR", local.CurrentCulture), sourceAccount.Name)
                                    + " \u00b7 " + translator.Format("Entry_EffectTo", MoneyText.Format(20000 - fee, "EUR", local.CurrentCulture, showPlus: true), destinationAccount.Name);
                                if (vm.EffectText != expected)
                                {
                                    _balanceEvidence = new { Context = name, Stage = stage, ExpectedFee = fee,
                                        Culture = local.CurrentCulture.Name, Language = local.CurrentLanguage.CultureName,
                                        vm.Kind, vm.AmountText, vm.FeeText, vm.DestinationFeeText, vm.ToAmountText, vm.ShowDestinationFee,
                                        ActualEffect = vm.EffectText, ExpectedEffect = expected, SourceMatches = vm.Account?.Id == sourceAccount.Id,
                                        DestinationMatches = vm.ToAccount?.Id == destinationAccount.Id };
                                    throw new ReviewCheckException("Original effect does not include both fee amounts.");
                                }
                            }
                        }
                        // Reopening a removed fee and a new transfer must preserve Simple/Advanced creation policy without writes.
                        var finalRows = JsonSerializer.Serialize(await store.GetEntriesAsync(cancellationToken: ct));
                        foreach (var query in new[] { new Dictionary<string, object> { ["id"] = transfer.Id }, new() { ["kind"] = "Transfer" } })
                        {
                            var page = await OpenAsync(store, fixture.GetRequiredService<ReceiptWorkStore>(), query);
                            var vm = (EntryEditorViewModel)page.BindingContext;
                            if (vm.ToAccount?.Id != destinationAccount.Id) throw new ReviewCheckException("Original transfer destination default differs.");
                            if (vm.ShowDestinationFee != (mode == ExperienceMode.Advanced)) throw new ReviewCheckException("Original fee-free creation policy differs.");
                            await UntilAsync(() => page.Handler?.PlatformView is Android.Views.View { HasWindowFocus: true }, ct);
                            var cancel = ReimbursementViews(page).OfType<Button>().Single(value => ReferenceEquals(value.Command, vm.CancelCommand));
                            if (cancel.Handler?.PlatformView is not Android.Views.View nativeCancel) throw new ReviewCheckException("Original native Cancel missing.");
                            await RevealAsync(nativeCancel);
                            if (!nativeCancel.PerformClick()) throw new ReviewCheckException("Original native Cancel refused.");
                            await UntilAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
                        }
                        if (finalRows != JsonSerializer.Serialize(await store.GetEntriesAsync(cancellationToken: ct))) throw new ReviewCheckException("Fee-free inspection wrote financial data.");
                        var report = new { SourceMvid = mvid, DataMvid = typeof(ZananceStore).Module.ModuleVersionId, Context = name,
                            NativeSaves = 3, Stages = stages, FeeFreeCreationPolicyPreserved = true, NoMainInstallationWrites = true };
                        await File.WriteAllTextAsync(completed, JsonSerializer.Serialize(report), ct);
                        results.Add(JsonSerializer.SerializeToElement(report));
                        LogReport("destination-fee-" + name, Path.GetFileName(root), report);
                    }
                }
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            LogReport("destination-fee-primary-" + mvid.ToString("N"), Path.GetFileName(root), new { Comparison = comparison,
                ErrorType = error.GetType().FullName, Stack = error.StackTrace, ReviewCheck = error is ReviewCheckException check ? check.Message : null });
            throw;
        }
        finally
        {
            // A diagnostic draft must leave navigation before display restoration; otherwise the normal dirty-form guard defers rebuilding.
            while (Shell.Current.CurrentPage is EntryEditorPage) await Shell.Current.GoToAsync("..");
            await SetLanguageAsync(app, local, originalLanguage, ct); await ThemeAsync(originalTheme);
        }
        if (mainBefore != await MainAsync()) throw new ReviewCheckException("Independent original application data changed.");
        return new { Comparison = comparison, Contexts = results, NativeSaves = results.Count * 3, OriginalIndependentMainDataPreserved = true,
            OriginalLanguageRestored = originalLanguage, OriginalThemeRestored = originalTheme.ToString(), MainPackageTouched = false,
            Boundary = "Original Release/AOT native editor Save and ledger fields on fictitious standalone SQLite; not phone, encryption, full-device or release acceptance." };

        async Task<EntryEditorPage> OpenAsync(ZananceStore store, ReceiptWorkStore receiptWork, Dictionary<string, object> query)
        {
            await UntilAsync(() => Shell.Current.CurrentPage?.Handler?.PlatformView is Android.Views.View { HasWindowFocus: true }, ct);
            var vm = ActivatorUtilities.CreateInstance<EntryEditorViewModel>(services, store, receiptWork);
            var page = new EntryEditorPage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            var route = "d369-transfer-" + Guid.NewGuid().ToString("N");
            Routing.RegisterRoute(route, new DestinationFeePageFactory(page));
            // Shell owns query delivery. Manual PushAsync delivers an empty query and would replace the prepared transfer with a new expense.
            try { await Shell.Current.GoToAsync(route, query); }
            finally { Routing.UnRegisterRoute(route); }
            var loading = typeof(EntryEditorViewModel).GetField("_loading", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new ReviewCheckException("Original asynchronous loader state missing.");
            await UntilAsync(() => vm.IsTransfer && vm.Account is not null && vm.ToAccount is not null && loading.GetValue(vm) is false, ct);
            return page;
        }
        async Task RevealAsync(Android.Views.View target)
        {
            await WaitLayoutAsync(target, ct);
            target.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, target.Width, target.Height), true);
            await Task.Delay(100, ct); await WaitLayoutAsync(target, ct);
            using var visible = new Android.Graphics.Rect();
            if (!target.HasWindowFocus || !target.GetGlobalVisibleRect(visible) || visible.Width() < 1 || visible.Height() < target.Height - 1)
                throw new ReviewCheckException("Original native field/action is not fully visible and focused.");
        }
        async Task ThemeAsync(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Changed(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Changed;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Changed; }
        }
        static async Task UntilAsync(Func<bool> ready, CancellationToken cancellation)
        {
            for (var attempt = 0; attempt < 300; attempt++) { if (ready()) return; await Task.Delay(50, cancellation); }
            throw new ReviewCheckException("Original native transfer transition did not complete; preserve fixture and process.");
        }
    }

    /// <summary>Supplies only the original editor with the isolated fixture; Shell retains its original query/navigation contract.</summary>
    private sealed class DestinationFeePageFactory(EntryEditorPage page) : RouteFactory
    {
        /// <inheritdoc />
        public override Element GetOrCreate() => page;
        /// <inheritdoc />
        public override Element GetOrCreate(IServiceProvider services) => page;
    }
}
