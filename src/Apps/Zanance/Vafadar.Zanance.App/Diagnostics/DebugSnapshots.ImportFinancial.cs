#if DEBUG && WINDOWS
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Invokes the original native import page on an independent desktop; no owner desktop input is sent.</summary>
    private static async Task ReviewImportFinancialWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var languageBefore = local.CurrentLanguage; var themeBefore = theme.Choice; var results = new List<object>();
        var captures = new List<object>(); var store = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await BaseRows();
        var literal = Environment.GetEnvironmentVariable("VAFADAR_IMPORT_LITERAL_TEXT") == "1";
        IReadOnlyDictionary<bool, JsonElement> Retained(string context, string directory)
        {
            var priorRun = Environment.GetEnvironmentVariable("VAFADAR_IMPORT_FINANCIAL_CONTINUATION");
            var retained = new Dictionary<bool, JsonElement>();
            if (string.IsNullOrEmpty(priorRun)) return retained;
            if (!Guid.TryParseExact(priorRun, "N", out _)) throw new IOException("Exact original import review run required.");
            var source = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "store-drafts", priorRun);
            using var process = JsonDocument.Parse(File.ReadAllText(Path.Combine(source, "process.json")));
            if (process.RootElement.GetProperty("Package").GetString() != "pro.vafadar.zanance.storepreview." + priorRun[..12])
                throw new IOException("Original independent import review identity differs.");
            foreach (var generic in new[] { false, true })
            {
                var prefix = generic ? "mapped" : "own"; var previous = Path.Combine(source, context);
                var receipt = Path.Combine(previous, prefix + "-complete.json");
                if (!File.Exists(receipt))
                {
                    var acceptedPath = Path.Combine(previous, prefix + "-accepted.json");
                    if (!File.Exists(acceptedPath)) continue;
                    using var accepted = JsonDocument.Parse(File.ReadAllText(acceptedPath));
                    if (accepted.RootElement.GetProperty("Context").GetString() != context
                        || !accepted.RootElement.GetProperty("AcceptedOnce").GetBoolean()) throw new IOException("Exact pending accepted case required.");
                    var wal = Path.Combine(previous, prefix + ".db-wal");
                    if (File.Exists(wal) && new FileInfo(wal).Length != 0) throw new IOException("Preserve an uncheckpointed original financial fixture.");
                    Directory.CreateDirectory(directory);
                    foreach (var suffix in new[] { ".db", "-accepted.json", "-refused.png" })
                        File.Copy(Path.Combine(previous, prefix + suffix), Path.Combine(directory, prefix + suffix), false);
                    captures.Add(new { Language = context, File = prefix + "-refused.png" });
                    continue;
                }
                using var json = JsonDocument.Parse(File.ReadAllText(receipt)); var item = json.RootElement;
                if (item.GetProperty("Context").GetString() != context || item.GetProperty("Generic").GetBoolean() != generic
                    || !item.GetProperty("OriginalImportAndUndoOnce").GetBoolean() || !item.GetProperty("CompleteStoredRowsRestored").GetBoolean())
                    throw new IOException("Incomplete original accepted import case.");
                Directory.CreateDirectory(directory);
                // Copy only the completed, independently owned fixture and its original observations;
                // the continuation skips its financial commands entirely.
                foreach (var suffix in new[] { ".db", "-accepted.json", "-complete.json", "-accepted.png", "-refused.png" })
                    File.Copy(Path.Combine(previous, prefix + suffix), Path.Combine(directory, prefix + suffix), false);
                foreach (var state in new[] { "accepted", "refused" }) captures.Add(new { Language = context, File = prefix + "-" + state + ".png" });
                retained.Add(generic, item.Clone());
            }
            return retained;
        }
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                if (literal && (language != "fa" || choice != ThemeChoice.Dark)) continue;
                await SetTheme(choice); await SetLanguageAsync(app, local, language);
                app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                var context = language + "-" + choice; var directory = Path.Combine(folder, context);
                var completed = literal ? new Dictionary<bool, JsonElement>() : Retained(context, directory);
                results.Add(await ImportFinancialNativeReview.RunAsync(services, directory, context,
                    async page => { await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(250); },
                    async (page, command) =>
                    {
                        var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && Visible(item));
                        var scroll = FindScrollView(page) ?? throw new IOException("Original import viewport missing.");
                        await ScrollToViewIfNeededAsync(scroll, button); await Task.Delay(150);
                        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                            || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                            throw new IOException("Original native import Invoke unavailable.");
                        action.Invoke(); await Task.Yield();
                    },
                    async (state, page, caption) =>
                    {
                        await Task.Delay(200);
                        var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == caption && Visible(item));
                        if (literal) await ScrollToViewIfNeededAsync(FindScrollView(page)!, label);
                        var expected = label.FlowDirection == FlowDirection.LeftToRight ? caption : Vafadar.Localization.Formatting.NativeDigits.Apply(caption);
                        var deadline = DateTime.UtcNow.AddSeconds(20);
                        while (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                            || native.Text != expected)
                        {
                            if (DateTime.UtcNow >= deadline)
                            {
                                await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, state + "-unaccepted.png"));
                                throw new IOException("Original financial review caption is incomplete; retain accepted financial state.");
                            }
                            await Task.Delay(50);
                        }
                        await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, state + ".png"));
                        captures.Add(new { Language = context, File = state + ".png" });
                    },
                    async page =>
                    {
                        if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original review page changed.");
                        await Shell.Current.Navigation.PopAsync(false);
                    }, CancellationToken.None, completed, literalText: literal));
            }
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw;
        }
        finally
        {
            // A modal financial page defers root/theme publication. Close only this independent
            // original review page before restoring the captured presentation (D-405).
            if (Shell.Current.CurrentPage is ImportExportPage) await Shell.Current.Navigation.PopAsync(false);
            await SetTheme(themeBefore); await SetLanguageAsync(app, local, languageBefore.CultureName);
        }
        if (before != await BaseRows()) throw new IOException("Original independent base store changed.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Cases = results, Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true, LiteralText = literal,
            OriginalThemeAndLanguageRestored = true, ActualNativeInvoke = true, NoOwnerDesktopInput = true }));
    }
}
#endif
