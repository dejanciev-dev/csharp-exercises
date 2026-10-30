namespace CSharpExercises.Simulations;

/// <summary>
/// Craig Reynolds' boids: flocking emerges from three local rules applied to each bird using only its neighbours:
/// separation (don't crowd), alignment (steer toward the average heading) and cohesion (move toward the centre). No bird
/// knows about the flock, yet the flock forms. Positions wrap around the edges of a square world.
/// </summary>
public sealed class Boids
{
    public record struct Boid(double X, double Y, double Vx, double Vy);

    private readonly double _size;
    private Boid[] _boids;

    public Boids(int count, double worldSize, Random random)
    {
        _size = worldSize;
        _boids = Enumerable.Range(0, count).Select(_ =>
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            return new Boid(random.NextDouble() * worldSize, random.NextDouble() * worldSize, Math.Cos(angle), Math.Sin(angle));
        }).ToArray();
    }

    public IReadOnlyList<Boid> Flock => _boids;

    public double Radius { get; init; } = 10;
    public double Separation { get; init; } = 0.05;
    public double Alignment { get; init; } = 0.05;
    public double Cohesion { get; init; } = 0.005;
    public double MaxSpeed { get; init; } = 2;

    public void Step()
    {
        // Compute every new velocity from the old state, so the update order doesn't matter.
        _boids = _boids.Select(b =>
        {
            var neighbours = _boids.Where(o => !o.Equals(b) && Distance(b, o) < Radius).ToList();
            var (vx, vy) = (b.Vx, b.Vy);
            if (neighbours.Count > 0)
            {
                foreach (var n in neighbours)
                {
                    var (dx, dy) = Delta(b, n);
                    var d = Math.Max(Math.Sqrt(dx * dx + dy * dy), 0.01);
                    vx -= Separation * dx / (d * d);
                    vy -= Separation * dy / (d * d);
                }
                vx += Alignment * (neighbours.Average(n => n.Vx) - b.Vx);
                vy += Alignment * (neighbours.Average(n => n.Vy) - b.Vy);
                vx += Cohesion * neighbours.Average(n => Delta(b, n).Dx);
                vy += Cohesion * neighbours.Average(n => Delta(b, n).Dy);
            }

            var speed = Math.Sqrt(vx * vx + vy * vy);
            if (speed > MaxSpeed) (vx, vy) = (vx / speed * MaxSpeed, vy / speed * MaxSpeed);
            return new Boid(Wrap(b.X + vx), Wrap(b.Y + vy), vx, vy);
        }).ToArray();
    }

    /// <summary>How aligned the flock is: 1 when every boid flies the same way, near 0 when headings are random.</summary>
    public double Polarisation()
    {
        var (sx, sy) = (0.0, 0.0);
        foreach (var b in _boids)
        {
            var speed = Math.Max(Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy), 1e-9);
            (sx, sy) = (sx + b.Vx / speed, sy + b.Vy / speed);
        }
        return Math.Sqrt(sx * sx + sy * sy) / _boids.Length;
    }

    /// <summary>The shortest offset on a wrapping world (the other side may be closer).</summary>
    private (double Dx, double Dy) Delta(Boid from, Boid to)
    {
        double Shortest(double d) => d > _size / 2 ? d - _size : d < -_size / 2 ? d + _size : d;
        return (Shortest(to.X - from.X), Shortest(to.Y - from.Y));
    }

    private double Distance(Boid a, Boid b)
    {
        var (dx, dy) = Delta(a, b);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private double Wrap(double v) => (v % _size + _size) % _size;
}
