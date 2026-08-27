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
using Bejeweled3Accessible.AndroidApp.Update;

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
        QuestRelicScreen,
        QuestChallengeScreen,
        ProfileSelectScreen,
        OptionsScreen,
        AudioSchool,
        ZenOptionsScreen,
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
        private readonly GameOptions _options;
        private BadgeManager _badgeMgr;
        private Board _board;

        private AndroidGameScreen _currentScreen = AndroidGameScreen.Loading;
        private int _loadingProgress = 0;
        private int _menuIdx = 0;
        private int _gameModeIdx = 0;
        private int _badgeIdx = 0;
        private int _recordsIdx = 0;
        private int _tutorialIdx = 0;
        private int _relicIdx = 0;
        private int _questChallengeIdx = 0;
        private int _profileIdx = 0;
        private int _optionsIdx = 0;
        private int _audioSchoolIdx = 0;
        private int _zenOptionsIdx = 0;
        private int _pauseIdx = 0;
        private int _gameOverIdx = 0;
        private int _score = 0;
        private int _level = 1;
        private int _shufflesRemaining = 3;
        private string _currentModeKey = "ModeClassic";
        private readonly List<GemColor> _pokerCards = new List<GemColor>();
        private int _pokerSkulls = 0;
        private int _pokerSkullCharge = 0;
        private int _pokerHandBonus = 0;
        private readonly int[] _iceColumns = new int[8];
        private readonly int[] _iceSkullTicks = new int[8];
        private int _cursorX = 3, _cursorY = 3;
        private int _selectedX = -1, _selectedY = -1;
        private float _startX, _startY;
        private bool _swapExecutedInCurrentTouch = false;

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

        private readonly System.Action _onPauseRequested;

        public GameScreenView(Context context, TalkBackBridge talkBack, AndroidSoundEngine sound, string modeKey, System.Action onPauseRequested) : base(context)
        {
            _context = context;
            _talkBack = talkBack;
            _sound = sound;
            _onPauseRequested = onPauseRequested;

            _talkBack?.AttachView(this);

            _profileMgr = ProfileManager.Load();
            _options = GameOptions.Load();

            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "Jugador 1";
            _badgeMgr = BadgeManager.Load(profName);

            Focusable = true;
            Clickable = true;
            ImportantForAccessibility = ImportantForAccessibility.Yes;

            StartGame(modeKey);
        }

        public string CurrentModeKey => _currentModeKey;

        public void Resume()
        {
            _sound?.PlayMusic(_currentModeKey == "ModeZen" ? MusicMap.FileName(MusicMap.ZenPart1) : MusicMap.FileName(MusicMap.ClassicPart1));
            _talkBack?.Speak("Juego reanudado.", true);
            Invalidate();
        }

        // Reanuda la musica tras volver del segundo plano, sin anunciar (lo gestiona el sistema).
        public void ResumePlayback()
        {
            if (_currentScreen == AndroidGameScreen.Playing)
            {
                _sound?.PlayMusic(_currentModeKey == "ModeZen" ? MusicMap.FileName(MusicMap.ZenPart1) : MusicMap.FileName(MusicMap.ClassicPart1));
            }
        }

        public void TogglePause()
        {
            _sound?.PlaySound(AudioMap.ButtonPress);
            _onPauseRequested?.Invoke();
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
                Localization.Get("MenuAudioSchool"),
                Localization.Get("MenuUpdateCheck"),
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
                Localization.Get("OptSoundVol", _sound != null ? _sound.SfxVol : 100),
                Localization.Get("OptMusicVol", _sound != null ? _sound.MusicVol : 80),
                Localization.Get("OptVoiceVol", _sound != null ? _sound.VoiceVol : 100),
                Localization.Get("OptBinaural", (_sound != null && _sound.BinauralEnabled) ? Localization.Get("StateOn") : Localization.Get("StateOff")),
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

        private string[] GetQuestRelicItems()
        {
            var items = new List<string>();
            for (int i = 1; i <= 5; i++)
            {
                int relicIdx = i - 1;
                int done = Progress.CountCompletedInRelic(relicIdx);
                items.Add(Localization.Get("Relic" + i) + (done >= 8 ? Localization.Get("QuestCompletedMark") : " (" + done + " de 8)"));
            }
            items.Add(Localization.Get("OptBack"));
            return items.ToArray();
        }

        private string[] GetQuestChallengeItems()
        {
            var items = new List<string>();
            QuestMission[] missions = QuestManager.GetRelicMissions(_relicIdx);
            foreach (var m in missions)
            {
                string item = m.GetName();
                if (Progress.IsQuestMissionComplete(m.MissionIndex))
                    item += Localization.Get("QuestCompletedMark");
                items.Add(item);
            }
            items.Add(Localization.Get("OptBack"));
            return items.ToArray();
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

        private string[] GetAudioSchoolItems()
        {
            bool en = Localization.CurrentLanguage == Language.English;
            Func<string, string, string> L = (es, e) => en ? e : es;
            var items = new List<string>();
            string[] cols = { "A", "B", "C", "D", "E", "F", "G", "H" };
            for (int i = 0; i < 8; i++)
                items.Add(L(string.Format("Columna {0} (izquierda a derecha)", cols[i]),
                            string.Format("Column {0} (left to right)", cols[i])));
            items.Add(L("Profundidad frente (cerca)", "Front depth (near)"));
            items.Add(L("Profundidad fondo (lejos)", "Back depth (far)"));
            items.Add(L("Barrido izquierda -> derecha", "Sweep left -> right"));
            items.Add(L("Barrido frente -> fondo", "Sweep front -> back"));
            items.Add(Localization.Get("OptBack"));
            return items.ToArray();
        }

        private string[] GetZenOptionsMenuItems()
        {
            string ambStr = _options.ZenAmbient != (int)AmbientType.None ? AmbientHelper.GetAmbientName((AmbientType)_options.ZenAmbient) : Localization.Get("StateDisabled");
            string manStr = _options.ZenMantras ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled");
            string breathStr = _options.ZenBreath ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled");

            return new string[]
            {
                Localization.Get("ZenOptAmbient", ambStr),
                Localization.Get("ZenOptMantras", manStr),
                Localization.Get("ZenOptBreath", breathStr),
                Localization.Get("OptBack")
            };
        }

        private string[] GetGameOverItems()
        {
            return new string[]
            {
                Localization.Get("GameOverReplay"),
                Localization.Get("GameOverMenu")
            };
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
                case AndroidGameScreen.QuestRelicScreen:
                    activeIdx = _relicIdx;
                    return GetQuestRelicItems();
                case AndroidGameScreen.QuestChallengeScreen:
                    activeIdx = _questChallengeIdx;
                    return GetQuestChallengeItems();
                case AndroidGameScreen.ProfileSelectScreen:
                    activeIdx = _profileIdx;
                    return GetProfileSelectItems();
                case AndroidGameScreen.OptionsScreen:
                    activeIdx = _optionsIdx;
                    return GetOptionsMenuItems();
                case AndroidGameScreen.AudioSchool:
                    activeIdx = _audioSchoolIdx;
                    return GetAudioSchoolItems();
                case AndroidGameScreen.ZenOptionsScreen:
                    activeIdx = _zenOptionsIdx;
                    return GetZenOptionsMenuItems();
                case AndroidGameScreen.PauseMenu:
                    activeIdx = _pauseIdx;
                    return GetPauseMenuItems();
                case AndroidGameScreen.GameOver:
                    activeIdx = _gameOverIdx;
                    return GetGameOverItems();
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
                case AndroidGameScreen.QuestRelicScreen: _relicIdx = idx; break;
                case AndroidGameScreen.QuestChallengeScreen: _questChallengeIdx = idx; break;
                case AndroidGameScreen.ProfileSelectScreen: _profileIdx = idx; break;
                case AndroidGameScreen.OptionsScreen: _optionsIdx = idx; break;
                case AndroidGameScreen.AudioSchool: _audioSchoolIdx = idx; break;
                case AndroidGameScreen.ZenOptionsScreen: _zenOptionsIdx = idx; break;
                case AndroidGameScreen.PauseMenu: _pauseIdx = idx; break;
                case AndroidGameScreen.GameOver: _gameOverIdx = idx; break;
            }
        }

        public AndroidGameScreen CurrentScreen => _currentScreen;
        public Board BoardInstance => _board;
        public int SoundVolume => _sound != null ? _sound.SfxVol : 100;
        public int MusicVolume => _sound != null ? _sound.MusicVol : 80;
        public int VoiceVolume => _sound != null ? _sound.VoiceVol : 100;

        public void AdjustOptionSlider(int sliderIdx, int delta)
        {
            if (_currentScreen != AndroidGameScreen.OptionsScreen) return;

            if (sliderIdx == 0) // Sound
            {
                int next = Math.Max(0, Math.Min(100, (_options.SoundVolume + delta)));
                _options.SoundVolume = next;
                _sound.SfxVol = next;
                _sound.PlaySound(AudioMap.Select);
                _options.Save();
                _talkBack?.Speak(Localization.Get("OptSoundVol", next), true);
            }
            else if (sliderIdx == 1) // Music
            {
                int next = Math.Max(0, Math.Min(100, (_options.MusicVolume + delta)));
                _options.MusicVolume = next;
                _sound.MusicVol = next;
                _sound.UpdateMusicVolume();
                _sound.PlaySound(AudioMap.Select);
                _options.Save();
                _talkBack?.Speak(Localization.Get("OptMusicVol", next), true);
            }
            else if (sliderIdx == 2) // Voice
            {
                int next = Math.Max(0, Math.Min(100, (_options.VoiceVolume + delta)));
                _options.VoiceVolume = next;
                _sound.VoiceVol = next;
                _sound.PlaySound(AudioMap.VoiceAwesome);
                _options.Save();
                _talkBack?.Speak(Localization.Get("OptVoiceVol", next), true);
            }
            Invalidate();
        }

        private GameAccessibilityNodeProvider _nodeProvider;

        public override AccessibilityNodeProvider AccessibilityNodeProvider
        {
            get
            {
                if (_nodeProvider == null)
                {
                    _nodeProvider = new GameAccessibilityNodeProvider(this);
                }
                return _nodeProvider;
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
            // Mantener la pantalla encendida solo durante la partida activa: en
            // menues y al segundo plano el sistema puede apagarla (ahorro de bateria).
            this.KeepScreenOn = (_currentScreen == AndroidGameScreen.Playing);
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
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density < 1.0f) density = 1.0f;

            _paint.Color = Color.White;
            _paint.TextSize = 22f * density;
            _paint.SetTypeface(Typeface.DefaultBold);

            string title = GetScreenTitle();
            canvas.DrawText(title, 20f * density, 45f * density, _paint);

            int startY = (int)(65f * density);
            int availableHeight = Height - startY - (int)(20f * density);
            int baseItemHeight = (int)(55f * density);
            int itemHeight = items.Length > 0 ? Math.Min(baseItemHeight, Math.Max((int)(40f * density), availableHeight / items.Length)) : baseItemHeight;

            for (int i = 0; i < items.Length; i++)
            {
                int top = startY + (i * itemHeight);
                RectF itemRect = new RectF(16f * density, top, Width - (16f * density), top + itemHeight - (6f * density));

                if (i == activeIdx)
                {
                    _paint.Color = Color.Rgb(255, 200, 0);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(itemRect, 10f * density, 10f * density, _paint);

                    _paint.Color = Color.Black;
                }
                else
                {
                    _paint.Color = Color.Argb(50, 255, 255, 255);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(itemRect, 10f * density, 10f * density, _paint);

                    _paint.Color = Color.White;
                }

                _paint.TextSize = Math.Min(18f * density, (itemHeight * 0.45f));
                _paint.SetTypeface(Typeface.Default);
                canvas.DrawText(items[i], 26f * density, top + (itemHeight / 2f) + (6f * density), _paint);
            }
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            // Al rotar a apaisado (la Activity no se recrea por ConfigurationChanges)
            // el tablero se redibuja en las nuevas dimensiones, pero TalkBack cachea
            // los limites (Rects setBoundsInParent/setBoundsInScreen) de los 64 nodos
            // virtuales del modo vertical. Avisar con WindowContentChanged para que
            // reconsulte las coordenadas en horizontal; si no, el toque no encuentra
            // ningun nodo bajo el dedo.
            RefreshAccessibilityStructure();
        }

        // Notifica a TalkBack que el arbol virtual cambio de tamano/posicion
        // (rotacion a apaisado) para que reconsulte los limites de los nodos.
        internal void RefreshAccessibilityStructure()
        {
            try
            {
                var evt = Android.Views.Accessibility.AccessibilityEvent.Obtain(Android.Views.Accessibility.EventTypes.WindowContentChanged);
                this.Parent?.RequestSendAccessibilityEvent(this, evt);
            }
            catch { }
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
                case AndroidGameScreen.QuestRelicScreen: return Localization.Get("ModeQuest");
                case AndroidGameScreen.QuestChallengeScreen: return Localization.Get("Relic" + (_relicIdx + 1));
                case AndroidGameScreen.ProfileSelectScreen: return Localization.Get("ProfileSelectTitle");
                case AndroidGameScreen.OptionsScreen: return Localization.Get("OptionsTitle");
                case AndroidGameScreen.AudioSchool: return Localization.Get("AudioSchoolTitle");
                case AndroidGameScreen.ZenOptionsScreen: return Localization.Get("ZenOptionsTitle");
                case AndroidGameScreen.PauseMenu: return Localization.Get("PauseTitle");
                case AndroidGameScreen.GameOver: return Localization.Get("GameOverTitle");
                default: return "Bejeweled 3";
            }
        }

        private void DrawLandscapeBoard(Canvas canvas)
        {
            if (_board == null) return;

            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density < 1.0f) density = 1.0f;

            int marginY = (int)(15f * density);
            int boardHeight = Height - (marginY * 2);
            int tileSize = Math.Max(1, boardHeight / Board.Rows);
            int offsetX = (int)(20f * density);
            int offsetY = marginY;

            for (int y = 0; y < Board.Rows; y++)
            {
                for (int x = 0; x < Board.Cols; x++)
                {
                    int left = offsetX + (x * tileSize) + (int)(2f * density);
                    int top = offsetY + (y * tileSize) + (int)(2f * density);
                    int right = left + tileSize - (int)(4f * density);
                    int bottom = top + tileSize - (int)(4f * density);
                    RectF rect = new RectF(left, top, right, bottom);

                    _paint.Color = Color.Argb(35, 255, 255, 255);
                    _paint.SetStyle(Paint.Style.Fill);
                    canvas.DrawRoundRect(rect, 6f * density, 6f * density, _paint);

                    Gem gem = _board.GetGem(x, y);
                    if (gem != null)
                    {
                        Color c = _gemColors.ContainsKey(gem.Color) ? _gemColors[gem.Color] : Color.Gray;
                        _paint.Color = c;
                        _paint.SetStyle(Paint.Style.Fill);
                        canvas.DrawCircle(rect.CenterX(), rect.CenterY(), (tileSize - (6f * density)) / 2f, _paint);
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
                        _paint.StrokeWidth = 4f * density;
                        canvas.DrawRoundRect(rect, 6f * density, 6f * density, _paint);
                    }
                    else if (_cursorX == x && _cursorY == y)
                    {
                        _paint.Color = Color.Yellow;
                        _paint.SetStyle(Paint.Style.Stroke);
                        _paint.StrokeWidth = 3f * density;
                        canvas.DrawRoundRect(rect, 6f * density, 6f * density, _paint);
                    }
                }
            }

            int panelLeft = offsetX + (Board.Cols * tileSize) + (int)(25f * density);
            int panelWidth = Width - panelLeft - (int)(20f * density);
            int btnHeight = (int)(55f * density);

            RectF hintRect = new RectF(panelLeft, offsetY + (int)(20f * density), panelLeft + panelWidth, offsetY + (int)(20f * density) + btnHeight);
            _paint.Color = Color.Rgb(0, 150, 255);
            _paint.SetStyle(Paint.Style.Fill);
            canvas.DrawRoundRect(hintRect, 10f * density, 10f * density, _paint);

            _paint.Color = Color.White;
            _paint.TextSize = 18f * density;
            _paint.SetTypeface(Typeface.DefaultBold);
            canvas.DrawText("💡 " + Localization.Get("HintTitle"), panelLeft + (15f * density), hintRect.CenterY() + (6f * density), _paint);

            // Boton PAUSA / MENU
            RectF pauseRect = new RectF(panelLeft, hintRect.Bottom + (int)(20f * density), panelLeft + panelWidth, hintRect.Bottom + (int)(20f * density) + btnHeight);
            _paint.Color = Color.Rgb(220, 50, 50);
            _paint.SetStyle(Paint.Style.Fill);
            canvas.DrawRoundRect(pauseRect, 10f * density, 10f * density, _paint);

            _paint.Color = Color.White;
            canvas.DrawText("⏸️ " + Localization.Get("PauseTitle"), panelLeft + (15f * density), pauseRect.CenterY() + (6f * density), _paint);
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
            // El juego es jugable por tactil tambien con un lector de pantalla
            // activo: bajo TalkBack el dedo selecciona/intercambia celdas igual
            // que sin el. No bloqueamos el touch (provocaba pantalla congelada).
            if (_currentScreen == AndroidGameScreen.Playing)
            {
                return HandleBoardTouch(e);
            }

            return HandleMenuTouch(e);
        }

        private bool HandleMenuTouch(MotionEvent e)
        {
            string[] items = GetCurrentItems(out int activeIdx);
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density < 1.0f) density = 1.0f;
            bool explore = _talkBack != null && _talkBack.IsTouchExplorationEnabled;

            int startY = (int)(65f * density);
            int availableHeight = Height - startY - (int)(20f * density);
            int baseItemHeight = (int)(55f * density);
            int itemHeight = items.Length > 0 ? Math.Min(baseItemHeight, Math.Max((int)(40f * density), availableHeight / items.Length)) : baseItemHeight;

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

                // Deslizamiento vertical deliberado (navegacion anterior/siguiente)
                if (!explore && Math.Abs(deltaY) > 80 && Math.Abs(deltaY) > Math.Abs(deltaX) * 1.5f)
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
                // Deslizamiento horizontal izquierdo (volver)
                else if (!explore && deltaX < -120 && Math.Abs(deltaX) > Math.Abs(deltaY) * 1.5f)
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
                // Toque estático sin desplazamiento: activar SOLO si se pulsa sobre la opción ya seleccionada
                else if (Math.Abs(deltaX) < 25 && Math.Abs(deltaY) < 25)
                {
                    int clickedIdx = (int)((e.GetY() - startY) / itemHeight);
                    if (clickedIdx >= 0 && clickedIdx < items.Length)
                    {
                        if (clickedIdx == activeIdx)
                        {
                            _sound?.PlaySound(AudioMap.ButtonPress);
                            ExecuteMenuItem(clickedIdx);
                            Invalidate();
                            return true;
                        }
                        else
                        {
                            SetActiveIndex(clickedIdx);
                            _sound?.PlaySound(AudioMap.ButtonMouseover);
                            _talkBack?.Speak(items[clickedIdx], true);
                            Invalidate();
                            return true;
                        }
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
                else if (idx == 5)
                {
                    Localization.ToggleLanguage();
                    _options.SelectedLanguage = Localization.CurrentLanguage;
                    _options.Save();
                    _talkBack?.Speak(GetMainMenuItems()[5], true);
                    Invalidate();
                    return;
                }
                else if (idx == 6) { _currentScreen = AndroidGameScreen.OptionsScreen; _optionsIdx = 0; }
                else if (idx == 7) { _currentScreen = AndroidGameScreen.AudioSchool; _audioSchoolIdx = 0; }
                else if (idx == 8)
                {
                    _sound?.PlaySound(AudioMap.Select);
                    _talkBack?.Speak(Localization.Get("UpdateChecking"), true);
                    Task.Run(async () =>
                    {
                        var info = await AndroidAutoUpdater.CheckForUpdatesAsync();
                        Post(() =>
                        {
                            if (info.IsNewer)
                            {
                                string msg = Localization.Get("UpdateFoundNoNotes", AndroidAutoUpdater.CurrentVersion, info.Tag);
                                _sound?.PlaySound(AudioMap.Rankup);
                                _talkBack?.Speak(msg + ". Abriendo enlace de descarga...", true);
                                AndroidAutoUpdater.OpenDownloadOrRelease(_context, info);
                            }
                            else
                            {
                                string msg = Localization.Get("UpdateNone", AndroidAutoUpdater.CurrentVersion);
                                _sound?.PlaySound(AudioMap.ButtonPress);
                                _talkBack?.Speak(msg, true);
                            }
                        });
                    });
                    return;
                }
                else if (idx == 9)
                {
                    _sound?.PlaySound(AudioMap.VoiceGoodbye);
                    _talkBack?.Speak(Localization.CurrentLanguage == Language.Spanish ? "¡Adiós!" : "Goodbye!", true);
                    PostDelayed(() => { System.Environment.Exit(0); }, 1000);
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.GameSelect)
            {
                string[] keys = GetGameModeKeys();
                string selectedKey = keys[idx];
                if (selectedKey == "BackToMain")
                {
                    _currentScreen = AndroidGameScreen.MainMenu;
                }
                else if (selectedKey == "ModeQuest")
                {
                    _sound?.PlaySound(AudioMap.QuestMenuButton1);
                    _currentScreen = AndroidGameScreen.QuestRelicScreen;
                    _relicIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else
                {
                    StartGame(selectedKey);
                }
            }
            else if (_currentScreen == AndroidGameScreen.QuestRelicScreen)
            {
                string[] items = GetQuestRelicItems();
                if (idx == items.Length - 1) // Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _currentScreen = AndroidGameScreen.GameSelect;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else
                {
                    _relicIdx = idx;
                    _sound?.PlaySound(AudioMap.QuestMenuButton1);
                    _sound?.PlaySound(AudioMap.QuestMenuRelicRevealedObject);
                    _currentScreen = AndroidGameScreen.QuestChallengeScreen;
                    _questChallengeIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.QuestChallengeScreen)
            {
                string[] items = GetQuestChallengeItems();
                if (idx == items.Length - 1) // Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _currentScreen = AndroidGameScreen.QuestRelicScreen;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else
                {
                    _questChallengeIdx = idx;
                    _sound?.PlaySound(AudioMap.QuestMenuButton1);
                    StartGame("ModeQuest");
                    return;
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
                else if (idx == _profileMgr.Profiles.Count) // Crear nuevo
                {
                    PromptCreateProfile();
                    return;
                }
                else // Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _menuIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.OptionsScreen)
            {
                string[] opts = GetOptionsMenuItems();
                if (idx == opts.Length - 1) // Opción Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _options.Save();
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _menuIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else if (idx == 0) // Volumen de Efectos
                {
                    _options.SoundVolume = (_options.SoundVolume + 10) % 110;
                    if (_options.SoundVolume == 0 && _sound.SfxVol == 100) _options.SoundVolume = 10;
                    _sound.SfxVol = _options.SoundVolume;
                    _sound.PlaySound(AudioMap.Select);
                    _options.Save();
                    _talkBack?.Speak(Localization.Get("OptSoundVol", _sound.SfxVol), true);
                }
                else if (idx == 1) // Volumen de Música
                {
                    _options.MusicVolume = (_options.MusicVolume + 10) % 110;
                    if (_options.MusicVolume == 0 && _sound.MusicVol == 100) _options.MusicVolume = 10;
                    _sound.MusicVol = _options.MusicVolume;
                    _sound.UpdateMusicVolume();
                    _sound.PlaySound(AudioMap.Select);
                    _options.Save();
                    _talkBack?.Speak(Localization.Get("OptMusicVol", _sound.MusicVol), true);
                }
                else if (idx == 2) // Volumen de Voz
                {
                    _options.VoiceVolume = (_options.VoiceVolume + 10) % 110;
                    if (_options.VoiceVolume == 0 && _sound.VoiceVol == 100) _options.VoiceVolume = 10;
                    _sound.VoiceVol = _options.VoiceVolume;
                    _sound.PlaySound(AudioMap.VoiceAwesome);
                    _options.Save();
                    _talkBack?.Speak(Localization.Get("OptVoiceVol", _sound.VoiceVol), true);
                }
                else if (idx == 3) // Audio Binaural
                {
                    _options.BinauralEnabled = !_options.BinauralEnabled;
                    _sound.BinauralEnabled = _options.BinauralEnabled;
                    _sound.PlaySound(AudioMap.Select);
                    _options.Save();
                    _talkBack?.Speak(Localization.Get("OptBinaural", _sound.BinauralEnabled ? Localization.Get("StateOn") : Localization.Get("StateOff")), true);
                }
                Invalidate();
            }
            else if (_currentScreen == AndroidGameScreen.BadgesScreen ||
                     _currentScreen == AndroidGameScreen.RecordsScreen ||
                     _currentScreen == AndroidGameScreen.TutorialScreen)
            {
                string[] curItems = GetCurrentItems(out int dummy);
                if (idx == curItems.Length - 1) // Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _menuIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else
                {
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    _talkBack?.Speak(curItems[idx], true);
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.AudioSchool)
            {
                string[] schoolItems = GetAudioSchoolItems();
                if (idx == schoolItems.Length - 1) // Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _menuIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else
                {
                    _talkBack?.Speak(schoolItems[idx], true);
                    PlayAudioSchoolTest(idx);
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.ZenOptionsScreen)
            {
                string[] zenItems = GetZenOptionsMenuItems();
                if (idx == zenItems.Length - 1) // Volver
                {
                    _sound?.PlaySound(AudioMap.Backtomain);
                    _options.Save();
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _menuIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else if (idx == 0) // Ambient
                {
                    int maxAmb = Enum.GetValues(typeof(AmbientType)).Length;
                    _options.ZenAmbient = (_options.ZenAmbient + 1) % maxAmb;
                    _sound?.PlaySound(AudioMap.ZenDropdownbutton);
                    _sound?.PlaySound(AudioMap.ZenNecklacePrefix + ((_options.ZenAmbient % 4) + 1));
                    _options.Save();
                    string ambStr = _options.ZenAmbient != (int)AmbientType.None ? AmbientHelper.GetAmbientName((AmbientType)_options.ZenAmbient) : Localization.Get("StateDisabled");
                    _talkBack?.Speak(Localization.Get("ZenOptAmbient", ambStr), true);
                }
                else if (idx == 1) // Mantras
                {
                    _options.ZenMantras = !_options.ZenMantras;
                    _sound?.PlaySound(_options.ZenMantras ? AudioMap.ZenCheckon : AudioMap.ZenCheckoff);
                    _options.Save();
                    _talkBack?.Speak(Localization.Get("ZenOptMantras", _options.ZenMantras ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled")), true);
                }
                else if (idx == 2) // Breath
                {
                    _options.ZenBreath = !_options.ZenBreath;
                    _sound?.PlaySound(_options.ZenBreath ? AudioMap.ZenCheckon : AudioMap.ZenCheckoff);
                    _options.Save();
                    _talkBack?.Speak(Localization.Get("ZenOptBreath", _options.ZenBreath ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled")), true);
                }
                Invalidate();
                return;
            }
            else if (_currentScreen == AndroidGameScreen.GameOver)
            {
                if (idx == 0) // Replay
                {
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    StartGame(_currentModeKey);
                    return;
                }
                else // Menu
                {
                    if (_context is MainActivity mainAct)
                    {
                        mainAct.ReturnToMainMenu();
                    }
                    return;
                }
            }
            else if (_currentScreen == AndroidGameScreen.PauseMenu)
            {
                if (idx == 0) // Reanudar
                {
                    if (_context is MainActivity mainAct)
                    {
                        mainAct.SetDesiredOrientation(true); // Horizontal para el tablero
                    }

                    _currentScreen = AndroidGameScreen.Playing;
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    _sound?.PlayMusic(_currentModeKey == "ModeZen" ? "11 - Zen - Part 1" : "03 - Classic Mode - Part 1");
                    _talkBack?.Speak("Juego reanudado.", true);
                    Invalidate();
                    return;
                }
                else if (idx == 1) // Reiniciar
                {
                    StartGame(_currentModeKey);
                    return;
                }
                else if (idx == 2) // Opciones
                {
                    _currentScreen = AndroidGameScreen.OptionsScreen;
                    _optionsIdx = 0;
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    AnnounceCurrentMenu();
                    Invalidate();
                    return;
                }
                else if (idx == 3) // Menú Principal
                {
                    if (_context is MainActivity mainAct)
                    {
                        mainAct.ReturnToMainMenu();
                    }
                    return;
                }
            }
            else
            {
                _currentScreen = AndroidGameScreen.MainMenu;
            }

            AnnounceCurrentMenu();
        }

        private void PromptCreateProfile()
        {
            if (!(_context is Android.App.Activity act)) return;

            act.RunOnUiThread(() =>
            {
                var builder = new Android.App.AlertDialog.Builder(act);
                builder.SetTitle(Localization.Get("ProfileCreateNew"));
                builder.SetMessage(Localization.Get("EnterNamePrompt"));

                var input = new Android.Widget.EditText(act)
                {
                    Text = "Jugador " + (_profileMgr.Profiles.Count + 1),
                    ImportantForAccessibility = ImportantForAccessibility.Yes
                };
                input.SetSelection(input.Text.Length);
                builder.SetView(input);

                builder.SetPositiveButton(Localization.Get("EnterNameConfirm"), (sender, args) =>
                {
                    string name = input.Text?.Trim();
                    if (string.IsNullOrEmpty(name)) name = "Jugador " + (_profileMgr.Profiles.Count + 1);

                    _profileMgr.Profiles.Add(new PlayerProfile(name));
                    _profileMgr.CurrentProfileIndex = _profileMgr.Profiles.Count - 1;
                    _profileMgr.Save();
                    _badgeMgr = BadgeManager.Load(name);
                    _sound?.PlaySound(AudioMap.Rankup);
                    _sound?.PlayMusic(MusicMap.MainTheme);
                    _sound?.PlaySound(AudioMap.VoiceWelcometobejeweled);
                    _talkBack?.Speak(string.Format("Nuevo perfil creado: {0}", name), true);
                    _currentScreen = AndroidGameScreen.MainMenu;
                    _menuIdx = 0;
                    AnnounceCurrentMenu();
                    Invalidate();
                });

                builder.SetNegativeButton(Localization.Get("OptBack"), (sender, args) =>
                {
                    _sound?.PlaySound(AudioMap.ButtonPress);
                    if (_profileMgr.Profiles.Count > 0)
                    {
                        _sound?.PlayMusic(MusicMap.MainTheme);
                        _currentScreen = AndroidGameScreen.MainMenu;
                    }
                    else
                    {
                        _currentScreen = AndroidGameScreen.MainMenu;
                    }
                    AnnounceCurrentMenu();
                    Invalidate();
                });

                builder.Show();
            });
        }

        private void PlayAudioSchoolTest(int idx)
        {
            string s = AudioMap.Select;
            if (idx >= 0 && idx <= 7)
            {
                float pan = SpatialAudio.PanColumn(idx);
                _sound?.PlaySoundSpatialPan(pan, 0.0f, s);
            }
            else if (idx == 8)
                _sound?.PlaySoundSpatialPan(0.0f, 0.0f, s);
            else if (idx == 9)
                _sound?.PlaySoundSpatialPan(0.0f, 1.0f, s);
            else if (idx == 10)
            {
                // Barrido izquierda -> derecha: varia el pan gradualmente.
                System.Threading.Tasks.Task.Run(async () =>
                {
                    for (float p = -1.0f; p <= 1.0f; p += 0.25f)
                    {
                        _sound?.PlaySoundSpatialPan(p, 0.0f, s);
                        await System.Threading.Tasks.Task.Delay(120);
                    }
                });
            }
            else if (idx == 11)
            {
                // Barrido frente -> fondo: varia la profundidad gradualmente.
                System.Threading.Tasks.Task.Run(async () =>
                {
                    for (float d = 0.0f; d <= 1.0f; d += 0.2f)
                    {
                        _sound?.PlaySoundSpatialPan(0.0f, d, s);
                        await System.Threading.Tasks.Task.Delay(120);
                    }
                });
            }
        }

        private void StartGame(string modeKey)
        {
            if (_context is MainActivity mainAct)
            {
                mainAct.SetDesiredOrientation(true); // Horizontal para el tablero
            }

            _currentModeKey = modeKey;
            _board = new Board(new Random().Next());
            _currentScreen = AndroidGameScreen.Playing;
            _score = 0;
            _level = 1;
            _shufflesRemaining = 3;
            _pokerCards.Clear();
            _pokerSkulls = 0;
            _pokerSkullCharge = 0;
            _pokerHandBonus = 0;
            for (int i = 0; i < 8; i++)
            {
                _iceColumns[i] = 0;
                _iceSkullTicks[i] = 0;
            }

            _sound?.PlaySound(AudioMap.VoiceGetready);

            if (modeKey == "ModeLightning")
            {
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.Lightning));
            }
            else if (modeKey == "ModePoker")
            {
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.Poker));
            }
            else if (modeKey == "ModeButterflies")
            {
                _board.InitializeButterfliesBoard();
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.Butterflies));
            }
            else if (modeKey == "ModeDiamondMine")
            {
                _board.InitializeDiamondMineBoard();
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.QuestBuriedTreasure));
            }
            else if (modeKey == "ModeIceStorm")
            {
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.IceStorm));
            }
            else if (modeKey == "ModeQuest")
            {
                QuestMission[] missions = QuestManager.GetRelicMissions(_relicIdx);
                if (_questChallengeIdx >= 0 && _questChallengeIdx < missions.Length)
                {
                    QuestMission m = missions[_questChallengeIdx];
                    switch (m.Type)
                    {
                        case QuestType.Butterflies:
                            _board.InitializeButterfliesBoard();
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.Butterflies));
                            break;
                        case QuestType.DiamondMine:
                        case QuestType.GoldRush:
                            _board.InitializeDiamondMineBoard();
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.QuestBuriedTreasure));
                            break;
                        case QuestType.TimeBomb:
                            _board.InitializeBoard(true);
                            _sound?.PlaySound(AudioMap.BombAppears);
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.QuestTimeBombs));
                            break;
                        case QuestType.IceStorm:
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.IceStorm));
                            break;
                        case QuestType.Poker:
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.Poker));
                            break;
                        case QuestType.Avalanche:
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.QuestTurnByTurn));
                            break;
                        default:
                            _sound?.PlayMusic(MusicMap.FileName(MusicMap.QuestTakeYourTime));
                            break;
                    }
                }
                else
                {
                    _sound?.PlayMusic(MusicMap.FileName(MusicMap.QuestTheme));
                }
            }
            else if (modeKey == "ModeZen")
            {
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.ZenPart1));
                if (_options.ZenAmbient != (int)AmbientType.None)
                {
                    _sound?.PlaySound(AmbientHelper.GetAmbientTrack((AmbientType)_options.ZenAmbient), 0.5f);
                }
            }
            else
            {
                _sound?.PlayMusic(MusicMap.FileName(MusicMap.ClassicPart1));
            }

            _talkBack?.Speak(Localization.Get(modeKey) + ". " + Localization.Get("GameReady") + ". Toca una gema para ver hacia dónde moverla o toca los botones de pista y pausa a la derecha.", true);
        }

        protected override bool DispatchHoverEvent(MotionEvent e)
        {
            // TalkBack usa HoverMove/HoverEnter para la exploracion tactil con 1 dedo.
            // Un AccessibilityNodeProvider "crudo" (no ExploreByTouchHelper) depende de
            // que la vista consuma el hover para dar feedback por celda; si no, el base
            // View no lo reenvia al provider y la exploracion de 1 dedo deja de funcionar.
            // Como consumimos el evento, TalkBack no lo recibe y no hay habla duplicada:
            // aqui anunciamos una sola vez por celda al pasar el dedo por encima.
            if (e.Action == MotionEventActions.HoverMove || e.Action == MotionEventActions.HoverEnter)
            {
                float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
                if (density < 1.0f) density = 1.0f;
                int marginY = (int)(15f * density);
                int boardHeight = Height - (marginY * 2);
                int tileSize = Math.Max(1, boardHeight / Board.Rows);
                int offsetX = (int)(20f * density);
                int offsetY = marginY;

                int cellX = (int)((e.GetX() - offsetX) / tileSize);
                int cellY = (int)((e.GetY() - offsetY) / tileSize);

                if (cellX >= 0 && cellX < Board.Cols && cellY >= 0 && cellY < Board.Rows)
                {
                    // Solo anunciar si el dedo cambio de casilla
                    if (_cursorX != cellX || _cursorY != cellY)
                    {
                        _cursorX = cellX;
                        _cursorY = cellY;
                        AnnounceCell(cellX, cellY);
                        Invalidate();
                    }
                }
                return true;
            }
            return base.DispatchHoverEvent(e);
        }

        private bool HandleBoardTouch(MotionEvent e)
        {
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density < 1.0f) density = 1.0f;
            bool explore = _talkBack != null && _talkBack.IsTouchExplorationEnabled;

            int marginY = (int)(15f * density);
            int boardHeight = Height - (marginY * 2);
            int tileSize = Math.Max(1, boardHeight / Board.Rows);
            int offsetX = (int)(20f * density);
            int offsetY = marginY;

            int cellX = (int)((e.GetX() - offsetX) / tileSize);
            int cellY = (int)((e.GetY() - offsetY) / tileSize);

            int panelLeft = offsetX + (Board.Cols * tileSize) + (int)(25f * density);
            int panelWidth = Width - panelLeft - (int)(20f * density);
            int btnHeight = (int)(55f * density);
            int hintTop = offsetY + (int)(20f * density);
            int pauseTop = hintTop + btnHeight + (int)(20f * density);

            if (e.Action == MotionEventActions.Down)
            {
                _startX = e.GetX();
                _startY = e.GetY();
                _swapExecutedInCurrentTouch = false;

                // Verificar si toco el panel lateral de botones
                if (e.GetX() >= panelLeft && e.GetX() <= panelLeft + panelWidth)
                {
                    if (e.GetY() >= hintTop && e.GetY() <= hintTop + btnHeight) // Boton PISTA
                    {
                        TriggerHint();
                        return true;
                    }
                    else if (e.GetY() >= pauseTop && e.GetY() <= pauseTop + btnHeight) // Boton PAUSA
                    {
                        TogglePause();
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
                            _swapExecutedInCurrentTouch = true;
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

                // Si ya ejecutamos el intercambio en el Down (toque de dos celdas
                // contiguas), ignorar el gesto de deslizamiento en el Up para no
                // disparar un segundo swap accidental.
                if (_swapExecutedInCurrentTouch)
                {
                    _swapExecutedInCurrentTouch = false;
                    return true;
                }

                // Con exploracion tactil (TalkBack) se desactiva el gesto de
                // deslizamiento para intercambiar, ya que arrastrar para explorar
                // dispararia swaps no deseados. El intercambio se hace tocando
                // dos celdas contiguas.
                if (!explore && (Math.Abs(deltaX) > 30 || Math.Abs(deltaY) > 30))
                {
                    int swapDx = Math.Abs(deltaX) > Math.Abs(deltaY) ? (deltaX > 0 ? 1 : -1) : 0;
                    int swapDy = Math.Abs(deltaX) > Math.Abs(deltaY) ? 0 : (deltaY > 0 ? 1 : -1);

                    int fromX = (_selectedX >= 0) ? _selectedX : cellX;
                    int fromY = (_selectedY >= 0) ? _selectedY : cellY;
                    int targetX = fromX + swapDx;
                    int targetY = fromY + swapDy;

                    if (targetX >= 0 && targetX < Board.Cols && targetY >= 0 && targetY < Board.Rows)
                    {
                        ExecuteSwap(fromX, fromY, targetX, targetY);
                        return true;
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

        internal void ExecuteSwap(int fromX, int fromY, int toX, int toY)
        {
            _sound?.PlaySoundSpatial(AudioMap.GemHit, toX, toY);
            _board.SwapGems(fromX, fromY, toX, toY);
            CascadeResult res = _board.ProcessMatchesAndGravity(false, false, false, false);
            if (res != null && res.AnyMatched)
            {
                int combo = Math.Min(res.CascadeDepth, 7);
                _sound?.PlaySoundSpatial(AudioMap.ComboPrefix + (combo > 0 ? combo.ToString() : "1"), toX, toY);

                // Explosiones y creación de gemas especiales
                if (res.SupernovaCreated > 0)
                {
                    _sound?.PlaySound(AudioMap.FireworkLaunch);
                    _sound?.PlaySound(AudioMap.FireworkThump);
                    _sound?.PlaySound(AudioMap.FireworkCrackle);
                    _sound?.PlaySound(AudioMap.LasergemCreated);
                    _sound?.PlaySound(AudioMap.ElectroExplode);
                }
                else if (res.HypercubeCreated > 0)
                {
                    _sound?.PlaySound(AudioMap.HypercubeCreate);
                    _sound?.PlaySound(AudioMap.Hyperspace);
                }
                else if (res.StarCreated > 0)
                {
                    _sound?.PlaySound(AudioMap.LasergemCreated);
                    _sound?.PlaySound(AudioMap.ElectroExplode);
                }
                else if (res.FlameCreated > 0)
                {
                    _sound?.PlaySound(AudioMap.PowergemCreated);
                    _sound?.PlaySound(AudioMap.Flamebonus);
                }

                // Lógica de Modos Especiales (Mariposas, Mina de Diamantes, Poker, Tormenta de Hielo)
                bool isButterfliesMode = _currentModeKey == "ModeButterflies";
                bool isDiamondMineActive = _currentModeKey == "ModeDiamondMine";
                bool isPokerActive = _currentModeKey == "ModePoker";
                bool isIceStormActive = _currentModeKey == "ModeIceStorm";

                if (isPokerActive)
                {
                    _sound?.PlaySound(AudioMap.Carddeal);
                    foreach (var color in res.MatchedColors)
                    {
                        _pokerCards.Add(color);
                        _sound?.PlaySound(AudioMap.Cardflip);
                    }

                    _pokerHandBonus += res.FlameDestroyed * 100 + res.StarDestroyed * 250;

                    if (_pokerCards.Count >= 5)
                    {
                        PokerHandType hand = PokerHandEvaluator.Evaluate(_pokerCards);
                        int handPts = PokerHandEvaluator.GetHandPoints(hand) + _pokerHandBonus;
                        bool isBadHand = hand == PokerHandType.HighCard;

                        if (isBadHand)
                        {
                            _pokerSkulls++;
                            _sound?.PlaySound(AudioMap.SkullcoinFlip);
                            _sound?.PlaySound(AudioMap.Skullcoinlose);
                            _sound?.PlaySound(AudioMap.SkullAppear);
                            _sound?.PlaySound(AudioMap.Pokerchips);
                            _pokerCards.Clear();
                            _pokerHandBonus = 0;

                            if (_pokerSkulls >= 5)
                            {
                                _currentScreen = AndroidGameScreen.GameOver;
                                _gameOverIdx = 0;
                                _sound?.PlaySound(AudioMap.SkullBuster);
                                _sound?.PlaySound(AudioMap.VoiceGameover);
                                _talkBack?.Speak(Localization.Get("PokerSkullGameOver") + " " + Localization.Get("GameOver", _score), true);
                                Invalidate();
                                _talkBack?.NotifyStructureChanged();
                                return;
                            }
                            _talkBack?.Speak(Localization.Get("PokerSkullAnnounce", _pokerSkulls), true);
                        }
                        else
                        {
                            _score += handPts;
                            if (hand == PokerHandType.Flush)
                            {
                                _sound?.PlaySound(AudioMap.PokerFlush);
                                _sound?.PlaySound(AudioMap.Skullcoinwin);
                            }
                            else if (hand == PokerHandType.FullHouse)
                            {
                                _sound?.PlaySound(AudioMap.PokerFullhouse);
                                _sound?.PlaySound(AudioMap.Skullcoinlands);
                            }
                            else if (hand == PokerHandType.FourOfAKind)
                            {
                                _sound?.PlaySound(AudioMap.Poker4ofakind);
                                _sound?.PlaySound(AudioMap.Skullcoinwin);
                            }
                            else
                            {
                                _sound?.PlaySound(AudioMap.Pokerscore);
                                _sound?.PlaySound(AudioMap.SkullBuster);
                            }

                            _sound?.PlaySound(AudioMap.Pokerchips);
                            _talkBack?.Speak(Localization.Get("PokerHandScored", Localization.GetPokerHandName(hand), handPts), true);
                            _pokerCards.Clear();
                            _pokerHandBonus = 0;

                            _pokerSkullCharge++;
                            if (_pokerSkullCharge >= 3 && _pokerSkulls > 0)
                            {
                                _pokerSkulls--;
                                _pokerSkullCharge = 0;
                                _sound?.PlaySound(AudioMap.SkullBuster);
                                _talkBack?.Speak(Localization.Get("PokerSkullEliminated", _pokerSkulls), true);
                            }
                        }
                    }
                }

                if (isIceStormActive)
                {
                    foreach (int col in res.MatchedColumns)
                    {
                        if (col < 0 || col >= 8 || _iceColumns[col] <= 0) continue;
                        bool shattered = res.VerticalMatchedColumns.Contains(col) || res.HypercubeTriggered || res.HypercubeCreated > 0;
                        if (shattered)
                        {
                            _iceColumns[col] = 0;
                            _iceSkullTicks[col] = 0;
                            _sound?.PlaySound(AudioMap.IceColumnBreak);
                        }
                        else
                        {
                            _iceColumns[col] = Math.Max(0, _iceColumns[col] - 2);
                            if (_iceColumns[col] < 8) _iceSkullTicks[col] = 0;
                            _sound?.PlaySound(AudioMap.IceStormColumnCombo);
                        }
                    }
                }

                if (isButterfliesMode)
                {
                    if (res.ButterfliesFreed > 0)
                    {
                        _sound?.PlaySoundSpatial(AudioMap.Butterflyescape, toX, toY);
                        _talkBack?.Speak(Localization.Get("ButterflyFreed", res.ButterfliesFreed), true);
                    }

                    _board.MoveButterfliesUp();
                    while (_board.GetButterflyCount() < 6)
                    {
                        _board.SpawnButterflyAtBottom();
                    }

                    if (_board.IsButterflyAtTop())
                    {
                        _currentScreen = AndroidGameScreen.GameOver;
                        _gameOverIdx = 0;
                        _sound?.PlaySound(AudioMap.ButterflyDeath1);
                        _sound?.PlaySound(AudioMap.VoiceGameover);
                        _talkBack?.Speak(Localization.Get("ButterflyCaught") + " " + Localization.Get("GameOver", _score), true);
                        Invalidate();
                        return;
                    }
                    else if (_board.IsButterflyInDanger())
                    {
                        _sound?.PlaySound(AudioMap.ButterflyAppear);
                    }
                }

                if (isDiamondMineActive)
                {
                    if (res.NuggetsMined > 0)
                    {
                        _sound?.PlaySound(AudioMap.DiamondMineTreasurefind);
                        _talkBack?.Speak(Localization.Get("NuggetFound"), true);
                    }
                    else if (res.RockCleared > 0)
                    {
                        _sound?.PlaySound(AudioMap.DiamondMineStoneCracked);
                    }
                    else if (res.DirtCleared > 0)
                    {
                        _sound?.PlaySound(AudioMap.DiamondMineDirtCracked);
                    }

                    if (!_board.HasDirtRemaining())
                    {
                        _sound?.PlaySound(AudioMap.DiamondMineTreasurefind);
                        _sound?.PlaySound(AudioMap.DiamondMineTreasurefindDiamonds);
                        _sound?.PlaySound(AudioMap.DiamondMineArtifactShowcase);
                        _talkBack?.Speak("¡Tesoro desenterrado! Avanzando en la mina.", true);
                        _board.ShiftDiamondMineDown();
                    }
                }

                // Cálculo y acumulación de puntuación
                int matchScore = res.TotalGemsDestroyed * 50 * Math.Max(1, res.CascadeDepth);
                _score += matchScore;
                Progress.TotalGemsCleared += res.TotalGemsDestroyed;
                _profileMgr.Save();

                // Locuciones auténticas de PopCap
                if (res.TotalGemsDestroyed >= 8) _sound?.PlaySound(AudioMap.VoiceUnbelievable);
                else if (res.TotalGemsDestroyed >= 6) _sound?.PlaySound(AudioMap.VoiceExtraordinary);
                else if (res.TotalGemsDestroyed >= 5) _sound?.PlaySound(AudioMap.VoiceAwesome);
                else if (res.TotalGemsDestroyed >= 4) _sound?.PlaySound(AudioMap.VoiceExcellent);
                else if (res.CascadeDepth >= 3) _sound?.PlaySound(AudioMap.VoiceSpectacular);

                string matchDesc = res.CascadeDepth > 1
                    ? Localization.Get("CascadeAnnounce", res.CascadeDepth, res.TotalGemsDestroyed, _score)
                    : Localization.Get("MatchAnnounce", res.TotalGemsDestroyed, _score);
                _talkBack?.Speak(matchDesc, true);

                // Verificación de movimientos restantes
                MoveHint? validHint = HintFinder.FindValidMove(_board);
                if (!validHint.HasValue)
                {
                    _sound?.PlaySound(AudioMap.VoiceNomoremoves);
                    if (_currentModeKey == "ModeClassic")
                    {
                        if (_shufflesRemaining > 0)
                        {
                            _shufflesRemaining--;
                            _sound?.PlaySound(AudioMap.Scramble);
                            _talkBack?.Speak(Localization.Get("ShuffleAnnounce", _shufflesRemaining), true);
                            _board.InitializeBoard();
                        }
                        else
                        {
                            _currentScreen = AndroidGameScreen.GameOver;
                            _gameOverIdx = 0;
                            _sound?.PlaySound(AudioMap.VoiceGameover);
                            _talkBack?.Speak(Localization.Get("NoShufflesLeft") + " " + Localization.Get("GameOver", _score), true);
                            Invalidate();
                            _talkBack?.NotifyStructureChanged();
                            return;
                        }
                    }
                    else
                    {
                        _sound?.PlaySound(AudioMap.Scramble);
                        _talkBack?.Speak(Localization.Get("NoMoreMovesScramble"), true);
                        _board.InitializeBoard();
                    }
                }
            }
            else
            {
                // Movimiento inválido: deshacer swap
                _board.SwapGems(toX, toY, fromX, fromY);
                _sound?.PlaySound(AudioMap.Badmove);
                _talkBack?.Speak(Localization.Get("InvalidMove"), true);
            }
            _selectedX = -1;
            _selectedY = -1;
            _cursorX = toX;
            _cursorY = toY;
            AnnounceCell(_cursorX, _cursorY);
            Invalidate();
            _talkBack?.NotifyStructureChanged();
        }

        public void ExecuteMenuItemFocus(int idx)
        {
            SetActiveIndex(idx);
            _sound?.PlaySound(AudioMap.ButtonMouseover);
        }

        public void AnnounceCurrentMenu()
        {
            string[] items = GetCurrentItems(out int activeIdx);
            string title = GetScreenTitle();
            if (items.Length > 0 && activeIdx < items.Length)
            {
                _talkBack?.Speak(title + ". Opción: " + items[activeIdx] + ". Desliza arriba o abajo para navegar, toca para confirmar.", true);
            }
            _talkBack?.NotifyStructureChanged();
            if (items.Length > 0 && activeIdx < items.Length)
            {
                _talkBack?.NotifyVirtualViewFocused(activeIdx);
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

        // Acciones de intercambio direccional para TalkBack (Solucion 1): como
        // TalkBack consume los deslizamientos de 1 dedo, el usuario mueve la gema
        // con el menu de acciones nativo en vez de arrastrar el dedo.
        public const int ActionSwapUp = 0x10001;
        public const int ActionSwapDown = 0x10002;
        public const int ActionSwapLeft = 0x10003;
        public const int ActionSwapRight = 0x10004;

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
                root.Focusable = false;
                root.Clickable = false;

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
            node.SetSource(_view, virtualViewId);
            node.VisibleToUser = true;
            node.Enabled = true;
            node.Focusable = true;
            node.Clickable = true;
            node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClick);
            node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionAccessibilityFocus);
            node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionClearAccessibilityFocus);

            if (_view.CurrentScreen == AndroidGameScreen.Playing)
            {
                float density = _view.Resources?.DisplayMetrics?.Density ?? 1.0f;
                if (density < 1.0f) density = 1.0f;

                int marginY = (int)(15f * density);
                int boardHeight = _view.Height - (marginY * 2);
                int tileSize = Math.Max(1, boardHeight / Board.Rows);
                int offsetX = (int)(20f * density);
                int offsetY = marginY;

                int panelLeft = offsetX + (Board.Cols * tileSize) + (int)(25f * density);
                int panelWidth = _view.Width - panelLeft - (int)(20f * density);
                int btnHeight = (int)(55f * density);
                int hintTop = offsetY + (int)(20f * density);
                int pauseTop = hintTop + btnHeight + (int)(20f * density);

                if (virtualViewId == VIRTUAL_ID_HINT)
                {
                    Rect rect = new Rect(panelLeft, hintTop, panelLeft + panelWidth, hintTop + btnHeight);
                    node.SetBoundsInParent(rect);
                    int[] loc = new int[2];
                    _view.GetLocationOnScreen(loc);
                    Rect screenRect = new Rect(rect.Left + loc[0], rect.Top + loc[1], rect.Right + loc[0], rect.Bottom + loc[1]);
                    node.SetBoundsInScreen(screenRect);
                    node.Text = "💡 " + Localization.Get("HintTitle");
                    node.ContentDescription = "Botón de Pista. Toca dos veces para encontrar un movimiento sugerido.";
                    return node;
                }

                if (virtualViewId == VIRTUAL_ID_PAUSE)
                {
                    Rect rect = new Rect(panelLeft, pauseTop, panelLeft + panelWidth, pauseTop + btnHeight);
                    node.SetBoundsInParent(rect);
                    int[] loc = new int[2];
                    _view.GetLocationOnScreen(loc);
                    Rect screenRect = new Rect(rect.Left + loc[0], rect.Top + loc[1], rect.Right + loc[0], rect.Bottom + loc[1]);
                    node.SetBoundsInScreen(screenRect);
                    node.Text = "⏸️ " + Localization.Get("PauseTitle");
                    node.ContentDescription = "Botón de Pausa. Toca dos veces para pausar la partida o volver al menú.";
                    return node;
                }

                if (virtualViewId >= VIRTUAL_BOARD_BASE && virtualViewId < VIRTUAL_BOARD_BASE + 64)
                {
                    int idx = virtualViewId - VIRTUAL_BOARD_BASE;
                    int x = idx % Board.Cols;
                    int y = idx / Board.Cols;

                    int left = offsetX + (x * tileSize) + (int)(2f * density);
                    int top = offsetY + (y * tileSize) + (int)(2f * density);
                    Rect rect = new Rect(left, top, left + tileSize - (int)(4f * density), top + tileSize - (int)(4f * density));
                    node.SetBoundsInParent(rect);
                    int[] loc = new int[2];
                    _view.GetLocationOnScreen(loc);
                    Rect screenRect = new Rect(rect.Left + loc[0], rect.Top + loc[1], rect.Right + loc[0], rect.Bottom + loc[1]);
                    node.SetBoundsInScreen(screenRect);

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

                    // Etiqueta hablada concisa (A1, Roja) para que TalkBack lea una
                    // sola celda de forma limpia al explorar; el ContentDescription
                    // conserva solo el detalle de movimientos validos (sin repetir
                    // el nombre, para no leer la gema dos veces).
                    node.Text = string.Format("{0}{1}, {2}", colLetter, rowNum, gemName);
                    node.ContentDescription = movesDesc;

                    // Acciones de intercambio direccional para TalkBack (Solucion 1).
                    if (y > 0) node.AddAction(new AccessibilityNodeInfo.AccessibilityAction(ActionSwapUp, Localization.Get("SwapUp")));
                    if (y < Board.Rows - 1) node.AddAction(new AccessibilityNodeInfo.AccessibilityAction(ActionSwapDown, Localization.Get("SwapDown")));
                    if (x > 0) node.AddAction(new AccessibilityNodeInfo.AccessibilityAction(ActionSwapLeft, Localization.Get("SwapLeft")));
                    if (x < Board.Cols - 1) node.AddAction(new AccessibilityNodeInfo.AccessibilityAction(ActionSwapRight, Localization.Get("SwapRight")));

                    return node;
                }
            }
            else
            {
                string[] items = _view.GetCurrentItems(out int activeIdx);
                if (virtualViewId >= 0 && virtualViewId < items.Length)
                {
                    float density = _view.Resources?.DisplayMetrics?.Density ?? 1.0f;
                    if (density < 1.0f) density = 1.0f;

                    int startY = (int)(65f * density);
                    int availableHeight = _view.Height - startY - (int)(20f * density);
                    int baseItemHeight = (int)(55f * density);
                    int itemHeight = items.Length > 0 ? Math.Min(baseItemHeight, Math.Max((int)(40f * density), availableHeight / items.Length)) : baseItemHeight;
                    int top = startY + (virtualViewId * itemHeight);
                    Rect rect = new Rect((int)(16f * density), top, _view.Width - (int)(16f * density), top + itemHeight - (int)(6f * density));
                    node.SetBoundsInParent(rect);
                    int[] loc = new int[2];
                    _view.GetLocationOnScreen(loc);
                    Rect screenRect = new Rect(rect.Left + loc[0], rect.Top + loc[1], rect.Right + loc[0], rect.Bottom + loc[1]);
                    node.SetBoundsInScreen(screenRect);
                    node.Text = items[virtualViewId];

                    if (_view.CurrentScreen == AndroidGameScreen.OptionsScreen && (virtualViewId == 0 || virtualViewId == 1 || virtualViewId == 2))
                    {
                        node.ClassName = "android.widget.SeekBar";
                        node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollForward);
                        node.AddAction(AccessibilityNodeInfo.AccessibilityAction.ActionScrollBackward);
                        node.ContentDescription = string.Format("{0}. Deslizador. Toca dos veces para cambiar el valor o desliza arriba y abajo.", items[virtualViewId]);
                    }
                    else
                    {
                        node.ContentDescription = string.Format("Opción {0} de {1}: {2}", virtualViewId + 1, items.Length, items[virtualViewId]);
                    }
                    return node;
                }
            }

            return node;
        }

        private int _focusedVirtualViewId = View.NoId;

        public override AccessibilityNodeInfo FindFocus(NodeFocus focus)
        {
            if (focus == NodeFocus.Accessibility)
            {
                if (_focusedVirtualViewId != View.NoId)
                {
                    return CreateAccessibilityNodeInfo(_focusedVirtualViewId);
                }
            }
            return null;
        }

        public override bool PerformAction(int virtualViewId, Android.Views.Accessibility.Action action, Android.OS.Bundle arguments)
        {
            if (action == Android.Views.Accessibility.Action.AccessibilityFocus)
            {
                if (_focusedVirtualViewId != virtualViewId)
                {
                    _focusedVirtualViewId = virtualViewId;
                    if (_view.CurrentScreen != AndroidGameScreen.Playing)
                    {
                        _view.ExecuteMenuItemFocus(virtualViewId);
                    }
                    _view.Invalidate();
                    return true;
                }
                return false;
            }
            if (action == Android.Views.Accessibility.Action.ClearAccessibilityFocus)
            {
                if (_focusedVirtualViewId == virtualViewId)
                {
                    _focusedVirtualViewId = View.NoId;
                    _view.Invalidate();
                    return true;
                }
                return false;
            }
            int actId = (int)action;
            if (actId == ActionSwapUp || actId == ActionSwapDown || actId == ActionSwapLeft || actId == ActionSwapRight)
            {
                if (_view.CurrentScreen == AndroidGameScreen.Playing
                    && virtualViewId >= VIRTUAL_BOARD_BASE && virtualViewId < VIRTUAL_BOARD_BASE + 64)
                {
                    int idx = virtualViewId - VIRTUAL_BOARD_BASE;
                    int x = idx % Board.Cols;
                    int y = idx / Board.Cols;
                    if (actId == ActionSwapUp) _view.ExecuteSwap(x, y, x, y - 1);
                    else if (actId == ActionSwapDown) _view.ExecuteSwap(x, y, x, y + 1);
                    else if (actId == ActionSwapLeft) _view.ExecuteSwap(x, y, x - 1, y);
                    else if (actId == ActionSwapRight) _view.ExecuteSwap(x, y, x + 1, y);
                    return true;
                }
                return false;
            }

            if (action == Android.Views.Accessibility.Action.Click)
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
            if (action == Android.Views.Accessibility.Action.ScrollForward)
            {
                if (_view.CurrentScreen == AndroidGameScreen.OptionsScreen && (virtualViewId == 0 || virtualViewId == 1 || virtualViewId == 2))
                {
                    _view.AdjustOptionSlider(virtualViewId, 10);
                    return true;
                }
            }
            if (action == Android.Views.Accessibility.Action.ScrollBackward)
            {
                if (_view.CurrentScreen == AndroidGameScreen.OptionsScreen && (virtualViewId == 0 || virtualViewId == 1 || virtualViewId == 2))
                {
                    _view.AdjustOptionSlider(virtualViewId, -10);
                    return true;
                }
            }
            return false;
        }
    }
}
