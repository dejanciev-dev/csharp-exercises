namespace CSharpExercises.Parsing;

public enum TokenKind { Number, Identifier, Plus, Minus, Star, Slash, Percent, Caret, LeftParen, RightParen, Comma, End }

public readonly record struct Token(TokenKind Kind, string Text, int Position);

/// <summary>A syntax error, with the position in the input where it was found.</summary>
public sealed class ParseException(string message, int position) : Exception($"{message} (at position {position})")
{
    public int Position => position;
}

/// <summary>
/// The first stage of parsing: turns <c>"2 * (x + 1)"</c> into tokens, so the parser can work with
/// <c>Number Star LeftParen Identifier Plus Number RightParen</c> instead of individual characters.
/// </summary>
public static class Lexer
{
    public static List<Token> Tokenize(string text)
    {
        List<Token> tokens = [];
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];
            var start = i;

            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (char.IsAsciiDigit(c) || c == '.')
            {
                // Take every digit and dot; the parser rejects malformed numbers such as "1.2.3" with a clear message.
                while (i < text.Length && (char.IsAsciiDigit(text[i]) || text[i] == '.')) i++;
                tokens.Add(new Token(TokenKind.Number, text[start..i], start));
            }
            else if (char.IsAsciiLetter(c) || c == '_')
            {
                while (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] == '_')) i++;
                tokens.Add(new Token(TokenKind.Identifier, text[start..i], start));
            }
            else
            {
                var kind = c switch
                {
                    '+' => TokenKind.Plus,
                    '-' => TokenKind.Minus,
                    '*' => TokenKind.Star,
                    '/' => TokenKind.Slash,
                    '%' => TokenKind.Percent,
                    '^' => TokenKind.Caret,
                    '(' => TokenKind.LeftParen,
                    ')' => TokenKind.RightParen,
                    ',' => TokenKind.Comma,
                    _ => throw new ParseException($"Unexpected character '{c}'", i),
                };
                tokens.Add(new Token(kind, c.ToString(), i));
                i++;
            }
        }

        tokens.Add(new Token(TokenKind.End, "", text.Length));
        return tokens;
    }
}
