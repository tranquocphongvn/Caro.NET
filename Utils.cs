using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caro.NET
{
    public static class Utils
    {
        public const int MAX_ROWS = 25;
        public const int MAX_COLUMNS = 25;
        public const int CELL_SIZE = 50;
        public const int CELL_PADDING = 10;
        public const int BOARD_LEFT = 10;
        public const int BOARD_TOP = 10;

        public const int CARO_NONE = 0;
        public const int CARO_X = 1;
        public const int CARO_O = 2;
        public const int CARO_UNDEF = -1;
        public const int CARO_EVAL_UNDEF = -99;
        public const int CARO_EVAL_FIRST = -1;
        public const int CARO_EVAL_SECOND = -2;
        public const int CARO_EVAL_HUMAN = -3;

        public enum GameStatus
        {
            New = 0,
            Playing = 1,
            Over = 2,
            Stop = 3
        }

        public struct CaroCell
        {
            public int Row, Column;

            public CaroCell()
            {
                Row = -1;
                Column = -1;
            }
            public CaroCell(int row, int column)
            {
                Row = row;
                Column = column;
            }
        }

        public struct SmPoint
        {
            public int X, Y;
            public SmPoint()
            {
                X = -1;
                Y = -1;
            }
            public SmPoint(int x, int y)
            {
                X = x; Y = y;
            }
        }

        public struct Rect
        {
            public int X, Y, Width, Height;
            public Rect()
            {
                X = -1;
                Y = -1;
                Width = 0;
                Height = 0;
            }
            public Rect(int x, int y, int width, int height)
            {
                X = x; Y = y;
                Width = width; Height = height;
            }
        }

        public struct CaroMove
        {
            public CaroCell Cell;
            //public SmPoint Center;
            public int CaroValue = CARO_UNDEF;
            public int EvalValue = CARO_EVAL_UNDEF;
            public CaroMove() {
                CaroValue = CARO_UNDEF;
                EvalValue = CARO_EVAL_UNDEF;
            }

            public CaroMove(int row, int column, int caroValue, int evalValue)
            {
                Cell.Row = row; Cell.Column = column;
                CaroValue = caroValue;
                EvalValue = evalValue;  
            }
        }

        public static int GetOpponentCaroValue(int caroValue)
        {
            return caroValue == Utils.CARO_X? Utils.CARO_O : Utils.CARO_X;
        }
        public static string CaroValueToText(int caroValue)
        {
            return caroValue == Utils.CARO_X ? "X" : (caroValue == Utils.CARO_O ? "O" : "_");
        }

    }
}
