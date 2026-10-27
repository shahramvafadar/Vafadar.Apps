using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.NotificationDelivery;

/// <summary>Independent SDK-only paired receiver diagnosis; never references a data store or production application.</summary>
internal static class DeliveryReview
{
    /// <summary>Only the new SDK-only fixture may expose these effects.</summary>
    internal const string Package = "pro.vafadar.zanance.notificationdeliveryreview";
    /// <summary>Only bounded records from this independent process are emitted.</summary>
    internal const string Tag = "ZananceDeliveryReview";
    /// <summary>The original failed goal id, safe to reuse only in this different owned UID.</summary>
    internal const int GoalId = 1920408283;
    /// <summary>The original paired review id, independent from every other application's queue.</summary>
    internal const int ReviewId = 1071083569;
    private static int _records;
    /// <summary>Distinguishes a receiver-created process from an Activity-created source.</summary>
    internal static int Windows;
    private static string Root => Android.App.Application.Context.FilesDir!.AbsolutePath;
    /// <summary>Durable single-preparation guard for only this new nonfinancial fixture.</summary>
    internal static string Marker => Path.Combine(Root, "paired-delivery.json");

    /// <summary>Refuse hardware and every other emulator/API before any review effect.</summary>
    internal static void RequireTarget()
    {
        if (Android.App.Application.Context.PackageName != Package || (int)Build.VERSION.SdkInt != 35
            || Build.Model != "sdk_gphone16k_x86_64" || Build.SupportedAbis?.FirstOrDefault() != "x86_64")
            throw new InvalidOperationException("Only the distinct owned API35 x64 emulator cohort is admitted.");
    }

    /// <summary>Keep bounded own SDK errors/messages, without private stacks, registers, credentials or other applications.</summary>
    internal static void Emit(object value)
    {
        if (Interlocked.Increment(ref _records) > 256) return;
        Android.Util.Log.Info(Tag, JsonSerializer.Serialize(value));
    }

    /// <summary>Arm exactly two real future SDK requests once, through the unchanged original request factory.</summary>
    internal static async Task ArmAsync(string run)
    {
        RequireTarget();
        if (!System.Text.RegularExpressions.Regex.IsMatch(run, "^[0-9a-f]{32}$") || File.Exists(Marker))
            throw new InvalidOperationException("A fresh explicit run is required; never replay accepted arming.");
        var service = LocalNotificationCenter.Current;
        if ((await service.GetPendingNotificationList()).Count != 0)
            throw new InvalidOperationException("Retain existing requests; this fresh cohort must start empty.");
        var at = DateTime.Now.AddMinutes(3);
        await File.WriteAllTextAsync(Marker, JsonSerializer.Serialize(new { Run = run, Stage = "arming", Due = at }));
        using var stream = Android.App.Application.Context.Assets!.Open("captions/AppResources.resx");
        var captions = XDocument.Load(stream).Root!.Elements("data").ToDictionary(x => x.Attribute("name")!.Value, x => x.Element("value")!.Value);
        var scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(run))).ToLowerInvariant();
        var link = GoalReminderLink.Format(scope, [new(Guid.NewGuid(), DateOnly.FromDateTime(at))]);
        var rows = new[]
        {
            new ReminderNotification(GoalId, captions["App_Name"], captions["Reminder_GoalGeneric"], at, link),
            new ReminderNotification(ReviewId, captions["App_Name"], captions["Reminder_ReviewGeneric"], at, "review"),
        };
        foreach (var row in rows)
        {
            var request = LocalNotificationScheduler.Request(row);
            Emit(new { Kind = "before-sdk-arm", request.NotificationId, request.Schedule.NotifyTime,
                AllowedDelay = request.Schedule.Android.AllowedDelay.ToString(), request.Schedule.RepeatType,
                ReturningDataLength = request.ReturningData?.Length, Mode = request.Schedule.Android.ScheduleMode.ToString() });
            if (!await service.Show(request)) throw new InvalidOperationException("Original SDK rejected the request.");
        }
        var pending = await service.GetPendingNotificationList();
        if (!pending.Select(x => x.NotificationId).Order().SequenceEqual(new[] { ReviewId, GoalId }))
            throw new InvalidOperationException("Only the two reviewed native ids may be pending.");
        await File.WriteAllTextAsync(Marker, JsonSerializer.Serialize(new { Run = run, Stage = "armed", Due = at,
            PreparedPid = Android.OS.Process.MyPid(), GoalId, ReviewId, NoFinancialSource = true }));
        Emit(new { Kind = "armed", Run = run, Pid = Android.OS.Process.MyPid(), Due = at });
    }

    /// <summary>Read only this synthetic SDK state; never arm, reschedule, open a window or touch financial storage.</summary>
    internal static async Task<string> ReadAsync()
    {
        RequireTarget();
        var service = LocalNotificationCenter.Current;
        var pending = await service.GetPendingNotificationList();
        var delivered = await service.GetDeliveredNotificationList();
        return JsonSerializer.Serialize(new { Pid = Android.OS.Process.MyPid(), Windows,
            Marker = File.Exists(Marker) ? JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(Marker)) : (JsonElement?)null,
            Pending = pending.Select(x => new { x.NotificationId, x.Schedule.NotifyTime,
                AllowedDelay = x.Schedule.Android.AllowedDelay.ToString(), x.Schedule.RepeatType, ReturningDataLength = x.ReturningData?.Length }),
            Delivered = delivered.Select(x => x.NotificationId), NoFinancialSource = true });
    }
}

/// <summary>Configure only the separate SDK logger on every cold process; do not eagerly initialize its service.</summary>
[Application(Name = DeliveryReview.Package + ".ReviewApplication")]
public sealed class ReviewApplication(IntPtr handle, JniHandleOwnership ownership) : Android.App.Application(handle, ownership)
{
    /// <inheritdoc />
    public override void OnCreate()
    {
        base.OnCreate();
        DeliveryReview.RequireTarget();
        LocalNotificationLogger.Logger = new SdkLogger();
        LocalNotificationLogger.LogLevel = LogLevel.Trace;
        DeliveryReview.Emit(new { Kind = "application-start", Pid = Android.OS.Process.MyPid(), Windows = 0 });
    }

    private sealed class SdkLogger : ILogger
    {
        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        /// <inheritdoc />
        public bool IsEnabled(LogLevel level) => true;
        /// <inheritdoc />
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (message.Length > 512) message = message[..512];
            var error = exception?.Message;
            if (error?.Length > 512) error = error[..512];
            DeliveryReview.Emit(new { Kind = "sdk-log", Level = level.ToString(), Message = message,
                ErrorType = exception?.GetType().FullName, Error = error });
        }
    }
}

/// <summary>One explicit nonfinancial foreground preparation; normal later launches cannot rearm it.</summary>
[Activity(Name = DeliveryReview.Package + ".MainActivity", MainLauncher = true, Exported = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public sealed class MainActivity : Activity
{
    private string? _run;
    private bool _started;
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        DeliveryReview.RequireTarget();
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.Secure);
        Interlocked.Increment(ref DeliveryReview.Windows);
        SetContentView(new TextView(this) { Text = "Independent notification delivery review. No accounts or financial source." });
        _run = savedInstanceState is null ? Intent?.GetStringExtra("review_run") : null;
        if (_run is null) return;
        if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            RequestPermissions([Android.Manifest.Permission.PostNotifications], 580);
        else StartOnce();
    }

    /// <inheritdoc />
    protected override void OnResume()
    {
        base.OnResume();
        if (_run is not null && CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted) StartOnce();
    }

    /// <summary>Preserve a refused or accepted preparation rather than retrying a configuration callback.</summary>
    private async void StartOnce()
    {
        if (_started || _run is null) return;
        _started = true;
        try { await DeliveryReview.ArmAsync(_run); }
        catch (Exception error) { DeliveryReview.Emit(new { Kind = "prepare-refused", ErrorType = error.GetType().FullName, error.Message }); }
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Interlocked.Decrement(ref DeliveryReview.Windows);
        base.OnDestroy();
    }
}

/// <summary>Privileged-shell-only synthetic readback, with no preparation or financial effect.</summary>
[ContentProvider([DeliveryReview.Package + ".status"], Exported = true, ReadPermission = Android.Manifest.Permission.Dump)]
public sealed class StatusProvider : ContentProvider
{
    /// <inheritdoc />
    public override bool OnCreate() => true;
    /// <inheritdoc />
    public override ICursor? Query(Android.Net.Uri uri, string[]? projection, string? selection, string[]? selectionArgs, string? sortOrder)
    {
        DeliveryReview.RequireTarget();
        if (uri.Path != "/status") throw new InvalidOperationException("Only the read-only synthetic status is supported.");
        var cursor = new MatrixCursor(["receipt"]);
        cursor.NewRow()?.Add(new Java.Lang.String(Task.Run(DeliveryReview.ReadAsync).GetAwaiter().GetResult()));
        return cursor;
    }
    /// <inheritdoc />
    public override string? GetType(Android.Net.Uri uri) => null;
    /// <inheritdoc />
    public override Android.Net.Uri? Insert(Android.Net.Uri uri, ContentValues? values) => throw new NotSupportedException();
    /// <inheritdoc />
    public override int Delete(Android.Net.Uri uri, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
    /// <inheritdoc />
    public override int Update(Android.Net.Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => throw new NotSupportedException();
}
