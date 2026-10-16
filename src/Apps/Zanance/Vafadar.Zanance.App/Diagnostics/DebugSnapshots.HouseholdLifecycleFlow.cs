#if DEBUG && WINDOWS
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Real main-app cancellation, retained owner privacy and cold receipt on one independent fictitious installation.</summary>
    private static async Task ReviewHouseholdLifecycleFlowAsync(App app, IServiceProvider services, string folder)
    {
        const string package = "pro.vafadar.zanance.householdlifecycle";
        if (AppInfo.Current.PackageName != package || !FileSystem.AppDataDirectory.Contains(package, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Independent lifecycle installation required.");
        var progressPath = Path.Combine(folder, "original-lifecycle-progress.json");
        var done = Path.Combine(folder, "original-lifecycle.complete");
        if (File.Exists(done)) throw new IOException("Completed native lifecycle must not repeat.");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<LifecycleFlowProgress>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException() : new(0, Guid.NewGuid(), Guid.NewGuid(), null, null, null, 0);
        async Task SaveAsync() => await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress));
        void Require(bool condition, string stage) { if (!condition) throw new IOException("Original main lifecycle failed: " + stage); }
        var store = services.GetRequiredService<ZananceStore>(); var profiles = services.GetRequiredService<ProfileService>();
        var journal = services.GetRequiredService<HouseholdJournal>(); var environment = services.GetRequiredService<IPersonalSyncEnvironment>();
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        await SetLanguageAsync(app, localization, "en"); services.GetRequiredService<ThemeService>().Set(ThemeChoice.Light);
        app.Windows[0].Width = 412; app.Windows[0].Height = 820;
        if (progress.Phase == 0) Require(profiles.Current.IsMain && profiles.All.Count == 1 && (await store.GetAccountsAsync()).Count == 0, "fresh owned installation");
        using var trust = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(Environment.CurrentDirectory, "artifacts", "household-hosting-d294-root.crt"));
        string pin;
        using (var tcp = new TcpClient())
        {
            await tcp.ConnectAsync("localhost", 14645); using var tls = new SslStream(tcp.GetStream());
            await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "localhost", CertificateChainPolicy = new X509ChainPolicy
                { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { trust }, RevocationMode = X509RevocationMode.NoCheck } });
            pin = Convert.ToHexStringLower(SHA256.HashData(tls.RemoteCertificate!.GetRawCertData()));
        }
        var vm = services.GetRequiredService<HouseholdViewModel>(); var page = new HouseholdPage(vm)
            { FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var body = (Grid)page.Content!; PageHeader.Attach(page, localization.IsRightToLeft); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
        for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
        if (!vm.IsReady && !vm.IsBusy && vm.Error is null) await vm.ActivateAsync();
        Require(vm.IsReady && vm.Error is null, "real page publication");
        vm.Address = "https://localhost:14645/"; vm.Fingerprint = pin;
        var scroll = body.Children.OfType<ScrollView>().Single(); var observations = new List<object>();
        async Task InvokeAsync(IAsyncRelayCommand command, bool wait = true)
        {
            var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
            await ScrollToViewIfNeededAsync(scroll, button);
            for (var attempt = 0; attempt < 40 && button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: false }; attempt++) await Task.Delay(50);
            Require(button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: true }, "enabled original native action " + button.Text);
            InvokeChoiceReview(button, observations);
            if (wait && command.ExecutionTask is { } running) await running;
            if (wait) Require(vm.Error is null, "native action " + button.Text);
        }
        Microsoft.UI.Xaml.FrameworkElement? Find(Microsoft.UI.Xaml.DependencyObject node, string caption)
        {
            if (node is Microsoft.UI.Xaml.FrameworkElement element && Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer &&
                peer.GetName() == caption && peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider) return element;
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                if (Find(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index), caption) is { } child) return child;
            return null;
        }
        async Task AnswerAsync(string caption, string file)
        {
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new IOException();
            Microsoft.UI.Xaml.FrameworkElement? answer = null;
            for (var wait = 0; wait < 160 && answer is null; wait++)
            {
                answer = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(item => Find(item.Child, caption)).FirstOrDefault(item => item is not null);
                if (answer is null) await Task.Delay(50);
            }
            Require(answer is not null, "actual own-window confirmation");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, file + ".png"));
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(answer!)!
                .GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
        }
        async Task ConfirmAsync(IAsyncRelayCommand command, string answer, string file)
        {
            await InvokeAsync(command, false); await AnswerAsync(answer, file);
            if (command.ExecutionTask is { } running) await running;
            Require(vm.Error is null, "confirmed native action");
        }
        if (progress.Phase == 0)
        {
            // This compiled credential belongs solely to the separately confirmed example.invalid fixture.
            // It is never obtained from owner storage, a command argument or an output log.
            vm.Email = "household-main-lifecycle@example.invalid"; vm.Password = "Fictitious-Household-Hosting9!";
            await InvokeAsync(vm.SignInCommand); progress = progress with { Phase = 1 }; await SaveAsync();
        }
        else await InvokeAsync(vm.DiscoverCommand);
        if (progress.Phase == 1)
        {
            await ConfirmAsync(vm.BeginOwnerCommand, translator["Sync_Confirm"], "create-confirm");
            progress = progress with { Phase = 2 }; await SaveAsync();
        }
        if (progress.Phase == 2)
        {
            vm.Name = "Fictitious native lifecycle";
            await ConfirmAsync(vm.CompleteOwnerCommand, translator["Sync_Confirm"], "publish-confirm");
            Require(vm.IsOwner && vm.HasMembership, "original owner publication"); progress = progress with { Phase = 3 }; await SaveAsync();
        }
        if (progress.Phase == 3)
        {
            if (!(await store.GetAccountsAsync()).Any(item => item.Id == progress.Account)) await store.SaveAccountAsync(new Account(progress.Account)
                { Name = "Fictitious retained local money", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = new(2026, 10, 16) });
            if (!(await store.GetEntriesAsync()).Any(item => item.Id == progress.Entry)) Require((await store.SaveEntryAsync(new LedgerEntry(progress.Entry)
                { AccountId = progress.Account, Kind = EntryKind.Expense, Amount = 450, Date = new(2026, 10, 16), Title = "Fictitious original lifecycle expense" })).Succeeded, "real local financial writer");
            await InvokeAsync(vm.SynchronizeCommand);
            progress = progress with { Before = await HouseholdFinancialFingerprintAsync(services), Phase = 4 }; await SaveAsync();
        }
        var saved = environment.ReadConnection(); using var channel = new HouseholdServiceTransport(new(saved.Address, saved.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>());
        var identity = await channel.IdentityAsync(); var state = await journal.CaptureAsync(channel.Peer, identity.Id) ?? throw new IOException();
        if (progress.Phase == 4)
        {
            await ConfirmAsync(vm.DeleteRemoteCommand, translator["Common_Cancel"], "delete-cancel-confirm");
            Require(await journal.ReadDeletionAsync(state.Authority) is null && !vm.HasRemoteDeletion && vm.CanDeleteRemote, "cancel creates no intent");
            await InvokeAsync(vm.ReadLifecycleCommand); Require(vm.LifecycleText == translator["HouseholdLifecycle_Active"], "cancel preserved remote authority");
            Require(progress.Before == await HouseholdFinancialFingerprintAsync(services), "cancel preserves all financial columns");
            progress = progress with { Phase = 5 }; await SaveAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "ready-for-retention.json"), JsonSerializer.Serialize(new { state.SpaceId, CancelPreservedFinanceAndAuthority = true }));
            vm.Deactivate(); return;
        }
        if (progress.Phase == 5)
        {
            await InvokeAsync(vm.ReadLifecycleCommand);
            Require(vm.LifecycleText == translator["HouseholdLifecycle_Retained"] && vm.CanDeleteRemote, "retained owner privacy still available");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "retained-before-delete.png"));
            await ConfirmAsync(vm.DeleteRemoteCommand, translator["Sync_Confirm"], "retained-delete-confirm");
            var intent = await journal.ReadDeletionAsync(state.Authority) ?? throw new IOException();
            Require(intent.Receipt is not null && !intent.Rejected && vm.HasRemoteDeletion && !vm.CanDeleteRemote && !vm.CanSynchronize, "accepted original consent blocks writes");
            progress = progress with { Phase = 6, Operation = intent.Receipt!.OperationId, Declared = intent.Receipt.DeclaredAtUnixSeconds, Process = Environment.ProcessId }; await SaveAsync();
            vm.Deactivate(); return;
        }
        if (progress.Phase == 6)
        {
            Require(progress.Process != Environment.ProcessId, "actual cold process");
            await InvokeAsync(vm.ResumeRemoteDeletionCommand); await InvokeAsync(vm.ReadLifecycleCommand);
            var intent = await journal.ReadDeletionAsync(state.Authority) ?? throw new IOException();
            Require(intent.Receipt is { StorageRetired: true } receipt && receipt.OperationId == progress.Operation && receipt.DeclaredAtUnixSeconds == progress.Declared &&
                vm.LifecycleText == translator["HouseholdLifecycle_Deleted"] && vm.HasRemoteDeletion && !vm.CanDeleteRemote && !vm.CanSynchronize, "cold original receipt and permanent remote retirement");
            Require(progress.Before == await HouseholdFinancialFingerprintAsync(services), "retained deletion preserves original complete local money");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "cold-deleted-local-preserved.png")); vm.Deactivate();
            await File.WriteAllTextAsync(done, JsonSerializer.Serialize(new { ActualMainNativeCancel = true, RetainedOwnerDelete = true, ColdOriginalReceipt = true, LocalFinancialColumnsUnchanged = true, Android = false }));
        }
    }

    /// <summary>Only public fixture progress and original identifiers; no password, root or session bytes.</summary>
    private sealed record LifecycleFlowProgress(int Phase, Guid Account, Guid Entry, string? Before, Guid? Operation, long? Declared, int Process);

    /// <summary>Only the newly observed permanent-deletion state in the existing completed main fixture; never repeats deletion or financial synchronization.</summary>
    private static async Task ReviewHouseholdDeletedLayoutAsync(App app, IServiceProvider services, string folder)
    {
        const string package = "pro.vafadar.zanance.householdlifecycle";
        if (AppInfo.Current.PackageName != package || !FileSystem.AppDataDirectory.Contains(package, StringComparison.OrdinalIgnoreCase)) throw new IOException();
        var originalFolder = Path.Combine(Environment.CurrentDirectory, "artifacts", "household-main-lifecycle-ui-d294");
        if (!File.Exists(Path.Combine(originalFolder, "original-lifecycle.complete")) || File.Exists(Path.Combine(folder, "deleted-layout-proof.json"))) throw new IOException("Preserve completed original main lifecycle.");
        var original = JsonSerializer.Deserialize<LifecycleFlowProgress>(await File.ReadAllTextAsync(Path.Combine(originalFolder, "original-lifecycle-progress.json"))) ?? throw new IOException();
        if (original.Phase != 6 || original.Before != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Original financial fixture changed.");
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>(); var themes = services.GetRequiredService<ThemeService>();
        var vm = services.GetRequiredService<HouseholdViewModel>(); var page = new HouseholdPage(vm);
        var body = (Grid)page.Content!; PageHeader.Attach(page, localization.IsRightToLeft); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
        for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
        if (!vm.IsReady && !vm.IsBusy && vm.Error is null) await vm.ActivateAsync();
        if (!vm.IsReady || vm.Error is not null) throw new IOException("Actual deletion page unavailable.");
        // One actual identity/lifecycle observation precedes local rendering. Each size,
        // language and theme reuses that same verified state without another HTTP action.
        await vm.DiscoverCommand.ExecuteAsync(null); await vm.ReadLifecycleCommand.ExecuteAsync(null);
        if (vm.Error is not null || !vm.HasRemoteDeletion || vm.CanDeleteRemote || vm.CanSynchronize || vm.LifecycleText != translator["HouseholdLifecycle_Deleted"] ||
            vm.RemoteDeletionText != translator["HouseholdLifecycle_Retired"]) throw new IOException("Original accepted receipt and retirement required.");
        var scroll = body.Children.OfType<ScrollView>().Single(); var observations = new List<object>();
        var progressPath = Path.Combine(folder, "deleted-layout-progress.json");
        var cases = File.Exists(progressPath) ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException() : [];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            if (cases.Any(item => item.Language == language && item.Theme == theme.ToString() && item.Width == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, localization, language); page.FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            foreach (var key in new[] { "HouseholdLifecycle_Deleted", "HouseholdLifecycle_Retired" })
            {
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == translator[key]);
                await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-{key}.png"));
            }
            cases.Add(new(language, theme.ToString(), width, true)); await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(cases));
        }
        if (original.Before != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Deletion layouts changed local money.");
        vm.Deactivate(); await File.WriteAllTextAsync(Path.Combine(folder, "deleted-layout-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases.Count, NativeObservations = observations.Count, OriginalActualDeletedState = true, NoRepeatedDeletionOrFinancialHttp = true, LocalMoneyUnchanged = true, Android = false }));
    }
}
#endif
