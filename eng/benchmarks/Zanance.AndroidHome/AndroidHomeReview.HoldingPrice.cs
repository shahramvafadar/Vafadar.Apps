using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    private static async Task<object> ReviewHoldingPriceAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var originalTheme = theme.Choice;
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(),
            Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var before = await BaseRows(); var captures = new List<object>();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        bool Visible(Element item)
        {
            for (Element? current = item; current is not null; current = current.Parent)
                if (current is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        ScrollView? FindScrollView(Element page) => ReimbursementViews(page).OfType<ScrollView>().SingleOrDefault(scroll => scroll.Orientation == ScrollOrientation.Vertical);
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Show(HoldingDetailPage page) { await Shell.Current.Navigation.PushModalAsync(page, false); await Task.Delay(250, ct); }
        async Task Close(HoldingDetailPage page)
        {
            if (!ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) throw new IOException("Original price page changed.");
            await Shell.Current.Navigation.PopModalAsync(false); await AwaitRootHomeAsync(ct);
        }
        async Task Invoke(HoldingDetailPage page, ICommand command, object? argument)
        {
            await Task.Delay(150);
            var button = ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command)
                && Equals(b.CommandParameter, argument) && Visible(b));
            await (FindScrollView(page) ?? throw new IOException("Original price viewport missing.")).ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            Android.Widget.Button? native;
            while ((native = button.Handler?.PlatformView as Android.Widget.Button) is null
                || !native.IsShown || !native.HasWindowFocus || !native.Enabled || native.Context?.PackageName != Package)
            { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original native price Invoke unavailable."); await Task.Delay(50, ct); }
            if (!native.PerformClick()) throw new ReviewCheckException("Original native price action refused.");
            await Task.Yield();
        }
        async Task Observe(HoldingDetailPage page, string stage)
        {
            await Task.Delay(200);
            var model = (HoldingDetailViewModel)page.BindingContext; var editor = model.PriceEditor;
            var input = page.FindByName<Entry>("ManualPriceInput"); var feedback = page.FindByName<Label>("ManualPriceFeedback");
            if (input.Text != editor.Raw || !ReferenceEquals(input.Parent, feedback.Parent))
                throw new IOException("Original price draft or field feedback binding changed.");
            if (editor.Error is not null)
            {
                await (FindScrollView(page) ?? throw new IOException("Original price viewport missing.")).ScrollToAsync(feedback, ScrollToPosition.MakeVisible, false);
                var limit = DateTime.UtcNow.AddSeconds(15);
                Android.Widget.TextView? native;
                while ((native = feedback.Handler?.PlatformView as Android.Widget.TextView) is null
                    || !native.IsShown || !native.HasWindowFocus || native.Text != NativeDigits.Apply(editor.Error)
                    || native.Layout is not { LineCount: > 0 })
                { if (DateTime.UtcNow > limit) throw new ReviewCheckException("Original complete price feedback missing."); await Task.Delay(50, ct); }
                for (var line = 0; line < native.Layout.LineCount; line++)
                    if (native.Layout.GetEllipsisCount(line) != 0 || native.Layout.GetLineWidth(line) > native.Width - native.TotalPaddingLeft - native.TotalPaddingRight + 1)
                        throw new ReviewCheckException("Original price feedback line incomplete.");
            }
            var save = ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, editor.SaveCommand) && Visible(b));
            if (save.IsEnabled != editor.CanSubmit) throw new IOException("Original price Save state differs.");
            captures.Add(new { Language = stage, editor.Raw, editor.Error, editor.CanSubmit, editor.NeedsRefresh });
        }
        HoldingPriceNativeReview.Prepared? prepared = null;
        try
        {
            prepared = await HoldingPriceNativeReview.PrepareAsync(services, Path.Combine(folder, "financial"), Show, Invoke, Close,
                (page, stage) => Observe(page, "original-" + stage), ct,
                resumeBeforeInvoke: Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_resume_price", false) == true);
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, name, ct);
                prepared.Model.PriceEditor.Retire();
                prepared.Model.PriceEditor.Error = services.GetRequiredService<Translator>()["Holding_PriceSourceChanged"];
                var page = new HoldingDetailPage(prepared.Model); await Show(page);
                await Observe(page, name + "-" + choice); await Close(page);
            }
            if (prepared.Rows != await ImportFinancialNativeReview.StoredAsync(prepared.Fixture, ct))
                throw new IOException("Original price caption observation changed rows.");
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally
        {
            if (Shell.Current.Navigation.ModalStack.LastOrDefault() is HoldingDetailPage) await Shell.Current.Navigation.PopModalAsync(false); await AwaitRootHomeAsync(ct);
            if (prepared is not null) await prepared.Fixture.DisposeAsync();
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, language.CultureName, ct);
        }
        if (before != await BaseRows()) throw new IOException("Independent base store changed during price review.");
        return new
        { Comparison = comparison, Cases = prepared!.Cases, Observations = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
            OriginalThemeAndLanguageRestored = true, ReadOnlyCaptions = 6, ConfirmationUsesExplicitPort = true };
    }
}