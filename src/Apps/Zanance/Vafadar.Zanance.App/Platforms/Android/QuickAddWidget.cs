using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace Vafadar.Zanance.App;

/// <summary>
/// The quick add home-screen widget (D-30): the symbol opens the app, the buttons open a new expense, income or
/// transfer. It shows no amounts and reads no data, so it needs no permission and exposes nothing on the home screen;
/// the app lock still applies before the editor opens.
/// </summary>
// A stable class name, so widgets on the home screen survive updates of the app.
[BroadcastReceiver(Name = "pro.vafadar.zanance.QuickAddWidget", Label = "Zanance", Exported = true)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_quick_add_info")]
public sealed class QuickAddWidget : AppWidgetProvider
{
    /// <summary>The intent action of a widget tap.</summary>
    public const string Action = "pro.vafadar.zanance.QUICK_ADD";

    /// <summary>The intent extra with the entry kind (Expense, Income, Transfer) or nothing to just open the app.</summary>
    public const string KindExtra = "kind";

    /// <inheritdoc />
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null)
        {
            return;
        }

        var views = new RemoteViews(context.PackageName, Resource.Layout.widget_quick_add);
        views.SetOnClickPendingIntent(Resource.Id.widget_logo, Open(context, null, 0));
        views.SetOnClickPendingIntent(Resource.Id.widget_expense, Open(context, "Expense", 1));
        views.SetOnClickPendingIntent(Resource.Id.widget_income, Open(context, "Income", 2));
        views.SetOnClickPendingIntent(Resource.Id.widget_transfer, Open(context, "Transfer", 3));
        appWidgetManager.UpdateAppWidget(appWidgetIds, views);
    }

    /// <summary>Redraws placed widgets, e.g. after the app language changed (their labels follow the app language).</summary>
    public static void Refresh(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var manager = AppWidgetManager.GetInstance(context);
        var ids = manager?.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(QuickAddWidget))));
        if (manager is not null && ids is { Length: > 0 })
        {
            new QuickAddWidget().OnUpdate(context, manager, ids);
        }
    }

    private static PendingIntent? Open(Context context, string? kind, int requestCode)
    {
        var intent = new Intent(context, typeof(MainActivity)).SetAction(Action).AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        if (kind is not null)
        {
            intent.PutExtra(KindExtra, kind);
        }

        return PendingIntent.GetActivity(context, requestCode, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
    }
}
