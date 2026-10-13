#if DEBUG && WINDOWS
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // Actual controls with fictitious preparation outcomes. No native PIN/key, owner data or OS setting is changed.
    private static async Task ReviewStartupAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        if (!app.StartupStorageRanOffWindowThread) throw new InvalidOperationException("Actual startup storage ran on the window thread.");
        var localization = services.GetRequiredService<ILocalizationService>();
        var window = app.Windows[0]; var original = window.Page;
        async Task<string> StoredAsync()
        {
            await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
            await db.Database.OpenConnectionAsync(); using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            var tables = new List<string>(); using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            var rows = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table.Replace("\"", "\"\"") + "\" ORDER BY rowid";
                using var reader = await command.ExecuteReaderAsync(); var values = new List<string>();
                while (await reader.ReadAsync()) values.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.IsDBNull(i) ? null : reader.GetValue(i)).ToArray()));
                rows[table] = values;
            }
            return JsonSerializer.Serialize(rows);
        }
        var before = await StoredAsync();
        try
        {
            foreach (var language in languages)
            {
                localization.SetLanguage(localization.SupportedLanguages.Single(value => value.CultureName == language));
                var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var attempts = 0;
                var vm = new StartupViewModel(() => ++attempts == 1 ? Task.FromException(new IOException("Fictitious preparation failure")) : held.Task);
                var page = new StartupPage(vm).WithFlowDirection(localization); window.Page = page;
                var reads = new List<object>();
                async Task CaptureState(string name)
                {
                    await Task.Delay(200);
                    foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label, reads);
                    await CaptureAsync(app, folder, language + "-startup-" + name);
                }
                await CaptureState("loading");
                await vm.InitializeAsync(); await CaptureState("failed");
                var button = VisualDescendants(page).OfType<Button>().Single(Visible);
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44)
                    throw new InvalidOperationException("Startup retry target is too small.");
                var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)!;
                if (peer.GetName() != Translator.Instance["Startup_Retry"]) throw new InvalidOperationException("Startup retry name differs.");
                var width = native.ActualWidth; var height = native.ActualHeight;
                InvokeChoiceReview(button, reads); await Task.Delay(100);
                if (!vm.IsBusy || vm.HasFailed || attempts != 2) throw new InvalidOperationException("Native startup retry did not retain the cover.");
                held.SetResult(); await Task.Delay(100);
                if (vm.IsBusy || vm.HasFailed || await StoredAsync() != before) throw new InvalidOperationException("Startup review changed stored data or failed completion.");
                await File.WriteAllTextAsync(Path.Combine(folder, language + "-startup-proof.json"), JsonSerializer.Serialize(new
                { ActualStartupWorkerThread = true, NativeLoadingFailureRetry = true, CompleteStoredRowsUnchanged = true,
                    RetryWidth = width, RetryHeight = height, NativeObservations = reads.Count }));
            }
        }
        finally { window.Page = original; }
    }
}
#endif
