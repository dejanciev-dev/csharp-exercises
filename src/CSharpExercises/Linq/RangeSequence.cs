using System.Collections;

namespace CSharpExercises.Linq;

/// <summary>
/// <c>Enumerable.Range</c> written without <c>yield return</c>, to show what the compiler generates for an iterator:
/// a sequence that hands out cursors, and a cursor that remembers its position between <c>MoveNext</c> calls.
/// (The compiler merges both into one class to save an allocation; they are separate here for clarity.)
/// </summary>
public sealed class RangeSequence : IEnumerable<int>
{
    private readonly int _start;
    private readonly int _count;

    public RangeSequence(int start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _start = start;
        _count = count;
    }

    /// <summary>The same sequence written with <c>yield return</c>, for comparison.</summary>
    public static IEnumerable<int> WithYield(int start, int count)
    {
        for (var i = 0; i < count; i++) yield return start + i;
    }

    // Every call gets its own cursor, so two foreach loops over the same sequence don't interfere.
    public IEnumerator<int> GetEnumerator() => new Enumerator(_start, _count);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(int start, int count) : IEnumerator<int>
    {
        // -1 means "before the first item"; count means "past the last item".
        private int _index = -1;

        public int Current =>
            _index >= 0 && _index < count
                ? start + _index
                : throw new InvalidOperationException("Enumeration has not started or has already finished.");

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_index < count) _index++;
            return _index < count;
        }

        public void Reset() => _index = -1;

        public void Dispose() { }
    }
}
