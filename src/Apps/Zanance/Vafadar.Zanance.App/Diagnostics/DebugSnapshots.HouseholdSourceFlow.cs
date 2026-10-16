#if DEBUG && WINDOWS
using System.Security.Cryptography;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Actual original main app source/profile/copy/publication against only an independently owned fictional local service.</summary>
    private static async Task ReviewHouseholdSourceFlowAsync(App app, IServiceProvider services, string folder)
    {
        const string package = "pro.vafadar.zanance.householdsourceflow";
        if (AppInfo.Current.PackageName != package || !FileSystem.AppDataDirectory.Contains(package, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Independent protected source flow installation required.");
        var fixtureRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_SOURCE_FIXTURE") ?? throw new IOException("Original local fixture required."));
        var allowed = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "artifacts", "household-network")) + Path.DirectorySeparatorChar;
        if (!fixtureRoot.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) ||
            await File.ReadAllTextAsync(Path.Combine(fixtureRoot, "fixture.marker")) != "Fictitious protected household HTTP process qualification v1")
            throw new IOException("Only an original fictional household service may be used.");
        for (var item = new DirectoryInfo(fixtureRoot); item is not null; item = item.Parent)
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected fictional service refused.");
        var completed = Path.Combine(fixtureRoot, "original-app-source-flow.complete");
        if (File.Exists(completed)) throw new IOException("Completed original source/profile flow cannot be repeated.");
        var privateFile = Path.Combine(fixtureRoot, "fictitious-app-identity.protected");
        var info = new FileInfo(privateFile);
        if (info.Length is < 64 or > 32768 || (info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Invalid fictional local identity frame.");
        var wrapped = await File.ReadAllBytesAsync(privateFile); byte[]? clear = null; FictitiousSourceIdentity input;
        try
        {
            clear = ProtectedData.Unprotect(wrapped, null, DataProtectionScope.CurrentUser);
            input = JsonSerializer.Deserialize<FictitiousSourceIdentity>(clear) ?? throw new IOException();
            if (input.Format != 1 || input.Email != "household-native-app-source@example.invalid" || input.Password.Length is < 12 or > 256 ||
                input.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds())
                throw new IOException("Original fictional identity frame expired or changed.");
            _ = new LocalSyncPeer(input.Address, input.Fingerprint);
            if (input.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) input = input with { Password = "" };
        }
        finally { CryptographicOperations.ZeroMemory(wrapped); if (clear is not null) CryptographicOperations.ZeroMemory(clear); }
        var store = services.GetRequiredService<ZananceStore>(); var profiles = services.GetRequiredService<ProfileService>();
        var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>(); await SetLanguageAsync(app, localization, "en");
        services.GetRequiredService<ThemeService>().Set(ThemeChoice.Light); app.Windows[0].Width = 412; app.Windows[0].Height = 820;
        var progressPath = Path.Combine(folder, "original-source-flow-progress.json");
        SourceFlowProgress progress;
        if (!File.Exists(progressPath))
        {
            if (!profiles.Current.IsMain || profiles.All.Count != 1 || (await store.GetAccountsAsync()).Count != 0)
                throw new IOException("Only a fresh independent source installation can begin this actual flow.");
            progress = new(profiles.Current.Id, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 0);
            await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress));
        }
        else progress = JsonSerializer.Deserialize<SourceFlowProgress>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException();
        async Task SaveAsync() => await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress));
        var observations = new List<object>();
        HouseholdViewModel? observedModel = null;
        void Require(bool condition, string stage) { if (!condition) throw new IOException("Original native source flow failed: " + stage); }
        Microsoft.UI.Xaml.FrameworkElement? FindAction(Microsoft.UI.Xaml.DependencyObject node, string caption)
        {
            if (node is Microsoft.UI.Xaml.FrameworkElement element &&
                Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer &&
                peer.GetName() == caption && peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                return element;
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                if (FindAction(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index), caption) is { } found) return found;
            return null;
        }
        async Task<Microsoft.UI.Xaml.DependencyObject> DialogAsync(string title, string caption)
        {
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new IOException("Own native window required.");
            for (var attempt = 0; attempt < 160; attempt++)
            {
                // MAUI's Windows action sheet is a flyout, while alert/prompt use ContentDialog.
                // Resolve the actual named Invoke provider instead of assuming a dialog class.
                var dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(item => item.Child)
                    .Where(item => item is not null).SingleOrDefault(item => FindAction(item, caption) is not null);
                if (dialog is not null) return dialog;
                await Task.Delay(50);
            }
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "source-flow-failed-dialog.png"));
            throw new IOException("Original native dialog did not arrive: " + title + "; state=" + observedModel?.SourceFailureKind + "; busy=" + observedModel?.IsBusy);
        }
        async Task AnswerAsync(string title, string caption, string? typed = null)
        {
            var dialog = await DialogAsync(title, caption);
            if (typed is not null)
            {
                Microsoft.UI.Xaml.Controls.TextBox? Find(Microsoft.UI.Xaml.DependencyObject node)
                {
                    if (node is Microsoft.UI.Xaml.Controls.TextBox text) return text;
                    for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                        if (Find(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index)) is { } found) return found;
                    return null;
                }
                var text = Find(dialog) ?? throw new IOException("Original native prompt input missing.");
                // The original explicit connection label pre-fills this prompt. Verify that
                // actual native value and invoke Save; do not substitute desktop keyboard input.
                Require(text.Text == typed, "native prompt preserves the explicitly reviewed destination label");
            }
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "source-flow-dialog-" + observations.Count + ".png"));
            var button = FindAction(dialog, caption) ?? throw new IOException("Original native dialog answer missing: " + caption);
            var answerPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(button) ?? throw new IOException("Original native answer peer missing.");
            if (answerPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                throw new IOException("Original dialog Invoke pattern missing.");
            observations.Add(new { title, caption }); invoke.Invoke(); await Task.Delay(150);
        }
        async Task<(HouseholdPage Page, HouseholdViewModel Model, ScrollView Scroll)> OpenAsync()
        {
            var vm = services.GetRequiredService<HouseholdViewModel>(); vm.Name = "Fictitious actual shared destination";
            var page = new HouseholdPage(vm);
            observedModel = vm;
            var body = (Grid)page.Content!; PageHeader.Attach(page, false); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
            for (var attempt = 0; attempt < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; attempt++) await Task.Delay(50);
            Require(vm.IsReady && vm.Error is null, "actual page publication"); vm.Address = input.Address; vm.Fingerprint = input.Fingerprint;
            return (page, vm, body.Children.OfType<ScrollView>().Single());
        }
        async Task InvokeAsync((HouseholdPage Page, HouseholdViewModel Model, ScrollView Scroll) view, IAsyncRelayCommand command, bool wait = true)
        {
            var button = VisualDescendants(view.Page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
            await ScrollToViewIfNeededAsync(view.Scroll, button); InvokeChoiceReview(button, observations);
            if (wait && command.ExecutionTask is { } running) await running;
            if (wait) Require(view.Model.Error is null, "original native action");
        }
        async Task AuthenticateAsync((HouseholdPage Page, HouseholdViewModel Model, ScrollView Scroll) view)
        {
            await InvokeAsync(view, view.Model.DiscoverCommand, wait: false);
            if (view.Model.DiscoverCommand.ExecutionTask is { } discover) await discover;
            if (view.Model.Error is not null)
            {
                Require(view.Model.Error == translator["Sync_SignInRequired"], "only actual missing identity permits fixture sign-in");
                Require(input.Password.Length >= 12, "expired fixture password cannot create a new session");
                view.Model.Email = input.Email; view.Model.Password = input.Password;
                await InvokeAsync(view, view.Model.SignInCommand);
            }
        }
        if (progress.Phase == 0)
        {
            Require(profiles.Current.Id == progress.SourceId, "original source profile");
            var accounts = await store.GetAccountsAsync();
            if (!accounts.Any(item => item.Id == progress.Selected)) await store.SaveAccountAsync(new Account(progress.Selected)
                { Name = "Fictitious selected shared source", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = new(2026, 10, 16) });
            if (!accounts.Any(item => item.Id == progress.Private)) await store.SaveAccountAsync(new Account(progress.Private)
                { Name = "Fictitious private source not shared", CurrencyCode = "USD", OpeningBalance = 20000, OpeningDate = new(2026, 10, 16) });
            var entries = await store.GetEntriesAsync();
            if (!entries.Any(item => item.Id == progress.SelectedEntry)) Require((await store.SaveEntryAsync(new LedgerEntry(progress.SelectedEntry)
                { AccountId = progress.Selected, Kind = EntryKind.Expense, Amount = 450, Date = new(2026, 10, 16), Title = "Fictitious selected expense" })).Succeeded, "selected source expense");
            if (!entries.Any(item => item.Id == progress.PrivateEntry)) Require((await store.SaveEntryAsync(new LedgerEntry(progress.PrivateEntry)
                { AccountId = progress.Private, Kind = EntryKind.Expense, Amount = 123, Date = new(2026, 10, 16), Title = "Fictitious private expense not shared" })).Succeeded, "private source expense");
            progress = progress with { Before = await ProtectedRowsFingerprintAsync(services), Phase = 1 }; await SaveAsync();
        }
        if (progress.Phase == 1)
        {
            if (profiles.Current.Id != progress.SourceId) Require(await profiles.SwitchToAsync(profiles.All.Single(item => item.Id == progress.SourceId)), "return to original source");
            Require(await ProtectedRowsFingerprintAsync(services) == progress.Before, "unchanged original source before review");
            var view = await OpenAsync(); await AuthenticateAsync(view); await InvokeAsync(view, view.Model.LoadSourceAccountsCommand);
            var selected = view.Model.SourceAccounts.Single(item => item.Id == progress.Selected);
            var check = VisualDescendants(view.Page).OfType<CheckBox>().Single(item => ReferenceEquals(item.BindingContext, selected));
            async Task ToggleAsync(CheckBox checkbox)
            {
                await ScrollToViewIfNeededAsync(view.Scroll, checkbox);
                if (checkbox.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.CheckBox native) throw new IOException("Native source checkbox missing.");
                var peer = new Microsoft.UI.Xaml.Automation.Peers.CheckBoxAutomationPeer(native);
                if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle) is not Microsoft.UI.Xaml.Automation.Provider.IToggleProvider toggle)
                    throw new IOException("Original source Toggle pattern missing.");
                toggle.Toggle(); await Task.Delay(100);
            }
            await ToggleAsync(check); await ToggleAsync(view.Page.FindByName<CheckBox>("HouseholdSourceHistory"));
            await InvokeAsync(view, view.Model.ReviewSourceCommand);
            Require(view.Model.HasSourceReview && view.Model.SourceAccounts.Count(item => item.Selected) == 1 && !view.Model.SourceAttachments, "exact explicit reviewed source");
            var existing = profiles.All.SingleOrDefault(item => item.Name == "Fictitious actual shared destination");
            await InvokeAsync(view, view.Model.OpenSourceDestinationCommand, wait: false);
            await AnswerAsync(translator["HouseholdSource_Title"], translator["Sync_Confirm"]);
            var existingIndex = existing is null ? -1 : Array.FindIndex(profiles.All.Where(item => item.Id != progress.SourceId).ToArray(), item => item.Id == existing.Id);
            await AnswerAsync(translator["HouseholdSource_Destination"], existing is null ? translator["Household_NewProfile"] : "«" + profiles.NameOf(existing) + "» · " + (existingIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (existing is null) await AnswerAsync(translator["Household_NewProfile"], translator["Common_Save"], "Fictitious actual shared destination");
            if (view.Model.OpenSourceDestinationCommand.ExecutionTask is { } moving) await moving;
            Require(view.Model.Error is null && profiles.Current.Id != progress.SourceId, "actual native profile creation and drained source handoff");
            progress = progress with { TargetId = profiles.Current.Id, Phase = 2 }; await SaveAsync();
        }
        var destination = await OpenAsync(); await AuthenticateAsync(destination);
        Require(profiles.Current.Id == progress.TargetId, "same actual destination");
        if (progress.Phase == 2)
        {
            Require(destination.Model.HasSourceHandoff && !destination.Model.CanCompleteOwner, "copy required before publication");
            await InvokeAsync(destination, destination.Model.CompleteSourceCopyCommand, wait: false);
            await AnswerAsync(translator["Household_Title"], translator["Sync_Confirm"]);
            if (destination.Model.CompleteSourceCopyCommand.ExecutionTask is { } copying) await copying;
            Require(destination.Model.Error is null && !destination.Model.HasSourceHandoff && destination.Model.CanCompleteOwner && !destination.Model.HasPendingSourceReceipt,
                "original actual selected copy completed");
            var copiedAccounts = await store.GetAccountsAsync(); var copiedEntries = await store.GetEntriesAsync();
            Require(copiedAccounts.Count == 1 && copiedAccounts[0].CurrencyCode == "EUR" && copiedAccounts[0].Id != progress.Selected &&
                copiedEntries.Count == 1 && copiedEntries[0].Amount == 450 && copiedEntries[0].Id != progress.SelectedEntry, "separate exact finance and fresh copied identities");
            progress = progress with { Phase = 3 }; await SaveAsync();
        }
        if (progress.Phase == 3)
        {
            await InvokeAsync(destination, destination.Model.CompleteOwnerCommand, wait: false);
            await AnswerAsync(translator["Household_Title"], translator["Sync_Confirm"]);
            if (destination.Model.CompleteOwnerCommand.ExecutionTask is { } publishing) await publishing;
            Require(destination.Model.Error is null && destination.Model.HasMembership && destination.Model.IsOwner, "original actual encrypted publication");
            var sync = await services.GetRequiredService<PersonalSyncStore>().ReviewAsync();
            Require(sync.TransportKind == ProfileSynchronizationKind.Household && sync.RemoteProfile is not null, "encrypted-only actual binding");
            progress = progress with { Phase = 4 }; await SaveAsync();
        }
        Require(await profiles.SwitchToAsync(profiles.All.Single(item => item.Id == progress.SourceId)), "return to unchanged personal source");
        Require(progress.Before == await ProtectedRowsFingerprintAsync(services), "complete unchanged source financial fingerprint");
        Require(await profiles.SwitchToAsync(profiles.All.Single(item => item.Id == progress.TargetId)), "same copied target reopens with original keys");
        destination = await OpenAsync(); await AuthenticateAsync(destination);
        Require(destination.Model.HasMembership && destination.Model.IsOwner && (await store.GetAccountsAsync()).Count == 1 && (await store.GetEntriesAsync()).Single().Amount == 450,
            "native target reopen and original owner authority");
        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "original-source-flow-target.png"));
        await File.WriteAllTextAsync(Path.Combine(folder, "original-source-flow-proof.json"), JsonSerializer.Serialize(new
        { ActualMainProfileCreationAndSwitch = true, ActualSourceRecheckedWhileDrained = true, ExplicitNativeConsentAndCopy = true,
            ActualNativeOwnerAndEncryptedPublication = true, OriginalSourceCompletelyUnchanged = true, TargetReopensWithOriginalKeys = true,
            SelectedAccounts = 1, SelectedHistoricalEntries = 1, SharedFiles = 0, AndroidOrPhysicalDeviceAcceptance = false, NativeOperations = observations.Count }));
        await File.WriteAllTextAsync(completed, "Original protected app source-copy and encrypted publication verified v1");
        input = input with { Password = "" };
    }

    private sealed record FictitiousSourceIdentity(int Format, string Address, string Fingerprint, string Email, string Password, long ExpiresAt)
    {
        /// <inheritdoc />
        public override string ToString() => nameof(FictitiousSourceIdentity);
    }
    private sealed record SourceFlowProgress(string SourceId, string? TargetId, Guid Selected, Guid Private, Guid SelectedEntry, Guid PrivateEntry, string? Before, int Phase);
}
#endif
