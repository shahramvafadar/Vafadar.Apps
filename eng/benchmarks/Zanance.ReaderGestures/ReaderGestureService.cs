using System.Text.Json;
using System.Xml.Linq;
using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views.Accessibility;

namespace Vafadar.Zanance.ReaderGestures;

/// <summary>Bounds public accessibility gestures to the authorized emulator's original fictitious application window.</summary>
[Service(Name = Component, Exported = true, Permission = Android.Manifest.Permission.BindAccessibilityService)]
[IntentFilter(["android.accessibilityservice.AccessibilityService"])]
[MetaData("android.accessibilityservice", Resource = "@xml/reader_gestures")]
public sealed class ReaderGestureService : AccessibilityService
{
    /// <summary>Explicit independent service identity; never part of the application package.</summary>
    public const string Component = "pro.vafadar.zanance.readergesturereview.ReaderGestureService";
    /// <summary>Only this original application may receive a reviewed gesture.</summary>
    private const string Owner = "pro.vafadar.zanance";
    /// <summary>Public, bounded diagnostic outcomes without arbitrary windows or private files.</summary>
    internal const string LogTag = "ZananceReaderGesture";
    private static ReaderGestureService? _current;
    private readonly HashSet<string> _safeCaptions = new(StringComparer.Ordinal);
    private readonly Handler _handler = new(Looper.MainLooper!);
    private bool _busy;
    private int _commands;
    private long _connected;

    /// <inheritdoc />
    protected override void OnServiceConnected()
    {
        base.OnServiceConnected();
        if (Build.Hardware is not ("ranchu" or "goldfish")) return;
        var keys = new HashSet<string>(["Entry_NewExpense", "Common_Cancel", "Tab_More", "About_Title", "Common_Back", "Tab_Home"], StringComparer.Ordinal);
        foreach (var file in Assets!.List("captions") ?? [])
        {
            using var stream = Assets.Open("captions/" + file);
            foreach (var entry in XDocument.Load(stream).Root!.Elements("data"))
                if (keys.Contains((string?)entry.Attribute("name") ?? "") && entry.Element("value")?.Value is { Length: > 0 } caption)
                    _safeCaptions.Add(caption);
        }
        if (_safeCaptions.Count < keys.Count) throw new InvalidOperationException("Original safe captions unavailable.");
        _commands = 0; _connected = SystemClock.ElapsedRealtime(); _current = this;
        Emit(new { Kind = "ready", PublicDispatchGesture = true, RequestsTouchExploration = false });
    }

    /// <inheritdoc />
    public override void OnAccessibilityEvent(AccessibilityEvent? e)
    {
        if (e?.EventType != EventTypes.ViewAccessibilityFocused || e.PackageName?.ToString() != Owner) return;
        using var node = e.Source;
        if (node is not null) Emit(new { Kind = "actual-accessibility-focus", Node = Describe(node), ElapsedMs = SystemClock.ElapsedRealtime() });
    }

    /// <inheritdoc />
    public override void OnInterrupt() { }

    /// <inheritdoc />
    public override void OnDestroy()
    {
        if (ReferenceEquals(_current, this)) _current = null;
        base.OnDestroy();
    }

    /// <summary>Accepts a finite shell-protected operation; serializes actual gestures and refuses stale windows/focus.</summary>
    internal static void Receive(Intent intent)
    {
        var service = _current;
        if (service is null) { Emit(new { Kind = "refused", Reason = "service-unavailable" }); return; }
        try { service.Execute(intent); }
        catch (Exception error) { Emit(new { Kind = "refused", ErrorType = error.GetType().FullName }); }
    }

    /// <summary>Dispatches bounded reader navigation/exploration or reviewed nonfinancial activation; never performs node actions.</summary>
    private void Execute(Intent intent)
    {
        var id = intent.GetStringExtra("request") ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[0-9a-f]{32}$") || ++_commands > 256
            || SystemClock.ElapsedRealtime() - _connected > 10 * 60 * 1000 || _busy)
            throw new InvalidOperationException("Finite serialized original review required.");
        using var root = RootInActiveWindow;
        if (root is null || root.PackageName?.ToString() != Owner || !root.VisibleToUser || root.Window?.IsFocused != true)
            throw new InvalidOperationException("Original application window is not focused.");
        var tree = Snapshot(root);
        var operation = intent.GetStringExtra("operation");
        if (operation == "snapshot") { Emit(new { Kind = "snapshot", Request = id, Root = tree }); return; }
        if (intent.GetIntExtra("window", -1) != root.WindowId) throw new InvalidOperationException("Stale original window.");
        var nodes = Flatten(tree).ToArray();
        var focused = nodes.Where(value => value.AccessibilityFocused).ToArray();
        using var path = new Android.Graphics.Path();
        var bounds = tree.Bounds;
        if (bounds[2] - bounds[0] < 200 || bounds[3] - bounds[1] < 200) throw new InvalidOperationException("Original viewport unavailable.");
        var x = (bounds[0] + bounds[2]) / 2f; var y = (bounds[1] + bounds[3]) / 2f;
        using var gesture = new GestureDescription.Builder();
        using var secondPath = new Android.Graphics.Path();
        if (operation is "next" or "previous")
        {
            var direction = operation == "next" ? 1 : -1;
            path.MoveTo(x - direction * 70, y); path.LineTo(x + direction * 70, y);
            gesture.AddStroke(new GestureDescription.StrokeDescription(path, 0, 180));
        }
        else if (operation == "explore")
        {
            var expected = intent.GetStringExtra("caption");
            var targets = nodes.Where(value => value.Visible && value.Enabled && value.Clickable && !value.Password
                && (value.Description == expected || value.Text == expected)).ToArray();
            if (expected is null || !_safeCaptions.Contains(expected) || targets.Length != 1)
                throw new InvalidOperationException("One original visible safe target is required for exploration.");
            var target = targets[0].Bounds;
            if (target[2] - target[0] < 44 || target[3] - target[1] < 44 || target[0] < bounds[0] || target[1] < bounds[1]
                || target[2] > bounds[2] || target[3] > bounds[3])
                throw new InvalidOperationException("Original target must be fully inside its owned viewport.");
            path.MoveTo((target[0] + target[2]) / 2f, (target[1] + target[3]) / 2f);
            gesture.AddStroke(new GestureDescription.StrokeDescription(path, 0, 80));
        }
        else if (operation == "scroll-forward")
        {
            // Real two-finger motion delegates scrolling through TalkBack; never call a node scroll/focus action.
            var expected = intent.GetStringExtra("caption");
            if (expected is null || !_safeCaptions.Contains(expected)
                || !nodes.Any(value => value.Description == expected || value.Text == expected))
                throw new InvalidOperationException("Original safe destination must belong to this tree.");
            var viewport = nodes.Where(value => value.Scrollable && value.Visible)
                .OrderByDescending(value => (value.Bounds[2] - value.Bounds[0]) * (value.Bounds[3] - value.Bounds[1])).FirstOrDefault();
            if (viewport is null || viewport.Bounds[3] - viewport.Bounds[1] < 240)
                throw new InvalidOperationException("Original scroll viewport unavailable.");
            var area = viewport.Bounds; var center = (area[0] + area[2]) / 2f;
            var start = area[1] + (area[3] - area[1]) * .75f; var end = area[1] + (area[3] - area[1]) * .25f;
            path.MoveTo(center - 30, start); path.LineTo(center - 30, end);
            secondPath.MoveTo(center + 30, start); secondPath.LineTo(center + 30, end);
            gesture.AddStroke(new GestureDescription.StrokeDescription(path, 0, 350));
            gesture.AddStroke(new GestureDescription.StrokeDescription(secondPath, 0, 350));
        }
        else if (operation == "activate")
        {
            var expected = intent.GetStringExtra("caption");
            if (expected is null || !_safeCaptions.Contains(expected) || focused.Length != 1
                || focused[0].Password || !focused[0].Visible || !focused[0].Enabled
                || (focused[0].Description != expected && focused[0].Text != expected))
                throw new InvalidOperationException("Exact original safe accessibility focus required before activation.");
            path.MoveTo(x, y);
            gesture.AddStroke(new GestureDescription.StrokeDescription(path, 0, 60));
            gesture.AddStroke(new GestureDescription.StrokeDescription(path, 130, 60));
        }
        else throw new InvalidOperationException("Unknown bounded operation.");
        using var description = gesture.Build() ?? throw new InvalidOperationException("Original gesture was not built.");
        _busy = true;
        var accepted = DispatchGesture(description, new Completion(this, id, operation), _handler);
        if (!accepted) _busy = false;
        Emit(new { Kind = "dispatch", Request = id, Operation = operation, Accepted = accepted, BeforeFocus = focused, WindowId = root.WindowId });
    }

    /// <summary>Observes real reader state after a public dispatch result; callback success alone is never acceptance.</summary>
    private sealed class Completion(ReaderGestureService service, string request, string operation) : GestureResultCallback
    {
        /// <inheritdoc />
        public override void OnCompleted(GestureDescription? gestureDescription) => Finish(true);
        /// <inheritdoc />
        public override void OnCancelled(GestureDescription? gestureDescription) => Finish(false);
        private void Finish(bool completed)
        {
            service._handler.PostDelayed(() =>
            {
                service._busy = false;
                Emit(new { Kind = "callback", Request = request, Operation = operation, Completed = completed });
            }, 400);
        }
    }

    /// <summary>Reads only bounded, password-redacted metadata in the explicitly owned window.</summary>
    private static Node Snapshot(AccessibilityNodeInfo root)
    {
        var count = 0;
        Node Read(AccessibilityNodeInfo node, int depth)
        {
            if (++count > 1500 || depth > 40 || node.PackageName?.ToString() != Owner)
                throw new InvalidOperationException("Original bounded tree required.");
            var value = Describe(node); var children = new List<Node>();
            for (var index = 0; index < node.ChildCount; index++)
            {
                using var child = node.GetChild(index);
                if (child is not null) children.Add(Read(child, depth + 1));
            }
            return value with { Children = children };
        }
        return Read(root, 0);
    }

    /// <summary>Copies original native focus/target metadata without activation or password contents.</summary>
    private static Node Describe(AccessibilityNodeInfo node)
    {
        using var bounds = new Rect(); node.GetBoundsInScreen(bounds);
        string? Text(Java.Lang.ICharSequence? text)
        {
            var value = text?.ToString();
            if (value?.Length > 2048) throw new InvalidOperationException("Bounded caption required.");
            return value;
        }
        return new Node(node.WindowId, node.ClassName?.ToString(), node.Password ? null : Text(node.TextFormatted),
            node.Password ? null : Text(node.ContentDescriptionFormatted), node.Password, node.VisibleToUser,
            node.Enabled, node.AccessibilityFocused, node.Clickable, node.Scrollable, [bounds.Left, bounds.Top, bounds.Right, bounds.Bottom], []);
    }

    /// <summary>Enumerates the copied original tree without holding native node handles.</summary>
    private static IEnumerable<Node> Flatten(Node node)
    {
        yield return node;
        foreach (var child in node.Children) foreach (var item in Flatten(child)) yield return item;
    }

    /// <summary>Emits bounded derived public records, never screenshots or private files.</summary>
    private static void Emit(object value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        if (bytes.Length > 512 * 1024) throw new InvalidOperationException("Bounded public observation required.");
        var id = Guid.NewGuid().ToString("N");
        var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++)
        {
            var offset = index * 1536; var length = Math.Min(1536, bytes.Length - offset);
            Android.Util.Log.Info(LogTag, id + "|" + index + "/" + count + "|" + Convert.ToBase64String(bytes.AsSpan(offset, length)));
        }
    }

    /// <summary>Immutable original native metadata with explicit redaction and focus.</summary>
    private sealed record Node(int WindowId, string? Class, string? Text, string? Description, bool Password,
        bool Visible, bool Enabled, bool AccessibilityFocused, bool Clickable, bool Scrollable, int[] Bounds, IReadOnlyList<Node> Children);
}

/// <summary>Receives commands only from senders holding Android's privileged dump permission.</summary>
[BroadcastReceiver(Name = "pro.vafadar.zanance.readergesturereview.CommandReceiver", Exported = true, Permission = Android.Manifest.Permission.Dump)]
[IntentFilter(["pro.vafadar.zanance.readergesturereview.COMMAND"])]
public sealed class CommandReceiver : BroadcastReceiver
{
    /// <inheritdoc />
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == "pro.vafadar.zanance.readergesturereview.COMMAND") ReaderGestureService.Receive(intent);
    }
}
