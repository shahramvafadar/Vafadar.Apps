using Android.Content;
using Android.Database;

namespace Vafadar.Zanance.App;

/// <summary>Exposes only synthetic readiness/counts from the independent fixture, without creating a window.</summary>
[ContentProvider([NativeReminderReview.Package + ".status"], Exported = true)]
public sealed class StatusProvider : ContentProvider
{
    /// <inheritdoc />
    public override bool OnCreate() => true;

    /// <inheritdoc />
    public override ICursor? Query(Android.Net.Uri uri, string[]? projection, string? selection,
        string[]? selectionArgs, string? sortOrder)
    {
        if (Context?.PackageName == NativeReminderReview.Package && uri.Path == "/profile-scope-status")
        {
            // D-493: report actual SDK tap/source/page evidence only; never create a source, switch or navigate.
            var result = new MatrixCursor(["receipt"]);
            var services = IPlatformApplication.Current?.Services;
            var receipt = services is null ? "{}" : Task.Run(() => NativeGoalProfileScopeReview.ReadStatusAsync(services)).GetAwaiter().GetResult();
            result.NewRow()?.Add(new Java.Lang.String(receipt));
            return result;
        }
        if (Context?.PackageName == NativeReminderReview.Package && uri.Path == "/route-status")
        {
            // D-492: observing an already started original route never prepares storage or posts a notification.
            var result = new MatrixCursor(["receipt"]);
            var services = IPlatformApplication.Current?.Services;
            var receipt = services is null ? "{}" : Task.Run(() => NativeGoalPeriodRouteReview.ReadStatusAsync(services)).GetAwaiter().GetResult();
            result.NewRow()?.Add(new Java.Lang.String(receipt));
            return result;
        }
        if (Context?.PackageName == NativeReminderReview.Package && uri.Path == "/goal-variants-status")
        {
            // D-491: observation never prepares a source or executes a metadata transition.
            var result = new MatrixCursor(["receipt"]);
            result.NewRow()?.Add(new Java.Lang.String(NativeGoalVariantReminderReview.ReadReceipt()));
            return result;
        }
        if (Context?.PackageName == NativeReminderReview.Package && uri.Path == "/period-status")
        {
            // D-489: a receipt query is read-only and never prepares the original protected source.
            var result = new MatrixCursor(["receipt"]);
            result.NewRow()?.Add(new Java.Lang.String(NativePeriodReminderReview.ReadReceipt()));
            return result;
        }
        if (Context?.PackageName == NativeReminderReview.Package && uri.Path == "/goal-status")
        {
            // D-488: read only the synthetic receipt; never initialize storage or rerun a native step.
            var result = new MatrixCursor(["receipt"]);
            var receipt = NativeGoalReminderReview.ReadReceipt();
            result.NewRow()?.Add(new Java.Lang.String(receipt));
            return result;
        }
        var cursor = new MatrixCursor(["run", "pid", "windows", "protected", "snoozes", "pendingSnoozes", "financialMatch", "errorType", "pendingPlans", "scheduledAt", "plannedId"]);
        string[] values;
        try
        {
            if (Context?.PackageName != NativeReminderReview.Package || uri.Path != "/status")
                throw new IOException("Independent read-only status required.");
            var services = IPlatformApplication.Current?.Services;
            values = services is null ? ["starting", "0", "0", "False", "0", "0", "False", "none", "0", "none", "0"]
                : Task.Run(() => NativeReminderReview.ReadStatusAsync(services)).GetAwaiter().GetResult();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            values = ["unavailable", "0", "0", "False", "0", "0", "False", error.GetType().Name, "0", "none", "0"];
        }
        var row = cursor.NewRow() ?? throw new InvalidOperationException("Status row was not allocated.");
        foreach (var value in values) row.Add(new Java.Lang.String(value));
        return cursor;
    }

    /// <inheritdoc />
    public override string? GetType(Android.Net.Uri uri) => "vnd.android.cursor.item/vnd.zanance.reminder-review";

    /// <inheritdoc />
    public override Android.Net.Uri? Insert(Android.Net.Uri uri, ContentValues? values) => throw new NotSupportedException();

    /// <inheritdoc />
    public override int Update(Android.Net.Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => throw new NotSupportedException();

    /// <inheritdoc />
    public override int Delete(Android.Net.Uri uri, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
}
