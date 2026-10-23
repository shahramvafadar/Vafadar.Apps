using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Vafadar.Zanance.App.Presentation;

/// <summary>Exercises original toolbar observation across independent native window trees.</summary>
[Activity(Name="pro.vafadar.zanance.toolbarreview.ToolbarActivity", Exported=true, Theme="@android:style/Theme.Material.Light.NoActionBar")]
public sealed class ToolbarActivity : Activity
{
    private int _callbacks;

    /// <inheritdoc />
    protected override void OnCreate(Bundle? state)
    {
        base.OnCreate(state);
        if (PackageName != "pro.vafadar.zanance.toolbarreview" || Intent?.GetStringExtra("review_run") is not ("d485" or "d485-before")) { Finish(); return; }
        Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        _ = RunAsync(Intent?.GetStringExtra("review_run") == "d485-before");
    }

    /// <summary>Moves one toolbar to a second actual window, then retires its original listener.</summary>
    private async Task RunAsync(bool beforeAttachment)
    {
        try
        {
            var root = new LinearLayout(this) { Orientation=Orientation.Vertical };
            var status = new TextView(this) { Text="Original toolbar lifetime" };
            root.AddView(status); SetContentView(root);
            for (var i=0; i<40 && !root.IsAttachedToWindow; i++) await Task.Delay(50);
            if (!root.IsAttachedToWindow) throw new IOException("Original root unavailable.");
            var toolbar = new Toolbar(this) { Title="Fictitious toolbar" };
            if (!beforeAttachment) root.AddView(toolbar);
            var listener = new AndroidToolbarLayoutObserver<ToolbarActivity>(this, static owner => owner._callbacks++);
            listener.Register(toolbar);
            if (beforeAttachment) root.AddView(toolbar);
            toolbar.RequestLayout(); await Task.Delay(200);
            var initial = _callbacks;
            if (initial == 0) throw new IOException("Initial callback missing.");
            root.RemoveView(toolbar); await Task.Delay(100);
            var detachedStart = _callbacks;
            root.RequestLayout(); await Task.Delay(200);
            var detachedCallbacks = _callbacks-detachedStart;
            using var dialog = new Dialog(this);
            var second = new LinearLayout(this) { Orientation=Orientation.Vertical };
            second.AddView(toolbar); dialog.SetContentView(second); dialog.Show(); await Task.Delay(200);
            var movedStart = _callbacks;
            toolbar.RequestLayout(); await Task.Delay(200);
            var movedCallbacks = _callbacks-movedStart;
            root.RequestLayout(); await Task.Delay(200);
            listener.Retire(toolbar);
            var retiredStart = _callbacks;
            toolbar.RequestLayout(); root.RequestLayout(); await Task.Delay(200);
            var retiredCallbacks = _callbacks-retiredStart;
            var report=$"beforeAttachment={beforeAttachment};initial={initial};detachedCallbacks={detachedCallbacks};movedCallbacks={movedCallbacks};retiredCallbacks={retiredCallbacks};firstRootAlive={root.IsAttachedToWindow};secondRootAlive={second.IsAttachedToWindow};complete=True";
            Android.Util.Log.Info("ZananceToolbarReview",report); status.Text=report;
            dialog.Dismiss();
            if (detachedCallbacks!=0 || movedCallbacks==0 || retiredCallbacks!=0) throw new IOException("Original toolbar lifetime refused.");
            Android.Util.Log.Info("ZananceToolbarReview","accepted=True");
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { Android.Util.Log.Error("ZananceToolbarReview","refused="+error.GetType().FullName); }
    }
}
