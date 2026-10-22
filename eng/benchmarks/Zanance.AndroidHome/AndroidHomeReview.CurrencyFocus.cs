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
        var resumeAfterArrow = Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_currency_resume", false) == true;
        var resumeAtUsdRow = Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_currency_usd_row", false) == true;
        var retainedScenes = resumeAtUsdRow ? 4 : resumeAfterArrow ? 2 : 0;
        if (resumeAtUsdRow && !resumeAfterArrow) throw new ReviewCheckException("Original currency continuation required before USD-row inspection.");
        if (resumeAfterArrow)
        {
            var prior = Platform.CurrentActivity?.Intent?.GetStringExtra("review_baseline");
            if (!isolated || prior is null || !System.Text.RegularExpressions.Regex.IsMatch(prior, "^[0-9a-f]{32}$"))
                throw new ReviewCheckException("Original isolated failed input identity required.");
            var queryPrior = Platform.CurrentActivity?.Intent?.GetStringExtra("review_currency_query_baseline");
            if (resumeAtUsdRow && (queryPrior is null || !System.Text.RegularExpressions.Regex.IsMatch(queryPrior, "^[0-9a-f]{32}$")))
                throw new ReviewCheckException("Original failed native-row observation identity required.");
            var found = false; var queryFound = !resumeAtUsdRow;
            var failureFiles = Directory.EnumerateFiles(root, "failure-*.json")
                .Concat(Directory.EnumerateFiles(Directory.GetParent(root)!.FullName, Path.GetFileName(root) + "-failure-*.json"));
            foreach (var file in failureFiles)
            {
                using var failure = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
                found |= failure.RootElement.GetProperty("Comparison").GetString() == prior
                    && failure.RootElement.GetProperty("ReviewCheck").GetString() == "Original focus-bound text route differs.";
                queryFound |= resumeAtUsdRow && failure.RootElement.GetProperty("Comparison").GetString() == queryPrior
                    && failure.RootElement.GetProperty("ErrorType").GetString() == "System.IO.IOException"
                    && (failure.RootElement.GetProperty("Stack").GetString()?.Contains("PlanEditorNativeReview.WaitAsync", StringComparison.Ordinal) ?? false);
            }
            if (!found) throw new ReviewCheckException("Preserved original failed name-input report required; never invent one.");
            if (!queryFound) throw new ReviewCheckException("Preserved original failed USD-row wait required.");
        }
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
        bool VisibleCurrencyRow(Android.Widget.Button button)
        {
            using var bounds = new Android.Graphics.Rect();
            var density = button.Resources?.DisplayMetrics?.Density ?? 0;
            // D-452: Samsung's popup is 123px tall while its 44dp overlay rounds to 124px.
            // Admit only that single physical-pixel rounding loss, retaining the full width and minimum target.
            return density > 0 && button.IsShown && button.IsAttachedToWindow && button.Enabled && button.Clickable
                && button.GetGlobalVisibleRect(bounds) && bounds.Width() == button.Width
                && bounds.Height() > 0 && button.Height - bounds.Height() is >= 0 and <= 1
                && bounds.Height() + 1 >= (int)Math.Ceiling(44 * density);
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
            object EditingState()
            {
                var editable = input.EditableText;
                return new { Text = input.Text, input.SelectionStart, input.SelectionEnd,
                    ComposingStart = editable is null ? -1 : BaseInputConnection.GetComposingSpanStart(editable),
                    ComposingEnd = editable is null ? -1 : BaseInputConnection.GetComposingSpanEnd(editable) };
            }
            var beforeInput = EditingState();
            // D-452: Samsung's IME can keep the entire preceding controlled value composing.
            // CommitText replaces that range despite SetSelection; settle it before explicit append/replace.
            var hasComposition = input.EditableText is { } buffer && BaseInputConnection.GetComposingSpanStart(buffer) >= 0;
            var finishedComposition = !hasComposition || connection.FinishComposingText();
            var afterComposition = EditingState();
            if (!finishedComposition || input.EditableText is { } remaining && BaseInputConnection.GetComposingSpanStart(remaining) >= 0)
                throw new ReviewCheckException("Controlled draft composition did not finish before selection.");
            var length = input.Text?.Length ?? 0;
            var selected = connection.SetSelection(append ? length : 0, length); var afterSelection = EditingState();
            if (!selected) throw new ReviewCheckException("Original input selection refused.");
            using var raw = new Java.Lang.String(value);
            var committed = connection.CommitText(raw, 1); var afterCommit = EditingState();
            await Task.Delay(150, ct);
            // Only this newly owned fictional draft is exposed; capture an actual mismatch before its page retires.
            LogReport("currency-focus-input-connection", comparison, new { Append = append, RequestFocus = requestFocus,
                Insert = value, Before = beforeInput, HadComposition = hasComposition, FinishedComposition = finishedComposition,
                AfterComposition = afterComposition, AfterSelection = afterSelection, AfterCommit = afterCommit,
                AfterDelay = EditingState(), SelectionAccepted = selected, CommitAccepted = committed });
            if (!committed) throw new ReviewCheckException("Original native draft commit refused.");
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
            var remainedOnName = nameInput.HasWindowFocus && nameInput.Equals(nameInput.RootView?.FindFocus());
            if (!resumeAtUsdRow)
            {
                if (!resumeAfterArrow) Record("before-arrow", box, nameInput, currencyInput);
                // A retired failed draft requires native setup, not recertification of completed observations.
                await Invoke(arrow);
                if (!resumeAfterArrow) Record("after-arrow-before-text", box, nameInput, currencyInput);
                else LogReport("currency-focus-restored-precondition", comparison, new { RetainedOriginalScenes = retainedScenes,
                    model.Name, model.CurrencyCode, box.IsDropDownOpen, RestoredNativeSetupOnly = true });
                remainedOnName = nameInput.HasWindowFocus && nameInput.Equals(nameInput.RootView?.FindFocus());
                if (remainedOnName)
                {
                    await Replace(nameInput, "USD", requestFocus: false, append: true);
                    Record("name-owner-input-result", box, nameInput, currencyInput);
                    if (model.Name != marker + "USD" || model.CurrencyCode != "EUR") throw new ReviewCheckException("Original focus-bound text route differs.");
                }
                if (box.IsDropDownOpen) await Invoke(arrow);
                await Scroll(entry); await Replace(nameInput, marker); await Scroll(box);
            }
            await Replace(currencyInput, "USD");
            if (!resumeAtUsdRow) Record("focused-currency-query", box, nameInput, currencyInput);
            else LogReport("currency-focus-restored-query", comparison, new { RetainedOriginalScenes = retainedScenes,
                model.Name, model.CurrencyCode, box.Text, CurrencyOwnsFocus = currencyInput.Equals(currencyInput.RootView?.FindFocus()),
                RestoredNativeSetupOnly = true });
            if (model.Name != marker) throw new ReviewCheckException("Focused currency input changed the name.");
            if (!box.IsDropDownOpen) await Invoke(arrow);
            object Geometry(Android.Views.View view)
            {
                using var rect = new Android.Graphics.Rect(); var visible = view.GetGlobalVisibleRect(rect);
                return new { Type = view.GetType().FullName, view.Width, view.Height, view.IsShown, view.Enabled,
                    view.Clickable, view.HasWindowFocus, HasVisibleRect = visible, Left = rect.Left, Top = rect.Top,
                    Right = rect.Right, Bottom = rect.Bottom };
            }
            void InspectPopup()
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return;
                var windows = Android.Views.Inspectors.WindowInspector.GlobalWindowViews
                    .Where(window => window.Context?.PackageName == Package && window.IsShown).Select(window => new
                    {
                        Root = Geometry(window), WindowType = (window.LayoutParameters as Android.Views.WindowManagerLayoutParams)?.Type.ToString(),
                        UsdCandidates = Walk(window).Where(view => view is Android.Widget.TextView label && label.Text == "USD" || Name(view) == "USD")
                            .Take(12).Select(view => new { View = Geometry(view), Name = Name(view), IsNativeButton = view is Android.Widget.Button }).ToArray()
                    }).ToArray();
                var decor = Platform.CurrentActivity!.Window!.DecorView;
                using var frame = new Android.Graphics.Rect(); decor.GetWindowVisibleDisplayFrame(frame);
                bool? imeVisible = null; int? imeBottom = null;
                if (OperatingSystem.IsAndroidVersionAtLeast(30) && decor.RootWindowInsets is { } insets)
                { imeVisible = insets.IsVisible(Android.Views.WindowInsets.Type.Ime()); imeBottom = insets.GetInsets(Android.Views.WindowInsets.Type.Ime()).Bottom; }
                LogReport("currency-focus-popup-inspection", comparison, new { box.IsDropDownOpen, box.Text,
                    Query = Geometry(currencyInput), VisibleFrame = new { frame.Left, frame.Top, frame.Right, frame.Bottom },
                    ImeVisible = imeVisible, ImeBottom = imeBottom, Windows = windows, ReadOnlyInspection = true });
            }
            Android.Widget.Button? usd = null; var inspectionCount = 0; long nextInspection = 0;
            await PlanEditorNativeReview.WaitAsync(() =>
            {
                if (inspectionCount < 3 && Environment.TickCount64 >= nextInspection)
                { inspectionCount++; nextInspection = Environment.TickCount64 + 1000; InspectPopup(); }
                // Preserve the actual owned panel/button/semantic identity; account only for the observed one-pixel popup rounding.
                usd = Panels().SelectMany(Walk).OfType<Android.Widget.Button>().SingleOrDefault(button => Name(button) == "USD" && VisibleCurrencyRow(button));
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
                NativeUsdSelected = true, NativeDiscard = true, IsolatedEmptySource = isolated, ResumedAfterOriginalArrow = resumeAfterArrow, ResumedAtUsdRow = resumeAtUsdRow, RetainedOriginalScenes = retainedScenes, AcceptedWrites = 0, OriginalSourceAndBaseUnchanged = true, Device = DeviceEvidence() };
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
