namespace CSharpExercises.Search;

/// <summary>
/// The core of every search engine: for each term, the sorted list of documents containing it (a posting list), with
/// the positions where it occurs. AND is an intersection of sorted lists (walk both in step), OR a union, NOT a
/// difference, so queries never scan documents. Positions make phrase queries possible.
/// </summary>
public sealed class InvertedIndex
{
    private readonly SortedDictionary<string, SortedDictionary<int, List<int>>> _postings = new(StringComparer.Ordinal);
    private readonly Dictionary<int, string> _documents = new();
    private readonly Dictionary<int, int> _lengths = new();

    public int DocumentCount => _documents.Count;
    public double AverageLength => _lengths.Count == 0 ? 0 : _lengths.Values.Average();

    public void Add(int id, string text)
    {
        if (!_documents.TryAdd(id, text)) throw new ArgumentException($"Document {id} is already indexed.", nameof(id));
        var length = 0;
        foreach (var (term, position) in Tokenizer.Tokenize(text))
        {
            if (!_postings.TryGetValue(term, out var docs)) _postings[term] = docs = new SortedDictionary<int, List<int>>();
            if (!docs.TryGetValue(id, out var positions)) docs[id] = positions = [];
            positions.Add(position);
            length++;
        }
        _lengths[id] = length;
    }

    public string Document(int id) => _documents[id];
    public int Length(int id) => _lengths[id];

    public IReadOnlyList<int> Postings(string term) =>
        _postings.TryGetValue(term, out var docs) ? docs.Keys.ToList() : [];

    public int TermFrequency(string term, int id) =>
        _postings.TryGetValue(term, out var docs) && docs.TryGetValue(id, out var positions) ? positions.Count : 0;

    public int DocumentFrequency(string term) => _postings.TryGetValue(term, out var docs) ? docs.Count : 0;

    /// <summary>Documents containing every term: merge-intersect the sorted posting lists, rarest first.</summary>
    public IReadOnlyList<int> All(string query)
    {
        var lists = Tokenizer.Terms(query).Distinct().Select(Postings).OrderBy(l => l.Count).ToList();
        if (lists.Count == 0) return [];
        return lists.Skip(1).Aggregate(lists[0], Intersect);
    }

    public IReadOnlyList<int> Any(string query) =>
        Tokenizer.Terms(query).SelectMany(Postings).Distinct().Order().ToList();

    /// <summary>Documents matching all of <paramref name="include"/> and none of <paramref name="exclude"/>.</summary>
    public IReadOnlyList<int> AllExcept(string include, string exclude)
    {
        var excluded = Any(exclude).ToHashSet();
        return All(include).Where(id => !excluded.Contains(id)).ToList();
    }

    /// <summary>Exact phrase: the terms appear at consecutive positions in the same document.</summary>
    public IReadOnlyList<int> Phrase(string phrase)
    {
        var tokens = Tokenizer.Tokenize(phrase).ToList();
        if (tokens.Count == 0) return [];
        var offset = tokens[0].Position;

        return All(phrase).Where(id =>
            _postings[tokens[0].Term][id].Any(start =>
                tokens.All(t => _postings[t.Term][id].BinarySearch(start + t.Position - offset) >= 0))).ToList();
    }

    public IEnumerable<string> Terms => _postings.Keys;

    /// <summary>Linear-time intersection of two sorted lists: advance whichever side is behind.</summary>
    private static IReadOnlyList<int> Intersect(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        var result = new List<int>();
        for (int i = 0, j = 0; i < a.Count && j < b.Count;)
        {
            if (a[i] == b[j]) { result.Add(a[i]); i++; j++; }
            else if (a[i] < b[j]) i++;
            else j++;
        }
        return result;
    }
}
