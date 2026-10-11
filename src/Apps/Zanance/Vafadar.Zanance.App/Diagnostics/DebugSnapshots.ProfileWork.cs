#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Profiles;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // Keep measured COM wrappers alive through this short diagnostic process. Native glyph/automation reads
    // must not finalize after their old XAML page is retired during the language walk-through. This is no
    // production GC change; observed WinRT finalizer failure remains negative evidence until normal exit passes.
    private static readonly List<object> ProfileNativeReads = [];

    /// <summary>Verified-looking fictitious facts confined to the independent snapshot catalogue, never release DI.</summary>
    private sealed class ProfileReviewAccess : IProfileWorkAccessSource
    {
        internal ProfileWorkAccess Current = ProfileWorkAccess.Inactive;
        public ProfileWorkAccess Capture(string cataloguePath) => Current;
    }
    /// <summary>Rejects unexpected financial-profile effects in this selection-only native review.</summary>
    private sealed class ProfileReviewRuntime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new InvalidOperationException("Unexpected profile switch in selection-only review.");
        public Task MigrateAsync() => throw new InvalidOperationException("Unexpected destination migration.");
        public Task<bool> AuthenticateAsync(string reason) => throw new InvalidOperationException("Unexpected authentication.");
        public Task ReloadLockAsync() => throw new InvalidOperationException("Unexpected lock reload.");
        public void DismissUndo() => throw new InvalidOperationException("Unexpected Undo dismissal.");
    }

    // Actual page/commands/native patterns over an independently owned catalogue; selection changes no financial row.
    private static async Task ReviewProfileWorkAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var root = Path.Combine(folder, "profile-work-fixture"); Directory.CreateDirectory(root);
        var path = Path.Combine(root, "zanance.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await store.SaveAccountAsync(new Account { Name = "Fictitious retained original account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 12) });
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete original profile review schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var preferences = new InMemorySettingsStore(); var access = new ProfileReviewAccess();
            var profiles = new ProfileService(fixture.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), store, preferences,
                new ProfileReviewRuntime(), translator, access);
            for (var i = 0; i < 3; i++) profiles.Create("Fictitious " + i + " complete household identity");
            var originalPacket = preferences.Get("profiles.work.catalogue"); var originals = profiles.All.ToArray();
            var vm = new ProfilesViewModel(profiles, translator, services.GetRequiredService<Interaction.IAppInteraction>(), services.GetRequiredService<Interaction.IAppFlowHost>());
            var page = new ProfilesPage(vm, localization); ProfileNativeReads.Add(page); await Shell.Current.Navigation.PushAsync(page); await vm.EnsureLoadedAsync(); await Task.Delay(250);
            var observations = new List<object>();
            async Task CheckAsync(string scenario)
            {
                if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new InvalidOperationException("Retired profile review page.");
                await Task.Delay(250);
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label, ProfileNativeReads);
                foreach (var button in VisualDescendants(page).OfType<Button>().Where(Visible).Where(b => b.CommandParameter is ProfileRow
                    || WrappingActions(page).Any(action => Visible(action) && VisualDescendants(action).Contains(b))))
                {
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44)
                        throw new InvalidOperationException("A profile action lost its native target size.");
                    var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)
                        ?? throw new InvalidOperationException("Missing profile action peer.");
                    if (button.CommandParameter is ProfileRow row && peer.GetName() != (button.Command == vm.ToggleCommand ? row.ChoiceDescription : row.AccessibilityText))
                        throw new InvalidOperationException("Original navigation/choice names were mixed.");
                    ProfileNativeReads.Add(native); ProfileNativeReads.Add(peer);
                    observations.Add(new { scenario, Name = peer.GetName(), native.ActualWidth, native.ActualHeight, native.IsEnabled });
                }
                if (await StoredAsync() != before) throw new InvalidOperationException("Profile selection changed complete original financial rows.");
                await CaptureAsync(app, folder, language + "-profile-work-" + scenario);
            }
            void Invoke(CommunityToolkit.Mvvm.Input.IRelayCommand command) => InvokeChoiceReview(VisualDescendants(WrappingActions(page).Single(a => a.Command == command && Visible(a))).OfType<Button>().Single(), ProfileNativeReads);
            async Task ToggleAsync(ProfileRow row)
            {
                var button = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.CommandParameter, row) && b.Command == vm.ToggleCommand);
                await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().Single(), button); InvokeChoiceReview(button, ProfileNativeReads); await Task.Delay(100);
            }
            await CheckAsync("inactive-originals");
            if (vm.CanChoose || !vm.CanAdd || vm.Profiles.Count != 4 || preferences.Get("profiles.work.catalogue") != originalPacket)
                throw new InvalidOperationException("Inactive registration changed original profiles.");
            access.Current = new(path, new(QuotaScopeKind.Device, Guid.Parse("71717171-8282-9393-a4a4-b5b5b5b5b5b5")),
                new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("61616161-7272-8383-9494-a5a5a5a5a5a5"))));
            Invoke(vm.ReloadCommand); if (vm.ReloadCommand.ExecutionTask is { } read) await read; await CheckAsync("bounded-missing-choice");
            if (!vm.CanChoose || vm.CanAdd || vm.Profiles.Any(row => row.IsSelected)) throw new InvalidOperationException("Profile choice guessed an original subset.");
            await ToggleAsync(vm.Profiles[^1]); var chosenId = vm.Profiles[^1].Profile.Id; await ToggleAsync(vm.Profiles[0]);
            if (!vm.OverCapacity || vm.CanSave) throw new InvalidOperationException("Over-capacity profile draft remained submittable.");
            await CheckAsync("over-capacity-draft"); await ToggleAsync(vm.Profiles[0]);
            Invoke(vm.SaveChoiceCommand); if (vm.SaveChoiceCommand.ExecutionTask is { } saving) await saving; await CheckAsync("saved-original-choice");
            if (!profiles.ReadWorkReview().CanUse(chosenId) || vm.IsDirty || !profiles.All.SequenceEqual(originals) || !profiles.Current.IsMain)
                throw new InvalidOperationException("Saving device choice changed an original profile or open file.");
            await ToggleAsync(vm.Profiles[^1]); await ToggleAsync(vm.Profiles[0]); profiles.Rename(profiles.All[0], "Fictitious changed original main");
            var stalePacket = preferences.Get("profiles.work.catalogue"); Invoke(vm.SaveChoiceCommand); if (vm.SaveChoiceCommand.ExecutionTask is { } rejected) await rejected;
            if (vm.CanInteract || !vm.IsDirty || !vm.Profiles[0].IsSelected || preferences.Get("profiles.work.catalogue") != stalePacket)
                throw new InvalidOperationException("Rejected original Save lost the exact draft or repeated a write.");
            await CheckAsync("rejected-original-draft");
            // The reviewed draft stays until the actual native discard dialog accepts explicit read-only Reload.
            async Task AnswerAsync(bool discard)
            {
                if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } nativeRoot })
                    throw new InvalidOperationException("Missing own native root.");
                Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
                for (var attempt = 0; attempt < 100 && dialog is null; attempt++)
                { dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(nativeRoot).Select(p => p.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault(); if (dialog is null) await Task.Delay(50); }
                if (dialog is null || !vm.IsBusy || vm.CanLeave) throw new InvalidOperationException("Missing guarded original discard dialog.");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-profile-work-discard-" + discard + ".png"));
                var answer = FindNativeButton(dialog, translator[discard ? "ActiveItems_Discard" : "Common_Cancel"]) ?? throw new InvalidOperationException("Missing original native dialog answer.");
                var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(answer) as Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
                if (peer?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                    throw new InvalidOperationException("Missing native discard pattern.");
                ProfileNativeReads.Add(dialog); ProfileNativeReads.Add(answer); ProfileNativeReads.Add(peer); ProfileNativeReads.Add(invoke);
                invoke.Invoke(); await Task.Delay(150);
            }
            Invoke(vm.ReloadCommand); await AnswerAsync(false); if (vm.ReloadCommand.ExecutionTask is { } cancelled) await cancelled;
            if (!vm.IsDirty || !vm.Profiles[0].IsSelected) throw new InvalidOperationException("Native Cancel lost the original draft.");
            Invoke(vm.ReloadCommand); await AnswerAsync(true); if (vm.ReloadCommand.ExecutionTask is { } reloaded) await reloaded;
            if (vm.IsDirty || !vm.CanInteract || preferences.Get("profiles.work.catalogue") != stalePacket) throw new InvalidOperationException("Native Retry repeated a choice write.");
            await CheckAsync("read-only-reload");
            preferences.Set("profiles.work.catalogue", originalPacket); if (!profiles.All.SequenceEqual(originals) || await StoredAsync() != before)
                throw new InvalidOperationException("Complete independent profile restoration failed.");
            InvokeSnapshotBack(page); await Task.Delay(150);
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-profile-work-proof.json"), JsonSerializer.Serialize(new
            { ActualOriginalProfileNavigationAndChoiceTargets = true, OriginalMainIdPreserved = true, InactiveReleaseRegistration = true,
                SelectedLaterOriginalWithoutFinancialSave = true, SeparateSavedWorkAndUnsavedDraft = true, GuardedNativeDiscardCancelRetry = true,
                Complete25TablesUnchanged = true, OriginalCataloguePacketRestored = true, observations }));
        }
    }
}
#endif
