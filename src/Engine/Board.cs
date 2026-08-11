using System;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public class CascadeResult
    {
        public bool AnyMatched;
        public int TotalGemsDestroyed;
        public int CascadeDepth;
        public int SimultaneousMatches;
        public int BasePoints;
        public int FlameCreated;
        public int StarCreated;
        public int HypercubeCreated;
        public bool HypercubeTriggered;
        public int SupernovaCreated;
        public int TimeGemsMatched;
        public int ExtraTimeSeconds;
        public bool ButterflyEscaped;
        public int ButterfliesFreed;
        public bool BombExploded;
        public int DirtCleared;
        public int RockCleared;
        public int NuggetsMined;
        public int GoldTilesConverted;
        public int BombsDestroyed;
        public int FlameDestroyed;
        public int StarDestroyed;
        public int HypercubeDestroyed;
        public bool AnnihilatorUsed;
        public List<int> MatchedColumns = new List<int>();
        public List<int> VerticalMatchedColumns = new List<int>();
        public int[] ColumnDestroyedCount = new int[8];
        public HashSet<GemColor> MatchedColors = new HashSet<GemColor>();
    }

    public class Board
    {
        public const int Rows = 8;
        public const int Cols = 8;
        private const int MAX_CASCADE_DEPTH = 40;
        private readonly Gem[,] _grid = new Gem[Rows, Cols];
        private readonly Random _rng;
        private bool _hyperSwapPending;
        private GemColor _hyperTargetColor;
        private int _hyperSwapX, _hyperSwapY;
        private bool _annihilatorPending;

        public Board(int seed)
        {
            _rng = new Random(seed);
            InitializeBoard();
        }

        public Gem GetGem(int x, int y)
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows) return null;
            return _grid[y, x];
        }

        public void SetGem(int x, int y, Gem gem)
        {
            if (x >= 0 && x < Cols && y >= 0 && y < Rows)
                _grid[y, x] = gem;
        }

        public void InitializeBoard(bool withBombs = false)
        {
            _hyperSwapPending = false;
            _annihilatorPending = false;

            // Regenerate until the board has no initial matches and at least one valid move
            for (int attempt = 0; attempt < 100; attempt++)
            {
                FillBoardWithRandomGems(withBombs);
                if (HintFinder.FindValidMove(this) != null) return;
            }

            // Last resort: force a configuration where a swap creates a match (without an existing match)
            _grid[0, 0] = new Gem(GemColor.Red);
            _grid[1, 0] = new Gem(GemColor.Red);
            _grid[2, 1] = new Gem(GemColor.Red);
        }

        private void FillBoardWithRandomGems(bool withBombs = false)
        {
            Array colors = Enum.GetValues(typeof(GemColor));
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    GemColor c;
                    bool hasMatch;
                    do
                    {
                        c = (GemColor)colors.GetValue(_rng.Next(colors.Length));

                        bool hasHorizontalMatch = (x >= 2 && _grid[y, x - 1] != null && _grid[y, x - 1].Color == c
                                                && _grid[y, x - 2] != null && _grid[y, x - 2].Color == c);

                        bool hasVerticalMatch = (y >= 2 && _grid[y - 1, x] != null && _grid[y - 1, x].Color == c
                                              && _grid[y - 2, x] != null && _grid[y - 2, x].Color == c);

                        hasMatch = hasHorizontalMatch || hasVerticalMatch;
                    }
                    while (hasMatch);

                    // Time Bomb quest: fresh boards keep a healthy bomb count so
                    // the "no moves" scramble never wipes the whole bomb field.
                    SpecialType spec = SpecialType.None;
                    if (withBombs && _rng.Next(100) < 10)
                    {
                        spec = SpecialType.Bomb;
                    }

                    _grid[y, x] = new Gem(c, spec);
                }
            }
        }

        public bool SwapGems(int x1, int y1, int x2, int y2)
        {
            if (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) != 1) return false;
            Gem g1 = GetGem(x1, y1);
            Gem g2 = GetGem(x2, y2);
            if (g1 == null || g2 == null) return false;

            // Hypercube swap handling (trigger is consumed by ProcessMatchesAndGravity)
            if (g1.Special == SpecialType.Hypercube || g2.Special == SpecialType.Hypercube)
            {
                SetGem(x1, y1, g2);
                SetGem(x2, y2, g1);

                if (g1.Special == SpecialType.Hypercube && g2.Special == SpecialType.Hypercube)
                {
                    _annihilatorPending = true;
                }
                else
                {
                    _hyperSwapPending = true;
                    _hyperTargetColor = (g1.Special == SpecialType.Hypercube) ? g2.Color : g1.Color;
                    _hyperSwapX = (g1.Special == SpecialType.Hypercube) ? x2 : x1;
                    _hyperSwapY = (g1.Special == SpecialType.Hypercube) ? y2 : y1;
                }
                return true;
            }

            SetGem(x1, y1, g2);
            SetGem(x2, y2, g1);

            if (HasAnyMatch()) return true;

            // Undo if no match
            SetGem(x1, y1, g1);
            SetGem(x2, y2, g2);
            return false;
        }

        public bool TestSwap(int x1, int y1, int x2, int y2)
        {
            if (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) != 1) return false;
            Gem g1 = GetGem(x1, y1);
            Gem g2 = GetGem(x2, y2);
            if (g1 == null || g2 == null) return false;

            if (g1.Special == SpecialType.Hypercube || g2.Special == SpecialType.Hypercube)
                return true;

            SetGem(x1, y1, g2);
            SetGem(x2, y2, g1);

            bool hasMatch = HasAnyMatch();

            SetGem(x1, y1, g1);
            SetGem(x2, y2, g2);

            return hasMatch;
        }

        private static bool IsTerrain(Gem g)
        {
            return g != null && (g.Special == SpecialType.Dirt || g.Special == SpecialType.HardRock || g.Special == SpecialType.GoldNugget);
        }

        public bool HasAnyMatch()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Gem current = _grid[y, x];
                    if (current == null || IsTerrain(current)) continue;

                    // Horizontal match of 3
                    if (x <= Cols - 3 &&
                        _grid[y, x + 1] != null && !IsTerrain(_grid[y, x + 1]) && _grid[y, x + 1].Color == current.Color &&
                        _grid[y, x + 2] != null && !IsTerrain(_grid[y, x + 2]) && _grid[y, x + 2].Color == current.Color)
                        return true;

                    // Vertical match of 3
                    if (y <= Rows - 3 &&
                        _grid[y + 1, x] != null && !IsTerrain(_grid[y + 1, x]) && _grid[y + 1, x].Color == current.Color &&
                        _grid[y + 2, x] != null && !IsTerrain(_grid[y + 2, x]) && _grid[y + 2, x].Color == current.Color)
                        return true;
                }
            }
            return false;
        }

        public CascadeResult ProcessMatchesAndGravity(bool isLightning = false, bool isButterflies = false, bool isAlchemy = false, bool withBombs = false)
        {
            CascadeResult res = new CascadeResult();
            int depth = 0;

            // Consume pending hypercube triggers set by SwapGems (only when actually swapped)
            if (_annihilatorPending)
            {
                _hyperSwapPending = false;
                _annihilatorPending = false;
                res.AnyMatched = true;
                res.HypercubeTriggered = true;
                res.AnnihilatorUsed = true;
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Cols; x++)
                    {
                        if (_grid[y, x] != null)
                        {
                            res.TotalGemsDestroyed++;
                            res.MatchedColors.Add(_grid[y, x].Color);
                            if (!res.MatchedColumns.Contains(x)) res.MatchedColumns.Add(x);
                            res.ColumnDestroyedCount[x]++;
                            _grid[y, x] = null;
                        }
                    }
                }
                ApplyGravity(isLightning, isButterflies);
            }
            else if (_hyperSwapPending)
            {
                _hyperSwapPending = false;
                res.AnyMatched = true;
                res.HypercubeTriggered = true;
                if (_grid[_hyperSwapY, _hyperSwapX] != null && _grid[_hyperSwapY, _hyperSwapX].Special == SpecialType.Hypercube)
                {
                    res.TotalGemsDestroyed++;
                    _grid[_hyperSwapY, _hyperSwapX] = null;
                }
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Cols; x++)
                    {
                        if (_grid[y, x] != null && _grid[y, x].Color == _hyperTargetColor)
                        {
                            res.TotalGemsDestroyed++;
                            res.MatchedColors.Add(_hyperTargetColor);
                            if (!res.MatchedColumns.Contains(x)) res.MatchedColumns.Add(x);
                            res.ColumnDestroyedCount[x]++;
                            _grid[y, x] = null;
                        }
                    }
                }
                ApplyGravity(isLightning, isButterflies);
            }

            while (true)
            {
                bool[,] toDestroy = new bool[Rows, Cols];
                List<Tuple<int, int, SpecialType, GemColor>> newSpecials = new List<Tuple<int, int, SpecialType, GemColor>>();
                bool foundMatchThisPass = false;

                // Check horizontal matches
                for (int y = 0; y < Rows; y++)
                {
                    int runLen = 1;
                    for (int x = 0; x < Cols; x++)
                    {
                        bool isLast = (x == Cols - 1);
                        bool isMatch = !isLast && _grid[y, x] != null && _grid[y, x + 1] != null
                                    && !IsTerrain(_grid[y, x]) && !IsTerrain(_grid[y, x + 1])
                                    && _grid[y, x].Color == _grid[y, x + 1].Color;

                        if (isMatch)
                        {
                            runLen++;
                        }
                        else
                        {
                            if (runLen >= 3)
                            {
                                foundMatchThisPass = true;
                                int startX = x - runLen + 1;
                                for (int i = startX; i <= x; i++) toDestroy[y, i] = true;

                                // Special Gem Creations (authentic Bejeweled 3)
                                // 4 in a row = Flame, 5 in a row = Hypercube,
                                // 6+ in a row = Supernova (L/T shapes below also
                                // create Supernovas).
                                GemColor c = _grid[y, startX].Color;
                                if (runLen == 4)
                                {
                                    newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(startX + 1, y, SpecialType.Flame, c));
                                    res.FlameCreated++;
                                }
                                else if (runLen == 5)
                                {
                                    newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(startX + 2, y, SpecialType.Hypercube, c));
                                    res.HypercubeCreated++;
                                }
                                else if (runLen >= 6)
                                {
                                    newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(startX + 2, y, SpecialType.Supernova, c));
                                    res.SupernovaCreated++;
                                }
                            }
                            runLen = 1;
                        }
                    }
                }

                // Check vertical matches
                for (int x = 0; x < Cols; x++)
                {
                    int runLen = 1;
                    for (int y = 0; y < Rows; y++)
                    {
                        bool isLast = (y == Rows - 1);
                        bool isMatch = !isLast && _grid[y, x] != null && _grid[y + 1, x] != null
                                    && !IsTerrain(_grid[y, x]) && !IsTerrain(_grid[y + 1, x])
                                    && _grid[y, x].Color == _grid[y + 1, x].Color;

                        if (isMatch)
                        {
                            runLen++;
                        }
                        else
                        {
                            if (runLen >= 3)
                            {
                                foundMatchThisPass = true;
                                int startY = y - runLen + 1;
                                for (int j = startY; j <= y; j++) toDestroy[j, x] = true;

                                // Ice Storm: a vertical match conceals an ice column
                                // (authentic rule: verticals shatter the ice front).
                                if (!res.VerticalMatchedColumns.Contains(x)) res.VerticalMatchedColumns.Add(x);

                                GemColor c = _grid[startY, x].Color;
                                if (runLen == 4)
                                {
                                    newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(x, startY + 1, SpecialType.Flame, c));
                                    res.FlameCreated++;
                                }
                                else if (runLen == 5)
                                {
                                    newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(x, startY + 2, SpecialType.Hypercube, c));
                                    res.HypercubeCreated++;
                                }
                                else if (runLen >= 6)
                                {
                                    newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(x, startY + 2, SpecialType.Supernova, c));
                                    res.SupernovaCreated++;
                                }
                            }
                            runLen = 1;
                        }
                    }
                }

                // Detect T / L / square shapes and create Supernova gems at the "elbow" cell
                HashSet<int> elbowCells = new HashSet<int>();
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Cols; x++)
                    {
                        if (!toDestroy[y, x] || _grid[y, x] == null) continue;
                        GemColor c = _grid[y, x].Color;

                        bool hasH = (x - 1 >= 0 && toDestroy[y, x - 1] && _grid[y, x - 1] != null && _grid[y, x - 1].Color == c)
                                 || (x + 1 < Cols && toDestroy[y, x + 1] && _grid[y, x + 1] != null && _grid[y, x + 1].Color == c);
                        bool hasV = (y - 1 >= 0 && toDestroy[y - 1, x] && _grid[y - 1, x] != null && _grid[y - 1, x].Color == c)
                                 || (y + 1 < Rows && toDestroy[y + 1, x] && _grid[y + 1, x] != null && _grid[y + 1, x].Color == c);
                        if (!hasH || !hasV) continue;

                        bool alreadyNear = false;
                        for (int dy = -1; dy <= 0 && !alreadyNear; dy++)
                        {
                            for (int dx = -1; dx <= 0; dx++)
                            {
                                int ay = y + dy;
                                int ax = x + dx;
                                if (ay < 0 || ay >= Rows || ax < 0 || ax >= Cols) continue;
                                if (elbowCells.Contains(ay * Cols + ax))
                                {
                                    alreadyNear = true;
                                    break;
                                }
                            }
                        }
                        if (alreadyNear) continue;

                        elbowCells.Add(y * Cols + x);
                        newSpecials.Add(new Tuple<int, int, SpecialType, GemColor>(x, y, SpecialType.Supernova, c));
                        res.SupernovaCreated++;
                    }
                }

                if (!foundMatchThisPass || depth >= MAX_CASCADE_DEPTH) break;

                depth++;
                res.AnyMatched = true;

                // Process special gem explosions (Flame, Star, Supernova)
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Cols; x++)
                    {
                        if (toDestroy[y, x] && _grid[y, x] != null)
                        {
                            res.MatchedColors.Add(_grid[y, x].Color);
                            res.TotalGemsDestroyed++;
                            if (_grid[y, x].IsButterfly) res.ButterfliesFreed++;
                            if (_grid[y, x].Special == SpecialType.Time5) { res.TimeGemsMatched++; res.ExtraTimeSeconds += 5; }
                            if (_grid[y, x].Special == SpecialType.Time10) { res.TimeGemsMatched++; res.ExtraTimeSeconds += 10; }
                            if (_grid[y, x].Special == SpecialType.Flame) res.FlameDestroyed++;
                            else if (_grid[y, x].Special == SpecialType.Star) res.StarDestroyed++;
                            else if (_grid[y, x].Special == SpecialType.Hypercube) res.HypercubeDestroyed++;

                            // Flame explosion 3x3
                            if (_grid[y, x].Special == SpecialType.Flame)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                    for (int dx = -1; dx <= 1; dx++)
                                        if (y + dy >= 0 && y + dy < Rows && x + dx >= 0 && x + dx < Cols)
                                            toDestroy[y + dy, x + dx] = true;
                            }
                            // Star blast row & col
                            else if (_grid[y, x].Special == SpecialType.Star)
                            {
                                for (int r = 0; r < Rows; r++) toDestroy[r, x] = true;
                                for (int c = 0; c < Cols; c++) toDestroy[y, c] = true;
                            }
                            // Supernova blast 3 rows & 3 cols
                            else if (_grid[y, x].Special == SpecialType.Supernova)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                    if (y + dy >= 0 && y + dy < Rows)
                                        for (int c = 0; c < Cols; c++) toDestroy[y + dy, c] = true;

                                for (int dx = -1; dx <= 1; dx++)
                                    if (x + dx >= 0 && x + dx < Cols)
                                        for (int r = 0; r < Rows; r++) toDestroy[r, x + dx] = true;
                            }
                        }
                    }
                }

                // Clear destroyed gems & clear adjacent Dirt/HardRock in Diamond Mine
                bool[,] dirtDestroyed = new bool[Rows, Cols];
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Cols; x++)
                    {
                        if (toDestroy[y, x] && _grid[y, x] != null && _grid[y, x].Special != SpecialType.Dirt && _grid[y, x].Special != SpecialType.HardRock && _grid[y, x].Special != SpecialType.GoldNugget)
                        {
                            // Ice Storm: remember which columns were matched (melt only those)
                            if (!res.MatchedColumns.Contains(x)) res.MatchedColumns.Add(x);
                            res.ColumnDestroyedCount[x]++;

                            if (_grid[y, x].Special == SpecialType.Bomb) res.BombsDestroyed++;

                            _grid[y, x] = null;

                            // Alchemy: destroyed gems turn their neighbours to gold
                            if (isAlchemy)
                            {
                                int[] adx = { 0, 0, -1, 1 };
                                int[] ady = { -1, 1, 0, 0 };
                                for (int d = 0; d < 4; d++)
                                {
                                    int ny = y + ady[d];
                                    int nx = x + adx[d];
                                    if (ny >= 0 && ny < Rows && nx >= 0 && nx < Cols && _grid[ny, nx] != null
                                        && _grid[ny, nx].Special == SpecialType.None && !_grid[ny, nx].IsButterfly)
                                    {
                                        _grid[ny, nx].Special = SpecialType.Gold;
                                        res.GoldTilesConverted++;
                                    }
                                }
                            }

                            // Damage adjacent dirt/rock
                            int[] dx = { 0, 0, -1, 1 };
                            int[] dy = { -1, 1, 0, 0 };
                            for (int d = 0; d < 4; d++)
                            {
                                int ny = y + dy[d];
                                int nx = x + dx[d];
                                if (ny >= 0 && ny < Rows && nx >= 0 && nx < Cols && _grid[ny, nx] != null)
                                {
                                    if (_grid[ny, nx].Special == SpecialType.Dirt)
                                    {
                                        dirtDestroyed[ny, nx] = true;
                                        res.DirtCleared++;
                                    }
                                    else if (_grid[ny, nx].Special == SpecialType.GoldNugget)
                                    {
                                        dirtDestroyed[ny, nx] = true;
                                        res.NuggetsMined++;
                                    }
                                    else if (_grid[ny, nx].Special == SpecialType.HardRock)
                                    {
                                        _grid[ny, nx].RockDurability--;
                                        if (_grid[ny, nx].RockDurability <= 0)
                                        {
                                            dirtDestroyed[ny, nx] = true;
                                            res.RockCleared++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Cols; x++)
                    {
                        if (dirtDestroyed[y, x]) _grid[y, x] = null;
                    }
                }

                // Place newly formed special gems
                foreach (var sp in newSpecials)
                {
                    _grid[sp.Item2, sp.Item1] = new Gem(sp.Item4, sp.Item3);
                }

                // Apply Gravity & Drop New Gems
                ApplyGravity(isLightning, isButterflies, withBombs);
            }

            res.CascadeDepth = depth;
            res.BasePoints = res.TotalGemsDestroyed * 50 + (depth > 1 ? (depth - 1) * 100 : 0);
            return res;
        }

        public void TriggerHypercubeColor(GemColor targetColor)
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_grid[y, x] != null && _grid[y, x].Color == targetColor)
                    {
                        _grid[y, x] = null;
                    }
                }
            }
            ApplyGravity();
        }

        private void ApplyGravity(bool isLightning = false, bool isButterflies = false, bool withBombs = false)
        {
            Array colors = Enum.GetValues(typeof(GemColor));

            for (int x = 0; x < Cols; x++)
            {
                int writeY = Rows - 1;
                for (int y = Rows - 1; y >= 0; y--)
                {
                    if (_grid[y, x] != null)
                    {
                        _grid[writeY, x] = _grid[y, x];
                        if (writeY != y) _grid[y, x] = null;
                        writeY--;
                    }
                }

                // Fill empty top spaces with new random gems
                for (int y = writeY; y >= 0; y--)
                {
                    GemColor c = (GemColor)colors.GetValue(_rng.Next(colors.Length));
                    SpecialType spec = SpecialType.None;

                    if (isLightning && _rng.Next(100) < 15)
                    {
                        spec = _rng.Next(2) == 0 ? SpecialType.Time5 : SpecialType.Time10;
                    }
                    else if (withBombs && _rng.Next(100) < 10)
                    {
                        spec = SpecialType.Bomb;
                    }

                    Gem g = new Gem(c, spec);
                    if (isButterflies && y == Rows - 1 && _rng.Next(100) < 25)
                    {
                        g.IsButterfly = true;
                    }
                    _grid[y, x] = g;
                }
            }
        }

        // Time Bombs: every tick decrements every bomb; when one reaches zero
        // it explodes, destroying its 3x3 area. Returns the number of explosions.
        public int TickBombs()
        {
            List<Tuple<int, int>> exploding = new List<Tuple<int, int>>();
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_grid[y, x] != null && _grid[y, x].Special == SpecialType.Bomb)
                    {
                        _grid[y, x].BombTimer--;
                        if (_grid[y, x].BombTimer <= 0)
                        {
                            exploding.Add(Tuple.Create(x, y));
                        }
                    }
                }
            }

            if (exploding.Count == 0) return 0;

            foreach (var e in exploding)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int ny = e.Item2 + dy;
                        int nx = e.Item1 + dx;
                        if (ny >= 0 && ny < Rows && nx >= 0 && nx < Cols && _grid[ny, nx] != null
                            && _grid[ny, nx].Special != SpecialType.Dirt
                            && _grid[ny, nx].Special != SpecialType.HardRock
                            && _grid[ny, nx].Special != SpecialType.GoldNugget)
                        {
                            _grid[ny, nx] = null;
                        }
                    }
            }

            ApplyGravity();
            return exploding.Count;
        }

        public List<Tuple<int, int, int>> GetBombInfo()
        {
            List<Tuple<int, int, int>> bombs = new List<Tuple<int, int, int>>();
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_grid[y, x] != null && _grid[y, x].Special == SpecialType.Bomb)
                    {
                        bombs.Add(Tuple.Create(x, y, _grid[y, x].BombTimer));
                    }
                }
            }
            return bombs;
        }

        public int GetBombCount()
        {
            int count = 0;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_grid[y, x] != null && _grid[y, x].Special == SpecialType.Bomb) count++;
                }
            }
            return count;
        }

        public void MoveButterfliesUp()
        {
            for (int x = 0; x < Cols; x++)
            {
                for (int y = 0; y < Rows - 1; y++)
                {
                    if (_grid[y + 1, x] != null && _grid[y + 1, x].IsButterfly)
                    {
                        Gem b = _grid[y + 1, x];
                        _grid[y + 1, x] = _grid[y, x];
                        _grid[y, x] = b;
                    }
                }
            }
        }

        // Butterflies replenish from the bottom, like gems after gravity: after a
        // turn frees some, new ones enter from the bottom so the board always has
        // a continuous stream (authentic Butterflies / Quest rule).
        public void SpawnButterflyAtBottom()
        {
            int spawned = 0;
            int attempts = 0;
            while (spawned < 1 && attempts < 200)
            {
                attempts++;
                int x = _rng.Next(Cols);
                for (int y = Rows - 1; y >= 0; y--)
                {
                    Gem g = _grid[y, x];
                    if (g == null || g.IsButterfly) continue;

                    Gem butterfly = g.Clone();
                    butterfly.IsButterfly = true;
                    _grid[y, x] = butterfly;
                    spawned++;
                    break;
                }
            }
        }

        public bool IsButterflyAtTop()
        {
            for (int x = 0; x < Cols; x++)
            {
                if (_grid[0, x] != null && _grid[0, x].IsButterfly)
                    return true;
            }
            return false;
        }

        // Starts a Butterfly mode board: normal board plus a set of butterflies
        // already placed in the lower rows (colors are preserved, so no new matches appear).
        public void InitializeButterfliesBoard()
        {
            InitializeBoard();

            int spawned = 0;
            int attempts = 0;
            while (spawned < 6 && attempts < 300)
            {
                attempts++;
                int x = _rng.Next(Cols);
                int y = _rng.Next(Rows - 3, Rows);
                Gem g = _grid[y, x];
                if (g == null || g.IsButterfly) continue;

                Gem butterfly = g.Clone();
                butterfly.IsButterfly = true;
                _grid[y, x] = butterfly;
                spawned++;
            }
        }

        public int GetButterflyCount()
        {
            int count = 0;
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                    if (_grid[y, x] != null && _grid[y, x].IsButterfly) count++;
            return count;
        }

        // Columns that currently contain at least one butterfly (sorted)
        public List<int> GetButterflyColumns()
        {
            List<int> cols = new List<int>();
            for (int x = 0; x < Cols; x++)
                for (int y = 0; y < Rows; y++)
                    if (_grid[y, x] != null && _grid[y, x].IsButterfly)
                    {
                        if (!cols.Contains(x)) cols.Add(x);
                        break;
                    }
            cols.Sort();
            return cols;
        }

        // Columns with a butterfly one row away from the spider (row 1)
        public List<int> GetButterflyDangerColumns()
        {
            List<int> cols = new List<int>();
            for (int x = 0; x < Cols; x++)
            {
                if (_grid[1, x] != null && _grid[1, x].IsButterfly)
                {
                    cols.Add(x);
                }
            }
            return cols;
        }

        public bool IsButterflyInDanger()
        {
            return GetButterflyDangerColumns().Count > 0;
        }

        public void InitializeDiamondMineBoard()
        {
            InitializeBoard();

            // Set bottom 3 rows (5, 6, 7) as Dirt & HardRock (rock breaks with 1 match)
            for (int x = 0; x < Cols; x++)
            {
                _grid[5, x] = new Gem(GemColor.Red, SpecialType.Dirt);
                _grid[6, x] = new Gem(GemColor.Yellow, (_rng.Next(2) == 0 ? SpecialType.Dirt : SpecialType.HardRock));
                _grid[7, x] = new Gem(GemColor.Blue, SpecialType.HardRock);
            }

            // Hide gold nuggets inside some dirt tiles (Gold Rush quest)
            HideNuggets(5, 7);
        }

        public bool HasDirtRemaining()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_grid[y, x] != null && (_grid[y, x].Special == SpecialType.Dirt || _grid[y, x].Special == SpecialType.HardRock || _grid[y, x].Special == SpecialType.GoldNugget))
                        return true;
                }
            }
            return false;
        }

        public void ShiftDiamondMineDown()
        {
            // Shift gems up by 2 rows to reveal 2 new rows of Dirt/Rock at the bottom
            for (int y = 0; y < Rows - 2; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    _grid[y, x] = _grid[y + 2, x];
                }
            }

            for (int x = 0; x < Cols; x++)
            {
                _grid[6, x] = new Gem(GemColor.Red, SpecialType.Dirt);
                _grid[7, x] = new Gem(GemColor.Yellow, (_rng.Next(2) == 0 ? SpecialType.Dirt : SpecialType.HardRock));
            }

            // Hide fresh gold nuggets inside the new dirt (Gold Rush quest): the
            // mine never dries up, so the mission stays achievable.
            HideNuggets(6, 7);
        }

        // Hides gold nuggets inside dirt rows (y1..y2), like the initial board
        private void HideNuggets(int y1, int y2)
        {
            for (int y = y1; y <= y2; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_rng.Next(100) < 20)
                    {
                        _grid[y, x] = new Gem(GemColor.Yellow, SpecialType.GoldNugget);
                    }
                }
            }
        }
    }
}
