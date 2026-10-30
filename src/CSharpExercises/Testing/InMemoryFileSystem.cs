namespace CSharpExercises.Testing;

/// <summary>The file operations the code needs, behind an interface so tests can swap in a fake.</summary>
public interface IFileSystem
{
    bool Exists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
    void Delete(string path);
    IEnumerable<string> List(string directory, string extension);
}

public sealed class RealFileSystem : IFileSystem
{
    public bool Exists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
    public void Delete(string path) => File.Delete(path);
    public IEnumerable<string> List(string directory, string extension) => Directory.EnumerateFiles(directory, "*" + extension);
}

/// <summary>
/// An in-memory fake file system: tests run fast, in parallel, without temp folders to clean up, and can inject failures
/// (a locked file, a full disk) that are hard to reproduce for real. Paths are normalised so "a/b" and "a\b" are the
/// same file, matching Windows and Unix behaviour closely enough for application code.
/// </summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _locked = new(StringComparer.OrdinalIgnoreCase);

    public void Lock(string path) => _locked.Add(Normalise(path));

    public bool Exists(string path) => _files.ContainsKey(Normalise(path));

    public string ReadAllText(string path) =>
        _files.TryGetValue(Normalise(path), out var contents) ? contents : throw new FileNotFoundException($"Could not find file '{path}'.", path);

    public void WriteAllText(string path, string contents)
    {
        var key = Normalise(path);
        if (_locked.Contains(key)) throw new IOException($"The process cannot access the file '{path}' because it is being used by another process.");
        _files[key] = contents;
    }

    public void Delete(string path) => _files.Remove(Normalise(path)); // like File.Delete: no error if missing

    public IEnumerable<string> List(string directory, string extension)
    {
        var prefix = Normalise(directory).TrimEnd('/') + "/";
        return _files.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && k.EndsWith(extension, StringComparison.OrdinalIgnoreCase) && !k[prefix.Length..].Contains('/')).Order();
    }

    private static string Normalise(string path) => path.Replace('\\', '/');
}

/// <summary>Code under test: rotates log files, keeping the newest N. It only knows <see cref="IFileSystem"/>.</summary>
public sealed class LogRotator(IFileSystem files, string directory, int keep)
{
    public IReadOnlyList<string> Rotate()
    {
        var logs = files.List(directory, ".log").OrderDescending().ToList(); // names start with a sortable date
        var removed = logs.Skip(keep).ToList();
        foreach (var path in removed) files.Delete(path);
        return removed;
    }

    public void Append(string date, string line)
    {
        var path = $"{directory}/{date}.log";
        var existing = files.Exists(path) ? files.ReadAllText(path) : "";
        files.WriteAllText(path, existing + line + "\n");
    }
}
