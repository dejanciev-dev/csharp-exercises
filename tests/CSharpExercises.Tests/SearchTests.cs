using CSharpExercises.Search;

namespace CSharpExercises.Tests;

public class SearchTests
{
    private static InvertedIndex BuildIndex()
    {
        var index = new InvertedIndex();
        index.Add(1, "The quick brown fox jumps over the lazy dog");
        index.Add(2, "A lazy cat sleeps all day, dreaming of running");
        index.Add(3, "Quick thinking: the fox ran from the dogs and cats");
        index.Add(4, "Dogs and cats living together. Dogs, dogs, dogs everywhere!");
        index.Add(5, "Café owners serve coffee to tired runners");
        return index;
    }

    [Theory]
    [InlineData("running", "run")]
    [InlineData("runs", "run")]
    [InlineData("jumped", "jump")]
    [InlineData("ponies", "pony")]
    [InlineData("classes", "class")]
    [InlineData("glass", "glass")]
    [InlineData("quickly", "quick")]
    public void Tokenizer_Stems(string word, string expected) => Assert.Equal(expected, Tokenizer.Stem(word));

    [Fact]
    public void Tokenizer_NormalisesAndDropsStopWords()
    {
        Assert.Equal([("cafe", 0), ("own", 1)], Tokenizer.Tokenize("Café owned").ToArray()[..2].Select(t => (t.Term, t.Position)));
        Assert.Equal([("fox", 1), ("dog", 3)], Tokenizer.Tokenize("the fox and dogs"));
    }

    [Fact]
    public void Index_AnswersBooleanQueries()
    {
        var index = BuildIndex();

        Assert.Equal([1, 3, 4], index.Postings("dog"));
        Assert.Equal([3, 4], index.All("dogs cats"));
        Assert.Equal([1, 2, 3, 4], index.Any("dog lazy"));
        Assert.Equal([3], index.AllExcept("dogs cats", "living"));
        Assert.Equal([5], index.All("cafe")); // accents removed on both sides
        Assert.Equal(4, index.TermFrequency("dog", 4));
    }

    [Fact]
    public void Index_MatchesPhrases()
    {
        var index = BuildIndex();

        Assert.Equal([1], index.Phrase("lazy dog"));
        Assert.Equal([1], index.Phrase("quick brown fox"));
        Assert.Empty(index.Phrase("dog lazy"));
        Assert.Equal([3, 4], index.Phrase("dogs and cats")); // "and" is a stop word but keeps its position
    }

    [Fact]
    public void Ranking_PrefersRareAndRepeatedTerms()
    {
        var index = BuildIndex();

        Assert.Equal(4, Ranking.TfIdf(index, "dogs")[0].Id);
        Assert.Equal(4, Ranking.Bm25(index, "dogs")[0].Id);
        Assert.Equal(1, Ranking.Bm25(index, "lazy fox jumps")[0].Id);
        Assert.Empty(Ranking.Bm25(index, "unicorn"));
    }

    [Fact]
    public void Ranking_Bm25SaturatesRepeatedTerms()
    {
        var index = new InvertedIndex();
        index.Add(1, "apple");
        index.Add(2, "apple apple apple apple apple apple apple apple");
        index.Add(3, "banana");

        var scores = Ranking.Bm25(index, "apple").ToDictionary(r => r.Id, r => r.Score);
        var tfidf = Ranking.TfIdf(index, "apple").ToDictionary(r => r.Id, r => r.Score);

        Assert.True(scores[2] < 2 * scores[1]); // eight times the term is nowhere near eight times the score
        Assert.True(tfidf[2] > 2 * tfidf[1]);
    }

    [Theory]
    [InlineData("dogs cats", "And(Term(dogs), Term(cats))")]
    [InlineData("dogs OR cats fox", "Or(Term(dogs), And(Term(cats), Term(fox)))")]
    [InlineData("(dogs OR cats) -lazy", "And(Or(Term(dogs), Term(cats)), Not(Term(lazy)))")]
    [InlineData("\"lazy dog\" AND fox", "And(Phrase(lazy dog), Term(fox))")]
    public void QueryParser_BuildsTheTree(string query, string expected) =>
        Assert.Equal(expected, Describe(QueryParser.Parse(query)));

    [Fact]
    public void QueryParser_EvaluatesAgainstTheIndex()
    {
        var index = BuildIndex();
        var all = new HashSet<int> { 1, 2, 3, 4, 5 };

        Assert.Equal([2, 3, 4], QueryParser.Evaluate(QueryParser.Parse("cats OR fox -jumps"), index, all).Order());
        Assert.Equal([2, 5], QueryParser.Evaluate(QueryParser.Parse("-dogs -fox"), index, all).Order());
        Assert.Throws<FormatException>(() => QueryParser.Parse("(dogs"));
        Assert.Throws<FormatException>(() => QueryParser.Parse("dogs OR"));
    }

    [Fact]
    public void Snippets_HighlightTheBestWindow()
    {
        const string text = "Our shop opened in 1990. We sell many things. Today the running shoes for runners are on sale, and running socks too.";

        var snippet = Snippets.Highlight(text, "run sale", windowWords: 8);

        Assert.Equal("… the [running] shoes for runners are on [sale], …", snippet); // "runners" stems to "runner", not "run"
        Assert.Equal("[Dogs], cats", Snippets.Highlight("Dogs, cats", "dog"));
    }

    [Fact]
    public void Autocomplete_SuggestsPopularCompletions()
    {
        var complete = new Autocomplete([("new york", 900), ("new york times", 500), ("newcastle", 300), ("new zealand", 700), ("nevada", 1000), ("New York", 50)]);

        Assert.Equal(["new york", "new zealand", "new york times"], complete.Suggest("new ", 3));
        Assert.Equal(["nevada", "new york", "new zealand", "new york times", "newcastle"], complete.Suggest("ne", 10));
        Assert.Empty(complete.Suggest("x"));
    }

    private static string Describe(SearchQuery q) => q switch
    {
        SearchQuery.Term t => $"Term({t.Text})",
        SearchQuery.Phrase p => $"Phrase({p.Text})",
        SearchQuery.And a => $"And({Describe(a.Left)}, {Describe(a.Right)})",
        SearchQuery.Or o => $"Or({Describe(o.Left)}, {Describe(o.Right)})",
        SearchQuery.Not n => $"Not({Describe(n.Inner)})",
        _ => "?",
    };
}
