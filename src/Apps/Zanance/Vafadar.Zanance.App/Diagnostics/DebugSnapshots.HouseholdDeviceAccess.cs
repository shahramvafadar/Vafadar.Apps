#if DEBUG && WINDOWS
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Original main page export/request/fingerprint approval/adoption and a separate cold process on the retained independent installation.</summary>
    private static async Task ReviewHouseholdDeviceAccessAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdsourceflow") throw new IOException("Independent original installation required.");
        var fixture = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-network", "d294-app-source"));
        var completed = Path.Combine(fixture, "original-app-device-access.complete");
        if (File.Exists(completed) || !File.Exists(Path.Combine(fixture, "original-app-root-management.complete"))) throw new IOException("Completed prior fixture required; never replay completed device access.");
        var profiles = services.GetRequiredService<ProfileService>(); var journal = services.GetRequiredService<HouseholdJournal>();
        var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>(); var environment = services.GetRequiredService<IPersonalSyncEnvironment>();
        var sourceProgress = JsonSerializer.Deserialize<SourceFlowProgress>(await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-source-flow-d294", "original-source-flow-progress.json"))) ?? throw new IOException();
        var progressPath = Path.Combine(folder, "device-access-progress.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<DeviceAccessUiProgress>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException() :
            new DeviceAccessUiProgress(0, sourceProgress.TargetId ?? throw new IOException(), null, null, null, Guid.Empty, null, 0);
        async Task SaveAsync(int phase) { progress = progress with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress)); }
        void Require(bool value, string stage) { if (!value) throw new IOException("Original native device adoption failed: " + stage); }
        async Task SwitchAsync(string id)
        {
            if (profiles.Current.Id != id) Require(await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), profiles.All.Single(item => item.Id == id)), "actual retained profile switch");
        }
        var observations = new List<object>();
        async Task<(HouseholdPage Page, HouseholdViewModel Model, ScrollView Scroll)> OpenAsync()
        {
            var vm = services.GetRequiredService<HouseholdViewModel>(); var page = new HouseholdPage(vm)
                { FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            var body = (Grid)page.Content!; PageHeader.Attach(page, localization.IsRightToLeft); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
            for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
            Require(vm.IsReady && vm.Error is null, "page published");
            return (page, vm, body.Children.OfType<ScrollView>().Single());
        }
        async Task InvokeAsync((HouseholdPage Page, HouseholdViewModel Model, ScrollView Scroll) view, IAsyncRelayCommand command, object? parameter = null, bool wait = true)
        {
            var button = VisualDescendants(view.Page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && (parameter is null || ReferenceEquals(item.CommandParameter, parameter)));
            await ScrollToViewIfNeededAsync(view.Scroll, button); InvokeChoiceReview(button, observations);
            if (wait && command.ExecutionTask is { } running) await running;
            if (wait) Require(view.Model.Error is null, "native command");
        }
        Microsoft.UI.Xaml.FrameworkElement? Find(Microsoft.UI.Xaml.DependencyObject node, Func<Microsoft.UI.Xaml.FrameworkElement, bool> predicate)
        {
            if (node is Microsoft.UI.Xaml.FrameworkElement element && predicate(element)) return element;
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                if (Find(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index), predicate) is { } child) return child;
            return null;
        }
        async Task AnswerAsync(string caption)
        {
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root } nativeWindow) throw new IOException();
            Microsoft.UI.Xaml.DependencyObject? dialog = null; Microsoft.UI.Xaml.FrameworkElement? answer = null;
            bool Match(Microsoft.UI.Xaml.FrameworkElement element) => Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer &&
                peer.GetName() == caption && peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
            for (var wait = 0; wait < 160 && answer is null; wait++)
            {
                dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(item => item.Child).FirstOrDefault(item => Find(item, Match) is not null);
                if (dialog is not null) answer = Find(dialog, Match);
                if (answer is null) await Task.Delay(50);
            }
            Require(answer is not null && dialog is not null, "original native confirmation");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "device-dialog-" + observations.Count + ".png"));
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(answer!)!
                .GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke(); await Task.Delay(100);
        }
        var cold = Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_DEVICE_COLD") == "1";
        await SetLanguageAsync(app, localization, "en"); themes.Set(ThemeChoice.Light); app.Windows[0].Width = 412; app.Windows[0].Height = 820;
        if (cold)
        {
            Require(progress.Phase == 4 && progress.ProcessId != Environment.ProcessId && progress.DeviceProfile is not null, "separate actual cold process");
            await SwitchAsync(progress.DeviceProfile!); var view = await OpenAsync(); await InvokeAsync(view, view.Model.DiscoverCommand);
            Require(view.Model.IsOwner && view.Model.CanSynchronize && await HouseholdFinancialFingerprintAsync(services) == progress.FinancialHash, "cold same-key household finance");
            var saved = environment.ReadConnection(); using var channel = new HouseholdServiceTransport(new(saved.Address, saved.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>());
            var identity = await channel.IdentityAsync(); var state = await journal.CaptureAsync(channel.Peer, identity.Id) ?? throw new IOException();
            Require(state.DeviceId == progress.DeviceId && state.DeviceFingerprint == progress.DeviceFingerprint && state.MembershipVersion > 0, "cold original device identity");
            await ReviewOwnDeviceAdoptionWriteBoundaryAsync(services, folder);
            view.Model.Deactivate(); await SaveAsync(5);
            await File.WriteAllTextAsync(Path.Combine(folder, "device-access-cold-proof.json"), JsonSerializer.Serialize(new { SameProtectedDeviceAndFinance = true, OriginalCurrentOwner = true, NewProcess = true }));
            await File.WriteAllTextAsync(completed, "Original household device management verified v1"); return;
        }
        if (progress.Phase == 0)
        {
            await SwitchAsync(progress.OwnerProfile); var owner = await OpenAsync(); await InvokeAsync(owner, owner.Model.DiscoverCommand);
            Require(owner.Model.IsOwner && owner.Model.Members.Count == 1, "original fixture owner");
            await InvokeAsync(owner, owner.Model.CreateDeviceAccessCommand, owner.Model.Members.Single(), wait: false); await AnswerAsync(translator["Sync_Confirm"]);
            if (owner.Model.CreateDeviceAccessCommand.ExecutionTask is { } exporting) await exporting;
            Require(owner.Model.Error is null && HouseholdSetupAnchors.Kind(owner.Model.InvitationText) == HouseholdSetupAnchors.DeviceAccess, "original native public export");
            progress = progress with { Package = owner.Model.InvitationText }; owner.Model.Deactivate();
            var profile = profiles.All.SingleOrDefault(item => item.Name == "Fictitious actual approved device") ??
                await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious actual approved device", CancellationToken.None);
            progress = progress with { DeviceProfile = profile.Id }; await SaveAsync(1);
        }
        if (progress.Phase == 1)
        {
            await SwitchAsync(progress.DeviceProfile!); var child = await OpenAsync(); await InvokeAsync(child, child.Model.DiscoverCommand);
            var package = JsonSerializer.Deserialize(progress.Package!, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage) ?? throw new IOException();
            child.Model.InvitationText = progress.Package!; child.Model.ConfirmedRoot = new PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey).Fingerprint;
            await InvokeAsync(child, child.Model.EnrollOwnDeviceCommand, wait: false); await AnswerAsync(translator["Sync_Confirm"]);
            if (child.Model.EnrollOwnDeviceCommand.ExecutionTask is { } requesting) await requesting;
            Require(child.Model.Error is null && !child.Model.IsOwner && !child.Model.CanSynchronize && !child.Model.IsOwnerRecoveryDraft, "pending request is not recovery or authority");
            var saved = environment.ReadConnection(); using var channel = new HouseholdServiceTransport(new(saved.Address, saved.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>());
            var identity = await channel.IdentityAsync(); var state = await journal.CaptureAsync(channel.Peer, identity.Id) ?? throw new IOException();
            progress = progress with { DeviceFingerprint = state.DeviceFingerprint, DeviceId = state.DeviceId }; child.Model.Deactivate(); await SaveAsync(2);
        }
        if (progress.Phase == 2)
        {
            await SwitchAsync(progress.OwnerProfile); var owner = await OpenAsync(); await InvokeAsync(owner, owner.Model.DiscoverCommand);
            var item = owner.Model.PendingDevices.Single(row => row.Source.DeviceId == progress.DeviceId);
            Require(new PublicSigningRecipientIdentity(item.Source.DeviceKeys.SigningKey, item.Source.DeviceKeys.WrappingKey).Fingerprint == progress.DeviceFingerprint, "independent original requesting-device comparison");
            await InvokeAsync(owner, owner.Model.ApproveDeviceCommand, item, wait: false); await AnswerAsync(translator["Common_Cancel"]);
            if (owner.Model.ApproveDeviceCommand.ExecutionTask is { } approving) await approving;
            Require(owner.Model.Error is null && !owner.Model.ApprovedDevices.Any(row => row.Source.Id == progress.DeviceId) && owner.Model.PendingDevices.Any(row => row.Source.DeviceId == progress.DeviceId), "native cancellation grants no device access");
            // Hidden WinUI windows do not expose the actual TextBox to an external UIA client.
            // Qualify the original native Cancel, then prepare this exact fictional approval through
            // the actual owner workspace; do not count this as native fingerprint-entry acceptance.
            var saved = environment.ReadConnection(); using var channel = new HouseholdServiceTransport(new(saved.Address, saved.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>());
            var identity = await channel.IdentityAsync(); var authority = await journal.CaptureAsync(channel.Peer, identity.Id) ?? throw new IOException();
            await services.GetRequiredService<HouseholdWorkspace>().ApproveDeviceAsync(channel, authority.Authority, authority.MembershipVersion, item.Source, progress.DeviceFingerprint!);
            owner.Model.Deactivate(); await SaveAsync(3);
        }
        if (progress.Phase == 3)
        {
            await SwitchAsync(progress.DeviceProfile!); var child = await OpenAsync(); await InvokeAsync(child, child.Model.DiscoverCommand);
            await InvokeAsync(child, child.Model.CheckAdmissionCommand);
            Require(child.Model.Error is null && child.Model.IsOwner && child.Model.CanSynchronize, "actual original admitted device page");
            var finances = services.GetRequiredService<ZananceStore>(); var accounts = await finances.GetAccountsAsync(); var entries = await finances.GetEntriesAsync();
            Require(accounts.Count == 1 && accounts[0].OpeningBalance == 10000 && entries.Count == 1 && entries[0].Amount == 450 && entries[0].AccountId == accounts[0].Id, "original complete source history");
            progress = progress with { FinancialHash = await HouseholdFinancialFingerprintAsync(services), ProcessId = Environment.ProcessId };
            child.Model.Deactivate(); await SaveAsync(4);
        }
        Require(progress.Phase == 4, "complete native original flow before cold verification");
        var layoutPath = Path.Combine(folder, "device-access-layout-progress.json");
        var cases = File.Exists(layoutPath) ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(layoutPath)) ?? throw new IOException() : [];
        var beforeLayout = await HouseholdFinancialFingerprintAsync(services);
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            if (cases.Any(item => item.Language == language && item.Theme == theme.ToString() && item.Width == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, localization, language); app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var view = await OpenAsync(); await InvokeAsync(view, view.Model.DiscoverCommand);
            Require(view.Model.IsOwner && view.Model.CanAdminister && !view.Model.CanSetUp, "approved device cannot request a different binding in this ledger");
            foreach (var key in new[] { "HouseholdDeviceAccess_Hint", "HouseholdDeviceAccess_Request", "HouseholdDeviceAccess_Export" })
            {
                var label = VisualDescendants(view.Page).OfType<Label>().Single(item => item.Text == translator[key]);
                await ScrollToViewIfNeededAsync(view.Scroll, label); CheckCaption(label, observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-{key}.png"));
            }
            view.Model.Deactivate(); cases.Add(new(language, theme.ToString(), width, true)); await File.WriteAllTextAsync(layoutPath, JsonSerializer.Serialize(cases));
        }
        Require(beforeLayout == await HouseholdFinancialFingerprintAsync(services), "complete original financial columns through new-control layouts");
        await File.WriteAllTextAsync(Path.Combine(folder, "device-access-flow-proof.json"), JsonSerializer.Serialize(new { OriginalNativeExportRequestCancelAndAdmission = true,
            OriginalOwnerWorkspaceApprovalAfterIndependentDeviceComparison = true, NativeOwnerFingerprintEntryAcceptance = false, ActualSeparateProtectedProfile = true, OriginalCopiedAmountsRetained = true, NativeObservations = observations.Count, ColdPending = true }));
    }

    /// <summary>Original native writer on only this fictional recipient; every metadata cut and possible test row stays inside a rolled-back transaction.</summary>
    private static async Task ReviewOwnDeviceAdoptionWriteBoundaryAsync(IServiceProvider services, string folder)
    {
        var original = await ProtectedRowsFingerprintAsync(services); var results = new List<object>();
        foreach (var cut in new[] { "missing-connection", "first-receive-not-applied" })
        {
            var denied = false;
            await using (var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync())
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                var sql = cut == "missing-connection" ? "DELETE FROM \"SyncBinding\" WHERE \"Id\"=1" : "UPDATE \"SyncBinding\" SET \"LastSuccessAt\"=NULL WHERE \"Id\"=1";
                if (await db.Database.ExecuteSqlRawAsync(sql) != 1) throw new IOException("Original fictional receiving binding required.");
                db.Accounts.Add(new Account(Guid.NewGuid()) { Name = "Fictitious rolled-back pre-adoption write", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) });
                try { await db.SaveChangesAsync(); }
                catch (HouseholdWriteRejectedException error) when (error.Reason == HouseholdWriteBlockReason.Unpublished) { denied = true; }
                await transaction.RollbackAsync();
            }
            if (original != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Native admission boundary fixture did not roll back exactly.");
            results.Add(new { Cut = cut, FinancialWriteDenied = denied, OriginalWholeDatabaseRestored = true });
            await File.WriteAllTextAsync(Path.Combine(folder, "device-adoption-write-boundary.json"), JsonSerializer.Serialize(results));
            if (!denied) throw new IOException("Approved metadata alone allowed a financial write before complete local adoption; the original whole database was restored.");
        }
    }

    /// <summary>Public operation/profile progress only; actual private keys and credentials remain in original native protected storage.</summary>
    private sealed record DeviceAccessUiProgress(int Phase, string OwnerProfile, string? DeviceProfile, string? Package, string? DeviceFingerprint, Guid DeviceId, string? FinancialHash, int ProcessId);
}
#endif
