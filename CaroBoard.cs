using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Caro.NET.Utils;

namespace Caro.NET
{
    public class CaroBoard
    {
        private static int[,] caroBoard = new int[Utils.MAX_ROWS, Utils.MAX_COLUMNS];
        public static List<CaroMove>? HistoryMoves { get; set; }
        public static CaroMove? FirstMoved { get; set; }
        public static CaroMove? SecondMoved { get; set; }

        public static int[,] GetCaroBoard()
        {
            return caroBoard;
        }

        public static void ClearBoard()
        {
            caroBoard = new int[Utils.MAX_ROWS, Utils.MAX_COLUMNS];
            FirstMoved = null;
            SecondMoved = null;
            HistoryMoves = null;
        }
        public static int[,] CloneBoard()
        {
            int[,] newCaroBoard;// = new int[Utils.MAX_ROW, Utils.MAX_COLUMN];
            //caroBoard.CopyTo(newCaroBoard, 0);
            newCaroBoard = (int[,])caroBoard.Clone();
            return newCaroBoard;
        }

        public static void AddMoveToHistory(CaroMove move)
        {
            if (CaroBoard.HistoryMoves == null)
                    CaroBoard.HistoryMoves = new List<CaroMove>();
            CaroBoard.HistoryMoves.Add(move);
        }

        public static void PutValueIntoBoard(int row, int col, int value)
        {
            caroBoard[row, col] = value;
        }

        public static void PutValueIntoBoard(CaroMove move)
        {
            caroBoard[move.Cell.Row, move.Cell.Column] = move.CaroValue;
        }


        public static int GetValueFromBoard(int row, int col)
        {
            return caroBoard[row, col];
        }

        public static bool IsEmptyCell(int row, int col)
        {
            return (caroBoard[row, col] <= 0);
        }

        public static string HistoryToText()
        {
            //let text = historyPlayed.reduce((accu_text, point) => {
            //return accu_text + `[${ point.row}, ${ point.column}: ${ point.evalValue}: ${ point.playerValue}]; `
            //        }, 
            //    '')

            string accText = string.Empty;
            if (HistoryMoves != null)
            {
                return HistoryMoves.Aggregate(accText, (accText, point) => { return accText + string.Format("[{0},{1}:{2}:{3}]; ", point.Cell.Row, point.Cell.Column, point.CaroValue, point.EvalValue); });
            }
            return accText;
        }
    }
}
