namespace CSharpExercises.Finance;

/// <summary>
/// The time value of money. Net present value discounts each future cash flow back to today: is an investment worth
/// more than it costs? The internal rate of return is the discount rate that makes NPV zero; it has no closed form,
/// so it's found numerically with Newton's method, falling back to bisection when Newton misbehaves.
/// </summary>
public static class CashFlows
{
    /// <param name="flows">flows[t] arrives at the end of period t; flows[0] is today (usually the negative investment).</param>
    public static double NetPresentValue(double ratePerPeriod, IReadOnlyList<double> flows)
    {
        var npv = 0.0;
        for (var t = 0; t < flows.Count; t++) npv += flows[t] / Math.Pow(1 + ratePerPeriod, t);
        return npv;
    }

    public static double? InternalRateOfReturn(IReadOnlyList<double> flows, double guess = 0.1)
    {
        if (!flows.Any(f => f < 0) || !flows.Any(f => f > 0)) return null; // no sign change, no IRR

        // Newton: r ← r - NPV(r) / NPV'(r)
        var rate = guess;
        for (var i = 0; i < 50; i++)
        {
            var npv = NetPresentValue(rate, flows);
            var derivative = 0.0;
            for (var t = 1; t < flows.Count; t++) derivative -= t * flows[t] / Math.Pow(1 + rate, t + 1);
            if (Math.Abs(npv) < 1e-9) return rate;
            if (derivative == 0 || double.IsNaN(derivative)) break;
            var next = rate - npv / derivative;
            if (next <= -1 || double.IsNaN(next)) break; // stepped outside the valid range
            rate = next;
        }

        // Bisection: slower but can't diverge once the root is bracketed.
        double low = -0.99, high = 10;
        if (Math.Sign(NetPresentValue(low, flows)) == Math.Sign(NetPresentValue(high, flows))) return null;
        for (var i = 0; i < 200; i++)
        {
            var mid = (low + high) / 2;
            if (Math.Sign(NetPresentValue(mid, flows)) == Math.Sign(NetPresentValue(low, flows))) low = mid;
            else high = mid;
        }
        return (low + high) / 2;
    }

    /// <summary>Periods until the cumulative cash flow turns positive, interpolated within the period; null if never.</summary>
    public static double? PaybackPeriod(IReadOnlyList<double> flows)
    {
        var cumulative = 0.0;
        for (var t = 0; t < flows.Count; t++)
        {
            var before = cumulative;
            cumulative += flows[t];
            if (cumulative >= 0 && before < 0) return t - 1 + -before / flows[t];
        }
        return null;
    }

    /// <summary>Compound growth: what an amount becomes after n periods at rate r, compounded k times per period.</summary>
    public static double FutureValue(double amount, double annualRate, int years, int compoundsPerYear = 1) =>
        amount * Math.Pow(1 + annualRate / compoundsPerYear, years * compoundsPerYear);

    /// <summary>The rule of 72 against the exact answer: years to double = ln 2 / ln(1 + r).</summary>
    public static (double RuleOf72, double Exact) YearsToDouble(double annualRatePercent) =>
        (72 / annualRatePercent, Math.Log(2) / Math.Log(1 + annualRatePercent / 100));
}
