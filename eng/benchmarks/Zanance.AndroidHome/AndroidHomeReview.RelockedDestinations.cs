using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Uses original native lock covers with an explicit authentication-response port, never device credentials.</summary>
    private static async Task<object> ReviewRelockedDestinationsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        if (Directory.Exists(root) || RedirectedAncestors(root).Length != 0)
            throw new ReviewCheckException("Fresh independent access source required; preserve accepted native actions.");
        Directory.CreateDirectory(root);
        var original = await ImportFinancialNativeReview.StoredAsync(services, ct);
        await using var fixture = new ServiceCollection().AddZananceData(Path.Combine(root, "access.db")).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>();
        await store.UpdateSettingsAsync(settings => settings.AppLockEnabled = true, ct);
        var source = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var translator = services.GetRequiredService<Translator>();
        var time = services.GetRequiredService<TimeProvider>();
        var results = new List<object>();
        async Task Ready(Func<bool> predicate)
        {
            for (var attempt = 0; attempt < 200; attempt++)
            {
                if (predicate()) return;
                await Task.Delay(50, ct);
            }
            throw new ReviewCheckException("Original lock-cover state did not become ready; retain source and native actions.");
        }
        LockPage CurrentCover() => app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() as LockPage
            ?? throw new ReviewCheckException("Original native cover is missing.");
        async Task ClickUnlockAsync(LockPage page)
        {
            var button = ((IVisualTreeElement)page).GetVisualTreeDescendants().OfType<Button>()
                .Single(value => value.Text == translator["Lock_Unlock"]);
            await Ready(() => button.Handler?.PlatformView is Android.Widget.Button native
                && native.IsShown && native.HasWindowFocus && native.Enabled && native.Width > 0 && native.Height > 0);
            if (CurrentCover() != page || button.Handler?.PlatformView is not Android.Widget.Button target
                || target.Context?.PackageName != Package || !target.PerformClick())
                throw new ReviewCheckException("Original owned native unlock button refused its action.");
            await Task.Yield();
        }
        foreach (var kind in new[] { "recovered-cover", "awaited-navigation-order" })
        {
            var authentication = new AccessResponsePort();
            var access = new AppLockService(authentication, store, translator, time,
                new PinLock(new AccessNoPin(), time), new MauiAppLockHost(translator));
            await access.StartAsync();
            var firstCover = CurrentCover();
            await Ready(() => authentication.Calls > 0);
            var order = new List<int>();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await access.RunWhenUnlockedAsync(async () =>
            {
                order.Add(1);
                if (kind == "recovered-cover")
                {
                    await access.SleepAsync();
                    await access.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
                }
                else { entered.SetResult(); await release.Task; }
            });
            await access.RunWhenUnlockedAsync(() => { order.Add(2); return Task.CompletedTask; });
            authentication.Outcome = AuthenticationOutcome.Success;
            await ClickUnlockAsync(firstCover);
            if (kind == "recovered-cover")
            {
                await Ready(() => access.IsLocked && order.Count >= 1
                    && app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() is LockPage renewed && renewed != firstCover);
                if (!order.SequenceEqual([1])) throw new ReviewCheckException("Pending destination ran beneath the renewed native cover.");
                await File.WriteAllTextAsync(Path.Combine(root, kind + "-covered.json"), JsonSerializer.Serialize(new
                { Order = order.ToArray(), access.IsLocked, OriginalNativeCover = true, AuthenticationResponsePort = true }), ct);
                await ClickUnlockAsync(CurrentCover());
            }
            else
            {
                try
                {
                    await entered.Task.WaitAsync(ct);
                    await access.RunWhenUnlockedAsync(() => { order.Add(3); return Task.CompletedTask; });
                    if (!order.SequenceEqual([1])) throw new ReviewCheckException("New input overtook the original pending native destination.");
                }
                finally { release.TrySetResult(); }
            }
            await Ready(() => !access.IsLocked && order.Count == 3);
            if (!order.SequenceEqual([1, 2, 3]) || app.Windows[0].Page!.Navigation.ModalStack.OfType<LockPage>().Any())
                throw new ReviewCheckException("Original native covers or destination order were not restored.");
            if (source != await ImportFinancialNativeReview.StoredAsync(fixture, ct)
                || original != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new ReviewCheckException("Access review changed the original complete fictitious source/base.");
            var result = new { Kind = kind, Order = order.ToArray(), NativeUnlocks = kind == "recovered-cover" ? 2 : 1,
                OriginalCoversRemoved = true, SourceEqual = true, BaseEqual = true, FinancialWrites = 0,
                AuthenticationResponsePort = true, RealDeviceAuthentication = false, PinWrites = 0 };
            await File.WriteAllTextAsync(Path.Combine(root, kind + "-accepted.json"), JsonSerializer.Serialize(result), ct);
            results.Add(result);
        }
        return new { Comparison = comparison, Cases = results, NativeUnlocks = 3, Completed = true,
            ActualNativeCovers = true, RealCredentialAcceptance = false, MainPackageTouched = false, Device = DeviceEvidence() };
    }

    /// <summary>Supplies declared fictional authentication responses without invoking a real owner credential.</summary>
    private sealed class AccessResponsePort : IDeviceAuthenticator
    {
        internal AuthenticationOutcome Outcome = AuthenticationOutcome.Failed;
        internal int Calls;
        /// <inheritdoc />
        public Task<bool> IsAvailableAsync() => Task.FromResult(true);
        /// <inheritdoc />
        public Task<AuthenticationOutcome> AuthenticateAsync(string reason) { Calls++; return Task.FromResult(Outcome); }
    }

    /// <summary>Never reads native SecureStorage and refuses every PIN write.</summary>
    private sealed class AccessNoPin : IPinStorage
    {
        /// <inheritdoc />
        public Task<PinState?> ReadAsync() => Task.FromResult<PinState?>(null);
        /// <inheritdoc />
        public Task WriteAsync(PinState? state) => throw new InvalidOperationException("Independent access review must not write PIN state.");
    }
}
