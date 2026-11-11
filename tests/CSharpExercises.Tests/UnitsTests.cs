using CSharpExercises.Units;

namespace CSharpExercises.Tests;

public class UnitsTests
{
    [Theory]
    [InlineData(100, "km/h", "m/s", 27.7777778)]
    [InlineData(1, "mi", "km", 1.609344)]
    [InlineData(1, "L", "cm^3", 1000)]
    [InlineData(1, "kW*h", "J", 3.6e6)]
    [InlineData(1, "N", "kg*m/s^2", 1)]
    [InlineData(9.81, "m/s/s", "ft/s^2", 32.1850394)]
    [InlineData(90, "min", "h", 1.5)]
    [InlineData(1, "mm", "m", 0.001)]
    public void Convert_CompoundUnits(double value, string from, string to, double expected)
    {
        Assert.Equal(expected, UnitConverter.Convert(value, from, to), 6);
    }

    [Fact]
    public void Convert_RejectsMismatchedDimensions()
    {
        var error = Assert.Throws<InvalidOperationException>(() => UnitConverter.Convert(1, "N", "J"));
        Assert.Contains("M L T^-2", error.Message);
        Assert.Contains("M L^2 T^-2", error.Message);

        Assert.Throws<FormatException>(() => UnitConverter.Convert(1, "furlong", "m"));
        Assert.Throws<FormatException>(() => UnitConverter.Convert(1, "kft", "m")); // feet don't take prefixes
    }

    [Fact]
    public void Dimension_ArithmeticAddsExponents()
    {
        var (_, energy) = UnitConverter.Parse("W*s");

        Assert.Equal(new Dimension(2, 1, -2), energy);
        Assert.Equal(Dimension.None, UnitConverter.Parse("m/km").Dimension);
        Assert.Equal("dimensionless", Dimension.None.ToString());
    }

    [Theory]
    [InlineData(100, TemperatureScale.Celsius, TemperatureScale.Fahrenheit, 212)]
    [InlineData(-40, TemperatureScale.Celsius, TemperatureScale.Fahrenheit, -40)]
    [InlineData(32, TemperatureScale.Fahrenheit, TemperatureScale.Kelvin, 273.15)]
    [InlineData(0, TemperatureScale.Kelvin, TemperatureScale.Celsius, -273.15)]
    public void Temperature_ReadingsUseTheOffset(double reading, TemperatureScale from, TemperatureScale to, double expected)
    {
        Assert.Equal(expected, Temperature.Convert(reading, from, to), 9);
    }

    [Fact]
    public void Temperature_DifferencesDoNot()
    {
        Assert.Equal(18, Temperature.ConvertDifference(10, TemperatureScale.Celsius, TemperatureScale.Fahrenheit), 9);
        Assert.Equal(10, Temperature.ConvertDifference(10, TemperatureScale.Celsius, TemperatureScale.Kelvin), 9);
        Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.Convert(-500, TemperatureScale.Fahrenheit, TemperatureScale.Celsius));
    }
}
