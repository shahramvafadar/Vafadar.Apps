#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Actual source selection and growing page controls on only the independently owned protected installation; no identity or member grant.</summary>
    private static async Task ReviewHouseholdSourceUiAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdreview") throw new IOException("Independent household review required.");
        var ownership = Path.Combine(FileSystem.AppDataDirectory, "fictitious-household-window.marker");
        if (!File.Exists(ownership) || await File.ReadAllTextAsync(ownership) != "Fictitious original household cost window v1")
            throw new IOException("Only the retained fictional household installation may be reviewed.");
        var proofPath = Path.Combine(folder, "household-source-ui-proof.json");
        if (File.Exists(proofPath)) throw new IOException("The completed source page review cannot be repeated.");
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = services.GetRequiredService<ZananceStore>();
        var fixturePath = Path.Combine(folder, "household-source-ui-fixture.json");
        SourceUiFixture fixture;
        if (!File.Exists(fixturePath))
        {
            fixture = new(await ProtectedRowsFingerprintAsync(services), [Guid.NewGuid(), Guid.NewGuid()]);
            await File.WriteAllTextAsync(fixturePath, JsonSerializer.Serialize(fixture));
        }
        else fixture = JsonSerializer.Deserialize<SourceUiFixture>(await File.ReadAllTextAsync(fixturePath)) ?? throw new IOException("Invalid original source review.");
        for (var index = 0; index < fixture.Accounts.Length; index++)
        {
            if ((await store.GetAccountsAsync()).Any(account => account.Id == fixture.Accounts[index])) continue;
            await store.SaveAccountAsync(new Account(fixture.Accounts[index]) { Name = "Fictitious explicitly selected household source account with a complete long name " + index,
                CurrencyCode = index == 0 ? "EUR" : "KWD", OpeningDate = new(2026, 10, 16), OpeningBalance = 12345 + index });
        }
        var before = await ProtectedRowsFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var progressPath = Path.Combine(folder, "household-source-ui-progress.json");
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
            var body = (Grid)page.Content!; PageHeader.Attach(page, localization.IsRightToLeft); NavigationPage.SetHasNavigationBar(page, false);
            app.ShowHouseholdReview(page);
            for (var wait = 0; wait < 100 && (!vm.IsReady || vm.IsBusy) && vm.Error is null; wait++) await Task.Delay(100);
            if (!vm.IsReady || vm.Error is not null || !vm.CanChooseSource || vm.IsOwner || vm.CanAdminister) throw new IOException("Original source page did not publish without authority.");
            var scroll = body.Children.OfType<ScrollView>().Single();
            async Task InvokeAsync(System.Windows.Input.ICommand command)
            {
                var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
                await ScrollToViewIfNeededAsync(scroll, button); InvokeChoiceReview(button, observations);
                for (var wait = 0; wait < 100 && vm.IsBusy; wait++) await Task.Delay(50);
                if (vm.Error is not null) throw new IOException("Original native source action failed.");
            }
            await InvokeAsync(vm.LoadSourceAccountsCommand);
            if (vm.SourceAccounts.Count != 2 || vm.SourceAccounts.Any(item => item.Selected) || vm.SourceHistory || vm.SourceAttachments)
                throw new IOException("Source data was selected without explicit consent.");
            foreach (var choice in vm.SourceAccounts)
            {
                var check = VisualDescendants(page).OfType<CheckBox>().Single(item => ReferenceEquals(item.BindingContext, choice));
                await ScrollToViewIfNeededAsync(scroll, check);
                if (check.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.CheckBox native) throw new IOException("Original native source checkbox is missing.");
                var peer = new Microsoft.UI.Xaml.Automation.Peers.CheckBoxAutomationPeer(native);
                if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle) is not Microsoft.UI.Xaml.Automation.Provider.IToggleProvider toggle)
                    throw new IOException("The actual source checkbox has no native Toggle pattern.");
                toggle.Toggle(); await Task.Delay(50); if (!choice.Selected) throw new IOException("Native source choice did not reach the original draft.");
            }
            await InvokeAsync(vm.ReviewSourceCommand);
            if (!vm.HasSourceReview || vm.HasSourceHandoff || vm.CanCompleteOwner) throw new IOException("Review created unintended setup or publication authority.");
            // Command completion publishes text before the next native layout pass. Observe its
            // actual growing surface after layout/scroll, rather than measuring a zero-sized old frame.
            await Task.Delay(200);
            var summary = VisualDescendants(page).OfType<Label>().Single(label => label.Text == vm.SourceSummary);
            await ScrollToViewIfNeededAsync(scroll, summary);
            if (summary.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock nativeSummary)
                await File.WriteAllTextAsync(Path.Combine(folder, $"{language}-{theme}-{width}-summary-geometry.json"), JsonSerializer.Serialize(new
                { NativeWidth = nativeSummary.ActualWidth, NativeHeight = nativeSummary.ActualHeight, nativeSummary.IsTextTrimmed, summary.Width, summary.Height }));
            foreach (var label in VisualDescendants(page).OfType<Label>().Where(label => Visible(label) && label.FontAutoScalingEnabled && !string.IsNullOrEmpty(label.Text)))
                CheckCaption(label, observations);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-source-review.png"));
            vm.Deactivate(); cases.Add(new(language, theme.ToString(), width, true));
            await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(cases));
        }
        if (before != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Original page review changed stored data.");
        await using (var db = await factory.CreateDbContextAsync())
        {
            var owned = await db.Accounts.Where(account => fixture.Accounts.Contains(account.Id)).ToArrayAsync();
            if (owned.Length != 2 || owned.Any(account => !account.Name.StartsWith("Fictitious explicitly selected household source account", StringComparison.Ordinal)))
                throw new IOException("Original source fixture ownership changed.");
            db.Accounts.RemoveRange(owned); await db.SaveChangesAsync();
        }
        if (fixture.Before != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Original source fixture data was not restored exactly.");
        await File.WriteAllTextAsync(proofPath, JsonSerializer.Serialize(new { Cases = cases, NativeObservations = observations.Count,
            RealCheckboxToggleAndReviewInvoke = true, ActualProtectedSource = true, NoIdentityOrWriteGrantManufactured = true,
            FinancialRowsUnchangedAndFixtureRestored = true, ProfileHandoffAndMobileAcceptance = false }));
    }

    /// <summary>Only exact independently owned source rows and their previous full financial fingerprint.</summary>
    private sealed record SourceUiFixture(string Before, Guid[] Accounts);
}
#endif
