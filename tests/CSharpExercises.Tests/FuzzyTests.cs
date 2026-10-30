using CSharpExercises.Fuzzy;

namespace CSharpExercises.Tests;

public class FuzzyTests
{
    [Theory]
    [InlineData("MARTHA", "MARHTA", 0.944, 0.961)]
    [InlineData("DWAYNE", "DUANE", 0.822, 0.840)]
    [InlineData("DIXON", "DICKSONX", 0.767, 0.813)]
    public void JaroWinkler_MatchesPublishedValues(string a, string b, double jaro, double winkler)
    {
        Assert.Equal(jaro, JaroWinkler.Jaro(a, b), 3);
        Assert.Equal(winkler, JaroWinkler.Similarity(a, b), 3);
    }

    [Fact]
    public void JaroWinkler_FindsLikelyNames()
    {
        var matches = JaroWinkler.BestMatches("Jonathon", ["Jonathan", "Johnathan", "Joanna", "Nathan"]).Select(m => m.Candidate);

        Assert.Equal(["Jonathan"], matches); // Johnathan scores just under the 0.85 threshold
        Assert.Equal(1, JaroWinkler.Similarity("", ""));
        Assert.Equal(0, JaroWinkler.Similarity("abc", ""));
    }

    [Theory]
    [InlineData("teh", "the", 1)]          // one swap, Levenshtein says 2
    [InlineData("kitten", "sitting", 3)]
    [InlineData("ca", "abc", 3)]            // the OSA variant can't edit a swapped pair again
    [InlineData("", "abc", 3)]
    public void Damerau_CountsSwapsAsOneEdit(string a, string b, int expected) =>
        Assert.Equal(expected, DamerauLevenshtein.Distance(a, b));

    [Fact]
    public void Damerau_BoundedVersionAgrees()
    {
        var random = new Random(1);
        for (var i = 0; i < 300; i++)
        {
            string Word() => new(Enumerable.Range(0, random.Next(0, 8)).Select(_ => (char)('a' + random.Next(4))).ToArray());
            var (a, b) = (Word(), Word());
            var exact = DamerauLevenshtein.Distance(a, b);
            Assert.Equal(exact <= 2 ? exact : null, DamerauLevenshtein.DistanceWithin(a, b, 2));
        }
    }

    [Fact]
    public void NGrams_SimilarityAndIndex()
    {
        Assert.True(NGramSimilarity.NGrams("Night").SetEquals(["#n", "ni", "ig", "gh", "ht", "t#"]));
        Assert.Equal(1, NGramSimilarity.Jaccard("same", "same"));
        Assert.True(NGramSimilarity.Dice("night", "nacht") > NGramSimilarity.Jaccard("night", "nacht"));

        var index = new NGramSimilarity.Index(["postgres", "postgresql", "mysql", "sqlite", "mongodb"]);
        Assert.Equal(["postgres", "postgresql"], index.Search("postgress").Select(r => r.Word));
    }

    [Fact]
    public void BkTree_FindsNearbyWordsWithoutCheckingAll()
    {
        var words = Enumerable.Range(0, 2000).Select(i => $"w{i * 7919 % 10_000:0000}").ToList();
        var tree = new BkTree();
        foreach (var w in words) tree.Add(w);
        tree.Add(words[0]); // duplicate ignored

        var results = tree.Search("w1234", 1);

        Assert.Equal(words.Count, tree.Count);
        Assert.Equal(words.Where(w => DamerauLevenshtein.Distance(w, "w1234") <= 1).Order(), results.Select(r => r.Word).Order());
        Assert.True(tree.LastComparisons < words.Count / 2);
    }

    [Fact]
    public void SpellChecker_PrefersFrequentWords()
    {
        var checker = new SpellChecker("the cat sat on the mat. the hat is red. that cat is fat. spelling spelling");

        Assert.Equal("the", checker.Correct("teh"));
        Assert.Equal("cat", checker.Correct("cta"));
        Assert.Equal("the", checker.Correct("thw")); // "the", "that", "hat"… the most frequent wins
        Assert.Equal("spelling", checker.Correct("speling"));
        Assert.Equal("spelling", checker.Correct("spellnig"));
        Assert.Equal("xyzzy", checker.Correct("xyzzy")); // nothing close: left alone
        Assert.Contains("helo", SpellChecker.Edits1("hello"));  // delete
        Assert.Contains("hlelo", SpellChecker.Edits1("hello")); // swap
    }

    [Fact]
    public void MinHash_EstimatesJaccard()
    {
        var a = MinHash.Shingles("the quick brown fox jumps over the lazy dog near the river bank today", 2);
        var b = MinHash.Shingles("the quick brown fox jumps over the sleepy dog near the river bank today", 2);
        var c = MinHash.Shingles("completely different words make up this other sentence entirely here", 2);
        var minHash = new MinHash(256);

        var exact = MinHash.ExactJaccard(a, b);
        var estimate = MinHash.EstimateSimilarity(minHash.Signature(a), minHash.Signature(b));

        Assert.InRange(estimate, exact - 0.15, exact + 0.15);
        Assert.True(MinHash.EstimateSimilarity(minHash.Signature(a), minHash.Signature(c)) < 0.05);
        Assert.Equal(1, MinHash.EstimateSimilarity(minHash.Signature(a), minHash.Signature(a)));
    }
}
