namespace Vafadar.Zanance.Data.Commerce;

/// <summary>A write already committed before an original change subscriber failed; callers must never resubmit it.</summary>
public sealed class CommittedWriteNotificationException : Exception
{
    /// <summary>Retains the subscriber failure without turning it into a rejected or uncommitted write.</summary>
    public CommittedWriteNotificationException(Exception innerException)
        : base("The write committed before its change notification failed.", innerException) { }
}

/// <summary>A completed operation with its original result retained after a post-commit subscriber failure.</summary>
public sealed class CommittedWriteNotificationException<T> : Exception
{
    /// <summary>Retains the exact accepted result; no caller may infer that the operation was rolled back.</summary>
    public CommittedWriteNotificationException(T result, Exception innerException)
        : base("The operation committed before its change notification failed.", innerException) => Result = result;
    /// <summary>Gets the original committed result, including import counts/batch or Undo outcome.</summary>
    public T Result { get; }
}

/// <summary>Preserves the known commit boundary when original change subscribers reject publication.</summary>
internal static class CommittedWriteNotification
{
    /// <summary>Publishes after commit while retaining the original result if a subscriber refuses it.</summary>
    internal static T Publish<T>(Action notify, T result)
    {
        try { notify(); return result; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { throw new CommittedWriteNotificationException<T>(result, exception); }
    }

    /// <summary>Invokes only the post-commit notification; failures remain visible with committed semantics.</summary>
    internal static void Publish(Action notify)
    {
        try { notify(); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { throw new CommittedWriteNotificationException(exception); }
    }
}
