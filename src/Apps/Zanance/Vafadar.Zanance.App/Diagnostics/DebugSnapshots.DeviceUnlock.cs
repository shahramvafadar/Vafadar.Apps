#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Native-effect response port with no device credential, authentication dialog or system-setting effect.</summary>
    private sealed class ReviewDeviceAuthentication : IDeviceAuthenticator
    {
        internal AuthenticationOutcome Outcome = AuthenticationOutcome.NotAvailable;
        internal bool Fail;
        internal int Calls;
        /// <inheritdoc/>
        public Task<bool> IsAvailableAsync() => Task.FromResult(Outcome != AuthenticationOutcome.NotAvailable);
        /// <inheritdoc/>
        public Task<AuthenticationOutcome> AuthenticateAsync(string reason)
        {
            Calls++;
            return Fail ? Task.FromException<AuthenticationOutcome>(new IOException("Fictitious authentication failure.")) : Task.FromResult(Outcome);
        }
    }

    /// <summary>Explicit empty verifier port that refuses any write; the native review never touches SecureStorage.</summary>
    private sealed class ReviewNoPin : IPinStorage
    {
        /// <inheritdoc/>
        public Task<PinState?> ReadAsync() => Task.FromResult<PinState?>(null);
        /// <inheritdoc/>
        public Task WriteAsync(PinState? state) => throw new InvalidOperationException("Device-only review must not write a PIN.");
    }

    /// <summary>Invokes actual LockPage controls for refused, unavailable, throwing and successful authentication responses.</summary>
    private static async Task ReviewDeviceUnlockPolicyAsync(App app, IServiceProvider services, string folder, bool feedbackOnly = false)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Access review requires its independent native identity.");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (!File.Exists(Path.Combine(Path.GetDirectoryName(location.Path)!, "protected-review-fixture.json")))
            throw new IOException("Independent access review marker is absent.");
        var before = await ProtectedRowsFingerprintAsync(services);
        await using var fixture = new ServiceCollection().AddZananceData(Path.Combine(folder, "access-fixture.db")).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>();
        var settings = await store.GetSettingsAsync(); settings.AppLockEnabled = true; await store.SaveSettingsAsync(settings);
        var localization = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<Presentation.ThemeService>();
        var translator = services.GetRequiredService<Translator>();
        var time = services.GetRequiredService<TimeProvider>();
        var observations = new List<object>(); var cases = 0;
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, localization, language);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var authentication = new ReviewDeviceAuthentication(); var opened = 0;
            var access = new AppLockService(authentication, store, translator, time,
                new PinLock(new ReviewNoPin(), time), new MauiAppLockHost(translator));
            await access.StartAsync(); await Task.Delay(150);
            await access.RunWhenUnlockedAsync(() => { opened++; return Task.CompletedTask; });
            var page = app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() as LockPage
                ?? throw new InvalidOperationException("Original native lock cover was not shown.");
            var button = VisualDescendants(page).OfType<Button>().Single(value => value.Text == translator["Lock_Unlock"]);
            var prefix = $"{language}-{theme.ToString().ToLowerInvariant()}-{width}";
            foreach (var result in feedbackOnly ? Array.Empty<AuthenticationOutcome>() : new[] { AuthenticationOutcome.NotAvailable, AuthenticationOutcome.Failed, (AuthenticationOutcome)99 })
            {
                authentication.Outcome = result;
                var calls = authentication.Calls; InvokeChoiceReview(button, observations);
                for (var attempt = 0; attempt < 30 && authentication.Calls == calls; attempt++) await Task.Delay(10);
                await Task.Delay(50);
                if (!access.IsLocked || opened != 0 || app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() != page)
                    throw new InvalidOperationException("Non-successful native authentication removed the original cover.");
            }
            if (!feedbackOnly) { authentication.Fail = true; InvokeChoiceReview(button, observations); await Task.Delay(100); }
            if (!access.IsLocked || opened != 0 || app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() != page)
                throw new InvalidOperationException("Native authentication exception released protected work.");
            authentication.Fail = false; authentication.Outcome = AuthenticationOutcome.NotAvailable;
            InvokeChoiceReview(button, observations); await Task.Delay(100);
            foreach (var caption in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(caption, observations);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, prefix + "-device-unavailable.png"));
            authentication.Outcome = AuthenticationOutcome.Success; InvokeChoiceReview(button, observations);
            for (var attempt = 0; attempt < 30 && access.IsLocked; attempt++) await Task.Delay(10);
            if (access.IsLocked || opened != 1 || app.Windows[0].Page!.Navigation.ModalStack.Contains(page))
                throw new InvalidOperationException("Successful authentication did not complete its exact original cover once.");
            if (await ProtectedRowsFingerprintAsync(services) != before)
                throw new IOException("Native lock review changed original financial data.");
            cases++;
        }
        await File.WriteAllTextAsync(Path.Combine(folder, feedbackOnly ? "device-unlock-feedback-proof.json" : "device-unlock-native-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases, ActualNativeUnlockInvoked = true, UnavailableFailedUnknownAndExceptionStayCovered = !feedbackOnly,
            FeedbackOnly = feedbackOnly,
            SuccessOpensQueuedWorkExactlyOnce = true, NativeCredentialOrPinWrites = false, CompleteFinancialRowsUnchanged = true,
            FinancialFingerprint = before, NativeObservations = observations.Count }));
    }
}
#endif
