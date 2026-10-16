#if DEBUG && WINDOWS
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only the new original-root display and native stale-invitation completion, without repeating completed device layout cases.</summary>
    private static async Task ReviewHouseholdRootManagementAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdsourceflow") throw new IOException("Independent installation required.");
        var fixture = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-network", "d294-app-source"));
        var complete = Path.Combine(fixture, "original-app-root-management.complete");
        if (File.Exists(complete) || await File.ReadAllTextAsync(Path.Combine(fixture, "original-app-management.complete")) !=
            "Original household device management verified v1") throw new IOException("Completed prior device review required; never replay it.");
        var env = services.GetRequiredService<IPersonalSyncEnvironment>(); var access = env.Capture(); var saved = env.ReadConnection();
        using var transport = new HouseholdServiceTransport(new(saved.Address, saved.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>(), TimeProvider.System, access.Validate);
        var journal = services.GetRequiredService<HouseholdJournal>(); var workspace = services.GetRequiredService<HouseholdWorkspace>();
        var identity = await transport.IdentityAsync(); var state = await journal.CaptureAsync(transport.Peer, identity.Id) ?? throw new IOException();
        var before = await HouseholdFinancialFingerprintAsync(services);
        var progressPath = Path.Combine(folder, "root-management-progress.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<RootManagementProgress>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException() :
            new RootManagementProgress(0, state.MembershipVersion);
        async Task SaveAsync(int phase) { progress = progress with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress)); }
        if (progress.Phase == 0)
        {
            _ = await workspace.InviteAsync(transport, state.Authority, HouseholdRole.Viewer);
            await SaveAsync(1);
        }
        if (progress.Phase == 1)
        {
            if (state.MembershipVersion == progress.Version)
            {
                var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                using var export = await workspace.ExportRecoveryKitAsync(transport, state.Authority, password); password = "";
                // This independent QA file qualifies core enrollment, not the native picker.
                await File.WriteAllBytesAsync(Path.Combine(fixture, "root-management-fictional-kit.zhr"), export.Content);
                await workspace.ConfirmRecoveryExportAsync(transport, state.Authority, export.Fingerprint);
                state = await workspace.CompleteRecoveryEnrollmentAsync(transport, state.Authority);
            }
            if (state.MembershipVersion != progress.Version + 1) throw new IOException("Only the original new-root publication may continue.");
            await SaveAsync(2);
        }
        var current = (await journal.ReadCurrentMembershipAsync(state.Authority)).Snapshot();
        var currentRoot = new PublicSigningRecipientIdentity(current.OwnerKeys.SigningKey, current.OwnerKeys.WrappingKey).Fingerprint;
        if (currentRoot == state.PinnedRoot.Fingerprint || await journal.ReadInvitationNoticeAsync(state.Authority) is null)
            throw new IOException("Original stale-root invitation must remain for explicit native completion.");
        var translator = services.GetRequiredService<Translator>(); await SetLanguageAsync(app, services.GetRequiredService<ILocalizationService>(), "en");
        services.GetRequiredService<ThemeService>().Set(ThemeChoice.Light); app.Windows[0].Width = 412; app.Windows[0].Height = 820;
        var vm = services.GetRequiredService<HouseholdViewModel>(); var page = new HouseholdPage(vm); var body = (Grid)page.Content!;
        PageHeader.Attach(page, false); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
        for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
        var scroll = body.Children.OfType<ScrollView>().Single(); var observations = new List<object>();
        async Task InvokeAsync(CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command, bool wait = true)
        {
            var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
            await ScrollToViewIfNeededAsync(scroll, button); InvokeChoiceReview(button, observations);
            if (wait && command.ExecutionTask is { } running) await running;
            if (wait && vm.Error is not null) throw new IOException("Original native root-management action failed.");
        }
        await InvokeAsync(vm.DiscoverCommand);
        if (!vm.CanAdminister || !vm.Fingerprints.Contains(currentRoot, StringComparison.Ordinal) || vm.Fingerprints.Contains(state.PinnedRoot.Fingerprint, StringComparison.Ordinal) ||
            !vm.HasPendingInvitation || vm.InvitationText.Length != 0) throw new IOException("Actual current root or original pending invitation not published.");
        var fingerprintLabel = VisualDescendants(page).OfType<Label>().Single(item => item.Text == vm.Fingerprints);
        await ScrollToViewIfNeededAsync(scroll, fingerprintLabel); CheckCaption(fingerprintLabel, observations);
        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "current-owner-root.png"));
        await InvokeAsync(vm.FinishInvitationCommand, wait: false);
        Microsoft.UI.Xaml.FrameworkElement? Find(Microsoft.UI.Xaml.DependencyObject node)
        {
            if (node is Microsoft.UI.Xaml.FrameworkElement element && Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer &&
                peer.GetName() == translator["Sync_Confirm"] && peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider) return element;
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                if (Find(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index)) is { } child) return child;
            return null;
        }
        Microsoft.UI.Xaml.FrameworkElement? answer = null;
        for (var wait = 0; wait < 160 && answer is null; wait++)
        {
            if (app.Windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                answer = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(item => Find(item.Child)).FirstOrDefault(item => item is not null);
            if (answer is null) await Task.Delay(50);
        }
        if (answer is null) throw new IOException("Original explicit Finish confirmation missing.");
        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "finish-original-invitation-confirmation.png"));
        ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(answer)!
            .GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
        if (vm.FinishInvitationCommand.ExecutionTask is { } finished) await finished;
        if (vm.Error is not null || vm.HasPendingInvitation || await journal.ReadInvitationNoticeAsync(state.Authority) is not null ||
            before != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Finish must remove only original invitation intent and preserve finance.");
        vm.Deactivate();
        await File.WriteAllTextAsync(Path.Combine(folder, "root-management-proof.json"), JsonSerializer.Serialize(new
        { CurrentVerifiedRootRendered = true, OriginalInvitationFinishedThroughNativeConfirmationWithoutClipboard = true, OriginalFinanceUnchanged = true, NativePickerAcceptance = false }));
        await SaveAsync(3); await File.WriteAllTextAsync(complete, "Original household device management verified v1");
    }

    /// <summary>Public original membership checkpoint; never invitation capabilities, passwords or private keys.</summary>
    private sealed record RootManagementProgress(int Phase, long Version);
}
#endif
