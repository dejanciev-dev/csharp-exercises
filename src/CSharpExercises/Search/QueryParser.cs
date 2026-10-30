namespace CSharpExercises.Search;

/// <summary>A boolean search query as a tree.</summary>
public abstract record SearchQuery
{
    public sealed record Term(string Text) : SearchQuery;
    public sealed record Phrase(string Text) : SearchQuery;
    public sealed record And(SearchQuery Left, SearchQuery Right) : SearchQuery;
    public sealed record Or(SearchQuery Left, SearchQuery Right) : SearchQuery;
    public sealed record Not(SearchQuery Inner) : SearchQuery;
}

/// <summary>
/// Parses the query syntax users expect from a search box: <c>cats AND (dogs OR "guinea pigs") -fish</c>, where
/// juxtaposed words mean AND, OR binds looser than AND, and a leading minus means NOT. Then evaluates the tree
/// against an <see cref="InvertedIndex"/> with set operations.
/// </summary>
public sealed class QueryParser
{
    private readonly List<string> _tokens;
    private int _pos;

    private QueryParser(string query) => _tokens = Lex(query);

    public static SearchQuery Parse(string query)
    {
        var parser = new QueryParser(query);
        if (parser._tokens.Count == 0) throw new FormatException("Empty query.");
        var result = parser.ParseOr();
        if (parser._pos < parser._tokens.Count) throw new FormatException($"Unexpected '{parser._tokens[parser._pos]}'.");
        return result;
    }

    public static IReadOnlySet<int> Evaluate(SearchQuery query, InvertedIndex index, IReadOnlySet<int> allIds) => query switch
    {
        SearchQuery.Term t => index.All(t.Text).ToHashSet(),
        SearchQuery.Phrase p => index.Phrase(p.Text).ToHashSet(),
        SearchQuery.And(var l, SearchQuery.Not(var n)) => Evaluate(l, index, allIds).Except(Evaluate(n, index, allIds)).ToHashSet(),
        SearchQuery.And a => Evaluate(a.Left, index, allIds).Intersect(Evaluate(a.Right, index, allIds)).ToHashSet(),
        SearchQuery.Or o => Evaluate(o.Left, index, allIds).Union(Evaluate(o.Right, index, allIds)).ToHashSet(),
        SearchQuery.Not n => allIds.Except(Evaluate(n.Inner, index, allIds)).ToHashSet(),
        _ => throw new ArgumentOutOfRangeException(nameof(query)),
    };

    // or  = and ("OR" and)*
    private SearchQuery ParseOr()
    {
        var left = ParseAnd();
        while (Accept("OR")) left = new SearchQuery.Or(left, ParseAnd());
        return left;
    }

    // and = unary (["AND"] unary)*   — a missing operator between two terms means AND
    private SearchQuery ParseAnd()
    {
        var left = ParseUnary();
        while (_pos < _tokens.Count && _tokens[_pos] is not ("OR" or ")"))
        {
            Accept("AND");
            left = new SearchQuery.And(left, ParseUnary());
        }
        return left;
    }

    private SearchQuery ParseUnary()
    {
        if (Accept("-") || Accept("NOT")) return new SearchQuery.Not(ParseUnary());
        if (Accept("("))
        {
            var inner = ParseOr();
            if (!Accept(")")) throw new FormatException("Missing ')'.");
            return inner;
        }
        if (_pos >= _tokens.Count) throw new FormatException("Query ends unexpectedly.");

        var token = _tokens[_pos++];
        if (token is "AND" or "OR" or ")") throw new FormatException($"Unexpected '{token}'.");
        return token.StartsWith('"') ? new SearchQuery.Phrase(token.Trim('"')) : new SearchQuery.Term(token);
    }

    private bool Accept(string token)
    {
        if (_pos >= _tokens.Count || _tokens[_pos] != token) return false;
        _pos++;
        return true;
    }

    private static List<string> Lex(string query)
    {
        var tokens = new List<string>();
        for (var i = 0; i < query.Length;)
        {
            var c = query[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c is '(' or ')' or '-') { tokens.Add(c.ToString()); i++; continue; }
            if (c == '"')
            {
                var end = query.IndexOf('"', i + 1);
                if (end < 0) throw new FormatException("Unterminated quote.");
                tokens.Add(query[i..(end + 1)]);
                i = end + 1;
                continue;
            }
            var start = i;
            while (i < query.Length && !char.IsWhiteSpace(query[i]) && query[i] is not ('(' or ')' or '"')) i++;
            tokens.Add(query[start..i]);
        }
        return tokens;
    }
}
