using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static Caro.NET.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace Caro.NET
{
    public partial class CaroForm : Form
    {

        private Brush brushCellEven = new SolidBrush(Color.FromArgb(255, 218, 218, 218));
        private Brush brushCellOdd = new SolidBrush(Color.FromArgb(255, 240, 240, 240));

        private CaroMove LatestMoved { get; set; }

        private int CurrentMoveIndex { get; set; }
        private bool IsWaiting { get; set; }
        private GameStatus CaroGameStatus { get; set; }

        private bool IsComputerPlaying { get; set; }
        private static bool IsServerRunning { get; set; }

        private Thread threadHTTPServer;
        private HttpListener httpListener;
        private Task PlayerX;
        private Task PlayerO;

        private CaroAI caroAI;
        private CaroAI_Minimax caroAIMinimax;

        public CaroForm()
        {
            InitializeComponent();
            gridCaro.Top = Utils.BOARD_TOP + 30;
            gridCaro.Left = Utils.BOARD_LEFT + 200;
            gridCaro.Width = 2 * Utils.BOARD_TOP + Utils.MAX_COLUMNS * Utils.CELL_SIZE + 2;
            gridCaro.Height = 2 * Utils.BOARD_LEFT + Utils.MAX_ROWS * Utils.CELL_SIZE + 2;
            gridCaro.BorderStyle = BorderStyle.FixedSingle;

            NewGame();
        }

        private void gridCaro_Paint(object sender, PaintEventArgs e)
        {
            DrawBoard(e.Graphics);
            DrawMoves(e.Graphics, CaroBoard.HistoryMoves);
        }

        /*
        private void caroGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var relativePoint = this.PointToClient(Cursor.Position);

            int x = relativePoint.X - caroGrid.Left - Utils.BOARD_LEFT;
            int y = relativePoint.Y - caroGrid.Top - Utils.BOARD_TOP;

            int cellColumn = x / Utils.CELL_SIZE;
            int cellRow = y / Utils.CELL_SIZE;

            int cellCenterX = cellColumn * Utils.CELL_SIZE + Utils.CELL_SIZE / 2;
            int cellCenterY = cellRow * Utils.CELL_SIZE + Utils.CELL_SIZE / 2;


            lblMouse.Text = String.Format("Mouse. X:{0} , Y:{1}", x, y);
            label1.Text = String.Format("Cell Center Point. X:{0} , Y:{1}", cellCenterX, cellCenterY);
            label2.Text = String.Format("Cell. ROW:{0} , COLUMN:{1}", cellRow, cellColumn);
        }
        */


        private void NewGame()
        {
            caroAI = new CaroAI();
            caroAIMinimax = new CaroAI_Minimax();

            CaroBoard.ClearBoard();
            CaroBoard.HistoryMoves = new List<CaroMove>();
            LatestMoved = new CaroMove();
            CaroGameStatus = GameStatus.New;
            CurrentMoveIndex = -1;
            IsWaiting = false;
            IsComputerPlaying = false;
            gridCaro.Refresh();
        }

        private void DrawAxis(Graphics g)
        {
            Brush brushText = new SolidBrush(Color.Gray);
            int top = gridCaro.Top;
            int left = gridCaro.Left;

            // Set format of string.
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.DirectionRightToLeft;

            for (int row = 0; row < Utils.MAX_ROWS; row++)
            {
                g.DrawString(row.ToString(), gridCaro.Font, brushText, left - 10, top + 6 + 2 * Utils.CELL_PADDING + row * Utils.CELL_SIZE, drawFormat);
            }

            for (int col = 0; col < Utils.MAX_COLUMNS; col++)
            {
                g.DrawString(col.ToString(), gridCaro.Font, brushText, left + 6 + 2 * Utils.CELL_PADDING + col * Utils.CELL_SIZE, top - 25);
            }
        }
        private void DrawBoard(Graphics g)
        {
            // Create a new pen.
            Pen penGrid = new Pen(Color.LightGray);
            Pen penBorder = new Pen(Brushes.DarkGray);

            // Set the pen's width.
            penGrid.Width = 1F;
            penBorder.Width = 3F;

            // Set the LineJoin property.
            penGrid.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;


            // draw grid
            for (int row = 0; row < Utils.MAX_ROWS; row++)
            {
                for (int col = 0; col < Utils.MAX_COLUMNS; col++)
                {
                    Rect rect = GetCellRectangle(row, col);
                    if ((row + col) % 2 == 0)
                        g.FillRectangle(brushCellEven, rect.X, rect.Y, rect.Width, rect.Height);
                    else
                        g.FillRectangle(brushCellOdd, rect.X, rect.Y, rect.Width, rect.Height);
                }
                g.DrawLine(penGrid, Utils.BOARD_LEFT, Utils.BOARD_TOP + row * Utils.CELL_SIZE, Utils.BOARD_TOP + Utils.MAX_COLUMNS * Utils.CELL_SIZE, Utils.BOARD_TOP + row * Utils.CELL_SIZE);
            }

            for (int col = 1; col < Utils.MAX_COLUMNS; col++)
            {
                g.DrawLine(penGrid, Utils.BOARD_LEFT + col * Utils.CELL_SIZE, Utils.BOARD_TOP, Utils.BOARD_LEFT + col * Utils.CELL_SIZE, Utils.BOARD_LEFT + Utils.MAX_ROWS * Utils.CELL_SIZE);
            }

            // draw borders
            g.DrawRectangle(penBorder, Utils.BOARD_LEFT - 1, Utils.BOARD_TOP - 1, 2 + Utils.MAX_COLUMNS * Utils.CELL_SIZE, 2 + Utils.MAX_ROWS * Utils.CELL_SIZE);

            penGrid.Dispose();
            penBorder.Dispose();
            //brushCellEven.Dispose();
            //brushCellOdd.Dispose();
        }

        private void DrawMoves(Graphics g, List<CaroMove>? caroMoves)
        {
            if (caroMoves != null)
            {
                int index = 0;
                foreach (CaroMove move in caroMoves)
                {
                    if (index <= CurrentMoveIndex)
                        DrawMove(g, move, false, false);
                    else
                        return;

                    index++;
                }
            }
        }

        private void DrawEvaluatedValue()
        {
            //gridCaro.Refresh();
            Graphics g = gridCaro.CreateGraphics();
            Brush brushText = new SolidBrush(Color.Black);
            for (int row = 0; row < MAX_ROWS; row++)
            {
                for (int column = 0; column < MAX_COLUMNS; column++)
                {
                    if (CaroBoard.GetEvaluatedValueFromBoard(row, column) > 0)
                    {
                        Rect rect = GetCellRectangle(row, column);
                        g.DrawString(CaroBoard.GetEvaluatedValueFromBoard(row, column).ToString(), gridCaro.Font, brushText, rect.X, rect.Y);
                    }
                }
            }
            g.Dispose();
        }

        //private Color GetColorAt(int x, int y)
        //{
        //    Bitmap bmp = new Bitmap(1, 1);
        //    Rectangle bounds = new Rectangle(x, y, 1, 1);
        //    using (Graphics g = Graphics.FromImage(bmp))
        //        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        //    return bmp.GetPixel(0, 0);
        //}

        private void DrawMove(Graphics g, CaroMove move, bool refresh, bool highLight)
        {
            Rect rect = GetInnerCellRectangle(move.Cell);
            if (refresh)
            {
                //Brush brush = new SolidBrush(GetColorAt(rect.X + 3, rect.Y + 3));
                if ((move.Cell.Row + move.Cell.Column) % 2 == 0)
                {
                    //brush = new SolidBrush(Color.FromArgb(228, 228, 228));
                    g.FillRectangle(brushCellEven, rect.X, rect.Y, rect.Width, rect.Height);
                    g.FillRectangle(brushCellEven, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    //brush = new SolidBrush(Color.FromArgb(240, 240, 240));
                    g.FillRectangle(brushCellOdd, rect.X, rect.Y, rect.Width, rect.Height);
                    g.FillRectangle(brushCellOdd, rect.X, rect.Y, rect.Width, rect.Height);
                }
                //brush.Dispose();
            }

            if (highLight)
            {
                Brush brush = new SolidBrush(Color.FromArgb(128, 255, 255, 0));
                g.FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
                brush.Dispose();
            }
            g.DrawImage(move.CaroValue == Utils.CARO_X ? picX.Image : picO.Image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        private void AddMoveToHistory(CaroMove move)
        {
            CaroBoard.AddMoveToHistory(move);
            LatestMoved = move;
            CurrentMoveIndex++;
        }

        private CaroMove? GetCaroMoveFromCursor(bool nextPlayer = true)
        {
            var relativePoint = this.PointToClient(Cursor.Position);

            int x = relativePoint.X - gridCaro.Left - Utils.BOARD_LEFT;
            int y = relativePoint.Y - gridCaro.Top - Utils.BOARD_TOP;

            CaroCell caroCell = new CaroCell(y / Utils.CELL_SIZE, x / Utils.CELL_SIZE);

            // only calculate in the grid, not on grid lines
            if ((x >= 0 && y >= 0) && (caroCell.Row < Utils.MAX_ROWS && caroCell.Column < Utils.MAX_COLUMNS) && (x % Utils.CELL_SIZE != 0 && y % Utils.CELL_SIZE != 0))
            {
                CaroMove nextMove = new CaroMove();
                nextMove.Cell = caroCell;
                nextMove.CaroValue = nextPlayer ? Utils.GetOpponentCaroValue(LatestMoved.CaroValue) : Utils.CARO_EVAL_UNDEF; // next player value or UNDEF
                nextMove.EvalValue = Utils.CARO_EVAL_HUMAN;

                return nextMove;
            }

            return null;
        }


        private Rect GetCellRectangle(int row, int column)
        {
            return new Rect(Utils.BOARD_LEFT + column * Utils.CELL_SIZE, Utils.BOARD_TOP + row * Utils.CELL_SIZE, Utils.CELL_SIZE, Utils.CELL_SIZE);
        }

        private Rect GetCellRectangle(CaroCell cell)
        {
            return GetCellRectangle(cell.Row, cell.Column);
        }

        private Rect GetInnerCellRectangle(CaroCell cell)
        {
            return new Rect(Utils.BOARD_LEFT + Utils.CELL_PADDING + cell.Column * Utils.CELL_SIZE, Utils.BOARD_TOP + Utils.CELL_PADDING + cell.Row * Utils.CELL_SIZE, Utils.CELL_SIZE - Utils.CELL_PADDING * 2, Utils.CELL_SIZE - Utils.CELL_PADDING * 2);
        }


        private bool CheckWinLatestMove()
        {
            int[,] board = CaroBoard.GetCaroBoard();
            var possibleWin = caroAI.CheckWin(LatestMoved.Cell.Row, LatestMoved.Cell.Column, LatestMoved.CaroValue, board);
            if (possibleWin != null)
            {
                CaroGameStatus = GameStatus.Over;
                Graphics g = gridCaro.CreateGraphics();
                foreach (var item in possibleWin.Take(5))
                {
                    int r = item.Value.Row;
                    int c = item.Value.Col;
                    DrawMove(g, new CaroMove(r, c, board[r, c], 0), true, true);
                }
                g.Dispose();

                MessageBox.Show(String.Format("Player {0} won!", Utils.CaroValueToText(LatestMoved.CaroValue)), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            return false;
        }

        private bool ManualMove(CaroMove? caroMove)
        {
            // if the cell is empty, add new move to the grid
            if (caroMove != null && CaroBoard.IsEmptyCell(caroMove.Value.Cell.Row, caroMove.Value.Cell.Column)) // if (!playedMoves.Exists(p => (p.Row == cellRow && p.Col == cellCol)))
            {
                if (CaroGameStatus == GameStatus.New)
                    CaroGameStatus = GameStatus.Playing;

                gridCaro.Refresh();

                // check it is first played
                if (CaroBoard.FirstMoved == null)
                    CaroBoard.FirstMoved = caroMove.Value;

                CaroBoard.PutValueIntoBoard(caroMove.Value.Cell.Row, caroMove.Value.Cell.Column, caroMove.Value.CaroValue);

                // update history playing
                LatestMoved = caroMove.Value;
                AddMoveToHistory(LatestMoved);

                // drawing
                Graphics g = gridCaro.CreateGraphics();
                DrawMove(g, caroMove.Value, false, true);
                g.Dispose();
                CheckWinLatestMove();
                return true;
            }
            return false;
        }

        private CaroMove PCMoveTask()
        {
            IsComputerPlaying = true;
            CaroMove nextMove = new CaroMove();
            Task task = Task.Run(() =>
            {
                if (gridCaro.IsDisposed)
                    return;

                Graphics g = gridCaro.CreateGraphics();
                nextMove = FindBestMove(LatestMoved.CaroValue, Utils.GetOpponentCaroValue(LatestMoved.CaroValue));
                if (nextMove.EvalValue > Utils.CARO_EVAL_UNDEF)
                {
                    lock (this)
                    {
                        CaroBoard.PutValueIntoBoard(nextMove.Cell.Row, nextMove.Cell.Column, nextMove.CaroValue);

                        // refresh Old Move Cell
                        if (LatestMoved.EvalValue > Utils.CARO_EVAL_UNDEF)
                        {
                            Thread.Sleep(200);
                            DrawMove(g, LatestMoved, true, false);
                        }

                        // update history playing
                        LatestMoved = nextMove;
                        AddMoveToHistory(LatestMoved);

                        //drawing

                        DrawMove(g, nextMove, false, true);
                    }
                }
                g.Dispose();
            });
            task.Wait();
            IsComputerPlaying = false;
            //Debug.WriteLine("Task: PC Move. DONE.");
            return nextMove;
        }


        private CaroMove PCMove(Graphics g)
        {
            IsComputerPlaying = true;
            CaroMove nextMove = FindBestMove(LatestMoved.CaroValue, Utils.GetOpponentCaroValue(LatestMoved.CaroValue));
            if (nextMove.EvalValue > Utils.CARO_EVAL_UNDEF)
            {
                if (CaroBoard.FirstMoved != null && CaroBoard.SecondMoved == null)
                {
                    CaroBoard.SecondMoved = nextMove;
                }

                CaroBoard.PutValueIntoBoard(nextMove.Cell.Row, nextMove.Cell.Column, nextMove.CaroValue);

                // update history playing
                CaroMove saved = LatestMoved;
                LatestMoved = nextMove;
                AddMoveToHistory(LatestMoved);

                //drawing
                //saved.CaroValue = Utils.GetOpponentCaroValue(saved.CaroValue);
                Thread.Sleep(500);
                DrawMove(g, saved, true, false);
                DrawMove(g, nextMove, false, true);
                CheckWinLatestMove();

                //if (caroAI.CheckWin(LatestMoved.Cell.Row, LatestMoved.Cell.Column, LatestMoved.CaroValue, CaroBoard.GetCaroBoard()))
                //{
                //    CaroGameStatus = GameStatus.Over;
                //    MessageBox.Show(String.Format("Player {0} won!", Utils.CaroValueToText(LatestMoved.CaroValue)), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //}
            }
            IsComputerPlaying = false;

            return nextMove;
        }

        private void gridCaro_MouseClick(object sender, MouseEventArgs e)
        {
            if (IsWaiting)
            {
                IsWaiting = false;
                return;
            }
            IsWaiting = true;

            if (IsComputerPlaying)
            {
                return;
            }

            if (!radPCvsPC.Checked)
            {
                CaroMove? caroMove = GetCaroMoveFromCursor(true);
                if (ManualMove(caroMove) && CaroGameStatus != GameStatus.Over)
                {
                    if (radPCvsHuman.Checked || radHumanPC.Checked)
                    {
                        gridCaro.Refresh();
                        CaroBoard.ClearEvaluatedBoard();
                        PCMoveTask();
                        DrawEvaluatedValue();
                    }
                    else if (radHuman.Checked) 
                    {
                        caroMove = GetCaroMoveFromCursor(true);
                        ManualMove(caroMove);
                    }
                    CheckWinLatestMove();
                }
            }
            IsWaiting = false;
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            NewGame();
            //int[,] newBoard = CaroBoard.CloneBoard();
            //CaroBoard.PutValueIntoBoard(3, 3, 3);
            Debug.WriteLine("Restart");
            if (radPCvsPC.Checked)
            {
                radPCvsPC_CheckedChanged(radPCvsPC, new EventArgs());
            }

        }

        private CaroMove FindBestMove(int playerCaroValue, int opponentCaroValue)
        {
            CaroMove newMove = new CaroMove();
            int[,] chessBoard = CaroBoard.GetCaroBoard();

            CaroMove? firstMoved = CaroBoard.FirstMoved;
            CaroMove? secondMoved = CaroBoard.SecondMoved;
            List<CaroCell> availablePositions = new List<CaroCell>();

            if (firstMoved == null)
            {
                newMove.EvalValue = Utils.CARO_EVAL_FIRST;
                newMove.Cell = new CaroCell(Utils.MAX_ROWS / 2, Utils.MAX_COLUMNS / 2);
                newMove.CaroValue = Utils.GetOpponentCaroValue(LatestMoved.CaroValue);
                CaroBoard.FirstMoved = newMove;
            }
            else if (secondMoved == null) // firstMoved != null && 
            {
                // calculate the secondMoved Value
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    { // (i !== 0 && j !== 0) because the 2nd moving should in the 4 corner (diagonal)
                        if ((i != 0 || j != 0) && (firstMoved.Value.Cell.Row + i >= 0 && firstMoved.Value.Cell.Row + i < Utils.MAX_ROWS && firstMoved.Value.Cell.Column + j >= 0 && firstMoved.Value.Cell.Column + j < Utils.MAX_COLUMNS))
                        {
                            availablePositions.Add(new CaroCell(firstMoved.Value.Cell.Row + i, firstMoved.Value.Cell.Column + j));
                        }
                    }
                }
                CaroCell availablePosition = availablePositions[new Random().Next(availablePositions.Count)];
                newMove.EvalValue = Utils.CARO_EVAL_SECOND;
                newMove.Cell = availablePosition;
                newMove.CaroValue = Utils.GetOpponentCaroValue(LatestMoved.CaroValue);
                CaroBoard.SecondMoved = newMove;
            }
            else
            {
                Move? aiMove = caroAI.FindBestMoveAggressive_V6_Lookahead(chessBoard, opponentCaroValue);
                //Move? aiMove = caroAIMinimax.FindBestMove(chessBoard, Utils.CARO_X, 3);
                if (aiMove != null)
                {
                    newMove.Cell.Column = aiMove.Value.Col;
                    newMove.Cell.Row = aiMove.Value.Row;
                    newMove.EvalValue = 0;
                    newMove.CaroValue = Utils.GetOpponentCaroValue(LatestMoved.CaroValue);
                }
            }
            return newMove;
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            if (IsComputerPlaying)
                return;

            string txt = CaroBoard.HistoryToText();
            if (!String.IsNullOrEmpty(txt))
            {
                FileStream file;
                if (File.Exists("history.txt"))
                    File.Delete("history.txt");

                file = File.OpenWrite("history.txt");
                byte[] data = new UTF8Encoding(true).GetBytes(txt);
                file.Write(data, 0, data.Length);
                file.Close();
            }
            else
            {
                MessageBox.Show("Nothing to save!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (IsComputerPlaying)
                return;

            StreamReader file = File.OpenText("history.txt");
            string txt = file.ReadToEnd();
            file.Close();

            string[] txtMoves = txt.Split(";");

            if (txtMoves.Length > 0)
            {
                NewGame();
                CaroGameStatus = GameStatus.Over;

                if (txtMoves[txtMoves.Length - 1].IndexOf("]") == -1)
                    txtMoves = txtMoves[..^1]; // all items in the array except the last -> remove last item

                Graphics g = gridCaro.CreateGraphics();

                //List <CaroMove> listMoves = new List<CaroMove>();
                foreach (string move in txtMoves)
                {
                    string[] strValues = (move.Replace(" ", "").Replace("[", "").Replace("]", "").Replace(":", ",")).Split(",");
                    int[] values = strValues.Select(int.Parse).ToArray();
                    CaroMove caroMove = new CaroMove(values[0], values[1], values[2], values[3]);

                    if (CaroBoard.FirstMoved == null)
                        CaroBoard.FirstMoved = caroMove;
                    else if (CaroBoard.SecondMoved == null)
                        CaroBoard.SecondMoved = caroMove;

                    CaroBoard.PutValueIntoBoard(caroMove);
                    AddMoveToHistory(caroMove);

                    DrawMove(g, caroMove, false, true);
                    Thread.Sleep(100);
                    if (txtMoves.Last() != move)
                        gridCaro.Refresh();
                }
                g.Dispose();
                CaroGameStatus = GameStatus.Playing;
            }
            else
            {
                MessageBox.Show("Nothing to load!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void radPCvsPC_CheckedChanged(object sender, EventArgs e)
        {
            if (radPCvsPC.Checked)
            {
                if (CaroGameStatus == GameStatus.Over)
                {
                    NewGame();
                }
                IsComputerPlaying = true;
                if (CaroGameStatus == GameStatus.New)
                    CaroGameStatus = GameStatus.Playing;
                do
                {
                    PCMoveTask();
                    Thread.Sleep(100);
                    System.Windows.Forms.Application.DoEvents();
                    CheckWinLatestMove();
                } while (CaroGameStatus == GameStatus.Playing && LatestMoved.EvalValue > Utils.CARO_EVAL_UNDEF);


                //Graphics g = gridCaro.CreateGraphics();

                //CaroMove move = FindBestMove(Utils.CARO_X, Utils.CARO_O);
                //while (move.EvalValue > Utils.CARO_EVAL_UNDEF)
                //{
                //    CaroBoard.PutValueIntoBoard(move.Cell.Row, move.Cell.Column, move.CaroValue);
                //    // update history moving
                //    LatestMoved = move;
                //    AddMoveToHistory(LatestMoved);

                //    //drawing
                //    Thread.Sleep(300);
                //    gridCaro.Refresh();
                //    DrawMove(g, move, false, true);

                //    if (caroAI.CheckWin(move.Cell.Row, move.Cell.Column, LatestMoved.CaroValue, CaroBoard.GetCaroBoard()))
                //    {
                //        IsGameOver = true;
                //        IsComputerPlaying = false;
                //        MessageBox.Show(String.Format("Player {0} won!", Utils.CaroValueToText(LatestMoved.CaroValue)), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //        break;
                //    }

                //    move = FindBestMove(LatestMoved.CaroValue, Utils.GetOpponentCaroValue(LatestMoved.CaroValue));
                //}

                //g.Dispose();
            }
        }

        private void radPCvsHuman_CheckedChanged(object sender, EventArgs e)
        {
            if (radPCvsHuman.Checked)
            {
                // next move
                PCMoveTask();
                CheckWinLatestMove();
            }
        }
        private void radHumanPC_CheckedChanged(object sender, EventArgs e)
        {
            if (!radHumanPC.Checked)
            {
            }
        }

        private void radHuman_CheckedChanged(object sender, EventArgs e)
        {
            if (radHuman.Checked)
            {
            }
        }

        private void CaroForm_Paint(object sender, PaintEventArgs e)
        {
            DrawAxis(e.Graphics);
        }

        private void btnFirst_Click(object sender, EventArgs e)
        {
            if (CaroGameStatus == GameStatus.Over)
            {
                CaroBoard.ClearBoard();
                CurrentMoveIndex = 0;
                if (CaroBoard.HistoryMoves != null)
                {
                    CaroBoard.PutValueIntoBoard(CaroBoard.HistoryMoves[CurrentMoveIndex]);
                }
                gridCaro.Refresh();
            }
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (CaroGameStatus == GameStatus.Over)
            {

                if (CurrentMoveIndex >= 0)
                    CurrentMoveIndex--;

                gridCaro.Refresh();
            }

            if (CaroGameStatus == GameStatus.Stop)
            {
                if (CurrentMoveIndex >= 0 && CaroBoard.HistoryMoves != null)
                {
                    LatestMoved = CaroBoard.RemoveLatestMoveFromHistory();
                    CurrentMoveIndex--;
                }

                gridCaro.Refresh();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (CaroGameStatus == GameStatus.Over)
            {
                if (CaroBoard.HistoryMoves != null && CurrentMoveIndex < CaroBoard.HistoryMoves.Count)
                    CurrentMoveIndex++;

                gridCaro.Refresh();
            }

        }

        private void btnLast_Click(object sender, EventArgs e)
        {
            if (CaroGameStatus == GameStatus.Over)
            {
                CurrentMoveIndex = CaroBoard.HistoryMoves != null ? CaroBoard.HistoryMoves.Count - 1 : -1;
                
            }
        }

        private void btnSuggest_Click(object sender, EventArgs e)
        {
            if (IsComputerPlaying)
                return;
            if (CaroGameStatus == GameStatus.Stop)
                CaroGameStatus = GameStatus.Playing;

            gridCaro.Refresh();
            CaroBoard.ClearEvaluatedBoard();

            PCMoveTask();

            DrawEvaluatedValue();
            CheckWinLatestMove();
        }


        private string GetRequestData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                Debug.WriteLine("No client data was sent with the request.");
                return String.Empty;
            }
            System.IO.Stream body = request.InputStream;
            System.Text.Encoding encoding = request.ContentEncoding;
            System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
            if (request.ContentType != null)
            {
                Debug.WriteLine("Client data content type {0}", request.ContentType);
            }
            Debug.WriteLine("Client data content length {0}", request.ContentLength64);

            Debug.WriteLine("Start of client data:");
            // Convert the data to a string and display it on the console.
            string s = reader.ReadToEnd();
            Debug.WriteLine(s);
            Debug.WriteLine("End of client data:");
            body.Close();
            reader.Close();
            return s;
        }
        private void ThreadHTTPServer()
        {
            if (httpListener == null)
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://+:5678/");
            }

            try
            {
                if (!httpListener.IsListening)
                {
                    httpListener.Start();
                    Debug.WriteLine("Listening for connections on http://+:5678/");
                    IsServerRunning = true;
                }

                while (IsServerRunning)
                {
                    try
                    {
                        HttpListenerContext context = httpListener.GetContext(); // Note: The GetContext method blocks while waiting for a request.
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if (request != null && request.HttpMethod == "POST")
                        {
                            //MessageBox.Show(GetRequestData(request));
                            //CaroMove caroMove = new CaroMove();
                            PCMoveTask();
                        }

                        // Write a respone back to the client
                        string responseString = "<html><body><h1>Hello world!</h1></body></html>";
                        byte[] buff = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buff.Length;
                        System.IO.Stream output = response.OutputStream;
                        output.Write(buff, 0, buff.Length);
                        output.Close();
                        output.Dispose();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
            catch (ThreadAbortException te)
            {
                MessageBox.Show(te.Message, "ThreadAbortException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (HttpListenerException he)
            {
                //MessageBox.Show(he.Message, "HttpListenerException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (httpListener != null && httpListener.IsListening)
                    httpListener.Stop();
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (threadHTTPServer == null)
            {
                btnStartStop.Text = "   Stop";
                threadHTTPServer = new Thread(ThreadHTTPServer);
                threadHTTPServer.Start();
            }
            else
            {
                IsServerRunning = false;
                if (httpListener != null && httpListener.IsListening)
                {
                    httpListener.Stop();
                    httpListener = null;
                }

                btnStartStop.Text = "   Start";
                threadHTTPServer.Interrupt();
                threadHTTPServer = null;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            CaroGameStatus = GameStatus.Stop;
        }

        private void CaroForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CaroGameStatus == GameStatus.Playing)
            {
                CaroGameStatus = GameStatus.Over;
                Thread.Sleep(700);
            }
        }
    }
}