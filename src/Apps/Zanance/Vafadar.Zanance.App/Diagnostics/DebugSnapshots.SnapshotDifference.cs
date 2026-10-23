#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies the original cash-difference captions on the independently owned non-input desktop.</summary>
    private static async Task ReviewSnapshotDifferenceWindowsAsync(App app, IServiceProvider services, string folder)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(10));var ct=timeout.Token;
        var local=services.GetRequiredService<ILocalizationService>();var themes=services.GetRequiredService<ThemeService>();
        var oldLanguage=local.CurrentLanguage.CultureName;var oldTheme=themes.Choice;var oldWidth=app.Windows[0].Width;
        var captures=new List<object>();
        async Task Scroll(Label target)
        {
            var viewport=PlanEditorNativeReview.Views((Page)Shell.Current.Navigation.ModalStack.Last()).OfType<ScrollView>()
                .Single(view=>view.Orientation==ScrollOrientation.Vertical);
            if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
                || target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement field)
                throw new IOException("Original comparison native viewport required.");
            var position=field.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point());
            var wanted=Math.Clamp(native.VerticalOffset+position.Y-native.ViewportHeight/2,0,native.ScrollableHeight);
            if (native.ScrollableHeight>0 && Math.Abs(wanted-native.VerticalOffset)>1)
            {
                if (FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Scroll) is not IScrollProvider scroll)
                    throw new IOException("Original native scroll pattern unavailable.");
                scroll.SetScrollPercent(-1,wanted*100/native.ScrollableHeight);
            }
            await Task.Delay(100,ct);
        }
        try
        {
            var result=await SnapshotDifferenceNativeReview.RunAsync(services,folder,[360,412,1280],async (language,theme,width)=>
            {
                await SetStoreThemeAsync(app,themes,theme);await SetLanguageAsync(app,local,language);
                app.Windows[0].Width=width;app.Windows[0].Height=800;await Task.Delay(200,ct);
            },async (page,key)=>
            {
                await PlanEditorNativeReview.WaitAsync(()=>PlanEditorNativeReview.Views(page).OfType<Label>()
                    .Any(label=>label.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement { ActualHeight:>0 }),ct);
                var targets=SnapshotDifferenceNativeReview.Targets(page);
                for (var index=0;index<targets.Count;index++)
                {
                    var target=targets[index];await Scroll(target);CheckCaption(target);
                    if (index==0 || index==targets.Count-1)
                    {
                        var file=key+(index==0 ? "-parts.png" : "-note.png");
                        await CaptureWindowAsync(app.Windows[0],Path.Combine(folder,file));captures.Add(new { File=file });
                    }
                }
            },ct);
            await File.WriteAllTextAsync(Path.Combine(folder,"complete.json"),JsonSerializer.Serialize(new
            { Package=AppInfo.Current.PackageName,ProcessId=Environment.ProcessId,Scope="snapshot-difference",Captures=captures,
                OriginalBaseStoreUnchanged=true,FinancialSaves=0,Evidence=result }),ct);
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder,"original-failure.txt"),error.ToString(),CancellationToken.None);throw; }
        finally
        {
            try { app.Windows[0].Width=oldWidth;await SetStoreThemeAsync(app,themes,oldTheme);await SetLanguageAsync(app,local,oldLanguage); }
            catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder,"cleanup-failure.txt"),error.ToString(),CancellationToken.None); }
        }
    }
}
#endif
