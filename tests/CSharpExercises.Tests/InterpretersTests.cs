using CSharpExercises.Interpreters;

namespace CSharpExercises.Tests;

public class InterpretersTests
{
    [Theory]
    [InlineData("3 + 4 * 2", "3 4 2 * +")]
    [InlineData("(3 + 4) * 2", "3 4 + 2 *")]
    [InlineData("2 ^ 3 ^ 2", "2 3 2 ^ ^")]          // right-associative
    [InlineData("10 - 4 - 3", "10 4 - 3 -")]        // left-associative
    [InlineData("-2 ^ 2", "2 2 ^ neg")]
    [InlineData("max(1, 2 + 3) * 2", "1 2 3 + max 2 *")]
    public void ShuntingYard_ConvertsToRpn(string infix, string rpn) =>
        Assert.Equal(rpn, string.Join(' ', ShuntingYard.ToRpn(infix)));

    [Theory]
    [InlineData("3 + 4 * 2 / (1 - 5) ^ 2 ^ 3", 3.0001220703125)]
    [InlineData("2 ^ 3 ^ 2", 512)]
    [InlineData("-2 ^ 2", -4)]
    [InlineData("3 - -2", 5)]
    [InlineData("sqrt(16) + abs(-3) * min(2, 7)", 10)]
    public void ShuntingYard_Evaluates(string infix, double expected) => Assert.Equal(expected, ShuntingYard.Evaluate(infix), 12);

    [Fact]
    public void ShuntingYard_ReportsErrors()
    {
        Assert.Throws<FormatException>(() => ShuntingYard.Evaluate("(1 + 2"));
        Assert.Throws<FormatException>(() => ShuntingYard.Evaluate("1 + 2)"));
        Assert.Throws<FormatException>(() => ShuntingYard.Evaluate("1 +"));
        Assert.Throws<FormatException>(() => ShuntingYard.Evaluate("1 2"));
    }

    private const string Factorials = """
        ; print n! for n = 1..10
            push 1
            store 0        ; n
            push 1
            store 1        ; acc
        loop:
            load 1
            load 0
            mul
            dup
            store 1
            print
            load 0
            push 1
            add
            dup
            store 0
            push 11
            lt
            jumpifzero end
            jump loop
        end:
            halt
        """;

    [Fact]
    public void StackMachine_RunsAnAssembledLoop()
    {
        var vm = new StackMachine();
        vm.Run(StackMachine.Assemble(Factorials));

        Assert.Equal([1L, 2, 6, 24, 120, 720, 5040, 40320, 362880, 3628800], vm.Output);
    }

    [Fact]
    public void StackMachine_CallsSubroutinesAndStopsRunaways()
    {
        var vm = new StackMachine();
        vm.Run(StackMachine.Assemble("""
                push 7
                call square
                print
                halt
            square:
                dup
                mul
                return
            """));
        Assert.Equal([49L], vm.Output);

        Assert.Throws<TimeoutException>(() => new StackMachine(maxSteps: 1000).Run(StackMachine.Assemble("top:\njump top")));
        Assert.Throws<InvalidOperationException>(() => new StackMachine().Run(StackMachine.Assemble("add")));
        Assert.Throws<FormatException>(() => StackMachine.Assemble("jump nowhere"));
    }

    [Theory]
    [InlineData("abc", "abc", true)]
    [InlineData("a.c", "axc", true)]
    [InlineData("ab*c", "ac", true)]
    [InlineData("ab*c", "abbbc", true)]
    [InlineData("ab+c", "ac", false)]
    [InlineData("colou?r", "color", true)]
    [InlineData("colou?r", "colour", true)]
    [InlineData("(cat|dog)s?", "dogs", true)]
    [InlineData("(cat|dog)s?", "cow", false)]
    [InlineData("(a|b)*abb", "babaabb", true)]
    [InlineData("(a|b)*abb", "babaab", false)]
    [InlineData("abc", "abcd", false)] // whole-input match
    public void TinyRegex_Matches(string pattern, string input, bool expected) =>
        Assert.Equal(expected, new TinyRegex(pattern).IsMatch(input));

    [Fact]
    public void TinyRegex_HasNoCatastrophicBacktracking()
    {
        // (a?){n}a{n} against a^n is the classic exponential case for backtracking engines.
        const int N = 30;
        var pattern = string.Concat(Enumerable.Repeat("a?", N)) + new string('a', N);
        var watch = System.Diagnostics.Stopwatch.StartNew();

        Assert.True(new TinyRegex(pattern).IsMatch(new string('a', N)));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
        Assert.Throws<FormatException>(() => new TinyRegex("(ab"));
    }

    [Fact]
    public void TinyLisp_EvaluatesExpressionsAndDefinitions()
    {
        var lisp = new TinyLisp();

        Assert.Equal(7.0, lisp.Run("(+ 1 (* 2 3))"));
        Assert.Equal("(1 2 3)", TinyLisp.Print(lisp.Run("(quote (1 2 3))")));
        Assert.Equal(true, lisp.Run("(< 1 2)"));
        Assert.Equal(120.0, lisp.Run("""
            (define fact (lambda (n) (if (< n 2) 1 (* n (fact (- n 1))))))
            (fact 5)
            """));
        Assert.Equal(15.0, lisp.Run("(let ((x 5) (y 10)) (+ x y))"));
    }

    [Fact]
    public void TinyLisp_SupportsClosuresListsAndDeepTailCalls()
    {
        var lisp = new TinyLisp();
        lisp.Run("(define make-adder (lambda (n) (lambda (x) (+ x n))))");
        lisp.Run("(define add5 (make-adder 5))");
        Assert.Equal(15.0, lisp.Run("(add5 10)"));

        lisp.Run("(define len (lambda (xs) (if (null? xs) 0 (+ 1 (len (cdr xs))))))");
        Assert.Equal(3.0, lisp.Run("(len (list 1 2 3))"));
        Assert.Equal("(0 1 2)", TinyLisp.Print(lisp.Run("(cons 0 (list 1 2))")));

        lisp.Run("(define count (lambda (n acc) (if (= n 0) acc (count (- n 1) (+ acc 1)))))");
        Assert.Equal(100_000.0, lisp.Run("(count 100000 0)")); // tail calls run in a loop

        Assert.Throws<KeyNotFoundException>(() => lisp.Run("(undefined 1)"));
        Assert.Throws<FormatException>(() => lisp.Run("(+ 1 2"));
    }

    [Fact]
    public void Brainfuck_HelloWorld()
    {
        const string hello = "++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++.";
        Assert.Equal("Hello World!\n", Brainfuck.Run(hello));
    }

    [Fact]
    public void Brainfuck_EchoesInputAndValidates()
    {
        Assert.Equal("cba", Brainfuck.Run(">,[>,]<[.<]", "abc")); // reverse the input
        Assert.Equal("\0", Brainfuck.Run("+++[-]."));
        Assert.Throws<FormatException>(() => Brainfuck.Run("[[]"));
        Assert.Throws<TimeoutException>(() => Brainfuck.Run("+[]", maxSteps: 1000));
    }
}
