using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Uses original occurrence pages/buttons/native dialogs; the host answers only exact owned fictitious dialogs.</summary>
    private static async Task<object> ReviewOccurrenceCommitAsync(App app, IServiceProvider services, string root, string run, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var text = services.GetRequiredService<Translator>(); var native = services.GetRequiredService<IAppInteraction>();
        var sequence = 0;
        Task PublishDialog(OccurrenceCommitNativeReview.Dialog dialog, string context)
        {
            Android.Util.Log.Info("ZananceOccurrenceStage", JsonSerializer.Serialize(new
            { Run = run, Comparison = comparison, Sequence = ++sequence, Context = context, dialog.Confirmation,
                dialog.Title, dialog.Message, dialog.Accept, dialog.Cancel, Package = Package }));
            return Task.CompletedTask;
        }
        async Task Theme(ThemeChoice choice)
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await Theme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en", ct);
        var cases = await OccurrenceCommitNativeReview.RunAsync(services, root,
            async page => { await Shell.Current.Navigation.PushAsync(page, false); },
            async (page, command, parameter) =>
            {
                var button = ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command)
                    && (parameter is null || ReferenceEquals(b.CommandParameter, parameter)));
                await page.FindByName<ScrollView>("ValidationViewport").ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
                await AwaitReimbursementFocusAsync(ct);
                if (button.Handler?.PlatformView is not Android.Widget.Button target || !target.IsShown || !target.HasWindowFocus
                    || target.Context?.PackageName != Package || !target.Enabled || !target.PerformClick())
                    throw new ReviewCheckException("Original native occurrence action unavailable.");
            }, PublishDialog,
            async page => { if (ReferenceEquals(Shell.Current.CurrentPage, page)) await Shell.Current.Navigation.PopAsync(false); }, ct);
        var captions = new List<object>();
        // Reopen only the original native message adapter, never the accepted financial command.
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            await Theme(choice);
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language, ct); await AwaitReimbursementFocusAsync(ct);
                var dialog = new OccurrenceCommitNativeReview.Dialog(false, text["Error_Title"], text["Occurrence_SavedNotificationFailed"], text["Common_Ok"], text["Common_Ok"]);
                var showing = native.AlertAsync(dialog.Title, dialog.Message, dialog.Accept);
                await PublishDialog(dialog, language + "-" + choice + "-saved-message"); await showing;
                captions.Add(new { Language = language, Theme = choice.ToString(), FinancialCommandInvoked = false });
            }
        }
        return new { Comparison = comparison, Cases = cases, Captions = captions, Dialogs = sequence,
            SeparateFictitiousPlainSqliteFixtures = true, MainPackageTouched = false, NoFinancialReplayForMessageViews = true };
    }
}
