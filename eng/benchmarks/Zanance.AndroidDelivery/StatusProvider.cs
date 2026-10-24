using Android.Content;
using Android.Database;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace Vafadar.Zanance.DeliveryReview;

/// <summary>Reads retained notification-only receipts without starting a window or publishing a request.</summary>
[ContentProvider([DeliveryApplication.Package + ".status"], Exported = true)]
public sealed class StatusProvider : ContentProvider
{
    /// <inheritdoc />
    public override bool OnCreate() => true;

    /// <inheritdoc />
    public override ICursor? Query(Android.Net.Uri uri, string[]? projection, string? selection, string[]? selectionArgs, string? sortOrder)
    {
        if (Context?.PackageName != DeliveryApplication.Package || uri.Path != "/status") throw new InvalidOperationException("Independent status required.");
        var root = FileSystem.AppDataDirectory;
        var result = new MatrixCursor(["receipt"]);
        var files = new[] { "started.json", "accepted-19994001.json", "accepted-19994002.json", "receipt.json", "failure.json" };
        var records = files.Where(name => File.Exists(Path.Combine(root, name))).ToDictionary(name => name,
            name => JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(root, name))));
        result.NewRow()?.Add(new Java.Lang.String(JsonSerializer.Serialize(new { Pid = Android.OS.Process.MyPid(), Records = records })));
        return result;
    }

    /// <inheritdoc />
    public override string? GetType(Android.Net.Uri uri) => "vnd.android.cursor.item/vnd.zanance.delivery-review";
    /// <inheritdoc />
    public override Android.Net.Uri? Insert(Android.Net.Uri uri, ContentValues? values) => throw new NotSupportedException();
    /// <inheritdoc />
    public override int Delete(Android.Net.Uri uri, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
    /// <inheritdoc />
    public override int Update(Android.Net.Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
}
