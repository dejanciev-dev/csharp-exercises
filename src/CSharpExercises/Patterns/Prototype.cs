namespace CSharpExercises.Patterns;

/// <summary>
/// Prototype: create objects by copying a configured template instead of building them from scratch. The important
/// detail is copy depth: a shallow copy (<see cref="object.MemberwiseClone"/>) shares nested objects, so changing the
/// copy's list changes the original's too; a deep copy clones what each object owns. A registry of named prototypes
/// gives pre-configured starting points.
/// </summary>
public sealed class ReportTemplate
{
    public string Title { get; set; } = "";
    public List<string> Sections { get; set; } = [];
    public Dictionary<string, string> Settings { get; set; } = [];

    /// <summary>Copies the fields only: the lists and dictionaries are shared with the original.</summary>
    public ReportTemplate ShallowCopy() => (ReportTemplate)MemberwiseClone();

    /// <summary>Copies the collections too, so the clone can be changed independently.</summary>
    public ReportTemplate DeepCopy() => new()
    {
        Title = Title,
        Sections = [.. Sections],
        Settings = new Dictionary<string, string>(Settings),
    };
}

public sealed class TemplateRegistry
{
    private readonly Dictionary<string, ReportTemplate> _prototypes = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, ReportTemplate prototype) => _prototypes[name] = prototype.DeepCopy();

    /// <summary>Always hands out a deep copy, so callers can't modify the registered prototype.</summary>
    public ReportTemplate Create(string name) =>
        _prototypes.TryGetValue(name, out var prototype) ? prototype.DeepCopy() : throw new KeyNotFoundException($"No template '{name}'.");
}
