using System;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public struct MoveHint
    {
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }

        public MoveHint(int fx, int fy, int tx, int ty)
            : this()
        {
            FromX = fx;
            FromY = fy;
            ToX = tx;
            ToY = ty;
        }
    }

    public static class HintFinder
    {
        public static MoveHint? FindValidMove(Board board)
        {
            for (int y = 0; y < Board.Rows; y++)
            {
                for (int x = 0; x < Board.Cols; x++)
                {
                    // Check Right Swap
                    if (x < Board.Cols - 1)
                    {
                        if (board.TestSwap(x, y, x + 1, y))
                        {
                            return new MoveHint(x, y, x + 1, y);
                        }
                    }
                    // Check Down Swap
                    if (y < Board.Rows - 1)
                    {
                        if (board.TestSwap(x, y, x, y + 1))
                        {
                            return new MoveHint(x, y, x, y + 1);
                        }
                    }
                }
            }
            return null;
        }

        // Returns the list of directions (dx, dy) in which the gem at (x, y)
        // can be swapped to make a valid move (Right, Left, Down, Up order).
        public static List<KeyValuePair<int, int>> GetValidMovesFrom(Board board, int x, int y)
        {
            List<KeyValuePair<int, int>> moves = new List<KeyValuePair<int, int>>();
            if (x < Board.Cols - 1 && board.TestSwap(x, y, x + 1, y)) moves.Add(new KeyValuePair<int, int>(1, 0));
            if (x > 0 && board.TestSwap(x, y, x - 1, y)) moves.Add(new KeyValuePair<int, int>(-1, 0));
            if (y < Board.Rows - 1 && board.TestSwap(x, y, x, y + 1)) moves.Add(new KeyValuePair<int, int>(0, 1));
            if (y > 0 && board.TestSwap(x, y, x, y - 1)) moves.Add(new KeyValuePair<int, int>(0, -1));
            return moves;
        }
    }
}
