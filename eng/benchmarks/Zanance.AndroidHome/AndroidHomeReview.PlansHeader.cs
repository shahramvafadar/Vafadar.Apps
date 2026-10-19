using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks only the original Plans header and actual Add/Cancel in six Release/AOT contexts.</summary>
    private static async Task<object> ReviewPlansHeaderAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        if ((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0)
            await store.SaveAccountAsync(new Account { Name = "D395 independent header account", CurrencyCode = "EUR",
                Type = AccountType.Checking, OpeningDate = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime), OpeningBalance = 0 }, ct);
        async Task<string> Stored() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct), Plans = await plans.GetSchedulesAsync(ct) });
        var before = await Stored(); var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>(); var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage.CultureName;
        var observations = new List<object>(); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        async Task UntilAsync(Func<bool> ready, CancellationToken cancellation)
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                if (ready()) return;
                await Task.Delay(50, cancellation);
            }
            throw new ReviewCheckException("Original Plans navigation did not publish.");
        }
        void Invoke(Button button)
        {
            if (button.Handler?.PlatformView is not Android.Widget.Button native || native.Context?.PackageName != Package
                || !native.IsShown || !native.HasWindowFocus || !native.Enabled || !native.PerformClick())
                throw new ReviewCheckException("Original native Plans action refused.");
        }
        try
        {
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await Shell.Current.GoToAsync("//home");
                if (themes.Choice != theme)
                {
                    var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                    app.ShellRebuilt += Rebuilt;
                    try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                    finally { app.ShellRebuilt -= Rebuilt; }
                }
                await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
                await Shell.Current.GoToAsync("//plans");
                await UntilAsync(() => Shell.Current.CurrentPage is PlansPage { IsLoaded: true }, ct);
                var page = Shell.Current.CurrentPage as PlansPage ?? throw new ReviewCheckException("Original Plans required.");
                await AwaitReimbursementFocusAsync(ct);
                var header = await Diagnostics.PlansAddHeaderReview.CheckAsync(page, ct);
                Invoke(page.FindByName<Button>("AddAction"));
                PlanEditorPage? editor = null;
                await UntilAsync(() => { editor = Shell.Current.Navigation.ModalStack.OfType<PlanEditorPage>().LastOrDefault()
                    ?? Shell.Current.CurrentPage as PlanEditorPage; return editor?.BindingContext is PlanEditorViewModel { CanInteract: true }; }, ct);
                var model = (PlanEditorViewModel)editor!.BindingContext;
                var cancel = ReimbursementViews(editor).OfType<Button>().Single(button => ReferenceEquals(button.Command, model.CancelCommand));
                // A ready editor model can precede native modal attachment; await its actual original target.
                await UntilAsync(() => cancel.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true }, ct);
                if (cancel.Handler?.PlatformView is not Android.Views.View nativeCancel) throw new ReviewCheckException("Original Cancel required.");
                await WaitLayoutAsync(nativeCancel, ct); Invoke(cancel);
                await UntilAsync(() => ReferenceEquals(Shell.Current.CurrentPage, page)
                    && !Shell.Current.Navigation.ModalStack.Contains(editor), ct);
                await page.PendingPublication.WaitAsync(ct);
                if (before != await Stored()) throw new ReviewCheckException("Header Add/Cancel changed original financial inputs.");
                var observation = new { Language = language, Theme = theme.ToString(), header, OriginalNativeAddAndCancel = true,
                    StoredInputsUnchanged = true, Returned = await Diagnostics.PlansAddHeaderReview.CheckAsync(page, ct) };
                observations.Add(observation);
                await File.WriteAllTextAsync(Path.Combine(root, language + "-" + theme + "-plans-header.json"), JsonSerializer.Serialize(observation), ct);
            }
        }
        finally { await Shell.Current.GoToAsync("//home"); themes.Set(oldTheme); await SetLanguageAsync(app, local, oldLanguage, ct); }
        return new { Comparison = comparison, Contexts = observations, OriginalStoredInputsUnchanged = before == await Stored(), OriginalThemeAndLanguageRestored = true,
            MainPackageTouched = false, Device = DeviceEvidence() };
    }
}
