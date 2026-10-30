namespace CSharpExercises.DataProcessing;

/// <summary>
/// Sorting data that doesn't fit in memory: read it in chunks that do, sort each chunk and write it to a temporary
/// file (a "run"), then merge all the runs at once with a k-way merge, a min-heap holding the current head line of each
/// run. Memory use is one chunk while sorting and one line per run while merging, however big the input.
/// </summary>
public static class ExternalSort
{
    public sealed record Stats(int Runs, long Lines);

    public static Stats Sort(TextReader input, TextWriter output, int linesPerChunk, StringComparer? comparer = null)
    {
        comparer ??= StringComparer.Ordinal;
        var runs = new List<string>();
        try
        {
            long lines = 0;
            var chunk = new List<string>(linesPerChunk);
            while (input.ReadLine() is { } line)
            {
                chunk.Add(line);
                lines++;
                if (chunk.Count == linesPerChunk) runs.Add(WriteRun(chunk, comparer));
            }
            if (chunk.Count > 0) runs.Add(WriteRun(chunk, comparer));

            MergeRuns(runs, output, comparer);
            return new Stats(runs.Count, lines);
        }
        finally
        {
            foreach (var run in runs) File.Delete(run);
        }
    }

    /// <summary>Merges any number of sorted sequences into one sorted sequence, lazily.</summary>
    public static IEnumerable<T> KWayMerge<T>(IEnumerable<IEnumerable<T>> sources, IComparer<T>? comparer = null)
    {
        comparer ??= Comparer<T>.Default;
        var enumerators = sources.Select(s => s.GetEnumerator()).ToList();
        try
        {
            // Ties broken by source index keep the merge stable.
            var heap = new PriorityQueue<int, (T Value, int Source)>(Comparer<(T Value, int Source)>.Create((a, b) =>
                comparer.Compare(a.Value, b.Value) is var c && c != 0 ? c : a.Source.CompareTo(b.Source)));
            for (var i = 0; i < enumerators.Count; i++)
                if (enumerators[i].MoveNext()) heap.Enqueue(i, (enumerators[i].Current, i));

            while (heap.TryDequeue(out var source, out var head))
            {
                yield return head.Value;
                if (enumerators[source].MoveNext()) heap.Enqueue(source, (enumerators[source].Current, source));
            }
        }
        finally
        {
            foreach (var e in enumerators) e.Dispose();
        }
    }

    private static string WriteRun(List<string> chunk, StringComparer comparer)
    {
        chunk.Sort(comparer);
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, chunk);
        chunk.Clear();
        return path;
    }

    private static void MergeRuns(List<string> runs, TextWriter output, StringComparer comparer)
    {
        var readers = runs.Select(r => new StreamReader(r)).ToList();
        try
        {
            foreach (var line in KWayMerge(readers.Select(ReadLines), comparer)) output.WriteLine(line);
        }
        finally
        {
            foreach (var reader in readers) reader.Dispose();
        }
    }

    private static IEnumerable<string> ReadLines(StreamReader reader)
    {
        while (reader.ReadLine() is { } line) yield return line;
    }
}
