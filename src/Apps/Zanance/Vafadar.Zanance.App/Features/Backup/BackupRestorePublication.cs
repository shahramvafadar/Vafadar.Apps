namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Carries only original native/publication validation through the existing package receiver's maintenance queue.</summary>
internal static class BackupRestorePublication
{
    private static readonly AsyncLocal<Action?> Validation = new();
    /// <summary>Binds the original request without exposing a file key, package bytes or authentication state.</summary>
    internal static IDisposable Enter(Action validate)
    { var previous = Validation.Value; Validation.Value = validate; return new Scope(previous); }
    /// <summary>Refuses a retired original request after actual exclusive maintenance acquisition.</summary>
    internal static void EnsureCurrent() => Validation.Value?.Invoke();
    private sealed class Scope(Action? previous) : IDisposable
    {
        private bool disposed;
        /// <inheritdoc />
        public void Dispose() { if (!disposed) { Validation.Value = previous; disposed = true; } }
    }
}
