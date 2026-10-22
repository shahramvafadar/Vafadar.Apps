using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using NativeValidationLayout = Vafadar.Zanance.App.Presentation.AndroidValidationLayout;

/// <summary>Qualifies original native layout cleanup with fictitious views only.</summary>
[Activity(Name="pro.vafadar.zanance.layoutreview.LayoutActivity", Exported=true, Theme="@android:style/Theme.Material.Light.NoActionBar")]
public sealed class LayoutActivity : Activity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? state)
    {
        base.OnCreate(state);
        if(PackageName!="pro.vafadar.zanance.layoutreview" || Intent?.GetStringExtra("review_run")!="d480-fixed-observer") { Finish(); return; }
        Window?.SetFlags(WindowManagerFlags.Secure,WindowManagerFlags.Secure);
        _ = RunAsync();
    }
    /// <summary>Uses the original listener and waiter through observer replacement and a real native timeout.</summary>
    private async Task RunAsync()
    {
        var text=new TextView(this) {Text="Checking original current-tree cleanup"};
        var root=new LinearLayout(this) {Orientation=Orientation.Vertical}; root.AddView(text);
        var scroll=new ScrollView(this); scroll.AddView(new TextView(this) {Text="Fictitious native layout only"}); root.AddView(scroll);
        var original=scroll.ViewTreeObserver!;
        var first=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbacks=0,afterCleanup=0; bool cleanup=false;
        using var listener=new NativeValidationLayout.LayoutListener(()=>{callbacks++;if(cleanup)afterCleanup++;first.TrySetResult();});
        original.AddOnGlobalLayoutListener(listener);
        SetContentView(root); scroll.RequestLayout();
        await Task.WhenAny(first.Task,Task.Delay(2000));
        var retired=!original.IsAlive;
        listener.Retire(scroll); cleanup=true;
        var before=callbacks;
        for(int i=0;i<3;i++){scroll.RequestLayout();await Task.Delay(150);}
        var currentAlive=scroll.ViewTreeObserver?.IsAlive==true;
        var arranged=await NativeValidationLayout.WaitAsync(scroll);
        var unattached=new ScrollView(this);
        var timedOut=!await NativeValidationLayout.WaitAsync(unattached);
        var report=$"fixed=True;completed={first.Task.IsCompletedSuccessfully};retired={retired};currentAlive={currentAlive};before={before};afterCleanup={afterCleanup};actualWait={arranged};boundedTimeout={timedOut}";
        Android.Util.Log.Info("ZananceLayoutReview",report); text.Text=report;
    }
}
