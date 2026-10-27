using System.Globalization;

namespace CSharpExercises.Parsing;

// The syntax tree. Records give value equality, so trees built by the parser and the simplifier can be compared.
public abstract record Expr;
public sealed record Number(double Value) : Expr;
public sealed record Variable(string Name) : Expr;
public sealed record Negate(Expr Operand) : Expr;
public sealed record Binary(char Operator, Expr Left, Expr Right) : Expr;
public sealed record Call(string Function, IReadOnlyList<Expr> Arguments) : Expr
{
    // A record compares a list property by reference; compare the arguments themselves instead.
    public bool Equals(Call? other) =>
        other is not null && Function == other.Function && Arguments.SequenceEqual(other.Arguments);

    public override int GetHashCode() => HashCode.Combine(Function, Arguments.Count);
}

/// <summary>
/// A recursive-descent parser: one method per grammar rule, and each rule calls the rules that bind tighter.
/// Operator precedence and associativity come from the shape of the grammar, not from a table.
/// </summary>
/// <remarks>
/// <code>
/// expression := term (('+' | '-') term)*
/// term       := unary (('*' | '/' | '%') unary)*
/// unary      := '-' unary | power
/// power      := primary ('^' unary)?           right-associative: 2^3^2 = 2^(3^2)
/// primary    := number | identifier | identifier '(' arguments ')' | '(' expression ')'
/// </code>
/// Because <c>unary</c> sits above <c>power</c>, <c>-2^2</c> is <c>-(2^2)</c> = -4, as in mathematics.
/// </remarks>
public sealed class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    private Parser(List<Token> tokens) => _tokens = tokens;

    public static Expr Parse(string text)
    {
        var parser = new Parser(Lexer.Tokenize(text));
        var expression = parser.ParseExpression();
        parser.Expect(TokenKind.End, "an operator or the end of the input");
        return expression;
    }

    private Token Current => _tokens[_position];

    private Expr ParseExpression()
    {
        var left = ParseTerm();
        while (Current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var op = Advance().Text[0];
            left = new Binary(op, left, ParseTerm());
        }
        return left;
    }

    private Expr ParseTerm()
    {
        var left = ParseUnary();
        while (Current.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.Percent)
        {
            var op = Advance().Text[0];
            left = new Binary(op, left, ParseUnary());
        }
        return left;
    }

    private Expr ParseUnary()
    {
        if (Current.Kind != TokenKind.Minus) return ParsePower();

        Advance();
        return new Negate(ParseUnary());
    }

    private Expr ParsePower()
    {
        var @base = ParsePrimary();
        if (Current.Kind != TokenKind.Caret) return @base;

        Advance();
        // Recursing into unary (not a loop) is what makes ^ right-associative, and allows 2^-1.
        return new Binary('^', @base, ParseUnary());
    }

    private Expr ParsePrimary()
    {
        var token = Advance();
        switch (token.Kind)
        {
            case TokenKind.Number:
                return double.TryParse(token.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
                    ? new Number(value)
                    : throw new ParseException($"'{token.Text}' is not a valid number", token.Position);

            case TokenKind.Identifier when Current.Kind == TokenKind.LeftParen:
                Advance();
                return new Call(token.Text, ParseArguments());

            case TokenKind.Identifier:
                return new Variable(token.Text);

            case TokenKind.LeftParen:
                var inner = ParseExpression();
                Expect(TokenKind.RightParen, "')'");
                return inner;

            default:
                throw Unexpected(token, "a number, a name or '('");
        }
    }

    private List<Expr> ParseArguments()
    {
        List<Expr> arguments = [];
        if (Current.Kind == TokenKind.RightParen)
        {
            Advance();
            return arguments;
        }

        while (true)
        {
            arguments.Add(ParseExpression());
            if (Current.Kind != TokenKind.Comma) break;
            Advance();
        }

        Expect(TokenKind.RightParen, "',' or ')'");
        return arguments;
    }

    private Token Advance()
    {
        var token = Current;
        if (token.Kind != TokenKind.End) _position++;
        return token;
    }

    private void Expect(TokenKind kind, string description)
    {
        if (Current.Kind != kind) throw Unexpected(Current, description);
        Advance();
    }

    private static ParseException Unexpected(Token token, string expected) =>
        new($"Expected {expected} but found {(token.Kind == TokenKind.End ? "the end of the input" : $"'{token.Text}'")}", token.Position);
}
