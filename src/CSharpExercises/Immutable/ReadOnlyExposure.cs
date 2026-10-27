using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace CSharpExercises.Immutable;

/// <summary>
/// Three ways to expose a private list, and what each actually protects. "Read-only" in .NET usually means
/// "you cannot change it through this reference", not "it will not change".
/// </summary>
public sealed class Playlist
{
    private readonly List<string> _songs = [];

    public void Add(string song) => _songs.Add(song);

    /// <summary>Weakest: it is the list itself, so a caller can cast it back to <c>List&lt;string&gt;</c> and modify it.</summary>
    public IReadOnlyList<string> SongsAsInterface => _songs;

    /// <summary>
    /// A wrapper that refuses changes and cannot be cast back. But it is a live <em>view</em>: when the playlist changes,
    /// what the caller holds changes too, and a caller looping over it while songs are added gets an exception.
    /// </summary>
    public ReadOnlyCollection<string> SongsAsView => _songs.AsReadOnly();

    /// <summary>
    /// A copy that can never change, by anyone. Costs a copy on every call, so callers that need a stable snapshot
    /// (to hand to another thread, store, or compare later) are the ones that should get this.
    /// </summary>
    public ImmutableArray<string> SongsAsSnapshot => [.. _songs];
}

/// <summary>
/// <see cref="FrozenDictionary{TKey, TValue}"/>: built once, read many times. Creating it is slower than a
/// <c>Dictionary</c> because it analyses the keys to pick the fastest lookup strategy, which pays off for
/// data that never changes after startup.
/// </summary>
public static class CountryCodes
{
    private static readonly FrozenDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["MK"] = "North Macedonia",
        ["DE"] = "Germany",
        ["US"] = "United States",
        ["JP"] = "Japan",
        ["BR"] = "Brazil",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> Codes => Names.Keys;

    public static string? NameOf(string code) => Names.GetValueOrDefault(code.Trim());
}
