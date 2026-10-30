namespace Vafadar.Zanance.App.Security;

internal static partial class NativeSensitivePresentation
{
    private static readonly Lock Gate = new();
    private static readonly List<Scope> Pending = [];
    /// <summary>Binds one actual native authentication modal before its original push; never a global navigation bypass.</summary>
    internal static Scope Bind(Page root, Page modal)
    {
        if (!ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, root))
            throw new InvalidOperationException("The sensitive presentation root retired.");
        var underlying = root is Shell shell ? shell.CurrentPage : root.Navigation.ModalStack.LastOrDefault() ?? root;
        var scope = new Scope(root, modal, underlying, Routing.GetRoute(modal));
        lock (Gate) { Pending.RemoveAll(item => !item.RootIsCurrent); Pending.Add(scope); }
        return scope;
    }
    /// <summary>Finds the original underlying page even while its owned modal is the presented page.</summary>
    internal static Page? Underlying(Page root)
    { lock (Gate) return Pending.FirstOrDefault(item => item.Owns(root))?.OriginalPage; }
    /// <summary>Gets whether this page is temporarily covered by its own bound native authentication presentation.</summary>
    internal static bool CoversOriginalPage(Page page)
    { lock (Gate) return Pending.Any(item => ReferenceEquals(item.OriginalPage, page) && item.IsCovering); }
    /// <summary>Allows only the exact original modal push or its one actual top-modal pop.</summary>
    internal static bool Allows(Page root, Page originalPage, ShellNavigatingEventArgs args)
    { lock (Gate) return Pending.Any(item => item.Owns(root) && ReferenceEquals(item.OriginalPage, originalPage) && item.Allows(args)); }
    /// <summary>One weak original root/modal registration, disposed only when that sensitive request ends.</summary>
    internal sealed class Scope(Page root, Page modal, Page underlying, string route) : IDisposable
    {
        private readonly WeakReference<Page> _root = new(root), _modal = new(modal), _underlying = new(underlying);
        private bool _starting = true, _disposed;
        /// <summary>Gets whether the exact original window root is still current.</summary>
        internal bool RootIsCurrent => !_disposed && _root.TryGetTarget(out var original)
            && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original);
        /// <summary>Gets only the original underlying page, never a replacement form.</summary>
        internal Page? OriginalPage => _underlying.TryGetTarget(out var page) ? page : null;
        /// <summary>Gets whether the bound presentation is starting or is the actual top modal.</summary>
        internal bool IsCovering => RootIsCurrent && _root.TryGetTarget(out var original) && _modal.TryGetTarget(out var prompt)
            && (_starting || ReferenceEquals(original.Navigation.ModalStack.LastOrDefault(), prompt));
        /// <summary>Matches the actual root and current owned presentation.</summary>
        internal bool Owns(Page page) => RootIsCurrent && _root.TryGetTarget(out var original) && ReferenceEquals(original, page) && IsCovering;
        /// <summary>Checks exact push identity, or a pop of this actual top modal, without permitting another route.</summary>
        internal bool Allows(ShellNavigatingEventArgs args)
        {
            if (!RootIsCurrent || !_root.TryGetTarget(out var original) || !_modal.TryGetTarget(out var prompt)) return false;
            var target = args.Target.Location.OriginalString;
            if (_starting && args.Source == ShellNavigationSource.Push) return IsExactTarget(target, route);
            return args.Source == ShellNavigationSource.Pop && target == ".."
                && ReferenceEquals(original.Navigation.ModalStack.LastOrDefault(), prompt);
        }
        /// <summary>Refuses a cancelled/failed modal push rather than waiting forever for an unseen prompt.</summary>
        internal void RequireShown()
        {
            lock (Gate)
            {
                if (!RootIsCurrent || !_root.TryGetTarget(out var original) || !_modal.TryGetTarget(out var prompt)
                    || !ReferenceEquals(original.Navigation.ModalStack.LastOrDefault(), prompt))
                    throw new InvalidOperationException("The original sensitive presentation was not shown.");
                _starting = false;
            }
        }
        /// <inheritdoc />
        public void Dispose() { lock (Gate) { _disposed = true; Pending.Remove(this); } }
    }
}
