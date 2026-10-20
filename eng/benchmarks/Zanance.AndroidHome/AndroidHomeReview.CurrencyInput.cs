using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Maui.Inputs;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks original first-open realization and native row selection without a Save surface.</summary>
    private static async Task<object> ReviewCurrencyInputAsync(App app, IServiceProvider services, string comparison, CancellationToken ct)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> Rows() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var before = await Rows();
        var model = new AccountFormModel(services.GetRequiredService<Translator>(), services.GetRequiredService<TimeProvider>())
        { Name = "", CurrencyCode = "EUR" };
        var form = new AccountFormView { BindingContext = model };
        var scroll = new ScrollView { Content = form, Padding = 16 };
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new Button { Text = "Finish currency observation", Command = new Command(() => done.TrySetResult()) };
        var layout = new Grid { RowDefinitions = new RowDefinitionCollection(new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)) };
        layout.Add(scroll); layout.Add(finish, 0, 1);
        var page = new ContentPage { Title = "Original unsaved currency input", Content = layout };
        var root = app.Windows[0].Page ?? throw new IOException("Original independent root required.");
        await root.Navigation.PushModalAsync(new NavigationPage(page), false);
        var box = ReimbursementViews(form).OfType<SfComboBox>().Single();
        var action = ReimbursementViews(box.DropDownButtonSettings.View).OfType<Button>().Single();
        var observations = new List<object>();
        Task? settled = null;
        var selectedOriginalRow = false;

        IEnumerable<Android.Views.View> OwnedPanels()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29)) yield break;
            foreach (var window in Android.Views.Inspectors.WindowInspector.GlobalWindowViews)
                if (window.Context?.PackageName == Package && window.IsShown && window.LayoutParameters is Android.Views.WindowManagerLayoutParams
                    { Type: Android.Views.WindowManagerTypes.ApplicationPanel }) yield return window;
        }
        IEnumerable<Android.Views.View> Walk(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++)
                    if (group.GetChildAt(i) is { } child) foreach (var item in Walk(child)) yield return item;
        }
        bool Visible(Android.Views.View view, bool complete = false)
        {
            if (!view.IsShown || !view.Enabled || view.Width <= 0 || view.Height <= 0) return false;
            using var rect = new Android.Graphics.Rect();
            return view.GetGlobalVisibleRect(rect) && (!complete || rect.Width() == view.Width && rect.Height() == view.Height);
        }
        bool VisibleAction(Android.Widget.Button view)
        {
            if (!Visible(view)) return false;
            using var rect = new Android.Graphics.Rect();
            if (!view.GetGlobalVisibleRect(rect)) return false;
            var density = view.Resources?.DisplayMetrics?.Density ?? 0;
            return density > 0 && rect.Width() + 1 >= 44 * density && rect.Height() + 1 >= 44 * density;
        }
        string? NativeName(Android.Views.View view)
        {
            // MAUI supplies semantic names through its accessibility delegate; the raw Android
            // ContentDescription alone does not represent the original native action's name.
            using var node = view.CreateAccessibilityNodeInfo();
            return node?.ContentDescription ?? view.ContentDescription;
        }
        List<object> PopupRows()
        {
            var packets = new List<object>();
            foreach (var panel in OwnedPanels())
            foreach (var view in Walk(panel).OfType<Android.Widget.TextView>())
            {
                var text = view.Text;
                if (view is not Android.Widget.Button && (text is not { Length: 3 } || !text.All(c => c is >= 'A' and <= 'Z'))) continue;
                int[] position = new int[2]; view.GetLocationOnScreen(position);
                using var rect = new Android.Graphics.Rect(); var visible = view.GetGlobalVisibleRect(rect);
                packets.Add(new { Text = text, Description = NativeName(view), RawDescription = view.ContentDescription, Type = view.GetType().FullName,
                    view.Width, view.Height, X = position[0], Y = position[1], GloballyVisible = Visible(view),
                    VisibleWidth = visible ? rect.Width() : 0, VisibleHeight = visible ? rect.Height() : 0,
                    CompleteBoundsVisible = Visible(view, true), IsActionTarget = view is Android.Widget.Button button && VisibleAction(button) });
            }
            return packets;
        }
        void Observe(string stage)
        {
            if (model.Name.Length != 0 && !model.Name.StartsWith("D423_", StringComparison.Ordinal))
                throw new IOException("Only the original fictitious input may enter this observation.");
            var packet = new { Comparison = comparison, Stage = stage, Sequence = observations.Count,
                model.Name, model.CurrencyCode, box.Text, Selected = box.SelectedItem as string, box.IsDropDownOpen,
                ProcessId = Environment.ProcessId, NoSaveSurface = true, PopupRows = PopupRows() };
            observations.Add(packet); LogReport("currency-input-observation", comparison, packet);
        }
        async Task SelectSettledOriginalRowAsync()
        {
            var deadline = DateTime.UtcNow.AddSeconds(15); Android.Widget.Button? choice = null;
            while (choice is null)
            {
                ct.ThrowIfCancellationRequested();
                if (!box.IsDropDownOpen || model.CurrencyCode != "EUR" || !Equals(box.SelectedItem, "EUR"))
                    throw new IOException("Original first-open selection changed before its native row review.");
                var panels = OwnedPanels().ToArray();
                var current = panels.SelectMany(Walk).OfType<Android.Widget.TextView>()
                    .Any(view => view.Text == "EUR" && Visible(view, true));
                if (current)
                {
                    var candidates = panels.SelectMany(Walk).OfType<Android.Widget.Button>()
                        .Where(view => NativeName(view) == "GBP" && VisibleAction(view)).ToArray();
                    if (candidates.Length > 1) throw new IOException("Ambiguous original native currency action.");
                    choice = candidates.SingleOrDefault();
                }
                if (choice is not null) break;
                if (DateTime.UtcNow >= deadline)
                {
                    Observe("original-first-open-observer-deadline");
                    throw new IOException("Original first-open rows did not become actually visible.");
                }
                await Task.Delay(50, ct);
            }
            Observe("original-first-open-settled-before-choice");
            // Active-window-only dumps omit this nonfocusable panel. Invoke the actual owned,
            // native row with a visible touch target; a model assignment is not native selection.
            if (finish.Handler?.PlatformView is not Android.Widget.Button endpoint || !endpoint.HasWindowFocus
                || !VisibleAction(choice) || !choice.PerformClick()) throw new IOException("Original native currency choice refused.");
            selectedOriginalRow = true;
            await Task.Delay(100, ct);
            Observe("original-native-gbp-choice-observed");
            if (model.CurrencyCode != "GBP" || !Equals(box.SelectedItem, "GBP") || box.IsDropDownOpen)
                throw new IOException("Original native currency choice did not publish the same selected code.");
        }
        void Changed(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName is "Text" or "SelectedItem" or "IsDropDownOpen") Observe("property-" + args.PropertyName);
        }
        void Clicked(object? sender, EventArgs args)
        {
            Observe("original-chevron-clicked");
            if (box.IsDropDownOpen && settled is null) settled = SelectSettledOriginalRowAsync();
        }
        box.PropertyChanged += Changed; action.Clicked += Clicked;
        try
        {
            await scroll.ScrollToAsync(box, ScrollToPosition.MakeVisible, false);
            while (finish.Handler?.PlatformView is not Android.Widget.Button native || !native.IsShown
                || !native.HasWindowFocus || !native.Enabled || native.Context?.PackageName != Package)
                await Task.Delay(50, ct);
            Observe("ready-for-original-native-input");
            await done.Task.WaitAsync(ct);
            if (settled is null) throw new IOException("Original native pointer was not observed.");
            await settled;
            Observe("finished-original-unsaved-draft");
            if (before != await Rows()) throw new IOException("Original currency observation changed stored money.");
            return new { Comparison = comparison, Observations = observations, OriginalAccountsAndEntriesUnchanged = true,
                NoSaveSurface = true, SelectedOriginalVisibleNativeRow = selectedOriginalRow, ProcessId = Environment.ProcessId };
        }
        finally
        {
            action.Clicked -= Clicked; box.PropertyChanged -= Changed;
            await root.Navigation.PopModalAsync(false);
            if (before != await Rows()) throw new IOException("Original currency observer changed stored accounts or entries.");
            LogReport("currency-input-original-rows", comparison, new { Comparison = comparison, OriginalAccountsAndEntriesUnchanged = true,
                NoSaveSurface = true, ProcessId = Environment.ProcessId });
        }
    }
}
