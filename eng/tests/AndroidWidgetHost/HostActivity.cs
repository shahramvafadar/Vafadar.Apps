using System.Text.RegularExpressions;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Vafadar.Zanance.Verification.WidgetHost;

/// <summary>Hosts only the original installed Zanance RemoteViews through normal per-widget native consent.</summary>
[Activity(Name = "pro.vafadar.zanance.widgetreview.HostActivity", Label = "Zanance widget review",
    Exported = true, ExcludeFromRecents = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public sealed class HostActivity : Activity
{
    private const string HostPackage = "pro.vafadar.zanance.widgetreview";
    private const string SourcePackage = "pro.vafadar.zanance";
    private const string SourceClass = "pro.vafadar.zanance.QuickAddWidget";
    private const int HostId = 47901;
    private const int BindRequest = 479;
    private AppWidgetHost? _host;
    private AppWidgetManager? _manager;
    private ISharedPreferences? _preferences;
    private FrameLayout? _frame;
    private TextView? _status;
    private Button? _add;
    private Button? _remove;
    private string _run = string.Empty;

    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _run = Intent?.GetStringExtra("review_run") ?? string.Empty;
        if (PackageName != HostPackage || !Regex.IsMatch(_run, "^[0-9a-f]{32}$", RegexOptions.CultureInvariant))
        {
            Finish();
            return;
        }
        Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
        _preferences = GetSharedPreferences("owned-widget", FileCreationMode.Private) ?? throw new IOException();
        var previous = _preferences.GetString("run", null);
        if (previous is not null && previous != _run) { Finish(); return; }
        if (previous is null && !_preferences.Edit()!.PutString("run", _run)!.Commit()) throw new IOException();
        _host = new AppWidgetHost(this, HostId);
        _manager = AppWidgetManager.GetInstance(this) ?? throw new IOException();
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetPadding(Dp(16), Dp(48), Dp(16), Dp(24));
        _status = new TextView(this) { Text = "Original Zanance widget qualification" };
        _add = new Button(this) { Text = "Add original widget" };
        _remove = new Button(this) { Text = "Remove owned widget" };
        _frame = new FrameLayout(this);
        root.AddView(_status);
        root.AddView(_add);
        root.AddView(_remove);
        root.AddView(_frame, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(112)));
        SetContentView(root);
        _add.Click += (_, _) => Guarded(AddOriginalWidget);
        _remove.Click += (_, _) => Guarded(RemoveOwnedWidget);
        Guarded(RenderOriginalWidget);
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        base.OnStart();
        _host?.StartListening();
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        _host?.StopListening();
        base.OnStop();
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _host?.StopListening();
        _host?.Dispose();
        _host = null;
        base.OnDestroy();
    }

    /// <inheritdoc />
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != BindRequest) return;
        Guarded(() =>
        {
            if (resultCode != Result.Ok) { RemoveOwnedWidget(); return; }
            RenderOriginalWidget();
        });
    }

    /// <summary>Allocates once and delegates consent to the original platform binding activity.</summary>
    private void AddOriginalWidget()
    {
        if (WidgetId != AppWidgetManager.InvalidAppwidgetId) throw new InvalidOperationException();
        var provider = _manager!.GetInstalledProvidersForPackage(SourcePackage, Android.OS.Process.MyUserHandle())
            .Single(info => info.Provider?.PackageName == SourcePackage && info.Provider.ClassName == SourceClass);
        if (provider.Configure is not null) throw new InvalidOperationException();
        var id = _host!.AllocateAppWidgetId();
        if (!_preferences!.Edit()!.PutInt("widget", id)!.Commit()) { _host.DeleteAppWidgetId(id); throw new IOException(); }
        _add!.Enabled = false;
        _remove!.Enabled = true;
        if (_manager.BindAppWidgetIdIfAllowed(id, provider.Provider)) { RenderOriginalWidget(); return; }
        var consent = new Intent(AppWidgetManager.ActionAppwidgetBind);
        consent.PutExtra(AppWidgetManager.ExtraAppwidgetId, id);
        consent.PutExtra(AppWidgetManager.ExtraAppwidgetProvider, provider.Provider);
        StartActivityForResult(consent, BindRequest);
    }

    /// <summary>Inflates actual provider-owned RemoteViews; the framework retains all original PendingIntents.</summary>
    private void RenderOriginalWidget()
    {
        var id = WidgetId;
        _frame!.RemoveAllViews();
        _add!.Enabled = id == AppWidgetManager.InvalidAppwidgetId;
        _remove!.Enabled = !_add.Enabled;
        if (_add.Enabled) { Status("No owned widget"); return; }
        var info = _manager!.GetAppWidgetInfo(id);
        if (info is null) { Status("Awaiting native binding"); return; }
        if (info.Provider?.PackageName != SourcePackage || info.Provider.ClassName != SourceClass || info.Configure is not null)
            throw new InvalidOperationException();
        var view = _host!.CreateView(this, id, info) ?? throw new IOException();
        _frame.AddView(view, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        Status("Original widget ready");
    }

    /// <summary>Deletes only IDs belonging to this package's fixed host, without touching any launcher host.</summary>
    private void RemoveOwnedWidget()
    {
        _host!.DeleteHost();
        if (!_preferences!.Edit()!.Remove("widget")!.Commit()) throw new IOException();
        RenderOriginalWidget();
    }

    /// <summary>Reads only the host's own durable widget ID.</summary>
    private int WidgetId => _preferences!.GetInt("widget", AppWidgetManager.InvalidAppwidgetId);

    /// <summary>Publishes bounded synthetic host metadata, without financial or private application reads.</summary>
    private void Status(string text)
    {
        _status!.Text = text;
        var owned = _host!.GetAppWidgetIds() ?? throw new IOException();
        _status.ContentDescription = $"run={_run};widget={WidgetId};owned={owned.Length}";
    }

    /// <summary>Retains a refused native operation without recreating or replaying an accepted binding.</summary>
    private void Guarded(Action action)
    {
        try { action(); }
        catch (Exception error) { _status!.Text = "Review refused: " + error.GetType().Name; }
    }

    /// <summary>Converts local host dimensions without changing OS configuration.</summary>
    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density + 0.5f);
}
