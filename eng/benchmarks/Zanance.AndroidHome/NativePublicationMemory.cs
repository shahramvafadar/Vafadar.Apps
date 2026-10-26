using System.Globalization;

namespace Vafadar.Zanance.Performance;

/// <summary>Fixed aggregate self-process counters; never raw status lines, source values, paths or memory contents.</summary>
internal readonly record struct NativePublicationMemory(long PssIncludingSwapKiB, long ResidentRssKiB, long PrivateVmSwapKiB)
{
    /// <summary>Accepts only the two exact kernel counters and original Android total PSS, without double-counting swap.</summary>
    internal static NativePublicationMemory Parse(string status, long totalPss)
    {
        ArgumentNullException.ThrowIfNull(status);
        long? rss = null; long? swap = null;
        foreach (var line in status.Split('\n'))
        {
            var colon = line.IndexOf(':'); if (colon < 0) continue;
            var key = line[..colon]; if (key is not ("VmRSS" or "VmSwap")) continue;
            var values = line[(colon + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 2 || values[1] != "kB"
                || !long.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("Invalid original self-memory counter.");
            if (key == "VmRSS") { if (rss is not null) throw new InvalidDataException("Duplicate self RSS counter."); rss = value; }
            else { if (swap is not null) throw new InvalidDataException("Duplicate self swap counter."); swap = value; }
        }
        if (totalPss <= 0 || rss is null or <= 0 || swap is null)
            throw new InvalidDataException("Complete original self-memory counters are required.");
        return new(totalPss, rss.Value, swap.Value);
    }
}
