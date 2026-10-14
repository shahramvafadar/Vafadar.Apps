#if DEBUG && ANDROID
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Checks the original Android lock page against actual unavailable device authentication on its own test identity.</summary>
internal static class DebugDeviceUnlockAndroid
{
    private static ServiceProvider? fixture;

    /// <summary>Retains the real native adapter; observation never changes or configures a device credential.</summary>
    private sealed class ObservedAuthentication(IDeviceAuthenticator original, Action observed) : IDeviceAuthenticator
    {
        internal int Calls;
        internal AuthenticationOutcome? Last;
        /// <inheritdoc/>
        public Task<bool> IsAvailableAsync() => original.IsAvailableAsync();
        /// <inheritdoc/>
        public async Task<AuthenticationOutcome> AuthenticateAsync(string reason)
        {
            Last = await original.AuthenticateAsync(reason); Calls++; observed(); return Last.Value;
        }
    }

    /// <summary>Refuses all verifier writes and never opens native SecureStorage.</summary>
    private sealed class NoPin : IPinStorage
    {
        /// <inheritdoc/>
        public Task<PinState?> ReadAsync() => Task.FromResult<PinState?>(null);
        /// <inheritdoc/>
        public Task WriteAsync(PinState? state) => throw new InvalidOperationException("Access review must not write a PIN.");
    }

    /// <summary>Shows the original cover only on an explicitly opted-in independent package.</summary>
    internal static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.accessacceptance"
            || Environment.GetEnvironmentVariable("VAFADAR_DEVICE_UNLOCK_REVIEW") != "1") return false;
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var report = Path.Combine(FileSystem.AppDataDirectory, "device-unlock-android-proof.json");
            try
            {
                await app.WaitForStartupAsync();
                var original = services.GetRequiredService<IDeviceAuthenticator>();
                if (await original.IsAvailableAsync()) throw new InvalidOperationException("Review requires an already unavailable device credential; no system setting will be changed.");
                var directory = Path.Combine(FileSystem.CacheDirectory, "device-access-review"); Directory.CreateDirectory(directory);
                fixture = new ServiceCollection().AddZananceData(Path.Combine(directory, "access-fixture.db")).BuildServiceProvider();
                fixture.MigrateLocalDatabase<ZananceDbContext>();
                var store = fixture.GetRequiredService<ZananceStore>();
                var settings = await store.GetSettingsAsync(); settings.AppLockEnabled = true; await store.SaveSettingsAsync(settings);
                AppLockService? access = null; ObservedAuthentication? authentication = null; var opened = 0;
                async Task ReportAsync()
                {
                    var packet = JsonSerializer.Serialize(new
                    {
                        ActualNativeAdapter = true, authentication!.Calls, Outcome = authentication.Last?.ToString(),
                        Covered = access!.IsLocked, OriginalModalPresent = app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() is LockPage,
                        QueuedDestinationsOpened = opened, DeviceOrPinSettingsChanged = false
                    });
                    await File.WriteAllTextAsync(report + ".writing", packet); File.Move(report + ".writing", report, true);
                }
                authentication = new(original, () => app.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), async () => await ReportAsync()));
                var time = services.GetRequiredService<TimeProvider>();
                access = new(authentication, store, services.GetRequiredService<Translator>(), time,
                    new PinLock(new NoPin(), time), new MauiAppLockHost(services.GetRequiredService<Translator>()));
                await access.StartAsync();
                await access.RunWhenUnlockedAsync(() => { opened++; return Task.CompletedTask; });
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { Passed = false, Error = error.GetType().Name }));
            }
        });
        return true;
    }
}
#endif
