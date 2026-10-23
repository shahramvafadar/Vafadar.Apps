using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Renders original incomplete legacy transfers without asking the current form to create invalid data.</summary>
    private static async Task<object> ReviewForecastMissingRateAsync(App app, IServiceProvider services, string root, CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var original = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var time = services.GetRequiredService<TimeProvider>();
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var path = Path.Combine(root, "legacy-forecast.db"); var exists = File.Exists(path);
        await using var fixture = new ServiceCollection().AddSingleton(time).AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        if (!exists)
        {
            await using var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            var source = new Account { Name="Original EUR source", CurrencyCode="EUR", OpeningBalance=10000, OpeningDate=day };
            var destination = new Account { Name="Original USD destination", CurrencyCode="USD", OpeningBalance=5000, OpeningDate=day };
            db.Accounts.AddRange(source, destination);
            db.Settings.Add(new ZananceSettings { DefaultAccountId=source.Id });
            // This independent raw legacy row is deliberately not a valid new form submission.
            db.Schedules.Add(new Schedule { Name="Legacy destination unavailable", AccountId=source.Id, ToAccountId=destination.Id,
                Kind=EntryKind.Transfer, Amount=1000, ToAmount=null, AmountMode=AmountMode.Fixed,
                Rule=new() { Frequency=Frequency.Once, Start=day } });
            await db.SaveChangesAsync(ct);
        }
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var work = fixture.GetRequiredService<ForecastWorkStore>();
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var oldTheme=themes.Choice; var oldLanguage=local.CurrentLanguage.CultureName;
        var observations = new List<JsonElement>();
        async Task Context(string language, ThemeChoice theme)
        {
            if (themes.Choice != theme)
            {
                var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
                app.ShellRebuilt += Rebuilt;
                try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                finally { app.ShellRebuilt -= Rebuilt; }
            }
            await SetLanguageAsync(app, local, language, ct); await AwaitRootHomeAsync(ct);
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                var key=language+"-"+theme; var receipt=Path.Combine(root,key+"-forecast.json");
                if (File.Exists(receipt))
                {
                    var retained=JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt,ct));
                    if (!retained.GetProperty("OriginalSourceUnchanged").GetBoolean()) throw new IOException("Retained forecast receipt refused.");
                    observations.Add(retained); continue;
                }
                await Context(language,theme);
                var dates = new DateFormatter(local);
                var vm = new ForecastViewModel(store,text,dates,local,time,work); await vm.LoadAsync();
                var source=vm.Workspace.Forecasts.Single(item=>item.CurrencyCode=="EUR");
                var destination=vm.Workspace.Forecasts.Single(item=>item.CurrencyCode=="USD");
                if (source.EndBalance!=9000 || source.IsIncomplete || destination.EndBalance!=5000
                    || destination.UnknownCount!=1 || destination.Items.Single().Effect is not null)
                    throw new ReviewCheckException("Original legacy forecast fabricated destination money.");
                var card=vm.Cards.Single(item=>item.CurrencyCode=="USD");
                if (card.IncompleteText!=text.Format("Forecast_Incomplete",1)
                    || card.Rows.Single().AmountText!=text["Plan_AmountUnknown"])
                    throw new ReviewCheckException("Original missing-rate warning is incomplete.");
                var page=new ForecastPage(vm,dates); await Shell.Current.Navigation.PushModalAsync(page);
                try
                {
                    await PlanEditorNativeReview.WaitAsync(()=>PlanEditorNativeReview.Views(page).OfType<Label>().Any(label=>
                        label.Text==card.IncompleteText && label.Handler?.PlatformView is Android.Widget.TextView
                            { IsAttachedToWindow:true, HasWindowFocus:true, Height:>0 }),ct);
                    var warning=PlanEditorNativeReview.Views(page).OfType<Label>().Single(label=>label.Text==card.IncompleteText);
                    var viewport=PlanEditorNativeReview.Views(page).OfType<ScrollView>().Single(view=>view.Orientation==ScrollOrientation.Vertical);
                    await viewport.ScrollToAsync(warning,ScrollToPosition.Center,false); await Task.Delay(100,ct);
                    if (warning.Handler?.PlatformView is not Android.Widget.TextView { IsShown:true, HasWindowFocus:true } native
                        || native.Layout is not { } layout || layout.Height>native.Height-native.TotalPaddingTop-native.TotalPaddingBottom+1)
                        throw new ReviewCheckException("Original incomplete warning is not visible.");
                    for (var line=0;line<layout.LineCount;line++)
                        if (layout.GetEllipsisCount(line)!=0) throw new ReviewCheckException("Original incomplete warning is truncated.");
                    Android.Util.Log.Info("ZananceHomeStage","forecast-missing-rate|"+key+"|EUR9000|USD5000|unknown1");
                }
                finally { if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(),page)) await Shell.Current.Navigation.PopModalAsync(); }
                if (before!=await ImportFinancialNativeReview.StoredAsync(fixture,ct)) throw new IOException("Original legacy rows changed during forecast reading.");
                var result=JsonSerializer.SerializeToElement(new { Language=language, Theme=theme.ToString(), SourceEndMinor=9000,
                    DestinationEndMinor=5000, DestinationUnknownCount=1, OriginalNativeWarningVisible=true, OriginalSourceUnchanged=true, FinancialSaves=0 });
                await File.WriteAllTextAsync(receipt,JsonSerializer.Serialize(result),ct); observations.Add(result);
            }
        }
        finally { await Context(oldLanguage,oldTheme); }
        if (original!=await ImportFinancialNativeReview.StoredAsync(services,ct)) throw new IOException("Independent base rows changed.");
        return new { Scope="forecast-missing-rate", Observations=observations, OriginalBaseUnchanged=true, FinancialSaves=0,
            Comparison=Platform.CurrentActivity?.Intent?.GetStringExtra("review_pass") };
    }
}
