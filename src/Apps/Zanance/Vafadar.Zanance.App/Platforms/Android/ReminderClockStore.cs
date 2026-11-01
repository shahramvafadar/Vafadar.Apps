using System.Globalization;
using Android.Content;
using Vafadar.Zanance.App.Reminders;

namespace Vafadar.Zanance.App;

/// <summary>Crash-safe private clock metadata; invalidate durably before SDK mutation and publish only after acceptance.</summary>
internal static class ReminderClockStore
{
    private const string StoreName = "zanance.reminder.clocks";

    /// <summary>Reads only metadata matching this exact persisted pending request; unknown requests remain untouched.</summary>
    internal static ReminderNativeClockBinding? Read(int id, DateTimeOffset scheduled)
    {
        using var store = Open();
        return ReminderNativeClockBinding.Match(id, store.GetString(Key(id), null), scheduled);
    }

    /// <summary>Atomically replaces the bounded queue's metadata; an empty set is the durable mutation barrier.</summary>
    internal static void Replace(IEnumerable<ReminderNativeClockBinding> bindings)
    {
        using var store = Open();
        using var edit = store.Edit() ?? throw new IOException("Reminder clock metadata editor unavailable.");
        edit.Clear();
        foreach (var binding in bindings) edit.PutString(Key(binding.Id), binding.Encode());
        if (!edit.Commit()) throw new IOException("Reminder clock metadata was not committed.");
    }

    /// <summary>Invalidates one id before a single native Show/Remove without disturbing other pending deadlines.</summary>
    internal static void Remove(int id)
    {
        using var store = Open();
        using var edit = store.Edit() ?? throw new IOException("Reminder clock metadata editor unavailable.");
        edit.Remove(Key(id));
        if (!edit.Commit()) throw new IOException("Reminder clock metadata was not invalidated.");
    }

    /// <summary>Publishes a single accepted pending request under the scheduler's existing native queue gate.</summary>
    internal static void Put(ReminderNativeClockBinding binding)
    {
        using var store = Open();
        using var edit = store.Edit() ?? throw new IOException("Reminder clock metadata editor unavailable.");
        edit.PutString(Key(binding.Id), binding.Encode());
        if (!edit.Commit()) throw new IOException("Reminder clock metadata was not committed.");
    }

    private static ISharedPreferences Open() => Android.App.Application.Context.GetSharedPreferences(StoreName, FileCreationMode.Private)
        ?? throw new IOException("Reminder clock metadata unavailable.");

    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);
}
