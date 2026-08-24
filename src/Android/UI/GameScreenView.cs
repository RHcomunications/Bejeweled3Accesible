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
    public enum AndroidGameScreen
    {
        MainMenu,
        GameSelect,
        BadgesScreen,
        RecordsScreen,
        TutorialScreen,
        ProfileSelectScreen,
        OptionsScreen,
        Playing,
        GameOver
    }

    public class GameScreenView : View
    {
        private readonly Context _context;
        private readonly TalkBackBridge _talkBack;
        private readonly AndroidSoundEngine _sound;
        private readonly ProfileManager _profileMgr;
        private BadgeManager _badgeMgr;
        private Board _board;

        private AndroidGameScreen _currentScreen = AndroidGameScreen.MainMenu;
        private int _menuIdx = 0;
        private int _gameModeIdx = 0;
        private int _badgeIdx = 0;
        private int _recordsIdx = 0;
        private int _tutorialIdx = 0;
        private int _profileIdx = 0;
        private int _optionsIdx = 0;
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

        public GameScreenView(Context context, TalkBackBridge talkBack, AndroidSoundEngine sound) : base(context)
        {
            _context = context;
            _talkBack = talkBack;
            _sound = sound;

            _profileMgr = ProfileManager.Load();
            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "Jugador 1";
            _badgeMgr = BadgeManager.Load(profName);

            Focusable = true;
            Clickable = true;

            AnnounceCurrentMenu();
        }

        private GameProgress Progress => _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.Progress : new GameProgress();

        private string[] GetMainMenuItems()
        {
            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "";
            return new string[]
            {
                Localization.Get("MenuPlay"),
                Localization.Get("MenuBadges"),
                Localization.Get("MenuRecords"),
                Localization.Get("MenuTutorial"),
                Localization.Get("MenuChangeUser", profName),
                Localization.Get("MenuLanguage"),
                Localization.Get("MenuOptions"),
                Localization.Get("MenuExit")
            };
        }

        private string[] GetGameModeKeys()
        {
            return new string[]
            {
                "ModeClassic",
                "ModeLightning",
                "ModeZen",
                "ModeQuest",
                Progress.IsPokerUnlocked ? "ModePoker" : "ModePokerLocked",
                Progress.IsButterfliesUnlocked ? "ModeButterflies" : "ModeButterfliesLocked",
                Progress.IsIceStormUnlocked ? "ModeIceStorm" : "ModeIceStormLocked",
                Progress.IsDiamondMineUnlocked ? "ModeDiamondMine" : "ModeDiamondMineLocked",
                "BackToMain"
            };
        }

        private string[] GetOptionsMenuItems()
        {
            return new string[]
            {
                Localization.Get("OptSoundVol", 100),
                Localization.Get("OptMusicVol", 80),
                Localization.Get("OptVoiceVol", 100),
                Localization.Get("OptBinaural", Localization.Get("StateOn")),
                Localization.Get("OptBack")
            };
        }

        private string[] GetRecordsItems()
        {
            List<string> list = new List<string>();
            var stats = Progress;
            list.Add(string.Format("{0}: Nivel {1}", Localization.Get("ModeClassic"), stats.ClassicLevel));
            list.Add(string.Format("{0}: {1}", Localization.Get("ModeLightning"), stats.LightningHighScore));
            list.Add(string.Format("{0}: Nivel {1}", Localization.Get("ModeZen"), stats.ZenLevel));
            list.Add(string.Format("{0}: {1}", Localization.Get("TotalGemsCleared"), stats.TotalGemsCleared));
            list.Add(Localization.Get("OptBack"));
            return list.ToArray();
        }

        private string[] GetTutorialItems()
        {
            return new string[]
            {
                Localization.Get("TutorialStep1"),
                Localization.Get("TutorialStep2"),
                Localization.Get("TutorialStep3"),
                Localization.Get("TutorialStep4"),
                Localization.Get("OptBack")
            };
        }

        private string[] GetProfileSelectItems()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < _profileMgr.Profiles.Count; i++)
            {
                string marker = (i == _profileMgr.CurrentProfileIndex) ? " (" + Localization.Get("StateEnabled") + ")" : "";
                list.Add(_profileMgr.Profiles[i].ProfileName + marker);
            }
            list.Add(Localization.Get("ProfileCreateNew"));
            list.Add(Localization.Get("OptBack"));
            return list.ToArray();
        }

        private string[] GetBadgeListItems()
        {
            string[] keys = new string[]
            {
                "BadgeInferno", "BadgeStellar", "BadgeChromatic", "BadgeBlaster",
                "BadgeBejeweler", "BadgeFinalFrenzy", "BadgeHighVoltage", "BadgeAnteUp",
                "BadgeRelicHunter", "BadgeButterflyMonarch", "OptBack"
            };
            List<string> list = new List<string>();
            foreach (var k in keys)
            {
                if (k == "OptBack") list.Add(Localization.Get("OptBack"));
                else
                {
                    BadgeTier t = _badgeMgr.GetTier(k);
                    string tierStr = Localization.Get("Tier" + t.ToString());
                    list.Add(string.Format("{0}: {1}", Localization.Get(k), tierStr));
                }
            }
            return list.ToArray();
        }

        private string[] GetCurrentItems(out int activeIdx)
        {
            switch (_currentScreen)
            {
                case AndroidGameScreen.MainMenu:
                    activeIdx = _menuIdx;
                    return GetMainMenuItems();
                case AndroidGameScreen.GameSelect:
                    activeIdx = _gameModeIdx;
                    string[] keys = GetGameModeKeys();
                    string[] titles = new string[keys.Length];
                    for (int i = 0; i < keys.Length; i++) titles[i] = Localization.Get(keys[i]);
                    return titles;
                case AndroidGameScreen.BadgesScreen:
                    activeIdx = _badgeIdx;
                    return GetBadgeListItems();
                case AndroidGameScreen.RecordsScreen:
                    activeIdx = _recordsIdx;
                    return GetRecordsItems();
                case AndroidGameScreen.TutorialScreen:
                    activeIdx = _tutorialIdx;
                    return GetTutorialItems();
                case AndroidGameScreen.ProfileSelectScreen:
                    activeIdx = _profileIdx;
                    return GetProfileSelectItems();
                case AndroidGameScreen.OptionsScreen:
                    activeIdx = _optionsIdx;
                    return GetOptionsMenuItems();
                default:
                    activeIdx = 0;
                    return Array.Empty<string>();
            }
        }

        private void SetActiveIndex(int idx)
        {
            switch (_currentScreen)
            {
                case AndroidGameScreen.MainMenu: _menuIdx = idx; break;
                case AndroidGameScreen.GameSelect: _gameModeIdx = idx; break;
                case AndroidGameScreen.BadgesScreen: _badgeIdx = idx; break;
                case AndroidGameScreen.RecordsScreen: _recordsIdx = idx; break;
                case AndroidGameScreen.TutorialScreen: _tutorialIdx = idx; break;
                case AndroidGameScreen.ProfileSelectScreen: _profileIdx = idx; break;
                case AndroidGameScreen.OptionsScreen: _optionsIdx = idx; break;
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            canvas.DrawColor(Color.Rgb(15, 15, 28));

            if (_currentScreen == AndroidGameScreen.Playing)
            {
                DrawBoard(canvas);
                return;
            }

            // Dibujar Menu Actual
            string[] items = GetCurrentItems(out int activeIdx);
            _paint.Color = Color.White;
            _paint.TextSize = 48f;
            _paint.SetTypeface(Typeface.DefaultBold);

            string title = GetScreenTitle();
            canvas.DrawText(title, 40, 100, _paint);

            int startY = 180;
            int itemHeight = 90;

            for (int i = 0; i < items.Length; i++)
            {
                int top = startY + (i * itemHeight);
                RectF itemRect = new RectF(30, top, Width - 30, top + itemHeight - 15);

                if (i == activeIdx)
                {
                    _paint.Color = Color.Rgb(255, 200, 0);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(itemRect, 12, 12, _paint);

                    _paint.Color = Color.Black;
                }
                else
                {
                    _paint.Color = Color.Argb(50, 255, 255, 255);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(itemRect, 12, 12, _paint);

                    _paint.Color = Color.White;
                }

                _paint.TextSize = 38f;
                _paint.SetTypeface(Typeface.Default);
                canvas.DrawText(items[i], 50, top + 50, _paint);
            }
        }

        private string GetScreenTitle()
        {
            switch (_currentScreen)
            {
                case AndroidGameScreen.MainMenu: return Localization.Get("AppTitle");
                case AndroidGameScreen.GameSelect: return Localization.Get("SelectMode");
                case AndroidGameScreen.BadgesScreen: return Localization.Get("MenuBadges");
                case AndroidGameScreen.RecordsScreen: return Localization.Get("MenuRecords");
                case AndroidGameScreen.TutorialScreen: return Localization.Get("TutorialTitle");
                case AndroidGameScreen.ProfileSelectScreen: return Localization.Get("ProfileSelectTitle");
                case AndroidGameScreen.OptionsScreen: return Localization.Get("OptionsTitle");
                default: return "Bejeweled 3";
            }
        }

        private void DrawBoard(Canvas canvas)
        {
            if (_board == null) return;

            int tileSize = Math.Min(Width / Board.Cols, (Height - 120) / Board.Rows);
            int offsetX = (Width - (tileSize * Board.Cols)) / 2;
            int offsetY = 100;

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
            if (_currentScreen == AndroidGameScreen.Playing)
            {
                return HandleBoardTouch(e);
            }

            return HandleMenuTouch(e);
        }

        private bool HandleMenuTouch(MotionEvent e)
        {
            string[] items = GetCurrentItems(out int activeIdx);
            int startY = 180;
            int itemHeight = 90;

            int clickedIdx = (int)((e.GetY() - startY) / itemHeight);

            if (e.Action == MotionEventActions.Down)
            {
                if (clickedIdx >= 0 && clickedIdx < items.Length)
                {
                    SetActiveIndex(clickedIdx);
                    _sound?.PlaySound(AudioMap.ButtonMouseover);
                    _talkBack?.Speak(items[clickedIdx], true);
                    Invalidate();
                }
            }
            else if (e.Action == MotionEventActions.Up)
            {
                if (clickedIdx >= 0 && clickedIdx < items.Length)
                {
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    ExecuteMenuItem(clickedIdx);
                    Invalidate();
                }
            }
            return true;
        }

        private void ExecuteMenuItem(int idx)
        {
            if (_currentScreen == AndroidGameScreen.MainMenu)
            {
                if (idx == 0) { _currentScreen = AndroidGameScreen.GameSelect; _gameModeIdx = 0; }
                else if (idx == 1) { _currentScreen = AndroidGameScreen.BadgesScreen; _badgeIdx = 0; }
                else if (idx == 2) { _currentScreen = AndroidGameScreen.RecordsScreen; _recordsIdx = 0; }
                else if (idx == 3) { _currentScreen = AndroidGameScreen.TutorialScreen; _tutorialIdx = 0; }
                else if (idx == 4) { _currentScreen = AndroidGameScreen.ProfileSelectScreen; _profileIdx = 0; }
                else if (idx == 5) { Localization.ToggleLanguage(); }
                else if (idx == 6) { _currentScreen = AndroidGameScreen.OptionsScreen; _optionsIdx = 0; }
                else if (idx == 7) { System.Environment.Exit(0); }
            }
            else if (_currentScreen == AndroidGameScreen.GameSelect)
            {
                string[] keys = GetGameModeKeys();
                string selectedKey = keys[idx];
                if (selectedKey == "BackToMain")
                {
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
                else
                {
                    StartGame(selectedKey);
                }
            }
            else
            {
                _currentScreen = AndroidGameScreen.MainMenu;
            }

            AnnounceCurrentMenu();
        }

        private void StartGame(string modeKey)
        {
            _board = new Board(new Random().Next());
            _currentScreen = AndroidGameScreen.Playing;
            _sound?.PlaySound(AudioMap.VoiceGetready);

            if (modeKey == "ModeLightning") _sound?.PlayMusic(MusicMap.FileName(MusicMap.Lightning));
            else if (modeKey == "ModePoker") _sound?.PlayMusic(MusicMap.FileName(MusicMap.Poker));
            else if (modeKey == "ModeButterflies") _sound?.PlayMusic(MusicMap.FileName(MusicMap.Butterflies));
            else if (modeKey == "ModeZen") _sound?.PlayMusic(MusicMap.FileName(MusicMap.ZenPart1));
            else _sound?.PlayMusic(MusicMap.FileName(MusicMap.ClassicPart1));

            _talkBack?.Speak(Localization.Get(modeKey) + ". " + Localization.Get("GameReady"), true);
        }

        private bool HandleBoardTouch(MotionEvent e)
        {
            int tileSize = Math.Min(Width / Board.Cols, (Height - 120) / Board.Rows);
            int offsetX = (Width - (tileSize * Board.Cols)) / 2;
            int offsetY = 100;

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
                            _sound?.PlaySoundSpatial(AudioMap.GemHit, cellX, cellY);
                            _board.SwapGems(_selectedX, _selectedY, cellX, cellY);
                            CascadeResult res = _board.ProcessMatchesAndGravity(false, false, false, false);
                            if (res != null && res.AnyMatched) _sound?.PlaySoundSpatial(AudioMap.ComboPrefix + "1", cellX, cellY);
                            _selectedX = -1;
                            _selectedY = -1;
                            Invalidate();
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
            return true;
        }

        private void AnnounceCurrentMenu()
        {
            string[] items = GetCurrentItems(out int activeIdx);
            string title = GetScreenTitle();
            if (items.Length > 0 && activeIdx < items.Length)
            {
                _talkBack?.Speak(title + ". " + items[activeIdx], true);
            }
        }

        private void AnnounceCell(int x, int y)
        {
            Gem g = _board.GetGem(x, y);
            string col = ((char)('A' + x)).ToString();
            int row = y + 1;
            string desc = g != null ? string.Format("{0}{1}: {2}", col, row, g.GetNameLocalized()) : string.Format("{0}{1}: Vacio", col, row);
            _talkBack?.Speak(desc, true);
        }
    }
}
