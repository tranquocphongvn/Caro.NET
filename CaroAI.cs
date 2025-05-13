using System;
using System.Collections.Generic;
using System.Diagnostics; // For Stopwatch (optional timing)
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
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
            {"FIVE", 300000},        // Winning move innerScore
            {"BLOCK_FIVE", 150000},   // Score for blocking opponent from forming 5 (even if not yet a winning 5)

            // Bonus innerScore for successfully double-blocking opponent's dangerous sequence
            //{"NEUTRALIZE_FOUR_BONUS", 30000}, // Must be significantly higher than BLOCK_FOUR_OPEN, Keep high for rewarding double-blocks
            //{"NEUTRALIZE_THREE_BONUS", 5000}, // Must be significantly higher than BLOCK_THREE_OPEN

            {"FOUR_OPEN", 20100},      // Creating own open four remains very valuable, (_xxxx*_; _xxx*x_; _xx*xx_)
            {"BLOCK_FOUR_OPEN", 20000}, // Blocking one end of opponent's open four

            {"THREE_OPEN", 2010}, // Keep offensive value high for now, (_*xxx*_ ; __xx*x__)
            {"SEMI_THREE_OPEN", 1000}, // Keep offensive value high for now, (_xx_x*_)
            {"BLOCK_THREE_OPEN", 2000},  // SIGNIFICANTLY INCREASED from 10000

            //{"THREE_OPEN", 50000},      // Creating own open three
            //{"BLOCK_THREE_OPEN", 10000}, // Blocking one end of opponent's open three

            // Other scores remain similar to the previous aggressive version
            {"FOUR_CLOSED", 2020},     // Creating a closed four, (_*xxxxo  or  oxxxx*_)
            {"BLOCK_FOUR_CLOSED", 2005},// Blocking opponent's closed four

            {"THREE_CLOSED", 200},      // Creating a closed three , 150 => 300 , (_xxxo_  or o___xxx__)
            {"BLOCK_THREE_CLOSED", 190}, // Blocking opponent's closed three

            {"TWO_OPEN", 200},          // Creating an open two, (___*xx___ ; ___xx*___ ; ___x*x___ )
            {"SEMI_TWO_OPEN", 150},     // Creating an open two, (___x_x*__; ___x__x*__)
            {"BLOCK_TWO_OPEN", 190},    // Blocking opponent's open two

            {"TWO_CLOSED", 25},         // Creating a closed two, (__*xxo_ or o_*xx_)
            {"BLOCK_TWO_CLOSED", 20},   // Blocking opponent's closed two
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
        /// Calculates innerScore for a line. Uses SCORE_AGGRESSIVE_V2.
        /// Check Block both side by Boundary or Opponent
        /// </summary>
        private bool BlockedBothByBoundaryOrOpponent(int[,] currentBoard, int row, int col, int dr, int dc, int player, int opponent)
        {
            // check blocked both side by Boundary or Opponent

            //if ((row == 5 && col == 2 && dr == 1))
            //{
            //    Debug.WriteLine("Warning: BlockedBothByBoundaryOrOpponent: STOP HERE for DEBUGGING... Row:{0}, Column:{1}", row, col);
            //}

            int count = 0; // max = WIN_LENGTH - 1

            int backward_r = row - dr; int backward_c = col - dc;
            int backward_count = 0; // start from 1
            bool backward_bound = IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't main_count

            int forward_r = row + dr; int forward_c = col + dc;
            int forward_count = 1; // start from 1
            bool forward_bound = IsOutOfCaroBoard(forward_r, forward_c); // forward boundary is out of the board, don't main_count

            // Count backwards & forwards
            while (count < WIN_LENGTH && (!backward_bound || !forward_bound))
            {
                // Check backwards
                backward_bound = backward_bound || IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't main_count
                if (backward_bound || currentBoard[backward_r, backward_c] == opponent) // blocked by Boundary or Opponent, dont need check cell value
                    backward_bound = true; // dont need check backward anymore
                else 
                {
                    // increase the counter for backward
                    backward_count++;
                    backward_r -= dr; backward_c -= dc;
                }


                // Check forwards
                forward_bound = forward_bound || IsOutOfCaroBoard(forward_r, forward_c); ; // forward boundary is out of the board, don't main_count
                if (forward_bound || currentBoard[forward_r, forward_c] == opponent) // blocked by Boundary or Opponent, dont need check cell value
                    forward_bound = true; // dont need check forward anymore
                else
                {
                    // increase the counter for forward
                    forward_count++;
                    forward_r += dr; forward_c += dc;
                }

                // increase the counter
                count++;
            }

            // blocked both side by Boundary or Opponent
            if (backward_bound && forward_bound && (backward_count + forward_count <= WIN_LENGTH + 1)) // WIN_LENGTH + 1, because boundary counts start from 1;
            {
                //Debug.WriteLine("BlockedBothByBoundaryOrOpponent... Row:{0}, Column:{1}, dr:{2}, dc:{3}, backward_count:{6}, forward_count:{7}, player:{4}, opponent:{5}", row, col, dr, dc, player, opponent, backward_count, forward_count);
                return true;
            }

            return false;
        }

        private int ScoreLine(int[,] currentBoard, int row, int col, int dr, int dc, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var currentScores = SCORE_AGGRESSIVE_V2; // Use V2 scores with Neutralize bonus


            //================= Begin of CountAndScoreSequence ==============
            // Helper: CountAndScoreSequence (Same as before - calculates innerScore for a single player's sequence)
            int CountAndScoreSequence(int r, int c, int p)
            {
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
                int main_count = 1; // start from 1 ..._XXXXO ; max = WIN_LENGTH, dont count itself

                bool backward_bound = false;
                bool forward_bound = false;

                int maxScore = 0;

                Dictionary<string, int> evaluteValues = new Dictionary<string, int>();


                // Count backwards & forwards
                while (main_count <= WIN_LENGTH && (!backward_bound || !forward_bound))
                {
                    int innerScore = 0;
                    int consecutive = 0;

                    // Check backwards
                    // for backward
                    int backward_r = row - dr; int backward_c = col - dc;
                    int backward_count = 1; // start from 1, because the backward_r = row - dr; backward_c = col - dc, above
                    int backward_empty = 0;
                    int backward_consecutive = 0;
                    bool backward_full_consecutive = true;
                    //backward_bound = false; // backward boundary is out of the board, don't main_count

                    int[] backward_pattern = new int[WIN_LENGTH];

                    do
                    {
                        backward_bound = backward_bound || IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't main_count
                        if (backward_bound || currentBoard[backward_r, backward_c] == otherPlayer) // blocked by Boundary or Opponent, dont need check cell value
                        {
                            backward_bound = true; // dont need check backward anymore
                            backward_pattern[WIN_LENGTH - backward_count] = otherPlayer;
                        }
                        else
                        {
                            if (currentBoard[backward_r, backward_c] == p)
                            {
                                backward_full_consecutive = backward_full_consecutive && (backward_empty == 0);
                                backward_consecutive++;
                                backward_pattern[WIN_LENGTH - backward_count] = p;
                            }
                            else if (currentBoard[backward_r, backward_c] == EMPTY_CELL)
                            {
                                backward_empty++;
                                backward_pattern[WIN_LENGTH - backward_count] = EMPTY_CELL;
                            }

                            // increase the counter for backward
                            backward_count++;
                            backward_r -= dr; backward_c -= dc;
                        }
                    }
                    while (backward_count <= main_count && !backward_bound);

                    // Check forwards
                    // for forward
                    int forward_r = row + dr; int forward_c = col + dc;
                    int forward_count = 1; // start from 1, because the forward_r = row + dr; forward_c = col + dc, above
                    int forward_empty = 0;
                    int forward_consecutive = 0;
                    bool forward_full_consecutive = true;
                    //forward_bound = false;

                    int[] forward_pattern = new int[WIN_LENGTH];

                    do
                    {
                        forward_bound = forward_bound || IsOutOfCaroBoard(forward_r, forward_c); // forward boundary is out of the board, don't main_count
                        if (forward_bound || currentBoard[forward_r, forward_c] == otherPlayer) // blocked by Boundary or Opponent, dont need check cell value
                        {
                            forward_bound = true; // dont need check forward anymore
                            forward_pattern[forward_count - 1] = otherPlayer;
                        }
                        else
                        {
                            if (currentBoard[forward_r, forward_c] == p)
                            {
                                forward_full_consecutive = forward_full_consecutive && (forward_empty == 0);
                                forward_consecutive++; // consecutive++;
                                forward_pattern[forward_count - 1] = p;
                            }
                            else if (currentBoard[forward_r, forward_c] == EMPTY_CELL)
                            {
                                forward_empty++;
                                forward_pattern[forward_count - 1] = EMPTY_CELL;
                            }
                            // increase the counter for forward
                            forward_count++;
                            forward_r += dr; forward_c += dc;
                        }
                    }
                    while (forward_count <= (WIN_LENGTH - main_count + 1) && !forward_bound);


                    backward_full_consecutive = backward_full_consecutive || backward_consecutive == 0;
                    forward_full_consecutive = forward_full_consecutive || forward_consecutive == 0;

                    consecutive = backward_consecutive + forward_consecutive + ((currentBoard[r, c] == p) ? 1 : 0);

                    // blocked both side by Boundary or Opponent
                    if (backward_bound && forward_bound && (backward_count + forward_count <= WIN_LENGTH + 1)) // WIN_LENGTH + 1, because boundary counts start from 1;
                    {
                        // dont calculate any more
                        return 0;
                    }
                    else if (consecutive > WIN_LENGTH)
                    {
                        // dont calculate any more
                        return 0;
                    }
                    else if ((!backward_bound && backward_count >= WIN_LENGTH && backward_empty <= 2 && IsInCaroBoard(backward_r, backward_c) && currentBoard[backward_r, backward_c] == p) // + dr, dc
                        || (!forward_bound && forward_count >= WIN_LENGTH && forward_empty <= 2 && IsInCaroBoard(forward_r, forward_c) && currentBoard[forward_r, forward_c] == p)) // - dr, dc
                    {
                        // Maybe over 6 (WIN_LENGTH + 1, because counts start from 1) in the row
                        // dont calculate any more
                        return 0;
                    }
                    else if (consecutive == WIN_LENGTH)
                    {
                        // max value
                        innerScore = currentScores["FIVE"];
                    }
                    else
                    { 
                        bool is_open = (!backward_bound && !forward_bound);
                        if (consecutive == 4)
                        {
                            if (is_open)
                            {
                                innerScore = currentScores["FOUR_OPEN"] + 30 - 2 * (backward_empty + forward_empty);
                            }

                            else
                            {
                                if (backward_full_consecutive && forward_full_consecutive)
                                    innerScore = currentScores["FOUR_CLOSED"] + 25 - 2 * (backward_empty + forward_empty);
                                else
                                    innerScore = currentScores["THREE_CLOSED"] + 18 - 2 * (backward_empty + forward_empty);
                            }
                        }
                        else if (consecutive == 3)
                        {
                            if (is_open)
                            {
                                if (backward_full_consecutive && forward_full_consecutive)
                                    innerScore = currentScores["THREE_OPEN"] + 25 - 2 * (backward_empty + forward_empty);
                                else
                                    innerScore = currentScores["SEMI_THREE_OPEN"] + 20 - 2 * (backward_empty + forward_empty);
                            }
                            else
                                innerScore = currentScores["THREE_CLOSED"] + 18 - 2 * (backward_empty + forward_empty);
                        }
                        else if (consecutive == 2)
                        {
                            if (is_open)
                            {
                                if (backward_full_consecutive && forward_full_consecutive)
                                    innerScore = currentScores["TWO_OPEN"] + 12 - (backward_empty + forward_empty);
                                else
                                    innerScore = currentScores["SEMI_TWO_OPEN"] + 12 - (backward_empty + forward_empty);
                            }
                            else
                                innerScore = currentScores["TWO_CLOSED"] + 12 - (backward_empty + forward_empty);
                        }
                        else if (consecutive == 1)
                        {
                            innerScore = (is_open) ? currentScores["ONE_OPEN"] : currentScores["ONE_CLOSED"];
                        }
                    }

                    string key = $"{ConvertArray2String(backward_pattern, forward_pattern)}:{main_count}:{backward_count}:{forward_count}";
                    if (!evaluteValues.ContainsKey(key))
                        evaluteValues.Add(key, innerScore);

                    if (innerScore > maxScore)
                        maxScore = innerScore;

                    // increase the main counter
                    main_count++;
                }
                return maxScore;
            }
            //================= End of CountAndScoreSequence ==============

            bool showEvaluateScore = StopHereForDebugging(row, col, player, dr, dc);

            int playerScore = 0;
            int neutralizeBonus = 0;
            //if (!BlockedBothByBoundaryOrOpponent(currentBoard, row, col, dr, dc, player, opponent))
            //{
            //    // 1. Calculate offensive innerScore (for player)
                playerScore = CountAndScoreSequence(row, col, player);
                // Check for immediate player win - return highest innerScore if found
                if (playerScore >= currentScores["FIVE"])
                    return currentScores["FIVE"];

                // 3. Calculate Neutralize Bonus (using the corrected logic)
                //neutralizeBonus = CalculateNeutralizeBonus_V2(currentBoard, row, col, dr, dc, player, opponent, currentScores);
            //}

            // 2. Calculate opponent's potential innerScore if they played here
            currentBoard[row, col] = opponent;
            //int opponentPotentialScore = 0;
            //if (!BlockedBothByBoundaryOrOpponent(currentBoard, row, col, dr, dc, opponent, player))
            int opponentPotentialScore = CountAndScoreSequence(row, col, opponent);
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


        // --- ScoreLine V3 (No Early BLOCK_FIVE Return) ---
            /// <summary>
            /// Calculates innerScore for a line. Uses SCORE_AGGRESSIVE_V2.
            /// **REMOVED** the early return for BLOCK_FIVE. Includes Neutralize Bonus logic.
            /// Assumes player's piece IS tentatively placed before calling.
            /// </summary>
        private int ScoreLine_V3_Final(int[,] currentBoard, int row, int col, int dr, int dc, int player)
        {
            int opponent = (player == PLAYER_X) ? PLAYER_O : PLAYER_X;
            var currentScores = SCORE_AGGRESSIVE_V2; // Use V2 scores with Neutralize bonus

            // Helper: CountAndScoreSequence (Same as before - calculates innerScore for a single player's sequence)
            int CountAndScoreSequence(int r, int c, int p)
            {
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
                int main_count = 0; // start from 1 ..._XXXXO ; max = WIN_LENGTH, dont count itself
                int consecutive = 0; // (currentBoard[r, c] == p)? 1 : 0;

                // for backward
                int backward_r = row - dr; int backward_c = col - dc;
                int backward_count = 0; // start from 1
                bool backward_bound = IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't main_count
                int backward_empty = 0;
                int backward_consecutive = 0;
                bool backward_full_consecutive = true;
                int[] backward_pattern = new int[WIN_LENGTH + 1];

                // for forward
                int forward_r = row + dr; int forward_c = col + dc;
                int forward_count = 0; // start from 1
                bool forward_bound = IsOutOfCaroBoard(forward_r, forward_c); // forward boundary is out of the board, don't main_count
                int forward_empty = 0;
                int forward_consecutive = 0;
                bool forward_full_consecutive = true;
                int[] forward_pattern = new int[WIN_LENGTH + 1];

                // Count backwards & forwards
                while (main_count < WIN_LENGTH && (!backward_bound || !forward_bound))
                {
                    // Check backwards
                    backward_bound = backward_bound || IsOutOfCaroBoard(backward_r, backward_c); // backward boundary is out of the board, don't main_count
                    if (backward_bound || currentBoard[backward_r, backward_c] == otherPlayer) // blocked by Boundary or Opponent, dont need check cell value
                    {
                        backward_bound = true; // dont need check backward anymore
                        backward_pattern[WIN_LENGTH - backward_count] = otherPlayer;
                    }
                    else
                    {
                        if (currentBoard[backward_r, backward_c] == p)
                        {
                            backward_full_consecutive = backward_full_consecutive && (backward_empty == 0);
                            backward_consecutive++;
                            backward_pattern[WIN_LENGTH - backward_count] = p;
                        }
                        else if (currentBoard[backward_r, backward_c] == EMPTY_CELL)
                        {
                            backward_empty++;
                            backward_pattern[WIN_LENGTH - backward_count] = EMPTY_CELL;
                        }

                        // increase the counter for backward
                        backward_count++;
                        backward_r -= dr; backward_c -= dc;
                    }


                    // Check forwards
                    forward_bound = forward_bound || IsOutOfCaroBoard(forward_r, forward_c); ; // forward boundary is out of the board, don't main_count
                    if (forward_bound || currentBoard[forward_r, forward_c] == otherPlayer) // blocked by Boundary or Opponent, dont need check cell value
                    {
                        forward_bound = true; // dont need check forward anymore
                        forward_pattern[forward_count] = otherPlayer;
                    }
                    else
                    {
                        if (currentBoard[forward_r, forward_c] == p)
                        {
                            forward_full_consecutive = forward_full_consecutive && (forward_empty == 0);
                            forward_consecutive++; // consecutive++;
                            forward_pattern[forward_count] = p;
                        }
                        else if (currentBoard[forward_r, forward_c] == EMPTY_CELL)
                        {
                            forward_empty++;
                            forward_pattern[forward_count] = EMPTY_CELL;
                        }
                        // increase the counter for forward
                        forward_count++;
                        forward_r += dr; forward_c += dc;
                    }

                    // increase the main counter
                    main_count++;
                }

                backward_full_consecutive = backward_full_consecutive || backward_consecutive == 0;
                forward_full_consecutive = forward_full_consecutive || forward_consecutive == 0;

                consecutive = consecutive + backward_consecutive + forward_consecutive;

                // blocked both side by Boundary or Opponent
                if (backward_bound && forward_bound && (backward_count + forward_count <= WIN_LENGTH)) // WIN_LENGTH, because boundary counts start from 0;
                {
                    return 0;
                }

                // Maybe over 6 (WIN_LENGTH + 1, because counts start from 1) in the row
                if ((!backward_bound && backward_count >= WIN_LENGTH && backward_empty <= 2 && IsInCaroBoard(backward_r + dr, backward_c + dc) && currentBoard[backward_r + dr, backward_c + dc] == p) // + dr, dc
                    || (!forward_bound && forward_count >= WIN_LENGTH && forward_empty <= 2 && IsInCaroBoard(forward_r - dr, forward_c - dc) && currentBoard[forward_r - dr, forward_c - dc] == p)) // - dr, dc
                {
                    return 0;
                }

                /*
                int otherPlayer = (p == PLAYER_X) ? PLAYER_O : PLAYER_X;
                // Count backwards
                int temp_r = r - dr; int temp_c = c - dc;
                int consecutive = 0; int openEnds = 0; bool startBlockedByOpponent = false; bool endBlockedByOpponent = false;
                temp_r = r - dr; temp_c = c - dc;
                consecutive = 0;
                while (IsInCaroBoard(temp_r, temp_c) && currentBoard[temp_r, temp_c] == p) 
                { 
                    consecutive++; 
                    temp_r -= dr; temp_c -= dc; 
                }
                if (IsOutOfCaroBoard(temp_r, temp_c)) { } 
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
                while (IsInCaroBoard(temp_r, temp_c) && currentBoard[temp_r, temp_c] == p) 
                { 
                    consecutive++; 
                    temp_r += dr; temp_c += dc; 
                }

                if (IsOutOfCaroBoard(temp_r, temp_c)) { }
                else if (currentBoard[temp_r, temp_c] == EMPTY_CELL) 
                { 
                    openEnds++; 
                } 
                else if (currentBoard[temp_r, temp_c] == otherPlayer) 
                { 
                    endBlockedByOpponent = true; 
                }
                consecutive++;
                */

                bool is_open = (!backward_bound && !forward_bound);

                if ((backward_bound && forward_bound && (backward_count + forward_count <= WIN_LENGTH)) || consecutive > WIN_LENGTH)
                {
                    return 0;
                }
                else if (consecutive == WIN_LENGTH)
                {
                    return currentScores["FIVE"];
                }
                else if (consecutive == 4)
                {
                    if (is_open)
                    {
                        return currentScores["FOUR_OPEN"] + 30 - 2 * (backward_empty + forward_empty);
                    }
                        
                    else
                    {
                        if (backward_full_consecutive && forward_full_consecutive)
                            return currentScores["FOUR_CLOSED"] + 25 - 2 * (backward_empty + forward_empty);
                        else
                            return currentScores["THREE_CLOSED"] + 18 - 2 * (backward_empty + forward_empty);
                    }
                    //return ((!backward_bound || backward_empty > 0) && (!forward_bound || forward_empty > 0)) ? currentScores["FOUR_OPEN"] : currentScores["FOUR_CLOSED"];
                }
                else if (consecutive == 3)
                {
                    if (is_open)
                    {
                        if (backward_full_consecutive && forward_full_consecutive)
                            return currentScores["THREE_OPEN"] + 25 - 2 * (backward_empty + forward_empty);
                        else
                            return currentScores["SEMI_THREE_OPEN"] + 20 - 2 * (backward_empty + forward_empty);
                    }
                    else
                        return currentScores["THREE_CLOSED"] + 18 - 2 * (backward_empty + forward_empty);
                    //return ((!backward_bound || backward_empty > 0) && (!forward_bound || forward_empty > 0)) ? currentScores["THREE_OPEN"] : currentScores["THREE_CLOSED"];
                }                
                else if (consecutive == 2)
                {
                    if (is_open)
                    {
                        if (backward_full_consecutive && forward_full_consecutive)
                            return currentScores["TWO_OPEN"] + 12 - (backward_empty + forward_empty);
                        else
                            return currentScores["SEMI_TWO_OPEN"] + 12 - (backward_empty + forward_empty);
                    }
                    else
                        return currentScores["TWO_CLOSED"] + 12 - (backward_empty + forward_empty);
                }
                else if (consecutive == 1)
                {
                    return (is_open) ? currentScores["ONE_OPEN"] : currentScores["ONE_CLOSED"];
                }
                return 0;
            }

            bool showEvaluateScore = StopHereForDebugging(row, col, player);

            int playerScore = 0;
            int neutralizeBonus = 0;
            //if (!BlockedBothByBoundaryOrOpponent(currentBoard, row, col, dr, dc, player, opponent))
            //{
            // 1. Calculate offensive innerScore (for player)
            playerScore = CountAndScoreSequence(row, col, player);
            // Check for immediate player win - return highest innerScore if found
            if (playerScore >= currentScores["FIVE"])
                return currentScores["FIVE"];

            // 3. Calculate Neutralize Bonus (using the corrected logic)
            //neutralizeBonus = CalculateNeutralizeBonus_V2(currentBoard, row, col, dr, dc, player, opponent, currentScores);
            //}

            // 2. Calculate opponent's potential innerScore if they played here
            currentBoard[row, col] = opponent;
            //int opponentPotentialScore = 0;
            //if (!BlockedBothByBoundaryOrOpponent(currentBoard, row, col, dr, dc, opponent, player))
            int opponentPotentialScore = CountAndScoreSequence(row, col, opponent);
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



        // --- Updated EvaluateCell (V3 - Calls Corrected ScoreLine) ---
        /// <summary>
        /// Calculates the total innerScore for placing 'player's piece at (row, col).
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
                totalScore += ScoreLine(currentBoard, row, col, dir.dr, dir.dc, player);
            }
            currentBoard[row, col] = EMPTY_CELL; // Revert board
            // Add center bonus
            double centerBonus = ((BOARD_SIZE / 2.0 - Math.Abs(row - (BOARD_SIZE - 1.0) / 2.0)) + (BOARD_SIZE / 2.0 - Math.Abs(col - (BOARD_SIZE - 1.0) / 2.0)));
            totalScore += (int)(centerBonus * 0.5);

            CaroBoard.PutEvaluatedValueIntoBoard(row, col, totalScore);

            return totalScore;
        }


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

        private bool StopHereForDebugging(int row, int column, int player, int dr, int dc)
        {
            if ((row == 10 && column == 10 && dc == 1) || (row == 10 && column == 11 && dc == 1))
            {
                Debug.WriteLine("Warning: STOP HERE for DEBUGGING... Row:{0}, Column:{1}", row, column);
                return true;
            }
            return false;
        }

        private bool StopHereForDebugging(int row, int column, int player)
        {
            if ((row == 12 && column == 16) || (row == 14 && column == 14))
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


        private string ConvertArray2String(int[] backwards, int[] forwards)
        {
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < backwards.Length; i++)
                str.Append(Utils.CaroValueToText(backwards[i]));

            str.Append("*");
            for (int i = 0; i < forwards.Length; i++)
                str.Append(Utils.CaroValueToText(forwards[i]));

            return str.ToString().Trim("_".ToCharArray());
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
                                Debug.WriteLine($"AI V6 Lookahead: Chose safe block {finalBestBlock.Value} with eval innerScore {bestEvalScore}");
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
                Debug.WriteLine($"AI V6 Lookahead: Chosen heuristic move: {bestHeuristicMove.Value} with innerScore: {scoreString}");
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
//4.  **`FindBestMoveAggressive`**: Replaces `findBestMoveAggressive`. It now calls `EvaluateCell_V2`. The explicit, separate step for checking and blocking opponent's open fours (`FindAndEvaluateForcedBlock`) has been removed, as the improved `EvaluateCell_V2` (thanks to `ScoreLine_V2`) should now correctly assign a higher innerScore to the proper neutralizing block, making the separate check redundant.

//Remember to update your game logic to use `FindBestMoveAggressive_V2` and ensure the `CaroAI` class uses the `SCORE_AGGRESSIVE_V2` tab