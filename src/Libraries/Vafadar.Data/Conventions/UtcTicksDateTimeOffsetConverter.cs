using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Vafadar.Data.Conventions;

/// <summary>
/// Stores a <see cref="DateTimeOffset"/> as its UTC ticks. Values are read back in UTC (offset zero); ordering and
/// comparison in SQL are exact.
/// </summary>
public sealed class UtcTicksDateTimeOffsetConverter : ValueConverter<DateTimeOffset, long>
{
    /// <summary>Creates the converter.</summary>
    public UtcTicksDateTimeOffsetConverter()
        : base(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
    {
    }
}
