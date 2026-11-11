namespace CSharpExercises.Units;

/// <summary>
/// A physical dimension as exponents of length, mass and time. Speed is L¹T⁻¹, force is M¹L¹T⁻², energy M¹L²T⁻².
/// Multiplying quantities adds exponents and dividing subtracts them, so checking that two units can be converted
/// is just comparing three integers, and it catches mistakes like converting newtons to joules.
/// </summary>
public readonly record struct Dimension(int Length, int Mass, int Time)
{
    public static readonly Dimension None = new(0, 0, 0);
    public static readonly Dimension L = new(1, 0, 0);
    public static readonly Dimension M = new(0, 1, 0);
    public static readonly Dimension T = new(0, 0, 1);

    public static Dimension operator *(Dimension a, Dimension b) =>
        new(a.Length + b.Length, a.Mass + b.Mass, a.Time + b.Time);

    public static Dimension operator /(Dimension a, Dimension b) =>
        new(a.Length - b.Length, a.Mass - b.Mass, a.Time - b.Time);

    public Dimension Pow(int n) => new(Length * n, Mass * n, Time * n);

    public override string ToString()
    {
        var parts = new[] { ("M", Mass), ("L", Length), ("T", Time) }
            .Where(p => p.Item2 != 0)
            .Select(p => p.Item2 == 1 ? p.Item1 : $"{p.Item1}^{p.Item2}")
            .ToList();
        return parts.Count == 0 ? "dimensionless" : string.Join(" ", parts);
    }
}
