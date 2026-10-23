using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Inspects original comparison labels, amount packets and reversal colors in actual owned native windows.</summary>
    private static async Task<object> ReviewSnapshotDifferenceAsync(App app,IServiceProvider services,string root,CancellationToken ct)
    {
        await AwaitOriginalMoneyWindowAsync(ct);app.OpenReviewShell();await AwaitRootHomeAsync(ct);
        var themes=services.GetRequiredService<ThemeService>();var local=services.GetRequiredService<ILocalizationService>();
        var oldTheme=themes.Choice;var oldLanguage=local.CurrentLanguage.CultureName;
        async Task Context(string language,ThemeChoice theme,int width)
        {
            if (themes.Choice!=theme)
            {
                var rebuilt=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Rebuilt(object? sender,EventArgs args)=>rebuilt.TrySetResult();
                app.ShellRebuilt+=Rebuilt;
                try { themes.Set(theme);await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30),ct); }
                finally { app.ShellRebuilt-=Rebuilt; }
            }
            await SetLanguageAsync(app,local,language,ct);await AwaitRootHomeAsync(ct);
        }
        try
        {
            var pixels=Platform.CurrentActivity?.Window?.DecorView?.Width ?? 0;
            var density=Platform.CurrentActivity?.Resources?.DisplayMetrics?.Density ?? 0;
            if (pixels<=0 || density<=0) throw new ReviewCheckException("Original native comparison width unavailable.");
            var width=(int)Math.Round(pixels/density,MidpointRounding.AwayFromZero);
            var result=await SnapshotDifferenceNativeReview.RunAsync(services,root,[width],Context,async (page,key)=>
            {
                await PlanEditorNativeReview.WaitAsync(()=>PlanEditorNativeReview.Views(page).OfType<Label>().Any(label=>
                    label.Handler?.PlatformView is Android.Widget.TextView { IsAttachedToWindow:true,HasWindowFocus:true,Height:>0 }),ct);
                var viewport=PlanEditorNativeReview.Views(page).OfType<ScrollView>().Single(view=>view.Orientation==ScrollOrientation.Vertical);
                var targets=SnapshotDifferenceNativeReview.Targets(page);
                foreach (var target in targets)
                {
                    await viewport.ScrollToAsync(target,ScrollToPosition.Center,false);await Task.Delay(80,ct);
                    if (target.Handler?.PlatformView is not Android.Widget.TextView { IsShown:true,HasWindowFocus:true } native
                        || native.Layout is not { } layout || layout.Height>native.Height-native.TotalPaddingTop-native.TotalPaddingBottom+1)
                        throw new ReviewCheckException("Original comparison caption is clipped.");
                    using var visible=new Android.Graphics.Rect();
                    if (!native.GetGlobalVisibleRect(visible) || visible.Height()<native.Height-1)
                        throw new ReviewCheckException("Original comparison caption is outside the owned viewport.");
                    for (var line=0;line<layout.LineCount;line++)
                        if (layout.GetEllipsisCount(line)!=0) throw new ReviewCheckException("Original comparison caption is truncated.");
                    if (target.BindingContext is AmountLine part && part.Label==Translator.Instance["Snapshot_UnplannedIncome"] && target.Text==part.Amount)
                    {
                        var actual=new Android.Graphics.Color(native.CurrentTextColor);var expected=Palette.ExpenseText;
                        if (Math.Abs(actual.R-expected.Red*255)>1 || Math.Abs(actual.G-expected.Green*255)>1 || Math.Abs(actual.B-expected.Blue*255)>1)
                            throw new ReviewCheckException("Original negative income reversal lost its native danger color.");
                    }
                }
                Android.Util.Log.Info("ZananceHomeStage","snapshot-difference|"+key+"|captions="+targets.Count);
            },ct);
            var report=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(JsonSerializer.Serialize(result))!;
            report["Comparison"]=JsonSerializer.SerializeToElement(Platform.CurrentActivity?.Intent?.GetStringExtra("review_pass"));return report;
        }
        finally { await Context(oldLanguage,oldTheme,412); }
    }
}
