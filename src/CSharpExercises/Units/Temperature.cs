namespace CSharpExercises.Units;

public enum TemperatureScale { Celsius, Fahrenheit, Kelvin }

/// <summary>
/// Temperatures don't fit the multiply-by-a-factor model because the scales have different zeros: 0 °C is 32 °F.
/// A reading needs the offset, but a difference doesn't: warming by 10 °C is warming by 18 °F, not by 50 °F.
/// Mixing the two up is a classic bug, so they are separate methods.
/// </summary>
public static class Temperature
{
    public static double Convert(double reading, TemperatureScale from, TemperatureScale to) =>
        FromKelvin(ToKelvin(reading, from), to);

    public static double ConvertDifference(double delta, TemperatureScale from, TemperatureScale to) =>
        delta * Scale(from) / Scale(to);

    private static double Scale(TemperatureScale scale) => scale == TemperatureScale.Fahrenheit ? 5.0 / 9 : 1;

    private static double ToKelvin(double reading, TemperatureScale scale)
    {
        var kelvin = scale switch
        {
            TemperatureScale.Celsius => reading + 273.15,
            TemperatureScale.Fahrenheit => (reading - 32) * 5 / 9 + 273.15,
            _ => reading,
        };
        if (kelvin < 0) throw new ArgumentOutOfRangeException(nameof(reading), "Below absolute zero.");
        return kelvin;
    }

    private static double FromKelvin(double kelvin, TemperatureScale scale) => scale switch
    {
        TemperatureScale.Celsius => kelvin - 273.15,
        TemperatureScale.Fahrenheit => (kelvin - 273.15) * 9 / 5 + 32,
        _ => kelvin,
    };
}
