namespace Vafadar.Temporal;

/// <summary>The unit of an age.</summary>
public enum AgeUnit
{
    /// <summary>Years.</summary>
    Years = 0,

    /// <summary>Thousands of years (ka).</summary>
    ThousandYears = 1,

    /// <summary>Millions of years (Ma).</summary>
    MillionYears = 2,

    /// <summary>Billions of years (Ga).</summary>
    BillionYears = 3,
}

/// <summary>What the years of an age are.</summary>
public enum AgeBasis
{
    /// <summary>Calendar years before the reference.</summary>
    CalendarYears = 0,

    /// <summary>
    /// Uncalibrated radiocarbon years. They are not calendar years: such an age is compared only with other
    /// radiocarbon ages and is never turned into a year by subtraction.
    /// </summary>
    RadiocarbonYears = 1,
}

/// <summary>
/// The moment an age is counted back from: a fixed year of the common era, or unspecified when the source only says
/// "years ago". A stated reference keeps "12,000 years ago" from moving by one year every year.
/// </summary>
public readonly record struct AgeReference
{
    private AgeReference(int? commonEraYear) => CommonEraYear = commonEraYear;

    /// <summary>Gets the reference year of the common era, or <see langword="null"/> when it is not stated.</summary>
    public int? CommonEraYear { get; }

    /// <summary>Gets whether the reference is stated.</summary>
    public bool IsSpecified => CommonEraYear is not null;

    /// <summary>The reference is not stated ("years ago").</summary>
    public static AgeReference Unspecified => default;

    /// <summary>The year 1950 of the common era, the reference of "before present" (BP).</summary>
    public static AgeReference BeforePresent => new(1950);

    /// <summary>A stated year of the common era.</summary>
    public static AgeReference Year(int commonEraYear)
    {
        if (commonEraYear is < 1 or > CalendarPosition.MaxYear)
        {
            throw new ArgumentOutOfRangeException(nameof(commonEraYear), commonEraYear, "The reference is a year of the common era.");
        }

        return new AgeReference(commonEraYear);
    }
}

/// <summary>
/// An age before a reference: "55 million years ago", "12 ka BP". Larger ages are further in the past. The value keeps
/// the digits of the source – "55" and "55.0" are the same age stated with different care – and no month or day exists.
/// </summary>
public sealed record AgePosition : TimePosition
{
    /// <summary>The largest age that can be stated, in years.</summary>
    public const decimal MaxYears = 100_000_000_000m;

    private AgePosition(decimal value, AgeUnit unit, AgeReference reference, AgeBasis basis, bool isApproximate)
    {
        if (!Enum.IsDefined(unit))
        {
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit.");
        }

        if (!Enum.IsDefined(basis))
        {
            throw new ArgumentOutOfRangeException(nameof(basis), basis, "Unknown basis.");
        }

        // Dividing the limit instead of multiplying the value keeps an absurd input from overflowing.
        if (value < 0 || value > MaxYears / YearsPer(unit))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"An age is between 0 and {MaxYears} years.");
        }

        Value = value;
        Unit = unit;
        Reference = reference;
        Basis = basis;
        IsApproximate = isApproximate;
    }

    /// <summary>Gets the age in <see cref="Unit"/>, with the digits of the source.</summary>
    public decimal Value { get; }

    /// <summary>Gets the unit of <see cref="Value"/>.</summary>
    public AgeUnit Unit { get; }

    /// <summary>Gets the moment the age is counted back from.</summary>
    public AgeReference Reference { get; }

    /// <summary>Gets what the years are.</summary>
    public AgeBasis Basis { get; }

    /// <summary>Gets whether the source gives the age as approximate ("about 55 million years ago").</summary>
    public bool IsApproximate { get; }

    /// <summary>Gets the age in years, for calculating only; the stated value and unit are what is shown.</summary>
    public decimal Years => Value * YearsPer(Unit);

    /// <summary>Creates an age.</summary>
    public static AgePosition Of(decimal value, AgeUnit unit, AgeReference reference, AgeBasis basis = AgeBasis.CalendarYears) =>
        new(value, unit, reference, basis, isApproximate: false);

    /// <summary>Returns this age marked as approximate.</summary>
    public AgePosition Approximately() => IsApproximate ? this : new(Value, Unit, Reference, Basis, isApproximate: true);

    private static decimal YearsPer(AgeUnit unit) => unit switch
    {
        AgeUnit.Years => 1m,
        AgeUnit.ThousandYears => 1_000m,
        AgeUnit.MillionYears => 1_000_000m,
        AgeUnit.BillionYears => 1_000_000_000m,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };
}
