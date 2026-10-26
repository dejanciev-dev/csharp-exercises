using System.Collections;
using CSharpExercises.Linq;

namespace CSharpExercises.Tests;

public class LinqInternalsTests
{
    private static readonly int[] Numbers = [5, 3, 8, 3, 1, 9, 5, 2];

    /// <summary>1, 2, 3, ... forever, calling <paramref name="onPull"/> each time an item is produced.</summary>
    private static IEnumerable<int> Naturals(Action? onPull = null)
    {
        for (var i = 1; ; i++)
        {
            onPull?.Invoke();
            yield return i;
        }
    }

    [Fact]
    public void Operators_MatchSystemLinq()
    {
        Assert.Equal(Numbers.Where(n => n > 3), Numbers.MyWhere(n => n > 3));
        Assert.Equal(Numbers.Select(n => n * 10), Numbers.MySelect(n => n * 10));
        Assert.Equal(Numbers.SelectMany(n => Enumerable.Repeat(n, 2)), Numbers.MySelectMany(n => Enumerable.Repeat(n, 2)));
        Assert.Equal(Numbers.Take(3), Numbers.MyTake(3));
        Assert.Equal(Numbers.Skip(5), Numbers.MySkip(5));
        Assert.Equal(Numbers.TakeWhile(n => n != 1), Numbers.MyTakeWhile(n => n != 1));
        Assert.Equal(Numbers.Distinct(), Numbers.MyDistinct());
        Assert.Equal(Numbers.Zip("abc"), Numbers.MyZip("abc"));
        Assert.Equal(Numbers.Reverse(), Numbers.MyReverse());
        Assert.Equal(Numbers.OrderBy(n => n), Numbers.MyOrderBy(n => n));
        Assert.Equal(Numbers.Count(), Numbers.MyCount());
        Assert.Equal(Numbers.Aggregate(0, (sum, n) => sum + n), Numbers.MyAggregate(0, (sum, n) => sum + n));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(100)]
    public void TakeAndSkip_EdgeCounts(int count)
    {
        Assert.Equal(Numbers.Take(count), Numbers.MyTake(count));
        Assert.Equal(Numbers.Skip(count), Numbers.MySkip(count));
    }

    [Fact]
    public void Query_DoesNothingUntilEnumerated()
    {
        var calls = 0;
        var query = Numbers.MyWhere(n => { calls++; return n > 3; }).MySelect(n => n * 2);

        Assert.Equal(0, calls);

        _ = query.MyToList();
        Assert.Equal(Numbers.Length, calls);
    }

    [Fact]
    public void Query_SeesChangesMadeToSourceBeforeEnumeration()
    {
        var source = new List<int> { 1, 2 };
        var doubled = source.MySelect(n => n * 2);

        source.Add(3);

        Assert.Equal([2, 4, 6], doubled);
    }

    [Fact]
    public void Streaming_WorksOnInfiniteSequences_AndPullsOnlyWhatIsNeeded()
    {
        var pulled = 0;

        var firstEvenSquares = Naturals(() => pulled++).MyWhere(n => n % 2 == 0).MySelect(n => n * n).MyTake(3);

        Assert.Equal([4, 16, 36], firstEvenSquares);
        Assert.Equal(6, pulled); // 1..6, not one more
    }

    [Fact]
    public void ImmediateOperators_StopEarly()
    {
        var pulled = 0;

        Assert.True(Naturals(() => pulled++).MyAny(n => n > 4));
        Assert.Equal(5, pulled);
        Assert.Equal(10, Naturals().MyFirst(n => n % 10 == 0));
    }

    [Fact]
    public void Query_CanBeEnumeratedMoreThanOnce()
    {
        var firstTwo = Numbers.MyTake(2);
        var distinct = Numbers.MyDistinct();

        Assert.Equal(firstTwo.MyToList(), firstTwo.MyToList());
        Assert.Equal(distinct.MyToList(), distinct.MyToList());
    }

    [Fact]
    public void ArgumentErrors_AreReportedImmediately_NotOnEnumeration()
    {
        int[] missing = null!;

        // No enumeration happens in these lambdas; the exception comes from the call itself.
        Assert.Throws<ArgumentNullException>(() => missing.MyWhere(n => n > 0));
        Assert.Throws<ArgumentNullException>(() => Numbers.MySelect<int, int>(null!));
        Assert.Throws<ArgumentNullException>(() => missing.MyOrderBy(n => n));
    }

    [Fact]
    public void BufferingOperators_AreStillLazy()
    {
        var source = new List<int> { 1, 2 };
        var reversed = source.MyReverse();
        var sorted = source.MyOrderBy(n => -n);

        source.Add(3);

        Assert.Equal([3, 2, 1], reversed);
        Assert.Equal([3, 2, 1], sorted);
    }

    [Fact]
    public void OrderBy_IsStable()
    {
        (string Name, int Age)[] people = [("Ana", 30), ("Ivan", 25), ("Sara", 30), ("Marko", 25), ("Elena", 30)];

        var byAge = people.MyOrderBy(p => p.Age).MySelect(p => p.Name);

        Assert.Equal(["Ivan", "Marko", "Ana", "Sara", "Elena"], byAge);
    }

    [Fact]
    public void OrderBy_AcceptsCustomComparer() =>
        Assert.Equal(["apple", "Banana", "cherry"],
            new[] { "cherry", "apple", "Banana" }.MyOrderBy(s => s, StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void GroupBy_KeepsFirstAppearanceOrder()
    {
        var groups = new[] { "apple", "avocado", "banana", "blueberry", "apricot", "cherry" }
            .MyGroupBy(s => s[0])
            .MyToList();

        Assert.Equal(['a', 'b', 'c'], groups.MySelect(g => g.Key));
        Assert.Equal(["apple", "avocado", "apricot"], groups[0]);
    }

    [Fact]
    public void Count_UsesKnownSizeWithoutEnumerating()
    {
        Assert.Equal(42, new SizedOnlyCollection(42).MyCount());
        Assert.Equal(4, Naturals().MyTake(4).MyCount()); // no known size: has to walk the sequence
    }

    [Fact]
    public void First_And_FirstOrDefault_OnNoMatch()
    {
        Assert.Throws<InvalidOperationException>(() => Numbers.MyFirst(n => n > 100));
        Assert.Equal(0, Numbers.MyFirstOrDefault(n => n > 100));
        Assert.Null(Array.Empty<string>().MyFirstOrDefault());
        Assert.False(Array.Empty<int>().MyAny());
    }

    [Fact]
    public void RangeSequence_MatchesEnumerableRangeAndYieldVersion()
    {
        Assert.Equal(Enumerable.Range(5, 4), new RangeSequence(5, 4));
        Assert.Equal(RangeSequence.WithYield(5, 4), new RangeSequence(5, 4));
        Assert.Empty(new RangeSequence(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeSequence(1, -1));
    }

    [Fact]
    public void RangeSequence_EnumeratorFollowsTheProtocol()
    {
        using var cursor = new RangeSequence(10, 2).GetEnumerator();

        Assert.Throws<InvalidOperationException>(() => cursor.Current); // before the first MoveNext
        Assert.True(cursor.MoveNext());
        Assert.Equal(10, cursor.Current);
        Assert.True(cursor.MoveNext());
        Assert.Equal(11, cursor.Current);
        Assert.False(cursor.MoveNext());
        Assert.False(cursor.MoveNext()); // stays finished
        Assert.Throws<InvalidOperationException>(() => cursor.Current);

        cursor.Reset();
        Assert.True(cursor.MoveNext());
        Assert.Equal(10, cursor.Current);
    }

    [Fact]
    public void RangeSequence_EachForeachGetsItsOwnCursor()
    {
        var range = new RangeSequence(1, 3);

        var pairs = range.MySelectMany(a => range.MySelect(b => (a, b))).MyCount();

        Assert.Equal(9, pairs);
    }

    /// <summary>Knows its size but throws if anyone tries to walk it.</summary>
    private sealed class SizedOnlyCollection(int count) : ICollection<int>
    {
        public int Count => count;
        public bool IsReadOnly => true;
        public IEnumerator<int> GetEnumerator() => throw new InvalidOperationException("Should not be enumerated.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(int item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(int item) => throw new NotSupportedException();
        public void CopyTo(int[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(int item) => throw new NotSupportedException();
    }
}
