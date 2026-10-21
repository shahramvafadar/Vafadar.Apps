#if HOUSEHOLD_MONEY_REVIEW
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Public counts/stages only; native errors never expose messages, credentials or connection text.</summary>
    private static readonly Dictionary<string, object?> SecondaryHouseholdEvidence = [];
    /// <summary>Uses the original protected fictional household membership and native cost pages, without financial Save or network authority.</summary>
    private static async Task<object> ReviewSecondaryHouseholdAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        SecondaryHouseholdEvidence.Clear(); SecondaryHouseholdEvidence["Phase"] = "original-window";
        if (AppInfo.Current.PackageName is not ("pro.vafadar.zanance.householdlifecycle" or "pro.vafadar.zanance.householdmoneyreview")) throw new ReviewCheckException("Original retained household package required.");
        var osReview = Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_household_os", false) == true;
        var width = Platform.CurrentActivity?.Intent?.GetIntExtra("review_os_width", 0) ?? 0;
        if (osReview)
        {
            var resources = Android.App.Application.Context.Resources;
            if (Package != "pro.vafadar.zanance.householdmoneyreview" || width is not (360 or 412)
                || Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) != true
                || Math.Abs((resources?.Configuration?.FontScale ?? 0) - 2f) > 0.01f
                || resources?.DisplayMetrics is not { Density: > 0 } metrics || Math.Abs(metrics.WidthPixels / metrics.Density - width) > 1
                || !File.Exists(Path.Combine(folder, "fresh-household-admitted.json"))
                || !File.Exists(Path.Combine(folder, "secondary-household-prepared.json"))
                || !File.Exists(Path.Combine(folder, "navigation-only", "prepared.json"))
                || (from name in new[] { "en", "fa", "de" } from themeCaption in new[] { "Light", "Dark" }
                    from scene in new[] { "shared-cost-original", "shared-cost-unit-feedback", "shared-repayment-unit-feedback" }
                    select Path.Combine(folder, name + "-" + themeCaption + "-" + scene + ".json")).Any(file => !File.Exists(file)))
                throw new ReviewCheckException("Complete original protected source and actual OS200% household width required.");
            // D-444: visual qualification cannot renew transport identity or perform fresh household enrollment.
            services.GetRequiredService<Vafadar.Zanance.App.Features.Sync.PersonalSyncForegroundWorker>().Dispose();
            await AwaitOriginalMoneyWindowAsync(ct);
        }
        else await AwaitReimbursementFocusAsync(ct);
        var appLock = services.GetRequiredService<Vafadar.Zanance.App.Security.AppLockService>();
        if (appLock.IsLocked) throw new ReviewCheckException("Original native app access must already be unlocked; qualification never authenticates or removes a cover.");
        app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        if (Shell.Current.CurrentPage is HomePage initialHome) await initialHome.PendingPublication.WaitAsync(ct);
        #if HOUSEHOLD_MONEY_FRESH
        if (!osReview) await PrepareFreshHouseholdMoneyAsync(services, folder, ct);
#endif
        var profiles = services.GetRequiredService<ProfileService>(); var originalProfile = profiles.Current; var catalogue = profiles.All.ToArray();
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var originalSettings = JsonSerializer.SerializeToElement(await services.GetRequiredService<ZananceStore>().GetSettingsAsync(ct));
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var culture = local.FormattingCultureName; var choice = theme.Choice;
        // The second-owner device was explicitly revoked by the original D-294 removal case.
        // Use the separately recovered original device, preserving both histories and their actual permissions.
        #if HOUSEHOLD_MONEY_FRESH
        var target = profiles.Current;
#else
        var target = profiles.All.Single(row => row.Name == "Fictitious household recovered");
#endif
        try
        {
            if (profiles.Current.Id != target.Id && !await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), target)) throw new ReviewCheckException("Original fictional profile switch refused.");
            // Republish the original isolated review root after switching storage. Do not alter restored onboarding preferences.
            app.OpenReviewShell();
            await AwaitRootHomeAsync(ct);
            if (Shell.Current.CurrentPage is HomePage targetHome) await targetHome.PendingPublication.WaitAsync(ct);
            if (appLock.IsLocked) throw new ReviewCheckException("Original target app access must already be unlocked.");
            SecondaryHouseholdEvidence["Phase"] = "original-member-read";
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            var authority = await services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
            SecondaryHouseholdEvidence["Protected"] = location.IsProtected; SecondaryHouseholdEvidence["CanWrite"] = authority.CanWrite;
            SecondaryHouseholdEvidence["Costs"] = authority.Costs.Count; SecondaryHouseholdEvidence["Repayments"] = authority.Settlements.Count;
            SecondaryHouseholdEvidence["Contacts"] = authority.Contacts.Count;
            if (!location.IsProtected || !authority.CanWrite || authority.Costs.Count == 0 || authority.Contacts.Count < 2)
                throw new ReviewCheckException("Original protected membership and existing financial graph required.");
            SecondaryHouseholdEvidence["Phase"] = "original-source-checkpoint";
            var before = await ImportFinancialNativeReview.StoredAsync(services, ct);
            var prepared = Path.Combine(folder, "secondary-household-prepared.json");
            var resume = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) == true;
            var checkpointExists = File.Exists(prepared);
            var navigationExists = Directory.Exists(Path.Combine(folder, "navigation-only"));
            LogReport("secondary-household-preflight", comparison, new { Phase = "original-protected-member", location.IsProtected,
                authority.CanWrite, Costs = authority.Costs.Count, Repayments = authority.Settlements.Count, Contacts = authority.Contacts.Count,
                OriginalCheckpointExists = checkpointExists, OriginalNavigationExists = navigationExists, NativeAppUnlocked = !appLock.IsLocked });
            if ((checkpointExists || resume) && (!checkpointExists || before != await File.ReadAllTextAsync(prepared, ct))) throw new ReviewCheckException("Original protected household checkpoint differs.");
            if (!checkpointExists)
            {
                await using var packet = new FileStream(prepared, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                await packet.WriteAsync(System.Text.Encoding.UTF8.GetBytes(before), ct);
            }
            using var original = resume ? await PlanEditorNativeReview.ContinueEmptyAsync(services, Path.Combine(folder, "navigation-only"), ct)
                : await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "navigation-only"), ct);
            void Validate()
            {
                ct.ThrowIfCancellationRequested();
                if (appLock.IsLocked || profiles.Current.Id != target.Id || Platform.CurrentActivity?.HasWindowFocus != true || AppInfo.Current.PackageName != Package)
                    throw new ReviewCheckException("Original native foreground/profile lifetime required.");
            }
            bool Completed(string stage) => File.Exists(Path.Combine(folder, stage + ".json"));
            async Task Focused()
            {
                while ((Shell.Current?.CurrentPage ?? app.Windows[0].Page)?.Handler?.PlatformView is not Android.Views.View
                    { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } view || view.Context?.PackageName != Package) await Task.Delay(100, ct);
            }
        static bool Visible(Element item)
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
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(target, ScrollToPosition.MakeVisible, false); await Task.Delay(30, ct); return; }
        }
        static IEnumerable<Android.Views.View> NativeNodes(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++)
                    if (group.GetChildAt(i) is { } child) foreach (var nested in NativeNodes(child)) yield return nested;
        }
        async Task Invoke(Page page, System.Windows.Input.ICommand command, bool? consent)
        {
            if (osReview && (page.BindingContext is not Features.Sharing.HouseholdCostsViewModel model
                || !ReferenceEquals(command, model.SaveCommand) || model.AmountText != "1,234" || !model.HasEditor || !model.CanWrite
                || !Vafadar.Zanance.Core.Money.DisplayUnits.All.Any(unit => unit.CurrencyCode == model.CurrencyCode && unit.Exponent == 2)))
                throw new ReviewCheckException("Only the original changed-unit household refusal belongs to OS geometry.");
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(item => Visible(item) && ReferenceEquals(item.Command, command));
            await Scroll(button);
            await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            if (!Visible(button) || button.Handler?.PlatformView is not Android.Widget.Button native || !native.PerformClick()) throw new ReviewCheckException("Actual original money Save unavailable.");
            await Task.Yield();
            if (consent is { } answer)
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new ReviewCheckException("Original account dialog inspection requires API 29.");
                var caption = services.GetRequiredService<Translator>()[answer ? "Reconcile_Adjust" : "Common_Cancel"];
                Android.Widget.Button? reply = null;
                await PlanEditorNativeReview.WaitAsync(() =>
                {
                    if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return false;
                    reply = Android.Views.Inspectors.WindowInspector.GlobalWindowViews
                        .Where(window => window.Context?.PackageName == Package && window.IsShown && window.HasWindowFocus)
                        .SelectMany(NativeNodes).OfType<Android.Widget.Button>()
                        .SingleOrDefault(button => button.IsShown && button.Enabled && button.Text == caption);
                    return reply is not null;
                }, ct);
                if (reply?.HasWindowFocus != true || !reply.PerformClick()) throw new ReviewCheckException("Original account confirmation target retired.");
                LogReport("secondary-household-native-consent", comparison, new { Caption = caption, Positive = answer, ProcessId = Environment.ProcessId });
            }
        }
        async Task Observe(Page page, string stage)
        {
            if (resume && Completed(stage)) return;
            await Focused(); await Task.Delay(150, ct); var readings = new List<object>();
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondaryHouseholdCaptions(page).Contains(text)))
            {
                await Scroll(label);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { Layout: { } layout } text || Vafadar.Core.Text.Digits.ToAscii(text.Text ?? string.Empty) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty)
                    || layout.LineCount == 0 || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    var native = label.Handler?.PlatformView as Android.Widget.TextView;
                    var failure = new { Stage = stage, Label = label.Text, Native = native?.Text, Width = native?.Width, Height = native?.Height,
                        Lines = native?.Layout?.LineCount, Ellipses = native?.Layout is { } current ? Enumerable.Range(0, current.LineCount).Select(current.GetEllipsisCount).ToArray() : [] };
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-failed-native.json"), JsonSerializer.Serialize(failure), ct);
                    LogReport("secondary-household-failed-native", comparison, failure);
                    throw new ReviewCheckException("Original monetary caption is incomplete.");
                }
                if (osReview) MonetaryOsScope.RequireCompleteHeight(text, layout);
                // The actual label presenter applies the selected native digit shapes without changing punctuation or money.
                readings.Add(new { label.Text, NativeText = text.Text, text.Width, text.Height, Lines = layout.LineCount });
            }
            foreach (var entry in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondaryHouseholdRaw(page).Contains(text)))
            {
                await Scroll(entry);
                if (entry.Handler?.PlatformView is not Android.Widget.EditText { IsShown: true, HasWindowFocus: true } input || input.Text != entry.Text)
                {
                    var native = entry.Handler?.PlatformView as Android.Widget.EditText;
                    var failure = new { Stage = stage, Raw = entry.Text, Native = native?.Text, Visible = native?.IsShown, Focused = native?.HasWindowFocus };
                    LogReport("secondary-household-failed-native", comparison, failure);
                    throw new ReviewCheckException("Original monetary raw input differs.");
                }
                readings.Add(new { entry.Text, input.Width, input.Height });
            }
            var row = new { Stage = stage, ProcessId = Environment.ProcessId, NativeReadings = readings };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(row), ct);
            original.Observations.Add(row); LogReport("secondary-household-observation", comparison, row);
        }

            foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                SecondaryHouseholdEvidence["Phase"] = name + "-" + value;
                await original.SecondaryHouseholdContextAsync(services, Validate, (osReview ? "os200-" + width + "-" : "") + name + "-" + value, Invoke, Observe, ct, resume ? Completed : null, freshRepayment: authority.Settlements.Count == 0);
            }
            if (before != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new ReviewCheckException("Original protected household rows changed.");
            return new { Comparison = comparison, Scope = osReview ? "household-os-" + width : null, NoEnrollmentOrAuthorityRefresh = osReview, OriginalProtectedRowsUnchanged = true, OriginalMembershipAuthority = true,
                AcceptedFinancialCommands = 0, NoAcceptedCommandReplayed = true, NativePresentationOnly = true, Observations = original.Observations, Device = DeviceEvidence() };
        }
        finally
        {
            // Native display/profile restoration uses production APIs; no OS setting or credential is changed.
            await SetSecondaryHouseholdThemeAsync(app, theme, choice, ct);
            await SetLanguageAsync(app, local, language.CultureName, ct);
            if (local.FormattingCultureName != culture)
            {
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
                app.ShellRebuilt += Rebuilt;
                try { local.SetFormattingCulture(culture); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                finally { app.ShellRebuilt -= Rebuilt; }
            }
            if (profiles.Current.Id != originalProfile.Id && !await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), originalProfile)) throw new ReviewCheckException("Original fictional profile restoration refused.");
            // Republish the original isolated review root after switching storage. Do not alter restored onboarding preferences.
            app.OpenReviewShell();
            await AwaitRootHomeAsync(ct);
            if (Shell.Current.CurrentPage is HomePage restoredHome) await restoredHome.PendingPublication.WaitAsync(ct);
            var restored = await ImportFinancialNativeReview.StoredAsync(services, ct);
            if (!profiles.All.SequenceEqual(catalogue) || baseline != restored)
            {
                var originalTables = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(baseline)!;
                var restoredTables = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(restored)!;
                var restoredSettings = JsonSerializer.SerializeToElement(await services.GetRequiredService<ZananceStore>().GetSettingsAsync(ct));
                SecondaryHouseholdEvidence["ChangedBaseSettingProperties"] = originalSettings.EnumerateObject().Where(property =>
                    !restoredSettings.TryGetProperty(property.Name, out var value) || value.GetRawText() != property.Value.GetRawText()).Select(property => property.Name).ToArray();
                SecondaryHouseholdEvidence["ChangedBaseTables"] = originalTables.Keys.Union(restoredTables.Keys).Where(key =>
                    !originalTables.TryGetValue(key, out var left) || !restoredTables.TryGetValue(key, out var right) || !left.SequenceEqual(right)).ToArray();
                throw new ReviewCheckException("Original base profile or catalogue changed.");
            }
        }
    }
    /// <summary>Waits for the exact original shell rebuild before restoring another application display choice.</summary>
    private static async Task SetSecondaryHouseholdThemeAsync(App app, ThemeService theme, ThemeChoice value, CancellationToken ct)
    {
        if (theme.Choice == value) return;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
        app.ShellRebuilt += Rebuilt;
        try { theme.Set(value); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
        finally { app.ShellRebuilt -= Rebuilt; }
    }
}
#endif
