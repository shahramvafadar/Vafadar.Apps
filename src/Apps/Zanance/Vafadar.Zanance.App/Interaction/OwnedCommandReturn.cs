namespace Vafadar.Zanance.App.Interaction;

/// <summary>One-use original parent returns, independent of shared busy flags or general navigation permission.</summary>
internal static class OwnedCommandReturn
{
    private static readonly object Gate = new();
    private static readonly List<Scope> Pending = [];
    /// <summary>Captures actual native identities before starting the original command's one Pop.</summary>
    internal static Scope Begin(AppShell root, Page page, object owner, Action validateOriginal)
    {
        var scope = new Scope(root, page, owner, validateOriginal);
        scope.RequireCurrent(); lock (Gate) Pending.Add(scope); return scope;
    }
    /// <summary>Consumes only the matching Pop; another page, forward route or second event cannot borrow it.</summary>
    internal static bool Allows(AppShell root, ShellNavigatingEventArgs args)
    { lock (Gate) return Pending.Any(scope => scope.TryConsume(root, args)); }

    /// <summary>Retains weak native identities until one original parent return finishes or is refused.</summary>
    internal sealed class Scope : IDisposable
    {
        private readonly WeakReference<AppShell> _root;
        private readonly WeakReference<Page> _page, _parent;
        private readonly WeakReference<object> _owner;
        private readonly Action _validate;
        private readonly string _parentLocation;
        private readonly int _depth;
        private bool _consumed, _disposed;
        /// <summary>Captures the actual immediate parent and its expected original route without navigating.</summary>
        internal Scope(AppShell root, Page page, object owner, Action validate)
        {
            var stack = page.Navigation.NavigationStack.Where(item => item is not null).ToArray();
            if (stack.Length < 2 || !ReferenceEquals(stack[^1], page))
                throw new InvalidOperationException("The original immediate parent is unavailable.");
            var location = root.CurrentState.Location.OriginalString.Split('?', '#')[0].TrimEnd('/');
            var separator = location.LastIndexOf('/');
            if (separator <= 0) throw new InvalidOperationException("The original parent route is unavailable.");
            _parentLocation = location[..separator]; _depth = stack.Length;
            _root = new(root); _page = new(page); _parent = new(stack[^2]); _owner = new(owner); _validate = validate;
        }
        /// <summary>Rejects a replaced window, page, binding, parent stack or retired command.</summary>
        internal void RequireCurrent()
        {
            _validate();
            if (_disposed || !_root.TryGetTarget(out var root) || !_page.TryGetTarget(out var page)
                || !_parent.TryGetTarget(out var parent) || !_owner.TryGetTarget(out var owner)
                || !ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, root)
                || !ReferenceEquals(root.CurrentPage, page) || !ReferenceEquals(page.BindingContext, owner)
                || root.Navigation.ModalStack.Count != 0)
                throw new OperationCanceledException("The original command return retired.");
            var stack = page.Navigation.NavigationStack.Where(item => item is not null).ToArray();
            if (stack.Length != _depth || !ReferenceEquals(stack[^1], page) || !ReferenceEquals(stack[^2], parent))
                throw new OperationCanceledException("The original parent changed.");
        }
        /// <summary>Accepts one immediate-parent event; a two-page stack also permits its equivalent single-level root pop.</summary>
        internal bool TryConsume(AppShell root, ShellNavigatingEventArgs args)
        {
            if (_disposed || _consumed || !_root.TryGetTarget(out var original) || !ReferenceEquals(root, original)
                || !(args.Source == ShellNavigationSource.Pop || _depth == 2 && args.Source == ShellNavigationSource.PopToRoot)) return false;
            var target = args.Target.Location.OriginalString.TrimEnd('/');
            if (target != ".." && target != _parentLocation) return false;
            try { RequireCurrent(); } catch (OperationCanceledException) { return false; }
            _consumed = true; return true;
        }
        /// <summary>Distinguishes actual completion from canceled GoToAsync that never left the source page.</summary>
        internal void RequireReturned()
        {
            if (!_consumed || !_root.TryGetTarget(out var root) || !_parent.TryGetTarget(out var parent)
                || !ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, root) || !ReferenceEquals(root.CurrentPage, parent))
                throw new OperationCanceledException("The original parent return was not completed.");
        }
        /// <inheritdoc />
        public void Dispose() { lock (Gate) { _disposed = true; Pending.Remove(this); } }
    }
}
