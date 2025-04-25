using System;
using System.Collections.Generic;
using System.Diagnostics; // For Stopwatch (optional timing)
using System.Linq;
using System.Threading.Tasks; // For asynchronous AI move trigger


namespace Caro.NET
{
    /// <summary>
    /// Represents a move on the Caro board.
    /// </summary>
    public struct Move
    {
        public int Row;
        public int Col;

        public Move(int r, int c) { Row = r; Col = c; }

        // Override for potential use in collections or debugging
        public override bool Equals(object obj) => obj is Move other && Row == other.Row && Col == other.Col;
        public override int GetHashCode() => HashCode.Combine(Row, Col);
        public override string ToString() => $"({Row}, {Col})";
    }

    /// <summary>
    /// Contains the logic for the Caro (Gomoku) AI using an aggressive strategy
    /// with enhanced evaluation for Vietnamese rules (double-blocking).
    /// </summary>
    public class CaroAI
    {
        // --- 1. Constants ---
        public const int BOARD_SIZE = Utils.MAX_ROWS;      // Board size (can be changed)
        public const int EMPTY_CELL = 0;
        public const int PLAYER_X = 1;         // Assume AI is player X
        public const int PLAYER_O = 2;         // Assume Human is player O
        public const int WIN_LENGTH = 5;       // Number of consecutive pieces to win

        // --- 2. Score Table for Aggressive AI (Version 2) ---
        // Includes NEUTRALIZE bonus for correctly double-blocking opponent threats
        private static readonly Dictionary<string, int> SCORE_AGGRESSIVE_V2 = new Dictionary<string, int>
        {
            {"FIVE", 10000000},        // Winning move score
            {"BLOCK_FIVE", 9000000},   // Score for blocking opponent from forming 5 (even if not yet a winning 5)

            // Bonus score for successfully double-blocking opponent's dangerous sequence
            {"NEUTRALIZE_FOUR_BONUS", 750000}, // Must be significantly higher than BLOCK_FOUR_OPEN, Keep high for rewarding double-blocks
            {"NEUTRALIZE_THREE_BONUS", 80000}, // Must be significantly higher than BLOCK_THREE_OPEN

            {"FOUR_OPEN", 800000},      // Creating own open four remains very valuable, (xxxx_)
            {"BLOCK_FOUR_OPEN", 300000}, // Blocking one end of opponent's open four

            {"THREE_OPEN", 60000}, // Keep offensive value high for now, (__xxx__)
            {"BLOCK_THREE_OPEN", 50000},  // SIGNIFICANTLY INCREASED from 10000

            //{"THREE_OPEN", 50000},      // Creating own open three
            //{"BLOCK_THREE_OPEN", 10000}, // Blocking one end of opponent's open three

            // Other scores remain similar to the previous aggressive version
            {"FOUR_CLOSED", 60000 + 2},     // Creating a closed four, (_xxxxo  or  oxxxx_)
            {"BLOCK_FOUR_CLOSED", 50000},// Blocking opponent's closed four
            
            {"BLOCK_THREE_CLOSED", 300}, // Blocking opponent's closed three

            {"TWO_OPEN", 200},          // Creating an open two, (___xx___)
            {"BLOCK_TWO_OPEN", 200},     // Blocking opponent's open two
            {"THREE_CLOSED", 300},      // Creating a closed three , 150 => 300 , (_xxxo_  or o___xxx__)
            {"TWO_CLOSED", 15},         // Creating a closed two, (_xxo___ or o___xx__)
            {"BLOCK_TWO_CLOSED", 10},   // Blocking opponent's closed two
            {"ONE_OPEN", 2},            // One piece with two open ends (____x____)
            {"ONE_CLOSED", 1}           // One piece with one open end (_xo______)
        };

        // Random number generator for tie-breaking
        private static readonly Random _random = new Random();

        // --- 3. CheckWin Function ---
        /// <summary>
        /// Checks if the specified 'player' wins at position (r, c) on the given board.
        /// Adheres to Caro rules: Exactly 5 consecutive pieces, not blocked on both ends by the opponent.
        /// </summary>
        /// <param name="r">Row index.</param>
        /// <param name="c">Column index.</param>
        /// <param name="player">The player to check for (PLAYER_X or PLAYER_O).</param>
        /// <param name="currentBoard">The board state (2D array) to check.</param>
        /// <returns>True if the player wins, false otherwise.</returns>
        public Dictionary<string, Move>? CheckWin(int r, int c, int player, int[,] currentBoard)
        {
            if (IsOutOfCaroBoard(r, c) || currentBoard[r, c] != player)
            {
                //return false; // Invalid cell or not the player's piece
                return null;
            }

            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) }; // H, V, Diag\, Diag/

            foreach (var dir in directions)
            {
                int dr = dir.dr;
                int dc = dir.dc;
                int count = 1; // Include the piece at (r, c)
                int blockedEnds = 0; // Count ends blocked by the opponent
                var possibleWin = new Dictionary<string, Move>();
                var possibleBlock = new Dictionary<string, Move>();

                // Check positive direction (+dr, +dc)
                int r_check = r + dr;
                int c_check = c + dc;
                while (IsInCaroBoard(r_check, c_check) && currentBoard[r_check, c_check] == player)
                {
                    possibleWin.Add($"{dr}_{dc}:{r_check}-{c_check}", new Move(r_check, c_check));

                    count++;
                    r_check += dr;
                    c_check += dc;
                }
                if (IsOutOfCaroBoard(r_check, c_check) || currentBoard[r_check, c_check] == opponent)
                {
                    possibleWin.Add($"{dr}_{dc}:forward", new Move(r_check, c_check));

                    blockedEnds++;
                }

                // Check negative direction (-dr, -dc)
                r_check = r - dr;
                c_check = c - dc;
                while (IsInCaroBoard(r_check, c_check) && currentBoard[r_check, c_check] == player)
                {
                    possibleWin.Add($"{dr}_{dc}:{r_check}-{c_check}", new Move(r_check, c_check));

                    count++;
                    r_check -= dr;
                    c_check -= dc;
                }
                if (IsOutOfCaroBoard(r_check, c_check) || currentBoard[r_check, c_check] == opponent)
                {
                    possibleWin.Add($"{dr}_{dc}:backward", new Move(r_check, c_check));

                    blockedEnds++;
                }

                // Evaluate win condition: Exactly WIN_LENGTH and not blocked on both ends by opponent
                if (count == WIN_LENGTH && blockedEnds < 2)
                {
                    //return true; // Win!
                    var last = possibleWin.Last();
                    possibleWin.Remove(last.Key);

                    string[] keys = possibleWin.Keys.First().Split(":");
                    possibleWin.Add($"{keys[0]}:{r}-{c}", new Move(r, c));

                    possibleWin.Add(last.Key , last.Value);

                    return possibleWin;
                }
            }
            //return false; // No winning line found
            return null; // No winning line found
        }

        /*
        // --- 4. ScoreLine Function (Version 2 - Includes Neutralize Bonus) ---
        /// <summary>
        /// Calculates the score for a single line passing through the cell (row, col)
        /// where 'player' intends to place a piece. Uses SCORE_AGGRESSIVE_V2.
        /// Includes a bonus for neutralizing opponent threats by double-blocking.
        /// IMPORTANT: Assumes player's piece IS tentatively placed on currentBoard[row, col] before calling.
        /// </summary>
        private int ScoreLine_V2(int[,] currentBoard, int row, int col, int dr, int dc, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            // ***** Use the new V2 score table *****
            var currentScores = SCORE_AGGRESSIVE_V2;

            // === Helper Function: Count and Score Sequence ===
            // Calculates the score based on a sequence of player 'p' pieces.
            int CountAndScoreSequence(int r, int c, int p)
            {
                int consecutive = 0;
                int openEnds = 0;
                bool startBlockedByOpponent = false;
                bool endBlockedByOpponent = false;
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;


                // Count backwards
                int backward_r = r - dr; int backward_c = c - dc;
                while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p)
                {
                    consecutive++; backward_r -= dr; backward_c -= dc;
                }
                if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) {/* Boundary }
                else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) { openEnds++; }
                else if (currentBoard[backward_r, backward_c] == otherPlayer) { startBlockedByOpponent = true; }

                // Count forwards
                backward_r = r + dr; backward_c = c + dc;
                while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p)
                {
                    consecutive++; backward_r += dr; backward_c += dc;
                }
                if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) {/* Boundary }
                else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) { openEnds++; }
                else if (currentBoard[backward_r, backward_c] == otherPlayer) { endBlockedByOpponent = true; }

                consecutive++; // Count the piece at (r, c)

                // Calculate score based on the sequence length and ends
                if (consecutive >= WIN_LENGTH)
                {
                    return (consecutive == WIN_LENGTH && !(startBlockedByOpponent && endBlockedByOpponent)) ? currentScores["FIVE"] : 0;
                }
                else if (consecutive == 4)
                {
                    return (openEnds == 2) ? currentScores["FOUR_OPEN"] : ((openEnds == 1) ? currentScores["FOUR_CLOSED"] : 0);
                }
                else if (consecutive == 3)
                {
                    return (openEnds == 2) ? currentScores["THREE_OPEN"] : ((openEnds == 1) ? currentScores["THREE_CLOSED"] : 0);
                }
                else if (consecutive == 2)
                {
                    return (openEnds == 2) ? currentScores["TWO_OPEN"] : ((openEnds == 1) ? currentScores["TWO_CLOSED"] : 0);
                }
                else if (consecutive == 1)
                {
                    return (openEnds == 2) ? currentScores["ONE_OPEN"] : ((openEnds == 1) ? currentScores["ONE_CLOSED"] : 0);
                }
                return 0;
            }
            // === End of Helper Function ===

            // === Main logic of scoreLine_V2 ===

            // 1. Calculate offensive score (for player)
            // Assumes player's piece is already placed at currentBoard[row, col]
            int playerScore = CountAndScoreSequence(row, col, player);

            // 2. Calculate defensive score (opponent's potential if they played here)
            currentBoard[row, col] = opponent; // Tentatively place opponent
            int opponentPotentialScore = CountAndScoreSequence(row, col, opponent);
            currentBoard[row, col] = player;   // Restore player's piece IMPORTANT!

            // 3. Calculate Neutralize Bonus (NEW STEP)
            // Check if placing the player's piece at (row, col) now double-blocks
            // a significant opponent sequence along this line.
            int neutralizeBonus = 0;
            // --- Helper function to check for neutralization ---
            // Checks if the move 'p_move' at 'r_center', 'c_center' double-blocks an opponent sequence
            int CheckNeutralizeBonus(int r_center, int c_center, int p_move)
            {
                int bonus = 0;
                int opp = (p_move == PLAYER_X) ? PLAYER_O : PLAYER_X;

                // Check sequences ending at the player's new move (P) where the other end is already blocked
                // Looks for patterns like [Blocker] O...O P or P O...O [Blocker]
                // [Blocker] can be player's piece or board boundary
                for (int len = 3; len <= WIN_LENGTH; len++) // Check if opponent sequences of length 2, 3, or 4 are double-blocked (needs len-1 opponent pieces)
                {
                    int oppSequenceLength = len - 1; // The number of opponent pieces needed

                    // Check backwards from P: [Blocker] O...O P (total length 'len')
                    int blocker_r = r_center - len * dr;
                    int blocker_c = c_center - len * dc;

                    // Check if the start position is blocked by player or boundary
                    bool start_is_blocked = blocker_r < 0 || blocker_r >= BOARD_SIZE || blocker_c < 0 || blocker_c >= BOARD_SIZE || currentBoard[blocker_r, blocker_c] == p_move;

                    if (start_is_blocked)
                    {
                        bool sequence_matches = true;
                        // Check if the 'oppSequenceLength' pieces between Blocker and P are opponent's pieces
                        for (int k = 1; k <= oppSequenceLength; k++)
                        {
                            int check_r = r_center - k * dr;
                            int check_c = c_center - k * dc;
                            // Check boundaries for the pieces being checked
                            if (check_r < 0 || check_r >= BOARD_SIZE || check_c < 0 || check_c >= BOARD_SIZE || currentBoard[check_r, check_c] != opp)
                            {
                                sequence_matches = false;
                                break;
                            }
                        }

                        if (sequence_matches)
                        {
                            // Found opponent sequence of length 'oppSequenceLength' double-blocked by this move
                            if (oppSequenceLength == 4) bonus = Math.Max(bonus, currentScores["NEUTRALIZE_FOUR_BONUS"]);
                            else if (oppSequenceLength == 3) bonus = Math.Max(bonus, currentScores["NEUTRALIZE_THREE_BONUS"]);
                            // Add bonus for neutralizing 2 if desired
                        }
                    }

                    // Check forwards from P: P O...O [Blocker] (total length 'len')
                    int end_blocker_r = r_center + len * dr;
                    int end_blocker_c = c_center + len * dc;

                    // Check if the end position is blocked by player or boundary
                    bool end_is_blocked = end_blocker_r < 0 || end_blocker_r >= BOARD_SIZE || end_blocker_c < 0 || end_blocker_c >= BOARD_SIZE || currentBoard[end_blocker_r, end_blocker_c] == p_move;

                    if (end_is_blocked)
                    {
                        bool sequence_matches = true;
                        // Check if the 'oppSequenceLength' pieces between P and Blocker are opponent's pieces
                        for (int k = 1; k <= oppSequenceLength; k++)
                        {
                            int check_r = r_center + k * dr;
                            int check_c = c_center + k * dc;
                            if (check_r < 0 || check_r >= BOARD_SIZE || check_c < 0 || check_c >= BOARD_SIZE || currentBoard[check_r, check_c] != opp)
                            {
                                sequence_matches = false;
                                break;
                            }
                        }
                        if (sequence_matches)
                        {
                            // Found opponent sequence of length 'oppSequenceLength' double-blocked by this move
                            if (oppSequenceLength == 4) bonus = Math.Max(bonus, currentScores["NEUTRALIZE_FOUR_BONUS"]);
                            else if (oppSequenceLength == 3) bonus = Math.Max(bonus, currentScores["NEUTRALIZE_THREE_BONUS"]);
                        }
                    }
                }
                return bonus;
            } // End of CheckNeutralizeBonus helper

            neutralizeBonus = CheckNeutralizeBonus(row, col, player);

            // 4. Combine scores
            if (playerScore >= currentScores["FIVE"]) return currentScores["FIVE"];
            if (opponentPotentialScore >= currentScores["FIVE"]) return currentScores["BLOCK_FIVE"];

            int blockScore = 0; // Score for blocking just one end (as before)
            if (opponentPotentialScore >= currentScores["FOUR_OPEN"]) blockScore = currentScores["BLOCK_FOUR_OPEN"];
            else if (opponentPotentialScore >= currentScores["FOUR_CLOSED"]) blockScore = currentScores["BLOCK_FOUR_CLOSED"];
            else if (opponentPotentialScore >= currentScores["THREE_OPEN"]) blockScore = currentScores["BLOCK_THREE_OPEN"];
            else if (opponentPotentialScore >= currentScores["THREE_CLOSED"]) blockScore = currentScores["BLOCK_THREE_CLOSED"];
            else if (opponentPotentialScore >= currentScores["TWO_OPEN"]) blockScore = currentScores["BLOCK_TWO_OPEN"];
            else if (opponentPotentialScore >= currentScores["TWO_CLOSED"]) blockScore = currentScores["BLOCK_TWO_CLOSED"];

            // Final score = Offensive Score + Single-End Block Score + Double-Block Bonus
            // The NEUTRALIZE bonus will significantly boost the score of moves like the outer block in X_OOOOX
            return playerScore + blockScore + neutralizeBonus;
        } // End of scoreLine_V2
        */

        /*
        // --- 5. Updated EvaluateCell Function ---
        /// <summary>
        /// Calculates the total score for placing 'player's piece at (row, col).
        /// IMPORTANT: This version MUST call scoreLine_V2.
        /// </summary>
        private int EvaluateCell_V2(int[,] currentBoard, int row, int col, int player)
        {
            if (currentBoard[row, col] != EMPTY_CELL)
            {
                return int.MinValue; // Cell is already occupied
            }

            int totalScore = 0;
            (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };

            // Tentatively place the player's piece
            currentBoard[row, col] = player;

            // Call the UPDATED scoreLine function for each direction
            foreach (var dir in directions)
            {
                // *** CALL V2 of scoreLine ***
                totalScore += ScoreLine_V2(currentBoard, row, col, dir.dr, dir.dc, player);
            }

            // Revert the board
            currentBoard[row, col] = EMPTY_CELL;

            // Optional: Add positional bonus
            double centerBonus = (
                (BOARD_SIZE / 2.0 - Math.Abs(row - (BOARD_SIZE - 1.0) / 2.0)) +
                (BOARD_SIZE / 2.0 - Math.Abs(col - (BOARD_SIZE - 1.0) / 2.0))
            );
            totalScore += (int)(centerBonus * 0.5); // Apply a small weight

            return totalScore;
        }
        */


        // --- 6. GetPossibleMoves function remains the same ---
        /// <summary>
        /// Gets a list of potential empty cells for the AI to consider.
        /// Optimization: Only considers empty cells adjacent (within 'radius')
        /// to existing pieces on the board.
        /// </summary>
        private List<Move> GetPossibleMoves(int[,] currentBoard)
        {
            var possibleMoves = new Dictionary<string, Move>();
            int radius = 2; // Search radius around existing pieces
            bool hasStones = false;

            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (currentBoard[r, c] != EMPTY_CELL)
                    {
                        hasStones = true;
                        for (int i = -radius; i <= radius; i++)
                        {
                            for (int j = -radius; j <= radius; j++)
                            {
                                if (i == 0 && j == 0) continue;
                                int nr = r + i; int nc = c + j; string key = $"{nr}-{nc}";
                                if (nr >= 0 && nr < BOARD_SIZE && nc >= 0 && nc < BOARD_SIZE &&
                                    currentBoard[nr, nc] == EMPTY_CELL && !possibleMoves.ContainsKey(key))
                                {
                                    possibleMoves.Add(key, new Move(nr, nc));
                                }
                            }
                        }
                    }
                }
            }
            if (!hasStones) { int center = BOARD_SIZE / 2; if (currentBoard[center, center] == EMPTY_CELL) return new List<Move> { new Move(center, center) }; }
            if (hasStones && possibleMoves.Count == 0)
            {
                Debug.WriteLine("Warning: No moves near stones! Searching ANY empty.");
                for (int r = 0; r < BOARD_SIZE; r++) for (int c = 0; c < BOARD_SIZE; c++) if (currentBoard[r, c] == EMPTY_CELL) return new List<Move> { new Move(r, c) };
                return new List<Move>(); // Full board
            }
            return new List<Move>(possibleMoves.Values);
        }

        /*
        // --- 7. Updated findBestMoveAggressive function ---
        /// <summary>
        /// Finds the best move for the AI using the UPDATED evaluation function (EvaluateCell_V2).
        /// The explicit check for blocking open-fours (`FindAndEvaluateForcedBlock`) is removed,
        /// relying on the improved evaluation to prioritize correctly.
        /// </summary>
        /// <param name="currentBoard">The current board state.</param>
        /// <param name="player">The AI player.</param>
        /// <returns>A nullable Move struct (Move?). Returns null if no move is possible.</returns>
        public Move? FindBestMoveAggressive_V2(int[,] currentBoard, int player)
        {
            int bestScore = int.MinValue;
            Move? bestMove = null;
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0)
            {
                Console.Error.WriteLine("AI V2: No possible moves left!");
                return null;
            }

            // Handle first move: prioritize center if available
            int moveCountOnBoard = 0;
            for (int r = 0; r < BOARD_SIZE; ++r) for (int c = 0; c < BOARD_SIZE; ++c) if (currentBoard[r, c] != EMPTY_CELL) moveCountOnBoard++;
            if (moveCountOnBoard <= 1)
            {
                int center = BOARD_SIZE / 2;
                foreach (var move in possibleMoves)
                { // More robust check
                    if (move.Row == center && move.Col == center)
                    {
                        Debug.WriteLine($"AI V2: First move, playing center ({center}, {center})");
                        return move;
                    }
                }
                // If center not in possibleMoves (e.g. small radius) & board empty, play first possible
                if (possibleMoves.Count > 0 && moveCountOnBoard == 0)
                {
                    Debug.WriteLine($"AI V2: Center not available on empty board? Playing first possible.");
                    return possibleMoves[0];
                }
            }

            // --- Check Priorities ---
            // Use a temporary board for win/loss checks to avoid modifying the original board
            // passed to EvaluateCell_V2 later in the loop.
            int[,] tempBoard = (int[,])currentBoard.Clone();

            // Priority 1: Win immediately
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, tempBoard))
                {
                    Debug.WriteLine($"AI V2: Found winning move at {move}");
                    return move; // Return winning move
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // Priority 2: Block opponent's immediate win (FIVE)
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = opponent;
                if (CheckWin(move.Row, move.Col, opponent, tempBoard))
                {
                    Debug.WriteLine($"AI V2: Found immediate block (FIVE) at {move}");
                    return move; // Return blocking move
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // *** Explicit check for blocking FOUR_OPEN is removed ***
            // Relying on EvaluateCell_V2 (with neutralize bonus) to score correctly.

            // Priority 3: Evaluate remaining moves using the NEW evaluation function
            foreach (var move in possibleMoves)
            {
                // *** CALL V2 of EvaluateCell ***
                // Pass the ORIGINAL board; EvaluateCell_V2 handles temporary placement internally
                int currentScore = EvaluateCell_V2(currentBoard, move.Row, move.Col, player);

                // Debugging: Log score for each possible move
                // Debug.WriteLine($"AI V2: Score for ({move.Row}, {move.Col}): {currentScore}");

                // Update best move if current move has higher score
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestMove = move;
                }
                // Optional: Add randomness for moves with equal scores
                else if (currentScore == bestScore && _random.NextDouble() < 0.3)
                {
                    bestMove = move;
                }
            }

            // Fallback: If no move improved score (should be rare)
            if (!bestMove.HasValue && possibleMoves.Count > 0)
            {
                Debug.WriteLine("Warning: AI V2: No best move evaluated, picking random possible one.");
                bestMove = possibleMoves[_random.Next(possibleMoves.Count)];
            }

            // Log the chosen move and its score
            if (bestMove.HasValue)
            {
                string scoreString = (bestScore != int.MinValue) ? bestScore.ToString() : "N/A (Fallback)";
                Debug.WriteLine($"AI V2: Chosen move: {bestMove.Value} with score: {scoreString}");
            }
            else
            {
                Console.Error.WriteLine("AI V2: Could not determine a best move.");
            }
            return bestMove; // Return the best move found (or null if none)

        } // End of findBestMoveAggressive_V2
        */

        /*
        /// <summary>
        /// Finds the best move using Aggressive V2 scoring.
        /// **REVISED LOGIC:** If multiple moves block an immediate opponent win,
        /// evaluates *only* those blocking moves to choose the best one.
        /// </summary>
        public Move? FindBestMoveAggressive_V3(int[,] currentBoard, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var _random = new Random(); // Ensure _random is accessible

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);

            if (possibleMoves.Count == 0)
            {
                Console.Error.WriteLine("AI V3: No possible moves left!");
                return null;
            }

            // --- Handle First Move ---
            int moveCountOnBoard = 0;
            for (int r = 0; r < BOARD_SIZE; ++r) for (int c = 0; c < BOARD_SIZE; ++c) if (currentBoard[r, c] != EMPTY_CELL) moveCountOnBoard++;
            if (moveCountOnBoard <= 1)
            {
                int center = BOARD_SIZE / 2;
                foreach (var move in possibleMoves)
                {
                    if (move.Row == center && move.Col == center)
                    {
                        Debug.WriteLine($"AI V3: First move, playing center {move}");
                        return move;
                    }
                }
                if (possibleMoves.Count > 0 && moveCountOnBoard == 0)
                {
                    Debug.WriteLine($"AI V3: Center not available? Playing first possible.");
                    return possibleMoves[0];
                }
            }

            // --- Check Priorities ---
            int[,] tempBoard = (int[,])currentBoard.Clone(); // Use clone for checks

            // Priority 1: Check if AI can win immediately
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, tempBoard))
                {
                    Debug.WriteLine($"AI V3: Found winning move at {move}");
                    // No need to revert tempBoard as we are returning
                    return move;
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // Priority 2: Find ALL moves that block an immediate opponent win (FIVE)
            List<Move> criticalBlockingMoves = new List<Move>();
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = opponent;
                if (CheckWin(move.Row, move.Col, opponent, tempBoard))
                {
                    // Found a move that blocks an immediate opponent win
                    criticalBlockingMoves.Add(move);
                    // *** DO NOT return immediately - collect all critical blocks ***
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // --- Decision based on Critical Blocking Moves ---
            if (criticalBlockingMoves.Count > 0)
            {
                Debug.WriteLine($"AI V3: Found {criticalBlockingMoves.Count} critical blocking move(s). Evaluating them...");
                // If there's only one way to block, take it.
                if (criticalBlockingMoves.Count == 1)
                {
                    Debug.WriteLine($"AI V3: Executing the only critical block at {criticalBlockingMoves[0]}");
                    return criticalBlockingMoves[0];
                }
                else
                {
                    // If there are MULTIPLE ways to block an immediate win (like the _OOOO_ case)
                    // Evaluate ONLY these critical blocking moves using the advanced EvaluateCell_V2
                    // to choose the strategically superior block.
                    int bestBlockScore = int.MinValue;
                    Move? bestBlockMove = null;

                    foreach (var blockingMove in criticalBlockingMoves)
                    {
                        // Evaluate this specific blocking move using the best evaluation function
                        int currentBlockScore = EvaluateCell_V2(currentBoard, blockingMove.Row, blockingMove.Col, player);
                        Debug.WriteLine($"   - Evaluating critical block {blockingMove}: Score = {currentBlockScore}"); // Debugging

                        if (currentBlockScore > bestBlockScore)
                        {
                            bestBlockScore = currentBlockScore;
                            bestBlockMove = blockingMove;
                        }
                        // Optional: Tie-breaking for critical blocks (e.g., prefer center, or random)
                        else if (currentBlockScore == bestBlockScore && _random.NextDouble() < 0.5)
                        {
                            bestBlockMove = blockingMove;
                        }
                    }

                    // Return the best among the critical blocking moves
                    if (bestBlockMove.HasValue)
                    {
                        Debug.WriteLine($"AI V3: Chose best critical block {bestBlockMove.Value} with score {bestBlockScore}");
                        return bestBlockMove.Value;
                    }
                    else
                    {
                        // Should not happen if criticalBlockingMoves.Count > 1, but as fallback:
                        Debug.WriteLine("Warning: AI V3: Could not choose between critical blocks, picking first.");
                        return criticalBlockingMoves[0];
                    }
                }
            }

            // --- Priority 3 (General Evaluation): No immediate win/loss threats ---
            // If the code reaches here, it means no immediate win for AI and no immediate win for opponent to block.
            // Proceed with evaluating all possible moves to find the best heuristic score.
            int bestHeuristicScore = int.MinValue;
            Move? bestHeuristicMove = null;

            foreach (var move in possibleMoves)
            {
                // Use the V2 evaluation function which includes neutralize bonus
                int currentScore = EvaluateCell_V2(currentBoard, move.Row, move.Col, player);

                // Update best heuristic move
                if (currentScore > bestHeuristicScore)
                {
                    bestHeuristicScore = currentScore;
                    bestHeuristicMove = move;
                }
                else if (currentScore == bestHeuristicScore && _random.NextDouble() < 0.3)
                {
                    bestHeuristicMove = move;
                }
            }

            // Fallback if no move evaluated positively (very rare unless board state is strange)
            if (!bestHeuristicMove.HasValue && possibleMoves.Count > 0)
            {
                Debug.WriteLine("Warning: AI V3: No best heuristic move found, picking random possible one.");
                bestHeuristicMove = possibleMoves[_random.Next(possibleMoves.Count)];
            }

            // Log the chosen heuristic move
            if (bestHeuristicMove.HasValue)
            {
                string scoreString = (bestHeuristicScore != int.MinValue) ? bestHeuristicScore.ToString() : "N/A";
                Debug.WriteLine($"AI V3: Chosen heuristic move: {bestHeuristicMove.Value} with score: {scoreString}");
            }
            else
            {
                Console.Error.WriteLine("AI V3: Could not determine any move.");
            }

            return bestHeuristicMove;

        } // End of FindBestMoveAggressive_V3
        */



        /*
        // --- Use SCORE_AGGRESSIVE_V2 dictionary as defined before ---
        // Including NEUTRALIZE_FOUR_BONUS and NEUTRALIZE_THREE_BONUS

        /// <summary>
        /// Calculates the score for a single line passing through the cell (row, col)
        /// where 'player' intends to place a piece. Uses SCORE_AGGRESSIVE_V2.
        /// **REVISED NEUTRALIZE LOGIC** to correctly identify double-blocking.
        /// IMPORTANT: Assumes player's piece IS tentatively placed on currentBoard[row, col] before calling.
        /// </summary>
        private int ScoreLine_V2_Fixed(int[,] currentBoard, int row, int col, int dr, int dc, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var currentScores = SCORE_AGGRESSIVE_V2;

            // --- Helper Function: Count and Score Sequence (Same as before) ---
            int CountAndScoreSequence(int r, int c, int p)
            {
                // ... (Exact same implementation as provided previously) ...
                int consecutive = 0; int openEnds = 0; bool startBlockedByOpponent = false; bool endBlockedByOpponent = false;
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
                int backward_r = r - dr; int backward_c = c - dc;
                while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p) { consecutive++; backward_r -= dr; backward_c -= dc; }
                if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) { } else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) { openEnds++; } else if (currentBoard[backward_r, backward_c] == otherPlayer) { startBlockedByOpponent = true; }
                backward_r = r + dr; backward_c = c + dc;
                while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p) { consecutive++; backward_r += dr; backward_c += dc; }
                if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) { } else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) { openEnds++; } else if (currentBoard[backward_r, backward_c] == otherPlayer) { endBlockedByOpponent = true; }
                consecutive++;
                if (consecutive >= WIN_LENGTH) { return (consecutive == WIN_LENGTH && !(startBlockedByOpponent && endBlockedByOpponent)) ? currentScores["FIVE"] : 0; }
                else if (consecutive == 4) { return (openEnds == 2) ? currentScores["FOUR_OPEN"] : ((openEnds == 1) ? currentScores["FOUR_CLOSED"] : 0); }
                else if (consecutive == 3) { return (openEnds == 2) ? currentScores["THREE_OPEN"] : ((openEnds == 1) ? currentScores["THREE_CLOSED"] : 0); }
                else if (consecutive == 2) { return (openEnds == 2) ? currentScores["TWO_OPEN"] : ((openEnds == 1) ? currentScores["TWO_CLOSED"] : 0); }
                else if (consecutive == 1) { return (openEnds == 2) ? currentScores["ONE_OPEN"] : ((openEnds == 1) ? currentScores["ONE_CLOSED"] : 0); }
                return 0;
            }
            // === End of Helper Function ===


            // === Main logic of scoreLine_V2_Fixed ===

            // 1. Calculate offensive score (for player)
            // Assumes player's piece is already placed at currentBoard[row, col]
            int playerScore = CountAndScoreSequence(row, col, player);

            // 2. Calculate defensive score (opponent's potential if they played here)
            currentBoard[row, col] = opponent; // Tentatively place opponent
            int opponentPotentialScore = CountAndScoreSequence(row, col, opponent);
            currentBoard[row, col] = player;   // Restore player's piece IMPORTANT!

            // 3. Calculate Neutralize Bonus (REVISED LOGIC)
            int neutralizeBonus = 0;
            // Check sequences adjacent to the player's move (P at [row, col])
            // Look for opponent sequences (O) that are now blocked at BOTH ends
            // by either P, another player piece (X), or the board boundary (B).

            // Check sequence ending before P: [Blocker] O...O [P]
            for (int len = 2; len <= 4; len++) // Check for neutralized lengths of 2, 3, 4 opponent pieces
            {
                int oppSequenceLength = len;
                int blocker_r = row - (oppSequenceLength + 1) * dr; // Position of potential Blocker
                int blocker_c = col - (oppSequenceLength + 1) * dc;

                // Check if the start position is actually blocked (by Player or Boundary)
                bool start_is_blocked = blocker_r < 0 || blocker_r >= BOARD_SIZE || blocker_c < 0 || blocker_c >= BOARD_SIZE || currentBoard[blocker_r, blocker_c] == player;

                if (start_is_blocked)
                {
                    bool sequence_matches = true;
                    // Check if the 'oppSequenceLength' pieces before P are opponent's pieces
                    for (int k = 1; k <= oppSequenceLength; k++)
                    {
                        int check_r = row - k * dr;
                        int check_c = col - k * dc;
                        // Need boundary check here too
                        if (check_r < 0 || check_r >= BOARD_SIZE || check_c < 0 || check_c >= BOARD_SIZE || currentBoard[check_r, check_c] != opponent)
                        {
                            sequence_matches = false;
                            break;
                        }
                    }

                    if (sequence_matches)
                    {
                        // Found opponent sequence double-blocked! Apply bonus.
                        if (oppSequenceLength == 4) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_FOUR_BONUS"]);
                        else if (oppSequenceLength == 3) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_THREE_BONUS"]);
                        // Add bonus for neutralizing 2 if desired
                    }
                }
            }

            // Check sequence starting after P: [P] O...O [Blocker]
            for (int len = 2; len <= 4; len++) // Check for neutralized lengths of 2, 3, 4 opponent pieces
            {
                int oppSequenceLength = len;
                int blocker_r = row + (oppSequenceLength + 1) * dr; // Position of potential Blocker
                int blocker_c = col + (oppSequenceLength + 1) * dc;

                // Check if the end position is actually blocked (by Player or Boundary)
                bool end_is_blocked = blocker_r < 0 || blocker_r >= BOARD_SIZE || blocker_c < 0 || blocker_c >= BOARD_SIZE || currentBoard[blocker_r, blocker_c] == player;

                if (end_is_blocked)
                {
                    bool sequence_matches = true;
                    // Check if the 'oppSequenceLength' pieces after P are opponent's pieces
                    for (int k = 1; k <= oppSequenceLength; k++)
                    {
                        int check_r = row + k * dr;
                        int check_c = col + k * dc;
                        if (check_r < 0 || check_r >= BOARD_SIZE || check_c < 0 || check_c >= BOARD_SIZE || currentBoard[check_r, check_c] != opponent)
                        {
                            sequence_matches = false;
                            break;
                        }
                    }
                    if (sequence_matches)
                    {
                        // Found opponent sequence double-blocked! Apply bonus.
                        if (oppSequenceLength == 4) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_FOUR_BONUS"]);
                        else if (oppSequenceLength == 3) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_THREE_BONUS"]);
                        // Add bonus for neutralizing 2 if desired
                    }
                }
            }


            // 4. Combine scores
            if (playerScore >= currentScores["FIVE"]) return currentScores["FIVE"];
            if (opponentPotentialScore >= currentScores["FIVE"]) return currentScores["BLOCK_FIVE"];

            int blockScore = 0; // Score for blocking just one end (as before)
            if (opponentPotentialScore >= currentScores["FOUR_OPEN"]) blockScore = currentScores["BLOCK_FOUR_OPEN"];
            else if (opponentPotentialScore >= currentScores["FOUR_CLOSED"]) blockScore = currentScores["BLOCK_FOUR_CLOSED"];
            else if (opponentPotentialScore >= currentScores["THREE_OPEN"]) blockScore = currentScores["BLOCK_THREE_OPEN"];
            else if (opponentPotentialScore >= currentScores["THREE_CLOSED"]) blockScore = currentScores["BLOCK_THREE_CLOSED"];
            else if (opponentPotentialScore >= currentScores["TWO_OPEN"]) blockScore = currentScores["BLOCK_TWO_OPEN"];
            else if (opponentPotentialScore >= currentScores["TWO_CLOSED"]) blockScore = currentScores["BLOCK_TWO_CLOSED"];

            // Final score = Offensive Score + Single-End Block Score + Double-Block Bonus
            return playerScore + blockScore + neutralizeBonus;
        } // End of scoreLine_V2_Fixed
        */

        /*
        // --- Update EvaluateCell to use the fixed scoreLine ---
        /// <summary>
        /// Calculates the total score for placing 'player's piece at (row, col).
        /// IMPORTANT: Calls the fixed scoreLine_V2_Fixed.
        /// </summary>
        private int EvaluateCell_V2_Fixed(int[,] currentBoard, int row, int col, int player)
        {
            if (currentBoard[row, col] != EMPTY_CELL)
            {
                return int.MinValue;
            }

            int totalScore = 0;
            (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };

            currentBoard[row, col] = player; // Tentatively place piece

            foreach (var dir in directions)
            {
                // *** CALL FIXED V2 of scoreLine ***
                totalScore += ScoreLine_V2_Fixed(currentBoard, row, col, dir.dr, dir.dc, player);
            }

            currentBoard[row, col] = EMPTY_CELL; // Revert board

            // Optional: Add positional bonus
            double centerBonus = (
                (BOARD_SIZE / 2.0 - Math.Abs(row - (BOARD_SIZE - 1.0) / 2.0)) +
                (BOARD_SIZE / 2.0 - Math.Abs(col - (BOARD_SIZE - 1.0) / 2.0))
            );
            totalScore += (int)(centerBonus * 0.5);

            return totalScore;
        }
        */

        /*
        // --- Update findBestMove to use the fixed EvaluateCell ---
        /// <summary>
        /// Finds the best move using Aggressive V2 scoring and the FIXED evaluation.
        /// Uses EvaluateCell_V2_Fixed.
        /// </summary>
        public Move? FindBestMoveAggressive_V3_Fixed(int[,] currentBoard, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var _random = new Random(); // Local random instance

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0) { /* ... no moves ... * return null; }
            // ... (Handle first move) ...

            int[,] tempBoard = (int[,])currentBoard.Clone(); // Clone for win/loss checks

            // Priority 1: Win immediately
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, tempBoard)) { /*... return move; }
                tempBoard[move.Row, move.Col] = EMPTY_CELL;
            }

            // Priority 2: Find ALL moves that block an immediate opponent win (FIVE)
            List<Move> criticalBlockingMoves = new List<Move>();
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = opponent;
                if (CheckWin(move.Row, move.Col, opponent, tempBoard))
                {
                    criticalBlockingMoves.Add(move);
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL;
            }

            // Decision based on Critical Blocking Moves
            if (criticalBlockingMoves.Count > 0)
            {
                if (criticalBlockingMoves.Count == 1) { /*... return criticalBlockingMoves[0]; }
                else
                {
                    // Evaluate ONLY these critical blocking moves using the FIXED evaluation
                    int bestBlockScore = int.MinValue;
                    Move? bestBlockMove = null;
                    foreach (var blockingMove in criticalBlockingMoves)
                    {
                        // *** CALL FIXED V2 of EvaluateCell ***
                        int currentBlockScore = EvaluateCell_V2_Fixed(currentBoard, blockingMove.Row, blockingMove.Col, player);
                        // Debug.WriteLine($"   - Evaluating critical block {blockingMove}: Score = {currentBlockScore}"); // Debug
                        if (currentBlockScore > bestBlockScore)
                        {
                            bestBlockScore = currentBlockScore;
                            bestBlockMove = blockingMove;
                        }
                        else if (currentBlockScore == bestBlockScore && _random.NextDouble() < 0.5)
                        {
                            bestBlockMove = blockingMove;
                        }
                    }
                    if (bestBlockMove.HasValue) { /*... return bestBlockMove.Value; }
                    else { /*... return criticalBlockingMoves[0]; } // Fallback
                }
            }

            // Priority 3: General Evaluation (No immediate threats)
            int bestHeuristicScore = int.MinValue;
            Move? bestHeuristicMove = null;
            foreach (var move in possibleMoves)
            {
                // *** CALL FIXED V2 of EvaluateCell ***
                int currentScore = EvaluateCell_V2_Fixed(currentBoard, move.Row, move.Col, player);
                // ... (Update best heuristic move logic) ...
                if (currentScore > bestHeuristicScore)
                {
                    bestHeuristicScore = currentScore;
                    bestHeuristicMove = move;
                }
                else if (currentScore == bestHeuristicScore && _random.NextDouble() < 0.3)
                {
                    bestHeuristicMove = move;
                }
            }

            // ... (Fallback logic and return bestHeuristicMove) ...
            if (!bestHeuristicMove.HasValue && possibleMoves.Count > 0)
            {
                Debug.WriteLine("Warning: AI V3 Fixed: No best heuristic move found, picking random possible one.");
                bestHeuristicMove = possibleMoves[_random.Next(possibleMoves.Count)];
            }
            if (bestHeuristicMove.HasValue)
            {
                string scoreString = (bestHeuristicScore != int.MinValue) ? bestHeuristicScore.ToString() : "N/A";
                Debug.WriteLine($"AI V3 Fixed: Chosen heuristic move: {bestHeuristicMove.Value} with score: {scoreString}");
            }
            else { Console.Error.WriteLine("AI V3 Fixed: Could not determine any move."); }
            return bestHeuristicMove;

        } // End of FindBestMoveAggressive_V3_Fixed

        // Include other necessary methods here:
        // CheckWin(...)
        // GetPossibleMoves(...)
        */


        /*
        // --- Use SCORE_AGGRESSIVE_V2 dictionary as defined before ---
        /// <summary>
        /// Calculates the score for a single line passing through the cell (row, col)
        /// where 'player' intends to place a piece. Uses SCORE_AGGRESSIVE_V2.
        /// **FINAL REVISED NEUTRALIZE LOGIC** to correctly identify double-blocking.
        /// IMPORTANT: Assumes player's piece IS tentatively placed on currentBoard[row, col] before calling.
        /// </summary>
        private int ScoreLine_V2_Final(int[,] currentBoard, int row, int col, int dr, int dc, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var currentScores = SCORE_AGGRESSIVE_V2;

            // === Helper Function: Count and Score Sequence (Same as before) ===
            int CountAndScoreSequence(int r, int c, int p)
            {
                // ... (Exact same implementation as provided previously) ...
                int consecutive = 0; int openEnds = 0; bool startBlockedByOpponent = false; bool endBlockedByOpponent = false;
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
                int backward_r = r - dr; int backward_c = c - dc;

                // check blocked both by Boundary or Opponent

                // Count backwards
                while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p) 
                { 
                    consecutive++; 
                    backward_r -= dr; backward_c -= dc; 
                }
                if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) { } 
                else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) 
                { 
                    openEnds++; 
                } else if (currentBoard[backward_r, backward_c] == otherPlayer) 
                { 
                    startBlockedByOpponent = true;
                }
                
                // Count forwards
                backward_r = r + dr; backward_c = c + dc;
                while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p) 
                { 
                    consecutive++; 
                    backward_r += dr; backward_c += dc; 
                }
                if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) { } 
                else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) 
                { 
                    openEnds++; 
                } 
                else if (currentBoard[backward_r, backward_c] == otherPlayer) 
                { 
                    endBlockedByOpponent = true;
                }
                consecutive++;

                if (consecutive >= WIN_LENGTH) 
                { 
                    return (consecutive == WIN_LENGTH && !(startBlockedByOpponent && endBlockedByOpponent)) ? currentScores["FIVE"] : 0; 
                }
                else if (consecutive == 4) 
                { 
                    return (openEnds == 2) ? currentScores["FOUR_OPEN"] : ((openEnds == 1) ? currentScores["FOUR_CLOSED"] : 0); 
                }
                else if (consecutive == 3) 
                { 
                    return (openEnds == 2) ? currentScores["THREE_OPEN"] : ((openEnds == 1) ? currentScores["THREE_CLOSED"] : 0); 
                }
                else if (consecutive == 2) 
                { 
                    return (openEnds == 2) ? currentScores["TWO_OPEN"] : ((openEnds == 1) ? currentScores["TWO_CLOSED"] : 0); 
                }
                else if (consecutive == 1) 
                { 
                    return (openEnds == 2) ? currentScores["ONE_OPEN"] : ((openEnds == 1) ? currentScores["ONE_CLOSED"] : 0); 
                }
                return 0;
            }
            // === End of Helper Function ===


            // === Main logic of scoreLine_V2_Final ===

            // 1. Calculate offensive score (for player)
            int playerScore = CountAndScoreSequence(row, col, player);

            // 2. Calculate defensive score (opponent's potential if they played here)
            currentBoard[row, col] = opponent; // Tentatively place opponent
            int opponentPotentialScore = CountAndScoreSequence(row, col, opponent);
            currentBoard[row, col] = player;   // Restore player's piece IMPORTANT!

            // 3. Calculate Neutralize Bonus (FINAL REVISED LOGIC)
            int neutralizeBonus = 0;
            // --- Check if placing player piece at (row, col) double-blocks opponent sequence ---

            // Check sequence ending before P: [Blocker] O...O [P]
            int len_before = 0;
            int r_before = row - dr;
            int c_before = col - dc;
            while (r_before >= 0 && r_before < BOARD_SIZE && c_before >= 0 && c_before < BOARD_SIZE && currentBoard[r_before, c_before] == opponent)
            {
                len_before++;
                r_before -= dr;
                c_before -= dc;
            }
            // Check the cell *before* the opponent sequence found
            if (len_before >= 2 && len_before <= 4)
            { // Only consider neutralizing 2, 3 or 4
                bool start_is_blocked = r_before < 0 || r_before >= BOARD_SIZE || c_before < 0 || c_before >= BOARD_SIZE || currentBoard[r_before, c_before] == player;
                if (start_is_blocked)
                { // The other end is blocked by player or boundary!
                    if (len_before == 4) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_FOUR_BONUS"]);
                    else if (len_before == 3) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_THREE_BONUS"]);
                    // Add bonus for 2 if needed
                }
            }

            // Check sequence starting after P: [P] O...O [Blocker]
            int len_after = 0;
            int r_after = row + dr;
            int c_after = col + dc;
            while (r_after >= 0 && r_after < BOARD_SIZE && c_after >= 0 && c_after < BOARD_SIZE && currentBoard[r_after, c_after] == opponent)
            {
                len_after++;
                r_after += dr;
                c_after += dc;
            }
            // Check the cell *after* the opponent sequence found
            if (len_after >= 2 && len_after <= 4)
            { // Only consider neutralizing 2, 3 or 4
                bool end_is_blocked = r_after < 0 || r_after >= BOARD_SIZE || c_after < 0 || c_after >= BOARD_SIZE || currentBoard[r_after, c_after] == player;
                if (end_is_blocked)
                { // The other end is blocked by player or boundary!
                    if (len_after == 4) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_FOUR_BONUS"]);
                    else if (len_after == 3) neutralizeBonus = Math.Max(neutralizeBonus, currentScores["NEUTRALIZE_THREE_BONUS"]);
                    // Add bonus for 2 if needed
                }
            }

            // 4. Combine scores
            if (playerScore >= currentScores["FIVE"]) return currentScores["FIVE"];
            if (opponentPotentialScore >= currentScores["FIVE"]) return currentScores["BLOCK_FIVE"];

            int blockScore = 0; // Score for blocking just one end (as before)
            if (opponentPotentialScore >= currentScores["FOUR_OPEN"]) blockScore = currentScores["BLOCK_FOUR_OPEN"];
            else if (opponentPotentialScore >= currentScores["FOUR_CLOSED"]) blockScore = currentScores["BLOCK_FOUR_CLOSED"];
            else if (opponentPotentialScore >= currentScores["THREE_OPEN"]) blockScore = currentScores["BLOCK_THREE_OPEN"];
            // ... other block scores ...
            else if (opponentPotentialScore >= currentScores["THREE_CLOSED"]) blockScore = currentScores["BLOCK_THREE_CLOSED"];
            else if (opponentPotentialScore >= currentScores["TWO_OPEN"]) blockScore = currentScores["BLOCK_TWO_OPEN"];
            else if (opponentPotentialScore >= currentScores["TWO_CLOSED"]) blockScore = currentScores["BLOCK_TWO_CLOSED"];


            // Final score = Offensive Score + Single-End Block Score + Double-Block Bonus
            return playerScore + blockScore + neutralizeBonus;
        } // End of scoreLine_V2_Final
        */

        /*
        // --- Update EvaluateCell to use the FINAL scoreLine ---
        private int EvaluateCell_V2_Final(int[,] currentBoard, int row, int col, int player)
        {
            if (currentBoard[row, col] != EMPTY_CELL) return int.MinValue;
            int totalScore = 0;
            (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };
            currentBoard[row, col] = player; // Tentatively place piece
            foreach (var dir in directions)
            {
                // *** CALL FINAL V2 of scoreLine ***
                totalScore += ScoreLine_V2_Final(currentBoard, row, col, dir.dr, dir.dc, player);
            }
            currentBoard[row, col] = EMPTY_CELL; // Revert board
                                                 // Add center bonus
            double centerBonus = ((BOARD_SIZE / 2.0 - Math.Abs(row - (BOARD_SIZE - 1.0) / 2.0)) + (BOARD_SIZE / 2.0 - Math.Abs(col - (BOARD_SIZE - 1.0) / 2.0)));
            totalScore += (int)(centerBonus * 0.5);
            return totalScore;
        }
        */

        /*
        // Include other necessary methods: CheckWin, GetPossibleMoves

        // --- Update findBestMove to use the FINAL EvaluateCell ---
        /// <summary>
        /// Finds the best move using Aggressive V2 scoring and the FIXED evaluation.
        /// Uses EvaluateCell_V2_Final.
        /// </summary>
        public Move? FindBestMoveAggressive_V3_Final(int[,] currentBoard, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var _random = new Random(); // Local random instance

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0) { /* ... no moves ...  return null; }
            // ... (Handle first move) ...

            int[,] tempBoard = (int[,])currentBoard.Clone(); // Clone for win/loss checks

            // Priority 1: Win immediately
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, tempBoard)) { /*... return move; }
                tempBoard[move.Row, move.Col] = EMPTY_CELL;
            }

            // Priority 2: Find ALL moves that block an immediate opponent win (FIVE)
            List<Move> criticalBlockingMoves = new List<Move>();
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = opponent;
                if (CheckWin(move.Row, move.Col, opponent, tempBoard))
                {
                    criticalBlockingMoves.Add(move);
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL;
            }

            // Decision based on Critical Blocking Moves
            if (criticalBlockingMoves.Count > 0)
            {
                if (criticalBlockingMoves.Count == 1) { /*... return criticalBlockingMoves[0]; }
                else
                {
                    // Evaluate ONLY these critical blocking moves using the FINAL evaluation
                    int bestBlockScore = int.MinValue;
                    Move? bestBlockMove = null;
                    foreach (var blockingMove in criticalBlockingMoves)
                    {
                        // *** CALL FINAL V2 of EvaluateCell ***
                        int currentBlockScore = EvaluateCell_V2_Final(currentBoard, blockingMove.Row, blockingMove.Col, player);
                        // Debug.WriteLine($"   - Evaluating critical block {blockingMove}: Score = {currentBlockScore}"); // Debug
                        if (currentBlockScore > bestBlockScore)
                        {
                            bestBlockScore = currentBlockScore;
                            bestBlockMove = blockingMove;
                        }
                        else if (currentBlockScore == bestBlockScore && _random.NextDouble() < 0.5)
                        {
                            bestBlockMove = blockingMove;
                        }
                    }
                    if (bestBlockMove.HasValue) { /*...* return bestBlockMove.Value; }
                    else { /*...* return criticalBlockingMoves[0]; } // Fallback
                }
            }

            // Priority 3: General Evaluation (No immediate threats)
            int bestHeuristicScore = int.MinValue;
            Move? bestHeuristicMove = null;
            foreach (var move in possibleMoves)
            {
                // *** CALL FINAL V2 of EvaluateCell ***
                int currentScore = EvaluateCell_V2_Final(currentBoard, move.Row, move.Col, player);
                // ... (Update best heuristic move logic) ...
                if (currentScore > bestHeuristicScore)
                {
                    bestHeuristicScore = currentScore;
                    bestHeuristicMove = move;
                }
                else if (currentScore == bestHeuristicScore && _random.NextDouble() < 0.3)
                {
                    bestHeuristicMove = move;
                }
            }

            // ... (Fallback logic and return bestHeuristicMove) ...
            if (!bestHeuristicMove.HasValue && possibleMoves.Count > 0)
            {
                Debug.WriteLine("Warning: AI V3 Final: No best heuristic move found, picking random possible one.");
                bestHeuristicMove = possibleMoves[_random.Next(possibleMoves.Count)];
            }
            if (bestHeuristicMove.HasValue)
            {
                string scoreString = (bestHeuristicScore != int.MinValue) ? bestHeuristicScore.ToString() : "N/A";
                Debug.WriteLine($"AI V3 Final: Chosen heuristic move: {bestHeuristicMove.Value} with score: {scoreString}");
            }
            else { Console.Error.WriteLine("AI V3 Final: Could not determine any move."); }
            return bestHeuristicMove;

        } // End of FindBestMoveAggressive_V3_Final
        */


        private bool IsOutOfCaroBoard(int row, int column)
        {
            return (row < 0 || column < 0 || row >= BOARD_SIZE || column >= BOARD_SIZE);
        }

        private bool IsInCaroBoard(int row, int column)
        {
            return (row >= 0 && column >= 0 && row < BOARD_SIZE && column < BOARD_SIZE);
        }

        // --- Check Block both side by Boundary or Opponent ---
        /// <summary>
        /// Calculates score for a line. Uses SCORE_AGGRESSIVE_V2.
        /// Check Block both side by Boundary or Opponent
        /// </summary>
        private bool BlockedBothByBoundaryOrOpponent(int[,] currentBoard, int row, int col, int dr, int dc, int player, int opponent)
        {
            // check blocked both side by Boundary or Opponent

            if ((row == 5 && col == 2 && dr == 1))
            {
                Debug.WriteLine("Warning: BlockedBothByBoundaryOrOpponent: STOP HERE for DEBUGGING... Row:{0}, Column:{1}", row, col);
            }

            int count = 0; // max = WIN_LENGTH

            int backward_r = row - dr; int backward_c = col - dc;
            int count_backward = 1; // start from 1
            bool bound_backward = IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't count

            int forward_r = row + dr; int forward_c = col + dc;
            int count_forward = 1; // start from 1
            bool bound_forward = IsOutOfCaroBoard(forward_r, forward_c); // forward boundary is out of the board, don't count

            // Count backwards & forwards
            while (count <= WIN_LENGTH && (!bound_backward || !bound_forward))
            {
                // Check backwards
                bound_backward = bound_backward || IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't count
                if (bound_backward || currentBoard[backward_r, backward_c] == opponent) // blocked by Boundary or Opponent, dont need check cell value
                    bound_backward = true; // dont need check backward anymore
                else 
                {
                    // increase the counter for backward
                    count_backward++;
                    backward_r -= dr; backward_c -= dc;
                }


                // Check forwards
                bound_forward = bound_forward || IsOutOfCaroBoard(forward_r, forward_c); ; // forward boundary is out of the board, don't count
                if (bound_forward || currentBoard[forward_r, forward_c] == opponent) // blocked by Boundary or Opponent, dont need check cell value
                    bound_forward = true; // dont need check forward anymore
                else
                {
                    // increase the counter for forward
                    count_forward++;
                    forward_r += dr; forward_c += dc;
                }

                // increase the counter
                count++;
            }

            // blocked both side by Boundary or Opponent
            if (bound_backward && bound_forward && (count_backward + count_forward <= WIN_LENGTH + 1)) // WIN_LENGTH + 1, because boundary counts start from 1;
            {
                //Debug.WriteLine("BlockedBothByBoundaryOrOpponent... Row:{0}, Column:{1}, dr:{2}, dc:{3}, count_backward:{6}, count_forward:{7}, player:{4}, opponent:{5}", row, col, dr, dc, player, opponent, count_backward, count_forward);
                return true;
            }

            return false;
        }


        // --- ScoreLine V3 (No Early BLOCK_FIVE Return) ---
        /// <summary>
        /// Calculates score for a line. Uses SCORE_AGGRESSIVE_V2.
        /// **REMOVED** the early return for BLOCK_FIVE. Includes Neutralize Bonus logic.
        /// Assumes player's piece IS tentatively placed before calling.
        /// </summary>
        private int ScoreLine_V3_Final(int[,] currentBoard, int row, int col, int dr, int dc, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var currentScores = SCORE_AGGRESSIVE_V2; // Use V2 scores with Neutralize bonus

            // Helper: CountAndScoreSequence (Same as before - calculates score for a single player's sequence)
            int CountAndScoreSequence(int r, int c, int p)
            {
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
                // Count backwards
                int temp_r = r - dr; int temp_c = c - dc;
                int consecutive = 0; int openEnds = 0; bool startBlockedByOpponent = false; bool endBlockedByOpponent = false;
                temp_r = r - dr; temp_c = c - dc;
                consecutive = 0;
                while (temp_r >= 0 && temp_r < BOARD_SIZE && temp_c >= 0 && temp_c < BOARD_SIZE && currentBoard[temp_r, temp_c] == p) 
                { 
                    consecutive++; 
                    temp_r -= dr; temp_c -= dc; 
                }
                if (temp_r < 0 || temp_r >= BOARD_SIZE || temp_c < 0 || temp_c >= BOARD_SIZE) { } 
                else if (currentBoard[temp_r, temp_c] == EMPTY_CELL) 
                { 
                    openEnds++; 
                } 
                else if (currentBoard[temp_r, temp_c] == otherPlayer) 
                {
                    startBlockedByOpponent = true; 
                }

                // Count forwards
                temp_r = r + dr; temp_c = c + dc;
                while (temp_r >= 0 && temp_r < BOARD_SIZE && temp_c >= 0 && temp_c < BOARD_SIZE && currentBoard[temp_r, temp_c] == p) 
                { 
                    consecutive++; 
                    temp_r += dr; temp_c += dc; 
                }

                if (temp_r < 0 || temp_r >= BOARD_SIZE || temp_c < 0 || temp_c >= BOARD_SIZE) { } 
                else if (currentBoard[temp_r, temp_c] == EMPTY_CELL) 
                { 
                    openEnds++; 
                } 
                else if (currentBoard[temp_r, temp_c] == otherPlayer) 
                { 
                    endBlockedByOpponent = true; 
                }
                consecutive++;

                if (consecutive >= WIN_LENGTH) 
                { 
                    return (consecutive == WIN_LENGTH && !(startBlockedByOpponent && endBlockedByOpponent)) ? currentScores["FIVE"] : 0; 
                }
                else if (consecutive == 4) 
                { 
                    return (openEnds == 2) ? currentScores["FOUR_OPEN"] : ((openEnds == 1) ? currentScores["FOUR_CLOSED"] : 0); 
                }
                else if (consecutive == 3) 
                { 
                    return (openEnds == 2) ? currentScores["THREE_OPEN"] : ((openEnds == 1) ? currentScores["THREE_CLOSED"] : 0); 
                }
                else if (consecutive == 2) 
                { 
                    return (openEnds == 2) ? currentScores["TWO_OPEN"] : ((openEnds == 1) ? currentScores["TWO_CLOSED"] : 0); 
                }
                else if (consecutive == 1) 
                { 
                    return (openEnds == 2) ? currentScores["ONE_OPEN"] : ((openEnds == 1) ? currentScores["ONE_CLOSED"] : 0); 
                }
                return 0;
            }

            bool showEvaluateScore = StopHereForDebugging(row, col, player);

            int playerScore = 0;
            int neutralizeBonus = 0;
            if (!BlockedBothByBoundaryOrOpponent(currentBoard, row, col, dr, dc, player, opponent))
            {
                // 1. Calculate offensive score (for player)
                playerScore = CountAndScoreSequence(row, col, player);
                // Check for immediate player win - return highest score if found
                if (playerScore >= currentScores["FIVE"])
                    return currentScores["FIVE"];

                // 3. Calculate Neutralize Bonus (using the corrected logic)
                //neutralizeBonus = CalculateNeutralizeBonus_V2(currentBoard, row, col, dr, dc, player, opponent, currentScores);
            }

            // 2. Calculate opponent's potential score if they played here
            currentBoard[row, col] = opponent;
            int opponentPotentialScore = 0;
            if (!BlockedBothByBoundaryOrOpponent(currentBoard, row, col, dr, dc, opponent, player))
                opponentPotentialScore = CountAndScoreSequence(row, col, opponent);
            currentBoard[row, col] = player; // Restore player's piece

            // 4. Calculate Block Score (based on opponent's potential, EXCLUDING FIVE)
            int blockScore = 0;
            // *** IMPORTANT: DO NOT check for >= FIVE here ***
            if (opponentPotentialScore >= currentScores["FOUR_OPEN"]) 
                blockScore = currentScores["BLOCK_FOUR_OPEN"];
            else if (opponentPotentialScore >= currentScores["FOUR_CLOSED"]) 
                blockScore = currentScores["BLOCK_FOUR_CLOSED"];
            else if (opponentPotentialScore >= currentScores["THREE_OPEN"]) 
                blockScore = currentScores["BLOCK_THREE_OPEN"];
            else if (opponentPotentialScore >= currentScores["THREE_CLOSED"]) 
                blockScore = currentScores["BLOCK_THREE_CLOSED"];
            else if (opponentPotentialScore >= currentScores["TWO_OPEN"]) 
                blockScore = currentScores["BLOCK_TWO_OPEN"];
            else if (opponentPotentialScore >= currentScores["TWO_CLOSED"]) 
                blockScore = currentScores["BLOCK_TWO_CLOSED"];

            // 5. Combine scores: Offensive + Regular Blocking (non-FIVE) + Neutralization Bonus
            return playerScore + blockScore + neutralizeBonus;
        }

        /*
        // --- Helper: CalculateNeutralizeBonus (Corrected Version) ---
        /// <summary>
        /// Calculates the neutralization bonus achieved by placing 'player' at (r_center, c_center).
        /// Checks if this move double-blocks any adjacent opponent sequences along the given direction.
        /// </summary>
        private int CalculateNeutralizeBonus(int[,] board, int r_center, int c_center, int dr, int dc, int player, int opponent, Dictionary<string, int> scores)
        {
            int max_bonus = 0;

            // Check sequence ending before P: [Blocker] O...O [P=(r_center,c_center)]
            int len_before = 0;
            int r_check = r_center - dr;
            int c_check = c_center - dc;
            while (r_check >= 0 && r_check < BOARD_SIZE && c_check >= 0 && c_check < BOARD_SIZE && board[r_check, c_check] == opponent)
            {
                len_before++; r_check -= dr; c_check -= dc;
            }
            if (len_before >= 2 && len_before <= 4)
            { // Check relevant lengths (2, 3, 4 opponent pieces)
              // Check the cell *before* the sequence
                bool start_is_blocked = r_check < 0 || r_check >= BOARD_SIZE || c_check < 0 || c_check >= BOARD_SIZE || board[r_check, c_check] == player;
                if (start_is_blocked)
                { // Double blocked!
                    if (len_before == 4) max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_FOUR_BONUS"]);
                    else if (len_before == 3) max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_THREE_BONUS"]);
                }
            }

            // Check sequence starting after P: [P=(r_center,c_center)] O...O [Blocker]
            int len_after = 0;
            r_check = r_center + dr;
            c_check = c_center + dc;
            while (r_check >= 0 && r_check < BOARD_SIZE && c_check >= 0 && c_check < BOARD_SIZE && board[r_check, c_check] == opponent)
            {
                len_after++; r_check += dr; c_check += dc;
            }
            if (len_after >= 2 && len_after <= 4)
            { // Check relevant lengths
              // Check the cell *after* the sequence
                bool end_is_blocked = r_check < 0 || r_check >= BOARD_SIZE || c_check < 0 || c_check >= BOARD_SIZE || board[r_check, c_check] == player;
                if (end_is_blocked)
                { // Double blocked!
                    if (len_after == 4) max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_FOUR_BONUS"]);
                    else if (len_after == 3) max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_THREE_BONUS"]);
                }
            }
            return max_bonus;
        }
        */


        // --- Updated EvaluateCell (V3 - Calls Corrected ScoreLine) ---
        /// <summary>
        /// Calculates the total score for placing 'player's piece at (row, col).
        /// Calls the corrected ScoreLine_V3_Final.
        /// </summary>
        private int EvaluateCell_V3_Final(int[,] currentBoard, int row, int col, int player)
        {
            if (currentBoard[row, col] != EMPTY_CELL) return int.MinValue;
            int totalScore = 0;
            (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };
            currentBoard[row, col] = player; // Tentatively place piece
            foreach (var dir in directions)
            {
                // *** CALL FINAL V3 of scoreLine (No Early Return version) ***
                totalScore += ScoreLine_V3_Final(currentBoard, row, col, dir.dr, dir.dc, player);
            }
            currentBoard[row, col] = EMPTY_CELL; // Revert board
            // Add center bonus
            double centerBonus = ((BOARD_SIZE / 2.0 - Math.Abs(row - (BOARD_SIZE - 1.0) / 2.0)) + (BOARD_SIZE / 2.0 - Math.Abs(col - (BOARD_SIZE - 1.0) / 2.0)));
            totalScore += (int)(centerBonus * 0.5);
            return totalScore;
        }

        /*

        // --- Final findBestMove (V4 - Uses Corrected EvaluateCell) ---
        /// <summary>
        /// Finds the best move using Aggressive V2 scoring and the FINAL evaluation logic.
        /// Correctly handles comparison of critical blocking moves.
        /// </summary>
        public Move? FindBestMoveAggressive_V4_Final(int[,] currentBoard, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var _random = new Random(); // Local instance

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0) { Console.Error.WriteLine("AI V4 Final: No possible moves left!"); return null; }

            // Handle first move
            int moveCountOnBoard = 0;
            for (int r = 0; r < BOARD_SIZE; ++r) for (int c = 0; c < BOARD_SIZE; ++c) if (currentBoard[r, c] != EMPTY_CELL) moveCountOnBoard++;
            if (moveCountOnBoard <= 1) {  ... Center logic ...  }

            int[,] tempBoard = (int[,])currentBoard.Clone(); // Clone ONLY for win/loss checks

            // Priority 1: AI Win immediately
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, tempBoard))
                {
                    Debug.WriteLine($"AI V4 Final: Found winning move at {move}");
                    return move;
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // Priority 2: Find ALL moves that block an immediate opponent win (FIVE)
            List<Move> criticalBlockingMoves = new List<Move>();
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = opponent;
                if (CheckWin(move.Row, move.Col, opponent, tempBoard))
                {
                    criticalBlockingMoves.Add(move);
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // Decision based on Critical Blocking Moves
            if (criticalBlockingMoves.Count > 0)
            {
                Debug.WriteLine($"AI V4 Final: Found {criticalBlockingMoves.Count} critical blocking move(s). Evaluating them...");
                if (criticalBlockingMoves.Count == 1)
                {
                    Debug.WriteLine($"AI V4 Final: Executing the only critical block at {criticalBlockingMoves[0]}");
                    return criticalBlockingMoves[0];
                }
                else
                {
                    // Evaluate ONLY critical blocks using the FINAL evaluation function
                    int bestBlockScore = int.MinValue;
                    Move? bestBlockMove = null;
                    foreach (var blockingMove in criticalBlockingMoves)
                    {
                        // *** CALL FINAL V3 EvaluateCell (which calls FINAL V3 ScoreLine) ***
                        int currentBlockScore = EvaluateCell_V3_Final(currentBoard, blockingMove.Row, blockingMove.Col, player);
                        Debug.WriteLine($"   - Evaluating critical block {blockingMove}: Score = {currentBlockScore}"); // Debug

                        if (currentBlockScore > bestBlockScore)
                        {
                            bestBlockScore = currentBlockScore;
                            bestBlockMove = blockingMove;
                        }
                        else if (currentBlockScore == bestBlockScore && _random.NextDouble() < 0.5)
                        {
                            bestBlockMove = blockingMove; // Tie-break randomly
                        }
                    }
                    if (bestBlockMove.HasValue)
                    {
                        Debug.WriteLine($"AI V4 Final: Chose best critical block {bestBlockMove.Value} with score {bestBlockScore}");
                        return bestBlockMove.Value;
                    }
                    else
                    {
                        Debug.WriteLine("Warning: AI V4 Final: Could not choose between critical blocks, picking first.");
                        return criticalBlockingMoves[0]; // Fallback
                    }
                }
            }

            // Priority 3: General Evaluation (No immediate threats)
            int bestHeuristicScore = int.MinValue;
            Move? bestHeuristicMove = null;
            foreach (var move in possibleMoves)
            {
                // *** CALL FINAL V3 EvaluateCell ***
                int currentScore = EvaluateCell_V3_Final(currentBoard, move.Row, move.Col, player);
                // ... (Update best heuristic move logic as before) ...
                if (currentScore > bestHeuristicScore)
                {
                    bestHeuristicScore = currentScore;
                    bestHeuristicMove = move;
                }
                else if (currentScore == bestHeuristicScore && _random.NextDouble() < 0.3)
                {
                    bestHeuristicMove = move;
                }
            }

            // ... (Fallback logic and return bestHeuristicMove) ...
            if (!bestHeuristicMove.HasValue && possibleMoves.Count > 0)
            {
                Debug.WriteLine("Warning: AI V4 Final: No best heuristic move found, picking random possible one.");
                bestHeuristicMove = possibleMoves[_random.Next(possibleMoves.Count)];
            }
            if (bestHeuristicMove.HasValue)
            {
                string scoreString = (bestHeuristicScore != int.MinValue) ? bestHeuristicScore.ToString() : "N/A";
                Debug.WriteLine($"AI V4 Final: Chosen heuristic move: {bestHeuristicMove.Value} with score: {scoreString}");
            }
            else { Console.Error.WriteLine("AI V4 Final: Could not determine any move."); }
            return bestHeuristicMove;

        } // End of FindBestMoveAggressive_V4_Final
        */


        // Include the Corrected CalculateNeutralizeBonus_V2 helper function here
        /// <summary>
        /// Calculates the neutralization bonus achieved by placing 'player' at (r_center, c_center).
        /// Checks if this move double-blocks any adjacent opponent sequences along the given direction.
        /// </summary>
        private int CalculateNeutralizeBonus_V2(int[,] board, int r_center, int c_center, int dr, int dc, int player, int opponent, Dictionary<string, int> scores)
        {
            int max_bonus = 0;
            // Check sequence ending before P: [Blocker] O...O [P=(r_center,c_center)]
            int len_before = 0;
            int r_check = r_center - dr; int c_check = c_center - dc;
            while (r_check >= 0 && r_check < BOARD_SIZE && c_check >= 0 && c_check < BOARD_SIZE && board[r_check, c_check] == opponent) 
            { 
                len_before++; r_check -= dr; c_check -= dc; 
            }

            if (len_before >= 2 && len_before <= 4)
            {
                bool start_is_blocked = IsOutOfCaroBoard(r_check, c_check) || board[r_check, c_check] == player;
                if (start_is_blocked)
                {
                    if (len_before == 4) 
                        max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_FOUR_BONUS"]); 
                    else if (len_before == 3) 
                        max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_THREE_BONUS"]);
                }
            }
            // Check sequence starting after P: [P=(r_center,c_center)] O...O [Blocker]
            int len_after = 0;
            r_check = r_center + dr; c_check = c_center + dc;
            while (r_check >= 0 && r_check < BOARD_SIZE && c_check >= 0 && c_check < BOARD_SIZE && board[r_check, c_check] == opponent) 
            { 
                len_after++; r_check += dr; c_check += dc; 
            }

            if (len_after >= 2 && len_after <= 4)
            {
                bool end_is_blocked = IsOutOfCaroBoard(r_check, c_check) || board[r_check, c_check] == player;
                if (end_is_blocked)
                {
                    if (len_after == 4) 
                        max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_FOUR_BONUS"]); 
                    else if (len_after == 3) 
                        max_bonus = Math.Max(max_bonus, scores["NEUTRALIZE_THREE_BONUS"]);
                }
            }
            return max_bonus;
        }

        /*
        /// <summary>
        /// Determines the score value of the highest threat from the opponent
        /// that would be blocked if the 'player' were to move at 'blockMove'.
        /// It simulates the opponent playing at 'blockMove' and finds the best pattern created.
        /// </summary>
        /// <param name="currentBoard">Current board state.</param>
        /// <param name="blockMove">The move the 'player' is considering making to block.</param>
        /// <param name="player">The AI player (who is blocking).</param>
        /// <returns>The score corresponding to the severity of the threat blocked (e.g., BLOCK_FIVE, BLOCK_FOUR_OPEN).</returns>
        private int GetBlockedThreatScore(int[,] currentBoard, Move blockMove, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var scores = SCORE_AGGRESSIVE_V2; // Use the latest score table

            // Temporarily place the OPPONENT's piece at the blocking spot
            if (currentBoard[blockMove.Row, blockMove.Col] == EMPTY_CELL) // Ensure it's empty before simulation
            {
                currentBoard[blockMove.Row, blockMove.Col] = opponent;

                int maxOpponentScoreCreated = 0;
                (int dr, int dc)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };

                // Check the best pattern the opponent creates along any line through this move
                foreach (var dir in directions)
                {
                    // Use CountAndScoreSequence to find the score of the opponent's sequence formed
                    // Need the CountAndScoreSequence helper from ScoreLine function available here
                    // Or replicate its core logic. Let's assume CountAndScoreSequence is accessible or replicated.
                    maxOpponentScoreCreated = Math.Max(maxOpponentScoreCreated,
                                                       CountAndScoreSequence_Helper(currentBoard, blockMove.Row, blockMove.Col, dir.dr, dir.dc, opponent, scores));
                }

                // Revert the board
                currentBoard[blockMove.Row, blockMove.Col] = EMPTY_CELL;

                // Return the corresponding BLOCKING score based on the threat level
                if (maxOpponentScoreCreated >= scores["FIVE"]) return scores["BLOCK_FIVE"];
                if (maxOpponentScoreCreated >= scores["FOUR_OPEN"]) return scores["BLOCK_FOUR_OPEN"];
                if (maxOpponentScoreCreated >= scores["FOUR_CLOSED"]) return scores["BLOCK_FOUR_CLOSED"];
                if (maxOpponentScoreCreated >= scores["THREE_OPEN"]) return scores["BLOCK_THREE_OPEN"];
                if (maxOpponentScoreCreated >= scores["THREE_CLOSED"]) return scores["BLOCK_THREE_CLOSED"];
                // Add lower threats if necessary
                if (maxOpponentScoreCreated >= scores["TWO_OPEN"]) return scores["BLOCK_TWO_OPEN"];
                if (maxOpponentScoreCreated >= scores["TWO_CLOSED"]) return scores["BLOCK_TWO_CLOSED"];

                return 0; // No significant threat was blocked by playing here
            }
            else
            {
                return 0; // Should not happen if called on an empty cell
            }
        }
        */

        /*
        // --- You need the CountAndScoreSequence_Helper accessible here ---
        // This is the same helper function used inside ScoreLine.
        // It calculates the score for player 'p's sequence at (r,c) along a direction.
        private int CountAndScoreSequence_Helper(int[,] currentBoard, int r, int c, int dr, int dc, int p, Dictionary<string, int> currentScores)
        {
            // ... (Exact same implementation as provided inside ScoreLine_V2_Final) ...
            int consecutive = 0; int openEnds = 0; bool startBlockedByOpponent = false; bool endBlockedByOpponent = false;
            int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
            int backward_r = r - dr; int backward_c = c - dc;
            while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p) { consecutive++; backward_r -= dr; backward_c -= dc; }
            if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) { } else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) { openEnds++; } else if (currentBoard[backward_r, backward_c] == otherPlayer) { startBlockedByOpponent = true; }
            backward_r = r + dr; backward_c = c + dc;
            while (backward_r >= 0 && backward_r < BOARD_SIZE && backward_c >= 0 && backward_c < BOARD_SIZE && currentBoard[backward_r, backward_c] == p) { consecutive++; backward_r += dr; backward_c += dc; }
            if (backward_r < 0 || backward_r >= BOARD_SIZE || backward_c < 0 || backward_c >= BOARD_SIZE) { } else if (currentBoard[backward_r, backward_c] == EMPTY_CELL) { openEnds++; } else if (currentBoard[backward_r, backward_c] == otherPlayer) { endBlockedByOpponent = true; }
            consecutive++;
            if (consecutive >= WIN_LENGTH) { return (consecutive == WIN_LENGTH && !(startBlockedByOpponent && endBlockedByOpponent)) ? currentScores["FIVE"] : 0; }
            else if (consecutive == 4) { return (openEnds == 2) ? currentScores["FOUR_OPEN"] : ((openEnds == 1) ? currentScores["FOUR_CLOSED"] : 0); }
            else if (consecutive == 3) { return (openEnds == 2) ? currentScores["THREE_OPEN"] : ((openEnds == 1) ? currentScores["THREE_CLOSED"] : 0); }
            else if (consecutive == 2) { return (openEnds == 2) ? currentScores["TWO_OPEN"] : ((openEnds == 1) ? currentScores["TWO_CLOSED"] : 0); }
            else if (consecutive == 1) { return (openEnds == 2) ? currentScores["ONE_OPEN"] : ((openEnds == 1) ? currentScores["ONE_CLOSED"] : 0); }
            return 0;
        }
        */

        /*
        /// <summary>
        /// Finds the best move using Aggressive V2 scoring.
        /// **REVISED LOGIC V5:** Compares critical blocking moves based on the
        /// severity of the threat they block (using GetBlockedThreatScore).
        /// Uses EvaluateCell_V3_Final for general evaluation and tie-breaking.
        /// </summary>
        public Move? FindBestMoveAggressive_V5_Final(int[,] currentBoard, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var _random = new Random();

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0) { Console.Error.WriteLine("AI V5 Final: No possible moves left!"); return null; }

            // --- Handle First Move ---
            // ... (Same center logic as before) ...

            int[,] tempBoard = (int[,])currentBoard.Clone(); // Clone ONLY for win/loss checks

            // Priority 1: AI Win immediately
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, tempBoard))
                {
                    Debug.WriteLine($"AI V5 Final: Found winning move at {move}");
                    return move;
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // Priority 2: Find ALL moves that block an immediate opponent win (FIVE)
            List<Move> criticalBlockingMoves = new List<Move>();
            foreach (var move in possibleMoves)
            {
                tempBoard[move.Row, move.Col] = opponent;
                if (CheckWin(move.Row, move.Col, opponent, tempBoard))
                {
                    criticalBlockingMoves.Add(move);
                }
                tempBoard[move.Row, move.Col] = EMPTY_CELL; // Revert temp board
            }

            // --- Decision based on Critical Blocking Moves ---
            if (criticalBlockingMoves.Count > 0)
            {
                Debug.WriteLine($"AI V5 Final: Found {criticalBlockingMoves.Count} critical blocking move(s). Evaluating threat levels...");
                if (criticalBlockingMoves.Count == 1)
                {
                    Debug.WriteLine($"AI V5 Final: Executing the only critical block at {criticalBlockingMoves[0]}");
                    return criticalBlockingMoves[0];
                }
                else
                {
                    // --- NEW COMPARISON LOGIC ---
                    // Compare critical blocks based on the THREAT LEVEL they block
                    int bestThreatScore = -1; // Use -1 to ensure any block is chosen initially
                    Move? bestBlockMove = null;
                    List<Move> bestThreatTiedMoves = new List<Move>(); // Moves blocking the same highest threat

                    foreach (var blockingMove in criticalBlockingMoves)
                    {
                        // Determine the score value of the threat being blocked
                        int threatScoreBlocked = GetBlockedThreatScore(currentBoard, blockingMove, player);
                        Debug.WriteLine($"   - Threat blocked by {blockingMove}: Score = {threatScoreBlocked}"); // Debug

                        if (threatScoreBlocked > bestThreatScore)
                        {
                            bestThreatScore = threatScoreBlocked;
                            bestBlockMove = blockingMove; // Found a more critical threat to block
                            bestThreatTiedMoves.Clear();
                            bestThreatTiedMoves.Add(blockingMove);
                        }
                        else if (threatScoreBlocked == bestThreatScore)
                        {
                            // Multiple moves block the same level of threat, add to list for tie-breaking
                            bestThreatTiedMoves.Add(blockingMove);
                        }
                    }

                    // If only one move blocks the highest threat, return it
                    if (bestThreatTiedMoves.Count == 1)
                    {
                        Debug.WriteLine($"AI V5 Final: Chose block {bestThreatTiedMoves[0]} blocking threat score {bestThreatScore}");
                        return bestThreatTiedMoves[0];
                    }
                    else if (bestThreatTiedMoves.Count > 1)
                    {
                        // TIE-BREAKER: Multiple moves block the same highest threat level.
                        // Use the general EvaluateCell (V3_Final with neutralize bonus)
                        // to choose the best among *these tied moves*.
                        Debug.WriteLine($"AI V5 Final: Tie-breaking between {bestThreatTiedMoves.Count} moves blocking threat {bestThreatScore} using EvaluateCell...");
                        int bestEvalScore = int.MinValue;
                        Move? finalBestBlock = null;
                        foreach (var tiedMove in bestThreatTiedMoves)
                        {
                            int evalScore = EvaluateCell_V3_Final(currentBoard, tiedMove.Row, tiedMove.Col, player); // Use the best evaluator
                            Debug.WriteLine($"     - Evaluating tied block {tiedMove}: Eval Score = {evalScore}"); // Debug
                            if (evalScore > bestEvalScore)
                            {
                                bestEvalScore = evalScore;
                                finalBestBlock = tiedMove;
                            }
                            else if (evalScore == bestEvalScore && _random.NextDouble() < 0.5)
                            {
                                finalBestBlock = tiedMove; // Random tie-break on eval score
                            }
                        }
                        if (finalBestBlock.HasValue)
                        {
                            Debug.WriteLine($"AI V5 Final: Chose tied block {finalBestBlock.Value} with eval score {bestEvalScore}");
                            return finalBestBlock.Value;
                        }
                        else
                        {
                            // Fallback if evaluation fails for tied moves
                            Debug.WriteLine("Warning: AI V5 Final: Could not tie-break critical blocks, picking first tied.");
                            return bestThreatTiedMoves[0];
                        }
                    }
                    else
                    {
                        // Should not happen if criticalBlockingMoves.Count > 1 initially
                        Debug.WriteLine("Warning: AI V5 Final: Logic error in critical block comparison, picking first critical.");
                        return criticalBlockingMoves[0];
                    }
                }
            }

            // Priority 3: General Evaluation (No immediate threats)
            int bestHeuristicScore = int.MinValue;
            Move? bestHeuristicMove = null;
            foreach (var move in possibleMoves)
            {
                int currentScore = EvaluateCell_V3_Final(currentBoard, move.Row, move.Col, player); // Use final evaluator
                                                                                                    // ... (Update best heuristic move logic as before) ...
                if (currentScore > bestHeuristicScore) { bestHeuristicMove = move; }
                else if (currentScore == bestHeuristicScore && _random.NextDouble() < 0.3) {  }
            }

            // ... (Fallback logic and return bestHeuristicMove) ...
            if (!bestHeuristicMove.HasValue && possibleMoves.Count > 0)
            {
                Debug.WriteLine("Warning: AI V5 Final: No best heuristic move found, picking random possible one.");
                bestHeuristicMove = possibleMoves[_random.Next(possibleMoves.Count)];
            }
            if (bestHeuristicMove.HasValue)
            {
                string scoreString = (bestHeuristicScore != int.MinValue) ? bestHeuristicScore.ToString() : "N/A";
                Debug.WriteLine($"AI V5 Final: Chosen heuristic move: {bestHeuristicMove.Value} with score: {scoreString}");
            }
            else { Console.Error.WriteLine("AI V5 Final: Could not determine any move."); }
            return bestHeuristicMove;


        } // End of FindBestMoveAggressive_V5_Final
        */

        private bool StopHereForDebugging(int row, int column, int player)
        {
            if ((row == 5 && column == 6) || (row == 5 && column == 10))
            {
                Debug.WriteLine("Warning: STOP HERE for DEBUGGING... Row:{0}, Column:{1}", row, column);
                return true;
            }
            return false;
        }

        private bool StopHereForDebugging(Move move, int player)
        {
            return StopHereForDebugging(move.Row, move.Col, player);
        }

        /// <summary>
        /// Finds the best move using Aggressive V2 scoring and evaluation.
        /// **REVISED LOGIC V6:** Includes a one-move lookahead when comparing
        /// multiple critical blocking moves to ensure the chosen block doesn't
        /// lead to an immediate loss on the opponent's next turn.
        /// </summary>
        public Move? FindBestMoveAggressive_V6_Lookahead(int[,] currentBoard, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var _random = new Random();

            List<Move> possibleMoves = GetPossibleMoves(currentBoard);
            if (possibleMoves.Count == 0) { Console.Error.WriteLine("AI V6 Lookahead: No possible moves left!"); return null; }

            // --- Handle First Move ---
            // ... (Same center logic as before) ...

            int[,] checkBoard = (int[,])currentBoard.Clone(); // Clone for win/loss checks

            // Priority 1: AI Win immediately
            foreach (var move in possibleMoves)
            {
                checkBoard[move.Row, move.Col] = player;
                if (CheckWin(move.Row, move.Col, player, checkBoard) != null)
                {
                    Debug.WriteLine($"AI V6 Lookahead: Found winning move at {move}");
                    return move;
                }
                checkBoard[move.Row, move.Col] = EMPTY_CELL; // Revert
            }

            // Priority 2: Find ALL moves that block an immediate opponent win (FIVE)
            List<Move> criticalBlockingMoves = new List<Move>();
            foreach (var move in possibleMoves)
            {
                checkBoard[move.Row, move.Col] = opponent;

                StopHereForDebugging(move, opponent);
                var possibleWin = CheckWin(move.Row, move.Col, opponent, checkBoard);
                if (possibleWin != null)
                {
                    criticalBlockingMoves.Add(move);
                    var last = possibleWin.Last();
                    string[] keys = last.Key.Split(":");
                    string[] dir = keys[0].Split("_");
                    int dr = int.Parse(dir[0]);
                    int dc = int.Parse(dir[1]);

                    if (keys[1].Contains("backward")) // blocked 1 side, add criticalBlocking move to the other side for decision below
                    {
                        int r = last.Value.Row + (WIN_LENGTH + 1) * dr;
                        int c = last.Value.Col + (WIN_LENGTH + 1) * dc;
                        if (IsInCaroBoard(r, c) && checkBoard[r, c] == EMPTY_CELL)
                            criticalBlockingMoves.Add(new Move(r, c));
                    }
                    else if (keys[1].Contains("forward")) // blocked 1 side, add criticalBlocking move to the other side for decision below
                    {
                        int r = last.Value.Row - (WIN_LENGTH + 1) * dr;
                        int c = last.Value.Col - (WIN_LENGTH + 1) * dc;
                        if (IsInCaroBoard(r, c) && checkBoard[r, c] == EMPTY_CELL)
                            criticalBlockingMoves.Add(new Move(r, c));
                    }
                }
                checkBoard[move.Row, move.Col] = EMPTY_CELL; // Revert
            }

            // --- Decision based on Critical Blocking Moves ---
            if (criticalBlockingMoves.Count > 0)
            {
                Debug.WriteLine($"AI V6 Lookahead: Found {criticalBlockingMoves.Count} critical blocking move(s). Performing lookahead check...");
                if (criticalBlockingMoves.Count == 1)
                {
                    Debug.WriteLine($"AI V6 Lookahead: Executing the only critical block at {criticalBlockingMoves[0]}");
                    return criticalBlockingMoves[0];
                }
                else
                {
                    // --- NEW LOOKAHEAD COMPARISON LOGIC ---
                    List<Move> safeBlockingMoves = new List<Move>();
                    int[,] lookaheadBoard = (int[,])currentBoard.Clone(); // Board for lookahead simulation

                    foreach (var blockingMove in criticalBlockingMoves)
                    {
                        // 1. Simulate AI making the blocking move
                        lookaheadBoard[blockingMove.Row, blockingMove.Col] = player;

                        // StopHereForDebugging(blockingMove, player);

                        // 2. Check if opponent has ANY winning move on their NEXT turn
                        bool opponentWinsNext = false;
                        List<Move> opponentResponses = GetPossibleMoves(lookaheadBoard); // Opponent's possible moves AFTER AI blocks
                        foreach (var oppMove in opponentResponses)
                        {
                            lookaheadBoard[oppMove.Row, oppMove.Col] = opponent; // Simulate opponent's response
                            if (CheckWin(oppMove.Row, oppMove.Col, opponent, lookaheadBoard) != null)
                            {
                                opponentWinsNext = true; // Found a winning response for opponent
                                lookaheadBoard[oppMove.Row, oppMove.Col] = EMPTY_CELL; // Revert opponent move
                                break; // No need to check other opponent moves
                            }
                            lookaheadBoard[oppMove.Row, oppMove.Col] = EMPTY_CELL; // Revert opponent move
                        }

                        // 3. If opponent does NOT win next, this blocking move is "safe" for now
                        if (!opponentWinsNext)
                        {
                            safeBlockingMoves.Add(blockingMove);
                        }
                        else
                        {
                            Debug.WriteLine($"   - Critical block {blockingMove} is unsafe (opponent wins next).");
                        }

                        // 4. Revert AI's blocking move on the lookahead board for the next iteration
                        lookaheadBoard[blockingMove.Row, blockingMove.Col] = EMPTY_CELL;
                    } // End foreach criticalBlockingMove

                    // --- Choose among the safe blocking moves ---
                    if (safeBlockingMoves.Count > 0)
                    {
                        Debug.WriteLine($"AI V6 Lookahead: Found {safeBlockingMoves.Count} safe blocking move(s). Evaluating them...");
                        // If only one block is safe, choose it
                        if (safeBlockingMoves.Count == 1)
                        {
                            Debug.WriteLine($"AI V6 Lookahead: Choosing the only safe block: {safeBlockingMoves[0]}");
                            return safeBlockingMoves[0];
                        }
                        else
                        {
                            // If multiple blocks are safe, use standard evaluation as a tie-breaker among them
                            int bestEvalScore = int.MinValue;
                            Move? finalBestBlock = null;
                            foreach (var safeMove in safeBlockingMoves)
                            {
                                // Use the latest EvaluateCell (V3_Final, which calls ScoreLine_V3_Final with no early return)
                                int evalScore = EvaluateCell_V3_Final(currentBoard, safeMove.Row, safeMove.Col, player);
                                Debug.WriteLine($"     - Evaluating safe block {safeMove}: Eval Score = {evalScore}");
                                if (evalScore > bestEvalScore)
                                {
                                    bestEvalScore = evalScore;
                                    finalBestBlock = safeMove;
                                }
                                else if (evalScore == bestEvalScore && _random.NextDouble() < 0.5)
                                {
                                    finalBestBlock = safeMove; // Random tie-break
                                }
                            }
                            if (finalBestBlock.HasValue)
                            {
                                Debug.WriteLine($"AI V6 Lookahead: Chose safe block {finalBestBlock.Value} with eval score {bestEvalScore}");
                                return finalBestBlock.Value;
                            }
                            else
                            {
                                Debug.WriteLine("Warning: AI V6 Lookahead: Could not tie-break safe blocks, picking first safe.");
                                return safeBlockingMoves[0]; // Fallback
                            }
                        }
                    }
                    else
                    {
                        // If ALL critical blocking moves lead to an immediate loss (unavoidable loss)
                        Debug.WriteLine("Warning: AI V6 Lookahead: All critical blocks lead to opponent win next turn! Choosing first critical block as fallback.");
                        return criticalBlockingMoves[0]; // Just make one of the blocks anyway
                    }
                }
            }

            // Priority 3: General Evaluation (No immediate threats)
            int bestHeuristicScore = int.MinValue;
            Move? bestHeuristicMove = null;
            foreach (var move in possibleMoves)
            {
                bool showEvaluateScore = StopHereForDebugging(move, player);

                // Use the latest EvaluateCell (V3_Final)
                int currentScore = EvaluateCell_V3_Final(currentBoard, move.Row, move.Col, player);
                //if (showEvaluateScore)
                //{
                //    Debug.WriteLine("Warning: AI V6 Lookahead: Row: {0}, Column: {1}. Evaluate Score: {2}", move.Row, move.Col, currentScore);
                //    showEvaluateScore = false;
                //}

                // ... (Update best heuristic move logic as before) ...
                if (currentScore > bestHeuristicScore) 
                { 
                    bestHeuristicScore = currentScore; 
                    bestHeuristicMove = move; 
                }
                else if (currentScore == bestHeuristicScore && _random.NextDouble() < 0.3) 
                { 
                    bestHeuristicMove = move; 
                }
            }

            // ... (Fallback logic and return bestHeuristicMove) ...
            if (!bestHeuristicMove.HasValue && possibleMoves.Count > 0) 
            {
                Debug.WriteLine("Warning: AI V6 Lookahead: No best heuristic move found, picking random possible one.");
                bestHeuristicMove = possibleMoves[_random.Next(possibleMoves.Count)]; 
            }

            // Log the chosen heuristic move
            if (bestHeuristicMove.HasValue)
            {
                string scoreString = (bestHeuristicScore != int.MinValue) ? bestHeuristicScore.ToString() : "N/A";
                Debug.WriteLine($"AI V6 Lookahead: Chosen heuristic move: {bestHeuristicMove.Value} with score: {scoreString}");
            }
            else
            {
                Console.Error.WriteLine("AI V6 Lookahead: Could not determine any move.");
            }

            return bestHeuristicMove;

        } // End of FindBestMoveAggressive_V6_Lookahead

        // Include other necessary methods:
        // SCORE_AGGRESSIVE_V2 dictionary
        // CheckWin(...)
        // ScoreLine_V3_Final(...) -> No early return, includes neutralize bonus calc
        // EvaluateCell_V3_Final(...) -> Calls ScoreLine_V3_Final
        // GetPossibleMoves(...)
        // CalculateNeutralizeBonus(...) -> Helper for ScoreLine
        // CountAndScoreSequence_Helper(...) -> Helper for GetBlockedThreatScore and ScoreLine
    }

} // End of CaroAI class

//**Summary of Changes:**

//1.  * *`SCORE_AGGRESSIVE_V2`**: Introduced with `NEUTRALIZE_FOUR_BONUS` and `NEUTRALIZE_THREE_BONUS`.
//2.  **`ScoreLine_V2`**: Replaces `scoreLine`. Contains the core logic change, including the `CheckNeutralizeBonus` helper function to add bonus points when a move successfully double-blocks an opponent's sequence of 3 or 4.
//3.  **`EvaluateCell_V2`**: Replaces `evaluateCell`. Its only change is that it now calls `ScoreLine_V2`.
//4.  **`FindBestMoveAggressive`**: Replaces `findBestMoveAggressive`. It now calls `EvaluateCell_V2`. The explicit, separate step for checking and blocking opponent's open fours (`FindAndEvaluateForcedBlock`) has been removed, as the improved `EvaluateCell_V2` (thanks to `ScoreLine_V2`) should now correctly assign a higher score to the proper neutralizing block, making the separate check redundant.

//Remember to update your game logic to use `FindBestMoveAggressive_V2` and ensure the `CaroAI` class uses the `SCORE_AGGRESSIVE_V2` tab