namespace CSharpExercises.Simulations;

/// <summary>
/// Perfect play by minimax with alpha-beta pruning: each side assumes the other plays its best move, and branches
/// that can't change the decision are skipped. Scores prefer faster wins and slower losses. The board is a string
/// of 9 characters ('X', 'O' or '.'), row by row.
/// </summary>
public static class TicTacToe
{
    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // columns
        [0, 4, 8], [2, 4, 6],            // diagonals
    ];

    public static char? Winner(string board)
    {
        foreach (var line in Lines)
        {
            var c = board[line[0]];
            if (c != '.' && c == board[line[1]] && c == board[line[2]]) return c;
        }
        return null;
    }

    public static bool IsDraw(string board) => Winner(board) is null && !board.Contains('.');

    public static char ToMove(string board) => board.Count(c => c == 'X') > board.Count(c => c == 'O') ? 'O' : 'X';

    /// <summary>The best square for the player to move, and the outcome with perfect play: +1 win, 0 draw, -1 loss.</summary>
    public static (int Square, int Outcome) BestMove(string board)
    {
        Validate(board);
        if (Winner(board) is not null || !board.Contains('.')) throw new InvalidOperationException("The game is over.");

        var player = ToMove(board);
        var cells = board.ToCharArray();
        var (bestSquare, bestScore) = (-1, int.MinValue);

        foreach (var square in EmptySquares(cells))
        {
            cells[square] = player;
            var score = -Negamax(cells, Opponent(player), 1, int.MinValue + 1, int.MaxValue);
            cells[square] = '.';
            if (score > bestScore) (bestSquare, bestScore) = (square, score);
        }
        return (bestSquare, Math.Sign(bestScore));
    }

    /// <summary>
    /// Negamax: the score for the player to move is minus the opponent's best score, so one function serves both
    /// sides. A win scores 10 minus the depth, so quicker wins are worth more.
    /// </summary>
    private static int Negamax(char[] cells, char player, int depth, int alpha, int beta)
    {
        var board = new string(cells);
        if (Winner(board) is not null) return -(10 - depth); // the previous move won
        if (!board.Contains('.')) return 0;

        var best = int.MinValue + 1;
        foreach (var square in EmptySquares(cells))
        {
            cells[square] = player;
            var score = -Negamax(cells, Opponent(player), depth + 1, -beta, -alpha);
            cells[square] = '.';

            best = Math.Max(best, score);
            alpha = Math.Max(alpha, score);
            if (alpha >= beta) break; // the opponent already has a better option elsewhere
        }
        return best;
    }

    /// <summary>Plays a whole game between two perfect players.</summary>
    public static string SelfPlay(string board = ".........")
    {
        while (Winner(board) is null && board.Contains('.'))
        {
            var (square, _) = BestMove(board);
            board = board[..square] + ToMove(board) + board[(square + 1)..];
        }
        return board;
    }

    private static IEnumerable<int> EmptySquares(char[] cells)
    {
        // Centre and corners first: better moves early means more pruning.
        foreach (var square in (int[])[4, 0, 2, 6, 8, 1, 3, 5, 7])
            if (cells[square] == '.') yield return square;
    }

    private static char Opponent(char player) => player == 'X' ? 'O' : 'X';

    private static void Validate(string board)
    {
        if (board.Length != 9 || board.Any(c => c is not ('X' or 'O' or '.')))
            throw new ArgumentException("A board is 9 characters of X, O or '.'.", nameof(board));
        var difference = board.Count(c => c == 'X') - board.Count(c => c == 'O');
        if (difference is not (0 or 1)) throw new ArgumentException("X moves first and players alternate.", nameof(board));
    }
}
