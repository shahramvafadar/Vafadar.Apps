#if DEBUG && WINDOWS
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Cryptography;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>New management controls against the retained original owner, without replaying source copy or manufacturing membership.</summary>
    private static async Task ReviewHouseholdManagementUiAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdsourceflow") throw new IOException("Independent installation required.");
        var fixture = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-network", "d294-app-source"));
        var completed = Path.Combine(fixture, "original-app-management.complete");
        if (File.Exists(completed) || await File.ReadAllTextAsync(Path.Combine(fixture, "original-app-source-flow.complete")) !=
            "Original protected app source-copy and encrypted publication verified v1") throw new IOException("Retain the completed original source fixture.");
        var environment = services.GetRequiredService<IPersonalSyncEnvironment>(); var access = environment.Capture();
        var saved = environment.ReadConnection(); var peer = new LocalSyncPeer(saved.Address, saved.Fingerprint);
        using var transport = new HouseholdServiceTransport(peer, services.GetRequiredService<IPersonalSyncTokenStore>(), TimeProvider.System, access.Validate);
        var journal = services.GetRequiredService<HouseholdJournal>(); var workspace = services.GetRequiredService<HouseholdWorkspace>();
        var identity = await transport.IdentityAsync();
        var state = await journal.CaptureAsync(peer, identity.Id) ?? throw new IOException("Original household profile required.");
        var keyPath = Path.Combine(fixture, "management-pending-device.protected");
        if (!File.Exists(keyPath))
        {
            using var fresh = SigningRecipientIdentity.Create(); var clear = fresh.ExportForProtectedStorage();
            var wrapped = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
            try { await File.WriteAllBytesAsync(keyPath, wrapped); }
            finally { CryptographicOperations.ZeroMemory(clear); CryptographicOperations.ZeroMemory(wrapped); }
        }
        var protectedKey = await File.ReadAllBytesAsync(keyPath); byte[]? unwrapped = null; SigningRecipientIdentity pending;
        try { unwrapped = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser); pending = SigningRecipientIdentity.ImportFromProtectedStorage(unwrapped); }
        finally { CryptographicOperations.ZeroMemory(protectedKey); if (unwrapped is not null) CryptographicOperations.ZeroMemory(unwrapped); }
        using var originalPending = pending;
        var existing = (await transport.PendingDevicesAsync(state.SpaceId, state.DeviceId)).SingleOrDefault(row =>
            new PublicSigningRecipientIdentity(row.DeviceKeys.SigningKey, row.DeviceKeys.WrappingKey).Fingerprint == pending.Public.Fingerprint);
        if (existing is null)
        {
            var session = await transport.SessionAsync();
            var possession = new HouseholdDeviceSessionStatement(1, state.SpaceId, Guid.Empty, Guid.NewGuid(), identity.Id, session.SessionId,
                new(pending.Public.SigningKey, pending.Public.WrappingKey), DateTimeOffset.UtcNow.AddMinutes(12).ToUnixTimeSeconds());
            var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(possession, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement),
                pending, "zanance-household-device-enrollment+jwt");
            var observed = await transport.EnrollDeviceAsync(state.SpaceId, new(new(pending.Public.SigningKey, pending.Public.WrappingKey), signed));
            if (observed.Approved || observed.Revoked) throw new IOException("Self enrollment must not grant access.");
        }
        var localization = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var progressPath = Path.Combine(folder, "household-management-progress.json");
        var cases = File.Exists(progressPath) ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException() : [];
        var observations = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            if (cases.Any(item => item.Language == language && item.Theme == theme.ToString() && item.Width == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, localization, language); app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var vm = services.GetRequiredService<HouseholdViewModel>(); var page = new HouseholdPage(vm)
                { FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            var body = (Grid)page.Content!; PageHeader.Attach(page, localization.IsRightToLeft); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
            for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
            var scroll = body.Children.OfType<ScrollView>().Single();
            var discover = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, vm.DiscoverCommand));
            await ScrollToViewIfNeededAsync(scroll, discover); InvokeChoiceReview(discover, observations);
            if (vm.DiscoverCommand.ExecutionTask is { } running) await running;
            if (vm.Error is not null || !vm.CanAdminister || vm.ApprovedDevices.Count != 1 || vm.ApprovedDevices[0].CanRevoke ||
                vm.PendingDevices.Count != 1 || vm.PendingDevices[0].Source.DeviceKeys.SigningKey != pending.Public.SigningKey)
                throw new IOException("Original owner inventory or current-device protection was not published.");
            await Task.Delay(200);
            foreach (var caption in vm.ApprovedDevices.Select(item => item.Caption).Concat(vm.PendingDevices.Select(item => item.Caption)))
            {
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == caption);
                await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, observations);
            }
            var approve = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, vm.ApproveDeviceCommand));
            await ScrollToViewIfNeededAsync(scroll, approve);
            if (!approve.IsEnabled || approve.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || !native.IsEnabled)
                throw new IOException("Pending approval must retain its original enabled native action.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-pending-device.png"));
            vm.Deactivate(); cases.Add(new(language, theme.ToString(), width, true)); await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(cases));
        }
        var store = services.GetRequiredService<ZananceStore>();
        var accounts = await store.GetAccountsAsync(); var entries = await store.GetEntriesAsync();
        if (accounts.Count != 1 || accounts[0].CurrencyCode != "EUR" || accounts[0].OpeningBalance != 10000 || entries.Count != 1 ||
            entries[0].Amount != 450 || entries[0].AccountId != accounts[0].Id) throw new IOException("Original copied source amounts were not retained.");
        await File.WriteAllTextAsync(Path.Combine(folder, "household-management-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases, NativeObservations = observations.Count, ActualOwnerAndPendingDevice = true, SelfEnrollmentGrantsNoAccess = true,
            CurrentDeviceRemovalDisabled = true, IndependentFingerprintApprovalNotBypassed = true, OriginalCopiedAmountsRetained = true,
            PriorWholeDatabaseComparisonIncludedChangedSessionMetadata = true, MobileAcceptance = false }));
        await File.WriteAllTextAsync(completed, "Original household device management verified v1");
    }

}
#endif
