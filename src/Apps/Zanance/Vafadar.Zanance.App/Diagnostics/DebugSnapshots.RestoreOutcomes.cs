#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only UI/security completion is fictitious; source/safety recovery and automatic posting stay original.</summary>
    private sealed class RestoreReviewRuntime(IBackupService backup, AutoPostProcessor autoPost, string safety) : IBackupRestoreRuntime
    {
        internal bool RefuseLock = true;
        internal int Primary, Safety, Opened;
        public Task<bool> ConfirmReplacementAsync() => Task.FromResult(true);
        public Task<bool> AuthenticateAsync() => Task.FromResult(true);
        public async Task CreateSafetyCopyAsync() { Safety++; await backup.CreateSafetyCopyAsync(new LocalFolderBackupStorage(safety)); }
        public Task RestorePrimaryAsync(byte[] package, string? password) { Primary++; return autoPost.RunExclusiveAsync(() => backup.RestorePackageAsync(package, password)); }
        public Task ApplyDisplayAsync() => Task.CompletedTask;
        public Task CompleteOnboardingAsync() => Task.CompletedTask;
        public Task ReloadLockAsync() => RefuseLock ? Task.FromException(new IOException("Fictitious native lock publication refusal.")) : Task.CompletedTask;
        public async Task RefreshFinancialWorkAsync(DateOnly day) => await autoPost.RunAsync(day);
        public Task OpenRestoredProfileAsync(bool onboarding) { Opened++; return Task.CompletedTask; }
    }

    // D-167 / AT-169: original page/session bindings and real independently owned source replacement.
    private static async Task ReviewRestoreOutcomesAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "restore-outcomes.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        await ApplyOwnedReviewModeAsync(store); var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious complete restored account", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 12345 };
        await store.SaveAccountAsync(account); var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete pre-Save fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var backup = new BackupService(fixture.GetServices<IBackupSource>(), fixture.GetServices<IBackupSummaryProvider>(),
            new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new(0, 1, 0), "Fictitious device", "Development"), new InMemorySettingsStore(), TimeProvider.System, new());
        var expected = await StoredAsync(); var package = await backup.CreatePackageAsync();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var changed = account.CopyForEditing(); changed.Name = "Fictitious live account before restore"; changed.OpeningBalance = 54321;
            await store.SaveAccountAsync(changed);
            var runtime = new RestoreReviewRuntime(backup, fixture.GetRequiredService<AutoPostProcessor>(), Path.Combine(folder, "safety-" + language));
            var vm = new BackupViewModel(backup, fixture.GetRequiredService<AutoPostProcessor>(), services.GetRequiredService<ReminderService>(),
                services.GetRequiredService<AppLockService>(), translator, new DateFormatter(localization), localization, TimeProvider.System,
                services.GetRequiredService<Vafadar.Authentication.Maui.CloudSignIn>(), fixture, runtime) { IndependentRestoreReview = true };
            var page = new BackupPage(vm); ProfileNativeReads.Add(page); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(200);
            using (var stream = new MemoryStream(package, writable: false)) await vm.Restore.PrepareAsync(stream, "Fictitious original backup.zanbak");
            await Task.Delay(200);
            async Task InvokeAsync(CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command)
            {
                await Task.Delay(150); var action = WrappingActions(page).Single(item => item.Command == command);
                CheckCaption(VisualDescendants(action).OfType<Label>().Single(), ProfileNativeReads);
                InvokeChoiceReview(VisualDescendants(action).OfType<Button>().Single(), ProfileNativeReads);
                if (command.ExecutionTask is { } running) await running; await Task.Delay(200);
            }
            await CaptureAsync(app, folder, language + "-restore-outcomes-preview");
            await InvokeAsync(vm.Restore.RestoreCommand);
            if (!vm.Restore.HasCompleted || vm.Restore.CanRestore || vm.CanLeave || vm.CanUseBackupTools || runtime.Primary != 1 || runtime.Safety != 1 || expected != await StoredAsync())
                throw new InvalidOperationException("The actual original restore result was lost or remained resubmittable.");
            foreach (var text in new[] { vm.Restore.Error, vm.Restore.Result }.Where(text => text is not null))
                CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == text), ProfileNativeReads);
            if (VisualDescendants(page).OfType<Entry>().First().Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox { IsEnabled: false })
                throw new InvalidOperationException("Accepted restore retained writable native backup inputs.");
            await CaptureAsync(app, folder, language + "-restore-outcomes-completed-followup-failed");
            runtime.RefuseLock = false; await InvokeAsync(vm.Restore.FinishCommand);
            if (!vm.CanLeave || vm.Restore.CanFinish || runtime.Primary != 1 || runtime.Safety != 1 || runtime.Opened != 1 || expected != await StoredAsync())
                throw new InvalidOperationException("The native completion retry replaced original data again.");
            await CaptureAsync(app, folder, language + "-restore-outcomes-opened");
            await Shell.Current.Navigation.PopModalAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-restore-outcomes-proof.json"), JsonSerializer.Serialize(new
            { ActualOriginalPageSessionAndNativeRestoreFinish = true, OriginalSafetyCopyAndAllSourceReplacement = true,
                CompletePinnedReasonResultGrowingTarget = true, AcceptedInputAndNativeBackAffordanceFrozen = true,
                NativeFinishWithoutRepeatOrExtraAccount = true, Complete25OriginalTablesRestored = true,
                ExplicitFictitiousSecurityConsentDisplayAndOpenEffects = true, IndependentDiscoverySkipsProviderIdentity = true }));
        }
    }
}
#endif
