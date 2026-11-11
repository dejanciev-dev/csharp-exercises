namespace CSharpExercises.Cards;

/// <summary>
/// Blackjack totals: picture cards count 10 and an ace counts 11 unless that would bust, in which case it counts 1.
/// A hand holding an ace that still counts 11 is "soft" (A-6 is soft 17: hitting can't bust it). Only one ace can
/// ever count 11, so a hand is just (hard total with aces as 1, has an ace). That small state also makes the dealer's
/// fixed strategy easy to analyse exactly: with an infinite deck, recurse over every possible next card.
/// </summary>
public static class Blackjack
{
    /// <summary>Card value for blackjack: 2–10 as is, faces 10, ace 1 (raised to 11 by <see cref="Total"/> when it fits).</summary>
    public static int Points(Card card) => card.Rank == 14 ? 1 : Math.Min(card.Rank, 10);

    public static (int Total, bool Soft) Total(IEnumerable<Card> hand)
    {
        var hard = 0;
        var ace = false;
        foreach (var card in hand)
        {
            hard += Points(card);
            ace |= card.Rank == 14;
        }
        return Best(hard, ace);
    }

    private static (int Total, bool Soft) Best(int hard, bool hasAce) =>
        hasAce && hard + 10 <= 21 ? (hard + 10, true) : (hard, false);

    public static bool IsBlackjack(IReadOnlyList<Card> hand) => hand.Count == 2 && Total(hand).Total == 21;

    private static bool DealerStands(int hard, bool hasAce, bool hitSoft17)
    {
        var (total, soft) = Best(hard, hasAce);
        return total > 17 || (total == 17 && !(soft && hitSoft17));
    }

    /// <summary>Plays the dealer's hand from a shoe: hit until 17 or more (and on soft 17 too, under H17 rules).</summary>
    public static IReadOnlyList<Card> PlayDealer(IReadOnlyList<Card> hand, Queue<Card> shoe, bool hitSoft17 = false)
    {
        var cards = hand.ToList();
        while (true)
        {
            var hard = cards.Sum(Points);
            if (DealerStands(hard, cards.Any(c => c.Rank == 14), hitSoft17) || hard > 21) return cards;
            cards.Add(shoe.Dequeue());
        }
    }

    /// <summary>
    /// Probabilities of the dealer finishing on 17, 18, 19, 20, 21 or busting (index 5), from an up card, with an
    /// infinite deck (every draw: 1/13 per rank, 4/13 for a ten-value card). Blackjacks count as 21.
    /// </summary>
    public static double[] DealerOutcomes(int upCardPoints, bool hitSoft17 = false)
    {
        if (upCardPoints is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(upCardPoints));
        var memo = new Dictionary<(int, bool), double[]>();
        return Outcomes(upCardPoints, upCardPoints == 1);

        double[] Outcomes(int hard, bool hasAce)
        {
            if (hard > 21) return [0, 0, 0, 0, 0, 1];
            if (DealerStands(hard, hasAce, hitSoft17))
            {
                var result = new double[6];
                result[Best(hard, hasAce).Total - 17] = 1;
                return result;
            }
            if (memo.TryGetValue((hard, hasAce), out var cached)) return cached;

            var sum = new double[6];
            for (var points = 1; points <= 10; points++)
            {
                var p = points == 10 ? 4.0 / 13 : 1.0 / 13;
                var next = Outcomes(hard + points, hasAce || points == 1);
                for (var i = 0; i < 6; i++) sum[i] += p * next[i];
            }
            return memo[(hard, hasAce)] = sum;
        }
    }
}
