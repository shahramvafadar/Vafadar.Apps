using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original retained occurrence reads, native covers and Retry over owned fixtures.</summary>
    private static async Task<object> ReviewOccurrenceRefreshAsync(App app, IServiceProvider services, string root,
        string run, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>();
        var theme = services.GetRequiredService<ThemeService>(); var text = services.GetRequiredService<Translator>();
        var sequence = 0; var context = string.Empty; var cases = new List<object>(); var views = new List<object>();
        async Task Ready(Func<bool> ready)
        {
            var until = DateTime.UtcNow.AddSeconds(40);
            while (!ready()) { if (DateTime.UtcNow >= until) throw new ReviewCheckException("Preserve the original pending native tab/read."); await Task.Delay(25, ct); }
        }
        async Task SelectTab(string route)
        {
            await AwaitReimbursementFocusAsync(ct);
            var shell = (AppShell)Shell.Current;
            var target = shell.CurrentItem.Items.Single(section => section.Items.Any(content => content.Route == route));
            if (ReferenceEquals(shell.CurrentItem.CurrentItem, target)) return;
            Android.Util.Log.Info("ZananceOccurrenceRefreshStage", JsonSerializer.Serialize(new
            { Run = run, Comparison = comparison, Sequence = ++sequence, Context = context, Route = route,
                Caption = text[route == "home" ? "Tab_Home" : "Tab_Plans"], Package }));
            await Ready(() => ReferenceEquals(shell.CurrentItem.CurrentItem, target));
        }
        async Task Theme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => done.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await done.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        bool Visible(Element item)
        {
            for (Element? current = item; current is not null; current = current.Parent)
                if (current is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        async Task Invoke(OccurrencePage page, ICommand command)
        {
            var button = ReimbursementViews(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
            if (!Visible(button)) throw new ReviewCheckException("Covered occurrence input cannot be invoked.");
            if (command != ((OccurrenceViewModel)page.BindingContext).ReloadCommand)
                await page.FindByName<ScrollView>("ValidationViewport").ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await AwaitReimbursementFocusAsync(ct);
            if (button.Handler?.PlatformView is not Android.Widget.Button native || !native.IsShown || !native.HasWindowFocus
                || native.Context?.PackageName != Package || !native.Enabled || !native.PerformClick())
                throw new ReviewCheckException("Original native occurrence button unavailable.");
            await Task.Yield();
        }
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            await Shell.Current.GoToAsync("//home"); await Theme(choice); await SetLanguageAsync(app, local, language, ct);
            context = language + "-" + choice;
            cases.Add(await OccurrenceRefreshNativeReview.RunAsync(services, Path.Combine(root, context), context,
                async page => { await SelectTab("plans"); await Shell.Current.Navigation.PushAsync(page, false); },
                () => SelectTab("home"), () => SelectTab("plans"),
                async (state, page, vm) =>
                {
                    await AwaitReimbursementFocusAsync(ct);
                    if (state == "loaded")
                    {
                        var field = ReimbursementViews(page).OfType<Entry>().Single(item => SemanticProperties.GetDescription(item) == text["Occurrence_ActualAmount"]);
                        if (field.Handler?.PlatformView is not Android.Widget.EditText native || native.Text != vm.ActualAmountText)
                            throw new ReviewCheckException("Original native payment draft changed.");
                    }
                    else
                    {
                        if (ReimbursementViews(page).OfType<Entry>().Any(Visible)) throw new ReviewCheckException("A covered original field remains visible.");
                        var message = text[state == "loading" ? "Occurrence_Loading" : "Occurrence_ReadFailed"];
                        var label = ReimbursementViews(page).OfType<Label>().Single(item => item.Text == message && Visible(item));
                        await Ready(() => label.Handler?.PlatformView is Android.Widget.TextView { Width: > 0, Height: > 0, IsShown: true });
                        if (((Android.Widget.TextView)label.Handler!.PlatformView!).Text != message)
                            throw new ReviewCheckException("Original native feedback differs.");
                    }
                    views.Add(new { Context = context, State = state, OriginalNativeTextMatched = true, CoveredInputsHidden = state != "loaded" });
                }, Invoke,
                async page => { if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original occurrence page changed."); await Shell.Current.Navigation.PopAsync(false); }, ct));
        }
        return new { Comparison = comparison, Cases = cases, NativeViews = views, OriginalTabInputs = sequence,
            SeparateFictitiousPlainSqliteFixtures = true, MainPackageTouched = false, NoFinancialReplay = true };
    }
}
