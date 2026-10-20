using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks the original complete editor and exact native edit/duplicate writers in independent Release/AOT.</summary>
    private static async Task<object> ReviewEntryFormAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var original = await EntryFormNativeReview.PrepareAsync(Path.Combine(folder, "original"), ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var observations = new List<object>();
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        async Task SetTheme(ThemeChoice value)
        {
            if (theme.Choice == value) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(value); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        Task Show(EntryEditorPage page, Dictionary<string, object> query) => EntryFormNativeReview.ShowRoutedAsync(page, query);
        async Task Close(EntryEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original editor route changed.");
            await Shell.Current.GoToAsync("..", false); await Task.Delay(100, ct);
        }
        async Task Invoke(EntryEditorPage page, string action)
        {
            var model = (EntryEditorViewModel)page.BindingContext;
            var retry = model.ReloadFormCommand;
            if (action == "amount")
            {
                var field = page.FindByName<Entry>("AmountEntry");
                await page.FindByName<ScrollView>("ValidationViewport").ScrollToAsync(field, ScrollToPosition.MakeVisible, false);
                if (field.Handler?.PlatformView is not Android.Widget.EditText { IsShown: true, HasWindowFocus: true, Enabled: true } input)
                    throw new ReviewCheckException("Original native amount field unavailable.");
                input.Text = MoneyText.ForInput(2100, "EUR", local.CurrentCulture); await Task.Yield(); return;
            }
            var target = action == "retry" ? page.FindByName<Button>("FormRetryButton")
                : ReimbursementViews(page).OfType<Button>().Single(button => ReferenceEquals(button.Command,
                    action == "save" ? model.SaveCommand : model.CancelCommand));
            if (!Visible(target)) throw new ReviewCheckException("Covered original editor target refused.");
            var deadline = DateTime.UtcNow.AddSeconds(30); Android.Widget.Button? native;
            while ((native = target.Handler?.PlatformView as Android.Widget.Button) is null || !native.IsShown || !native.HasWindowFocus
                || !native.Enabled || native.Context?.PackageName != Package)
            { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original editor native action unavailable."); await Task.Delay(50, ct); }
            if (native.Height / (double)(native.Resources?.DisplayMetrics?.Density ?? 1) < 43.5 || !native.PerformClick())
                throw new ReviewCheckException("Original editor native action refused."); await Task.Yield();
            if (action == "retry")
            {
                var until = DateTime.UtcNow.AddSeconds(30);
                while (retry.ExecutionTask is null)
                { if (DateTime.UtcNow > until) throw new IOException("Original Retry never started."); await Task.Delay(20); }
                await retry.ExecutionTask.WaitAsync(TimeSpan.FromSeconds(45));
            }
        }
        IEnumerable<Android.Views.View> Walk(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++)
                    if (group.GetChildAt(i) is { } child) foreach (var item in Walk(child)) yield return item;
        }
        async Task RetireConfirmation(EntryEditorPage page, Func<Task> replace)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new ReviewCheckException("Original owned dialog inspection requires API 29.");
            var model = (EntryEditorViewModel)page.BindingContext; var cancel = model.CancelCommand;
            await Invoke(page, "cancel"); Android.Widget.Button? discard = null; var until = DateTime.UtcNow.AddSeconds(20);
            while (discard is null)
            {
                discard = Android.Views.Inspectors.WindowInspector.GlobalWindowViews
                    .Where(window => window.Context?.PackageName == Package && window.IsShown && window.HasWindowFocus)
                    .SelectMany(Walk).OfType<Android.Widget.Button>()
                    .SingleOrDefault(button => button.IsShown && button.Text == NativeDigits.Apply(Translator.Instance["Common_Discard"]));
                if (DateTime.UtcNow > until) throw new ReviewCheckException("Original native discard panel unavailable.");
                if (discard is null) await Task.Delay(50, ct);
            }
            await replace();
            if (!discard.IsShown || !discard.HasWindowFocus || !discard.PerformClick()) throw new ReviewCheckException("Original discard target retired unexpectedly.");
            if (cancel.ExecutionTask is { } pending) await pending.WaitAsync(TimeSpan.FromSeconds(15), ct);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Retired discard closed a replacement form.");
        }
        async Task<object> Observe(EntryEditorPage page, string stage)
        {
            await Task.Delay(150, ct); var model = (EntryEditorViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadyFormSurface")) != model.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("FormLoadingSurface")) != model.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("FormErrorSurface")) != model.Loading.HasFailed)
                throw new ReviewCheckException("Original editor native cover differs.");
            var reads = new List<object>();
            if (!model.Loading.IsReady)
            {
                if (model.CanSave || model.IsFormEditable) throw new ReviewCheckException("Covered editor retained authority.");
                var label = page.FindByName<Label>(model.Loading.HasFailed ? "FormErrorCaption" : "FormLoadingCaption");
                if (label.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true, Layout: { LineCount: > 0 } } text
                    || text.Text != NativeDigits.Apply(label.Text)) throw new ReviewCheckException("Original editor cover text missing.");
                var contentWidth = text.Width - text.TotalPaddingLeft - text.TotalPaddingRight;
                var lines = Enumerable.Range(0, text.Layout.LineCount).Select(line => new
                { Line = line, WidthIncludingWhitespace = text.Layout.GetLineWidth(line), VisibleWidth = text.Layout.GetLineMax(line),
                    Left = text.Layout.GetLineLeft(line), Right = text.Layout.GetLineRight(line), Ellipsis = text.Layout.GetEllipsisCount(line) }).ToArray();
                LogReport("entry-form-caption-geometry", comparison, new { Stage = stage, Text = text.Text, ContentWidth = contentWidth, Lines = lines });
                // Android line width includes trailing whitespace. Check realized visible extents instead,
                // as the existing native settings/table observers do, without shrinking or changing the label.
                if (lines.Any(line => line.Ellipsis != 0 || line.Left < -1 || line.Right > contentWidth + 1 || line.VisibleWidth > contentWidth + 1))
                    throw new ReviewCheckException("Original editor cover text incomplete.");
                reads.Add(new { Text = text.Text, text.Width, text.Height, Lines = lines });
            }
            else
            {
                var scroll = page.FindByName<ScrollView>("ValidationViewport");
                foreach (var name in new[] { "AmountEntry", "ToAmountInput", "FeeInput", "DestinationFeeInput" })
                {
                    var entry = page.FindByName<Entry>(name); if (!Visible(entry)) continue;
                    await scroll.ScrollToAsync(entry, ScrollToPosition.MakeVisible, false); await Task.Delay(60, ct);
                    var until = DateTime.UtcNow.AddSeconds(20); Android.Widget.EditText? input;
                    while ((input = entry.Handler?.PlatformView as Android.Widget.EditText) is null || !input.IsShown
                        || !input.HasWindowFocus || !input.Enabled || input.Text != entry.Text || input.Width <= 0)
                    {
                        if (DateTime.UtcNow > until) throw new ReviewCheckException("Original native amount differs: " + name);
                        await Task.Delay(50, ct);
                    }
                    reads.Add(new { Field = name, input.Text, input.Width, input.Height });
                }
                foreach (var name in new[] { "AccountInput", "ToAccountInput" })
                {
                    var picker = page.FindByName<Picker>(name); if (!Visible(picker)) continue;
                    await scroll.ScrollToAsync(picker, ScrollToPosition.MakeVisible, false); await Task.Delay(60, ct);
                    if (picker.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } text
                        || picker.SelectedItem is not null && string.IsNullOrEmpty(text.Text)
                        || name == "AccountInput" && (model.Account is null) != (picker.SelectedItem is null))
                        throw new ReviewCheckException("Original native account caption unavailable.");
                    reads.Add(new { Field = name, text.Text, text.Width, text.Height });
                }
                await scroll.ScrollToAsync(0, 0, false);
            }
            var result = new { Stage = stage, model.Loading.IsLoading, model.Loading.HasFailed, model.Loading.IsReady,
                model.CanSave, model.IsFormEditable, model.CanReloadForm, model.HasNoAccounts, model.AmountText, model.ToAmountText,
                model.FeeText, model.DestinationFeeText, model.Note, model.IsDirty, model.HasPublishedForm, model.LoadError,
                NativeReadings = reads, ProcessId = Environment.ProcessId };
            observations.Add(result); LogReport("entry-form-observation", comparison, result); return result;
        }
        try
        {
            var first = !original.CompletedStages.Contains("en-Light-missing");
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                if (original.CompletedStages.Contains(name + "-" + value + "-missing")) continue;
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await EntryFormNativeReview.ContextAsync(services, original, name + "-" + value, first,
                    Show, Close, Invoke, Observe, RetireConfirmation, ct); first = false;
            }
            await EntryFormNativeReview.FinancialAsync(services, original, Show, Invoke, Observe, ct);
            await EntryFormNativeReview.FreshDefaultsAsync(services, original, Show, Close, Observe, ct);
        }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct); await original.Fixture.DisposeAsync(); }
        return new { Comparison = comparison, Observations = observations, Cases = original.Cases, ProcessId = Environment.ProcessId,
            OriginalReadOnlyRowsUnchanged = true, NativeFinancialSaves = 2, KnownAcceptanceBeforeNotification = true,
            NoAcceptedCommandReplayed = true, NativeControlsAndInvokeChecked = true, OriginalThemeAndLanguageRestored = true, Device = DeviceEvidence() };
    }
}
