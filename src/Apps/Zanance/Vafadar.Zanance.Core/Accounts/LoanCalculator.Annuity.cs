using System.Numerics;

namespace Vafadar.Zanance.Core.Accounts;

public static partial class LoanCalculator
{
    /// <summary>Rounds the original positive-rate nominal annuity up without floating-point cancellation or lost minor units.</summary>
    private static long AnnuityInstallment(long principal, decimal annualRatePercent, int months)
    {
        var (p, q) = MonthlyRateFraction(annualRatePercent);
        // A finite positive-rate annuity is strictly above both principal/months and its initial monthly interest.
        var minimum = BigInteger.Max((BigInteger)principal / months + 1, principal * p / q + 1);
        if (minimum > long.MaxValue) { throw new OverflowException(); }

        if (months <= sizeof(long) * 8)
        {
            var factor = BigInteger.Pow(p + q, months);
            return checked((long)CeilingQuotient(principal * p * factor, q * (factor - BigInteger.Pow(q, months))));
        }

        // For reduced p/q, an integral annuity requires (p+q)^(months-1) <= principal/q.
        // Since p+q >= 2 and principal fits positive Int64, the exact branch above covers all integer results.
        // Longer schedules therefore admit a unique ceiling after refining these outward-rounded intervals.
        for (var precision = 128; ; precision = checked(precision * 2))
        {
            var scale = BigInteger.One << precision;
            var (lowerPower, upperPower) = InverseAnnuityPowerBounds(q, p + q, months, scale);
            if (upperPower >= scale) { continue; }
            var dividend = principal * p * scale;
            var lower = BigInteger.Max(minimum, CeilingQuotient(dividend, q * (scale - lowerPower)));
            if (lower > long.MaxValue) { throw new OverflowException(); }
            var upper = CeilingQuotient(dividend, q * (scale - upperPower));
            if (lower == upper) { return checked((long)lower); }
        }
    }

    /// <summary>Preserves every decimal coefficient/scale bit while converting annual percent into an exact monthly fraction.</summary>
    private static (BigInteger Numerator, BigInteger Denominator) MonthlyRateFraction(decimal annualRatePercent)
    {
        var bits = decimal.GetBits(annualRatePercent);
        // Decimal's coefficient words are unsigned; their signed array representation is not monetary arithmetic.
        var numerator = (BigInteger)unchecked((uint)bits[0])
            + ((BigInteger)unchecked((uint)bits[1]) << 32)
            + ((BigInteger)unchecked((uint)bits[2]) << 64);
        var denominator = 1200 * BigInteger.Pow(10, (bits[3] >> 16) & 0xFF);
        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        return (numerator / divisor, denominator / divisor);
    }

    /// <summary>Bounds a positive inverse power without constructing integers proportional to the requested month count.</summary>
    private static (BigInteger Lower, BigInteger Upper) InverseAnnuityPowerBounds(
        BigInteger numerator, BigInteger denominator, int months, BigInteger scale)
    {
        var factorLower = numerator * scale / denominator;
        var factorUpper = CeilingQuotient(numerator * scale, denominator);
        var lower = scale;
        var upper = scale;
        for (var remaining = months; remaining > 0; remaining >>= 1)
        {
            if ((remaining & 1) != 0)
            {
                lower = lower * factorLower / scale;
                upper = CeilingQuotient(upper * factorUpper, scale);
            }
            if (remaining > 1)
            {
                factorLower = factorLower * factorLower / scale;
                factorUpper = CeilingQuotient(factorUpper * factorUpper, scale);
            }
        }
        return (lower, upper);
    }

    /// <summary>Returns the exact ceiling of a nonnegative rational without narrowing any intermediate.</summary>
    private static BigInteger CeilingQuotient(BigInteger numerator, BigInteger denominator)
    {
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        return remainder.IsZero ? quotient : quotient + 1;
    }
}
