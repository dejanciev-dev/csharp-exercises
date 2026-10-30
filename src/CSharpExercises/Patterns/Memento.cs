namespace CSharpExercises.Patterns;

/// <summary>
/// Memento: an object saves its own state into an opaque snapshot that others can store but not inspect or modify, and
/// can later restore from it. The caretaker (here, a checkpoint list) keeps history without knowing the originator's
/// internals, so encapsulation survives. Unlike Command, which records operations, Memento records states.
/// </summary>
public sealed class DrawingCanvas
{
    private readonly List<string> _layers = [];
    private string _brush = "pencil";
    private int _zoom = 100;

    public IReadOnlyList<string> Layers => _layers;
    public string Brush => _brush;
    public int Zoom => _zoom;

    public void AddLayer(string name) => _layers.Add(name);
    public void SetBrush(string brush) => _brush = brush;
    public void SetZoom(int percent) => _zoom = Math.Clamp(percent, 10, 800);

    /// <summary>The memento: callers hold it but can't see inside (the type and its contents are private).</summary>
    public interface ISnapshot
    {
        DateTimeOffset TakenAt { get; }
    }

    private sealed record Snapshot(string[] Layers, string Brush, int Zoom, DateTimeOffset TakenAt) : ISnapshot;

    public ISnapshot Save(DateTimeOffset now) => new Snapshot([.. _layers], _brush, _zoom, now);

    public void Restore(ISnapshot snapshot)
    {
        if (snapshot is not Snapshot s) throw new ArgumentException("Not a snapshot of this canvas.", nameof(snapshot));
        _layers.Clear();
        _layers.AddRange(s.Layers);
        (_brush, _zoom) = (s.Brush, s.Zoom);
    }
}

/// <summary>The caretaker: named checkpoints, with a cap so history can't grow forever.</summary>
public sealed class Checkpoints(int capacity)
{
    private readonly LinkedList<(string Name, DrawingCanvas.ISnapshot Snapshot)> _saved = new();

    public IEnumerable<string> Names => _saved.Select(s => s.Name);

    public void Add(string name, DrawingCanvas.ISnapshot snapshot)
    {
        _saved.AddLast((name, snapshot));
        if (_saved.Count > capacity) _saved.RemoveFirst(); // forget the oldest
    }

    public DrawingCanvas.ISnapshot Get(string name) =>
        _saved.LastOrDefault(s => s.Name == name).Snapshot ?? throw new KeyNotFoundException($"No checkpoint '{name}'.");
}
