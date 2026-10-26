namespace CSharpExercises.Linq;

/// <summary>
/// Re-implementations of common LINQ operators, showing how LINQ works inside.
/// The "My" prefix avoids clashing with System.Linq, which is always in scope.
/// <para>
/// Three kinds of operator:
/// <list type="bullet">
/// <item><b>Deferred, streaming</b> (Where, Select, Take, ...): nothing runs until you enumerate, then items flow through one at a time.</item>
/// <item><b>Deferred, buffering</b> (Reverse, OrderBy, GroupBy): still lazy, but must read the whole source before yielding anything.</item>
/// <item><b>Immediate</b> (Count, First, ToList, ...): run the query right away and return a value.</item>
/// </list>
/// </para>
/// </summary>
public static class MyLinq
{
    // Each deferred operator validates its arguments first and then returns a local iterator function.
    // An iterator's body doesn't run until the first MoveNext, so validating inside it would delay
    // a null-argument error until the query is enumerated, far from the line with the mistake.

    public static IEnumerable<T> MyWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        return Iterator();

        IEnumerable<T> Iterator()
        {
            foreach (var item in source)
            {
                if (predicate(item)) yield return item;
            }
        }
    }

    public static IEnumerable<TResult> MySelect<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            foreach (var item in source) yield return selector(item);
        }
    }

    public static IEnumerable<TResult> MySelectMany<T, TResult>(this IEnumerable<T> source, Func<T, IEnumerable<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            foreach (var item in source)
            {
                foreach (var inner in selector(item)) yield return inner;
            }
        }
    }

    public static IEnumerable<T> MyTake<T>(this IEnumerable<T> source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Iterator();

        IEnumerable<T> Iterator()
        {
            // Count down a copy: decrementing the captured `count` itself would leave it at 0
            // for the next enumeration of the same query.
            var remaining = count;
            if (remaining <= 0) yield break;

            foreach (var item in source)
            {
                yield return item;

                // Stop before asking the source for another item, so Take(3) pulls exactly 3.
                if (--remaining == 0) yield break;
            }
        }
    }

    public static IEnumerable<T> MySkip<T>(this IEnumerable<T> source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Iterator();

        IEnumerable<T> Iterator()
        {
            var skipped = 0;
            foreach (var item in source)
            {
                if (skipped < count) skipped++;
                else yield return item;
            }
        }
    }

    public static IEnumerable<T> MyTakeWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        return Iterator();

        IEnumerable<T> Iterator()
        {
            foreach (var item in source)
            {
                if (!predicate(item)) yield break;
                yield return item;
            }
        }
    }

    /// <summary>Streams each item the first time it is seen; the set of seen items is created fresh per enumeration.</summary>
    public static IEnumerable<T> MyDistinct<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Iterator();

        IEnumerable<T> Iterator()
        {
            var seen = new HashSet<T>();
            foreach (var item in source)
            {
                if (seen.Add(item)) yield return item;
            }
        }
    }

    /// <summary>Pairs items from two sequences, stopping at the end of the shorter one.</summary>
    public static IEnumerable<(TFirst First, TSecond Second)> MyZip<TFirst, TSecond>(
        this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return Iterator();

        IEnumerable<(TFirst, TSecond)> Iterator()
        {
            using var a = first.GetEnumerator();
            using var b = second.GetEnumerator();
            while (a.MoveNext() && b.MoveNext()) yield return (a.Current, b.Current);
        }
    }

    public static IEnumerable<T> MyReverse<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Iterator();

        IEnumerable<T> Iterator()
        {
            // The last item can't be yielded until the source has been read to the end.
            var buffer = source.MyToList();
            for (var i = buffer.Count - 1; i >= 0; i--) yield return buffer[i];
        }
    }

    /// <summary>A stable sort: items with equal keys keep their original order, as with the real OrderBy.</summary>
    public static IEnumerable<T> MyOrderBy<T, TKey>(
        this IEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        var keyComparer = comparer ?? Comparer<TKey>.Default;
        return Iterator();

        IEnumerable<T> Iterator()
        {
            // Compute each key once, and remember each item's position to break ties
            // (List.Sort alone is not stable).
            var entries = new List<(T Item, TKey Key, int Index)>();
            foreach (var item in source) entries.Add((item, keySelector(item), entries.Count));

            entries.Sort((x, y) =>
            {
                var byKey = keyComparer.Compare(x.Key, y.Key);
                return byKey != 0 ? byKey : x.Index.CompareTo(y.Index);
            });

            foreach (var entry in entries) yield return entry.Item;
        }
    }

    /// <summary>Groups come out in the order their keys first appear in the source.</summary>
    public static IEnumerable<IGrouping<TKey, T>> MyGroupBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        return Iterator();

        IEnumerable<IGrouping<TKey, T>> Iterator()
        {
            var lookup = new Dictionary<TKey, Grouping<TKey, T>>();
            var inOrder = new List<Grouping<TKey, T>>();
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!lookup.TryGetValue(key, out var group))
                {
                    group = new Grouping<TKey, T>(key);
                    lookup[key] = group;
                    inOrder.Add(group);
                }

                group.Items.Add(item);
            }

            foreach (var group in inOrder) yield return group;
        }
    }

    public static List<T> MyToList<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = new List<T>();
        foreach (var item in source) list.Add(item);
        return list;
    }

    public static int MyCount<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Like the real Count(), don't walk the sequence when it already knows its size.
        if (source is ICollection<T> collection) return collection.Count;
        if (source is IReadOnlyCollection<T> readOnly) return readOnly.Count;

        var count = 0;
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext()) count++;
        return count;
    }

    /// <summary>Stops at the first match instead of reading the whole source.</summary>
    public static bool MyAny<T>(this IEnumerable<T> source, Func<T, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            if (predicate is null || predicate(item)) return true;
        }

        return false;
    }

    public static T MyFirst<T>(this IEnumerable<T> source, Func<T, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            if (predicate is null || predicate(item)) return item;
        }

        throw new InvalidOperationException("Sequence contains no matching element.");
    }

    public static T? MyFirstOrDefault<T>(this IEnumerable<T> source, Func<T, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            if (predicate is null || predicate(item)) return item;
        }

        return default;
    }

    public static TAccumulate MyAggregate<T, TAccumulate>(
        this IEnumerable<T> source, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(func);

        var result = seed;
        foreach (var item in source) result = func(result, item);
        return result;
    }

    private sealed class Grouping<TKey, TElement>(TKey key) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;

        public List<TElement> Items { get; } = [];

        public IEnumerator<TElement> GetEnumerator() => Items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
