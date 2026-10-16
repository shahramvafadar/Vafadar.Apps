#if DEBUG && ANDROID
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Independent main-page recovery qualification; real native buttons retain every consequential operation.</summary>
internal static class DebugHouseholdRecoveryAndroid
{
    private const string Package = "pro.vafadar.zanance.householdlifecycle";
    // Only this explicit example.invalid fixture uses this in-memory input. It is
    // never persisted as a preference, included in a report or supplied in arguments.
    private const string FixturePassword = "Fictitious-Recovery-Android9!";
    private static HouseholdViewModel? active;
    private static ILocalizationService? activeLocalization;
    private static ThemeService? activeThemes;
    private static HouseholdPage? activePage;
    private static string? activeDisplayMode;

    /// <summary>Supplies fictitious form inputs or app-only display choices; never submits a household command or confirmation.</summary>
    internal static void ApplyInputMode(string? mode)
    {
        if (AppInfo.Current.PackageName != Package || Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_RECOVERY_REVIEW") != "1" || active is not { IsBusy: false } vm) return;
        if (mode?.Split('-') is ["view", var language, var theme] && language is "en" or "fa" or "de" && theme is "light" or "dark" &&
            activeLocalization is { } localization && activeThemes is { } themes && activePage is { } page)
        {
            // Application-only display changes on this same retained page/state.
            // The operator still invokes its actual native Reload afterwards; no
            // repeated rejected service rebind is needed for translated UI review.
            localization.SetLanguage(localization.SupportedLanguages.Single(item => item.CultureName == language));
            themes.Set(theme == "dark" ? ThemeChoice.Dark : ThemeChoice.Light);
            page.FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            activeDisplayMode = language + "-" + theme;
            return;
        }
        if (mode is not ("wrong" or "valid")) return;
        vm.RecoveryPassword = mode == "wrong" ? "Fictitious-Incorrect-Recovery9!" : FixturePassword;
        vm.RecoveryPasswordConfirm = vm.RecoveryPassword;
    }

    /// <summary>Publishes the actual page against retained fictitious profiles, with bounded public observations.</summary>
    internal static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (AppInfo.Current.PackageName != Package || Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_RECOVERY_REVIEW") != "1") return false;
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var folder = FileSystem.AppDataDirectory; var phase = "startup";
            try
            {
                await app.WaitForStartupAsync();
                if (!folder.Contains(Package, StringComparison.Ordinal)) throw new IOException("Independent storage required.");
                var stage = Platform.CurrentActivity?.Intent?.GetStringExtra("review_stage");
                if (stage is not ("owner" or "export" or "restore" or "cold" or "original" or "transfer" or "device" or "device-cold")) throw new IOException("Explicit recovery stage required.");
                services.GetRequiredService<ILocalizationService>().SetLanguage(services.GetRequiredService<ILocalizationService>().SupportedLanguages.Single(item => item.CultureName == "en"));
                var profiles = services.GetRequiredService<ProfileService>();
                var path = Path.Combine(folder, "household-main-recovery-fixture.json");
                RecoveryFixture fixture;
                if (File.Exists(path)) fixture = JsonSerializer.Deserialize<RecoveryFixture>(await File.ReadAllTextAsync(path)) ?? throw new IOException("Invalid original fixture.");
                else
                {
                    if (stage != "owner" || !profiles.Current.IsMain || !File.Exists(Path.Combine(folder, "household-financial-baseline.json"))) throw new IOException("Completed original lifecycle fixture required.");
                    using var original = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "household-main-android-proof.json")));
                    if (!original.RootElement.GetProperty("HasRemoteDeletion").GetBoolean() || !original.RootElement.GetProperty("LocalMoneyPreserved").GetBoolean())
                        throw new IOException("Original completed deletion and preserved finances required.");
                    fixture = new(await DebugSnapshots.HouseholdFinancialFingerprintAsync(services), null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
                    await SaveAsync();
                }
                async Task SaveAsync() { await File.WriteAllTextAsync(path + ".writing", JsonSerializer.Serialize(fixture)); File.Move(path + ".writing", path, true); }
                async Task<LocalProfile> CreateOnceAsync(string name)
                {
                    var published = profiles.All.SingleOrDefault(item => item.Name == name);
                    if (published is not null)
                    {
                        if (fixture.PendingName == name) { fixture = fixture with { PendingName = null }; await SaveAsync(); }
                        return published;
                    }
                    // Persist intent before preparation. An interrupted unpublished generation
                    // must be inspected/continued, never silently replaced with a new key.
                    if (fixture.PendingName is not null) throw new IOException("Original profile preparation needs continuation.");
                    fixture = fixture with { PendingName = name }; await SaveAsync();
                    var created = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), name);
                    fixture = fixture with { PendingName = null }; await SaveAsync(); return created;
                }
                phase = "profile";
                LocalProfile selected;
                if (stage is "device" or "device-cold")
                {
                    if (stage == "device-cold" && fixture.Device is null) throw new IOException("Original pending device profile required.");
                    selected = fixture.Device is null ? await CreateOnceAsync("Fictitious household second owner device") : profiles.All.Single(item => item.Id == fixture.Device);
                    fixture = fixture with { Device = selected.Id }; await SaveAsync();
                }
                else if (stage == "original") selected = profiles.All.Single(item => item.IsMain);
                else if (stage == "restore")
                {
                    if (fixture.Before is null) throw new IOException("Original complete financial baseline required.");
                    selected = fixture.Recovered is null ? await CreateOnceAsync("Fictitious household recovered") : profiles.All.Single(item => item.Id == fixture.Recovered);
                    fixture = fixture with { Recovered = selected.Id }; await SaveAsync();
                }
                else if (stage is "cold" or "transfer") selected = profiles.All.Single(item => item.Id == fixture.Recovered);
                else
                {
                    selected = fixture.Owner is null ? await CreateOnceAsync("Fictitious household recovery") : profiles.All.Single(item => item.Id == fixture.Owner);
                    fixture = fixture with { Owner = selected.Id }; await SaveAsync();
                }
                if (profiles.Current.Id != selected.Id && !await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), selected)) throw new IOException("Reviewed profile switch refused.");
                if (stage == "original" && fixture.Original != await DebugSnapshots.HouseholdFinancialFingerprintAsync(services)) throw new IOException("Original lifecycle finances changed.");
                phase = "peer";
                using var trust = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(folder, "household-public-root.crt"));
                string pin;
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync("localhost", 14645); using var tls = new SslStream(tcp.GetStream());
                    await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "localhost", CertificateChainPolicy = new X509ChainPolicy
                        { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { trust }, RevocationMode = X509RevocationMode.NoCheck } });
                    pin = Convert.ToHexStringLower(SHA256.HashData(tls.RemoteCertificate!.GetRawCertData()));
                }
                if (stage is not ("device" or "device-cold"))
                using (var channel = new HouseholdServiceTransport(new("https://localhost:14645/", pin), services.GetRequiredService<IPersonalSyncTokenStore>()))
                    if ((await channel.IdentityAsync()).Id != "448dfe96-c60c-419d-ae38-f33d9ba847ef") throw new IOException("Only original fictional identity allowed.");
                phase = "seed";
                if (stage == "export" && fixture.Before is null)
                {
                    var store = services.GetRequiredService<ZananceStore>(); var costs = services.GetRequiredService<HouseholdCostStore>();
                    if (!(await store.GetAccountsAsync()).Any(item => item.Id == fixture.Account)) await store.SaveAccountAsync(new Account(fixture.Account)
                        { Name = "Fictitious recovery cash", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = new(2026, 10, 16) });
                    if (!(await store.GetEntriesAsync()).Any(item => item.Id == fixture.Entry) && !(await store.SaveEntryAsync(new LedgerEntry(fixture.Entry)
                        { AccountId = fixture.Account, Kind = EntryKind.Expense, Amount = 450, Date = new(2026, 10, 16), Title = "Fictitious recovery expense" })).Succeeded) throw new IOException("Guarded cash writer refused.");
                    var review = await costs.ReadAsync();
                    if (!review.Contacts.Any(item => item.Id == fixture.First)) await costs.SaveContactAsync(review, new(fixture.First) { Name = "Fictitious payer" });
                    review = await costs.ReadAsync();
                    if (!review.Contacts.Any(item => item.Id == fixture.Second)) await costs.SaveContactAsync(review, new(fixture.Second) { Name = "Fictitious participant" });
                    review = await costs.ReadAsync();
                    if (!review.Costs.Any(item => item.Id == fixture.Cost)) await costs.SaveCostAsync(review, new(fixture.Cost)
                        { Kind = HouseholdCostKind.Expense, Date = new(2026, 10, 16), Amount = 1001, CurrencyCode = "KWD", PaidByContactId = fixture.First,
                            PaymentKind = HouseholdPaymentKind.Private, Title = "Fictitious private shared cost" },
                        [new() { CostId = fixture.Cost, ContactId = fixture.First, Amount = 501 }, new() { CostId = fixture.Cost, ContactId = fixture.Second, Amount = 500 }]);
                    fixture = fixture with { Before = await DebugSnapshots.HouseholdFinancialFingerprintAsync(services) }; await SaveAsync();
                }
                phase = "page";
                PublicSigningRecipientIdentity? handoff = null;
                if (stage == "transfer")
                {
                    var publicPath = Path.Combine(folder, "household-transfer-recipient-public.json");
                    if (new FileInfo(publicPath).Length is < 1 or > 4096) throw new IOException("Bounded independent public recipient required.");
                    handoff = JsonSerializer.Deserialize<PublicSigningRecipientIdentity>(await File.ReadAllTextAsync(publicPath)) ?? throw new IOException();
                    using var validated = handoff.OpenWrappingKey();
                }
                var vm = services.GetRequiredService<HouseholdViewModel>(); active = vm;
                vm.ReviewActionFailure = (kind, status, result) => File.WriteAllText(Path.Combine(folder, "household-recovery-action-failure.json"),
                    JsonSerializer.Serialize(new { Kind = kind, HttpStatus = status, HResult = result, Process = Environment.ProcessId }));
                var page = new HouseholdPage(vm); activePage = page;
                activeLocalization = services.GetRequiredService<ILocalizationService>(); activeThemes = services.GetRequiredService<ThemeService>();
                app.ShowHouseholdReview(page);
                for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
                if (!vm.IsReady && !vm.IsBusy && vm.Error is null) await vm.ActivateAsync();
                if (!vm.IsReady || vm.Error is not null) throw new IOException("Original recovery page did not publish.");
                vm.Address = "https://localhost:14645/"; vm.Fingerprint = pin; vm.Name = "Fictitious Android recovery";
                if (stage == "device")
                {
                    var accessPath = Path.Combine(folder, "main-owner-device-access.json");
                    if (new FileInfo(accessPath).Length is < 1 or > 8192) throw new IOException("Bounded original public device authorization required.");
                    vm.InvitationText = await File.ReadAllTextAsync(accessPath);
                    var package = JsonSerializer.Deserialize(vm.InvitationText, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage) ?? throw new IOException();
                    const string comparedRoot = "301c6684c86c9688381e61414780a85ba74edebc4a657c9e5a2452c1bb19438f";
                    if (new PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey).Fingerprint != comparedRoot) throw new IOException("Actual transferred root differs from independently verified checkpoint.");
                    vm.ConfirmedRoot = comparedRoot; vm.Email = "household-main-lifecycle@example.invalid"; vm.Password = "Fictitious-Household-Hosting9!";
                }
                else await vm.DiscoverCommand.ExecuteAsync(null);
                ApplyInputMode("valid");
                var revision = 0;
                async Task ReportAsync()
                {
                    var hash = await DebugSnapshots.HouseholdFinancialFingerprintAsync(services);
                    using var observation = new HouseholdServiceTransport(new("https://localhost:14645/", pin), services.GetRequiredService<IPersonalSyncTokenStore>());
                    var state = await services.GetRequiredService<HouseholdJournal>().CaptureAsync(observation.Peer, stage is "device" or "device-cold" ? "df8f98b5-121e-412a-b996-b69b19808e35" : "448dfe96-c60c-419d-ae38-f33d9ba847ef");
                    var packet = JsonSerializer.Serialize(new { Stage = stage, ActualMainPage = true, Process = Environment.ProcessId, Profile = profiles.Current.Id,
                        DisplayMode = activeDisplayMode, Flow = activePage?.FlowDirection.ToString(),
                        vm.IsReady, vm.IsBusy, HasError = vm.Error is not null, vm.IsOwner, vm.HasMembership, vm.CanCompleteOwner, vm.CanSynchronize,
                        vm.CanSetUp, vm.CanExportRecovery, vm.CanEnableRecovery, vm.CanPickRecovery, vm.CanImportRecovery, vm.CanRevokeRecovery, vm.IsOwnerRecoveryDraft, vm.RecoveryStatus,
                        vm.HasOwnershipTransfer, vm.CanAdvanceOwnership, vm.CanCancelOwnership, vm.CanFinishOwnership, vm.OwnershipStatus,
                        state?.SpaceId, state?.DeviceId, state?.DeviceFingerprint,
                        PendingDevices = vm.Pending.Select(item => new { item.Source.IdentityId, Fingerprint = new PublicSigningRecipientIdentity(item.Source.DeviceKeys.SigningKey, item.Source.DeviceKeys.WrappingKey).Fingerprint }),
                        ApprovedDevices = vm.ApprovedDevices.Select(item => new { item.IdentityId, item.Source.Id, item.CanTransfer,
                            Fingerprint = new PublicSigningRecipientIdentity(item.Source.Keys.SigningKey, item.Source.Keys.WrappingKey).Fingerprint }),
                        FinancialHash = hash, CompleteFinancialMatch = fixture.Before is not null && hash == fixture.Before,
                        OriginalLifecycleProfilePreserved = stage == "original" && hash == fixture.Original, PhoneOrReleaseAcceptance = false });
                    var report = Path.Combine(folder, "household-main-recovery-proof.json");
                    await File.WriteAllTextAsync(report + ".writing", packet); File.Move(report + ".writing", report, true);
                }
                vm.PropertyChanged += (_, change) =>
                {
                    if (stage == "transfer" && handoff is not null && change.PropertyName == nameof(vm.InvitationText) && vm.InvitationText.Length != 0)
                    {
                        // Copy only recipient-encrypted bytes across the two independent
                        // installations. Clear the private editor synchronously before any
                        // native tree/capture can persist its capability in review output.
                        var clear = Encoding.UTF8.GetBytes(vm.InvitationText);
                        try
                        {
                            var invitation = JsonSerializer.Deserialize(clear, HouseholdMetadataJsonContext.Default.HouseholdInvitationPackage) ?? throw new IOException();
                            using var key = handoff.OpenWrappingKey();
                            var envelope = Jose.JWT.EncodeBytes(clear, key, Jose.JweAlgorithm.RSA_OAEP_256, Jose.JweEncryption.A256GCM, settings: StandardJoseSuite.CreateSettings());
                            File.WriteAllText(Path.Combine(folder, "household-transfer-invitation.jwe"), envelope);
                            File.WriteAllText(Path.Combine(folder, "household-transfer-public-root.json"), JsonSerializer.Serialize(new
                            { RootFingerprint = new PublicSigningRecipientIdentity(invitation.OwnerKeys.SigningKey, invitation.OwnerKeys.WrappingKey).Fingerprint, RecipientFingerprint = handoff.Fingerprint }));
                        }
                        finally { CryptographicOperations.ZeroMemory(clear); vm.InvitationText = ""; }
                    }
                    var current = ++revision;
                    app.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), async () =>
                    {
                        if (current != revision || vm.IsBusy) return;
                        try { await ReportAsync(); }
                        catch (Exception error) when (error is not OutOfMemoryException)
                        { await File.WriteAllTextAsync(Path.Combine(folder, "household-main-recovery-error.json"), JsonSerializer.Serialize(new { Phase = "observation", Kind = error.GetType().Name, Process = Environment.ProcessId })); }
                    });
                };
                await ReportAsync();
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            { await File.WriteAllTextAsync(Path.Combine(folder, "household-main-recovery-error.json"), JsonSerializer.Serialize(new { Phase = phase, Kind = error.GetType().Name, Process = Environment.ProcessId })); }
        });
        return true;
    }

    /// <summary>Only public fictitious identifiers and exact financial hashes survive observation/process changes.</summary>
    private sealed record RecoveryFixture(string Original, string? Owner, string? Recovered, string? Before,
        Guid Account, Guid Entry, Guid First, Guid Second, Guid Cost, string? PendingName, string? Device = null);
}
#endif
