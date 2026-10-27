using CSharpExercises.ErrorHandling;

namespace CSharpExercises.Domain;

/// <summary>
/// A value object: defined by its value, immutable, and impossible to create in an invalid state. The constructor is
/// private, so the only way in is <see cref="Create"/>, and every <see cref="EmailAddress"/> in the program is valid:
/// no method that receives one ever needs to check it again.
/// </summary>
public sealed record EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public string Domain => Value[(Value.IndexOf('@') + 1)..];

    /// <summary>Validates and normalises (trimmed, lower case), reusing the rules from <see cref="SignupValidator"/>.</summary>
    public static Result<EmailAddress> Create(string? input) => SignupValidator.ParseEmail(input).Map(email => new EmailAddress(email));

    public override string ToString() => Value;
}

/// <summary>
/// An inclusive range of dates. The invariant (start is not after end) is checked once in the constructor, and the
/// operations on it (overlap, intersection, merging) live with the data instead of being repeated wherever dates are compared.
/// </summary>
public readonly record struct DateRange
{
    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start) throw new ArgumentException($"The range cannot end ({end:yyyy-MM-dd}) before it starts ({start:yyyy-MM-dd}).");
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }
    public DateOnly End { get; }

    /// <summary>Both ends count: 1 to 3 March is 3 days (or 2 nights, for a hotel).</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;

    public DateRange? Intersect(DateRange other) =>
        Overlaps(other) ? new DateRange(Max(Start, other.Start), Min(End, other.End)) : null;

    /// <summary>
    /// Combines overlapping and touching ranges: [1–5, 4–8, 9–10, 20–21] becomes [1–10, 20–21].
    /// Sorting by start first means each range only has to be compared with the last merged one: O(n log n).
    /// </summary>
    public static IReadOnlyList<DateRange> Merge(IEnumerable<DateRange> ranges)
    {
        List<DateRange> merged = [];
        foreach (var range in ranges.OrderBy(r => r.Start))
        {
            if (merged.Count > 0 && range.Start.DayNumber <= merged[^1].End.DayNumber + 1)
                merged[^1] = new DateRange(merged[^1].Start, Max(merged[^1].End, range.End));
            else
                merged.Add(range);
        }
        return merged;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd}..{End:yyyy-MM-dd}";

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;
    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}
