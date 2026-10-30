using System.Globalization;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Original durable acceptance and its separately fallible active-profile publication.</summary>
internal sealed record DisplayUnitPublicationResult(DisplayUnitsReview Review, bool NotificationFailed, bool Published);

/// <summary>Owns the actual display-unit Save boundary independently of device preference migration.</summary>
internal static class DisplayUnitPublication
{
    /// <summary>Separators reserved by the original profile-unit serialization.</summary>
    internal static readonly char[] Reserved = ['|', ';'];
    /// <summary>Captures explicit input before awaits; applies units only after the original selective writer commits.</summary>
    internal static async Task<DisplayUnitPublicationResult> SaveAsync(ZananceStore store, IEnumerable<DisplayUnit> units,
        DisplayUnitsReview? original = null, Func<bool>? ownsOriginal = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(store); ArgumentNullException.ThrowIfNull(units);
        var submitted = units.ToArray();
        if (submitted.Any(unit => !unit.IsValid || unit.Name.IndexOfAny(Reserved) >= 0))
            throw new ArgumentException("Only explicit valid display-unit definitions can be saved.", nameof(units));
        submitted = submitted.GroupBy(unit => unit.CurrencyCode.ToUpperInvariant()).Select(group => group.Last() with
            { CurrencyCode = group.Key, Name = group.Last().Name.Trim() }).OrderBy(unit => unit.CurrencyCode, StringComparer.Ordinal).ToArray();
        var ambient = Serialize(DisplayUnits.All); var serialized = Serialize(submitted);
        bool Current() => (ownsOriginal?.Invoke() ?? true) && ambient == Serialize(DisplayUnits.All);
        original ??= await store.ReadDisplayUnitsAsync(ct);
        DisplayUnitsReview accepted; var notificationFailed = false;
        try { accepted = await store.SaveDisplayUnitsAsync(original, serialized, Current, ct); }
        catch (CommittedWriteNotificationException<DisplayUnitsReview> saved) { accepted = saved.Result; notificationFailed = true; }
        var publish = false;
        try { publish = Current(); if (publish) accepted.EnsureCurrent(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // The original writer already committed: a retired/failing source may stop publication, never erase its receipt.
            notificationFailed = true; publish = false;
        }
        if (publish) DisplayUnits.Set(submitted);
        return new(accepted, notificationFailed, publish);
    }
    /// <summary>Captures only canonical current definitions so an old editor cannot borrow another profile's later publication.</summary>
    internal static string CaptureCurrent() => Serialize(DisplayUnits.All);
    /// <summary>Applies only a successfully read original active-profile serialization, without database or device access.</summary>
    internal static void Apply(string serialized)
    {
        var units = new List<DisplayUnit>();
        foreach (var item in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries))
            if (item.Split('|') is [var code, var name, var exponent]
                && int.TryParse(exponent, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                units.Add(new(code, name, value));
        DisplayUnits.Set(units);
    }
    private static string Serialize(IEnumerable<DisplayUnit> units) => string.Join(';', units.Select(unit =>
        string.Create(CultureInfo.InvariantCulture, $"{unit.CurrencyCode}|{unit.Name}|{unit.Exponent}")));
}
