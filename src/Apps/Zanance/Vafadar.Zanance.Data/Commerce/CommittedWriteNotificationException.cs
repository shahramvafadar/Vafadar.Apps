namespace Vafadar.Zanance.Data.Commerce;

/// <summary>A write already committed before an original change subscriber failed; callers must never resubmit it.</summary>
public sealed class CommittedWriteNotificationException : Exception
{
    /// <summary>Retains the subscriber failure without turning it into a rejected or uncommitted write.</summary>
    public CommittedWriteNotificationException(Exception innerException)
        : base("The write committed before its change notification failed.", innerException) { }
}

/// <summary>Preserves the known commit boundary when original change subscribers reject publication.</summary>
internal static class CommittedWriteNotification
{
    /// <summary>Invokes only the post-commit notification; failures remain visible with committed semantics.</summary>
    internal static void Publish(Action notify)
    {
        try { notify(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { throw new CommittedWriteNotificationException(exception); }
    }
}
