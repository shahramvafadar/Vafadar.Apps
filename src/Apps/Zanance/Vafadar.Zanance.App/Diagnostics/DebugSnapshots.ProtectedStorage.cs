#if DEBUG && WINDOWS
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Continues only retirement of the exact independently created profile after an observed native failure.</summary>
    private static async Task ReviewProtectedProfileRetirementAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Retirement continuation requires its independent identity.");
        var profiles = services.GetRequiredService<Profiles.ProfileService>();
        var prior = Path.Combine(Path.GetDirectoryName(folder)!, "windows-profile-backup-lifecycle");
        var created = JsonSerializer.Deserialize<Profiles.LocalProfile>(await File.ReadAllTextAsync(Path.Combine(prior, "created-profile.json")))
            ?? throw new IOException("Exact synthetic profile authority is absent.");
        if (!profiles.Current.IsMain)
            throw new IOException("Recorded synthetic profile review is not on its original main profile.");
        var before = await ProtectedRowsFingerprintAsync(services);
        if (profiles.All.Contains(created)) await profiles.DeleteAsync(created);
        if (profiles.All.Count != 1 || await ProtectedRowsFingerprintAsync(services) != before)
            throw new IOException("Native retirement changed the remaining main financial rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, "protected-retirement-proof.json"), JsonSerializer.Serialize(new
        {
            ActualRecordedProfileRetired = true, MainFinancialRowsUnchanged = true, FinancialFingerprint = before,
            SuccessfulProfileBackupStagesRepeated = false
        }));
        var theme = services.GetRequiredService<Presentation.ThemeService>();
        foreach (var choice in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        {
            theme.Set(choice); await Task.Delay(250);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "protected-titlebar-" + choice.ToString().ToLowerInvariant() + ".png"));
        }
    }

    /// <summary>Exercises original native profile, safety retention and portable replacement on the retained independent installation.</summary>
    private static async Task ReviewProtectedProfileBackupAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Profile/backup review requires its independent native identity.");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var root = Path.GetDirectoryName(location.Path)!;
        if (!File.Exists(Path.Combine(root, "protected-review-fixture.json")) || !location.IsProtected)
            throw new IOException("Retained protected synthetic fixture is unavailable.");
        var profiles = services.GetRequiredService<Profiles.ProfileService>();
        if (!profiles.Current.IsMain || profiles.All.Count != 1)
            throw new InvalidOperationException("Review requires its original single-profile baseline.");
        var before = await ProtectedRowsFingerprintAsync(services);
        var store = services.GetRequiredService<ZananceStore>();
        var main = profiles.Current;
        var created = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Synthetic protected lifecycle");
        await File.WriteAllTextAsync(Path.Combine(folder, "created-profile.json"), JsonSerializer.Serialize(created));
        if (!await profiles.SwitchToAsync(created) || !location.IsProtected || (await store.GetAccountsAsync()).Count != 0)
            throw new IOException("Original native profile creation/switch did not isolate financial data.");
        await store.SaveAccountAsync(new Core.Accounts.Account
        {
            Name = "Synthetic isolated EUR account", CurrencyCode = "EUR", OpeningBalance = 23456,
            OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime)
        });
        var profileBefore = await ProtectedRowsFingerprintAsync(services);
        var backup = services.GetRequiredService<Vafadar.Backup.IBackupService>();
        var host = services.GetRequiredService<ProtectedStorageHost>();
        var options = services.GetRequiredService<Vafadar.Backup.BackupOptions>();
        var keep = options.MaxBackupsToKeep;
        var safety = new Vafadar.Backup.Storage.LocalFolderBackupStorage(Path.Combine(FileSystem.AppDataDirectory, "backups-safety"));
        var autoPost = services.GetRequiredService<AutoPostProcessor>();
        byte[]? portable = null;
        try
        {
            options.MaxBackupsToKeep = 1;
            for (var count = 0; count < 3; count++)
                await autoPost.RunExclusiveAsync(async () =>
                {
                    var file = await backup.CreateSafetyCopyAsync(safety);
                    await host.AfterUploadAsync(file);
                });
            var retained = await safety.ListAsync();
            if (retained.Count != 1 || !retained[0].FileName.EndsWith(".vbs", StringComparison.Ordinal))
                throw new IOException("Native safety retention did not retain exactly the configured latest copy.");
            await using (var input = await safety.OpenReadAsync(retained[0].Id))
            {
                using var bytes = new MemoryStream(); await input.CopyToAsync(bytes);
                var envelope = bytes.ToArray();
                try
                {
                    var opened = await host.OpenAsync(envelope, CancellationToken.None);
                    try { if (opened.Length == 0) throw new IOException("Retained safety copy did not authenticate."); }
                    finally { CryptographicOperations.ZeroMemory(opened); }
                }
                finally { CryptographicOperations.ZeroMemory(envelope); }
            }
            portable = await backup.CreatePackageAsync();
            var account = (await store.GetAccountsAsync()).Single();
            account.Name = "Synthetic mutation to replace";
            await store.SaveAccountAsync(account);
            if (await ProtectedRowsFingerprintAsync(services) == profileBefore)
                throw new IOException("Explicit synthetic mutation was not stored before replacement.");
            await autoPost.RunExclusiveAsync(() => backup.RestorePackageAsync(portable));
            store.ResetSession();
            if (!location.IsProtected || await ProtectedRowsFingerprintAsync(services) != profileBefore)
                throw new IOException("Original protected package receiver did not restore all original typed rows.");
            if (!await profiles.SwitchToAsync(main) || await ProtectedRowsFingerprintAsync(services) != before)
                throw new IOException("Native return to the main profile changed its financial rows.");
            await File.WriteAllTextAsync(Path.Combine(folder, "protected-backup-stages-proof.json"), JsonSerializer.Serialize(new
            {
                ActualNativeProfileCreatedAndSwitched = true, ActualSafetyCopies = 3,
                RetainedSafetyCopies = retained.Count, RetainedSafetyAuthenticated = true,
                ActualPortableReplacement = true, CompleteTypedRowsRestored = true, FinancialFingerprint = before
            }));
            await profiles.DeleteAsync(created);
            if (profiles.All.Count != 1 || await ProtectedRowsFingerprintAsync(services) != before)
                throw new IOException("Original native retirement changed the remaining main profile.");
            await File.WriteAllTextAsync(Path.Combine(folder, "protected-profile-backup-proof.json"), JsonSerializer.Serialize(new
            {
                ActualNativeProfileCreateSwitchDelete = true, IsolatedFinancialRows = true,
                ActualSafetyCopies = 3, RetainedSafetyCopies = retained.Count, RetainedSafetyAuthenticated = true,
                ActualPortableReplacement = true, RestoredCompleteTypedRows = true,
                MainFinancialRowsUnchanged = true, FinancialFingerprint = before,
                PlaintextPortableFileWritten = false, SecretValuesLogged = false
            }));
        }
        finally
        {
            options.MaxBackupsToKeep = keep;
            if (portable is not null) CryptographicOperations.ZeroMemory(portable);
        }
    }

    /// <summary>Reopens the same recovered native installation without loading or supplying any optional recovery material.</summary>
    private static async Task ReviewProtectedColdReopenAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Cold review requires its independent native identity.");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var host = services.GetRequiredService<ProtectedStorageHost>();
        if (!location.IsProtected || !host.RecoveryEnrolled || !app.StartupStorageRanOffWindowThread)
            throw new InvalidOperationException("Original cold device-key startup did not complete.");
        var baselinePath = Path.Combine(Path.GetDirectoryName(folder)!, "windows-isolated-protected-ui-verified", "protected-storage-proof.json");
        using var baseline = JsonDocument.Parse(await File.ReadAllTextAsync(baselinePath));
        var actual = await ProtectedRowsFingerprintAsync(services);
        if (actual != baseline.RootElement.GetProperty("FinancialFingerprint").GetString())
            throw new IOException("Cold device-key reopen changed original financial rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, "protected-cold-reopen-proof.json"), JsonSerializer.Serialize(new
        {
            ActualColdDeviceKeyStartup = true, OptionalRecoveryMaterialRead = false,
            CompleteFinancialRowsUnchanged = true, FinancialFingerprint = actual
        }));
    }

    /// <summary>Exercises original covered key-loss recovery on the independent native identity using only DPAPI-wrapped synthetic material.</summary>
    private static async Task ReviewProtectedKeyRecoveryAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Key-loss review requires the independent native identity.");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var root = Path.GetDirectoryName(location.Path)!;
        if (!File.Exists(Path.Combine(root, "protected-review-fixture.json")))
            throw new IOException("Synthetic key-loss fixture marker is absent.");
        Startup.StartupViewModel? startup = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            startup = (app.Windows[0].Page as Startup.StartupPage)?.BindingContext as Startup.StartupViewModel;
            if (startup is { IsBusy: false, HasFailed: true }) break;
            await Task.Delay(100);
        }
        if (startup is not { IsBusy: false, HasFailed: true } || location.IsProtected)
            throw new InvalidOperationException("Missing device key did not retain actual unpublished covered startup.");
        var materialFolder = Path.Combine(Path.GetDirectoryName(folder)!, "windows-isolated-protected-ui-verified");
        var binding = JsonSerializer.Deserialize<DatabaseKeyBinding>(await File.ReadAllTextAsync(Path.Combine(materialFolder, "synthetic-recovery-binding.json")))
            ?? throw new IOException("Synthetic recovery binding is absent.");
        var envelope = await File.ReadAllBytesAsync(Path.Combine(materialFolder, "synthetic-recovery.dpapi"));
        byte[]? secret = null;
        try
        {
            secret = WindowsDatabaseKeyProtector.Unprotect(envelope, binding);
            var host = services.GetRequiredService<ProtectedStorageHost>();
            var wrong = secret.ToArray(); wrong[0] ^= 1;
            var rejected = false;
            try { await host.RecoverDeviceKeysAsync("ZRK1-" + Convert.ToBase64String(wrong), CancellationToken.None); }
            catch (CryptographicException) { rejected = true; }
            catch (Microsoft.Data.Sqlite.SqliteException error) when (error.SqliteErrorCode is 26 or 11) { rejected = true; }
            finally { CryptographicOperations.ZeroMemory(wrong); }
            if (!rejected || location.IsProtected) throw new InvalidOperationException("Wrong synthetic recovery code was not refused under the cover.");
            await host.RecoverDeviceKeysAsync("ZRK1-" + Convert.ToBase64String(secret), CancellationToken.None);
            await startup.InitializeAsync();
            if (startup.HasFailed || startup.IsBusy || !location.IsProtected)
                throw new InvalidOperationException("Original covered recovery did not reopen protected financial storage.");
            var baseline = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(materialFolder, "protected-storage-proof.json")));
            using (baseline)
            {
                var expected = baseline.RootElement.GetProperty("FinancialFingerprint").GetString();
                var actual = await ProtectedRowsFingerprintAsync(services);
                if (actual != expected) throw new IOException("Cold key recovery changed original financial rows.");
                await File.WriteAllTextAsync(Path.Combine(folder, "protected-key-recovery-proof.json"), JsonSerializer.Serialize(new
                {
                    ActualMissingKeyCover = true, WrongRecoveryCodeRefused = true, OriginalRecoveryReenrolledDeviceKeys = true,
                    ProtectedStartupReopened = true, CompleteFinancialRowsUnchanged = true, FinancialFingerprint = actual,
                    PlaintextRecoveryMaterialWritten = false, SecretValuesLogged = false
                }));
            }
        }
        finally { CryptographicOperations.ZeroMemory(envelope); if (secret is not null) CryptographicOperations.ZeroMemory(secret); }
    }

    /// <summary>Reviews the actual protected Windows host on an explicitly marked, retained synthetic installation without reseeding it.</summary>
    private static async Task ReviewProtectedStorageAsync(App app, IServiceProvider services, string folder)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var root = Path.GetDirectoryName(location.Path)!;
        var marker = Path.Combine(root, "protected-review-fixture.json");
        if (!File.Exists(marker) || File.ReadAllText(marker).Trim() != "{\"SyntheticProtectedStorageReview\":true}")
            throw new IOException("Protected review requires its explicit independently owned fixture marker.");
        if (!location.IsProtected || !app.StartupStorageRanOffWindowThread)
            throw new InvalidOperationException("Original protected startup did not complete off the window thread.");
        var host = services.GetRequiredService<ProtectedStorageHost>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var access = services.GetRequiredService<AppLockService>();
        if (access.IsLocked)
            throw new InvalidOperationException($"Protected review retains the original access gate: PIN={access.PinEnabled}, unavailable={access.PinUnavailable}, device={access.DeviceLockEnabled}, page={app.Windows[0].Page?.GetType().Name}.");
        if (app.Windows[0].Page is Features.Onboarding.OnboardingPage)
        {
            // The retained fixture came from an intentionally unfinished first-run restore review.
            // Complete only that original restore flag; do not create an additional account or reseed financial rows.
            var store = services.GetRequiredService<ZananceStore>();
            if ((await store.GetAccountsAsync()).Count == 0)
            {
                if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
                    throw new InvalidOperationException("Fresh synthetic setup requires the independent native review identity.");
                await store.SaveAccountAsync(new Core.Accounts.Account
                {
                    Name = "Protected Windows review", CurrencyCode = "EUR", OpeningBalance = 12345,
                    OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime)
                });
            }
            await store.CompleteRestoredOnboardingAsync();
            app.ShowMainShell(); await Task.Delay(300);
        }
        var shell = app.Windows[0].Page as AppShell
            ?? throw new InvalidOperationException($"Protected review requires its completed original Shell, actual page={app.Windows[0].Page?.GetType().Name}.");
        if (shell.CurrentPage is Features.Home.HomePage home) await home.PendingPublication;
        var before = await ProtectedRowsFingerprintAsync(services);
        await shell.GoToAsync(AppShell.SettingsRoute);
        await Task.Delay(350);

        // Keep synthetic recovery material protected by DPAPI between this process and its cold recovery check.
        // No recovery code, DEK, RSA material or financial row is logged or included in a screenshot.
        var enrolledNow = false;
        if (!host.RecoveryEnrolled)
        {
            await app.ShowRecoverySetupAsync();
            var setup = app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() as RecoverySetupPage
                ?? throw new InvalidOperationException("Actual recovery setup did not open.");
            ProtectedRecoveryDraft? draft = null;
            for (var attempt = 0; attempt < 100 && draft is null; attempt++)
            {
                await Task.Delay(100);
                draft = typeof(RecoverySetupPage).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(setup) as ProtectedRecoveryDraft;
            }
            if (draft is null) throw new InvalidOperationException("Actual recovery draft was not published.");
            var binding = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var envelope = WindowsDatabaseKeyProtector.Protect(draft.Secret.Span, binding);
            try
            {
                await File.WriteAllBytesAsync(Path.Combine(folder, "synthetic-recovery.dpapi"), envelope);
                await File.WriteAllTextAsync(Path.Combine(folder, "synthetic-recovery-binding.json"), JsonSerializer.Serialize(binding));
            }
            finally { CryptographicOperations.ZeroMemory(envelope); }
            var toggle = VisualDescendants(setup).OfType<Switch>().Single();
            if (toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch nativeToggle
                || new Microsoft.UI.Xaml.Automation.Peers.ToggleSwitchAutomationPeer(nativeToggle).GetPattern(
                    Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle) is not Microsoft.UI.Xaml.Automation.Provider.IToggleProvider provider)
                throw new InvalidOperationException("Recovery acknowledgment has no native Toggle pattern.");
            provider.Toggle();
            var save = VisualDescendants(setup).OfType<Button>().Single(button => button.Text == Translator.Instance["Common_Save"]);
            InvokeChoiceReview(save, new List<object>());
            for (var attempt = 0; attempt < 100 && app.Windows[0].Page!.Navigation.ModalStack.Contains(setup); attempt++) await Task.Delay(100);
            if (!host.RecoveryEnrolled || app.Windows[0].Page!.Navigation.ModalStack.Contains(setup))
                throw new InvalidOperationException("Native recovery Save did not complete original enrollment.");
            enrolledNow = true;
        }

        var reads = new List<object>();
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            services.GetRequiredService<Presentation.ThemeService>().Set(theme);
            localization.SetLanguage(localization.SupportedLanguages.Single(value => value.CultureName == language));
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            await app.ShowRecoverySetupAsync();
            var setup = app.Windows[0].Page!.Navigation.ModalStack.Last() as RecoverySetupPage
                ?? throw new InvalidOperationException("Enrolled recovery surface is unavailable.");
            await Task.Delay(250);
            if (VisualDescendants(setup).OfType<Editor>().Any(editor => editor.IsVisible))
                throw new InvalidOperationException("Enrolled recovery surface exposes a code editor.");
            foreach (var label in VisualDescendants(setup).OfType<Label>().Where(label => label.IsVisible)) CheckCaption(label, reads);
            await CaptureAsync(app, folder, $"{language}-{theme.ToString().ToLowerInvariant()}-{width}-protected-recovery");
            var back = VisualDescendants(setup).OfType<Button>().Single(button => button.Text == Translator.Instance["Common_Back"]);
            InvokeChoiceReview(back, reads); await Task.Delay(150);
            if (app.Windows[0].Page!.Navigation.ModalStack.Contains(setup)) throw new InvalidOperationException("Native recovery Back retained a retired modal.");
        }
        if (await ProtectedRowsFingerprintAsync(services) != before) throw new IOException("Recovery review changed original financial rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, "protected-storage-proof.json"), JsonSerializer.Serialize(new
        {
            ActualCoveredStartupWorker = true, ActualNativeAcknowledgmentAndSave = enrolledNow,
            RecoveryEnrolled = host.RecoveryEnrolled, Cases = 18, CompleteFinancialRowsUnchanged = true,
            FinancialFingerprint = before, SecretValuesLoggedOrCaptured = false, NativeObservations = reads.Count
        }));
    }

    /// <summary>Hashes every original typed table row through the actual protected application context without exporting its values.</summary>
    private static async Task<string> ProtectedRowsFingerprintAsync(IServiceProvider services)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
        await db.Database.OpenConnectionAsync(); using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var tables = new List<string>();
        using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        var rows = new SortedDictionary<string, List<string>>();
        foreach (var table in tables)
        {
            command.CommandText = "SELECT * FROM \"" + table.Replace("\"", "\"\"") + "\" ORDER BY rowid";
            using var reader = await command.ExecuteReaderAsync(); var values = new List<string>();
            while (await reader.ReadAsync()) values.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray()));
            rows[table] = values;
        }
        var encoded = JsonSerializer.SerializeToUtf8Bytes(rows);
        try { return Convert.ToHexStringLower(SHA256.HashData(encoded)); }
        finally { CryptographicOperations.ZeroMemory(encoded); }
    }
}
#endif
