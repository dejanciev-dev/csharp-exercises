using System.Globalization;
using System.Text;

namespace CSharpExercises.Patterns;

/// <summary>
/// The implementation side of the bridge: how to draw primitives.
/// </summary>
public interface IDrawingApi
{
    string Circle(double x, double y, double radius);
    string Line(double x1, double y1, double x2, double y2);
}

public sealed class SvgApi : IDrawingApi
{
    public string Circle(double x, double y, double r) => $"<circle cx=\"{N(x)}\" cy=\"{N(y)}\" r=\"{N(r)}\"/>";
    public string Line(double x1, double y1, double x2, double y2) => $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\"/>";
    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}

public sealed class CanvasApi : IDrawingApi
{
    public string Circle(double x, double y, double r) => FormattableString.Invariant($"ctx.arc({x}, {y}, {r}, 0, 2 * Math.PI);");
    public string Line(double x1, double y1, double x2, double y2) => FormattableString.Invariant($"ctx.moveTo({x1}, {y1}); ctx.lineTo({x2}, {y2});");
}

/// <summary>
/// Bridge: separate an abstraction (what to draw: figures) from its implementation (how: SVG, canvas), so both can
/// vary independently. With inheritance alone, 3 figures × 2 back-ends would need 6 classes, and every new back-end
/// would add 3 more; with a bridge it's 3 + 2, and adding either side touches nothing on the other.
/// </summary>
public abstract class Figure(IDrawingApi api)
{
    protected IDrawingApi Api { get; } = api;
    public abstract string Draw();
}

public sealed class Dot(IDrawingApi api, double x, double y) : Figure(api)
{
    public override string Draw() => Api.Circle(x, y, 1);
}

public sealed class Ring(IDrawingApi api, double x, double y, double radius) : Figure(api)
{
    public override string Draw() => Api.Circle(x, y, radius);
}

/// <summary>A refined abstraction built only from primitives, so it works with every back-end for free.</summary>
public sealed class Triangle(IDrawingApi api, (double X, double Y) a, (double X, double Y) b, (double X, double Y) c) : Figure(api)
{
    public override string Draw() => new StringBuilder()
        .Append(Api.Line(a.X, a.Y, b.X, b.Y))
        .Append(Api.Line(b.X, b.Y, c.X, c.Y))
        .Append(Api.Line(c.X, c.Y, a.X, a.Y))
        .ToString();
}
