using System.ComponentModel;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>One device-wide preference shared by every profile and AI entry point.</summary>
public sealed class AiFeatures(bool enabled, Action<bool> persist) : INotifyPropertyChanged
{
    private readonly object gate = new();
    private CancellationTokenSource? session = enabled ? new() : null;

    /// <summary>Gets the app-wide choice initialized before the first page is constructed.</summary>
    public static AiFeatures Current { get; internal set; } = new(true, _ => { });

    /// <summary>Gets or sets whether optional AI features may be used on this installation.</summary>
    public bool Enabled
    {
        get => Volatile.Read(ref session) is not null;
        set
        {
            CancellationTokenSource? retired;
            lock (gate)
            {
                if ((session is not null) == value) return;
                persist(value);
                retired = session;
                Volatile.Write(ref session, value ? new CancellationTokenSource() : null);
            }
            // Old work must stay retired even when the user subsequently enables AI.
            try { retired?.Cancel(); }
            finally { PropertyChanged?.Invoke(this, new(nameof(Enabled))); }
        }
    }

    /// <summary>Captures the current enabled session or an already canceled token while AI is off.</summary>
    public CancellationToken Capture() => Volatile.Read(ref session)?.Token ?? new CancellationToken(true);

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
