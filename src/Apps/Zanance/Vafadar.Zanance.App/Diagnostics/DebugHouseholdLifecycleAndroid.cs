#if DEBUG && ANDROID
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original main household page on one explicit fictitious Android installation; all consequential actions remain native user buttons.</summary>
internal static class DebugHouseholdLifecycleAndroid
{
    /// <summary>Only the independent opted-in package can publish this original page and public qualification observations.</summary>
    internal static bool StartIfRequested(App app, IServiceProvider services)
    {
        const string package = "pro.vafadar.zanance.householdlifecycle";
        if (AppInfo.Current.PackageName != package || Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_ANDROID_REVIEW") != "1") return false;
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var folder = FileSystem.AppDataDirectory; var report = Path.Combine(folder, "household-main-android-proof.json");
            try
            {
                await app.WaitForStartupAsync();
                if (!folder.Contains(package, StringComparison.Ordinal)) throw new IOException("Independent Android storage required.");
                var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
                var intent = Platform.CurrentActivity?.Intent;
                var language = intent?.GetStringExtra("review_language") ?? "en"; var theme = intent?.GetStringExtra("review_theme") ?? "light";
                if (language is not ("en" or "fa" or "de") || theme is not ("light" or "dark")) throw new IOException("Explicit application-only display choices required.");
                localization.SetLanguage(localization.SupportedLanguages.Single(item => item.CultureName == language));
                services.GetRequiredService<ThemeService>().Set(theme == "dark" ? ThemeChoice.Dark : ThemeChoice.Light);
                using var trust = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(folder, "household-public-root.crt"));
                string pin;
                using (var tcp = new TcpClient())
                {
                    await tcp.ConnectAsync("localhost", 14645); using var tls = new SslStream(tcp.GetStream());
                    await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "localhost", CertificateChainPolicy = new X509ChainPolicy
                        { TrustMode = X509ChainTrustMode.CustomRootTrust, CustomTrustStore = { trust }, RevocationMode = X509RevocationMode.NoCheck } });
                    pin = Convert.ToHexStringLower(SHA256.HashData(tls.RemoteCertificate!.GetRawCertData()));
                }
                var store = services.GetRequiredService<ZananceStore>(); var journal = services.GetRequiredService<HouseholdJournal>();
                var environment = services.GetRequiredService<IPersonalSyncEnvironment>(); HouseholdJournalState? own = null;
                var identityFile = new FileInfo(Path.Combine(folder, "household-public-identity.json"));
                if (identityFile.Length is < 1 or > 4096) throw new IOException("Bounded public fictional identity required.");
                using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(identityFile.FullName));
                if (expected.RootElement.GetProperty("Stage").GetString() != "android-identity" || !expected.RootElement.GetProperty("Passed").GetBoolean()) throw new IOException("Explicit fictional enrollment required.");
                var expectedIdentity = expected.RootElement.GetProperty("IdentityId").GetString();
                if (string.IsNullOrEmpty(expectedIdentity) || expectedIdentity.Length > 450) throw new IOException("Public fictional identity required.");
                var baselinePath = Path.Combine(folder, "household-financial-baseline.json");
                AndroidLifecycleSeed? seed = File.Exists(baselinePath) ? JsonSerializer.Deserialize<AndroidLifecycleSeed>(await File.ReadAllTextAsync(baselinePath)) ?? throw new IOException() : null;
                async Task<string> LedgerHashAsync() => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
                    { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() })));
                var saved = environment.ReadConnection();
                var authenticated = false;
                var allowInitialMissingSession = seed is null && (await store.GetAccountsAsync()).Count == 0;
                if (saved.Address.Length != 0)
                {
                    using var channel = new HouseholdServiceTransport(new(saved.Address, saved.Fingerprint), services.GetRequiredService<IPersonalSyncTokenStore>());
                    try
                    {
                        var identity = await channel.IdentityAsync();
                        if (identity.Id != expectedIdentity) throw new IOException("Only this fictional Android identity is allowed.");
                        own = await journal.CaptureAsync(channel.Peer, identity.Id); authenticated = true;
                    }
                    catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.Unauthorized && allowInitialMissingSession)
                    {
                        // A remembered/default peer is not an authenticated identity. A
                        // fresh empty installation must still show the real sign-in form.
                    }
                    var access = own is null ? null : await journal.ReadAccessAsync(own.Authority);
                    if (own is { MembershipVersion: > 0 } && access is { Role: HouseholdRole.Owner, CanWrite: true } && seed?.Before is null)
                    {
                        // Seed only after real native owner publication, through the real
                        // guarded financial writer. Retain identifiers before any effect.
                        seed ??= new(Guid.NewGuid(), Guid.NewGuid(), null);
                        await File.WriteAllTextAsync(baselinePath, JsonSerializer.Serialize(seed));
                        if (!(await store.GetAccountsAsync()).Any(item => item.Id == seed.Account)) await store.SaveAccountAsync(new Account(seed.Account)
                            { Name = "Fictitious Android retained money", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = new(2026, 10, 16) });
                        if (!(await store.GetEntriesAsync()).Any(item => item.Id == seed.Entry) && !(await store.SaveEntryAsync(new LedgerEntry(seed.Entry)
                            { AccountId = seed.Account, Kind = EntryKind.Expense, Amount = 450, Date = new(2026, 10, 16), Title = "Fictitious Android lifecycle expense" })).Succeeded) throw new IOException("Original financial writer refused.");
                        seed = seed with { Before = await LedgerHashAsync() }; await File.WriteAllTextAsync(baselinePath, JsonSerializer.Serialize(seed));
                    }
                }
                var vm = services.GetRequiredService<HouseholdViewModel>(); var page = new HouseholdPage(vm)
                    { FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                vm.ReviewActionFailure = (kind, status, result) => File.WriteAllText(Path.Combine(folder, "household-action-failure.json"),
                    JsonSerializer.Serialize(new { Kind = kind, HttpStatus = status, HResult = result, Process = Environment.ProcessId }));
                app.ShowHouseholdReview(page);
                for (var wait = 0; wait < 160 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(50);
                if (!vm.IsReady && !vm.IsBusy && vm.Error is null) await vm.ActivateAsync();
                if (!vm.IsReady || vm.Error is not null) throw new IOException("Original main Android page did not publish.");
                vm.Address = "https://localhost:14645/"; vm.Fingerprint = pin; vm.Name = "Fictitious Android lifecycle";
                // This known credential belongs only to the independently confirmed example.invalid
                // fixture. No owner credential is read, typed, placed in arguments or logged.
                vm.Email = "household-main-android@example.invalid"; vm.Password = "Fictitious-Household-Hosting9!";
                if (authenticated) await vm.DiscoverCommand.ExecuteAsync(null);
                var revision = 0;
                async Task ReportAsync()
                {
                    var hash = await LedgerHashAsync(); var deletion = own is null ? null : await journal.ReadDeletionAsync(own.Authority);
                    if (seed?.Before is { } before && before != hash) throw new IOException("Original local financial rows changed.");
                    var packet = JsonSerializer.Serialize(new { ActualMainPage = true, Language = language, Theme = theme, Process = Environment.ProcessId,
                        vm.IsReady, vm.IsBusy, HasError = vm.Error is not null, vm.StatusText, vm.IsOwner, vm.HasMembership, vm.CanCompleteOwner, vm.CanSynchronize,
                        vm.CanDeleteRemote, vm.HasRemoteDeletion, vm.LifecycleText, vm.RemoteDeletionText, LocalMoneyPreserved = seed?.Before is not null && hash == seed.Before,
                        Receipt = deletion?.Receipt, EntireFinancialGraphAcceptance = false, PhoneOrReleaseAcceptance = false });
                    await File.WriteAllTextAsync(report + ".writing", packet); File.Move(report + ".writing", report, true);
                }
                vm.PropertyChanged += (_, _) =>
                {
                    var current = ++revision;
                    app.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), async () =>
                    {
                        if (current != revision || vm.IsBusy) return;
                        try { await ReportAsync(); }
                        catch (Exception error) when (error is not OutOfMemoryException)
                        { await File.WriteAllTextAsync(Path.Combine(folder, "household-main-android-error.txt"), error.GetType().Name); }
                    });
                };
                await ReportAsync();
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            { await File.WriteAllTextAsync(Path.Combine(folder, "household-main-android-error.txt"), error.GetType().Name); }
        });
        return true;
    }

    /// <summary>Public fictional financial identifiers and baseline hash, never device keys or sessions.</summary>
    private sealed record AndroidLifecycleSeed(Guid Account, Guid Entry, string? Before);
}
#endif
