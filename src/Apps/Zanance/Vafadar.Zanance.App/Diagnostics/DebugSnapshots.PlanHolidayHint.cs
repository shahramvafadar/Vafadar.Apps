#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Reads only the changed original holiday caption; completed contexts never replay a writer or native scene.</summary>
    private static async Task ReviewPlanHolidayHintAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.holidayhintsreview")
            throw new IOException("The original holiday hint requires its independent compiled identity.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(12)); var ct = deadline.Token;
        var path = Path.Combine(folder, "holiday-hint.db"); var setup = Path.Combine(folder, "holiday-hint-setup.json");
        var resume = Environment.GetEnvironmentVariable("VAFADAR_HOLIDAY_HINT_RESUME") == "1";
        if (File.Exists(path) != resume || File.Exists(setup) != resume)
            throw new IOException("Preserve an incomplete original fixture; do not repeat its accepted setup.");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(path).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        string before;
        if (!resume)
        {
            fixture.MigrateLocalDatabase<ZananceDbContext>();
            var account = new Account { Name = "Original holiday caption cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 9) };
            await store.SaveAccountAsync(account, ct);
            await store.UpdateSettingsAsync(settings =>
            { settings.Mode = ExperienceMode.Advanced; settings.OnboardingCompleted = true; settings.DefaultAccountId = account.Id; }, ct);
            before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            await using var output = new FileStream(setup, FileMode.CreateNew, FileAccess.Write);
            await JsonSerializer.SerializeAsync(output, new { Before = before }, cancellationToken: ct);
        }
        else
        {
            using var original = JsonDocument.Parse(await File.ReadAllTextAsync(setup, ct));
            before = original.RootElement.GetProperty("Before").GetString() ?? throw new IOException("Original fixture receipt missing.");
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                throw new IOException("The retained original caption source changed.");
        }
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0];
        var oldLanguage = local.CurrentLanguage.CultureName; var oldRegion = local.CurrentRegion; var oldTheme = themes.Choice;
        var oldWidth = window.Width; var oldHeight = window.Height; var observations = new List<object>();
        await using var reminders = new ReminderService(new NoReminderScheduler(), store, plans,
            fixture.GetRequiredService<GoalStore>(), fixture.GetRequiredService<HoldingStore>(),
            services.GetRequiredService<Vafadar.Core.Settings.ISettingsStore>(), translator, new DateFormatter(local), local, TimeProvider.System);
        try
        {
            local.SetRegion("DE"); window.Height = 892;
            foreach (var language in new[] { "en", "fa", "de", "es", "fr", "it" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                var key = $"{language}-{theme}-{width}"; var receipt = Path.Combine(folder, key + "-complete.json");
                await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language);
                var template = translator["Weekend_Hint"];
                if (File.Exists(receipt))
                {
                    using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                    if (retained.RootElement.GetProperty("SourceTemplate").GetString() != template
                        || !retained.RootElement.GetProperty("StoredRowsUnchanged").GetBoolean()
                        || !File.Exists(Path.Combine(folder, key + ".png")))
                        throw new IOException("The original completed caption context is not reusable.");
                    observations.Add(new { Context = key, Retained = true }); continue;
                }
                var editor = new PlanEditorViewModel(store, plans, translator, local, new DateFormatter(local),
                    TimeProvider.System, reminders, new EditorWriteReviewInteraction());
                await editor.ApplyQueryAsync(new Dictionary<string, object>());
                var page = new PlanEditorPage(editor); ProfileNativeReads.Add(page);
                var root = window.Page ?? throw new IOException("Independent original window root missing.");
                await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
                try
                {
                    await PlanEditorNativeReview.WaitAsync(() => page.IsLoaded && editor.Loading.IsReady, ct);
                    window.Width = width;
                    for (var attempt = 0; attempt < 4; attempt++)
                    {
                        await Task.Delay(180, ct);
                        if (Math.Abs(page.Width - width) <= 1) break;
                        window.Width += width - page.Width;
                    }
                    if (Math.Abs(page.Width - width) > 1) throw new IOException("Actual original page width does not match the requested context.");
                    editor.Start = new(2026, 12, 25); editor.PresetIndex = 3; editor.EndIndex = 2;
                    editor.CountText = "2"; editor.WeekendIndex = 2; editor.SkipHolidays = true; editor.ShowRuleOptions = true;
                    await Task.Delay(180, ct);
                    var label = PlanEditorNativeReview.Views(page).OfType<Label>().Single(item => item.Text == editor.WeekendHint);
                    ScrollView? viewport = null;
                    for (Element? parent = label.Parent; parent is not null; parent = parent.Parent)
                        if (parent is ScrollView scroll) { viewport = scroll; break; }
                    if (viewport is null) throw new IOException("Original caption scroll owner missing.");
                    var visible = false; double top = 0;
                    for (var attempt = 0; attempt < 10 && !visible; attempt++)
                    {
                        await ScrollToViewIfNeededAsync(viewport, label);
                        if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
                            || label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
                            throw new IOException("Original native holiday caption missing.");
                        top = text.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
                        visible = top >= -1 && top + text.ActualHeight <= native.ActualHeight + 1;
                        if (!visible) await Task.Delay(120, ct);
                    }
                    if (!visible) throw new IOException("Original holiday caption is outside its native viewport.");
                    CheckCaption(label, ProfileNativeReads);
                    if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock block || block.Text != editor.WeekendHint
                        || !editor.CanSkipHolidays || !editor.SkipHolidays || string.IsNullOrWhiteSpace(editor.HolidayHint)
                        || before != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                        throw new IOException("The original caption or complete fixture rows changed.");
                    await CaptureWindowAsync(window, Path.Combine(folder, key + ".png"));
                    var result = new { Language = language, Theme = theme.ToString(), RequestedWidth = width, PageWidth = page.Width,
                        SourceTemplate = template, Text = block.Text, NativeWidth = block.ActualWidth, NativeHeight = block.ActualHeight,
                        ViewportTop = top, HolidayEnabled = true, StoredRowsUnchanged = true, FinancialSaves = 0 };
                    await using var output = new FileStream(receipt, FileMode.CreateNew, FileAccess.Write);
                    await JsonSerializer.SerializeAsync(output, result, cancellationToken: ct); observations.Add(result);
                }
                finally { await root.Navigation.PopModalAsync(false); }
            }
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Original caption fixture changed after retirement.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            { Scope = "original-holiday-hint", Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
                Contexts = observations, StoredRowsUnchanged = true, FinancialSaves = 0 }), ct);
        }
        finally
        {
            window.Width = oldWidth; window.Height = oldHeight; local.SetRegion(oldRegion);
            await SetStoreThemeAsync(app, themes, oldTheme); await SetLanguageAsync(app, local, oldLanguage);
        }
    }
}
#endif
