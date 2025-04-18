using System;
using System.Collections.Generic;
using System.Linq; // For Count() if used
using System.Diagnostics;
using Caro.NET;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

// --- Assume Move struct is defined ---
// public struct Move { public int Row; public int Col; ... }

namespace Caro.NET
{

    public class CaroAI_Minimax
    {
        // --- Constants ---
        public const int BOARD_SIZE = Utils.MAX_ROWS;
        public const int EMPTY_CELL = 0;
        public const int PLAYER_X = 1; // AI Player (Maximizer)
        public const int PLAYER_O = 2; // Human Player (Minimizer)
        public const int WIN_LENGTH = 5;

        // --- Evaluation Scores (Simpler for Minimax Leaf Nodes) ---
        // Magnitudes should be distinct: Win > Threat > Positional
        private const int WIN_SCORE = 1000000;
        private const int LOSE_SCORE = -1000000;
        private const int DRAW_SCORE = 0;
        // Heuristic scores for non-terminal leaves (can be tuned)
        private const int OPEN_FOUR_SCORE = 50000;
        private const int CLOSED_FOUR_SCORE = 5000;
        private const int OPEN_THREE_SCORE = 500;
        private const int CLOSED_THREE_SCORE = 50;
        private const int OPEN_TWO_SCORE = 5;
        private const int CLOSED_TWO_SCORE = 1;

        private static readonly Random _random = new Random(); // For tie-breaking moves

        // --- CheckWin Function (Same as before) ---
        /// <summary>
        /// Checks if the specified 'player' wins at position (r, c).
        /// Adheres to Caro rules: Exactly 5, not double-blocked by opponent.
        /// </summary>
        public bool CheckWin(int r, int c, int player, int[,] currentBoard)
        {
            // ... (Implementation is the same as the robust one provided previously) ...
            if (r < 0 || r >= BOARD_SIZE || c < 0 || c >= BOARD_SIZE || currentBoard[r, c] != player) return false;
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };
            foreach (var dir in directions)
            {
                int count = 1, blockedEnds = 0;
                int r_check = r + dir.dr, c_check = c + dir.dc;
                while (r_check >= 0 && r_check < BOARD_SIZE && c_check >= 0 && c_check < BOARD_SIZE && currentBoard[r_check, c_check] == player) { count++; r_check += dir.dr; c_check += dir.dc; }
                if (r_check < 0 || r_check >= BOARD_SIZE || c_check < 0 || c_check >= BOARD_SIZE || currentBoard[r_check, c_check] == opponent) blockedEnds++;
                r_check = r - dir.dr; c_check = c - dir.dc;
                while (r_check >= 0 && r_check < BOARD_SIZE && c_check >= 0 && c_check < BOARD_SIZE && currentBoard[r_check, c_check] == player) { count++; r_check -= dir.dr; c_check -= dir.dc; }
                if (r_check < 0 || r_check >= BOARD_SIZE || c_check < 0 || c_check >= BOARD_SIZE || currentBoard[r_check, c_check] == opponent) blockedEnds++;
                if (count == WIN_LENGTH && blockedEnds < 2) return true;
            }
            return false;
        }

        // --- GetPossibleMoves Function (Same as before - optimized preferred) ---
        /// <summary>
        /// Gets a list of potential empty cells for the AI to consider,
        /// typically optimized to look near existing pieces.
        /// </summary>
        private List<Move> GetPossibleMoves(int[,] currentBoard)
        {
            // ... (Implementation is the same as the optimized one provided previously) ...
            var possibleMoves = new Dictionary<string, Move>(); int radius = 2; bool hasStones = false;
            for (int r = 0; r < BOARD_SIZE; r++) { for (int c = 0; c < BOARD_SIZE; c++) { if (currentBoard[r, c] != EMPTY_CELL) { hasStones = true; for (int i = -radius; i <= radius; i++) { for (int j = -radius; j <= radius; j++) { if (i == 0 && j == 0) continue; int nr = r + i; int nc = c + j; string key = $"{nr}-{nc}"; if (nr >= 0 && nr < BOARD_SIZE && nc >= 0 && nc < BOARD_SIZE && currentBoard[nr, nc] == EMPTY_CELL && !possibleMoves.ContainsKey(key)) { possibleMoves.Add(key, new Move(nr, nc)); } } } } } }
            if (!hasStones) { int center = BOARD_SIZE / 2; if (currentBoard[center, center] == EMPTY_CELL) return new List<Move> { new Move(center, center) }; }
            if (hasStones && possibleMoves.Count == 0) { Console.WriteLine("Warning: No moves near stones! Searching ANY empty."); for (int r = 0; r < BOARD_SIZE; r++) for (int c = 0; c < BOARD_SIZE; c++) if (currentBoard[r, c] == EMPTY_CELL) return new List<Move> { new Move(r, c) }; return new List<Move>(); }
            return new List<Move>(possibleMoves.Values);
        }

        // --- Evaluation Function (for Minimax Leaf Nodes) ---
        /// <summary>
        /// Evaluates the board state from the perspective of the AI player (PLAYER_X).
        /// Used by Minimax at terminal nodes or max depth. Higher score is better for AI.
        /// Recognizes wins/losses and uses a simple heuristic otherwise.
        /// </summary>
        private int EvaluateBoard(int[,] currentBoard, int lastPlayerWhoMoved)
        {
            int aiPlayer = PLAYER_X;
            int opponentPlayer = PLAYER_O;

            // 1. Check for immediate win/loss based on the last move (more efficient)
            // Need coordinates of last move if possible, otherwise scan relevant parts
            // For simplicity here, we might just check the whole board or rely on depth limit.
            // A full implementation would check if 'lastPlayerWhoMoved' just won.
            // Let's assume for now we check win state generally if depth is 0 or game ends.

            // A more robust check would involve checking if either player has won
            // bool aiWon = CheckIfPlayerWon(currentBoard, aiPlayer); // Requires a function to check the whole board
            // bool opponentWon = CheckIfPlayerWon(currentBoard, opponentPlayer);
            // if(aiWon) return WIN_SCORE;
            // if(opponentWon) return LOSE_SCORE;
            // if(IsBoardFull(currentBoard)) return DRAW_SCORE; // Requires IsBoardFull check

            // 2. Simple Heuristic Score (if not a terminal win/loss state)
            // Calculate score based on patterns (can be simpler than previous heuristics)
            // This is just one example heuristic; tuning is needed.
            int score = 0;
            // Ideally, scan lines efficiently (horizontal, vertical, diagonals)
            // For simplicity, this example might be less efficient than a dedicated pattern scanner
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (currentBoard[r, c] == EMPTY_CELL) continue;
                    // Simplified: Add points for AI pieces, subtract for opponent
                    // A real version would count patterns like open threes, etc.
                    // score += (currentBoard[r,c] == aiPlayer) ? 1 : -1;
                }
            }

            // --- Add scores for significant patterns ---
            // This requires helper functions to count patterns efficiently
            // score += CountPatterns(currentBoard, aiPlayer, OPEN_FOUR_SCORE, CLOSED_FOUR_SCORE, ...);
            // score -= CountPatterns(currentBoard, opponentPlayer, OPEN_FOUR_SCORE, CLOSED_FOUR_SCORE, ...);

            // Placeholder: return 0 if no win/loss detected and no heuristic implemented yet
            // You MUST implement a proper heuristic evaluation here based on patterns.
            // Example: return CalculateHeuristicScore(currentBoard, aiPlayer);
            return 0; // ** REPLACE WITH ACTUAL HEURISTIC CALCULATION **
        }

        // --- Minimax with Alpha-Beta Pruning ---
        /// <summary>
        /// The core recursive Minimax function with Alpha-Beta pruning.
        /// </summary>
        /// <param name="board">Current board state.</param>
        /// <param name="depth">Remaining search depth.</param>
        /// <param name="alpha">Best score found so far for Maximizer (AI).</param>
        /// <param name="beta">Best score found so far for Minimizer (Opponent).</param>
        /// <param name="isMaximizingPlayer">True if it's AI's turn (maximize), False if opponent's (minimize).</param>
        /// <param name="lastMove">Optional: The last move made to reach this state (for win checking).</param>
        /// <returns>The evaluated score for this board state.</returns>
        private int AlphaBeta(int[,] board, int depth, int alpha, int beta, bool isMaximizingPlayer, Move? lastMove = null)
        {
            // --- Base Cases ---
            // 1. Check if game is over (win/loss) based on last move
            if (lastMove.HasValue)
            {
                int lastPlayer = isMaximizingPlayer ? PLAYER_O : PLAYER_X; // Player who made the lastMove
                if (CheckWin(lastMove.Value.Row, lastMove.Value.Col, lastPlayer, board))
                {
                    // If opponent just won (AI is minimizing), return LOSE_SCORE.
                    // If AI just won (AI is maximizing), return WIN_SCORE.
                    // Add depth to score to prioritize faster wins / slower losses
                    return isMaximizingPlayer ? (LOSE_SCORE - depth) : (WIN_SCORE + depth);
                }
            }

            // 2. Check if max depth reached or board is full (draw)
            // bool boardFull = IsBoardFull(board); // Requires IsBoardFull function
            bool boardFull = false; // Placeholder
            if (depth == 0 || boardFull)
            {
                // Evaluate the leaf node using the heuristic function
                // Pass the player whose turn it *would* be if game continued (for evaluation perspective)
                int evalPlayer = isMaximizingPlayer ? PLAYER_X : PLAYER_O;
                return EvaluateBoard(board, evalPlayer); // ** NEEDS IMPLEMENTATION **
            }

            // --- Recursive Step ---
            List<Move> possibleMoves = GetPossibleMoves(board);
            // Optional: Order moves (e.g., evaluate promising moves first) for better pruning
            // possibleMoves = OrderMoves(board, possibleMoves, isMaximizingPlayer);

            if (isMaximizingPlayer) // AI's turn (Maximize score)
            {
                int maxEval = int.MinValue;
                foreach (var move in possibleMoves)
                {
                    // Create a copy of the board to simulate the move
                    int[,] nextBoard = (int[,])board.Clone();
                    nextBoard[move.Row, move.Col] = PLAYER_X; // AI makes the move

                    // Recursive call for the opponent's turn
                    int eval = AlphaBeta(nextBoard, depth - 1, alpha, beta, false, move);

                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval); // Update alpha (best score for maximizer)

                    // Alpha-Beta Pruning check
                    if (beta <= alpha)
                    {
                        break; // Prune this branch
                    }
                }
                return maxEval;
            }
            else // Opponent's turn (Minimize score)
            {
                int minEval = int.MaxValue;
                foreach (var move in possibleMoves)
                {
                    // Create a copy of the board to simulate the move
                    int[,] nextBoard = (int[,])board.Clone();
                    nextBoard[move.Row, move.Col] = PLAYER_O; // Opponent makes the move

                    // Recursive call for the AI's turn
                    int eval = AlphaBeta(nextBoard, depth - 1, alpha, beta, true, move);

                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval); // Update beta (best score for minimizer)

                    // Alpha-Beta Pruning check
                    if (beta <= alpha)
                    {
                        break; // Prune this branch
                    }
                }
                return minEval;
            }
        }

        // --- Top-Level Function to Find Best Move ---
        /// <summary>
        /// Finds the best move for the AI player using Minimax with Alpha-Beta pruning.
        /// </summary>
        /// <param name="currentBoard">The current board state.</param>
        /// <param name="player">The AI player (PLAYER_X).</param>
        /// <param name="depth">The maximum search depth.</param>
        /// <returns>The best Move found, or null if no moves are possible.</returns>
        public Move? FindBestMove(int[,] currentBoard, int player, int depth)
        {
            if (player != PLAYER_X)
            {
                // This implementation assumes AI is always PLAYER_X (Maximizer)
                Console.Error.WriteLine("Error: Minimax AI currently only supports playing as PLAYER_X.");
                return null;
            }

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0) return null;

            Move? bestMove = null;
            int bestScore = int.MinValue;
            int alpha = int.MinValue;
            int beta = int.MaxValue;

            // Iterate through the first level of moves for the AI
            foreach (var move in possibleMoves)
            {
                // Create a copy for the first move simulation
                int[,] nextBoard = (int[,])currentBoard.Clone();
                nextBoard[move.Row, move.Col] = player; // AI makes the move

                // Call AlphaBeta for the opponent's subsequent turn
                int score = AlphaBeta(nextBoard, depth - 1, alpha, beta, false, move); // false = Minimizer's turn next

                Console.WriteLine($"Debug: Move {move} evaluated with score {score}"); // Debugging

                // Update the best move found so far for the AI (Maximizer)
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                // Optional: Random tie-breaking if scores are equal
                else if (score == bestScore && _random.NextDouble() < 0.5)
                {
                    bestMove = move;
                }

                // Update alpha for the top level (though less critical here than inside recursion)
                alpha = Math.Max(alpha, score);

                // Note: No beta check needed at the absolute top level loop
            }

            if (bestMove.HasValue)
            {
                Console.WriteLine($"AI Minimax: Chosen move {bestMove.Value} with score {bestScore} at depth {depth}");
            }
            else if (possibleMoves.Count > 0)
            {
                Console.WriteLine("Warning: AI Minimax: No move selected, choosing first possible.");
                bestMove = possibleMoves[0]; // Fallback if something went wrong
            }
            else
            {
                Console.Error.WriteLine("AI Minimax: No possible moves found.");
            }

            return bestMove;
        }

        // --- Placeholder for Heuristic Evaluation ---
        // You MUST replace this with a real evaluation based on patterns
        private int CalculateHeuristicScore(int[,] board, int player)
        {
            // Example: return count of player's open threes - count of opponent's open threes
            // This needs to be implemented properly by scanning the board for patterns.
            Console.WriteLine("Warning: CalculateHeuristicScore needs implementation!");
            return 0;
        }

        // Placeholder for IsBoardFull check
        private bool IsBoardFull(int[,] board)
        {
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (board[r, c] == EMPTY_CELL) return false;
                }
            }
            return true;
        }

        // Placeholder/Example for Move Ordering (Optional Optimization)
        private List<Move> OrderMoves(int[,] board, List<Move> moves, bool isMaximizingPlayer)
        {
            // Simple example: Evaluate moves quickly with depth 1 or simple heuristic
            // and sort them to explore potentially better moves first.
            // This can significantly improve alpha-beta pruning effectiveness.
            // ... Implementation needed ...
            return moves; // Return original list if not implemented
        }

    } // End of CaroAI_Minimax class

}
// --- Example Usage ---
/*
public class GameController
{
    private int[,] gameBoard;
    private CaroAI_Minimax caroAI;
    private int currentPlayer;
    private bool isGameOver;
    private int aiSearchDepth = 4; // Adjust search depth (higher = stronger but slower)

    public GameController() {
        gameBoard = new int[CaroAI_Minimax.BOARD_SIZE, CaroAI_Minimax.BOARD_SIZE];
        caroAI = new CaroAI_Minimax();
        // ... Initialize game ...
    }

    // ... HandleMove logic ...

    public async Task TriggerAIMoveAsync() {
        if (isGameOver || currentPlayer != CaroAI_Minimax.PLAYER_X) return;
        Console.WriteLine($"AI Minimax (Depth {aiSearchDepth}) is thinking...");
        var stopwatch = Stopwatch.StartNew();

        // Use Task.Run for the potentially long Minimax calculation
        Move? aiMove = await Task.Run(() => caroAI.FindBestMove(gameBoard, CaroAI_Minimax.PLAYER_X, aiSearchDepth));

        stopwatch.Stop();
        Console.WriteLine($"AI calculation time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");

        if (aiMove.HasValue) {
            // Make the move using the game's HandleMove or similar method
            // MakeMove(aiMove.Value.Row, aiMove.Value.Col);
            Console.WriteLine($"AI intends to move to {aiMove.Value}"); // Placeholder
        } else {
            Console.Error.WriteLine("AI Error: Minimax cannot find a move.");
        }
    }
}
*/
/*
```

**Explanation and Important Notes:**

1.  * *`CaroAI_Minimax` Class:**Encapsulates the AI logic.
2.  **`SCORE_...` Constants:**Simplified scores used only by the `EvaluateBoard` function at the leaves of the search tree. `WIN_SCORE` and `LOSE_SCORE` must be much larger than heuristic scores.
3.  **`EvaluateBoard` Function:****THIS IS CRITICAL AND NEEDS IMPLEMENTATION.** The placeholder returns 0. You need to write logic here to score the board based on patterns (open threes, closed fours, etc.) for both players when the search reaches maximum depth or a non-win/loss terminal state. The quality of this function significantly impacts AI strength.
4.  **`AlphaBeta` Function:**The core recursive Minimax implementation with pruning.
    * It checks for base cases (depth 0, win/loss).
    * It alternates between maximizing (AI) and minimizing (opponent) scores.
    * It uses `alpha` and `beta` to prune branches (`if (beta <= alpha) break;`).
    ***Board Copying: **It uses `board.Clone()` to create copies for simulating moves. This is crucial to avoid modifying the board state of parent nodes in the recursion.
5.  **`FindBestMove` Function:**The top - level function that initiates the search for the current board state. It calls `AlphaBeta` for each possible first move and chooses the move leading to the best score returned by the recursive search.
6.  **Search Depth:**The `depth` parameter controls how many moves ahead the AI looks. A higher depth means stronger play but exponentially increases computation time. You'll need to experiment to find a depth that provides good play within acceptable thinking time (e.g., start with 3 or 4).
7.  **Optimizations (Optional but Recommended):**
    ***Move Ordering: **Evaluating more promising moves first drastically improves alpha-beta pruning effectiveness. Implement the `OrderMoves` function.
    * **Transposition Tables:**Store results for previously evaluated board states to avoid re-computation (more advanced).
    * **Efficient Pattern Matching:**The `EvaluateBoard` function needs an efficient way to count relevant patterns.

This code provides a solid foundation for a Minimax AI for Caro. The most important next step is to implement a good heuristic `EvaluateBoard` function
*/