using Android.Views.InputMethods;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Maui.Inputs;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Diagnoses actual draft-name versus currency input ownership without another accepted type or event.</summary>
    private static async Task<object> ReviewCurrencyFocusAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var isolated = AppInfo.Current.PackageName == "pro.vafadar.zanance.currencyfocusreview";
        var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>();
        if (isolated)
        {
            await SetLanguageAsync(app, local, "en", ct);
            if (themes.Choice != ThemeChoice.Light)
            {
                var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                app.ShellRebuilt += Rebuilt;
                try { themes.Set(ThemeChoice.Light); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                finally { app.ShellRebuilt -= Rebuilt; }
            }
            await AwaitRootHomeAsync(ct);
        }
        if (local.CurrentLanguage.CultureName != "en" || themes.Choice != ThemeChoice.Light
            || Math.Abs((Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 0) - 1f) > 0.01f)
            throw new ReviewCheckException("Original normal English/Light currency boundary required.");
        var folder = Path.Combine(root, "original-physical-rates");
        if (!isolated && !File.Exists(Path.Combine(folder, "rate-native-delete-accepted.json")))
            throw new ReviewCheckException("Original completed physical source required; never seed another type or event.");
        using var original = isolated ? null : await PlanEditorNativeReview.ContinuePhysicalRateOutcomesAsync(services, folder, ct);
        var store = original?.Store ?? services.GetRequiredService<ZananceStore>();
        var holdings = (original?.Services ?? services).GetRequiredService<HoldingStore>();
        if (isolated && ((await store.GetAccountsAsync(cancellationToken: ct)).Count != 0
            || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0 || (await holdings.GetTypesAsync(ct)).Count != 0))
            throw new ReviewCheckException("Independent phone probe must have no financial account, entry or type.");
        Task<string> ReadSource() => original is not null ? original.AllMoneyRowsAsync(ct) : ImportFinancialNativeReview.StoredAsync(services, ct);
        var source = await ReadSource(); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var text = services.GetRequiredService<Translator>();
        var model = new AssetTypeEditorViewModel(holdings, store, text, local, services.GetRequiredService<IAppInteraction>());
        var page = new AssetTypeEditorPage(model);
        var route = isolated ? "currency-focus-native-" + Guid.NewGuid().ToString("N") : await original!.ShowAsync(page);
        if (isolated)
        {
            Routing.RegisterRoute(route, new CurrencyFocusPageFactory(page));
            await Shell.Current.GoToAsync(route, new Dictionary<string, object>());
        }
        async Task Leave()
        {
            if (original is not null) await original.LeaveAsync();
            else { await Shell.Current.GoToAsync("//home", false); await AwaitRootHomeAsync(ct); }
        }
        var observations = new List<object>(); const string marker = "D445_unsaved_currency";
        IEnumerable<Android.Views.View> Walk(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++) if (group.GetChildAt(i) is { } child)
                    foreach (var nested in Walk(child)) yield return nested;
        }
        IEnumerable<Android.Views.View> Panels()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29)) yield break;
            foreach (var window in Android.Views.Inspectors.WindowInspector.GlobalWindowViews)
                if (window.Context?.PackageName == Package && window.IsShown && window.LayoutParameters is Android.Views.WindowManagerLayoutParams
                    { Type: Android.Views.WindowManagerTypes.ApplicationPanel }) yield return window;
        }
        bool Visible(Android.Views.View view)
        {
            using var bounds = new Android.Graphics.Rect();
            return view.IsShown && view.Enabled && view.GetGlobalVisibleRect(bounds) && bounds.Width() == view.Width && bounds.Height() == view.Height;
        }
        string? Name(Android.Views.View view)
        {
            using var node = view.CreateAccessibilityNodeInfo(); return node?.ContentDescription ?? view.ContentDescription;
        }
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(target, ScrollToPosition.MakeVisible, false); break; }
            await Task.Delay(100, ct);
        }
        async Task Invoke(Button button)
        {
            await Scroll(button);
            await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Widget.Button { IsShown: true, Enabled: true, HasWindowFocus: true }, ct);
            var native = (Android.Widget.Button)button.Handler!.PlatformView!;
            if (native.Context?.PackageName != Package || !Visible(native) || !native.PerformClick())
                throw new ReviewCheckException("Original owned native draft action required.");
            await Task.Delay(100, ct);
        }
        async Task Replace(Android.Widget.EditText input, string value, bool requestFocus = true, bool append = false)
        {
            await PlanEditorNativeReview.WaitAsync(() => input.IsShown && input.IsAttachedToWindow && input.HasWindowFocus, ct);
            if (requestFocus && !input.RequestFocus()) throw new ReviewCheckException("Original requested editor did not accept focus.");
            if (!input.IsFocused || input.Context?.PackageName != Package || !input.Equals(input.RootView?.FindFocus()))
            {
                LogReport("currency-focus-owner-refusal", comparison, new { input.IsFocused, input.HasWindowFocus,
                    OwnPackage = input.Context?.PackageName == Package, RootOwns = input.Equals(input.RootView?.FindFocus()),
                    ActivityOwns = input.Equals(Platform.CurrentActivity?.CurrentFocus) });
                throw new ReviewCheckException("Actual focused input owner required before commit.");
            }
            using var info = new EditorInfo(); using var connection = input.OnCreateInputConnection(info)
                ?? throw new ReviewCheckException("Original focused InputConnection required.");
            var length = input.Text?.Length ?? 0;
            if (!connection.SetSelection(append ? length : 0, length)) throw new ReviewCheckException("Original input selection refused.");
            using var raw = new Java.Lang.String(value);
            if (!connection.CommitText(raw, 1)) throw new ReviewCheckException("Original native draft commit refused.");
            await Task.Delay(150, ct);
        }
        void Record(string stage, SfComboBox box, Android.Widget.EditText nameInput, Android.Widget.EditText currencyInput)
        {
            var packet = new { Stage = stage, ProcessId = Environment.ProcessId, model.Name, model.CurrencyCode, box.Text,
                Selected = box.SelectedItem as string, box.IsDropDownOpen, NameOwnsFocus = nameInput.HasWindowFocus && nameInput.Equals(nameInput.RootView?.FindFocus()),
                NameWindowHasFocus = nameInput.HasWindowFocus, ActivityNameOwnsFocus = nameInput.Equals(Platform.CurrentActivity?.CurrentFocus),
                CurrencyOwnsFocus = currencyInput.HasWindowFocus && currencyInput.Equals(currencyInput.RootView?.FindFocus()),
                CurrencyWindowHasFocus = currencyInput.HasWindowFocus, NameNativeText = nameInput.Text,
                CurrencyNativeText = currencyInput.Text, VisiblePopupCodes = Panels().SelectMany(Walk).OfType<Android.Widget.Button>()
                    .Where(Visible).Select(Name).Where(name => name is { Length: 3 }).ToArray() };
            observations.Add(packet); LogReport("currency-focus-observation", comparison, packet);
        }
        try
        {
            await model.ApplyQueryAsync(new Dictionary<string, object>());
            await PlanEditorNativeReview.WaitAsync(() => model.CanEdit && page.Width > 0, ct);
            model.KindIndex = (int)AssetKind.CountableItem;
            var entry = PlanEditorNativeReview.Views(page).OfType<Entry>().Single(field => SemanticProperties.GetDescription(field) == text["AssetType_Name"]);
            var box = PlanEditorNativeReview.Views(page).OfType<SfComboBox>().Single();
            var arrow = PlanEditorNativeReview.Views(box.DropDownButtonSettings.View).OfType<Button>().Single();
            await Scroll(entry);
            await PlanEditorNativeReview.WaitAsync(() => entry.Handler?.PlatformView is Android.Widget.EditText { IsShown: true }, ct);
            var nameInput = (Android.Widget.EditText)entry.Handler!.PlatformView!;
            await Replace(nameInput, marker); await Scroll(box);
            await PlanEditorNativeReview.WaitAsync(() => box.Handler?.PlatformView is Android.Views.ViewGroup, ct);
            Android.Widget.EditText? currencyEditor = null;
            await PlanEditorNativeReview.WaitAsync(() =>
            {
                currencyEditor = Walk((Android.Views.View)box.Handler!.PlatformView!).OfType<Android.Widget.EditText>().SingleOrDefault(input => input.IsShown);
                return currencyEditor is not null;
            }, ct);
            var currencyInput = currencyEditor!;
            if (model.CurrencyCode != "EUR" || model.Name != marker) throw new ReviewCheckException("Original default EUR and unsaved name required.");
            Record("before-arrow", box, nameInput, currencyInput);
            await Invoke(arrow); Record("after-arrow-before-text", box, nameInput, currencyInput);
            var remainedOnName = nameInput.HasWindowFocus && nameInput.Equals(nameInput.RootView?.FindFocus());
            if (remainedOnName)
            {
                // A window-level text injector follows the still-focused name; do not call that a currency search.
                await Replace(nameInput, "USD", requestFocus: false, append: true);
                if (model.Name != marker + "USD" || model.CurrencyCode != "EUR") throw new ReviewCheckException("Original focus-bound text route differs.");
                Record("name-owner-reproduction", box, nameInput, currencyInput);
            }
            if (box.IsDropDownOpen) await Invoke(arrow);
            await Scroll(entry); await Replace(nameInput, marker); await Scroll(box);
            await Replace(currencyInput, "USD"); Record("focused-currency-query", box, nameInput, currencyInput);
            if (model.Name != marker) throw new ReviewCheckException("Focused currency input changed the name.");
            if (!box.IsDropDownOpen) await Invoke(arrow);
            Android.Widget.Button? usd = null;
            await PlanEditorNativeReview.WaitAsync(() =>
            {
                usd = Panels().SelectMany(Walk).OfType<Android.Widget.Button>().SingleOrDefault(button => Name(button) == "USD" && Visible(button));
                return usd is not null;
            }, ct);
            if (usd is null || !usd.PerformClick()) throw new ReviewCheckException("Original visible filtered USD target required.");
            await PlanEditorNativeReview.WaitAsync(() => model.CurrencyCode == "USD" && Equals(box.SelectedItem, "USD") && !box.IsDropDownOpen, ct);
            Record("actual-native-usd-selected", box, nameInput, currencyInput);
            if (model.Name != marker || model.Committed) throw new ReviewCheckException("USD draft selection changed identity or saved a type.");
            var cancel = PlanEditorNativeReview.Views(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, model.CancelCommand));
            await Invoke(cancel);
            Android.Widget.Button? discard = null;
            await PlanEditorNativeReview.WaitAsync(() =>
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return false;
                discard = Android.Views.Inspectors.WindowInspector.GlobalWindowViews.Where(window => window.Context?.PackageName == Package && window.IsShown && window.HasWindowFocus)
                    .SelectMany(Walk).OfType<Android.Widget.Button>().SingleOrDefault(button => button.Text == text["Common_Discard"] && Visible(button));
                return discard is not null;
            }, ct);
            if (discard is null || !discard.PerformClick()) throw new ReviewCheckException("Original native draft discard required.");
            if (model.CancelCommand.ExecutionTask is { } closing) await closing.WaitAsync(ct);
            await Leave();
            if (source != await ReadSource() || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new ReviewCheckException("Currency input boundary changed original source or base rows.");
            return new { Comparison = comparison, Scope = "currency-focus", Observations = observations, NameRetainedFocusAfterArrow = remainedOnName,
                NativeUsdSelected = true, NativeDiscard = true, IsolatedEmptySource = isolated, AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, Device = DeviceEvidence() };
        }
        finally { await Leave(); if (isolated) Routing.UnRegisterRoute(route); }
    }

    /// <summary>Routes only this actual unsaved page in the separately identified empty physical probe.</summary>
    private sealed class CurrencyFocusPageFactory(Page page) : RouteFactory
    {
        /// <inheritdoc />
        public override Element GetOrCreate() => page;
        /// <inheritdoc />
        public override Element GetOrCreate(IServiceProvider services) => page;
    }
}
