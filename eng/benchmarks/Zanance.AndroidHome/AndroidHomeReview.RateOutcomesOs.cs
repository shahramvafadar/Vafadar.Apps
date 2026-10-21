using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Text;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Reads historical accepted rate captions into original disabled controls without restoring a deleted rate.</summary>
    private static async Task<object> ReviewRateOutcomesOsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var width = Platform.CurrentActivity?.Intent?.GetIntExtra("review_os_width", 0) ?? 0;
        var resources = Android.App.Application.Context.Resources;
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.androidhomereview" || width is not (360 or 412)
            || Math.Abs((resources?.Configuration?.FontScale ?? 0) - 2f) > 0.01f
            || resources?.DisplayMetrics is not { Density: > 0 } metrics || Math.Abs(metrics.WidthPixels / metrics.Density - width) > 1)
            throw new ReviewCheckException("Actual independent OS200% rate-outcome width required.");
        var folder = Path.Combine(root, "original-physical-rates");
        string[] feedback = ["Rates_SourceChanged", "Rates_ChangeFailed", "Rates_SavedFollowUpFailed"];
        if (!File.Exists(Path.Combine(folder, "physical-rate-actions-complete.json"))
            || !File.Exists(Path.Combine(folder, "rate-native-delete-accepted.json"))
            || (from language in new[] { "en", "fa", "de" } from theme in new[] { "Light", "Dark" } from key in feedback
                select Path.Combine(root, language + "-" + theme + "-" + key + ".json")).Any(file => !File.Exists(file)))
            throw new ReviewCheckException("Completed original rate actions, delete and all native caption receipts required.");
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        using var original = await PlanEditorNativeReview.ContinuePhysicalRateOutcomesAsync(services, folder, ct);
        var source = await original.AllMoneyRowsAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        if ((await original.Store.GetRatesAsync(ct)).Count != 0)
            throw new ReviewCheckException("Original accepted rate deletion must remain in force.");
        using var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "physical-rate-actions-complete.json"), ct));
        using var oldRows = JsonDocument.Parse(accepted.RootElement.GetProperty("Rows").GetString()!);
        using var rate = JsonDocument.Parse(oldRows.RootElement.GetProperty("ExchangeRates").EnumerateArray().Single().GetString()!);
        Dictionary<string, int> columns;
        await using (var db = await original.Factory.CreateDbContextAsync(ct))
        {
            await db.Database.OpenConnectionAsync(ct); using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT * FROM ExchangeRates LIMIT 0";
            using var reader = await command.ExecuteReaderAsync(ct);
            columns = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, ordinal => ordinal, StringComparer.Ordinal);
        }
        var values = rate.RootElement;
        var id = Guid.Parse(values[columns["Id"]].GetString()!);
        var date = DateOnly.ParseExact(values[columns["Date"]].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var ratio = decimal.Parse(values[columns["Rate"]].GetString()!, CultureInfo.InvariantCulture);
        var estimate = values[columns["IsEstimate"]].GetInt32() == 1;
        if (id == Guid.Empty || ratio != 0.00000002469135780246913578m || !estimate
            || values[columns["FromCurrencyCode"]].GetString() != "USD" || values[columns["ToCurrencyCode"]].GetString() != "EUR")
            throw new ReviewCheckException("Exact original later-version rate receipt required; never invent a positive source.");
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var oldLanguage = local.CurrentLanguage.CultureName; var oldTheme = themes.Choice;
        var observations = new List<object>(); var prefix = "rate-outcomes-os-" + width + "-";
        bool Completed(string stage) => File.Exists(Path.Combine(root, stage + ".json"));
        async Task ChangeTheme(ThemeChoice theme)
        {
            if (themes.Choice == theme) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(theme); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        static bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        async Task Observe(Page page, string stage)
        {
            if (Completed(stage)) return;
            var originalStage = stage[prefix.Length..];
            using var frame = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, originalStage + ".json"), ct));
            var caption = frame.RootElement.GetProperty("NativeReadings").EnumerateArray().Select(row => row.GetProperty("Text").GetString())
                .OfType<string>().Single(text => text.Contains("USD", StringComparison.Ordinal) && text.Contains("EUR", StringComparison.Ordinal) && text.Contains('='));
            if (!Digits.ToAscii(caption).Replace(',', '.').Contains(ratio.ToString("0.############################", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new ReviewCheckException("Original native caption differs from its immutable precise rate.");
            var model = (RatesViewModel)page.BindingContext;
            if (model.CanEdit || model.CanReload || model.CanAdd || model.Rates.Count != 0)
                throw new ReviewCheckException("Deleted source and original read-only outcome required.");
            // The original display text is evidence, not a reconstructed writer review or a new formatter implementation.
            var row = new RateRow(id, caption, services.GetRequiredService<IDateFormatter>().Format(date, DateFormatStyle.Long), estimate);
            model.Rates.Add(row); model.HasRates = true;
            var expected = new HashSet<string>(StringComparer.Ordinal) { model.Error!, row.Text, row.DateText,
                services.GetRequiredService<Translator>()["Rates_EstimateBadge"] };
            var readings = new List<object>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            await Task.Delay(200, ct);
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(label => label.Text is not null && expected.Contains(label.Text)))
            {
                for (Element? parent = label.Parent; parent is not null; parent = parent.Parent)
                    if (parent is ScrollView viewport) { await viewport.ScrollToAsync(label, ScrollToPosition.MakeVisible, false); break; }
                await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Android.Widget.TextView { IsShown: true, HasWindowFocus: true, Layout.LineCount: > 0 }, ct);
                var native = (Android.Widget.TextView)label.Handler!.PlatformView!; var layout = native.Layout!;
                if (Digits.ToAscii(native.Text ?? "") != Digits.ToAscii(label.Text!)
                    || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                    throw new ReviewCheckException("Actual OS-scaled historical rate text is incomplete.");
                MonetaryOsScope.RequireCompleteHeight(native, layout); seen.Add(label.Text!);
                readings.Add(new { label.Text, NativeText = native.Text, native.Width, native.Height, Lines = layout.LineCount });
            }
            if (!expected.SetEquals(seen)) throw new ReviewCheckException("All original rate outcome, precision, date and estimate captions required.");
            var deletes = PlanEditorNativeReview.Views(page).OfType<Button>().Where(Visible).Where(button => ReferenceEquals(button.Command, model.DeleteCommand)).ToArray();
            if (deletes.Length != 1 || deletes[0].Handler?.PlatformView is not Android.Widget.Button { Enabled: false })
                throw new ReviewCheckException("Historical rate geometry must not enable a delete target.");
            if (source != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new ReviewCheckException("Historical rate geometry changed original source or base rows.");
            var observation = new { Stage = stage, ProcessId = Environment.ProcessId, NativeReadings = readings,
                HistoricalSource = true, OriginalRateId = id, ExactRatio = ratio, estimate, WriterDisabled = true,
                DeletedRateStillAbsent = true, SourceAndBaseUnchanged = true, AcceptedWrites = 0 };
            await File.WriteAllTextAsync(Path.Combine(root, stage + ".json"), JsonSerializer.Serialize(observation), ct);
            observations.Add(observation); LogReport("rate-outcomes-os-observation", comparison, observation);
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await ChangeTheme(theme); await SetLanguageAsync(app, local, language, ct);
                await original.PhysicalRateFeedbackAsync(prefix + language + "-" + theme, Observe, ct, Completed);
            }
        }
        finally { await ChangeTheme(oldTheme); await SetLanguageAsync(app, local, oldLanguage, ct); }
        return new { Comparison = comparison, Scope = "rate-outcomes-os-" + width, Observations = observations,
            OriginalSourceAndBaseUnchanged = source == await original.AllMoneyRowsAsync(ct) && baseline == await ImportFinancialNativeReview.StoredAsync(services, ct),
            HistoricalPresentationOnly = true, AcceptedWrites = 0, DeletedRateStillAbsent = (await original.Store.GetRatesAsync(ct)).Count == 0, Device = DeviceEvidence() };
    }
}
