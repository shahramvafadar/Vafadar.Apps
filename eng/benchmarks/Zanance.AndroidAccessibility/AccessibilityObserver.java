package pro.vafadar.zanance.diagnostics;

import android.accessibilityservice.AccessibilityServiceInfo;
import android.app.UiAutomation;
import android.graphics.Rect;
import android.os.HandlerThread;
import android.os.Looper;
import android.os.Process;
import android.os.SystemClock;
import android.view.accessibility.AccessibilityEvent;
import android.view.accessibility.AccessibilityNodeInfo;
import org.json.JSONArray;
import org.json.JSONObject;

/** Read-only, package-scoped native observations that keep the real screen reader running. */
public final class AccessibilityObserver {
    /** Bound the native tree rather than collecting arbitrary windows or unbounded text. */
    private static final int MAX_NODES = 1500;
    private static int nodes;
    private static int eventCount;

    /** Prevent construction of this shell-only entry point. */
    private AccessibilityObserver() { }

    /** Prints one tree or a bounded stream of actual accessibility-focus events; never injects input. */
    public static void main(String[] args) throws Exception {
        if (Process.myUid() != 2000) throw new SecurityException("Explicit adb shell identity required.");
        if (args.length < 2 || args.length > 3) throw new IllegalArgumentException("Package and snapshot/events mode required.");
        final String owner = args[0];
        if (!owner.equals("pro.vafadar.zanance") && !owner.equals("pro.vafadar.zanance.androidhomereview"))
            throw new SecurityException("Only the explicitly owned Zanance window may be read.");
        final boolean events = args[1].equals("events");
        if (!events && !args[1].equals("snapshot")) throw new IllegalArgumentException("Unknown observation mode.");
        final int seconds = args.length == 3 ? Integer.parseInt(args[2]) : 10;
        if (seconds < 1 || seconds > 20) throw new IllegalArgumentException("Observation duration must be bounded.");
        prepareShellLooper();
        HandlerThread thread = new HandlerThread("ZananceReadOnlyAccessibility");
        thread.start();
        UiAutomation automation = null;
        boolean connected = false;
        try {
            // The platform shell wrapper uses these same framework constructors. Reflection avoids
            // bundling internal Android classes; the shell UID already owns the platform permission.
            Class<?> connectionType = Class.forName("android.app.UiAutomationConnection");
            Class<?> connectionInterface = Class.forName("android.app.IUiAutomationConnection");
            Object connection = connectionType.getConstructor().newInstance();
            automation = UiAutomation.class.getConstructor(Looper.class, connectionInterface)
                .newInstance(thread.getLooper(), connection);
            // D-348: a plain connect() suppresses TalkBack and can invalidate the very observation.
            UiAutomation.class.getMethod("connect", int.class).invoke(automation,
                UiAutomation.FLAG_DONT_SUPPRESS_ACCESSIBILITY_SERVICES);
            connected = true;
            AccessibilityServiceInfo info = automation.getServiceInfo();
            info.flags |= AccessibilityServiceInfo.FLAG_REPORT_VIEW_IDS
                | AccessibilityServiceInfo.FLAG_RETRIEVE_INTERACTIVE_WINDOWS;
            automation.setServiceInfo(info);
            if (events) {
                automation.setOnAccessibilityEventListener(event -> {
                    if (event.getEventType() != AccessibilityEvent.TYPE_VIEW_ACCESSIBILITY_FOCUSED
                        || !owner.contentEquals(event.getPackageName() == null ? "" : event.getPackageName())) return;
                    if (++eventCount > 100) return;
                    try {
                        AccessibilityNodeInfo source = event.getSource();
                        JSONObject result = new JSONObject().put("kind", "accessibility-focus")
                            .put("package", owner).put("elapsedMs", SystemClock.elapsedRealtime());
                        if (source != null) {
                            try { result.put("node", describe(source)); }
                            finally { recycle(source); }
                        }
                        print(result);
                    } catch (Exception error) {
                        printError(error);
                    }
                });
                print(new JSONObject().put("kind", "ready").put("preservesServices", true).put("package", owner));
                Thread.sleep(seconds * 1000L);
                print(new JSONObject().put("kind", "complete").put("preservesServices", true));
            } else {
                AccessibilityNodeInfo root = null;
                // Window metadata can arrive after the connection callback; observe the same client.
                for (int attempt = 0; attempt < 25 && root == null; attempt++) {
                    root = automation.getRootInActiveWindow();
                    if (root == null) Thread.sleep(100L);
                }
                if (root == null) {
                    print(new JSONObject().put("kind", "unavailable-root").put("preservesServices", true)
                        .put("serviceFlags", info.flags).put("capabilities", info.getCapabilities()));
                    return;
                }
                try {
                    if (!owner.contentEquals(root.getPackageName() == null ? "" : root.getPackageName())) {
                        print(new JSONObject().put("kind", "outside-owner").put("package", text(root.getPackageName())));
                        return;
                    }
                    nodes = 0;
                    print(new JSONObject().put("kind", "snapshot").put("package", owner)
                        .put("preservesServices", true).put("root", tree(root, owner, 0)));
                } finally { recycle(root); }
            }
        } finally {
            try {
                if (connected) UiAutomation.class.getMethod("disconnect").invoke(automation);
            } finally {
                thread.quitSafely();
            }
        }
    }

    /** Match the platform shell initialization; no ActivityThread creates a main looper here. */
    @SuppressWarnings("deprecation")
    private static void prepareShellLooper() {
        if (Looper.getMainLooper() == null) Looper.prepareMainLooper();
    }

    /** Copies only bounded metadata from an explicitly owned native node. */
    private static JSONObject tree(AccessibilityNodeInfo node, String owner, int depth) throws Exception {
        if (++nodes > MAX_NODES || depth > 40) throw new IllegalStateException("Native tree bound exceeded.");
        if (!owner.contentEquals(node.getPackageName() == null ? "" : node.getPackageName()))
            return new JSONObject().put("outsideOwnerChild", true);
        JSONObject result = describe(node);
        JSONArray children = new JSONArray();
        for (int index = 0; index < node.getChildCount(); index++) {
            AccessibilityNodeInfo child = node.getChild(index);
            if (child == null) continue;
            try { children.put(tree(child, owner, depth + 1)); }
            finally { recycle(child); }
        }
        return result.put("children", children);
    }

    /** Preserves actual accessibility focus separately from ordinary keyboard focus. */
    private static JSONObject describe(AccessibilityNodeInfo node) throws Exception {
        Rect rect = new Rect();
        node.getBoundsInScreen(rect);
        return new JSONObject().put("class", text(node.getClassName()))
            .put("text", node.isPassword() ? JSONObject.NULL : text(node.getText()))
            .put("description", node.isPassword() ? JSONObject.NULL : text(node.getContentDescription()))
            .put("viewId", text(node.getViewIdResourceName())).put("password", node.isPassword())
            .put("visible", node.isVisibleToUser()).put("enabled", node.isEnabled())
            .put("focusable", node.isFocusable()).put("keyboardFocused", node.isFocused())
            .put("accessibilityFocused", node.isAccessibilityFocused()).put("clickable", node.isClickable())
            .put("bounds", new JSONArray().put(rect.left).put(rect.top).put(rect.right).put(rect.bottom));
    }

    /** Never emit unbounded native strings. */
    private static Object text(CharSequence value) {
        if (value == null) return JSONObject.NULL;
        String result = value.toString();
        if (result.length() > 2048) throw new IllegalStateException("Native text bound exceeded.");
        return result;
    }

    /** Serializes event callbacks and snapshots without interleaving JSON lines. */
    private static synchronized void print(JSONObject value) {
        System.out.println(value.toString());
        System.out.flush();
    }

    /** Exposes only exception type, never a private native message or stack. */
    private static void printError(Exception error) {
        try { print(new JSONObject().put("kind", "observation-error").put("type", error.getClass().getName())); }
        catch (Exception ignored) { throw new IllegalStateException("Could not serialize observation status."); }
    }

    /** Release native node handles on the supported API24+ baseline. */
    @SuppressWarnings("deprecation")
    private static void recycle(AccessibilityNodeInfo node) {
        node.recycle();
    }
}
