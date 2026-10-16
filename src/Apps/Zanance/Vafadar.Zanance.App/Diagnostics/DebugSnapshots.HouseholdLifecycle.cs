#if DEBUG && WINDOWS
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only new actual lifecycle/audit status controls on the original independent owner installation; no destructive confirmation or fabricated service authority.</summary>
    private static async Task ReviewHouseholdLifecycleStatusAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdsourceflow") throw new IOException("Original independent installation required.");
        var fixture = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-network", "d294-app-source"));
        var done = Path.Combine(fixture, "original-app-lifecycle-status.complete");
        if (File.Exists(done) || !File.Exists(Path.Combine(fixture, "original-app-ownership-status.complete"))) throw new IOException("Preserve prior completed ownership layouts.");
        var before = await HouseholdFinancialFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>(); var themes = services.GetRequiredService<ThemeService>();
        var observations = new List<object>(); var progress = Path.Combine(folder, "lifecycle-layout-progress.json");
        var cases = File.Exists(progress) ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(progress)) ?? throw new IOException() : [];
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
            await File.WriteAllTextAsync(Path.Combine(folder, "page-publication.json"), JsonSerializer.Serialize(new
            { vm.Available, vm.IsReady, vm.IsBusy, HasError = vm.Error is not null, Attached = ReferenceEquals(page.Window, app.Windows[0]),
                CorrectBinding = ReferenceEquals(page.BindingContext, vm), HasNativeHandler = page.Handler is not null }));
            if (!vm.IsReady && !vm.IsBusy && vm.Error is null && vm.Available)
            {
                // A hidden review root may not dispatch Page.OnAppearing. Use the real
                // visible-lifetime method, not fabricated ready state or a bypassed command.
                await vm.ActivateAsync();
            }
            if (!vm.IsReady || vm.Error is not null) throw new IOException("Original lifecycle page did not publish actual current state.");
            var scroll = body.Children.OfType<ScrollView>().Single();
            async Task InvokeAsync(IAsyncRelayCommand command)
            {
                var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
                await ScrollToViewIfNeededAsync(scroll, button);
                // Native binding enablement can publish one dispatcher turn after the
                // preceding awaited action finishes. Do not invoke a disabled peer or
                // repeat a financial/service action to compensate for layout timing.
                for (var wait = 0; wait < 40 && button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: false }; wait++) await Task.Delay(50);
                await File.WriteAllTextAsync(Path.Combine(folder, "native-action-stage.txt"), button.Text + "\n" + button.IsEnabled + "\n" + vm.IsReady + "\n" + vm.IsBusy);
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true }) throw new IOException("Original lifecycle native action remains disabled.");
                InvokeChoiceReview(button, observations);
                if (command.ExecutionTask is { } running) await running;
                if (vm.Error is not null) throw new IOException("Original lifecycle status native action failed.");
            }
            await InvokeAsync(vm.DiscoverCommand); await InvokeAsync(vm.ReadLifecycleCommand); await InvokeAsync(vm.ReadAuditCommand);
            if (!vm.IsOwner || vm.HasRemoteDeletion || !vm.CanDeleteRemote || vm.LifecycleText != translator["HouseholdLifecycle_Active"])
                throw new IOException("Status reads must not fabricate deletion or alter the original owner.");
            foreach (var key in new[] { "HouseholdLifecycle_Hint", "HouseholdLifecycle_Active", "HouseholdLifecycle_AuditHint" })
            {
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == translator[key]);
                await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-{key}.png"));
            }
            vm.Deactivate(); cases.Add(new(language, theme.ToString(), width, true)); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(cases));
        }
        if (before != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Lifecycle status review changed original financial columns.");
        await File.WriteAllTextAsync(Path.Combine(folder, "lifecycle-layout-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases.Count, NativeObservations = observations.Count, ActualNativeLifecycleAndAudit = true, FinancialColumnsUnchanged = true,
            DestructiveConfirmationAccepted = false, OtherLifecycleStatesAccepted = false, AndroidAcceptance = false }));
        await File.WriteAllTextAsync(done, "Original household device management verified v1");
    }
}
#endif
