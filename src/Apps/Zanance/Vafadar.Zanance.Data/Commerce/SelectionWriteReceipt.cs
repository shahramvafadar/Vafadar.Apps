namespace Vafadar.Zanance.Data.Commerce;

/// <summary>One original choice writer's known outcome, retained before fallible post-commit notifications.</summary>
public sealed class SelectionWriteReceipt
{
    private int _state;
    /// <summary>Gets whether the original writer confirmed a commit or an already matching canonical selection.</summary>
    public bool Accepted => Volatile.Read(ref _state) > 0;
    /// <summary>Gets whether that confirmed original operation changed the stored canonical selection.</summary>
    public bool Changed => Volatile.Read(ref _state) == 2;
    /// <summary>Claims this receipt for exactly one original writer attempt, including failed attempts.</summary>
    internal void Begin()
    {
        if (Interlocked.CompareExchange(ref _state, -1, 0) != 0)
            throw new InvalidOperationException("The original choice receipt already belongs to a request.");
    }
    /// <summary>Records the writer-confirmed outcome before any notification can fail.</summary>
    internal void Accept(bool changed) => Volatile.Write(ref _state, changed ? 2 : 1);
}
