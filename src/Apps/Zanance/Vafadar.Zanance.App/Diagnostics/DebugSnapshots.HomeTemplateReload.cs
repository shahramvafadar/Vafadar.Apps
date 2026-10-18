#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original template reload/resize-to-empty lifecycle without posting money or changing definitions.</summary>
    private static async Task ReviewHomeTemplatesAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Templates = await store.GetTemplatesAsync() });
        var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var name = language + "-" + theme; var output = Path.Combine(folder, name); Directory.CreateDirectory(output);
            void Mark(string stage) => File.WriteAllText(Path.Combine(folder, "current-phase.json"), JsonSerializer.Serialize(new { Case = name, Stage = stage }));
            Mark("context");
            await Shell.Current.GoToAsync("//home");
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language); await WaitForStorePageAsync(app);
            var page = (HomePage)Shell.Current.CurrentPage; await page.PendingPublication;
            var vm = (HomeViewModel)page.BindingContext; var stored = await StoredAsync();
            if (vm.QuickTemplates.Count < 2) throw new IOException("Original template teardown fixture requires multiple definitions.");
            var strip = VisualDescendants(page).OfType<HorizontalStackLayout>()
                .Single(value => ReferenceEquals(BindableLayout.GetItemsSource(value), vm.QuickTemplates));
            await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(strip, ScrollToPosition.MakeVisible, false);
            var roots = strip.Children.OfType<View>().ToArray();
            var handlers = roots.Select(view => view.Handler?.PlatformView).ToArray();
            if (handlers.Any(value => value is null)) throw new IOException("Original template native handlers unavailable.");
            Mark("original-reload");
            await vm.LoadAsync(); await Task.Delay(150);
            var retained = roots.Length == strip.Children.Count && roots.Select((view, index) =>
                ReferenceEquals(view, strip.Children[index]) && ReferenceEquals(handlers[index], view.Handler?.PlatformView)).All(value => value);
            File.WriteAllText(Path.Combine(output, "reload.json"), JsonSerializer.Serialize(new { RetainedOriginalNativeRows = retained,
                Before = roots.Length, After = strip.Children.Count, SourceCount = vm.QuickTemplates.Count }));
            if (!retained) throw new IOException("Unchanged Home templates unnecessarily disconnect and recreate their native rows.");
            var collection = (object)vm.QuickTemplates as SnapshotCollection<QuickTemplate>
                ?? throw new IOException("Original Home templates need complete snapshot publication.");
            var originals = vm.QuickTemplates.ToArray(); var observations = new List<object>();
            foreach (var count in new[] { 2, 1, 0, 2, 0, 2 })
            {
                Mark("transition-" + observations.Count + "-" + count);
                var rows = originals.Reverse().Take(count).Select(item => item with
                    { Name = item.Name + " retained", Description = item.Description + " retained", IconColor = Palette.WarningText, CanUse = false }).ToArray();
                collection.ReplaceAll(rows); vm.HasQuickTemplates = rows.Length != 0;
                await Task.Delay(100);
                if (strip.Children.Count != rows.Length) throw new IOException("Original template count transition differs.");
                for (var index = 0; index < rows.Length; index++)
                {
                    var root = (View)strip.Children[index];
                    var button = VisualDescendants(root).OfType<Button>().Single();
                    var caption = VisualDescendants(root).OfType<Label>().Single(value => value.Text == rows[index].Name);
                    if (!ReferenceEquals(root.BindingContext, rows[index]) || !ReferenceEquals(button.Command, vm.UseTemplateCommand)
                        || !ReferenceEquals(button.CommandParameter, rows[index]) || button.IsEnabled
                        || SemanticProperties.GetDescription(button) != rows[index].Description)
                        throw new IOException("Rebound original template command, metadata or enablement differs.");
                    CheckCaption(caption);
                }
                observations.Add(new { Count = count, OriginalMetadataAndDisabledTargets = true });
            }
            Mark("restore-original-read"); await vm.LoadAsync();
            if (!vm.Loading.IsReady || vm.QuickTemplates.Count != originals.Length || stored != await StoredAsync())
                throw new IOException("Original Home template lifecycle changed stored inputs or did not restore.");
            await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(strip, ScrollToPosition.MakeVisible, false);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "home.png"));
            File.WriteAllText(Path.Combine(output, "complete.json"), JsonSerializer.Serialize(new
                { RetainedOriginalNativeRows = true, LegitimateCountTransitions = observations, StoredInputsUnchanged = true, FinancialCommandsInvoked = false }));
            captures.Add(new { Language = name, File = "home.png" });
        }
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, FinancialRowsUnchangedAfterSeeding = true }));
    }
}
#endif
