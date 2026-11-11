namespace CSharpExercises.Cards;

/// <summary>A playing card: rank 2–14 (11 jack, 12 queen, 13 king, 14 ace) and suit c, d, h or s. Parses "As", "Td", "10h".</summary>
public readonly record struct Card(int Rank, char Suit)
{
    private const string RankChars = "23456789TJQKA";

    public static Card Parse(string text)
    {
        if (text.Length is < 2 or > 3) throw new FormatException($"'{text}' is not a card.");
        var rankText = text[..^1].ToUpperInvariant();
        var suit = char.ToLowerInvariant(text[^1]);
        var rank = rankText == "10" ? 10 : rankText.Length == 1 ? RankChars.IndexOf(rankText[0]) + 2 : 1;
        if (rank < 2 || "cdhs".IndexOf(suit) < 0) throw new FormatException($"'{text}' is not a card.");
        return new Card(rank, suit);
    }

    /// <summary>Parses a space-separated list such as "As Kd 7h".</summary>
    public static Card[] ParseMany(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray();

    public override string ToString() => $"{RankChars[Rank - 2]}{Suit}";
}
