#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Original native cost read/editor geometry over an independently protected installation; no fake member grant or financial Save.</summary>
    private static async Task ReviewHouseholdUiAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdreview") throw new IOException("Independent household review required.");
        var root = FileSystem.AppDataDirectory; var marker = Path.Combine(root, "fictitious-household-window.marker");
        const string ownership = "Fictitious original household cost window v1";
        var continuation = File.Exists(marker);
        if (continuation && (Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_UI_CONTINUE") != "1" || await File.ReadAllTextAsync(marker) != ownership ||
            File.Exists(Path.Combine(folder, "household-ui-proof.json")))) throw new IOException("Only an explicit incomplete original household review can continue.");
        if (!continuation) await File.WriteAllTextAsync(marker, ownership);
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (!continuation)
        {
        await using (var db = await factory.CreateDbContextAsync())
        {
            if (await db.Accounts.AnyAsync() || await db.HouseholdCosts.AnyAsync() || await db.HouseholdContacts.AnyAsync())
                throw new IOException("Only an empty independently owned household installation can be seeded.");
            ProtectedSqliteAccess.Require((Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection());
            var first = new HouseholdContact { Name = "Fictitious long household cost participant" };
            var second = new HouseholdContact { Name = "Fictitious second household participant" };
            db.HouseholdContacts.AddRange(first, second);
            var date = new DateOnly(2026, 10, 16);
            var cost = new HouseholdCost { Title = "Fictitious complete shared household cost description for native review", Date = date,
                Amount = long.MaxValue, CurrencyCode = "KWD", Kind = HouseholdCostKind.Expense,
                PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = first.Id };
            var another = new HouseholdCost { Title = "Fictitious second actual cost", Date = date, Amount = 1001,
                CurrencyCode = "KWD", Kind = HouseholdCostKind.Expense, PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = second.Id };
            db.HouseholdCosts.AddRange(cost, another);
            db.HouseholdAllocations.AddRange(new() { CostId = cost.Id, ContactId = first.Id, Amount = cost.Amount },
                new() { CostId = another.Id, ContactId = second.Id, Amount = another.Amount });
            await db.SaveChangesAsync();
        }
        }
        var original = await ProtectedRowsFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var results = File.Exists(Path.Combine(folder, "household-ui-progress.json"))
            ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(Path.Combine(folder, "household-ui-progress.json"))) ?? throw new IOException("Invalid original review progress.")
            : [];
        var observations = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            if (results.Any(row => row.Language == language && row.Theme == theme.ToString() && row.Width == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, localization, language);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var vm = services.GetRequiredService<HouseholdCostsViewModel>();
            if (vm.IsReady || vm.CanWrite) throw new IOException("Native cost construction must remain covered.");
            var page = new HouseholdCostsPage(vm) { FlowDirection = localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            var body = (Grid)page.Content!;
            // Use the same growing child header as the production Shell, rather than NavigationPage's fixed native title bar.
            PageHeader.Attach(page, localization.IsRightToLeft); NavigationPage.SetHasNavigationBar(page, false);
            app.ShowHouseholdReview(page);
            for (var wait = 0; wait < 100 && !vm.IsReady && vm.Error is null; wait++) await Task.Delay(100);
            if (!vm.IsReady || vm.Error is not null || vm.CanWrite || vm.Costs.Count != 2)
                throw new IOException("The original detached household history did not publish read-only.");
            await Task.Delay(200);
            var scroll = body.Children.OfType<ScrollView>().Single();
            var item = vm.Costs.First();
            var surface = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, vm.EditCostCommand) && ReferenceEquals(action.CommandParameter, item));
            await ScrollToViewIfNeededAsync(scroll, surface);
            InvokeChoiceReview(VisualDescendants(surface).OfType<Button>().Single(), observations);
            for (var wait = 0; wait < 100 && (!vm.EditingCost || vm.IsBusy); wait++) await Task.Delay(50);
            if (!vm.EditingCost || vm.Shares.Count != 2 || vm.CanWrite) throw new IOException("Original native cost review action or role boundary failed.");
            var editor = VisualDescendants(page).OfType<Border>().Single(border => border.IsVisible && border.Content is VerticalStackLayout stack &&
                stack.Children.OfType<Label>().Any(label => label.Text == services.GetRequiredService<Translator>()["HouseholdCost_Editor"]));
            await Task.Delay(200); await ScrollToViewIfNeededAsync(scroll, editor);
            var heading = ((VerticalStackLayout)editor.Content!).Children.OfType<Label>().First();
            if (heading.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeHeading ||
                scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer nativeViewport)
                throw new IOException("Actual editor heading/viewport is unavailable.");
            var headingBounds = nativeHeading.TransformToVisual(nativeViewport).TransformBounds(new Windows.Foundation.Rect(0, 0, nativeHeading.ActualWidth, nativeHeading.ActualHeight));
            if (headingBounds.Top < -1 || headingBounds.Bottom > nativeViewport.ActualHeight + 1)
                throw new IOException("The captured original editor heading is outside the actual native viewport.");
            foreach (var label in VisualDescendants(page).OfType<Label>().Where(label => Visible(label) && label.FontAutoScalingEnabled && !string.IsNullOrEmpty(label.Text)))
                CheckCaption(label, observations);
            var save = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.SaveCommand));
            if (save.IsEnabled) throw new IOException("Detached financial history must never acquire write authority from a layout fixture.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-cost-editor.png"));
            await ScrollToEndIfNeededAsync(scroll);
            foreach (var label in VisualDescendants(page).OfType<Label>().Where(label => Visible(label) && label.FontAutoScalingEnabled && !string.IsNullOrEmpty(label.Text)))
                CheckCaption(label, observations);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-cost-shares.png"));
            var cancel = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.CancelEditCommand));
            InvokeChoiceReview(cancel, observations);
            if (vm.HasEditor || vm.Shares.Count != 0) throw new IOException("Original native cancellation did not clear the unsaved cost draft.");
            vm.Deactivate(); results.Add(new(language, theme.ToString(), width, true));
            await File.WriteAllTextAsync(Path.Combine(folder, "household-ui-progress.json"), JsonSerializer.Serialize(results));
        }
        if (original != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Native cost layout review changed stored financial state.");
        await File.WriteAllTextAsync(Path.Combine(folder, "household-ui-proof.json"), JsonSerializer.Serialize(new
        { Cases = results, NativeObservations = observations.Count, CompleteStoredRowsUnchanged = true, IndependentProtectedInstallation = true,
            ActualWriteAuthorityNotManufactured = true, WritableEditorCheckedSeparatelyOnNativeHousehold = true,
            ProductionGrowingHeaderAndNativeScrollPattern = true, EditorHeadingInsideActualViewport = true, AndroidOrPhysicalDeviceAcceptance = false }));
    }

    /// <summary>Retained completed native case identity; failed observation resumes never repeat successful cases.</summary>
    private sealed record HouseholdUiCase(string Language, string Theme, int Width, bool ReadOnly);
}
#endif
