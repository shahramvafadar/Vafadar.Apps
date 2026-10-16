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
    /// <summary>New original status controls on the retained real owner device; no fabricated transfer, role, recipient or signing authority.</summary>
    private static async Task ReviewHouseholdOwnershipStatusAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdsourceflow") throw new IOException("Independent original installation required.");
        var fixture = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-network", "d294-app-source"));
        var completed = Path.Combine(fixture, "original-app-ownership-status.complete");
        if (File.Exists(completed) || !File.Exists(Path.Combine(fixture, "original-app-device-access.complete"))) throw new IOException("Preserve original completed device acceptance; do not replay status layouts.");
        var original = await HouseholdFinancialFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var observations = new List<object>();
        var progressPath = Path.Combine(folder, "ownership-status-layout-progress.json");
        var cases = File.Exists(progressPath) ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException() : [];
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
            async Task InvokeAsync(IAsyncRelayCommand command)
            {
                var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
                await ScrollToViewIfNeededAsync(scroll, button); InvokeChoiceReview(button, observations);
                if (command.ExecutionTask is { } running) await running;
                if (vm.Error is not null) throw new IOException("Original ownership status native action failed.");
            }
            await InvokeAsync(vm.DiscoverCommand); await InvokeAsync(vm.ReadOwnershipCommand);
            if (!vm.IsOwner || !vm.CanAdminister || vm.HasOwnershipTransfer || vm.CanAdvanceOwnership || vm.CanCancelOwnership || vm.CanFinishOwnership ||
                vm.OwnershipStatus != translator["HouseholdOwnership_None"]) throw new IOException("A read must not infer an ownership transfer or approval.");
            foreach (var key in new[] { "HouseholdOwnership_Title", "HouseholdOwnership_Hint", "HouseholdOwnership_None" })
            {
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == translator[key]);
                await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-{key}.png"));
            }
            vm.Deactivate(); cases.Add(new(language, theme.ToString(), width, true)); await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(cases));
        }
        if (original != await HouseholdFinancialFingerprintAsync(services)) throw new IOException("Original financial columns changed during status inspection.");
        await File.WriteAllTextAsync(Path.Combine(folder, "ownership-status-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases.Count, NativeObservations = observations.Count, OriginalNativeDiscoverAndStatusRefresh = true, FinancialColumnsUnchanged = true,
            InventedParticipantAuthority = false, TransferPromptAndOtherStagesAccepted = false, AndroidAcceptance = false }));
        await File.WriteAllTextAsync(completed, "Original household device management verified v1");
    }
}
#endif
