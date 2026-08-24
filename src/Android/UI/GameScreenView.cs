using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Views.Accessibility;
using Bejeweled3Accessible.Audio;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;
using Bejeweled3Accessible.AndroidApp.Audio;

namespace Bejeweled3Accessible.AndroidApp.UI
{
    public enum AndroidGameScreen
    {
        Loading,
        MainMenu,
        GameSelect,
        BadgesScreen,
        RecordsScreen,
        TutorialScreen,
        ProfileSelectScreen,
        OptionsScreen,
        PauseMenu,
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

        private AndroidGameScreen _currentScreen = AndroidGameScreen.Loading;
        private int _loadingProgress = 0;
        private int _menuIdx = 0;
        private int _gameModeIdx = 0;
        private int _badgeIdx = 0;
        private int _recordsIdx = 0;
        private int _tutorialIdx = 0;
        private int _profileIdx = 0;
        private int _optionsIdx = 0;
        private int _pauseIdx = 0;
        private string _currentModeKey = "ModeClassic";
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

            _talkBack?.AttachView(this);

            _profileMgr = ProfileManager.Load();
            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "Jugador 1";
            _badgeMgr = BadgeManager.Load(profName);

            Focusable = true;
            Clickable = true;
            ImportantForAccessibility = ImportantForAccessibility.Yes;

            StartLoadingSequence();
        }

        private void StartLoadingSequence()
        {
            _currentScreen = AndroidGameScreen.Loading;
            _loadingProgress = 0;
            _sound?.PlayMusic(MusicMap.Intro);
            _sound?.PlaySound(AudioMap.VoiceWelcometobejeweled);
            _talkBack?.Speak("Cargando Bejeweled 3 Accesible. Toca la pantalla para continuar al menú principal.", true);

            PostDelayed(() =>
            {
                if (_currentScreen == AndroidGameScreen.Loading)
                {
                    TransitionToMainMenu();
                }
            }, 3500);
        }

        private void TransitionToMainMenu()
        {
            _currentScreen = AndroidGameScreen.MainMenu;
            _menuIdx = 0;
            _sound?.PlayMusic(MusicMap.MainTheme);
            _sound?.PlaySound(AudioMap.VoiceWelcomeback);
            AnnounceCurrentMenu();
            Invalidate();
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

        private string[] GetPauseMenuItems()
        {
            return new string[]
            {
                Localization.Get("PauseResume"),
                Localization.Get("PauseRestart"),
                Localization.Get("PauseOptions"),
                Localization.Get("PauseMainMenu")
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

        public string[] GetCurrentItems(out int activeIdx)
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
                case AndroidGameScreen.PauseMenu:
                    activeIdx = _pauseIdx;
                    return GetPauseMenuItems();
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
                case AndroidGameScreen.PauseMenu: _pauseIdx = idx; break;
            }
        }

        public AndroidGameScreen CurrentScreen => _currentScreen;
        public Board BoardInstance => _board;

        public override AccessibilityNodeProvider AccessibilityNodeProvider
        {
            get
            {
                return new GameAccessibilityNodeProvider(this);
            }
        }

        public void SelectOrSwapCell(int cellX, int cellY)
        {
            if (_board == null) return;

            if (_selectedX >= 0 && _selectedY >= 0)
            {
                int dx = cellX - _selectedX;
                int dy = cellY - _selectedY;
                if (Math.Abs(dx) + Math.Abs(dy) == 1)
                {
                    ExecuteSwap(_selectedX, _selectedY, cellX, cellY);
                    return;
                }
            }

            _selectedX = cellX;
            _selectedY = cellY;
            _sound?.PlaySoundSpatial(AudioMap.Select, cellX, cellY);
            AnnounceCell(cellX, cellY);
            Invalidate();
        }

        public void TogglePause()
        {
            _sound?.PlaySound(AudioMap.ButtonPress);
            _currentScreen = AndroidGameScreen.PauseMenu;
            _pauseIdx = 0;
            _sound?.StopMusic();
            AnnounceCurrentMenu();
            Invalidate();
        }

        public override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo info)
        {
            base.OnInitializeAccessibilityNodeInfo(info);
            info.ClassName = "android.view.View";
            info.ContentDescription = GetScreenDescriptionForAccessibility();
        }

        private string GetScreenDescriptionForAccessibility()
        {
            if (_currentScreen == AndroidGameScreen.Loading)
            {
                return "Cargando Bejeweled 3 Accesible. Toca para continuar.";
            }
            if (_currentScreen == AndroidGameScreen.Playing)
            {
                return "Tablero de juego Bejeweled 3. Desliza para mover gemas o usa los botones de la derecha.";
            }
            string[] items = GetCurrentItems(out int activeIdx);
            string title = GetScreenTitle();
            string cur = (items.Length > 0 && activeIdx < items.Length) ? items[activeIdx] : "";
            return title + ". Opción actual: " + cur;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            canvas.DrawColor(Color.Rgb(15, 15, 28));

            if (_currentScreen == AndroidGameScreen.Loading)
            {
                DrawLoadingScreen(canvas);
                return;
            }

            if (_currentScreen == AndroidGameScreen.Playing)
            {
                DrawLandscapeBoard(canvas);
                return;
            }

            string[] items = GetCurrentItems(out int activeIdx);
            _paint.Color = Color.White;
            _paint.TextSize = 44f;
            _paint.SetTypeface(Typeface.DefaultBold);

            string title = GetScreenTitle();
            canvas.DrawText(title, 50, 70, _paint);

            int startY = 110;
            int itemHeight = 70;

            for (int i = 0; i < items.Length; i++)
            {
                int top = startY + (i * itemHeight);
                RectF itemRect = new RectF(50, top, Width - 50, top + itemHeight - 10);

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

                _paint.TextSize = 34f;
                _paint.SetTypeface(Typeface.Default);
                canvas.DrawText(items[i], 70, top + 42, _paint);
            }
        }

        private void DrawLoadingScreen(Canvas canvas)
        {
            _paint.Color = Color.White;
            _paint.TextSize = 56f;
            _paint.SetTypeface(Typeface.DefaultBold);
            _paint.TextAlign = Paint.Align.Center;

            canvas.DrawText("BEJEWELED 3 ACCESIBLE", Width / 2f, Height / 2f - 40, _paint);

            _paint.TextSize = 32f;
            _paint.SetTypeface(Typeface.Default);
            _paint.Color = Color.Rgb(255, 200, 0);
            canvas.DrawText("Cargando... Toca la pantalla para comenzar", Width / 2f, Height / 2f + 40, _paint);

            _paint.TextAlign = Paint.Align.Left;
        }

        private string GetScreenTitle()
        {
            switch (_currentScreen)
            {
                case AndroidGameScreen.Loading: return Localization.Get("LoadingTitle");
                case AndroidGameScreen.MainMenu: return Localization.Get("AppTitle");
                case AndroidGameScreen.GameSelect: return Localization.Get("SelectMode");
                case AndroidGameScreen.BadgesScreen: return Localization.Get("MenuBadges");
                case AndroidGameScreen.RecordsScreen: return Localization.Get("MenuRecords");
                case AndroidGameScreen.TutorialScreen: return Localization.Get("TutorialTitle");
                case AndroidGameScreen.ProfileSelectScreen: return Localization.Get("ProfileSelectTitle");
                case AndroidGameScreen.OptionsScreen: return Localization.Get("OptionsTitle");
                case AndroidGameScreen.PauseMenu: return Localization.Get("PauseTitle");
                default: return "Bejeweled 3";
            }
        }

        private void DrawLandscapeBoard(Canvas canvas)
        {
            if (_board == null) return;

            int boardHeight = Height - 40;
            int tileSize = boardHeight / Board.Rows;
            int offsetX = 30;
            int offsetY = 20;

            for (int y = 0; y < Board.Rows; y++)
            {
                for (int x = 0; x < Board.Cols; x++)
                {
                    int left = offsetX + (x * tileSize) + 2;
                    int top = offsetY + (y * tileSize) + 2;
                    int right = left + tileSize - 4;
                    int bottom = top + tileSize - 4;
                    RectF rect = new RectF(left, top, right, bottom);

                    _paint.Color = Color.Argb(35, 255, 255, 255);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(rect, 8, 8, _paint);

                    Gem gem = _board.GetGem(x, y);
                    if (gem != null)
                    {
                        Color c = _gemColors.ContainsKey(gem.Color) ? _gemColors[gem.Color] : Color.Gray;
                        _paint.Color = c;
                        _paint.SetStyle(Paint.Style.Fill);
                        canvas.DrawCircle(rect.CenterX(), rect.CenterY(), (tileSize - 10) / 2f, _paint);
                    }

                    var validMoves = HintFinder.GetValidMovesFrom(_board, x, y);
                    if (validMoves != null && validMoves.Count > 0)
                    {
                        foreach (var m in validMoves)
                        {
                            DrawArrow(canvas, rect, m.Key, m.Value);
                        }
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

            int panelLeft = offsetX + (Board.Cols * tileSize) + 40;
            int panelWidth = Width - panelLeft - 30;

            RectF hintRect = new RectF(panelLeft, 60, panelLeft + panelWidth, 160);
            _paint.Color = Color.Rgb(0, 150, 255);
            _paint.SetStyle(Paint.Style.Fill);
            canvas.DrawRoundRect(hintRect, 16, 16, _paint);

            _paint.Color = Color.White;
            _paint.TextSize = 36f;
            _paint.SetTypeface(Typeface.DefaultBold);
            canvas.DrawText("💡 " + Localization.Get("HintTitle"), panelLeft + 30, 125, _paint);

            // Boton PAUSA / MENU
            RectF pauseRect = new RectF(panelLeft, 190, panelLeft + panelWidth, 290);
            _paint.Color = Color.Rgb(220, 50, 50);
            _paint.SetStyle(Paint.Style.Fill);
            canvas.DrawRoundRect(pauseRect, 16, 16, _paint);

            _paint.Color = Color.White;
            canvas.DrawText("⏸️ " + Localization.Get("PauseTitle"), panelLeft + 30, 255, _paint);
        }

        private void DrawArrow(Canvas canvas, RectF rect, int dx, int dy)
        {
            _paint.Color = Color.Yellow;
            _paint.SetStyle(Paint.Style.Fill);

            Android.Graphics.Path path = new Android.Graphics.Path();
            float cx = rect.CenterX();
            float cy = rect.CenterY();
            float arrowSize = 10f;

            if (dx == 1) // Derecha
            {
                path.MoveTo(rect.Right - 3, cy);
                path.LineTo(rect.Right - 3 - arrowSize, cy - arrowSize / 2);
                path.LineTo(rect.Right - 3 - arrowSize, cy + arrowSize / 2);
            }
            else if (dx == -1) // Izquierda
            {
                path.MoveTo(rect.Left + 3, cy);
                path.LineTo(rect.Left + 3 + arrowSize, cy - arrowSize / 2);
                path.LineTo(rect.Left + 3 + arrowSize, cy + arrowSize / 2);
            }
            else if (dy == 1) // Abajo
            {
                path.MoveTo(cx, rect.Bottom - 3);
                path.LineTo(cx - arrowSize / 2, rect.Bottom - 3 - arrowSize);
                path.LineTo(cx + arrowSize / 2, rect.Bottom - 3 - arrowSize);
            }
            else // Arriba
            {
                path.MoveTo(cx, rect.Top + 3);
                path.LineTo(cx - arrowSize / 2, rect.Top + 3 + arrowSize);
                path.LineTo(cx + arrowSize / 2, rect.Top + 3 + arrowSize);
            }
            path.Close();
            canvas.DrawPath(path, _paint);
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
            int startY = 110;
            int itemHeight = 70;

            if (e.Action == MotionEventActions.Down)
            {
                _startX = e.GetX();
                _startY = e.GetY();

                int clickedIdx = (int)((e.GetY() - startY) / itemHeight);
                if (clickedIdx >= 0 && clickedIdx < items.Length)
                {
                    if (activeIdx != clickedIdx)
                    {
                        SetActiveIndex(clickedIdx);
                        _sound?.PlaySound(AudioMap.ButtonMouseover);
                        _talkBack?.Speak(items[clickedIdx], true);
                        Invalidate();
                    }
                }
            }
            else if (e.Action == MotionEventActions.Move)
            {
                int hoverIdx = (int)((e.GetY() - startY) / itemHeight);
                if (hoverIdx >= 0 && hoverIdx < items.Length && hoverIdx != activeIdx)
                {
                    SetActiveIndex(hoverIdx);
                    _sound?.PlaySound(AudioMap.ButtonMouseover);
                    _talkBack?.Speak(items[hoverIdx], true);
                    Invalidate();
                }
            }
            else if (e.Action == MotionEventActions.Up)
            {
                float deltaX = e.GetX() - _startX;
                float deltaY = e.GetY() - _startY;

                if (Math.Abs(deltaY) > 60 && Math.Abs(deltaY) > Math.Abs(deltaX))
                {
                    if (deltaY > 0)
                    {
                        int nextIdx = (activeIdx + 1) % items.Length;
                        SetActiveIndex(nextIdx);
                        _sound?.PlaySound(AudioMap.ButtonMouseover);
                        _talkBack?.Speak(items[nextIdx], true);
                        Invalidate();
                        return true;
                    }
                    else
                    {
                        int prevIdx = (activeIdx - 1 + items.Length) % items.Length;
                        SetActiveIndex(prevIdx);
                        _sound?.PlaySound(AudioMap.ButtonMouseover);
                        _talkBack?.Speak(items[prevIdx], true);
                        Invalidate();
                        return true;
                    }
                }
                else if (deltaX < -100 && Math.Abs(deltaX) > Math.Abs(deltaY))
                {
                    if (_currentScreen != AndroidGameScreen.MainMenu)
                    {
                        _sound?.PlaySound(AudioMap.ButtonPress);
                        _currentScreen = AndroidGameScreen.MainMenu;
                        AnnounceCurrentMenu();
                        Invalidate();
                        return true;
                    }
                }
                else if (Math.Abs(deltaX) < 40 && Math.Abs(deltaY) < 40)
                {
                    int clickedIdx = (int)((e.GetY() - startY) / itemHeight);
                    if (clickedIdx >= 0 && clickedIdx < items.Length)
                    {
                        _sound?.PlaySound(AudioMap.ButtonPress);
                        ExecuteMenuItem(clickedIdx);
                        Invalidate();
                    }
                    else if (activeIdx >= 0 && activeIdx < items.Length)
                    {
                        _sound?.PlaySound(AudioMap.ButtonPress);
                        ExecuteMenuItem(activeIdx);
                        Invalidate();
                    }
                }
            }
            return true;
        }

        public void ExecuteMenuItem(int idx)
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
            else if (_currentScreen == AndroidGameScreen.ProfileSelectScreen)
            {
                if (idx < _profileMgr.Profiles.Count)
                {
                    _profileMgr.CurrentProfileIndex = idx;
                    _profileMgr.Save();
                    string name = _profileMgr.Profiles[idx].ProfileName;
                    _badgeMgr = BadgeManager.Load(name);
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    _talkBack?.Speak(string.Format("Perfil seleccionado: {0}", name), true);
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
                else if (idx == _profileMgr.Profiles.Count) // Crear nuevo perfil
                {
                    string newName = "Jugador " + (_profileMgr.Profiles.Count + 1);
                    _profileMgr.Profiles.Add(new PlayerProfile(newName));
                    _profileMgr.CurrentProfileIndex = _profileMgr.Profiles.Count - 1;
                    _profileMgr.Save();
                    _badgeMgr = BadgeManager.Load(newName);
                    _sound?.PlaySound(AudioMap.Rankup);
                    _talkBack?.Speak(string.Format("Nuevo perfil creado: {0}", newName), true);
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
                else
                {
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
            }
            else if (_currentScreen == AndroidGameScreen.OptionsScreen)
            {
                string[] opts = GetOptionsMenuItems();
                if (idx == opts.Length - 1)
                {
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
                else
                {
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    _talkBack?.Speak(opts[idx] + " ajustado.", true);
                }
            }
            else if (_currentScreen == AndroidGameScreen.BadgesScreen ||
                     _currentScreen == AndroidGameScreen.RecordsScreen ||
                     _currentScreen == AndroidGameScreen.TutorialScreen)
            {
                string[] curItems = GetCurrentItems(out int dummy);
                if (idx == curItems.Length - 1) // Volver
                {
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
                else
                {
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    _talkBack?.Speak(curItems[idx], true);
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.PauseMenu)
            {
                if (idx == 0) // Resume
                {
                    _currentScreen = AndroidGameScreen.Playing;
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    _talkBack?.Speak(Localization.Get("GameResumed"), true);
                }
                else if (idx == 1) // Restart
                {
                    StartGame(_currentModeKey);
                }
                else if (idx == 2) // Options
                {
                    _currentScreen = AndroidGameScreen.OptionsScreen;
                    _optionsIdx = 0;
                }
                else if (idx == 3) // Main Menu
                {
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _sound?.StopMusic();
                    _sound?.PlayMusic(MusicMap.MainTheme);
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
            _currentModeKey = modeKey;
            _board = new Board(new Random().Next());
            _currentScreen = AndroidGameScreen.Playing;
            _sound?.PlaySound(AudioMap.VoiceGetready);

            if (modeKey == "ModeLightning") _sound?.PlayMusic(MusicMap.FileName(MusicMap.Lightning));
            else if (modeKey == "ModePoker") _sound?.PlayMusic(MusicMap.FileName(MusicMap.Poker));
            else if (modeKey == "ModeButterflies") _sound?.PlayMusic(MusicMap.FileName(MusicMap.Butterflies));
            else if (modeKey == "ModeZen") _sound?.PlayMusic(MusicMap.FileName(MusicMap.ZenPart1));
            else _sound?.PlayMusic(MusicMap.FileName(MusicMap.ClassicPart1));

            _talkBack?.Speak(Localization.Get(modeKey) + ". " + Localization.Get("GameReady") + ". Toca una gema para ver hacia dónde moverla o toca los botones de pista y pausa a la derecha.", true);
        }

        private bool HandleBoardTouch(MotionEvent e)
        {
            int boardHeight = Height - 40;
            int tileSize = boardHeight / Board.Rows;
            int offsetX = 30;
            int offsetY = 20;

            int cellX = (int)((e.GetX() - offsetX) / tileSize);
            int cellY = (int)((e.GetY() - offsetY) / tileSize);

            int panelLeft = offsetX + (Board.Cols * tileSize) + 40;
            int panelWidth = Width - panelLeft - 30;

            if (e.Action == MotionEventActions.Down)
            {
                _startX = e.GetX();
                _startY = e.GetY();

                // Verificar si toco el panel lateral de botones
                if (e.GetX() >= panelLeft && e.GetX() <= panelLeft + panelWidth)
                {
                    if (e.GetY() >= 60 && e.GetY() <= 160) // Boton PISTA
                    {
                        TriggerHint();
                        return true;
                    }
                    else if (e.GetY() >= 190 && e.GetY() <= 290) // Boton PAUSA
                    {
                        _currentScreen = AndroidGameScreen.PauseMenu;
                        _pauseIdx = 0;
                        _sound?.PlaySound(AudioMap.ButtonPress);
                        AnnounceCurrentMenu();
                        Invalidate();
                        return true;
                    }
                }

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
                float deltaX = e.GetX() - _startX;
                float deltaY = e.GetY() - _startY;

                if (Math.Abs(deltaX) > 40 || Math.Abs(deltaY) > 40)
                {
                    int swapDx = Math.Abs(deltaX) > Math.Abs(deltaY) ? (deltaX > 0 ? 1 : -1) : 0;
                    int swapDy = Math.Abs(deltaX) > Math.Abs(deltaY) ? 0 : (deltaY > 0 ? 1 : -1);

                    int fromX = (_selectedX >= 0) ? _selectedX : _cursorX;
                    int fromY = (_selectedY >= 0) ? _selectedY : _cursorY;
                    int targetX = fromX + swapDx;
                    int targetY = fromY + swapDy;

                    if (targetX >= 0 && targetX < Board.Cols && targetY >= 0 && targetY < Board.Rows)
                    {
                        ExecuteSwap(fromX, fromY, targetX, targetY);
                    }
                }
            }
            return true;
        }

        public void TriggerHint()
        {
            _sound?.PlaySound(AudioMap.ButtonPress);
            MoveHint? hint = HintFinder.FindValidMove(_board);
            if (hint.HasValue)
            {
                var h = hint.Value;
                _selectedX = h.FromX;
                _selectedY = h.FromY;
                _cursorX = h.ToX;
                _cursorY = h.ToY;
                string colFrom = ((char)('A' + h.FromX)).ToString();
                string colTo = ((char)('A' + h.ToX)).ToString();
                string dir = h.ToX > h.FromX ? "la derecha" : (h.ToX < h.FromX ? "la izquierda" : (h.ToY > h.FromY ? "abajo" : "arriba"));
                _talkBack?.Speak(string.Format("Pista: Mueve {0}{1} hacia {2} a {3}{4}", colFrom, h.FromY + 1, dir, colTo, h.ToY + 1), true);
                _sound?.PlaySoundSpatial(AudioMap.Select, h.FromX, h.FromY);
                Invalidate();
            }
            else
            {
                _talkBack?.Speak("No hay movimientos válidos disponibles. Mezclando tablero.", true);
            }
        }

        private void ExecuteSwap(int fromX, int fromY, int toX, int toY)
        {
            _sound?.PlaySoundSpatial(AudioMap.GemHit, toX, toY);
            _board.SwapGems(fromX, fromY, toX, toY);
            CascadeResult res = _board.ProcessMatchesAndGravity(false, false, false, false);
            if (res != null && res.AnyMatched)
            {
                int combo = Math.Min(res.CascadeDepth, 7);
                _sound?.PlaySoundSpatial(AudioMap.ComboPrefix + (combo > 0 ? combo.ToString() : "1"), toX, toY);

                // Voces auténticas del locutor de PopCap según el rendimiento
                if (res.TotalGemsDestroyed >= 8) _sound?.PlaySound(AudioMap.VoiceUnbelievable);
                else if (res.TotalGemsDestroyed >= 6) _sound?.PlaySound(AudioMap.VoiceExtraordinary);
                else if (res.TotalGemsDestroyed >= 5) _sound?.PlaySound(AudioMap.VoiceAwesome);
                else if (res.TotalGemsDestroyed >= 4) _sound?.PlaySound(AudioMap.VoiceExcellent);
                else if (res.CascadeDepth >= 3) _sound?.PlaySound(AudioMap.VoiceSpectacular);
            }
            _selectedX = -1;
            _selectedY = -1;
            _cursorX = toX;
            _cursorY = toY;
            AnnounceCell(_cursorX, _cursorY);
            Invalidate();
        }

        private void AnnounceCurrentMenu()
        {
            string[] items = GetCurrentItems(out int activeIdx);
            string title = GetScreenTitle();
            if (items.Length > 0 && activeIdx < items.Length)
            {
                _talkBack?.Speak(title + ". Opción: " + items[activeIdx] + ". Desliza arriba o abajo para navegar, toca para confirmar.", true);
            }
        }

        private void AnnounceCell(int x, int y)
        {
            Gem g = _board.GetGem(x, y);
            string col = ((char)('A' + x)).ToString();
            int row = y + 1;
            string gemName = g != null ? g.GetNameLocalized() : "Vacío";

            var moves = HintFinder.GetValidMovesFrom(_board, x, y);
            string movesDesc = "";
            if (moves != null && moves.Count > 0)
            {
                List<string> dirs = new List<string>();
                foreach (var m in moves)
                {
                    if (m.Key == 1) dirs.Add("derecha");
                    else if (m.Key == -1) dirs.Add("izquierda");
                    else if (m.Value == 1) dirs.Add("abajo");
                    else if (m.Value == -1) dirs.Add("arriba");
                }
                movesDesc = ". Puedes mover hacia " + string.Join(" o ", dirs);
            }

            string desc = string.Format("{0}{1}: {2}{3}", col, row, gemName, movesDesc);
            _talkBack?.Speak(desc, true);
        }
    }

    public class GameAccessibilityNodeProvider : AccessibilityNodeProvider
    {
        private readonly GameScreenView _view;

        public const int VIRTUAL_ID_HINT = 200;
        public const int VIRTUAL_ID_PAUSE = 201;
        public const int VIRTUAL_BOARD_BASE = 100;

        public GameAccessibilityNodeProvider(GameScreenView view)
        {
            _view = view;
        }

        public override AccessibilityNodeInfo CreateAccessibilityNodeInfo(int virtualViewId)
        {
            if (virtualViewId == View.NoId)
            {
                var root = AccessibilityNodeInfo.Obtain(_view);
                _view.OnInitializeAccessibilityNodeInfo(root);

                if (_view.CurrentScreen == AndroidGameScreen.Playing)
                {
                    for (int y = 0; y < Board.Rows; y++)
                    {
                        for (int x = 0; x < Board.Cols; x++)
                        {
                            root.AddChild(_view, VIRTUAL_BOARD_BASE + (y * Board.Cols + x));
                        }
                    }
                    root.AddChild(_view, VIRTUAL_ID_HINT);
                    root.AddChild(_view, VIRTUAL_ID_PAUSE);
                }
                else if (_view.CurrentScreen != AndroidGameScreen.Loading)
                {
                    string[] items = _view.GetCurrentItems(out int activeIdx);
                    for (int i = 0; i < items.Length; i++)
                    {
                        root.AddChild(_view, i);
                    }
                }
                return root;
            }

            var node = AccessibilityNodeInfo.Obtain(_view, virtualViewId);
            node.PackageName = _view.Context.PackageName;
            node.ClassName = "android.widget.Button";
            node.Source = _view;
            node.VisibleToUser = true;
            node.Enabled = true;
            node.Focusable = true;
            node.Clickable = true;
            node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClick);
            node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionAccessibilityFocus);
            node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClearAccessibilityFocus);

            if (_view.CurrentScreen == AndroidGameScreen.Playing)
            {
                int boardHeight = _view.Height - 40;
                int tileSize = boardHeight / Board.Rows;
                int offsetX = 30;
                int offsetY = 20;

                if (virtualViewId == VIRTUAL_ID_HINT)
                {
                    int panelLeft = offsetX + (Board.Cols * tileSize) + 40;
                    int panelWidth = _view.Width - panelLeft - 30;
                    Rect rect = new Rect(panelLeft, 60, panelLeft + panelWidth, 160);
                    node.SetBoundsInParent(rect);
                    node.Text = "💡 " + Localization.Get("HintTitle");
                    node.ContentDescription = "Botón de Pista. Toca dos veces para encontrar un movimiento sugerido.";
                    return node;
                }

                if (virtualViewId == VIRTUAL_ID_PAUSE)
                {
                    int panelLeft = offsetX + (Board.Cols * tileSize) + 40;
                    int panelWidth = _view.Width - panelLeft - 30;
                    Rect rect = new Rect(panelLeft, 190, panelLeft + panelWidth, 290);
                    node.SetBoundsInParent(rect);
                    node.Text = "⏸️ " + Localization.Get("PauseTitle");
                    node.ContentDescription = "Botón de Pausa. Toca dos veces para pausar la partida o volver al menú.";
                    return node;
                }

                if (virtualViewId >= VIRTUAL_BOARD_BASE && virtualViewId < VIRTUAL_BOARD_BASE + 64)
                {
                    int idx = virtualViewId - VIRTUAL_BOARD_BASE;
                    int x = idx % Board.Cols;
                    int y = idx / Board.Cols;

                    int left = offsetX + (x * tileSize) + 2;
                    int top = offsetY + (y * tileSize) + 2;
                    Rect rect = new Rect(left, top, left + tileSize - 4, top + tileSize - 4);
                    node.SetBoundsInParent(rect);

                    Gem g = _view.BoardInstance?.GetGem(x, y);
                    string colLetter = ((char)('A' + x)).ToString();
                    int rowNum = y + 1;
                    string gemName = g != null ? g.GetNameLocalized() : "Vacío";

                    var moves = HintFinder.GetValidMovesFrom(_view.BoardInstance, x, y);
                    string movesDesc = "";
                    if (moves != null && moves.Count > 0)
                    {
                        List<string> dirs = new List<string>();
                        foreach (var m in moves)
                        {
                            if (m.Key == 1) dirs.Add("derecha");
                            else if (m.Key == -1) dirs.Add("izquierda");
                            else if (m.Value == 1) dirs.Add("abajo");
                            else if (m.Value == -1) dirs.Add("arriba");
                        }
                        movesDesc = ". Movimientos válidos hacia " + string.Join(" o ", dirs);
                    }

                    node.Text = string.Format("{0}{1}: {2}", colLetter, rowNum, gemName);
                    node.ContentDescription = node.Text + movesDesc;
                    return node;
                }
            }
            else
            {
                string[] items = _view.GetCurrentItems(out int activeIdx);
                if (virtualViewId >= 0 && virtualViewId < items.Length)
                {
                    int startY = 110;
                    int itemHeight = 70;
                    int top = startY + (virtualViewId * itemHeight);
                    Rect rect = new Rect(50, top, _view.Width - 50, top + itemHeight - 10);
                    node.SetBoundsInParent(rect);
                    node.Text = items[virtualViewId];
                    node.ContentDescription = string.Format("Opción {0} de {1}: {2}", virtualViewId + 1, items.Length, items[virtualViewId]);
                    return node;
                }
            }

            return node;
        }

        public override bool PerformAction(int virtualViewId, int action, Android.OS.Bundle arguments)
        {
            if (action == (int)AccessibilityAction.Click)
            {
                if (_view.CurrentScreen == AndroidGameScreen.Playing)
                {
                    if (virtualViewId == VIRTUAL_ID_HINT)
                    {
                        _view.TriggerHint();
                        return true;
                    }
                    if (virtualViewId == VIRTUAL_ID_PAUSE)
                    {
                        _view.TogglePause();
                        return true;
                    }
                    if (virtualViewId >= VIRTUAL_BOARD_BASE && virtualViewId < VIRTUAL_BOARD_BASE + 64)
                    {
                        int idx = virtualViewId - VIRTUAL_BOARD_BASE;
                        int x = idx % Board.Cols;
                        int y = idx / Board.Cols;
                        _view.SelectOrSwapCell(x, y);
                        return true;
                    }
                }
                else
                {
                    string[] items = _view.GetCurrentItems(out int activeIdx);
                    if (virtualViewId >= 0 && virtualViewId < items.Length)
                    {
                        _view.ExecuteMenuItem(virtualViewId);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
