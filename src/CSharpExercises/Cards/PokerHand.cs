namespace CSharpExercises.Cards;

public enum HandCategory { HighCard, Pair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush }

/// <summary>
/// A hand's strength: its category, then tiebreak ranks compared in order. Grouping the ranks by how often they
/// appear (most first, then highest first) gives both at once: K K 7 7 2 is the pattern 2-2-1 (two pair) with
/// tiebreakers K, 7, 2. The ace plays low in exactly one place, the "wheel" A-2-3-4-5, a straight whose high card is 5.
/// </summary>
public sealed record HandValue(HandCategory Category, IReadOnlyList<int> Tiebreakers) : IComparable<HandValue>
{
    public int CompareTo(HandValue? other)
    {
        if (other is null) return 1;
        if (Category != other.Category) return Category.CompareTo(other.Category);
        for (var i = 0; i < Math.Min(Tiebreakers.Count, other.Tiebreakers.Count); i++)
            if (Tiebreakers[i] != other.Tiebreakers[i]) return Tiebreakers[i].CompareTo(other.Tiebreakers[i]);
        return 0;
    }

    public static bool operator >(HandValue a, HandValue b) => a.CompareTo(b) > 0;
    public static bool operator <(HandValue a, HandValue b) => a.CompareTo(b) < 0;
}

public static class PokerHand
{
    public static HandValue Evaluate(IReadOnlyList<Card> hand)
    {
        if (hand.Count != 5) throw new ArgumentException("A poker hand has five cards.", nameof(hand));
        if (hand.Distinct().Count() != 5) throw new ArgumentException("Duplicate card.", nameof(hand));

        var groups = hand.GroupBy(c => c.Rank)
            .Select(g => (Rank: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count).ThenByDescending(g => g.Rank)
            .ToList();
        var ranks = groups.Select(g => g.Rank).ToList();
        var counts = groups.Select(g => g.Count).ToList();

        var flush = hand.All(c => c.Suit == hand[0].Suit);
        int? straightHigh = counts.Count == 5
            ? ranks[0] - ranks[4] == 4 ? ranks[0]
            : ranks.SequenceEqual([14, 5, 4, 3, 2]) ? 5
            : null
            : null;

        if (straightHigh is int high) return new(flush ? HandCategory.StraightFlush : HandCategory.Straight, [high]);
        if (flush) return new(HandCategory.Flush, ranks);

        var category = counts switch
        {
            [4, 1] => HandCategory.FourOfAKind,
            [3, 2] => HandCategory.FullHouse,
            [3, 1, 1] => HandCategory.ThreeOfAKind,
            [2, 2, 1] => HandCategory.TwoPair,
            [2, 1, 1, 1] => HandCategory.Pair,
            _ => HandCategory.HighCard,
        };
        return new(category, ranks);
    }

    public static HandValue Evaluate(string hand) => Evaluate(Card.ParseMany(hand));

    /// <summary>The best five-card hand out of more cards, e.g. hold'em's two hole cards plus five on the board.</summary>
    public static (HandValue Value, IReadOnlyList<Card> Cards) Best(IReadOnlyList<Card> cards)
    {
        if (cards.Count < 5) throw new ArgumentException("Need at least five cards.", nameof(cards));
        (HandValue Value, IReadOnlyList<Card> Cards)? best = null;
        foreach (var five in Combinations(cards, 5))
        {
            var value = Evaluate(five);
            if (best is null || value > best.Value.Value) best = (value, five);
        }
        return best!.Value;
    }

    private static IEnumerable<Card[]> Combinations(IReadOnlyList<Card> cards, int k, int start = 0)
    {
        if (k == 0)
        {
            yield return [];
            yield break;
        }
        for (var i = start; i <= cards.Count - k; i++)
            foreach (var rest in Combinations(cards, k - 1, i + 1))
                yield return [cards[i], .. rest];
    }
}
