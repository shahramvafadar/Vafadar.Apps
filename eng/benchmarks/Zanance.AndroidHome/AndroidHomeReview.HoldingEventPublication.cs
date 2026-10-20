using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies actual original event loading/error/Retry controls and routed draft rebinding.</summary>
    private static async Task<object> ReviewHoldingEventPublicationAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var captures = new List<object>();
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(), Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var before = await BaseRows();
        bool Visible(Element item)
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
            try { theme.Set(value); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        async Task Show(AssetEventEditorPage page) { await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(200); }
        async Task Close(AssetEventEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original event page changed.");
            await Shell.Current.Navigation.PopAsync(false);
        }
        async Task Retry(AssetEventEditorPage page)
        {
            var button = page.FindByName<Button>("RetryLoadButton");
            if (!Visible(button)) throw new IOException("Original event Retry is covered.");
            var deadline = DateTime.UtcNow.AddSeconds(30); Android.Widget.Button? native;
            while ((native = button.Handler?.PlatformView as Android.Widget.Button) is null || !native.IsShown || !native.HasWindowFocus || !native.Enabled || native.Context?.PackageName != Package)
            { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original event native Retry unavailable."); await Task.Delay(50, ct); }
            if (!native.PerformClick()) throw new ReviewCheckException("Original event native Retry refused."); await Task.Yield();
        }
        async Task Observe(AssetEventEditorPage page, string stage)
        {
            await Task.Delay(200); var vm = (AssetEventEditorViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadySurface")) != vm.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("LoadingSurface")) != vm.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("ErrorSurface")) != vm.Loading.HasFailed)
                throw new IOException("Original event native cover differs.");
            var label = vm.Loading.IsLoading ? page.FindByName<Label>("LoadingCaption") : vm.Loading.HasFailed ? page.FindByName<Label>("ErrorCaption") : null;
            var text = vm.Loading.IsLoading ? services.GetRequiredService<Translator>()["Holdings_Loading"] : vm.LoadError;
            if (label is not null)
            {
                var deadline = DateTime.UtcNow.AddSeconds(15); Android.Widget.TextView? native;
                while ((native = label.Handler?.PlatformView as Android.Widget.TextView) is null || !native.IsShown || !native.HasWindowFocus
                    || native.Text != NativeDigits.Apply(text) || native.Layout is not { LineCount: > 0 })
                { if (DateTime.UtcNow > deadline) throw new ReviewCheckException("Original complete event cover caption missing."); await Task.Delay(50, ct); }
                for (var line = 0; line < native.Layout.LineCount; line++)
                    if (native.Layout.GetEllipsisCount(line) != 0 || native.Layout.GetLineWidth(line) > native.Width - native.TotalPaddingLeft - native.TotalPaddingRight + 1)
                        throw new ReviewCheckException("Original event cover caption incomplete.");
            }
            if (vm.Loading.IsReady)
            {
                if (page.FindByName<Entry>("QuantityInput").Handler?.PlatformView is not Android.Widget.EditText entry || entry.Text != vm.QuantityText)
                    throw new ReviewCheckException("Original native event quantity binding differs.");
                var save = ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.SaveCommand));
                if (save.Handler?.PlatformView is not Android.Widget.Button native || native.Enabled != vm.CanSave)
                    throw new ReviewCheckException("Original event native Save enablement differs.");
            }
            captures.Add(new { Stage = stage, vm.TypeName, vm.QuantityText, vm.Note, vm.CanSave, vm.CanReload, vm.Loading.IsReady, vm.Loading.IsLoading, vm.Loading.HasFailed,
                NativeCoverCaptionAndQuantityChecked = true, ProcessId = Environment.ProcessId });
        }
        var original = await HoldingEventPublicationNativeReview.PrepareAsync(services, Path.Combine(folder, "original"), ct);
        try
        {
            var first = true;
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await HoldingEventPublicationNativeReview.ContextAsync(services, original, name + "-" + value, first, Show, Retry, Close, Observe, ct); first = false;
            }
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally
        {
            if (Shell.Current.CurrentPage is AssetEventEditorPage page) await Close(page);
            await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct);
        }
        if (original.Rows != await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct) || before != await BaseRows())
            throw new IOException("Original event native observation changed fixture/base rows.");
        await original.Fixture.DisposeAsync();
        return new { Comparison = comparison, Observations = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, OriginalTypedRowsUnchanged = true, OriginalBaseStoreUnchanged = true, OnlySeparateFictitiousLedgerChanged = true, NoFinancialOrMetadataCommand = true, ControlledReadOnWorker = true, OriginalLanguageAndThemeRestored = true };
    }
}
