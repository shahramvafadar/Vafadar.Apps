using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Examines original read-only Settings controls at actual authorized OS scale, without invoking security or save actions.</summary>
    private static async Task<object> ReviewSettingsTextAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct, string? onlyContext = null)
    {
        var configuration = Platform.CurrentActivity?.Resources?.Configuration;
        if (configuration is null || Math.Abs(configuration.FontScale - 2f) > .001f || configuration.ScreenWidthDp is not (360 or 412))
            throw new ReviewCheckException("Settings review requires actual OS scale2.0 at360dp or412dp.");
        ValidateSurfaceContext(onlyContext);
        var path = Path.Combine(root, "settings-text.db"); var fresh = !File.Exists(path);
        if (configuration.ScreenWidthDp == 360 && (fresh || !File.Exists(Path.Combine(root, "settings-text-prepared.marker"))))
            throw new ReviewCheckException("Narrow continuation requires the original marked Settings fixture.");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>();
        if (fresh)
        {
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var first = new Account { Name = "Gemeinschaftskonto für laufende Haushaltsausgaben", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            await store.SaveAccountAsync(first, ct);
            await store.SaveAccountAsync(new Account { Name = "Fictitious separate savings account", CurrencyCode = "EUR", OpeningDate = first.OpeningDate }, ct);
            await store.UpdateSettingsAsync(value => { value.Mode = ExperienceMode.Advanced; value.DefaultAccountId = first.Id;
                value.ReminderDaysBefore = 365; value.ReminderTime = new(23, 59); value.EssentialEstimate = 123456;
                value.EssentialEstimateCurrency = "EUR"; }, ct);
            File.WriteAllText(Path.Combine(root, "settings-text-prepared.marker"), "Fictitious settings geometry only.");
        }
        if (!File.Exists(Path.Combine(root, "settings-text-prepared.marker"))) throw new ReviewCheckException("Incomplete settings fixture retained; never reseed.");
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct) });
        var before = await StoredAsync(); var observations = new List<object>();
        var localization = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell();
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            if (onlyContext is not null && !onlyContext.EndsWith("-" + choice, StringComparison.Ordinal)) continue;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            foreach (var language in new[] { "en", "fa", "de" })
            {
                if (onlyContext is not null && onlyContext != language + "-" + choice) continue;
                await SetLanguageAsync(app, localization, language, ct); await AwaitReimbursementFocusAsync(ct);
                var vm = ActivatorUtilities.CreateInstance<SettingsViewModel>(services, store);
                var page = new SettingsPage(vm, localization) { FlowDirection = localization.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Published(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
                { if (args.PropertyName == nameof(vm.Loading.IsReady) && vm.Loading.IsReady) ready.TrySetResult(); }
                vm.Loading.PropertyChanged += Published;
                try { await Shell.Current.Navigation.PushAsync(page, false); await ready.Task.WaitAsync(TimeSpan.FromSeconds(45), ct); }
                finally { vm.Loading.PropertyChanged -= Published; }
                // Presentation-only availability shows the existing security row without configuring or invoking authentication.
                vm.LockAvailable = true;
                await Task.Delay(100, ct);
                var failures = new List<object>(); var checkedLabels = 0; var checkedPickers = 0; var checkedEntries = 0;
                foreach (var control in ReimbursementViews(page).OfType<VisualElement>().Where(ReimbursementEffectivelyVisible).ToArray())
                {
                    string? expected = control switch { Label label when label.Text is not null && label.Text != "?" => label.Text,
                        Picker picker when picker.SelectedIndex >= 0 => picker.Items[picker.SelectedIndex], Entry entry => entry.Text, _ => null };
                    if (string.IsNullOrWhiteSpace(expected) || control.Handler?.PlatformView is not Android.Widget.TextView native) continue;
                    await AwaitReimbursementFocusAsync(ct);
                    if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original Settings page retired during review.");
                    native.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, native.Width, native.Height), true);
                    await Task.Delay(75, ct); await WaitLayoutAsync(native, ct); RequireHomeWindowFocus();
                    var issue = SettingsTextIssue(native);
                    if (issue is not null) failures.Add(new { Control = control.GetType().Name, Text = expected, Issue = issue,
                        Width = native.Width, Height = native.Height, NativeTextSize = native.TextSize, NativeScrollX = native.ScrollX,
                        NativeLines = native.Layout?.LineCount, FullNativeText = native.Text });
                    if (control is Label) checkedLabels++; else if (control is Picker) checkedPickers++; else checkedEntries++;
                }
                var popupRows = 0; var popupCancelProved = false;
                if (observations.Count == 0)
                    LogReport("settings-text-page", Path.GetFileName(root), new { Comparison = comparison, Language = language,
                        Theme = choice.ToString(), Labels = checkedLabels, Pickers = checkedPickers, Entries = checkedEntries, Failures = failures,
                        SourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId });
                // Stop at an actual page-geometry failure before spending or accepting unrelated modal checks.
                if (failures.Count == 0)
                {
                    var accountPicker = ReimbursementViews(page).OfType<Picker>().Single(value => ReferenceEquals(value.ItemsSource, vm.DefaultAccounts));
                    var selected = accountPicker.SelectedIndex;
                    if (accountPicker.Handler?.PlatformView is not Android.Widget.TextView pickerNative) throw new ReviewCheckException("Original Settings picker unavailable.");
                    pickerNative.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, pickerNative.Width, pickerNative.Height), true);
                    await Task.Delay(100, ct); RequireHomeWindowFocus(); pickerNative.PerformClick(); await Task.Delay(150, ct);
                    // Managed framework metadata is used only by this independent caller to inspect the original native dialog.
                    var field = typeof(Microsoft.Maui.Handlers.PickerHandler).GetField("_dialog", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field?.GetValue(accountPicker.Handler) is not AndroidX.AppCompat.App.AlertDialog dialog || dialog.ListView is not { } list)
                        throw new ReviewCheckException("Original native picker dialog unavailable.");
                    try
                    {
                        // Showing a native dialog schedules focus delivery; elapsed delay is never proof of focus.
                        while (dialog.IsShowing && dialog.Window?.DecorView.HasWindowFocus != true) await Task.Delay(50, ct);
                        if (!dialog.IsShowing) throw new ReviewCheckException("Original Settings dialog closed before focus.");
                        for (var index = 0; index < accountPicker.Items.Count; index++)
                        {
                            list.SetSelection(index); await Task.Delay(100, ct);
                            if (dialog.Window?.DecorView.HasWindowFocus != true) throw new ReviewCheckException("Original Settings popup lost focus.");
                            var row = list.GetChildAt(index - list.FirstVisiblePosition);
                            var text = SettingsNativeViews(row).OfType<Android.Widget.TextView>().FirstOrDefault(value => value.Text == accountPicker.Items[index]);
                            if (text is null) throw new ReviewCheckException("Original native picker row did not realize.");
                            var issue = SettingsTextIssue(text);
                            if (issue is not null) failures.Add(new { Control = "NativePickerRow", Text = text.Text, Issue = issue });
                            popupRows++;
                        }
                        if (dialog.Window?.DecorView.HasWindowFocus != true) throw new ReviewCheckException("Original Settings popup lost focus before Cancel.");
                        if (dialog.GetButton((int)Android.Content.DialogButtonType.Negative) is not { } cancel || !cancel.PerformClick())
                            throw new ReviewCheckException("Original native picker Cancel unavailable.");
                    }
                    finally { if (dialog.IsShowing) dialog.Dismiss(); }
                    await AwaitReimbursementFocusAsync(ct);
                    if (accountPicker.SelectedIndex != selected) throw new ReviewCheckException("Read-only Settings review changed stored preferences or selection.");
                    popupCancelProved = true;
                }
                if (before != await StoredAsync()) throw new ReviewCheckException("Read-only Settings review changed stored preferences.");
                observations.Add(new { Language = language, Theme = choice.ToString(), Labels = checkedLabels, Pickers = checkedPickers,
                    Entries = checkedEntries, PopupRows = popupRows, Failures = failures, OriginalPickerCancelRetainsSelection = popupCancelProved, PopupSkippedForPageGeometry = failures.Count > 0,
                    FixtureStoredInputsUnchanged = true, SyntheticSecurityAvailabilityOnly = true });
                await File.WriteAllTextAsync(Path.Combine(root, "settings-text-progress-" + comparison + ".json"), JsonSerializer.Serialize(observations), ct);
                await Shell.Current.Navigation.PopAsync(false);
            }
        }
        return new { Comparison = comparison, OsFontScale = configuration.FontScale, WidthDp = configuration.ScreenWidthDp,
            SourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId, Observations = observations, StoredInputsUnchanged = before == await StoredAsync(),
            OriginalWindowFocusRequired = true,
            MainPackageTouched = false, SecurityOrFinancialActionsInvoked = false, SystemSettingsWrittenByCaller = false };
    }

    /// <summary>Visits only native views in the original picker dialog.</summary>
    private static IEnumerable<Android.Views.View> SettingsNativeViews(Android.Views.View? view)
    {
        if (view is null) yield break;
        yield return view;
        if (view is Android.Views.ViewGroup group)
            for (var index = 0; index < group.ChildCount; index++) foreach (var child in SettingsNativeViews(group.GetChildAt(index))) yield return child;
    }

    /// <summary>Reports actual native allocation/ellipsis/viewport failures, preserving original text and OS scaling.</summary>
    private static string? SettingsTextIssue(Android.Widget.TextView native)
    {
        if (native.Layout is not { } layout || native.Width <= 0 || native.Height <= 0) return "no-native-layout";
        var visible = new Android.Graphics.Rect();
        if (!native.GetGlobalVisibleRect(visible) || visible.Width() + 1 < native.Width || visible.Height() + 1 < native.Height) return "outside-visible-viewport";
        if (layout.Height > native.Height - native.TotalPaddingTop - native.TotalPaddingBottom + 1) return "vertical-text-clipping";
        for (var line = 0; line < layout.LineCount; line++)
            if (layout.GetEllipsisCount(line) != 0 || layout.GetLineRight(line) - native.ScrollX > native.Width - native.TotalPaddingLeft - native.TotalPaddingRight + 1
                || layout.GetLineLeft(line) - native.ScrollX < -1) return "horizontal-text-clipping";
        return null;
    }
}
