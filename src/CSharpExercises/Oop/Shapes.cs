namespace CSharpExercises.Oop;

/// <summary>
/// Abstraction and polymorphism: every shape knows how to compute its own area and perimeter.
/// </summary>
public abstract class Shape
{
    public abstract double Area { get; }
    public abstract double Perimeter { get; }

    public override string ToString() => $"{GetType().Name} (area: {Area:F2}, perimeter: {Perimeter:F2})";
}

public class Circle(double radius) : Shape
{
    public double Radius { get; } = radius > 0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));

    public override double Area => Math.PI * Radius * Radius;
    public override double Perimeter => 2 * Math.PI * Radius;
}

public class Rectangle(double width, double height) : Shape
{
    public double Width { get; } = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width));
    public double Height { get; } = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height));

    public override double Area => Width * Height;
    public override double Perimeter => 2 * (Width + Height);
}

public class Square(double side) : Rectangle(side, side);
