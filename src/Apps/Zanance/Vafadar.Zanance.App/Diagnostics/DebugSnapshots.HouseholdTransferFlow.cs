#if DEBUG && WINDOWS
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Cryptography;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Original hidden Windows page participates in a real cross-platform transfer; public controls never carry credentials or invitation capabilities.</summary>
    private static async Task ReviewHouseholdTransferFlowAsync(App app, IServiceProvider services, string folder)
    {
        const string package = "pro.vafadar.zanance.householdlifecycle";
        if (AppInfo.Current.PackageName != package || !FileSystem.AppDataDirectory.Contains(package, StringComparison.OrdinalIgnoreCase)) throw new IOException("Independent transfer installation required.");
        var previous = Path.Combine(Environment.CurrentDirectory, "artifacts", "household-main-lifecycle-ui-d294", "original-lifecycle.complete");
        if (!File.Exists(previous)) throw new IOException("Preserve the original completed lifecycle fixture.");
        var profiles = services.GetRequiredService<ProfileService>();
        var path = Path.Combine(folder, "transfer-fixture.json");
        var progress = File.Exists(path) ? JsonSerializer.Deserialize<TransferReviewProgress>(await File.ReadAllTextAsync(path)) ?? throw new IOException() : null;
        if (progress is null)
        {
            if (!profiles.Current.IsMain) throw new IOException("Original completed main profile required.");
            progress = new(await HouseholdFinancialFingerprintAsync(services), null, false, 0, 0, null);
            await SaveAsync();
        }
        async Task SaveAsync()
        { await File.WriteAllTextAsync(path + ".writing", JsonSerializer.Serialize(progress)); File.Move(path + ".writing", path, true); }
        LocalProfile target;
        if (progress.Profile is null)
        {
            target = profiles.All.SingleOrDefault(item => item.Name == "Fictitious ownership recipient")!;
            if (target is null)
            {
                if (progress.Creating) throw new IOException("Continue original unpublished profile preparation; do not replace it.");
                progress = progress with { Creating = true }; await SaveAsync();
                target = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Fictitious ownership recipient");
            }
            progress = progress with { Profile = target.Id, Creating = false }; await SaveAsync();
        }
        else target = profiles.All.Single(item => item.Id == progress.Profile);
        if (profiles.Current.Id != target.Id && !await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), target)) throw new IOException("Original target switch refused.");
        var resumeUnconfirmedClaim = progress.Requested == progress.Completed + 1 && progress.Error is null &&
            Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_TRANSFER_RESUME") == "unconfirmed-claim";
        var resumeUnconfirmedDevice = progress.Requested == 16 && progress.Completed == 15 && progress.Error == "InvalidOperationException" &&
            Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_TRANSFER_RESUME") == "unconfirmed-device";
        var resumeReadonlyLayout = progress.Requested == 27 && progress.Completed == 26 && progress.Error == "IOException" &&
            Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_TRANSFER_RESUME") == "readonly-conflict-layout";
        if (progress.Requested != progress.Completed && !resumeUnconfirmedClaim && !resumeUnconfirmedDevice && !resumeReadonlyLayout) throw new IOException("Inspect the retained original action before continuing; do not replay a submission.");
        var keyPath = Path.Combine(FileSystem.AppDataDirectory, "fictitious-transfer-handoff.dpapi");
        if (!File.Exists(keyPath))
        {
            using var created = SigningRecipientIdentity.Create(); var clear = created.ExportForProtectedStorage();
            try { await File.WriteAllBytesAsync(keyPath, ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser)); }
            finally { CryptographicOperations.ZeroMemory(clear); }
        }
        var wrapped = await File.ReadAllBytesAsync(keyPath); var unwrapped = ProtectedData.Unprotect(wrapped, null, DataProtectionScope.CurrentUser);
        using var handoff = SigningRecipientIdentity.ImportFromProtectedStorage(unwrapped);
        CryptographicOperations.ZeroMemory(unwrapped); CryptographicOperations.ZeroMemory(wrapped);
        // Public encryption destination only. Actual household membership/device keys
        // are generated by the real Claim path, not this temporary delivery channel.
        await File.WriteAllTextAsync(Path.Combine(folder, "handoff-public.json"), JsonSerializer.Serialize(handoff.Public));
        using var trust = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(Environment.CurrentDirectory, "artifacts", "household-hosting-d294-root.crt"));
        string pin;
        using (var tcp = new TcpClient())
        {
            await tcp.ConnectAsync("localhost", 14645); using var tls = new SslStream(tcp.GetStream());
            await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "localhost", CertificateChainPolicy = new X509ChainPolicy
                { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { trust }, RevocationMode = X509RevocationMode.NoCheck } });
            pin = Convert.ToHexStringLower(SHA256.HashData(tls.RemoteCertificate!.GetRawCertData()));
        }
        var localization = services.GetRequiredService<ILocalizationService>(); await SetLanguageAsync(app, localization, "en");
        app.Windows[0].Width = 412; app.Windows[0].Height = 820;
        var translator = services.GetRequiredService<Translator>(); var vm = services.GetRequiredService<HouseholdViewModel>();
        var page = new HouseholdPage(vm); var body = (Grid)page.Content!;
        PageHeader.Attach(page, false); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page);
        for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
        // A hidden replacement root can omit Appearing after startup already
        // selected this same profile. Publish only the untouched covered local
        // read explicitly; never duplicate an in-flight read or a submitted command.
        if (!vm.IsReady && !vm.IsBusy && vm.Error is null) await vm.ActivateAsync();
        for (var wait = 0; wait < 1200 && vm.IsBusy && vm.Error is null; wait++) await Task.Delay(50);
        if (!vm.IsReady || vm.Error is not null) throw new IOException("Actual transfer page did not publish.");
        vm.Address = "https://localhost:14645/"; vm.Fingerprint = pin; vm.Name = "Fictitious Windows future owner";
        vm.ReviewActionFailure = (kind, status, result) => File.WriteAllText(Path.Combine(folder, "transfer-action-failure.json"),
            JsonSerializer.Serialize(new { Kind = kind, HttpStatus = status, HResult = result, Process = Environment.ProcessId }));
        var observations = new List<object>(); var scroll = body.Children.OfType<ScrollView>().Single();
        var observedExpectedRefusal = false;
        HouseholdCostsViewModel? conflictCosts = null; PersonalSyncViewModel? conflictReview = null; ContentPage? conflictPage = null;
        async Task InvokeAsync(IAsyncRelayCommand command, bool confirm, object? parameter = null, string? prompt = null, Page? reviewPage = null, string? confirmCaption = null)
        {
            var commandPage = reviewPage ?? page;
            var commandScroll = reviewPage is null ? scroll : FindScrollView(commandPage) ?? throw new IOException("Original scrolling page required.");
            var button = VisualDescendants(commandPage).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && (parameter is null || ReferenceEquals(item.CommandParameter, parameter)));
            if (prompt is not null)
            {
                if (prompt.Length != 64 || !prompt.All(Uri.IsHexDigit)) throw new IOException("Independent original public fingerprint required.");
                services.GetRequiredService<MauiAppInteraction>().ReviewPublicDevicePrompt =
                    (translator["Household_Approve"], translator["Household_DeviceFingerprintPrompt"], prompt);
            }
            var previous = command.ExecutionTask;
            await ScrollToViewIfNeededAsync(commandScroll, button); InvokeChoiceReview(button, observations);
            // Native Invoke may enqueue the click. Observe this exact command's
            // new execution instead of mistaking its previous completed task for it.
            for (var wait = 0; wait < 160 && (command.ExecutionTask is null || ReferenceEquals(command.ExecutionTask, previous)); wait++) await Task.Delay(50);
            var execution = command.ExecutionTask;
            if (execution is null || ReferenceEquals(execution, previous)) throw new IOException("Original native command did not start; preserve its submission.");
            if (confirm)
            {
                if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new IOException();
                Microsoft.UI.Xaml.FrameworkElement? answer = null;
                Microsoft.UI.Xaml.FrameworkElement? Find(Microsoft.UI.Xaml.DependencyObject node)
                {
                    if (node is Microsoft.UI.Xaml.FrameworkElement element && Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer &&
                        peer.GetName() == (confirmCaption ?? translator["Sync_Confirm"]) && peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider) return element;
                    for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                        if (Find(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index)) is { } found) return found;
                    return null;
                }
                for (var wait = 0; wait < 160 && answer is null; wait++)
                {
                    answer = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(item => Find(item.Child)).FirstOrDefault(item => item is not null);
                    if (answer is null) await Task.Delay(50);
                }
                if (answer is null) throw new IOException("Actual original confirmation missing; preserve pending command.");
                // Claim's private invitation remains behind its confirmation. Never
                // capture that editor or capability, even through a modal overlay.
                if (!ReferenceEquals(command, vm.ClaimCommand)) await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, "confirm-" + progress!.Requested + ".png"));
                ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(answer)!
                    .GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
            }
            await execution;
            services.GetRequiredService<MauiAppInteraction>().ReviewPublicDevicePrompt = null;
        }
        async Task ClaimOriginalAsync(TransferReviewAction action)
        {
            if (action.RootFingerprint is not { Length: 64 } || !action.RootFingerprint.All(Uri.IsHexDigit)) throw new IOException("Independently compared root required.");
            var envelopePath = Path.Combine(folder, "android-invitation.jwe");
            if (new FileInfo(envelopePath).Length is < 1 or > 32768) throw new IOException("Bounded encrypted invitation required.");
            var clear = handoff.DecryptRecipientEnvelope((await File.ReadAllTextAsync(envelopePath)).Trim());
            try
            {
                var invitation = JsonSerializer.Deserialize(clear, HouseholdMetadataJsonContext.Default.HouseholdInvitationPackage) ?? throw new IOException();
                if (new PublicSigningRecipientIdentity(invitation.OwnerKeys.SigningKey, invitation.OwnerKeys.WrappingKey).Fingerprint != action.RootFingerprint) throw new IOException("Original independent root differs.");
                vm.InvitationText = Encoding.UTF8.GetString(clear); vm.ConfirmedRoot = action.RootFingerprint;
                await InvokeAsync(vm.ClaimCommand, true);
            }
            finally { vm.InvitationText = ""; CryptographicOperations.ZeroMemory(clear); }
        }
        if (resumeUnconfirmedClaim)
        {
            var originalAction = JsonSerializer.Deserialize<TransferReviewAction>(await File.ReadAllTextAsync(Path.Combine(folder, "transfer-control.json"))) ?? throw new IOException();
            using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "transfer-public-proof.json")));
            if (originalAction.Action != "claim" || originalAction.Sequence != progress.Requested || prior.RootElement.GetProperty("Profile").GetString() != target.Id ||
                prior.RootElement.GetProperty("Completed").GetInt32() != progress.Completed || prior.RootElement.GetProperty("HasMembership").GetBoolean() ||
                prior.RootElement.GetProperty("DeviceId").ValueKind != JsonValueKind.Null) throw new IOException("Original unconfirmed claim observations differ.");
            await InvokeAsync(vm.DiscoverCommand, false);
            using var channel = new HouseholdServiceTransport(new(vm.Address, vm.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>());
            var observedIdentity = (await channel.IdentityAsync()).Id; var observedHash = await HouseholdFinancialFingerprintAsync(services);
            var initializedEmptyUnits = false;
            if (prior.RootElement.GetProperty("FinancialHash").GetString() != observedHash)
            {
                // Only this guarded fictitious installation: inspect table counts
                // and allowlisted display/financial settings, never key/session rows.
                await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
                await db.Database.OpenConnectionAsync(); await using var snapshot = await db.Database.BeginTransactionAsync();
                using var query = db.Database.GetDbConnection().CreateCommand(); query.Transaction = snapshot.GetDbTransaction();
                var counts = new SortedDictionary<string, long>(StringComparer.Ordinal);
                foreach (var table in HouseholdFinancialSchema.Manifest.Tables)
                { query.CommandText = "SELECT COUNT(*) FROM \"" + table.Name + "\""; counts[table.Name] = Convert.ToInt64(await query.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture); }
                var columns = HouseholdFinancialSchema.Manifest.Tables.Single(table => table.Name == "Settings").Columns;
                query.CommandText = "SELECT " + string.Join(",", columns.Select(column => "\"" + column.Name + "\"")) + " FROM \"Settings\"";
                using var rows = await query.ExecuteReaderAsync(); var settings = new List<object?[]>();
                while (await rows.ReadAsync()) settings.Add(Enumerable.Range(0, rows.FieldCount).Select(index => rows.IsDBNull(index) ? null : rows.GetValue(index)).ToArray());
                string? normalizedOriginal = null;
                var units = Array.FindIndex(columns, column => column.Name == "DisplayUnits");
                if (counts.All(item => item.Value == (item.Key == "Settings" ? 1 : 0)) && settings.Count == 1 && units >= 0 &&
                    settings[0][units] is string { Length: 0 } && Preferences.Default.Get("display_units", string.Empty).Length == 0)
                {
                    // Covered startup legitimately copies an unset profile DisplayUnits
                    // from the legacy device preference once (App.PrepareStartupAsync).
                    // Prove the entire old hash by changing exactly that one cell in
                    // memory; do not edit storage, ignore other columns or weaken a
                    // later full source/recipient financial equality comparison.
                    var originalTables = new SortedDictionary<string, List<string>>();
                    foreach (var name in counts.Keys) originalTables[name] = [];
                    var originalSettings = settings[0].ToArray(); originalSettings[units] = null;
                    originalTables["Settings"].Add(JsonSerializer.Serialize(originalSettings));
                    var serialized = JsonSerializer.SerializeToUtf8Bytes(originalTables);
                    try { normalizedOriginal = Convert.ToHexStringLower(SHA256.HashData(serialized)); }
                    finally { CryptographicOperations.ZeroMemory(serialized); }
                    initializedEmptyUnits = normalizedOriginal == prior.RootElement.GetProperty("FinancialHash").GetString();
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "original-empty-profile-shape-" + Environment.ProcessId + ".json"), JsonSerializer.Serialize(new
                { Profile = profiles.Current.Id, Counts = counts, SettingsColumns = columns.Select(column => column.Name), SettingsRows = settings,
                    NormalizedOriginalHash = normalizedOriginal, ExactEmptyLegacyUnitsInitialization = initializedEmptyUnits, StorageModifiedByInspection = false }));
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "unconfirmed-continuation-observation-" + Environment.ProcessId + ".json"), JsonSerializer.Serialize(new
            { vm.CanSetUp, vm.IsReady, vm.IsBusy, HasError = vm.Error is not null, ObservedIdentity = observedIdentity, FinancialHash = observedHash,
                OriginalHash = prior.RootElement.GetProperty("FinancialHash").GetString(), ActualNativeExecutionObserved = true }));
            if (!vm.CanSetUp || vm.Error is not null || observedIdentity != "df8f98b5-121e-412a-b996-b69b19808e35" ||
                prior.RootElement.GetProperty("FinancialHash").GetString() != observedHash && !initializedEmptyUnits)
                throw new IOException("Original profile is not still empty and without a household journal; preserve it.");
            // The real Claim journal persists before any service effect. No journal,
            // no claimed server device and the captured unconfirmed modal establish
            // that only this original local UI consent needs continuation.
            await ClaimOriginalAsync(originalAction);
            progress = progress with { Completed = originalAction.Sequence, Error = vm.Error is null ? null : "ActualActionFailed" }; await SaveAsync();
        }
        async Task OpenOriginalConflictAsync()
        {
                        if ((await services.GetRequiredService<PersonalSyncStore>().ConflictsAsync()).Count == 0) throw new IOException("Original authenticated conflict required.");
                        conflictReview = services.GetRequiredService<PersonalSyncViewModel>(); conflictPage = new PersonalSyncPage(conflictReview);
                        PageHeader.Attach(conflictPage, false); NavigationPage.SetHasNavigationBar(conflictPage, false); app.ShowHouseholdReview(conflictPage);
                        for (var wait = 0; wait < 40 && !conflictReview.IsReady && !conflictReview.IsBusy && conflictReview.Error is null; wait++) await Task.Delay(50);
                        if (!conflictReview.IsReady && !conflictReview.IsBusy && conflictReview.Error is null) await conflictReview.ActivateAsync();
                        for (var wait = 0; wait < 1200 && conflictReview.IsBusy && conflictReview.Error is null; wait++) await Task.Delay(50);
                        if (!conflictReview.IsReady || conflictReview.Error is not null || !conflictReview.IsHouseholdConnection || conflictReview.CanConnect || conflictReview.ShowPersonalConnection || conflictReview.Conflicts.Count == 0) throw new IOException("Original encrypted household review did not publish.");
                        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"actual-household-conflict-{Environment.ProcessId}-before-choice.png"));
        }
        async Task ReportAsync()
        {
            var tokens = services.GetRequiredService<IPersonalSyncTokenStore>();
            using var channel = new HouseholdServiceTransport(new(vm.Address, vm.Fingerprint), tokens);
            HouseholdJournalState? state = null;
            if (vm.HasMembership || vm.IsOwnerRecoveryDraft || !vm.CanSetUp)
            {
                try { state = await services.GetRequiredService<HouseholdJournal>().CaptureAsync(channel.Peer, (await channel.IdentityAsync()).Id); }
                catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.Unauthorized) { }
            }
            var unresolved = await services.GetRequiredService<PersonalSyncStore>().ConflictsAsync();
            var report = JsonSerializer.Serialize(new { Process = Environment.ProcessId, progress!.Requested, progress.Completed, progress.Error,
                Profile = profiles.Current.Id, NeedsReview = unresolved.Count > 0,
                OriginalPage = conflictPage?.GetType().Name ?? nameof(HouseholdPage),
                CostReady = conflictCosts?.IsReady, CostEditing = conflictCosts?.EditingCost,
                CostDraftPrepared = conflictCosts?.Note == "Fictitious Windows competing cost note", CostHasError = conflictCosts?.Error is not null,
                HouseholdConflictReview = conflictReview?.IsHouseholdConnection,
                ReviewItems = conflictReview?.ReviewItems.Select(item => new { item.Title, item.CanChooseLocal, item.CanChooseRemote,
                    Versions = item.Versions, Table = item.Review.Conflict.Proposal.Record.Table }),
                vm.IsReady, vm.IsBusy, HasError = conflictPage is PersonalSyncPage ? conflictReview?.Error is not null : conflictPage is HouseholdCostsPage ? conflictCosts?.Error is not null : vm.Error is not null, vm.IsOwner, vm.HasMembership, vm.CanCheckAdmission,
                vm.CanSynchronize, vm.CanAdminister, vm.CanSetUp, vm.CanReadAudit, RefusalObserved = observedExpectedRefusal, PublicFingerprintLabels = vm.Fingerprints,
                PendingDeviceRequests = vm.PendingDevices.Select(item => new { item.Source.IdentityId, item.Source.DeviceId,
                    Fingerprint = new PublicSigningRecipientIdentity(item.Source.DeviceKeys.SigningKey, item.Source.DeviceKeys.WrappingKey).Fingerprint }),
                ApprovedDevices = vm.ApprovedDevices.Select(item => new { item.IdentityId, item.Source.Id, item.CanRevoke,
                    Fingerprint = new PublicSigningRecipientIdentity(item.Source.Keys.SigningKey, item.Source.Keys.WrappingKey).Fingerprint }),
                vm.HasOwnershipTransfer, vm.CanAdvanceOwnership, vm.CanCancelOwnership, vm.CanFinishOwnership, vm.OwnershipStatus,
                state?.SpaceId, state?.DeviceId, state?.DeviceFingerprint, FinancialHash = await HouseholdFinancialFingerprintAsync(services),
                NativeObservations = observations.Count, WholeBatchOrPhoneAcceptance = false });
            var reportPath = Path.Combine(folder, "transfer-public-proof.json"); await File.WriteAllTextAsync(reportPath + ".writing", report); File.Move(reportPath + ".writing", reportPath, true);
        }
        await ReportAsync();
        if (resumeUnconfirmedDevice)
        {
            var originalControl = JsonSerializer.Deserialize<TransferReviewAction>(await File.ReadAllTextAsync(Path.Combine(folder, "transfer-control.json"))) ?? throw new IOException();
            if (originalControl.Sequence != 16 || originalControl.Action != "device-approve" || originalControl.RootFingerprint != "5aa2af161b888ed7c08fd50762d8509724bb45474eba298624859a33b049046f" ||
                await HouseholdFinancialFingerprintAsync(services) != "4f28c370feb93b0e131223bf6638e24d30f2b29b0e749f2cabaf35271ff25592") throw new IOException("Exact unconfirmed original device action required.");
            await InvokeAsync(vm.DiscoverCommand, false);
            await InvokeAsync(vm.RefreshMembersCommand, false);
            var pendingDevice = vm.PendingDevices.Single(item => item.Source.DeviceId == Guid.Parse("f4f4d6ec-40f4-4f6b-9772-5e4cf928f928") &&
                new PublicSigningRecipientIdentity(item.Source.DeviceKeys.SigningKey, item.Source.DeviceKeys.WrappingKey).Fingerprint == originalControl.RootFingerprint);
            // The original command stopped before confirmation and HTTP. Actual
            // owner-scoped pending metadata must still contain that same device.
            await InvokeAsync(vm.ApproveDeviceCommand, true, pendingDevice, originalControl.RootFingerprint);
            progress = progress with { Completed = 16, Error = vm.Error is null ? null : "ActualActionFailed" }; await SaveAsync(); await ReportAsync();
            if (progress.Error is not null) throw new IOException("Original device confirmation failed; preserve its request.");
        }
        if (resumeReadonlyLayout)
        {
            var originalReadonly = JsonSerializer.Deserialize<TransferReviewAction>(await File.ReadAllTextAsync(Path.Combine(folder, "transfer-control.json"))) ?? throw new IOException();
            if (originalReadonly.Sequence != 27 || originalReadonly.Action != "review-layout" || originalReadonly.RootFingerprint is not null ||
                await HouseholdFinancialFingerprintAsync(services) != "22a9eaa0958b8a970ed69ee6a3dfe65ad7dc3c03fc9cd3d1af3ae0921a476bea") throw new IOException("Exact original readonly review required.");
            await OpenOriginalConflictAsync();
            progress = progress with { Error = null }; await SaveAsync(); await ReportAsync();
        }
        while (true)
        {
            var control = Path.Combine(folder, "transfer-control.json");
            if (!File.Exists(control)) { await Task.Delay(500); continue; }
            var info = new FileInfo(control); if (info.Length is < 1 or > 4096) throw new IOException("Bounded public transfer control required.");
            var action = JsonSerializer.Deserialize<TransferReviewAction>(await File.ReadAllTextAsync(control)) ?? throw new IOException();
            if (action.Sequence <= progress!.Completed) { await Task.Delay(500); continue; }
            if (action.Sequence != progress.Completed + 1 || progress.Error is not null) throw new IOException("Inspect original failed action; no automatic replay.");
            progress = progress with { Requested = action.Sequence }; await SaveAsync();
            try
            {
                switch (action.Action)
                {
                    case "signin":
                        // Existing separately verified example.invalid identity only.
                        vm.Email = "household-main-lifecycle@example.invalid"; vm.Password = "Fictitious-Household-Hosting9!";
                        await InvokeAsync(vm.SignInCommand, false); break;
                    case "claim":
                        await ClaimOriginalAsync(action);
                        break;
                    case "discover": await InvokeAsync(vm.DiscoverCommand, false); break;
                    case "admission": await InvokeAsync(vm.CheckAdmissionCommand, false); break;
                    case "sync": await InvokeAsync(vm.SynchronizeCommand, false); break;
                    case "refusal-sync":
                        if (conflictPage is not null || !vm.CanSynchronize || !vm.CanAdminister) throw new IOException("Actual positive warm presentation required before the scoped refusal.");
                        await InvokeAsync(vm.SynchronizeCommand, false);
                        using (var refusedChannel = new HouseholdServiceTransport(new(vm.Address, vm.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>()))
                        {
                            var refusedState = await services.GetRequiredService<HouseholdJournal>().CaptureAsync(refusedChannel.Peer, "df8f98b5-121e-412a-b996-b69b19808e35") ?? throw new IOException();
                            var refusedAccess = await services.GetRequiredService<HouseholdJournal>().ReadAccessAsync(refusedState.Authority);
                            if (vm.Error is null || vm.CanSynchronize || vm.CanAdminister || vm.CanSetUp || refusedAccess.BlockedBy != HouseholdWriteBlockReason.RemoteRefused) throw new IOException("The original warm scoped refusal did not close presentation authority.");
                        }
                        observedExpectedRefusal = true; break;
                    case "cost-open":
                        if (conflictPage is not null) throw new IOException("Finish the original displayed page first.");
                        await InvokeAsync(vm.DiscoverCommand, false); await InvokeAsync(vm.RefreshMembersCommand, false);
                        conflictCosts = services.GetRequiredService<HouseholdCostsViewModel>(); conflictPage = new HouseholdCostsPage(conflictCosts);
                        PageHeader.Attach(conflictPage, false); NavigationPage.SetHasNavigationBar(conflictPage, false); app.ShowHouseholdReview(conflictPage);
                        for (var wait = 0; wait < 1200 && (!conflictCosts.IsReady || conflictCosts.IsBusy) && conflictCosts.Error is null; wait++) await Task.Delay(50);
                        if (!conflictCosts.IsReady && !conflictCosts.IsBusy && conflictCosts.Error is null) await conflictCosts.ActivateAsync();
                        var originalCost = conflictCosts.Costs.Single(item => item.Source.Id == Guid.Parse("790d83c4-8b2d-44f2-8997-a6c1b0211658"));
                        if (!conflictCosts.CanWrite || originalCost.Source.Amount != 1001 || originalCost.Source.CurrencyCode != "KWD" || originalCost.Source.Note is not null)
                            throw new IOException("Original unedited actual cost required; never replay the editor Save.");
                        await InvokeAsync(conflictCosts.EditCostCommand, false, originalCost, reviewPage: conflictPage);
                        if (!conflictCosts.EditingCost || conflictCosts.Shares.Count != 2) throw new IOException("Original complete cost editor required.");
                        conflictCosts.Note = "Fictitious Windows competing cost note"; break;
                    case "cost-save":
                        if (conflictCosts is not { EditingCost: true, CanWrite: true } || conflictPage is null || conflictCosts.Note != "Fictitious Windows competing cost note") throw new IOException();
                        await InvokeAsync(conflictCosts.SaveCommand, true, reviewPage: conflictPage, confirmCaption: translator["Common_Save"]);
                        if (conflictCosts.Error is not null || conflictCosts.EditingCost) throw new IOException("Actual original native cost Save failed; retain its draft."); break;
                    case "household":
                        if (conflictCosts?.EditingCost == true) throw new IOException("Original draft must be explicitly completed first.");
                        conflictPage = null; conflictCosts = null; conflictReview = null;
                        vm = services.GetRequiredService<HouseholdViewModel>(); page = new HouseholdPage(vm); body = (Grid)page.Content!;
                        PageHeader.Attach(page, false); NavigationPage.SetHasNavigationBar(page, false); app.ShowHouseholdReview(page); scroll = body.Children.OfType<ScrollView>().Single();
                        for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
                        if (!vm.IsReady && !vm.IsBusy && vm.Error is null) await vm.ActivateAsync();
                        vm.Address = "https://localhost:14645/"; vm.Fingerprint = pin;
                        await InvokeAsync(vm.DiscoverCommand, false); break;
                    case "review-open": await OpenOriginalConflictAsync(); break;
                    case "review-layout":
                        if (conflictReview is null || conflictPage is null || conflictReview.ReviewItems.Count != 1) throw new IOException("Original complete linked conflict required.");
                        var frozen = conflictReview.ReviewItems.Single().Review; var originalFinance = await HouseholdFinancialFingerprintAsync(services);
                        var layoutPath = Path.Combine(folder, "main-original-conflict-layout-cases.json");
                        var layoutCases = File.Exists(layoutPath) ? JsonSerializer.Deserialize<List<OriginalConflictLayoutCase>>(await File.ReadAllTextAsync(layoutPath)) ?? throw new IOException() : [];
                        if (layoutCases.Any(item => item.Operation != frozen.Operation || item.Digest != frozen.Digest || item.FinancialHash != originalFinance)) throw new IOException("A completed layout belongs to another original review.");
                        var themes = services.GetRequiredService<ThemeService>(); var currentLanguage = services.GetRequiredService<ILocalizationService>();
                        foreach (var language in new[] { "en", "fa", "de" })
                        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
                        foreach (var width in new[] { 360, 412, 1280 })
                        {
                            if (layoutCases.Any(item => item.Language == language && item.Theme == theme.ToString() && item.Width == width)) continue;
                            themes.Set(theme); await SetLanguageAsync(app, currentLanguage, language); app.Windows[0].Width = width; app.Windows[0].Height = 820;
                            conflictReview = services.GetRequiredService<PersonalSyncViewModel>();
                            conflictPage = new PersonalSyncPage(conflictReview) { FlowDirection = currentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                            PageHeader.Attach(conflictPage, currentLanguage.IsRightToLeft); NavigationPage.SetHasNavigationBar(conflictPage, false); app.ShowHouseholdReview(conflictPage);
                            for (var wait = 0; wait < 40 && !conflictReview.IsReady && !conflictReview.IsBusy && conflictReview.Error is null; wait++) await Task.Delay(50);
                            if (!conflictReview.IsReady && !conflictReview.IsBusy && conflictReview.Error is null) await conflictReview.ActivateAsync();
                            for (var wait = 0; wait < 1200 && conflictReview.IsBusy && conflictReview.Error is null; wait++) await Task.Delay(50);
                            if (!conflictReview.IsReady || conflictReview.Error is not null) throw new IOException("Original covered conflict case did not publish.");
                            await Task.Delay(250);
                            var selected = conflictReview.ReviewItems.Single();
                            if (selected.Review.Operation != frozen.Operation || selected.Review.Digest != frozen.Digest || selected.Review.LocalSequence != frozen.LocalSequence ||
                                conflictReview.CanConnect || conflictReview.ShowPersonalConnection || !selected.CanChooseLocal || !selected.CanChooseRemote) throw new IOException("The immutable original household review changed.");
                            foreach (var label in VisualDescendants(conflictPage).OfType<Label>().Where(label => Visible(label) && label.FontAutoScalingEnabled && !string.IsNullOrEmpty(label.Text))) CheckCaption(label, observations);
                            foreach (var version in selected.Versions)
                            foreach (var field in version.Fields.Where(item => item.Value.Length != 0))
                                if (!VisualDescendants(conflictPage).OfType<Label>().Any(label => label.Text == field.Value && Visible(label))) throw new IOException("A complete actual compared field is absent.");
                            var conflictScroll = FindScrollView(conflictPage) ?? throw new IOException();
                            var note = VisualDescendants(conflictPage).OfType<Label>().Single(item => item.Text == "Fictitious Windows competing cost note");
                            await ScrollToViewIfNeededAsync(conflictScroll, note);
                            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"main-conflict-{language}-{theme}-{width}-versions.png"));
                            foreach (var command in new IAsyncRelayCommand[] { conflictReview.KeepLocalCommand, conflictReview.KeepRemoteCommand })
                            {
                                var decisionTarget = VisualDescendants(conflictPage).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
                                await ScrollToViewIfNeededAsync(conflictScroll, decisionTarget);
                                if (!ReferenceEquals(decisionTarget.CommandParameter, selected.Review) || decisionTarget.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native ||
                                    native.ActualWidth < 44 || native.ActualHeight < 44 || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) !=
                                    (ReferenceEquals(command, conflictReview.KeepRemoteCommand) ? selected.RemoteAction : selected.LocalAction)) throw new IOException("Original complete native decision target changed.");
                            }
                            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"main-conflict-{language}-{theme}-{width}-actions.png"));
                            if (originalFinance != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Layout review changed original financial rows.");
                            layoutCases.Add(new(language, theme.ToString(), width, frozen.Operation, frozen.Digest, originalFinance));
                            await File.WriteAllTextAsync(layoutPath + ".writing", JsonSerializer.Serialize(layoutCases)); File.Move(layoutPath + ".writing", layoutPath, true);
                        }
                        await File.WriteAllTextAsync(Path.Combine(folder, "main-original-conflict-layout-proof.json"), JsonSerializer.Serialize(new { Cases = layoutCases.Count, NativeObservations = observations.Count,
                            OriginalFinancialColumnsUnchanged = true, OriginalConflictRetained = true, NativeDecisionInvoked = false }));
                        break;
                    case "review-remote":
                        if (conflictReview is null || conflictPage is null) throw new IOException();
                        var compared = conflictReview.ReviewItems.Single();
                        if (!compared.CanChooseRemote || compared.Review.Conflict.LinkedGroup is null ||
                            !new[] { compared.Review }.Concat(compared.Review.GroupMembers ?? []).Any(item => item.Conflict.Proposal.Record.Table == "HouseholdCosts") ||
                            !compared.Versions.SelectMany(item => item.Fields).Any(item => item.Value == "Fictitious Windows competing cost note") ||
                            !compared.Versions.SelectMany(item => item.Fields).Any(item => item.Value == "Fictitious Android competing cost note")) throw new IOException("The whole original cost/allocation comparison is required.");
                        await InvokeAsync(conflictReview.KeepRemoteCommand, true, compared.Review, reviewPage: conflictPage);
                        if (conflictReview.Error is not null || conflictReview.Conflicts.Count != 0) throw new IOException("Actual original conflict decision failed."); break;
                    case "devices": await InvokeAsync(vm.RefreshMembersCommand, false); break;
                    case "device-revoke":
                        if (conflictPage is not null || !vm.IsOwner || action.RootFingerprint != "5aa2af161b888ed7c08fd50762d8509724bb45474eba298624859a33b049046f") throw new IOException("Original noncurrent approved device required.");
                        await InvokeAsync(vm.RefreshMembersCommand, false);
                        var revokeDevice = vm.ApprovedDevices.Single(item => item.Source.Id == Guid.Parse("f4f4d6ec-40f4-4f6b-9772-5e4cf928f928") && item.CanRevoke &&
                            new PublicSigningRecipientIdentity(item.Source.Keys.SigningKey, item.Source.Keys.WrappingKey).Fingerprint == action.RootFingerprint);
                        await InvokeAsync(vm.RevokeDeviceCommand, true, revokeDevice, confirmCaption: translator["Household_RevokeDevice"]); break;
                    case "device-export":
                        await InvokeAsync(vm.CreateDeviceAccessCommand, true, vm.Members.Single(item => item.Source.IdentityId == "df8f98b5-121e-412a-b996-b69b19808e35"));
                        if (HouseholdSetupAnchors.Kind(vm.InvitationText) != HouseholdSetupAnchors.DeviceAccess) throw new IOException("Actual public member authorization required.");
                        await File.WriteAllTextAsync(Path.Combine(folder, "main-owner-device-access.json"), vm.InvitationText); vm.InvitationText = ""; break;
                    case "device-approve":
                        var pending = vm.PendingDevices.Single(item => new PublicSigningRecipientIdentity(item.Source.DeviceKeys.SigningKey, item.Source.DeviceKeys.WrappingKey).Fingerprint == action.RootFingerprint);
                        await InvokeAsync(vm.ApproveDeviceCommand, true, pending, action.RootFingerprint); break;
                    case "ownership": await InvokeAsync(vm.ReadOwnershipCommand, false); break;
                    case "advance": await InvokeAsync(vm.AdvanceOwnershipCommand, true); break;
                    case "finish": await InvokeAsync(vm.FinishOwnershipCommand, true); break;
                    case "stop":
                        if (vm.IsBusy || !await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), profiles.All.Single(item => item.IsMain)) ||
                            progress.Original != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Original lifecycle financial data changed.");
                        progress = progress with { Completed = action.Sequence }; await SaveAsync();
                        await File.WriteAllTextAsync(Path.Combine(folder, "original-profile-preserved.complete"), "Original complete financial columns preserved after independent transfer review."); return;
                    default: throw new IOException("Unknown public transfer action.");
                }
                var expectedConflict = action.Action == "sync" && vm.Error == translator["Household_ConflictReview"] && (await services.GetRequiredService<PersonalSyncStore>().ConflictsAsync()).Count > 0;
                progress = progress with { Completed = action.Sequence, Error = vm.Error is null || expectedConflict || observedExpectedRefusal || action.Action == "review-open" || action.Action == "review-remote" ? null : "ActualActionFailed" }; await SaveAsync(); await ReportAsync();
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            { progress = progress with { Error = error.GetType().Name }; await SaveAsync(); await ReportAsync(); throw; }
        }
    }

    /// <summary>Public original profile ids/hash and submission sequence only; no invitation, credential or key.</summary>
    private sealed record TransferReviewProgress(string Original, string? Profile, bool Creating, int Requested, int Completed, string? Error);
    /// <summary>Completed readonly native case over the same original financial and conflict snapshots.</summary>
    private sealed record OriginalConflictLayoutCase(string Language, string Theme, int Width, Guid Operation, string Digest, string FinancialHash);
    /// <summary>Only an explicit command and independently compared public root digest, never a private form value.</summary>
    private sealed record TransferReviewAction(int Sequence, string Action, string? RootFingerprint);
}
#endif
