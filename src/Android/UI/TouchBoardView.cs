using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;

namespace Bejeweled3Accessible.AndroidApp.UI
{
    public class TouchBoardView : View
    {
        private Board _board;
        private TalkBackBridge _talkBack;
        private int _cursorX = 3, _cursorY = 3;
        private int _selectedX = -1, _selectedY = -1;
        private float _startX, _startY;
        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias);

        private readonly Dictionary<GemColor, Color> _gemColors = new Dictionary<GemColor, Color>
        {
            { GemColor.Red, Color.Rgb(220, 20, 60) },
            { GemColor.Yellow, Color.Rgb(255, 215, 0) },
            { GemColor.Green, Color.Rgb(50, 205, 50) },
            { GemColor.Blue, Color.Rgb(30, 144, 255) },
            { GemColor.Purple, Color.Rgb(147, 112, 219) },
            { GemColor.White, Color.Rgb(245, 245, 245) },
            { GemColor.Orange, Color.Rgb(255, 140, 0) }
        };

        public TouchBoardView(Context context, Board board, TalkBackBridge talkBack) : base(context)
        {
            _board = board;
            _talkBack = talkBack;
            Focusable = true;
            Clickable = true;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            canvas.DrawColor(Color.Rgb(20, 20, 35));

            int tileSize = Math.Min(Width / Board.Cols, Height / Board.Rows);
            int offsetX = (Width - (tileSize * Board.Cols)) / 2;
            int offsetY = (Height - (tileSize * Board.Rows)) / 2;

            for (int y = 0; y < Board.Rows; y++)
            {
                for (int x = 0; x < Board.Cols; x++)
                {
                    int left = offsetX + (x * tileSize) + 4;
                    int top = offsetY + (y * tileSize) + 4;
                    int right = left + tileSize - 8;
                    int bottom = top + tileSize - 8;
                    RectF rect = new RectF(left, top, right, bottom);

                    _paint.Color = Color.Argb(40, 255, 255, 255);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(rect, 8, 8, _paint);

                    Gem gem = _board.GetGem(x, y);
                    if (gem != null)
                    {
                        Color c = _gemColors.ContainsKey(gem.Color) ? _gemColors[gem.Color] : Color.Gray;
                        _paint.Color = c;
                        _paint.SetStyle(Paint.Style.Fill);
                        canvas.DrawCircle(rect.CenterX(), rect.CenterY(), (tileSize - 14) / 2f, _paint);
                    }

                    if (_selectedX == x && _selectedY == y)
                    {
                        _paint.Color = Color.Lime;
                        _paint.SetStyle(Paint.Style.Stroke);
                        _paint.StrokeWidth = 6;
                        canvas.DrawRoundRect(rect, 8, 8, _paint);
                    }
                    else if (_cursorX == x && _cursorY == y)
                    {
                        _paint.Color = Color.Yellow;
                        _paint.SetStyle(Paint.Style.Stroke);
                        _paint.StrokeWidth = 4;
                        canvas.DrawRoundRect(rect, 8, 8, _paint);
                    }
                }
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            int tileSize = Math.Min(Width / Board.Cols, Height / Board.Rows);
            int offsetX = (Width - (tileSize * Board.Cols)) / 2;
            int offsetY = (Height - (tileSize * Board.Rows)) / 2;

            int cellX = (int)((e.GetX() - offsetX) / tileSize);
            int cellY = (int)((e.GetY() - offsetY) / tileSize);

            if (e.Action == MotionEventActions.Down)
            {
                _startX = e.GetX();
                _startY = e.GetY();

                if (cellX >= 0 && cellX < Board.Cols && cellY >= 0 && cellY < Board.Rows)
                {
                    _cursorX = cellX;
                    _cursorY = cellY;

                    if (_selectedX >= 0 && _selectedY >= 0)
                    {
                        int dx = cellX - _selectedX;
                        int dy = cellY - _selectedY;
                        if (Math.Abs(dx) + Math.Abs(dy) == 1)
                        {
                            _board.SwapGems(_selectedX, _selectedY, cellX, cellY);
                            _board.ProcessMatchesAndGravity(false, false, false, false);
                            _selectedX = -1;
                            _selectedY = -1;
                            Invalidate();
                            return true;
                        }
                    }

                    _selectedX = cellX;
                    _selectedY = cellY;
                    AnnounceCell(cellX, cellY);
                    Invalidate();
                }
            }
            else if (e.Action == MotionEventActions.Up)
            {
                float dx = e.GetX() - _startX;
                float dy = e.GetY() - _startY;
                if (Math.Abs(dx) > 40 || Math.Abs(dy) > 40)
                {
                    int swapDx = Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? 1 : -1) : 0;
                    int swapDy = Math.Abs(dx) > Math.Abs(dy) ? 0 : (dy > 0 ? 1 : -1);
                    int targetX = _cursorX + swapDx;
                    int targetY = _cursorY + swapDy;

                    if (targetX >= 0 && targetX < Board.Cols && targetY >= 0 && targetY < Board.Rows)
                    {
                        _board.SwapGems(_cursorX, _cursorY, targetX, targetY);
                        _board.ProcessMatchesAndGravity(false, false, false, false);
                        _selectedX = -1;
                        _selectedY = -1;
                        _cursorX = targetX;
                        _cursorY = targetY;
                        AnnounceCell(_cursorX, _cursorY);
                        Invalidate();
                    }
                }
            }
            return true;
        }

        private void AnnounceCell(int x, int y)
        {
            Gem g = _board.GetGem(x, y);
            string col = ((char)('A' + x)).ToString();
            int row = y + 1;
            string desc = g != null ? string.Format("{0}{1}: {2}", col, row, g.GetNameLocalized()) : string.Format("{0}{1}: Vacio", col, row);

            List<KeyValuePair<int, int>> moves = HintFinder.GetValidMovesFrom(_board, x, y);
            if (moves.Count > 0)
            {
                List<string> dirs = new List<string>();
                foreach (var m in moves)
                {
                    if (m.Key == 1) dirs.Add("derecha");
                    else if (m.Key == -1) dirs.Add("izquierda");
                    else if (m.Value == 1) dirs.Add("abajo");
                    else if (m.Value == -1) dirs.Add("arriba");
                }
                desc += ". Mover a " + string.Join(" o ", dirs);
            }
            _talkBack?.Speak(desc, true);
        }
    }
}