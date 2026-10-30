using System.Text;

namespace CSharpExercises.Patterns;

/// <summary>
/// Composite: files and folders share one abstraction, so code asks any entry for its size or searches it without
/// caring whether it's a leaf or a container. The folder's answer is the combination of its children's answers.
/// </summary>
public abstract class FileSystemEntry(string name)
{
    public string Name { get; } = name;
    public abstract long Size { get; }
    public abstract IEnumerable<(string Path, FileSystemEntry Entry)> Walk(string parentPath = "");

    public IEnumerable<string> Find(Func<FileSystemEntry, bool> predicate) =>
        Walk().Where(e => predicate(e.Entry)).Select(e => e.Path);
}

public sealed class FileEntry(string name, long size) : FileSystemEntry(name)
{
    public override long Size => size;

    public override IEnumerable<(string, FileSystemEntry)> Walk(string parentPath = "")
    {
        yield return ($"{parentPath}/{Name}", this);
    }
}

public sealed class FolderEntry(string name) : FileSystemEntry(name)
{
    private readonly List<FileSystemEntry> _children = [];

    public IReadOnlyList<FileSystemEntry> Children => _children;

    public FolderEntry Add(params FileSystemEntry[] entries)
    {
        foreach (var entry in entries)
        {
            if (_children.Any(c => c.Name == entry.Name)) throw new InvalidOperationException($"'{entry.Name}' already exists in '{Name}'.");
            _children.Add(entry);
        }
        return this;
    }

    public override long Size => _children.Sum(c => c.Size);

    public override IEnumerable<(string, FileSystemEntry)> Walk(string parentPath = "")
    {
        var path = $"{parentPath}/{Name}";
        yield return (path, this);
        foreach (var child in _children)
            foreach (var entry in child.Walk(path))
                yield return entry;
    }

    /// <summary>Like the `tree` command, with box-drawing connectors.</summary>
    public string Render()
    {
        var sb = new StringBuilder().Append(Name).Append('\n');
        RenderChildren(this, "", sb);
        return sb.ToString();
    }

    private static void RenderChildren(FolderEntry folder, string indent, StringBuilder sb)
    {
        for (var i = 0; i < folder._children.Count; i++)
        {
            var child = folder._children[i];
            var last = i == folder._children.Count - 1;
            sb.Append(indent).Append(last ? "└── " : "├── ").Append(child.Name);
            sb.Append(child is FileEntry ? $" ({child.Size} B)" : "").Append('\n');
            if (child is FolderEntry sub) RenderChildren(sub, indent + (last ? "    " : "│   "), sb);
        }
    }
}
