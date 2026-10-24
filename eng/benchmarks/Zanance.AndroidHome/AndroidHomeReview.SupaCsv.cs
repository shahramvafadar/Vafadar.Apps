using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks new original SUPA review/consent controls without an import or another caller's data.</summary>
    private static async Task<object> ReviewSupaCsvAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var local = services.GetRequiredService<ILocalizationService>(); var languageBefore = local.CurrentLanguage;
        var theme = services.GetRequiredService<ThemeService>(); var themeBefore = theme.Choice;
        var translator = services.GetRequiredService<Translator>();
        var baseRows = await ImportFinancialNativeReview.StoredAsync(services, ct);
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(Path.Combine(root, "supa.db")).BuildServiceProvider();
        var setup = Path.Combine(root, "supa-original.json");
        var store = fixture.GetRequiredService<ZananceStore>();
        if (!File.Exists(setup))
        {
            if (File.Exists(Path.Combine(root, "supa.db"))) throw new ReviewCheckException("Preserve interrupted original SUPA setup.");
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            await store.EnsureDefaultCategoriesAsync(ct);
            var account = new Account { Name = "Fictitious SUPA target", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
            await store.SaveAccountAsync(account, ct);
            await store.UpdateSettingsAsync(value => { value.Mode = ExperienceMode.Advanced; value.OnboardingCompleted = true; value.DefaultAccountId = account.Id; }, ct);
            await File.WriteAllTextAsync(setup, await ImportFinancialNativeReview.StoredAsync(fixture, ct), ct);
        }
        var before = await File.ReadAllTextAsync(setup, ct);
        if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new ReviewCheckException("Original SUPA rows changed.");
        var observations = new List<JsonElement>();
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                var key = language + "-" + choice; var receipt = Path.Combine(root, "supa-" + key + ".json");
                if (File.Exists(receipt))
                {
                    using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                    if (!retained.RootElement.GetProperty("RowsUnchanged").GetBoolean()) throw new ReviewCheckException("Retained source mismatch.");
                    observations.Add(retained.RootElement.Clone()); continue;
                }
                await AwaitReimbursementFocusAsync(ct); theme.Set(choice); local.SetLanguage(local.SupportedLanguages.Single(value => value.CultureName == language));
                await Task.Delay(150, ct); await AwaitRootHomeAsync(ct);
                var files = new SupaNativeFiles();
                var model = new ImportExportViewModel(store, translator, new DateFormatter(local), local, services.GetRequiredService<TimeProvider>(), fixture.GetRequiredService<HoldingStore>(), files, services.GetRequiredService<IAppInteraction>());
                await model.LoadAsync(); var page = new ImportExportPage(model).WithFlowDirection(local);
                await Shell.Current.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
                try
                {
                    await PlanEditorNativeReview.WaitAsync(() => page.IsLoaded && !model.IsBusy, ct);
                    var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(value => value.Orientation == ScrollOrientation.Vertical);
                    var help = ReimbursementViews(page).OfType<Vafadar.Maui.Controls.HelpButton>().Single(value => value.Topic == "SupaCsv");
                    var helpButton = ReimbursementViews(help).OfType<Button>().Single();
                    await scroll.ScrollToAsync(helpButton, ScrollToPosition.Center, false); await Task.Delay(100, ct);
                    if (helpButton.Handler?.PlatformView is not Android.Widget.Button nativeHelp || !nativeHelp.IsShown || !nativeHelp.HasWindowFocus || !nativeHelp.PerformClick())
                        throw new ReviewCheckException("Original SUPA help unavailable.");
                    var helpBody = translator["Help_SupaCsv_Text"] + Environment.NewLine + Environment.NewLine + translator.Format("Help_Example", translator["Help_SupaCsv_Example"]);
                    var helpLines = await ReadSupaHelpAsync(helpBody, translator["Common_Ok"], ct);
                    if (files.Picks != 0) throw new ReviewCheckException("Help opened a source.");
                    var pick = ReimbursementViews(page).OfType<WrappingAction>().Single(value => ReferenceEquals(value.Command, model.PickSupaCommand));
                    var pickButton = ReimbursementViews(pick).OfType<Button>().Single();
                    await scroll.ScrollToAsync(pickButton, ScrollToPosition.Center, false); await Task.Delay(100, ct);
                    if (pickButton.Handler?.PlatformView is not Android.Widget.Button nativePick || !nativePick.IsShown || !nativePick.HasWindowFocus || !nativePick.PerformClick())
                        throw new ReviewCheckException("Original focused SUPA picker unavailable.");
                    if (model.PickSupaCommand.ExecutionTask is { } task) await task;
                    if (!model.HasPreview || model.ImportCount != 2 || model.CanRunImport || files.Picks != 1) throw new ReviewCheckException("Original SUPA preview/consent differs.");
                    var geometry = new List<object>();
                    var review = page.FindByName<VerticalStackLayout>("BankPresetReview");
                    foreach (var caption in new[] { model.BankSourceText, model.BankPresetWarning, model.BankPresetAcknowledgement })
                    {
                        // The common warning also repeats below the preset. Bind this observation to its original review owner.
                        var label = ReimbursementViews(review).OfType<Label>().Single(value => value.Text == caption);
                        await scroll.ScrollToAsync(label, ScrollToPosition.Center, false); await Task.Delay(100, ct);
                        if (label.Handler?.PlatformView is not Android.Widget.TextView native) throw new ReviewCheckException("Native SUPA caption missing.");
                        await WaitLayoutAsync(native, ct); using var visible = new Android.Graphics.Rect(); var layout = native.Layout;
                        var expected = label.FlowDirection == FlowDirection.LeftToRight ? label.Text : NativeDigits.Apply(label.Text);
                        if (!native.IsShown || !native.HasWindowFocus || native.Text != expected || !native.GetGlobalVisibleRect(visible) || layout is null || layout.LineCount == 0
                            || layout.GetLineEnd(layout.LineCount - 1) != expected?.Length
                            || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0)
                            || native.Height - native.TotalPaddingTop - native.TotalPaddingBottom < layout.Height
                            || visible.Height() < native.Height - 2 || visible.Width() < native.Width - 2)
                            throw new ReviewCheckException("Original SUPA caption is clipped.");
                        geometry.Add(new { Text = native.Text, native.Width, native.Height, Lines = layout.LineCount, VisibleWidth = visible.Width(), VisibleHeight = visible.Height() });
                    }
                    var consent = ReimbursementViews(page).OfType<Switch>().Single(value => SemanticProperties.GetDescription(value) == model.BankPresetAcknowledgement);
                    await scroll.ScrollToAsync(consent, ScrollToPosition.Center, false); await Task.Delay(100, ct);
                    if (consent.Handler?.PlatformView is not Android.Widget.CompoundButton toggle || !toggle.IsShown || !toggle.HasWindowFocus || !toggle.Enabled
                        || toggle.Checked || model.BankAcknowledged)
                        throw new ReviewCheckException("Original SUPA consent unavailable.");
                    // D-516: CompoundButton changes state before returning whether a click listener handled it.
                    // A false return must never trigger a second click that would undo the original consent.
                    var consentClickHandled = toggle.PerformClick();
                    await PlanEditorNativeReview.WaitAsync(() => toggle.Checked && model.BankAcknowledged && model.CanRunImport, ct);
                    if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new ReviewCheckException("Presentation wrote financial rows.");
                    var result = new { Context = key, Comparison = comparison, Geometry = geometry, Help = helpBody, HelpLines = helpLines, ActualPageWidth = page.Width, NativePicker = true, NativeConsent = toggle.Checked,
                        ConsentClickListenerHandled = consentClickHandled, RowsUnchanged = true, FinancialWrites = 0, ExternalPickerAcceptance = false };
                    await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result), ct); observations.Add(JsonSerializer.SerializeToElement(result));
                    LogReport("supa-context", Path.GetFileName(root), result);
                }
                finally { await Shell.Current.Navigation.PopModalAsync(false); await AwaitRootHomeAsync(ct); }
            }
        }
        finally { local.SetLanguage(languageBefore); theme.Set(themeBefore); }
        if (baseRows != await ImportFinancialNativeReview.StoredAsync(services, ct) || before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new ReviewCheckException("Original/base source changed.");
        return new { Observations = observations, RowsUnchanged = true, FinancialWrites = 0, MainTouched = false };
    }

    /// <summary>Reads the original focused help body, reaches its example and invokes its owned OK button.</summary>
    private static async Task<int> ReadSupaHelpAsync(string expected, string ok, CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new ReviewCheckException("Native window inspection requires API29; this is not API24 qualification.");
        IEnumerable<Android.Views.View> Walk(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++) if (group.GetChildAt(i) is { } child)
                    foreach (var nested in Walk(child)) yield return nested;
        }
        Android.Widget.TextView? body = null; Android.Widget.Button? close = null;
        for (var attempt = 0; attempt < 150 && (body is null || close is null); attempt++)
        {
            var nodes = Android.Views.Inspectors.WindowInspector.GlobalWindowViews.Where(view => view.Context?.PackageName == Package && view.HasWindowFocus && view.IsShown)
                .SelectMany(Walk).ToArray();
            body = nodes.OfType<Android.Widget.TextView>().FirstOrDefault(view => view.Text == expected && view.Layout is not null);
            close = nodes.OfType<Android.Widget.Button>().FirstOrDefault(view => view.Text == ok && view.IsShown && view.Enabled);
            if (body is null || close is null) await Task.Delay(40, ct);
        }
        var layout = body?.Layout;
        if (body is null || close is null || layout is null || layout.LineCount == 0
            || layout.GetLineEnd(layout.LineCount - 1) != expected.Length || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
            throw new ReviewCheckException("Complete original SUPA help not realized.");
        for (Android.Views.IViewParent? parent = body.Parent; parent is not null; parent = parent.Parent)
            if (parent is Android.Widget.ScrollView viewport)
            {
                viewport.FullScroll(Android.Views.FocusSearchDirection.Down); await Task.Delay(100, ct);
                if (viewport.CanScrollVertically(1)) throw new ReviewCheckException("Original help example unreachable.");
                break;
            }
        if (!close.HasWindowFocus || !close.PerformClick()) throw new ReviewCheckException("Original help OK refused.");
        await Task.Delay(100, ct); return layout.LineCount;
    }

    /// <summary>Supplies only a fictitious original CSV through the production selected-file port.</summary>
    private sealed class SupaNativeFiles : IDataFileInteraction
    {
        internal int Picks;
        public Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding)
        {
            if (encoding != BankCsvEncoding.Utf8) throw new ReviewCheckException("SUPA requires UTF-8.");
            Picks++;
            return Task.FromResult<PickedDataFile?>(new("fictitious-supa.csv", "Id,AcctId,BookgDt,ValDt,Amt,AmtCcy,CdtDbtInd,BookgTxt\r\noriginal-1,supa-original,2026-10-24,2026-10-25,100.00,EUR,CRDT,Original income\r\noriginal-2,supa-original,2026-10-24,2026-10-25,12.34,EUR,DBIT,Original spending\r\n"));
        }
        public Task<PickedDataFile?> PickCsvAsync(string title) => throw new ReviewCheckException("Generic source outside SUPA review.");
        public Task<bool> ConfirmExportAsync(string reason) => throw new ReviewCheckException("No export.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new ReviewCheckException("No share.");
    }
}
