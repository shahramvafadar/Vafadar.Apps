using System.Text.Json;
using System.Xml.Linq;
using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views.Accessibility;

namespace Vafadar.Zanance.PhysicalReaderGestures;

/// <summary>Bounds public accessibility gestures to the explicitly admitted Samsung and current signed original application window.</summary>
[Service(Name = Component, Exported = true, Permission = Android.Manifest.Permission.BindAccessibilityService)]
[IntentFilter(["android.accessibilityservice.AccessibilityService"])]
[MetaData("android.accessibilityservice", Resource = "@xml/reader_gestures")]
public sealed class ReaderGestureService : AccessibilityService
{
    /// <summary>Explicit independent service identity; never part of the application package.</summary>
    public const string Component = "pro.vafadar.zanance.physicalreadergesturereview.ReaderGestureService";
    /// <summary>Only this original application may receive a reviewed gesture.</summary>
    private const string Owner = "pro.vafadar.zanance";
    /// <summary>Public, bounded diagnostic outcomes without arbitrary windows or private files.</summary>
    internal const string LogTag = "ZanancePhysicalReader";
    private static ReaderGestureService? _current;
    private readonly HashSet<string> _safeCaptions = new(StringComparer.Ordinal);
    private readonly Handler _handler = new(Looper.MainLooper!);
    private bool _busy;
    private int _commands;
    private long _connected;

    private readonly CancellationTokenSource _bindingCancellation = new();
    private ReaderOwnerInstall? _admittedInstall;
    private Context? _ownerContext;
    private int _bindingVersion;
    private bool _destroyed;

    /// <inheritdoc />
    protected override void OnServiceConnected()
    {
        base.OnServiceConnected();
        if (ReferenceEquals(_current, this)) _current = null;
        _admittedInstall = null;
        var version = ++_bindingVersion;
        var context = ApplicationContext;
        if (context is not null) _ = ConnectScopedAsync(context, version);
    }

    /// <summary>Admit public package bytes off the UI thread, then reject a retired connection before native work.</summary>
    private async Task ConnectScopedAsync(Context context, int version)
    {
        try
        {
            var ct = _bindingCancellation.Token;
            var admitted = await Task.Run(() => PhysicalReaderTarget.AdmitAsync(context, ct), ct);
            if (!PhysicalReaderPolicy.CurrentCallback(version, _bindingVersion, !_destroyed)) return;
            if (admitted is null) { Emit(new { Kind = "refused", Reason = "physical-owner-admission" }); return; }
            _admittedInstall = admitted; _ownerContext = context;
            foreach (var language in new[] { "en", "fa", "de", "es", "fr", "it" })
            {
                var suffix = language == "en" ? "" : "." + language;
                var original = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var prefix in new[] { "AppResources", "SharedStrings" })
                {
                    using var stream = Assets!.Open("captions/" + prefix + suffix + ".resx");
                    foreach (var entry in XDocument.Load(stream).Root!.Elements("data"))
                        if (entry.Attribute("name")?.Value is { } key && entry.Element("value")?.Value is { } value)
                            original.Add(key, value);
                }
                _safeCaptions.UnionWith(PhysicalReaderCaptions.FromOriginalResources(key => original[key]));
            }
            _commands = 0; _connected = SystemClock.ElapsedRealtime(); _current = this;
            Emit(new { Kind = "ready", PhysicalTargetAdmitted = true, OwnerUid = PhysicalReaderPolicy.OwnerUid,
                OwnerApk = PhysicalReaderPolicy.OwnerApkSha256, PublicDispatchGesture = true,
                RequestsTouchExploration = false, LifetimeMilliseconds = PhysicalReaderPolicy.LifetimeMilliseconds,
                MaximumCommands = PhysicalReaderPolicy.MaximumCommands });
        }
        catch (System.OperationCanceledException) { }
        catch (Exception error)
        {
            if (!_destroyed) EmitRefusal(null, error);
        }
    }

    /// <summary>Expire observations as well as gestures; an APK replacement cannot inherit an earlier binding.</summary>
    private bool SessionOwnedNow()
    {
        if (!ReferenceEquals(_current, this) || _destroyed || _ownerContext is null || _admittedInstall is null
            || !PhysicalReaderPolicy.SessionOpen(SystemClock.ElapsedRealtime(), _connected, _commands, true)) return false;
        try { return PhysicalReaderPolicy.SameInstall(_admittedInstall, PhysicalReaderTarget.ReadInstall(_ownerContext)); }
        catch { return false; }
    }

    /// <inheritdoc />
    public override void OnAccessibilityEvent(AccessibilityEvent? e)
    {
        if (!SessionOwnedNow() || e?.EventType != EventTypes.ViewAccessibilityFocused || e.PackageName?.ToString() != Owner) return;
        try
        {
            using var root = RootInActiveWindow;
            if (root is null || root.PackageName?.ToString() != Owner || !root.VisibleToUser || root.Window?.IsFocused != true) return;
            using var node = e.Source;
            if (node is not null && node.WindowId == root.WindowId)
                Emit(new { Kind = "actual-accessibility-focus", Node = Describe(node), ElapsedMs = SystemClock.ElapsedRealtime() });
        }
        catch (Exception error)
        {
            if (!_destroyed) EmitRefusal(null, error);
        }
    }

    /// <inheritdoc />
    public override void OnInterrupt() { }

    /// <inheritdoc />
    public override void OnDestroy()
    {
        _destroyed = true; ++_bindingVersion; _admittedInstall = null;
        if (ReferenceEquals(_current, this)) _current = null;
        _bindingCancellation.Cancel();
        _handler.RemoveCallbacksAndMessages(null);
        _bindingCancellation.Dispose();
        base.OnDestroy();
    }

    /// <summary>Accepts a finite shell-protected operation; serializes actual gestures and refuses stale windows/focus.</summary>
    internal static void Receive(Intent intent)
    {
        var service = _current;
        var request = PhysicalReaderPolicy.RefusalRequest(intent.GetStringExtra("request"));
        if (service is null) { Emit(new { Kind = "refused", Request = request, Reason = "service-unavailable" }); return; }
        try { service.Execute(intent); }
        catch (Exception error) { EmitRefusal(request, error); }
    }

    /// <summary>Retain request lineage and a fixed guard code without exporting arbitrary exception data.</summary>
    private static void EmitRefusal(string? request, Exception error) => Emit(new
    {
        Kind = "refused", Request = PhysicalReaderPolicy.RefusalRequest(request),
        Reason = PhysicalReaderPolicy.RefusalReason(error.Message), ErrorType = error.GetType().FullName
    });

    /// <summary>Dispatches bounded reader navigation/exploration or reviewed nonfinancial activation; never performs node actions.</summary>
    private void Execute(Intent intent)
    {
        var id = intent.GetStringExtra("request") ?? "";
        if (!SessionOwnedNow() || !System.Text.RegularExpressions.Regex.IsMatch(id, "^[0-9a-f]{32}$")
            || ++_commands > PhysicalReaderPolicy.MaximumCommands || _busy)
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
            var minimum = PhysicalReaderPolicy.MinimumTargetPixels(Resources?.DisplayMetrics?.Density ?? float.NaN);
            if (target[2] - target[0] < minimum || target[3] - target[1] < minimum || target[0] < bounds[0] || target[1] < bounds[1]
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
        var accepted = DispatchGesture(description, new Completion(this, _bindingVersion, id, operation), _handler);
        if (!accepted) _busy = false;
        Emit(new { Kind = "dispatch", Request = id, Operation = operation, Accepted = accepted, BeforeFocus = focused, WindowId = root.WindowId });
    }

    /// <summary>Observes real reader state after a public dispatch result; callback success alone is never acceptance.</summary>
    private sealed class Completion : GestureResultCallback
    {
        private readonly WeakReference<ReaderGestureService> _owner;
        private readonly int _binding;
        private readonly string _request;
        private readonly string _operation;

        /// <summary>A callback owns only the admitted binding token, not a native service lifetime.</summary>
        internal Completion(ReaderGestureService owner, int binding, string request, string operation)
        { _owner = new(owner); _binding = binding; _request = request; _operation = operation; }

        /// <inheritdoc />
        public override void OnCompleted(GestureDescription? gestureDescription) => Finish(true);
        /// <inheritdoc />
        public override void OnCancelled(GestureDescription? gestureDescription) => Finish(false);

        private void Finish(bool completed)
        {
            if (!_owner.TryGetTarget(out var live)
                || !PhysicalReaderPolicy.CurrentCallback(_binding, live._bindingVersion, live.SessionOwnedNow())) return;
            // D-572: the supplied main-looper handler serializes destruction; queued work checks the same weak token.
            live._handler.PostDelayed(() =>
            {
                if (!_owner.TryGetTarget(out var current)
                    || !PhysicalReaderPolicy.CurrentCallback(_binding, current._bindingVersion, current.SessionOwnedNow())) return;
                current._busy = false;
                Emit(new { Kind = "callback", Request = _request, Operation = _operation, Completed = completed });
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
[BroadcastReceiver(Name = "pro.vafadar.zanance.physicalreadergesturereview.CommandReceiver", Exported = true, Permission = Android.Manifest.Permission.Dump)]
[IntentFilter(["pro.vafadar.zanance.physicalreadergesturereview.COMMAND"])]
public sealed class CommandReceiver : BroadcastReceiver
{
    /// <inheritdoc />
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == "pro.vafadar.zanance.physicalreadergesturereview.COMMAND") ReaderGestureService.Receive(intent);
    }
}
