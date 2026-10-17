#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Actual aggregate page and native Cancel, independent fictitious finance/effects; no provider/network/credit operation.</summary>
    private static async Task ReviewAiAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.aireview" || Path.GetFullPath(folder) !=
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "ai-native-d298"))) throw new IOException("Exact independent analysis review required.");
        var retainedTermsOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_AI_TERMS") == "1";
        var complete = Path.Combine(folder, retainedTermsOnly ? "retained-terms-complete.json" : "complete.json"); if (File.Exists(complete)) throw new IOException("Completed analysis cases must not be replayed.");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(Path.Combine(folder, "analysis-fixture.db")).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>(); await store.EnsureDefaultCategoriesAsync(); await store.GetSettingsAsync();
        if ((await store.GetAccountsAsync()).Count == 0)
        {
            var account = new Account { Name = "Fictitious original / نمونه / Beispielkonto", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
            await store.SaveAccountAsync(account);
            await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Date = DateOnly.FromDateTime(DateTime.Today), Kind = EntryKind.Income, Amount = 123456, Review = ReviewState.Confirmed });
        }
        var before = await HouseholdFinancialFingerprintAsync(fixture);
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0]; var progress = Path.Combine(folder, retainedTermsOnly ? "retained-terms-cases.json" : "cases.json");
        var finished = File.Exists(progress) ? JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progress))! : [];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            // Original selection/dialog geometry already passed18 contexts. Only changed historical text needs these six narrow native reads.
            if (retainedTermsOnly && width != 360) continue;
            var key = (retainedTermsOnly ? "retained-terms-" : "") + $"{language}-{theme}-{width}"; if (finished.Contains(key)) continue;
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var effects = new AnalysisLayoutRuntime();
            var model = new AiAnalysisViewModel(fixture.GetRequiredService<AiSnapshotStore>(), text, local, services.GetRequiredService<IDateFormatter>(),
                TimeProvider.System, services.GetRequiredService<IAppInteraction>(), effects);
            var page = new AiAnalysisPage(model).WithFlowDirection(local); var root = window.Page ?? throw new IOException();
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
            try
            {
                for (var wait = 0; wait < 200 && !model.Loaded; wait++) await Task.Delay(25);
                if (!model.Loaded || model.Accounts.Count != 1 || model.Accounts.Any(a => a.Selected)) throw new IOException("Original account choice was not explicit.");
                var reads = new List<object>(); var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
                if (!retainedTermsOnly)
                {
                var box = VisualDescendants(page).OfType<CheckBox>().Single(); await ScrollToViewIfNeededAsync(scroll, box);
                if (box.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.CheckBox nativeBox ||
                    FrameworkElementAutomationPeer.CreatePeerForElement(nativeBox)?.GetPattern(PatternInterface.Toggle) is not IToggleProvider toggle)
                    throw new IOException("Original analysis selection has no native toggle.");
                toggle.Toggle(); await Task.Delay(60); await Action(model.ReviewCommand);
                if (model.Disclosure is null || model.Disclosure.Contains("Fictitious original", StringComparison.Ordinal)) throw new IOException("Minimal original disclosure failed.");
                await CaptureWindowAsync(window, Path.Combine(folder, key + "-source.png"));
                await Cancel(model.ShareCommand, "manual-cancel");
                }
                await Action(model.CheckProviderCommand); if (!retainedTermsOnly) await Action(model.QuoteCommand);
                if ((!retainedTermsOnly && !model.CanSend) || model.ResultSnapshot is null || model.ResultText is null) throw new IOException("Fictitious metadata/read-only result not displayed.");
                if (retainedTermsOnly && (!model.ResultSnapshot.Contains(effects.Destination, StringComparison.Ordinal) || !model.ResultSnapshot.Contains("\"MaximumUnits\":8", StringComparison.Ordinal) || !model.ResultSnapshot.Contains("fixture-v1", StringComparison.Ordinal)))
                    throw new IOException("Original retained destination/model/max consent is missing.");
                var index = 0;
                foreach (var editor in VisualDescendants(page).OfType<Editor>().Where(Visible))
                {
                    await ScrollToViewIfNeededAsync(scroll, editor); await Task.Delay(50);
                    if (editor.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox observed)
                        await File.WriteAllTextAsync(Path.Combine(folder, $"{key}-editor-{index}.json"), JsonSerializer.Serialize(new { editor.IsReadOnly, editor.Text,
                            NativeText = observed.Text, NativeReadOnly = observed.IsReadOnly, observed.AcceptsReturn }));
                    // Native WinUI text boxes canonicalize line endings. Compare the actual complete text after that platform normalization only.
                    if (!editor.IsReadOnly || editor.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox native ||
                        Lines(native.Text) != Lines(editor.Text ?? "") || !native.IsReadOnly || native.AcceptsReturn != true)
                        throw new IOException("Original analysis packet/result is not complete native read-only text.");
                    reads.Add(native); await CaptureWindowAsync(window, Path.Combine(folder, $"{key}-readonly-{index++}.png"));
                }
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(l => Visible(l) && l.FontAutoScalingEnabled && !string.IsNullOrEmpty(l.Text)))
                { await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(20); CheckCaption(label, reads); }
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>().Where(Visible))
                    if (VisualDescendants(action).OfType<Button>().Single().Handler?.PlatformView is not NativeButton { ActualWidth: >= 44, ActualHeight: >= 44 })
                        throw new IOException("Analysis action target is smaller than44.");
                if (!retainedTermsOnly) await Cancel(model.SendCommand, "send-cancel");
                if (effects.Shares != 0 || effects.Submissions != 0 || before != await HouseholdFinancialFingerprintAsync(fixture)) throw new IOException("Native Cancel changed finance or disclosed data.");
                finished.Add(key); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(finished));
                await File.WriteAllTextAsync(Path.Combine(folder, key + "-proof.json"), JsonSerializer.Serialize(new { NativeReads = reads.Count, FinancialHash = before,
                    effects.Shares, effects.Submissions, RealNativeSelectionAndCancelThisStage = !retainedTermsOnly,
                    OriginalNativeSelectionAndCancelPreviouslyQualified = retainedTermsOnly, FictitiousProviderEffects = true, RealHttpOrModel = false }));

                async Task Action(System.Windows.Input.ICommand command)
                {
                    var target = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, command));
                    await ScrollToViewIfNeededAsync(scroll, target); InvokeChoiceReview(VisualDescendants(target).OfType<Button>().Single(), reads);
                    await Task.Delay(50); for (var wait = 0; wait < 200 && model.IsBusy; wait++) await Task.Delay(25);
                }
                async Task Cancel(System.Windows.Input.ICommand command, string caption)
                {
                    var target = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, command));
                    await ScrollToViewIfNeededAsync(scroll, target); InvokeChoiceReview(VisualDescendants(target).OfType<Button>().Single(), reads);
                    if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException();
                    NativeButton? cancel = null;
                    for (var wait = 0; wait < 200 && cancel is null; wait++)
                    {
                        cancel = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => Nodes(p.Child)).OfType<NativeButton>()
                            .FirstOrDefault(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == text["Common_Cancel"]);
                        if (cancel is null) await Task.Delay(25);
                    }
                    if (cancel is null || FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new IOException("Native analysis Cancel unavailable.");
                    await CaptureWindowAsync(window, Path.Combine(folder, key + "-" + caption + ".png")); invoke.Invoke();
                    for (var wait = 0; wait < 200 && model.IsBusy; wait++) await Task.Delay(25);
                }
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = finished.Count, FinancialHash = before, RetainedTermsOnly = retainedTermsOnly, RealHttpOrModel = false }));
        static IEnumerable<NativeNode> Nodes(NativeNode node)
        { yield return node; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Nodes(VisualTreeHelper.GetChild(node, i))) yield return child; }
        static string Lines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
    private sealed class AnalysisLayoutRuntime : IAiRuntime, IAiRemote
    {
        internal int Shares, Submissions;
        public bool NetworkAvailable => true;
        public string Destination => "https://localhost/fictitious-analysis-layout";
        public AiUiAccess Capture(string originalFile) => new(() => { }, CancellationToken.None);
        public Task<bool> ConfirmDisclosureAsync() => throw new IOException("Only original native Cancel is permitted in layout review.");
        public Task ShareAsync(string text) { Shares++; throw new IOException("No external sharing belongs to layout."); }
        public Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct) => Task.FromResult<IAiRemote>(this);
        public Task<AiAvailability> AvailabilityAsync(CancellationToken ct) => Task.FromResult(new AiAvailability(true, "fictitious-local", "fixture-v1", new(50, 20, 0, 0)));
        public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct) => Task.FromResult(new AiOffer(Guid.NewGuid(), "fictitious-local", "fixture-v1", purpose, 8, DateTimeOffset.UtcNow.AddMinutes(5)));
        public Task<AiClientRecord?> ReadAsync(CancellationToken ct)
        {
            var payload = new AiPayload(1, AiPurpose.PeriodExplanation, "en", new(new(2026, 10, 1), new(2026, 10, 17), 1, true, [new("EUR", 123456, 0, 1234, 500)], 0), null);
            var offer = new AiOffer(Guid.NewGuid(), "fictitious-local", "fixture-v1", payload.Purpose, 8, DateTimeOffset.UtcNow.AddMinutes(5)); var id = Guid.NewGuid();
            return Task.FromResult<AiClientRecord?>(new(1, new(id, offer, payload), new(id, AiProtocol.Digest(payload), AiRequestState.Completed, 3,
                "Fictitious plain text / متن آزمایشی / Beispieltext. <script> is text, never an action.\n" + new string('x', 180), new(50, 20, 0, 0))));
        }
        public Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct) { Submissions++; throw new IOException("No provider submission belongs to layout."); }
        public Task<AiRequestResult> ContinueAsync(CancellationToken ct) => throw new IOException("No service continuation belongs to layout.");
        public Task<AiRequestResult> CloseAsync(CancellationToken ct) => throw new IOException("No service closure belongs to layout.");
        public void Dispose() { }
    }
}
#endif
