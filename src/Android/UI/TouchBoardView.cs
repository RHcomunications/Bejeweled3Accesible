using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Bejeweled3Accessible.Audio;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;
using Bejeweled3Accessible.AndroidApp.Audio;

namespace Bejeweled3Accessible.AndroidApp.UI
{
    public class TouchBoardView : View
    {
        private Board _board;
        private TalkBackBridge _talkBack;
        private AndroidSoundEngine _sound;
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

        private class TeardropSplash
        {
            public int Col;
            public int Row;
            public int StartMs;
            public int DurationMs = 260;
        }
        private readonly List<TeardropSplash> _teardrops = new List<TeardropSplash>();

        public TouchBoardView(Context context, Board board, TalkBackBridge talkBack, AndroidSoundEngine sound) : base(context)
        {
            _board = board;
            _talkBack = talkBack;
            _sound = sound;
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

                    // Dibujar flechas de movimientos validos para jugadores normovisuales
                    if ((x == _cursorX && y == _cursorY) || (_selectedX == x && _selectedY == y))
                    {
                        List<KeyValuePair<int, int>> validMoves = HintFinder.GetValidMovesFrom(_board, x, y);
                        foreach (var m in validMoves)
                        {
                            DrawArrow(canvas, rect, m.Key, m.Value);
                        }
                    }
                }
            }

            // Efecto visual "lagrima": salpicaduras transitorias en las celdas que regeneran
            long nowDrop = Java.Lang.JavaSystem.CurrentTimeMillis();
            for (int i = _teardrops.Count - 1; i >= 0; i--)
            {
                TeardropSplash t = _teardrops[i];
                long age = nowDrop - t.StartMs;
                if (age < 0) continue;
                if (age > t.DurationMs) { _teardrops.RemoveAt(i); continue; }
                float p = (float)age / t.DurationMs;
                float cx = offsetX + (t.Col * tileSize) + tileSize / 2f;
                float cy = offsetY + (t.Row * tileSize) + tileSize / 2f;
                int alpha = (int)(200f * (1f - p));
                float r = 4f + 14f * p;
                _paint.Color = Color.Argb(alpha, 130, 200, 255);
                _paint.SetStyle(Paint.Style.Fill);
                canvas.DrawCircle(cx, cy - (10f * p), r, _paint);
            }
        }

        private void DrawArrow(Canvas canvas, RectF rect, int dx, int dy)
        {
            _paint.Color = Color.Yellow;
            _paint.SetStyle(Paint.Style.Fill);

            Android.Graphics.Path path = new Android.Graphics.Path();
            float cx = rect.CenterX();
            float cy = rect.CenterY();
            float arrowSize = 14f;

            if (dx == 1) // Derecha
            {
                path.MoveTo(rect.Right - 4, cy);
                path.LineTo(rect.Right - 4 - arrowSize, cy - arrowSize / 2);
                path.LineTo(rect.Right - 4 - arrowSize, cy + arrowSize / 2);
            }
            else if (dx == -1) // Izquierda
            {
                path.MoveTo(rect.Left + 4, cy);
                path.LineTo(rect.Left + 4 + arrowSize, cy - arrowSize / 2);
                path.LineTo(rect.Left + 4 + arrowSize, cy + arrowSize / 2);
            }
            else if (dy == 1) // Abajo
            {
                path.MoveTo(cx, rect.Bottom - 4);
                path.LineTo(cx - arrowSize / 2, rect.Bottom - 4 - arrowSize);
                path.LineTo(cx + arrowSize / 2, rect.Bottom - 4 - arrowSize);
            }
            else // Arriba
            {
                path.MoveTo(cx, rect.Top + 4);
                path.LineTo(cx - arrowSize / 2, rect.Top + 4 + arrowSize);
                path.LineTo(cx + arrowSize / 2, rect.Top + 4 + arrowSize);
            }
            path.Close();
            canvas.DrawPath(path, _paint);
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
                            ExecuteSwap(_selectedX, _selectedY, cellX, cellY);
                            return true;
                        }
                    }

                    _selectedX = cellX;
                    _selectedY = cellY;
                    _sound?.PlaySoundSpatial(AudioMap.Select, cellX, cellY);
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
                        ExecuteSwap(_cursorX, _cursorY, targetX, targetY);
                    }
                }
            }
            return true;
        }

        private void ExecuteSwap(int fromX, int fromY, int toX, int toY)
        {
            _board.SwapGems(fromX, fromY, toX, toY);
            CascadeResult res = _board.ProcessMatchesAndGravity(false, false, false, false);
            if (res != null && res.AnyMatched)
            {
                // Efecto "lagrima": cada gema que cae/regenera suena como una gota de
                // GemHit espaciada ~100ms y subiendo semitonos (fire-and-forget).
                int dropCount = Math.Max(1, Math.Min(res.TotalGemsDestroyed, 16));
                int cx = toX, cy = toY;
                var snd = _sound;
                System.Threading.Tasks.Task.Run(async () =>
                {
                    int levels = Math.Max(1, Math.Min(res.CascadeDepth, 7));
                    int gemIndex = 0;
                    for (int lvl = 1; lvl <= levels; lvl++)
                    {
                        int gemsThisLevel = (dropCount + levels - 1) / levels;
                        if (gemIndex + gemsThisLevel > dropCount) gemsThisLevel = dropCount - gemIndex;
                        if (gemsThisLevel < 1 && lvl == 1) gemsThisLevel = 1;
                        for (int g = 0; g < gemsThisLevel && gemIndex < dropCount; g++, gemIndex++)
                        {
                            float p = (float)System.Math.Pow(2.0, (lvl - 1 + g) / 12.0);
                            snd?.PlaySoundSpatialPitch(AudioMap.GemHit, cx, cy, p);
                            await System.Threading.Tasks.Task.Delay(100);
                        }

                        // Combo de este nivel de cadena, cada 130 ms
                        snd?.PlaySoundSpatial(AudioMap.ComboPrefix + lvl, cx, cy);
                        await System.Threading.Tasks.Task.Delay(130);
                    }
                });

                // Visual "lagrima": salpicaduras en las celdas por donde entran las gemas
                int nowMs = (int)Java.Lang.JavaSystem.CurrentTimeMillis();
                var splashCols = (res.MatchedColumns != null && res.MatchedColumns.Count > 0)
                    ? res.MatchedColumns.ToArray() : new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                for (int k = 0; k < dropCount; k++)
                {
                    int sCol = splashCols[k % splashCols.Length];
                    int sRow = (k < splashCols.Length) ? 0 : ((k / splashCols.Length) % 8);
                    _teardrops.Add(new TeardropSplash { Col = sCol, Row = sRow, StartMs = nowMs + k * 100 });
                }
                if (_teardrops.Count > 200) _teardrops.RemoveRange(0, _teardrops.Count - 200);
            }
            _selectedX = -1;
            _selectedY = -1;
            _cursorX = toX;
            _cursorY = toY;
            AnnounceCell(_cursorX, _cursorY);
            Invalidate();
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