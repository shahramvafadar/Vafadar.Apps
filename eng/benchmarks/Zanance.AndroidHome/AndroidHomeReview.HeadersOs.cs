using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies current headers without reseeding the original base or replaying financial actions.</summary>
    private static async Task<object> ReviewHeadersOsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var width = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetIntExtra("review_os_width", 0) ?? 0;
        var positionOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_header_native_position", false) == true;
        var resources = Android.App.Application.Context.Resources;
        if (AppInfo.Current.PackageName != Package || width is not (360 or 412)
            || Math.Abs((resources?.Configuration?.FontScale ?? 0) - 2f) > 0.01f
            || resources?.DisplayMetrics is not { Density: > 0 } metrics || Math.Abs(metrics.WidthPixels / metrics.Density - width) > 1)
            throw new ReviewCheckException("Actual independent OS200% header width required.");
        var source = Path.Combine(root, "original-secondary-fields");
        if (!new[] { "goal", "price", "override", "partial", "bill" }
            .All(key => File.Exists(Path.Combine(source, "secondary-fields-" + key + "-accepted.json"))))
            throw new ReviewCheckException("Original completed secondary source required for filled header geometry.");
        if (positionOnly && (from language in new[] { "en", "fa", "de" }
            from theme in new[] { "Light", "Dark" } from kind in new[] { "home", "plans", "filled-plans" }
            select Path.Combine(root, "headers-os-" + width + "-" + language + "-" + theme + "-" + kind + ".json")).Any(file => !File.Exists(file)))
            throw new ReviewCheckException("All original header scenes required before native-position-only continuation.");
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var store = services.GetRequiredService<ZananceStore>();
        if ((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0)
            throw new ReviewCheckException("Retained independent base account required; never seed for header geometry.");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, source, ct);
        var originalRows = await original.AllMoneyRowsAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage.CultureName;
        var observations = new List<object>();
        async Task Until(Func<bool> ready)
        {
            while (!ready()) await Task.Delay(50, ct);
        }
        async Task ChangeTheme(ThemeChoice theme)
        {
            if (themes.Choice == theme) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Invoke(Button button)
        {
            await Until(() => button.Handler?.PlatformView is Android.Widget.Button
                { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true, Enabled: true });
            var native = (Android.Widget.Button)button.Handler!.PlatformView!;
            using var visible = new Android.Graphics.Rect();
            if (native.Context?.PackageName != Package || !native.GetGlobalVisibleRect(visible)
                || visible.Width() != native.Width || visible.Height() != native.Height || !native.PerformClick())
                throw new ReviewCheckException("Original visible header or Cancel target required.");
            await Task.Yield();
        }
        async Task<object> Geometry(Page page, bool home)
        {
            await AwaitReimbursementFocusAsync(ct); await Task.Delay(250, ct);
            var grid = (Grid)((ContentPage)page).Content!;
            var header = page.FindByName<Grid>(home ? "HomeHeader" : "PlansHeader");
            var add = page.FindByName<Button>("AddAction");
            var title = header.Children.OfType<Label>().Single(label => Grid.GetColumn(label) == 1);
            var viewport = home ? (VisualElement)page.FindByName<ScrollView>("ContentViewport")
                : ((PlansViewModel)page.BindingContext).IsEmpty ? page.FindByName<ScrollView>("EmptyViewport")
                : page.FindByName<CollectionView>("ContentViewport");
            // D-442: the valid Undo reservation makes historical Home row counts obsolete; measure the actual ordinary viewport.
            if (Shell.GetNavBarIsVisible(page) || Grid.GetRow(header) != 0 || add.Parent != header
                || Grid.GetColumn(add) != 2 || header.FlowDirection != FlowDirection.LeftToRight
                || add.Width < 44 || add.Height < 44 || add.Width > 50 || add.Height > 50 || !add.IsEnabled || !add.IsVisible
                || string.IsNullOrWhiteSpace(SemanticProperties.GetDescription(add))
                || title.Bounds.Right > add.Bounds.Left + 1 || viewport.Y < header.Height - 1
                || Math.Abs(viewport.Y + viewport.Height - grid.Height) > 2)
                throw new ReviewCheckException("Current header action or uninterrupted viewport geometry differs.");
            var nativeRows = new List<object>();
            int? titleRight = null;
            foreach (var element in new VisualElement[] { title, add })
            {
                await Until(() => element.Handler?.PlatformView is Android.Widget.TextView { Layout.LineCount: > 0, IsShown: true, HasWindowFocus: true });
                var text = (Android.Widget.TextView)element.Handler!.PlatformView!; var layout = text.Layout!;
                var expected = element is Label label ? label.Text : ((Button)element).Text;
                using var visible = new Android.Graphics.Rect();
                if (text.Text != expected || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0)
                    || !text.GetGlobalVisibleRect(visible) || visible.Width() != text.Width || visible.Height() != text.Height)
                    throw new ReviewCheckException("Actual OS-scaled header text or target is incomplete.");
                MonetaryOsScope.RequireCompleteHeight(text, layout);
                if (element is Label) titleRight = visible.Right;
                else if (titleRight is null || visible.Left < titleRight.Value)
                    throw new ReviewCheckException("Actual Android Add target is not physically right of the complete title.");
                nativeRows.Add(new { Text = expected, text.Width, text.Height, Lines = layout.LineCount, NativeVisible = true,
                    NativeLeft = visible.Left, NativeRight = visible.Right, NativeTop = visible.Top, NativeBottom = visible.Bottom });
            }
            return new { Header = header.Bounds, Add = add.Bounds, Title = title.Bounds, Viewport = viewport.Bounds,
                Root = grid.Bounds, NativeReadings = nativeRows, NoBottomAddBand = true,
                FullName = SemanticProperties.GetDescription(add), Filled = page.BindingContext is PlansViewModel plans ? !plans.IsEmpty : ((HomeViewModel)page.BindingContext).HasAccounts };
        }
        async Task CancelEditor(bool home)
        {
            Page? editor = null;
            await Until(() =>
            {
                editor = Shell.Current.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage;
                return home ? editor?.BindingContext is EntryEditorViewModel { IsFormEditable: true }
                    : editor?.BindingContext is PlanEditorViewModel { CanInteract: true };
            });
            var model = editor!.BindingContext;
            var command = model is EntryEditorViewModel entry ? entry.CancelCommand : ((PlanEditorViewModel)model).CancelCommand;
            var cancel = ReimbursementViews(editor).OfType<Button>().Single(button => ReferenceEquals(button.Command, command));
            await Invoke(cancel);
            await Until(() => !Shell.Current.Navigation.ModalStack.Contains(editor) && !ReferenceEquals(Shell.Current.CurrentPage, editor));
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await Shell.Current.GoToAsync("//home", false); await ChangeTheme(theme);
                await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                foreach (var kind in new[] { "home", "plans", "filled-plans" })
                {
                    var stage = "headers-os-" + width + "-" + language + "-" + theme + "-" + kind + (positionOnly ? "-native-placement" : "");
                    var file = Path.Combine(root, stage + ".json");
                    if (File.Exists(file)) { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(file, ct))); continue; }
                    Page page;
                    if (kind == "filled-plans")
                    {
                        var model = new PlansViewModel(original.Store, original.Plans, original.Translator, local,
                            services.GetRequiredService<IDateFormatter>(), original.Time, services.GetRequiredService<ReminderService>());
                        page = new PlansPage(model); await original.ShowAsync(page);
                        await ((PlansPage)page).PendingPublication.WaitAsync(ct); model.SegmentIndex = 2;
                        await Until(() => model.Rows.Count > 0);
                    }
                    else
                    {
                        await Shell.Current.GoToAsync(kind == "home" ? "//home" : "//plans", false);
                        await Until(() => kind == "home" ? Shell.Current.CurrentPage is HomePage { IsLoaded: true } : Shell.Current.CurrentPage is PlansPage { IsLoaded: true });
                        page = Shell.Current.CurrentPage;
                        await (page is HomePage homePage ? homePage.PendingPublication : ((PlansPage)page).PendingPublication).WaitAsync(ct);
                    }
                    var before = await Geometry(page, kind == "home");
                    object? returned = null;
                    if (!positionOnly && kind != "filled-plans")
                    {
                        await Invoke(page.FindByName<Button>("AddAction")); await CancelEditor(kind == "home");
                        await (page is HomePage homePage ? homePage.PendingPublication : ((PlansPage)page).PendingPublication).WaitAsync(ct);
                        returned = await Geometry(page, kind == "home");
                    }
                    if (originalRows != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                        throw new ReviewCheckException("Header geometry or Add/Cancel changed original rows.");
                    var observation = new { Stage = stage, ProcessId = Environment.ProcessId, Before = before, Returned = returned,
                        NativeAddAndCancel = !positionOnly && kind != "filled-plans", NativePlacementOnly = positionOnly, SourceAndBaseUnchanged = true, AcceptedWrites = 0 };
                    await File.WriteAllTextAsync(file, JsonSerializer.Serialize(observation), ct); observations.Add(observation);
                    LogReport("headers-os-observation", comparison, observation);
                    await original.LeaveAsync();
                }
            }
        }
        finally { await Shell.Current.GoToAsync("//home", false); await ChangeTheme(oldTheme); await SetLanguageAsync(app, local, oldLanguage, ct); }
        return new { Comparison = comparison, Scope = (positionOnly ? "headers-os-native-" : "headers-os-") + width, Observations = observations,
            OriginalSourceAndBaseUnchanged = originalRows == await original.AllMoneyRowsAsync(ct) && baseline == await ImportFinancialNativeReview.StoredAsync(services, ct),
            AcceptedWrites = 0, NoSeedOrFinancialReplay = true, Device = DeviceEvidence() };
    }
}
