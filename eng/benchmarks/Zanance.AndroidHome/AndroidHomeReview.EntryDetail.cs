using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks actual original transaction detail controls and metadata commands in an independent Release/AOT package.</summary>
    private static async Task<object> ReviewEntryDetailAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var original = await EntryDetailNativeReview.PrepareAsync(Path.Combine(folder, "original"), ct);
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
        Task Show(EntryDetailPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(EntryDetailPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original transaction route changed.");
            await Shell.Current.Navigation.PopAsync(false); await Task.Delay(100, ct);
        }
        async Task Invoke(EntryDetailPage page, string action)
        {
            var model = (EntryDetailViewModel)page.BindingContext;
            var target = action == "retry" ? page.FindByName<Button>("RetryLoadButton")
                : ReimbursementViews(page).OfType<Button>().Single(button => ReferenceEquals(button.Command,
                    action == "review" ? model.MarkReviewedCommand : model.SaveRuleCommand));
            if (!Visible(target)) throw new ReviewCheckException("Covered detail target refused.");
            if (action != "retry") await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(target, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150, ct); var deadline = DateTime.UtcNow.AddSeconds(30); Android.Widget.Button? native;
            while ((native = target.Handler?.PlatformView as Android.Widget.Button) is null || !native.IsShown || !native.HasWindowFocus
                || !native.Enabled || native.Context?.PackageName != Package)
            { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original detail native action unavailable."); await Task.Delay(50, ct); }
            if (native.Height / (double)(native.Resources?.DisplayMetrics?.Density ?? 1) < 43.5 || !native.PerformClick())
                throw new ReviewCheckException("Original detail native action refused."); await Task.Yield();
        }
        async Task<object> Observe(EntryDetailPage page, string stage)
        {
            await Task.Delay(150, ct); var model = (EntryDetailViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadySurface")) != model.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("LoadingSurface")) != model.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("ErrorSurface")) != model.Loading.HasFailed
                || Visible(page.FindByName<Label>("MissingSurface")) != (model.Loading.IsReady && model.NotFound))
                throw new ReviewCheckException("Original transaction native cover differs.");
            var reads = new List<object>();
            async Task Read(Label label)
            {
                if (model.Loading.IsReady && !model.NotFound)
                    await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(label, ScrollToPosition.MakeVisible, false);
                await Task.Delay(40, ct);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true, Layout: { LineCount: > 0 } } text
                    || text.Text != NativeDigits.Apply(label.Text)) throw new ReviewCheckException("Original transaction field missing.");
                for (var line = 0; line < text.Layout.LineCount; line++)
                    if (text.Layout.GetEllipsisCount(line) != 0 || text.Layout.GetLineWidth(line) > text.Width - text.TotalPaddingLeft - text.TotalPaddingRight + 1)
                        throw new ReviewCheckException("Original transaction native field incomplete.");
                reads.Add(new { Text = text.Text, text.Width, text.Height, Lines = text.Layout.LineCount });
            }
            if (!model.Loading.IsReady)
            {
                if (model.CanInteract) throw new ReviewCheckException("Covered detail retained authority.");
                await Read(page.FindByName<Label>(model.Loading.HasFailed ? "ErrorCaption" : "LoadingCaption"));
            }
            else if (!model.NotFound)
            {
                foreach (var expected in new[] { model.Heading, model.AmountText }.Concat(model.Lines.Select(line => line.Value)))
                {
                    var labels = ReimbursementViews(page).OfType<Label>().Where(label => Visible(label) && label.Text == expected).ToArray();
                    if (labels.Length == 0) throw new ReviewCheckException("Complete original detail field unavailable.");
                    foreach (var label in labels) await Read(label);
                }
            }
            else await Read(page.FindByName<Label>("MissingSurface"));
            var result = new { Stage = stage, model.Loading.IsReady, model.Loading.HasFailed, model.Loading.IsLoading,
                model.NotFound, model.Heading, model.AmountText, Fields = model.Lines.ToArray(), NativeReadings = reads,
                Documents = model.Attachments.Select(row => new { row.Name, row.Details }).ToArray(), ProcessId = Environment.ProcessId };
            observations.Add(result); LogReport("entry-detail-observation", comparison, result); return result;
        }
        try
        {
            var first = true;
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await EntryDetailNativeReview.ContextAsync(services, original, name + "-" + value, first, Show, Close, Invoke, Observe, ct); first = false;
            }
            await EntryDetailNativeReview.MetadataAsync(services, original, Show, Close, Invoke, Observe, ct);
        }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct); await original.Fixture.DisposeAsync(); }
        return new { Comparison = comparison, Observations = observations, Cases = original.Cases, ProcessId = Environment.ProcessId,
            OriginalReadOnlyRowsUnchanged = true, MetadataCommands = 2, FinancialCommands = 0, NoAcceptedCommandReplayed = true,
            NativeControlsAndInvokeChecked = true, OriginalThemeAndLanguageRestored = true, Device = DeviceEvidence() };
    }
}
