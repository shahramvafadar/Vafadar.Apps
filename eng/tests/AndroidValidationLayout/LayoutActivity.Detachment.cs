using Android.Views;
using Android.Widget;
using NativeLayout = Vafadar.Zanance.App.Presentation.AndroidValidationLayout;

public sealed partial class LayoutActivity
{
    /// <summary>Checks actual native detachment, collection and false completion without financial or OS effects.</summary>
    private async Task RunDetachAsync(bool includeAfter)
    {
        var result=new TextView(this) {Text="Owned native detach boundary"};
        try
        {
            var root=new LinearLayout(this) {Orientation=Orientation.Vertical};root.AddView(result);SetContentView(root);
            for(int i=0;i<40 && !root.IsAttachedToWindow;i++) await Task.Delay(50);
            if(!root.IsAttachedToWindow)throw new IOException("Native root unavailable.");
            foreach(var beforeAttach in includeAfter ? new[]{false,true} : new[]{true})
            {
                var target=new ScrollView(this);target.AddView(new TextView(this){Text="Fictitious layout only"});
                if(!beforeAttach)root.AddView(target);
                var initial=target.ViewTreeObserver!;
                var first=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var listener=new NativeLayout.LayoutListener(()=>first.TrySetResult());
                using var queue=new Java.Lang.Ref.ReferenceQueue();using var weak=new Java.Lang.Ref.WeakReference(listener,queue);
                if(!listener.Register(target))throw new IOException("Registration refused.");
                if(beforeAttach)root.AddView(target);
                target.RequestLayout();await Task.WhenAny(first.Task,Task.Delay(2000));
                if(!first.Task.IsCompletedSuccessfully)throw new IOException("Original callback unavailable.");
                var live=target.ViewTreeObserver!;
                var initialRetired=!initial.IsAlive;
                root.RemoveView(target);var current=target.ViewTreeObserver!;
                var changed=!live.Equals(current);var bothAlive=live.IsAlive&&current.IsAlive;
                Android.Util.Log.Info("ZananceDetachReview",$"stage=fixed-detach;beforeAttach={beforeAttach};changed={changed};bothAlive={bothAlive};initialRetired={initialRetired}");
                listener.Retire(target);listener.Dispose();
                // Android merge keeps listener arrays on the dead observer. Release this measurement's
                // own retired wrapper before using a ReferenceQueue to test the listener's collectibility.
                if(initialRetired)initial.Dispose();root.RequestLayout();
                var control=new NativeLayout.LayoutListener(()=>{});
                using var controlQueue=new Java.Lang.Ref.ReferenceQueue();using var controlWeak=new Java.Lang.Ref.WeakReference(control,controlQueue);control.Dispose();
                for(int i=0;i<3;i++){Java.Lang.JavaSystem.Gc();await Task.Delay(150);}
                using var collected=queue.Poll();using var controlCollected=controlQueue.Poll();
                var report=$"fixed=True;beforeAttach={beforeAttach};changed={changed};bothAlive={bothAlive};initialRetired={initialRetired};listenerCollected={collected is not null};controlCollected={controlCollected is not null};rootStillAlive={live.IsAlive&&root.IsAttachedToWindow}";
                Android.Util.Log.Info("ZananceDetachReview",report);result.Text=report;
                if(!changed||!bothAlive||initialRetired!=beforeAttach||collected is null||controlCollected is null)throw new IOException("Native collection contract refused.");
            }
            {
                var target=new ScrollView(this);
                var initial=target.ViewTreeObserver!;
                var callbacks=0;
                var listener=new NativeLayout.LayoutListener(()=>callbacks++);
                using var queue=new Java.Lang.Ref.ReferenceQueue();using var weak=new Java.Lang.Ref.WeakReference(listener,queue);
                if(!listener.Register(target))throw new IOException("Pre-frame registration refused.");
                root.AddView(target);var live=target.ViewTreeObserver!;root.RemoveView(target);
                var changed=!live.Equals(target.ViewTreeObserver);var initialRetired=!initial.IsAlive;
                listener.Retire(target);listener.Dispose();
                // Android merge keeps listener arrays on the dead observer. Release this measurement's
                // own retired wrapper before using a ReferenceQueue to test the listener's collectibility.
                if(initialRetired)initial.Dispose();root.RequestLayout();
                var control=new NativeLayout.LayoutListener(()=>{});
                using var controlQueue=new Java.Lang.Ref.ReferenceQueue();using var controlWeak=new Java.Lang.Ref.WeakReference(control,controlQueue);control.Dispose();
                for(int i=0;i<3;i++){Java.Lang.JavaSystem.Gc();await Task.Delay(150);}
                using var collected=queue.Poll();using var controlCollected=controlQueue.Poll();
                var report=$"beforeFirstLayout=True;callbacks={callbacks};changed={changed};initialRetired={initialRetired};listenerCollected={collected is not null};controlCollected={controlCollected is not null};rootStillAlive={live.IsAlive&&root.IsAttachedToWindow}";
                Android.Util.Log.Info("ZananceDetachReview",report);result.Text=report;
                if(callbacks!=0||!changed||!initialRetired||collected is null||controlCollected is null||!live.IsAlive)throw new IOException("Pre-frame collection contract refused.");
            }
            foreach(var disposeTarget in new[]{false,true})
            {
                var target=new ScrollView(this);root.AddView(target);
                var waiting=NativeLayout.WaitAsync(target);root.RemoveView(target);
                if(disposeTarget)target.Dispose();
                var detachedReturnsFalse=!await waiting.WaitAsync(TimeSpan.FromSeconds(2));
                root.RequestLayout();await Task.Delay(150);
                var report=$"waitDetached=True;disposeTarget={disposeTarget};returnsFalse={detachedReturnsFalse}";
                Android.Util.Log.Info("ZananceDetachReview",report);result.Text=report;
                if(!detachedReturnsFalse)throw new IOException("Retired native wait accepted.");
            }
            Android.Util.Log.Info("ZananceDetachReview",$"complete=True;cases={(includeAfter?5:4)}");result.Text=$"complete=True;cases={(includeAfter?5:4)}";
        }
        catch(Exception error)when(error is not OutOfMemoryException){Android.Util.Log.Error("ZananceDetachReview","refused="+error.GetType().FullName);result.Text="Refused: "+error.GetType().Name;}
    }
}
