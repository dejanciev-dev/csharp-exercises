using CSharpExercises.Parsing;

namespace CSharpExercises.Tests;

public class ParsingTests
{
    private static string Roundtrip(string text) => ExpressionTools.Format(Parser.Parse(text));

    [Fact]
    public void Lexer_SplitsInputIntoTokens()
    {
        var kinds = Lexer.Tokenize("max(x1, 2.5) ^ -3").Select(t => t.Kind);

        Assert.Equal(
            [
                TokenKind.Identifier, TokenKind.LeftParen, TokenKind.Identifier, TokenKind.Comma, TokenKind.Number,
                TokenKind.RightParen, TokenKind.Caret, TokenKind.Minus, TokenKind.Number, TokenKind.End,
            ],
            kinds);
    }

    [Fact]
    public void Lexer_RejectsUnknownCharacters()
    {
        var ex = Assert.Throws<ParseException>(() => Lexer.Tokenize("2 + $"));
        Assert.Equal(4, ex.Position);
    }

    [Fact]
    public void Parser_BuildsATreeThatRespectsPrecedence()
    {
        Assert.Equal(
            new Binary('+', new Number(1), new Binary('*', new Number(2), new Variable("x"))),
            Parser.Parse("1 + 2 * x"));
    }

    [Fact]
    public void Parser_CallsCompareByTheirArguments()
    {
        Assert.Equal(Parser.Parse("max(1, x)"), Parser.Parse("max( 1 , x )"));
        Assert.NotEqual(Parser.Parse("max(1, x)"), Parser.Parse("max(x, 1)"));
    }

    [Theory]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("(1 + 2) * 3", 9)]
    [InlineData("10 - 4 - 3", 3)]          // left-associative
    [InlineData("2 ^ 3 ^ 2", 512)]         // right-associative: 2^(3^2)
    [InlineData("-2 ^ 2", -4)]             // power binds tighter than unary minus
    [InlineData("(-2) ^ 2", 4)]
    [InlineData("2 ^ -1", 0.5)]
    [InlineData("--3", 3)]
    [InlineData("17 % 5", 2)]
    [InlineData("sqrt(16) + abs(-2)", 6)]
    [InlineData("max(1, 7, 3) - min(4, 2)", 5)]
    [InlineData(".5 * 4", 2)]
    public void Evaluate_Arithmetic(string text, double expected)
    {
        Assert.Equal(expected, ExpressionTools.Evaluate(text), precision: 10);
    }

    [Fact]
    public void Evaluate_UsesVariables()
    {
        var area = Parser.Parse("pi * r ^ 2");

        Assert.Equal(Math.PI * 9, ExpressionTools.Evaluate(area, new Dictionary<string, double> { ["pi"] = Math.PI, ["r"] = 3 }));
    }

    [Theory]
    [InlineData("1 +", "Expected a number, a name or '(' but found the end of the input")]
    [InlineData("(1 + 2", "Expected ')' but found the end of the input")]
    [InlineData("1 2", "Expected an operator or the end of the input but found '2'")]
    [InlineData("max(1 2)", "Expected ',' or ')' but found '2'")]
    [InlineData("1.2.3 + 1", "'1.2.3' is not a valid number")]
    [InlineData("* 3", "Expected a number, a name or '(' but found '*'")]
    public void Parse_ErrorsSayWhatWasExpected(string text, string message)
    {
        var ex = Assert.Throws<ParseException>(() => Parser.Parse(text));
        Assert.StartsWith(message, ex.Message);
    }

    [Fact]
    public void Evaluate_ReportsRuntimeErrors()
    {
        Assert.Throws<DivideByZeroException>(() => ExpressionTools.Evaluate("1 / (2 - 2)"));
        Assert.Throws<KeyNotFoundException>(() => ExpressionTools.Evaluate("x + 1"));
        Assert.Throws<KeyNotFoundException>(() => ExpressionTools.Evaluate("cube(2)"));
        Assert.Throws<ArgumentException>(() => ExpressionTools.Evaluate("sqrt(1, 2)"));
    }

    [Theory]
    [InlineData("x * (3 - 2) + 0", "x")]
    [InlineData("2 * 3 + x", "6 + x")]
    [InlineData("(x + 1) - (x + 1)", "0")]
    [InlineData("y * 0 + z ^ 1", "z")]
    [InlineData("x ^ 0", "1")]
    [InlineData("--x", "x")]
    [InlineData("-(2 * 3)", "-6")]
    [InlineData("max(1, 2) * sqrt(x)", "2 * sqrt(x)")]
    [InlineData("1 / 0 + 1", "1 / 0 + 1")]   // left for Evaluate to report
    [InlineData("unknown(1)", "unknown(1)")]
    public void Simplify_FoldsConstantsAndRemovesIdentities(string text, string expected)
    {
        Assert.Equal(expected, ExpressionTools.Format(ExpressionTools.Simplify(Parser.Parse(text))));
    }

    [Theory]
    [InlineData("((1 + 2)) * 3", "(1 + 2) * 3")]
    [InlineData("1 + (2 * 3)", "1 + 2 * 3")]
    [InlineData("1 - (2 - 3)", "1 - (2 - 3)")]
    [InlineData("(1 - 2) - 3", "1 - 2 - 3")]
    [InlineData("(2 ^ 3) ^ 2", "(2 ^ 3) ^ 2")]
    [InlineData("2 ^ (3 ^ 2)", "2 ^ 3 ^ 2")]
    [InlineData("(-2) ^ 2", "(-2) ^ 2")]
    [InlineData("-(x + 1)", "-(x + 1)")]
    [InlineData("max(a,b,c)", "max(a, b, c)")]
    public void Format_UsesOnlyTheParenthesesItNeeds(string text, string expected)
    {
        Assert.Equal(expected, Roundtrip(text));
    }

    [Fact]
    public void Format_NegativeConstantsKeepTheirMeaning()
    {
        // Simplifying -(3) gives Number(-3); as the base of a power it must print as (-3), not -3, which means -(3^2).
        var simplified = ExpressionTools.Simplify(Parser.Parse("(-3) ^ x"));
        var text = ExpressionTools.Format(simplified);

        Assert.Equal("(-3) ^ x", text);
        Assert.Equal(9, ExpressionTools.Evaluate(text, new Dictionary<string, double> { ["x"] = 2 }));
    }

    [Theory]
    [InlineData("1 + 2 * 3 - 4 / 2")]
    [InlineData("2 ^ 3 ^ 2 - (1 - 5)")]
    [InlineData("-(-2) ^ 2 + max(1, -3) % 2")]
    [InlineData("((7 - 2) - (1 - 3)) * 2 ^ -2")]
    public void Format_ThenParse_GivesTheSameTree(string text)
    {
        var tree = Parser.Parse(text);

        Assert.Equal(tree, Parser.Parse(ExpressionTools.Format(tree)));
    }
}
