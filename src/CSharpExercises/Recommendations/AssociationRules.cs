namespace CSharpExercises.Recommendations;

public sealed record Rule(IReadOnlySet<string> If, IReadOnlySet<string> Then, double Support, double Confidence, double Lift)
{
    public override string ToString() =>
        $"{{{string.Join(", ", If.Order())}}} → {{{string.Join(", ", Then.Order())}}} (conf {Confidence:0.00}, lift {Lift:0.00})";
}

/// <summary>
/// Market-basket analysis with Apriori: find itemsets bought together in at least a minimum share of baskets (support),
/// growing candidates one item at a time and pruning any whose subsets were already infrequent, then turn them into
/// rules "if A then B" with confidence P(B | A) and lift (confidence / P(B): above 1 means a real association, not just
/// two popular items). The classic "customers who bought this also bought…".
/// </summary>
public static class AssociationRules
{
    public static IReadOnlyDictionary<string, double> FrequentItemsets(IReadOnlyList<IReadOnlySet<string>> baskets, double minSupport)
    {
        var result = new Dictionary<string, double>();
        var current = baskets.SelectMany(b => b).Distinct().Select(i => new SortedSet<string> { i }).ToList();

        while (current.Count > 0)
        {
            var frequent = current
                .Select(set => (Set: set, Support: (double)baskets.Count(b => set.IsSubsetOf(b)) / baskets.Count))
                .Where(c => c.Support >= minSupport)
                .ToList();
            foreach (var (set, support) in frequent) result[Key(set)] = support;

            // Join step: combine frequent k-itemsets into (k+1)-candidates; prune those with an infrequent subset.
            var frequentKeys = frequent.Select(f => Key(f.Set)).ToHashSet();
            var nextSize = frequent.Count == 0 ? 0 : frequent[0].Set.Count + 1;
            current = frequent
                .SelectMany(a => frequent.Select(b => new SortedSet<string>(a.Set.Union(b.Set))))
                .Where(c => c.Count == nextSize)
                .DistinctBy(Key)
                .Where(c => c.All(item => frequentKeys.Contains(Key(c.Where(x => x != item)))))
                .ToList();
        }
        return result;
    }

    public static IReadOnlyList<Rule> Rules(IReadOnlyList<IReadOnlySet<string>> baskets, double minSupport, double minConfidence)
    {
        var supports = FrequentItemsets(baskets, minSupport);
        var rules = new List<Rule>();
        foreach (var (key, support) in supports)
        {
            var items = key.Split('|');
            if (items.Length < 2) continue;
            // Every non-empty proper subset can be the "if" side.
            for (var mask = 1; mask < (1 << items.Length) - 1; mask++)
            {
                var ifSide = items.Where((_, i) => (mask >> i & 1) == 1).ToHashSet();
                var thenSide = items.Where((_, i) => (mask >> i & 1) == 0).ToHashSet();
                var confidence = support / supports[Key(ifSide)];
                if (confidence < minConfidence) continue;
                var lift = confidence / supports[Key(thenSide)];
                rules.Add(new Rule(ifSide, thenSide, support, Math.Round(confidence, 4), Math.Round(lift, 4)));
            }
        }
        return rules.OrderByDescending(r => r.Lift).ThenByDescending(r => r.Confidence).ToList();
    }

    private static string Key(IEnumerable<string> items) => string.Join('|', items.Order(StringComparer.Ordinal));
}
