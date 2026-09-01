using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bejeweled3Accessible.Accessibility;
using Bejeweled3Accessible.Audio;
using Bejeweled3Accessible.Engine;
using Updater = Bejeweled3Accessible.Update.AutoUpdater;

namespace Bejeweled3Accessible.UI
{
    public enum GameScreen { Loading, ProfileInput, ProfileSelectScreen, MainMenu, GameSelect, Options, BadgesScreen, RecordsScreen, TutorialScreen, ZenOptionsScreen, QuestRelicScreen, QuestChallengeScreen, AudioSchool, Playing, PauseMenu, GameOver }

    public class MainWindow : Form
    {
        private Board _board;
        private readonly NvdaSpeech _speech;
        private readonly SoundEngine _sound;
        private readonly Timer _renderTimer;
        private readonly Timer _lightningTimer;
        private readonly Timer _loadingTimer;
        private readonly ProfileManager _profileMgr;
        private readonly GameOptions _options;
        private BadgeManager _badgeMgr;
        private ZenManager _zenMgr;

        private GameScreen _screen = GameScreen.Loading;
        private GameScreen _optionsOriginScreen = GameScreen.MainMenu;
        private int _loadingProgress = 0;
        private bool _updateChecked = false;
        private string _latestTag = null;
        private string _latestNotesRaw = null;
        private bool _updatePromptActive = false;
        private readonly object _dlLock = new object();
        private long _dlTotal = 0;
        private long _dlReceived = 0;
        private double _dlSpeed = 0;
        private long _dlLastBytes = 0;
        private DateTime _dlLastTime = DateTime.MinValue;
        private int _dlNextAnnounce = 10;
        private bool _loadingComplete = false;

        private int _menuIdx = 0;
        private int _gameModeIdx = 0;
        private int _optionsIdx = 0;
        private int _pauseIdx = 0;
        private int _badgeIdx = 0;
        private int _recordsIdx = 0;
        private int _tutorialIdx = 0;
        private int _relicIdx = 0;
        private int _audioSchoolIdx = 0;
        private int _questChallengeIdx = 0;
        private int _profileSelectIdx = 0;
        private int _zenOptionsIdx = 0;
        private int _gameOverIdx = 0;
        private int _cursorX = 3, _cursorY = 3;
        private int _score = 0, _level = 1;
        private int _levelProgressPoints = 0; // Points accumulated in current level bar
        private int _cascadeChain = 0;
        private bool _isSwapping = false;
        private string _currentModeKey = "ModeClassic";

        private int GetLevelTargetPoints(int level)
        {
            // Authentic Bejeweled 3: Level 1 target ~2500 base, scaling with Level
            // e.g. L1: 2500, L2: 5000, L5: 12500, L12: 30000 pts per level
            return 2500 * Math.Max(1, level);
        }

        private string _profileInputBuffer = "";

        private int _lightningTimeLeft = 60;
        private int _lightningMultiplier = 1;

        // Duracion real (ms) de cada sonido de combo, medida de los ogg originales.
        // Sirve para tocar los combos en sucesion (que termine uno antes del otro),
        // no solapados como un barrido.
        private static readonly System.Collections.Generic.Dictionary<string, int> _comboDurations =
            new System.Collections.Generic.Dictionary<string, int>
        {
            { AudioMap.Combo1, 839 },
            { AudioMap.Combo2, 839 },
            { AudioMap.Combo3, 1678 },
            { AudioMap.Combo4, 1630 },
            { AudioMap.Combo5, 1583 },
            { AudioMap.Combo6, 1538 },
            { AudioMap.Combo7, 1494 },
            { AudioMap.ZenCombo2, 863 },
        };

        private static int ComboDurationMs(string name)
        {
            int ms;
            if (_comboDurations.TryGetValue(name, out ms)) return ms;
            return 1500;
        }

        // Impacto de ruptura (explosion) de un nivel de cascada, paneado a la
        // posicion del movimiento. Suena JUNTO al combo de ese nivel (mismo
        // evento: las gemas se rompen), asi la cadena de combos no se solapa
        // con una rafaga de explosiones al final.
        private void PlaySwapImpact(int totalGems, int col, int row)
        {
            if (totalGems >= 10)
            {
                _sound.PlaySoundSpatial(AudioMap.ElectroPath, col, row);
                _sound.PlaySoundSpatial(AudioMap.ElectroPath2, col, row);
                _sound.PlaySoundSpatial(AudioMap.CoinCreated, col, row);
                _sound.PlaySoundSpatial(AudioMap.Coinappear, col, row);
            }
            else if (totalGems >= 4)
            {
                _sound.PlaySoundSpatial(AudioMap.SmallExplode, col, row);
                _sound.PlaySoundSpatial(AudioMap.GemShatters, col, row);
            }
            else
            {
                _sound.PlaySoundSpatial(AudioMap.GemHit, col, row);
            }
        }

        // Creacion de gemas especiales producto de la jugada (supernova,
        // hipercubo, estrella, flama). Suena en el ultimo nivel de la cadena,
        // junto a la explosion de impacto (es el mismo evento climax).
        private void PlaySwapSpecialCreation(CascadeResult res, int col, int row)
        {
            if (res.SupernovaCreated > 0)
            {
                _sound.PlaySound(AudioMap.FireworkLaunch);
                _sound.PlaySound(AudioMap.FireworkThump);
                _sound.PlaySound(AudioMap.FireworkCrackle);
                _sound.PlaySound(AudioMap.LasergemCreated);
                _sound.PlaySound(AudioMap.ElectroExplode);
                _sound.DuckMusicVolume(0.3f, 500);
                _sound.RestoreMusicVolume(500);
                AwardBadge("BadgeSuperstar", BadgeTier.Platinum);
            }
            else if (res.HypercubeCreated > 0)
            {
                _sound.PlayHypercubeSweep(AudioMap.HypercubeCreate, col, row);
                _sound.PlaySound(AudioMap.Hyperspace);
            }
            else if (res.StarCreated > 0)
            {
                _sound.PlayStarGemLaser(AudioMap.LasergemCreated, col, row);
                _sound.PlaySound(AudioMap.ElectroExplode);
            }
            else if (res.FlameCreated > 0)
            {
                _sound.PlaySound(AudioMap.PowergemCreated);
                _sound.PlaySound(AudioMap.Flamebonus);
                _sound.PlaySound(AudioMap.Flamespeed1);
            }
        }
        private int _lightningTankSeconds = 0;
        private bool _lastHurrahActive = false;
        private int _lastHurrahScore = 0;

        private int[] _iceColumns = new int[8]; // Column heights from 0 to 8 (uniform front)
        private int _iceRiseCounter = 0;
        private int _iceRiseInterval = 4;
        // Once a column's ice crests the board (height 8) a skull appears and an
        // internal column rises; if it isn't melted within ICE_SKULL_GRACE_TICKS
        // seconds the whole board freezes (authentic Ice Storm loss).
        private const int ICE_SKULL_GRACE_TICKS = 6;
        private int[] _iceSkullTicks = new int[8];
        private int _diamondDepthMeters = 0;

        private string _activeQuestName = "";
        private Engine.QuestMission _activeQuest = null;
        private int _activeQuestIndex = -1;
        private int _questGemsCleared = 0;
        private int _questButterfliesFreed = 0;
        private int _questNuggets = 0;
        private int _questGoldConverted = 0;
        private int _questBombsDestroyed = 0;
        private int _questMaxCascade = 0;
        private int _questHandsScored = 0;
        private int _questIceColumnsBroken = 0;
        private int _pokerSkulls = 0;
        private int _pokerHandBonus = 0;
        private int _pokerSkullCharge = 0;
        private int _shufflesRemaining = 3;
        private List<GemColor> _pokerCards = new List<GemColor>();

        private readonly Dictionary<GemColor, Color> _gemColors = new Dictionary<GemColor, Color>
        {
            { GemColor.Red, Color.Red },
            { GemColor.Yellow, Color.Gold },
            { GemColor.Green, Color.LimeGreen },
            { GemColor.Blue, Color.DeepSkyBlue },
            { GemColor.Purple, Color.Purple },
            { GemColor.White, Color.Snow },
            { GemColor.Orange, Color.Orange }
        };

        private Bitmap[][] _gemFrames;
        private Bitmap[] _gemShadows;
        private Bitmap _heatwaveLogo;
        private int _gemAnimTick = 0;

        // Mouse interaction support: click-to-select, click-adjacent-to-swap, and drag-and-drop
        private bool _mouseEnabled = true;
        private int _selectedGemX = -1, _selectedGemY = -1;
        private int _dragStartX = -1, _dragStartY = -1;
        private Point _dragStartPixel = Point.Empty;
        private bool _isDragging = false;

        private GameProgress _progress
        {
            get { return _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.Progress : new GameProgress(); }
        }

        private string[] GetMainMenuItems()
        {
            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "";
            List<string> items = new List<string>
            {
                Localization.Get("MenuPlay"),
                Localization.Get("MenuBadges"),
                Localization.Get("MenuRecords"),
                Localization.Get("MenuTutorial"),
                Localization.Get("MenuChangeUser", profName),
                Localization.Get("MenuLanguage"),
                Localization.Get("MenuOptions"),
                Localization.Get("MenuAudioSchool")
            };
#if !DEBUG
            items.Add(Localization.Get("MenuUpdateCheck"));
#endif
            items.Add(Localization.Get("MenuExit"));
            return items.ToArray();
        }



        private string[] GetGameModeKeys()
        {
            List<string> keys = new List<string>
            {
                "ModeClassic",
                "ModeLightning",
                "ModeZen",
                "ModeQuest",
                _progress.IsPokerUnlocked ? "ModePoker" : "ModePokerLocked",
                _progress.IsButterfliesUnlocked ? "ModeButterflies" : "ModeButterfliesLocked",
                _progress.IsIceStormUnlocked ? "ModeIceStorm" : "ModeIceStormLocked",
                _progress.IsDiamondMineUnlocked ? "ModeDiamondMine" : "ModeDiamondMineLocked",
                "BackToMain"
            };
            return keys.ToArray();
        }

        public MainWindow()
        {
            string baseDir = Path.GetDirectoryName(Application.ExecutablePath);
            _speech = new NvdaSpeech();
            _sound = new SoundEngine(baseDir);
            // Warm the voice-duration cache off the UI thread so the first voice
            // used does not decode its OGG synchronously on the form thread.
            try { System.Threading.ThreadPool.QueueUserWorkItem(delegate { _sound.PreloadVoiceDurations(); }); }
            catch { }

            _options = GameOptions.Load();
            _profileMgr = ProfileManager.Load();
            string profileName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "Jugador 1";
            _badgeMgr = BadgeManager.Load(profileName);
            _zenMgr = new ZenManager(baseDir, _speech, _sound);
            _zenMgr.SelectedAmbient = (Engine.AmbientType)_options.ZenAmbient;
            _zenMgr.AmbientEnabled = _zenMgr.SelectedAmbient != Engine.AmbientType.None;
            _zenMgr.MantrasEnabled = _options.ZenMantras;
            _zenMgr.BreathModulationEnabled = _options.ZenBreath;

            _sound.MusicVol = _options.MusicVolume;
            _sound.SfxVol = _options.SoundVolume;
            _sound.VoiceVol = _options.VoiceVolume;
            _sound.BinauralEnabled = _options.BinauralEnabled;
            _mouseEnabled = _options.MouseEnabled;
            Localization.CurrentLanguage = _options.SelectedLanguage;

            Text = Localization.Get("AppTitle");
            Size = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            _board = new Board(new Random().Next());
            LoadVisualAssets(baseDir);

            _renderTimer = new Timer { Interval = 30 };
            _renderTimer.Tick += (s, e) => Invalidate();
            _renderTimer.Start();

            _lightningTimer = new Timer { Interval = 1000 };
            _lightningTimer.Tick += LightningTimer_Tick;

            _loadingTimer = new Timer { Interval = 50 };
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();

#if !DEBUG
            // Background update check: announces once if a newer release exists,
            // without blocking the startup or opening any browser (only in Release).
            try { System.Threading.ThreadPool.QueueUserWorkItem(delegate { CheckForUpdatesAsync(); }); }
            catch { }
#endif

            KeyDown += MainWindow_KeyDown;
            KeyPress += MainWindow_KeyPress;
            MouseDown += MainWindow_MouseDown;
            MouseMove += MainWindow_MouseMove;
            MouseUp += MainWindow_MouseUp;

            // When the intro track has played through, the loading screen
            // advances to the menu on its own (like the original game).
            _sound.MusicRechained += Sound_MusicRechained;

            _sound.PlayMusic(MusicMap.FileName(MusicMap.Intro));
            _speech.Speak(Localization.Get("LoadingTitle"), true);
        }

        // Runs on the music monitor's worker thread; hop to the UI thread.
        private void Sound_MusicRechained(object sender, EventArgs e)
        {
            if (_screen != GameScreen.Loading) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (_screen == GameScreen.Loading)
                    {
                        TransitionToMainMenu(true);
                    }
                });
            }
            catch { }
        }

        private void SaveOptionsState()
        {
            _options.MusicVolume = _sound.MusicVol;
            _options.SoundVolume = _sound.SfxVol;
            _options.VoiceVolume = _sound.VoiceVol;
            _options.BinauralEnabled = _sound.BinauralEnabled;
            _options.MouseEnabled = _mouseEnabled;
            _options.SelectedLanguage = Localization.CurrentLanguage;
            _options.ZenAmbient = (int)_zenMgr.SelectedAmbient;
            _options.ZenMantras = _zenMgr.MantrasEnabled;
            _options.ZenBreath = _zenMgr.BreathModulationEnabled;
            _options.Save();
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            if (_screen != GameScreen.Loading)
            {
                _loadingTimer.Stop();
                return;
            }
            _loadingProgress += 2;
            if (_loadingProgress >= 100)
            {
                _loadingProgress = 100;
                _loadingTimer.Stop();
                _loadingComplete = true;
                _speech.Speak(Localization.Get("LoadingPrompt"), false);
            }
        }

        private void TransitionToMainMenu(bool speakWelcomeBack = false)
        {
            if (_zenMgr != null) _zenMgr.StopZenSession();

            // Persist once when leaving gameplay, not on every turn.
            _profileMgr.Save();

            _sound.PlaySound(AudioMap.Backtomain);
            _sound.PlaySound(AudioMap.Menuspin);

            if (_profileMgr.Profiles.Count == 0)
            {
                _screen = GameScreen.ProfileInput;
                _profileInputBuffer = "";
                // First launch: the menu theme starts right when the
                // "Welcome to Bejeweled 3" locution plays, like the original.
                _sound.PlayMusic(MusicMap.FileName(MusicMap.MainTheme));
                _sound.PlaySound(AudioMap.VoiceWelcometobejeweled);
                _speech.Speak(Localization.Get("CreateProfileTitle") + ". " + Localization.Get("EnterNamePrompt"), true);
            }
            else
            {
                _screen = GameScreen.MainMenu;
                // The menu theme is already chaining; just announce the return.
                if (_sound.MusicNowPlaying != MusicMap.FileName(MusicMap.MainTheme))
                    _sound.PlayMusic(MusicMap.FileName(MusicMap.MainTheme));
                if (speakWelcomeBack)
                {
                    _sound.PlaySound(AudioMap.VoiceWelcomeback);
                    _speech.Speak(Localization.Get("Welcome"), true);
                }
                else
                {
                    _speech.Speak(Localization.Get("Welcome"), true);
                }
            }
        }

        // Which modes run on the periodic lightning/Ice clock:
        // Lightning (countdown), Diamond Mine (screen timer), Ice Storm and
        // Time Bomb (their threats tick every second).
        private bool UsesClockTimer()
        {
            switch (_currentModeKey)
            {
                case "ModeLightning":
                case "ModeIceStorm":
                case "ModeDiamondMine":
                    return true;
                case "ModeQuest":
                    return _activeQuest != null &&
                           (_activeQuest.Type == Engine.QuestType.IceStorm ||
                            _activeQuest.Type == Engine.QuestType.TimeBomb);
                default:
                    return false;
            }
        }

        private void LightningTimer_Tick(object sender, EventArgs e)
        {
            if (_screen != GameScreen.Playing) return;

            if (_currentModeKey == "ModeButterflies" || (_currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.Butterflies))
            {
                // In authentic Butterflies mode, butterflies move up 1 row AFTER EACH TURN, not via periodic timer
            }
            else if (_currentModeKey == "ModeIceStorm" || (_currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.IceStorm))
            {
                // Authentic Ice Storm: the cold front rises in ALL columns at once,
                // faster as the level increases. Matches melt only their own column.
                // When a column crests the board it does NOT lose the game instantly:
                // a skull appears and an internal column rises; the player must melt
                // it before the internal column reaches the top or the board freezes.
                _iceRiseCounter++;
                if (_iceRiseCounter >= _iceRiseInterval)
                {
                    _iceRiseCounter = 0;
                    List<int> dangerCols = new List<int>();
                    List<int> newSkullCols = new List<int>();
                    for (int col = 0; col < 8; col++)
                    {
                        _iceColumns[col] = Math.Min(8, _iceColumns[col] + 1);
                        if (_iceColumns[col] == 7) dangerCols.Add(col);
                        if (_iceColumns[col] >= 8 && _iceSkullTicks[col] == 0)
                        {
                            // A fresh crest: arm the internal ice column with the
                            // grace period instead of ending the game immediately.
                            _iceSkullTicks[col] = ICE_SKULL_GRACE_TICKS;
                            newSkullCols.Add(col);
                        }
                    }

                    if (newSkullCols.Count > 0)
                    {
                        _sound.PlaySound(AudioMap.TowerHitsTop1);
                        _sound.PlaySound(AudioMap.IceWarning);
                        _sound.PlaySound(AudioMap.IceStormSteamBuildUp);
                        _speech.Speak(Localization.Get("IceSkullColumns", FormatColumns(newSkullCols)), false);
                    }

                    if (dangerCols.Count > 0)
                    {
                        _sound.PlaySound(AudioMap.IceWarning);
                        _sound.PlaySound(AudioMap.IceStormSteamBuildUp);
                        _speech.Speak(Localization.Get("IceDangerColumns", FormatColumns(dangerCols)), false);
                    }
                }

                // The internal column claws its way up every second; if any column's
                // grace expires while still iced to the top, the board freezes over.
                bool frozen = false;
                for (int col = 0; col < 8; col++)
                {
                    if (_iceColumns[col] >= 8 && _iceSkullTicks[col] > 0)
                    {
                        _iceSkullTicks[col]--;
                        if (_iceSkullTicks[col] <= 0) frozen = true;
                    }
                }

                if (frozen)
                {
                    _lightningTimer.Stop();
                    _screen = GameScreen.GameOver;
                    _sound.PlaySound(AudioMap.IceStormFinalThud);
                    _sound.PlaySound(AudioMap.IceStormGameOver);
                    if (_currentModeKey == "ModeIceStorm" && _score > _progress.IceStormHighScore)
                    {
                        _progress.IceStormHighScore = _score;
                        _sound.PlaySound(AudioMap.Rankup);
                        _profileMgr.Save();
                    }
                    CheckSecretRecordsBadge();
                    _speech.Speak(Localization.Get("GameOver", _score), true);
                    return;
                }
            }
            else if (_currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.TimeBomb)
            {
                // Time Bombs: countdown ticks down every second
                int exploded = _board.TickBombs();
                if (exploded > 0)
                {
                    _sound.PlaySound(AudioMap.SkullBusted);
                    _sound.PlaySound(AudioMap.GemCountdownDestroyed);
                    _speech.Speak(Localization.Get("BombExploded"), true);
                }
            }
            else if (_currentModeKey == "ModeLightning")
            {
                // Reloj del multiplicador de tiempo: ya NO tickea segundo a segundo
                // (el tick leve es solo al mover/emparejar gemas de tiempo).
                _lightningTimeLeft--;
                if (_lightningTimeLeft == 30)
                {
                    _sound.PlaySound(AudioMap.VoiceThirtyseconds);
                }
                else if (_lightningTimeLeft <= 10 && _lightningTimeLeft > 0)
                {
                    _sound.PlaySound(AudioMap.CountdownWarning);
                    _speech.Speak(_lightningTimeLeft.ToString(), false);
                }

                if (_lightningTimeLeft <= 0)
                {
                    if (_lightningTankSeconds > 0)
                    {
                        _lightningTimeLeft = _lightningTankSeconds;
                        _lightningTankSeconds = 0;
                        _lastHurrahActive = true;
                        _lastHurrahScore = 0;
                        _lightningMultiplier++;
                        _sound.PlaySound(AudioMap.LightningTubeFill10);
                        _sound.PlaySound(AudioMap.MultiplierAppears);
                        _sound.PlaySound(AudioMap.MultiplierHurrahed);
                        _sound.PlaySound(AudioMap.IceStormMultiplerUp);
                        _speech.Speak(Localization.Get("TimeExtended", _lightningMultiplier), true);
                    }
                    else
                    {
                        _lightningTimer.Stop();
                        _screen = GameScreen.GameOver;
                        _sound.PlaySound(AudioMap.VoiceTimeup);
                        _sound.PlaySound(AudioMap.VoiceGameover);

                        if (_score > _progress.LightningHighScore)
                        {
                            _progress.LightningHighScore = _score;
                            _sound.PlaySound(AudioMap.Rankup);
                            _profileMgr.Save();
                        }

                        // Wait for "Time up!" and "Game over" voices before the TTS score announce
                        _speech.Speak(Localization.Get("GameOver", _score), true);
                    }
                }
            }
            else if (_currentModeKey == "ModeDiamondMine")
            {
                // Diamond Mine: 60 seconds per screen, extended by 30s each time the screen is cleared
                _lightningTimeLeft--;
                if (_lightningTimeLeft == 30)
                {
                    _sound.PlaySound(AudioMap.VoiceThirtyseconds);
                }
                else if (_lightningTimeLeft <= 10 && _lightningTimeLeft > 0)
                {
                    _sound.PlaySound(AudioMap.CountdownWarning);
                    _speech.Speak(_lightningTimeLeft.ToString(), false);
                }

                if (_lightningTimeLeft <= 0)
                {
                    _lightningTimer.Stop();
                    _screen = GameScreen.GameOver;
                    _sound.PlaySound(AudioMap.VoiceTimeup);
                    _sound.PlaySound(AudioMap.VoiceGameover);

                    if (_score > _progress.DiamondMineHighScore)
                    {
                        _progress.DiamondMineHighScore = _score;
                        _sound.PlaySound(AudioMap.Rankup);
                    }
                    _profileMgr.Save();
                    CheckSecretRecordsBadge();

                    _speech.Speak(Localization.Get("GameOver", _score), true);
                }
            }
        }

        private void MainWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_screen == GameScreen.ProfileInput)
            {
                if (e.KeyChar == '\r' || e.KeyChar == '\n')
                {
                    string name = _profileInputBuffer.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        _speech.Speak(Localization.Get("EnterNamePrompt"), true);
                        return;
                    }

                    PlayerProfile newProfile = new PlayerProfile(name);
                    _profileMgr.Profiles.Add(newProfile);
                    _profileMgr.CurrentProfileIndex = _profileMgr.Profiles.Count - 1;
                    _profileMgr.Save();

                    _badgeMgr = BadgeManager.Load(newProfile.ProfileName);
                    _profileInputBuffer = "";

                    _sound.PlaySound(AudioMap.ButtonPress);
                    TransitionToMainMenu(false);
                    return;
                }
                else if (e.KeyChar == '\b')
                {
                    if (_profileInputBuffer.Length > 0)
                    {
                        _profileInputBuffer = _profileInputBuffer.Substring(0, _profileInputBuffer.Length - 1);
                        _sound.PlaySound(AudioMap.Select);
                        _speech.Speak(_profileInputBuffer.Length > 0 ? _profileInputBuffer : Localization.Get("Empty"), true);
                    }
                }
                else if (!char.IsControl(e.KeyChar))
                {
                    _profileInputBuffer += e.KeyChar;
                    _sound.PlaySound(AudioMap.Select);
                    _speech.Speak(e.KeyChar.ToString(), true);
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (_updatePromptActive)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    _updatePromptActive = false;
                    PerformUpdate();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _updatePromptActive = false;
                    _sound.PlaySound(AudioMap.ButtonMouseover);
                    _speech.Speak(Localization.Get("UpdateCancelled"), true);
                }
                return;
            }

            if (_screen == GameScreen.ProfileInput)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (_profileMgr.Profiles.Count > 0)
                    {
                        _screen = GameScreen.ProfileSelectScreen;
                        _profileSelectIdx = 0;
                        _sound.PlaySound(AudioMap.ButtonPress);
                        _speech.Speak(Localization.Get("ProfileSelectTitle") + ". " + GetProfileSelectItems()[0], true);
                    }
                    else
                    {
                        _sound.PlaySound(AudioMap.ButtonMouseover);
                        _speech.Speak(Localization.Get("EnterNamePrompt"), true);
                    }
                }
                return;
            }
            else if (_screen == GameScreen.Loading)
            {
                // Any key skips the intro track and opens the menu right away.
                // If nothing is pressed, the intro plays through and the game
                // advances on its own (Sound_MusicRechained).
                TransitionToMainMenu(true);
            }
            else if (_screen == GameScreen.MainMenu) HandleMainMenuKeys(e);
            else if (_screen == GameScreen.AudioSchool) HandleAudioSchoolKeys(e);
            else if (_screen == GameScreen.GameSelect) HandleGameSelectKeys(e);
            else if (_screen == GameScreen.Options) HandleOptionsKeys(e);
            else if (_screen == GameScreen.BadgesScreen) HandleBadgesKeys(e);
            else if (_screen == GameScreen.RecordsScreen) HandleRecordsKeys(e);
            else if (_screen == GameScreen.TutorialScreen) HandleTutorialKeys(e);
            else if (_screen == GameScreen.QuestRelicScreen) HandleQuestRelicKeys(e);
            else if (_screen == GameScreen.QuestChallengeScreen) HandleQuestChallengeKeys(e);
            else if (_screen == GameScreen.ProfileSelectScreen) HandleProfileSelectKeys(e);
            else if (_screen == GameScreen.ZenOptionsScreen) HandleZenOptionsKeys(e);
            else if (_screen == GameScreen.Playing) HandlePlayingKeys(e);
            else if (_screen == GameScreen.PauseMenu) HandlePauseMenuKeys(e);
            else if (_screen == GameScreen.GameOver) HandleGameOverKeys(e);
        }

        // Runs on a pool thread: asks GitHub for the latest release and, when
        // it is newer than the running version, announces it once in the main
        // menu with the current version and the new release notes.
        private void CheckForUpdatesAsync()
        {
            // El canal dev (compilacion Debug) no comprueba actualizaciones: el
            // usuario ya tiene la ultima version reconstruyendo bin\Debug.
            if (Updater.IsDevBuild) return;

            Updater.ReleaseInfo release = null;
            try { release = Updater.GetLatestRelease(); } catch { }
            bool newer = release != null && release.IsValid && Updater.IsNewerThanCurrent(release.Tag);
            string tag = newer ? release.Tag : null;
            string notesRaw = newer ? release.Notes : null;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    _updateChecked = true;
                    if (newer)
                    {
                        _latestTag = tag;
                        _latestNotesRaw = notesRaw;
                        if (_screen == GameScreen.MainMenu)
                        {
                            _speech.Speak(BuildUpdateAnnouncement(false), false);
                        }
                    }
                });
            }
            catch { }
        }

        // "You are on version X. The new version Y is available. What's new:
        // <notes>." plus, when inMenu, the Enter/Escape instruction. Notes are
        // extracted at speak time so the announcement follows the active
        // language even if the user toggled it after the background check.
        private string BuildUpdateAnnouncement(bool inMenu)
        {
            string notes = Updater.ExtractNotes(_latestNotesRaw, Localization.CurrentLanguage == Language.Spanish);
            if (notes.Length > 0)
            {
                return Localization.Get(inMenu ? "UpdateFound" : "UpdateAvailable",
                    Updater.CurrentVersionString, Updater.DisplayVersion(_latestTag), notes);
            }
            return Localization.Get(inMenu ? "UpdateFoundNoNotes" : "UpdateAvailableNoNotes",
                Updater.CurrentVersionString, Updater.DisplayVersion(_latestTag));
        }

        // Download + install flow: prepares the update in %TEMP% and hands over
        // to a hidden script that swaps the game folder and reopens the game.
        private bool _updateBusy = false;
        private async void PerformUpdate()
        {
            if (_updateBusy) return; // doble pulsacion de Enter: solo una descarga
            _updateBusy = true;
            try
            {
                string tag = _latestTag;
                if (tag == null) return;
                lock (_dlLock)
                {
                    _dlTotal = 0; _dlReceived = 0; _dlSpeed = 0;
                    _dlLastBytes = 0; _dlLastTime = DateTime.UtcNow; _dlNextAnnounce = 10;
                }
                _sound.PlaySound(AudioMap.ButtonPress);
                _speech.Speak(Localization.Get("UpdateDownloading"), true);

                Updater.UpdateDownloadResult result = null;
                try
                {
                    string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                    result = await Task.Run(() => Updater.PrepareUpdate(tag, exeDir, e => OnDownloadProgress(e)));
                }
                catch (Exception ex)
                {
                    _speech.Speak(Localization.Get("UpdateError", ex.Message), true);
                    return;
                }

                if (result == null || result.Error != null)
                {
                    _speech.Speak(Localization.Get("UpdateError", result != null ? result.Error : "error"), true);
                    return;
                }

                _speech.Speak(Localization.Get("UpdateInstalling"), true);
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.ScriptPath)
                    {
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _speech.Speak(Localization.Get("UpdateError", ex.Message), true);
                    return;
                }

                // Close the game: the script waits for this process to end, replaces
                // the files and starts the new version by itself.
                Task.Delay(1500).ContinueWith(_2 =>
                {
                    try { Invoke(new Action(() => Close())); } catch { }
                });
            }
            finally { _updateBusy = false; }
        }

        // Download progress callback (worker thread): keeps the status that the
        // Space key reads and announces every 10% crossed (10, 20, ... 90).
        private void OnDownloadProgress(System.Net.DownloadProgressChangedEventArgs e)
        {
            long recv = e.BytesReceived;
            long total = e.TotalBytesToReceive;
            DateTime now = DateTime.UtcNow;
            lock (_dlLock)
            {
                if (_dlLastTime != DateTime.MinValue)
                {
                    double dt = (now - _dlLastTime).TotalSeconds;
                    if (dt >= 0.25)
                    {
                        _dlSpeed = (recv - _dlLastBytes) / dt;
                        _dlLastBytes = recv;
                        _dlLastTime = now;
                    }
                }
                else
                {
                    _dlLastBytes = recv;
                    _dlLastTime = now;
                }
                _dlReceived = recv;
                if (total > 0) _dlTotal = total;
            }
            int pct = total > 0 ? (int)(recv * 100.0 / total) : 0;
            if (pct < 100 && pct >= _dlNextAnnounce)
            {
                int announced = (pct / 10) * 10;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _speech.Speak(Localization.Get("UpdateProgress", announced), false);
                    });
                }
                catch { }
                lock (_dlLock) { _dlNextAnnounce = announced + 10; }
            }
        }

        // Announces a download detail queried with the number keys.
        // Mode 1: total file size. Mode 2: downloaded so far out of the total.
        // Mode 3: speed and remaining time. Space announces the overall
        // percentage instead (UpdateProgress).
        private string BuildDownloadStatus(int mode)
        {
            long total, recv;
            double speed;
            lock (_dlLock) { total = _dlTotal; recv = _dlReceived; speed = _dlSpeed; }
            bool es = Localization.CurrentLanguage == Language.Spanish;
            if (total <= 0) return Localization.Get("UpdateDownloading");
            if (mode == 1)
                return Localization.Get("UpdateSize", Updater.FormatBytes(total, es));
            if (mode == 2)
                return Localization.Get("UpdateDownloaded", Updater.FormatBytes(recv, es), Updater.FormatBytes(total, es));
            double eta = speed > 0 ? (total - recv) / speed : 0;
            return Localization.Get("UpdateSpeed", Updater.FormatSpeed(speed, es), Updater.FormatDuration(eta, es));
        }

        private string[] GetGameOverItems()
        {
            return new string[]
            {
                Localization.Get("GameOverReplay"),
                Localization.Get("GameOverMenu")
            };
        }

        private void HandleGameOverKeys(KeyEventArgs e)
        {
            string[] items = GetGameOverItems();
            if (_gameOverIdx >= items.Length) _gameOverIdx = 0;
            if (e.KeyCode == Keys.Down)
            {
                _gameOverIdx = (_gameOverIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_gameOverIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _gameOverIdx = (_gameOverIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_gameOverIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                if (_gameOverIdx == 0) StartNewGame(_currentModeKey);
                else TransitionToMainMenu();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                TransitionToMainMenu();
            }
        }

        private void HandleMainMenuKeys(KeyEventArgs e)
        {
            string[] items = GetMainMenuItems();
            if (e.KeyCode == Keys.Down)
            {
                _menuIdx = (_menuIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_menuIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _menuIdx = (_menuIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_menuIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                if (_menuIdx == 0) // Play
                {
                    _screen = GameScreen.GameSelect;
                    _gameModeIdx = 0;
                    _speech.Speak(Localization.Get("SelectMode") + Localization.Get(GetGameModeKeys()[0]), true);
                }
                else if (_menuIdx == 1) // Badges
                {
                    _screen = GameScreen.BadgesScreen;
                    _badgeIdx = 0;
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _speech.Speak(Localization.Get("MenuBadges") + ". " + GetBadgeListItems()[0], true);
                }
                else if (_menuIdx == 2) // Records
                {
                    _screen = GameScreen.RecordsScreen;
                    _recordsIdx = 0;
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _speech.Speak(Localization.Get("MenuRecords") + ". " + GetRecordsItems()[0], true);
                }
                else if (_menuIdx == 3) // Tutorial
                {
                    _screen = GameScreen.TutorialScreen;
                    _tutorialIdx = 0;
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _speech.Speak(Localization.Get("TutorialTitle") + ". " + GetTutorialItems()[0], true);
                }
                else if (_menuIdx == 4) // Change User
                {
                    _screen = GameScreen.ProfileSelectScreen;
                    _profileSelectIdx = 0;
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _speech.Speak(Localization.Get("ProfileSelectTitle") + ". " + GetProfileSelectItems()[0], true);
                }
                else if (_menuIdx == 5) // Language
                {
                    Localization.ToggleLanguage();
                    Text = Localization.Get("AppTitle");
                    SaveOptionsState();
                    _speech.Speak(GetMainMenuItems()[5], true);
                }
                else if (_menuIdx == 6) // Options
                {
                    _screen = GameScreen.Options;
                    _optionsOriginScreen = GameScreen.MainMenu;
                    _optionsIdx = 0;
                    _speech.Speak(Localization.Get("OptionsTitle") + ". " + GetOptionsMenuItems()[0], true);
                }
                else if (_menuIdx == 7) // Escuela de Audio
                {
                    _screen = GameScreen.AudioSchool;
                    _audioSchoolIdx = 0;
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _speech.Speak(Localization.Get("AudioSchoolTitle") + ". " + GetAudioSchoolItems()[0], true);
                }
#if !DEBUG
                else if (_menuIdx == 8) // Update check
                {
                    if (!_updateChecked)
                    {
                        _speech.Speak(Localization.Get("UpdateChecking"), true);
                    }
                    else if (_latestTag == null)
                    {
                        _speech.Speak(string.Format(Localization.Get("UpdateNone"), Updater.CurrentVersionString), true);
                    }
                    else
                    {
                        _speech.Speak(BuildUpdateAnnouncement(true), true);
                        _updatePromptActive = true;
                    }
                }
#endif
                else if (_menuIdx == items.Length - 1) // Exit (always last item)
                {
                    _sound.PlaySound(AudioMap.VoiceGoodbye);
                    _speech.Speak(Localization.CurrentLanguage == Language.Spanish ? "¡Adiós!" : "Goodbye!", true);
                    Task.Delay(1200).ContinueWith(_2 =>
                    {
                        try { Invoke(new Action(() => Close())); } catch { }
                    });
                }
            }
            else if (e.KeyCode == Keys.Space && _updateBusy)
            {
                // Estado general de la descarga: porcentaje actual.
                long total, recv;
                lock (_dlLock) { total = _dlTotal; recv = _dlReceived; }
                _sound.PlaySound(AudioMap.ButtonPress);
                int pct = total > 0 ? (int)(recv * 100.0 / total) : -1;
                _speech.Speak(pct >= 0
                    ? Localization.Get("UpdateProgress", Math.Min(99, pct))
                    : Localization.Get("UpdateDownloading"), true);
            }
            else if (_updateBusy && (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1))
            {
                // Detalle 1: tamano del archivo.
                _sound.PlaySound(AudioMap.ButtonPress);
                _speech.Speak(BuildDownloadStatus(1), true);
            }
            else if (_updateBusy && (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2))
            {
                // Detalle 2: descargado de total.
                _sound.PlaySound(AudioMap.ButtonPress);
                _speech.Speak(BuildDownloadStatus(2), true);
            }
            else if (_updateBusy && (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3))
            {
                // Detalle 3: velocidad y tiempo restante.
                _sound.PlaySound(AudioMap.ButtonPress);
                _speech.Speak(BuildDownloadStatus(3), true);
            }
        }

        private string[] GetOptionsMenuItems()
        {
            return new string[]
            {
                Localization.Get("OptMusicVol", _sound.MusicVol),
                Localization.Get("OptSoundVol", _sound.SfxVol),
                Localization.Get("OptVoiceVol", _sound.VoiceVol),
                Localization.Get("OptBinaural", _sound.BinauralEnabled ? Localization.Get("StateOn") : Localization.Get("StateOff")),
                Localization.Get("OptMouse", _mouseEnabled ? Localization.Get("StateOn") : Localization.Get("StateOff")),
                Localization.Get("OptBack")
            };
        }

        // "Escuela de Audio": demostracion corta del unico modelo espacial de
        // este juego (sin perfiles): recorre columnas (L/R) y profundidad
        // (frente/fondo) y un par de barridos, con un sonido real del tablero.
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
            return items.ToArray();
        }

        private void HandleAudioSchoolKeys(KeyEventArgs e)
        {
            string[] items = GetAudioSchoolItems();
            if (e.KeyCode == Keys.Down)
            {
                _audioSchoolIdx = (_audioSchoolIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_audioSchoolIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _audioSchoolIdx = (_audioSchoolIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_audioSchoolIdx], true);
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                // Sin click de menu: el tono de calibracion (sin500) debe oirse solo,
                // si no se solapa con el click y parece "otro click del menu".
                // Confirma la opcion (la repite) y luego reproduce el sonido posicionado.
                _speech.Speak(items[_audioSchoolIdx], true);
                PlayAudioSchoolTest(_audioSchoolIdx);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                _screen = GameScreen.MainMenu;
                _menuIdx = 7;
                _speech.Speak(Localization.Get("MenuAudioSchool") + ". " + GetMainMenuItems()[7], true);
            }
        }

        // Reproduce la prueba indicada por indice (ver GetAudioSchoolItems):
        // 0-7 columnas (L/R), 8 frente, 9 fondo, 10 barrido L->R, 11 frente->fondo.
        private void PlayAudioSchoolTest(int idx)
        {
            string s = AudioMap.Select;
            if (idx >= 0 && idx <= 7)
            {
                float pan = Audio.SpatialAudio.PanColumn(idx);
                _sound.PlaySoundSpatialPan(pan, 0.0f, s);
            }
            else if (idx == 8)
                _sound.PlaySoundSpatialPan(0.0f, 0.0f, s);
            else if (idx == 9)
                _sound.PlaySoundSpatialPan(0.0f, 1.0f, s);
            else if (idx == 10)
                _sound.PlaySoundSpatialSweepPan(-1.0f, 1.0f, 0.0f, 0.0f, s);
            else if (idx == 11)
                _sound.PlaySoundSpatialSweepPan(0.0f, 0.0f, 0.0f, 1.0f, s);
        }

        private void HandleOptionsKeys(KeyEventArgs e)
        {
            string[] items = GetOptionsMenuItems();
            if (e.KeyCode == Keys.Down)
            {
                _optionsIdx = (_optionsIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_optionsIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _optionsIdx = (_optionsIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_optionsIdx], true);
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                if (_optionsIdx == 0)
                {
                    _sound.MusicVol = (e.KeyCode == Keys.Right) ? Math.Min(100, _sound.MusicVol + 5) : Math.Max(0, _sound.MusicVol - 5);
                    _sound.UpdateMusicVolume();
                    _speech.Speak(Localization.Get("OptMusicVol", _sound.MusicVol), true);
                }
                else if (_optionsIdx == 1)
                {
                    _sound.SfxVol = (e.KeyCode == Keys.Right) ? Math.Min(100, _sound.SfxVol + 5) : Math.Max(0, _sound.SfxVol - 5);
                    _sound.PlaySound(AudioMap.Select);
                    _speech.Speak(Localization.Get("OptSoundVol", _sound.SfxVol), true);
                }
                else if (_optionsIdx == 2)
                {
                    _sound.VoiceVol = (e.KeyCode == Keys.Right) ? Math.Min(100, _sound.VoiceVol + 5) : Math.Max(0, _sound.VoiceVol - 5);
                    _speech.Speak(Localization.Get("OptVoiceVol", _sound.VoiceVol), true);
                }
                else if (_optionsIdx == 3)
                {
                    _sound.BinauralEnabled = !_sound.BinauralEnabled;
                    _sound.PlaySound(AudioMap.Select);
                    _speech.Speak(Localization.Get("OptBinaural", _sound.BinauralEnabled ? Localization.Get("StateOn") : Localization.Get("StateOff")), true);
                }
                else if (_optionsIdx == 4)
                {
                    _mouseEnabled = !_mouseEnabled;
                    _sound.PlaySound(AudioMap.Select);
                    _speech.Speak(Localization.Get("OptMouse", _mouseEnabled ? Localization.Get("StateOn") : Localization.Get("StateOff")), true);
                }
                SaveOptionsState();
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                SaveOptionsState();
                if (_optionsOriginScreen == GameScreen.PauseMenu)
                {
                    _screen = GameScreen.PauseMenu;
                    _speech.Speak(GetPauseMenuItems()[_pauseIdx], true);
                }
                else
                {
                    TransitionToMainMenu();
                }
            }
        }

        private string[] GetZenOptionsMenuItems()
        {
            string ambStr = _zenMgr.AmbientEnabled ? Engine.ZenManager.GetAmbientName(_zenMgr.SelectedAmbient) : Localization.Get("StateDisabled");
            string manStr = _zenMgr.MantrasEnabled ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled");
            string breathStr = _zenMgr.BreathModulationEnabled ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled");

            return new string[]
            {
                Localization.Get("ZenOptAmbient", ambStr),
                Localization.Get("ZenOptMantras", manStr),
                Localization.Get("ZenOptBreath", breathStr),
                Localization.Get("OptBack")
            };
        }

        private void HandleZenOptionsKeys(KeyEventArgs e)
        {
            string[] items = GetZenOptionsMenuItems();
            if (e.KeyCode == Keys.Down)
            {
                _zenOptionsIdx = (_zenOptionsIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_zenOptionsIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _zenOptionsIdx = (_zenOptionsIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_zenOptionsIdx], true);
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                if (_zenOptionsIdx == 0) // Ambient Track
                {
                    int maxAmb = Enum.GetValues(typeof(AmbientType)).Length;
                    int curAmb = (int)_zenMgr.SelectedAmbient;
                    if (e.KeyCode == Keys.Right) curAmb = (curAmb + 1) % maxAmb;
                    else curAmb = (curAmb - 1 + maxAmb) % maxAmb;

                    _zenMgr.SelectedAmbient = (AmbientType)curAmb;
                    _zenMgr.AmbientEnabled = (_zenMgr.SelectedAmbient != AmbientType.None);
                    _sound.PlaySound(AudioMap.ZenDropdownbutton);
                    _sound.PlaySound(AudioMap.ZenNecklacePrefix + ((curAmb % 4) + 1));
                    _zenMgr.StartZenSession(_level);
                }
                else if (_zenOptionsIdx == 1) // Mantras
                {
                    _zenMgr.MantrasEnabled = !_zenMgr.MantrasEnabled;
                    _sound.PlaySound(_zenMgr.MantrasEnabled ? AudioMap.ZenCheckon : AudioMap.ZenCheckoff);
                    _zenMgr.UpdateZenSessionState();
                }
                else if (_zenOptionsIdx == 2) // Breath
                {
                    _zenMgr.BreathModulationEnabled = !_zenMgr.BreathModulationEnabled;
                    _sound.PlaySound(_zenMgr.BreathModulationEnabled ? AudioMap.ZenCheckon : AudioMap.ZenCheckoff);
                    _zenMgr.UpdateZenSessionState();
                }
                SaveOptionsState();
                _speech.Speak(GetZenOptionsMenuItems()[_zenOptionsIdx], true);
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ZenMenuclose);
                _screen = GameScreen.PauseMenu;
                _speech.Speak(GetPauseMenuItems()[_pauseIdx], true);
            }
        }

        private string[] GetBadgeListKeys()
        {
            return new string[]
            {
                "BadgeInferno",
                "BadgeStellar",
                "BadgeChromatic",
                "BadgeBlaster",
                "BadgeBejeweler",
                "BadgeFinalFrenzy",
                "BadgeHighVoltage",
                "BadgeAnteUp",
                "BadgeGambler",
                "BadgeGlacialExplorer",
                "BadgeIceBreaker",
                "BadgeDiamondMine",
                "BadgeRelicHunter",
                "BadgeButterflyMonarch",
                "BadgeButterflyBonanza",
                "BadgeAnnihilator",
                "BadgeSuperstar",
                "BadgeLevelord",
                "BadgeTopSecret",
                "BadgeHeroes",
                "OptBack"
            };
        }

        private string[] GetBadgeListItems()
        {
            string[] keys = GetBadgeListKeys();
            List<string> list = new List<string>();
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "OptBack")
                {
                    list.Add(Localization.Get("OptBack"));
                }
                else
                {
                    BadgeTier t = _badgeMgr.GetTier(keys[i]);
                    string tierStr = Localization.Get(string.Format("Tier{0}", t.ToString()));
                    list.Add(string.Format("{0}: {1}", Localization.Get(keys[i]), tierStr));
                }
            }
            list.Add(Localization.Get("BadgeMenuHelp"));
            return list.ToArray();
        }

        private void HandleBadgesKeys(KeyEventArgs e)
        {
            string[] items = GetBadgeListItems();
            if (e.KeyCode == Keys.Down)
            {
                _badgeIdx = (_badgeIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_badgeIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _badgeIdx = (_badgeIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_badgeIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (items[_badgeIdx] == Localization.Get("OptBack") || items[_badgeIdx] == Localization.Get("BadgeMenuHelp"))
                {
                    _sound.PlaySound(AudioMap.ButtonPress);
                    TransitionToMainMenu();
                }
                else
                {
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _speech.Speak(items[_badgeIdx], true);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                TransitionToMainMenu();
            }
        }

        private string[] GetRecordsItems()
        {
            List<string> list = new List<string>();
            string rankStr = RankSystem.GetRankTitle(_progress.TotalScore);
            list.Add(Localization.Get("StatPlayerRank", rankStr));
            list.Add(Localization.Get("StatTotalScore", _progress.TotalScore));
            list.Add(Localization.Get("StatTotalGems", _progress.TotalGemsCleared));
            list.Add(Localization.Get("StatClassicLevel", _progress.ClassicLevel));
            list.Add(Localization.Get("StatZenLevel", _progress.ZenLevel));
            list.Add(Localization.Get("StatLightningRecord", _progress.LightningHighScore));
            list.Add(Localization.Get("StatPokerRecord", _progress.PokerHighScore));
            list.Add(Localization.Get("StatButterfliesRecord", _progress.ButterfliesHighScore));
            list.Add(Localization.Get("StatIceStormRecord", _progress.IceStormHighScore));
            list.Add(Localization.Get("StatDiamondMineRecord", _progress.DiamondMineHighScore));
            list.Add(Localization.Get("StatFlamesDestroyed", _progress.TotalFlameGemsDestroyed));
            list.Add(Localization.Get("StatStarsDestroyed", _progress.TotalStarGemsDestroyed));
            list.Add(Localization.Get("StatHypercubesDestroyed", _progress.TotalHypercubesDestroyed));
            list.Add(Localization.Get("OptBack"));
            return list.ToArray();
        }

        private void HandleRecordsKeys(KeyEventArgs e)
        {
            string[] items = GetRecordsItems();
            if (e.KeyCode == Keys.Down)
            {
                _recordsIdx = (_recordsIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _sound.PlaySound(AudioMap.Tooltip);
                _speech.Speak(items[_recordsIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _recordsIdx = (_recordsIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _sound.PlaySound(AudioMap.Tooltip);
                _speech.Speak(items[_recordsIdx], true);
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonRelease);
                _sound.PlaySound(AudioMap.RankCountup);
                TransitionToMainMenu();
            }
        }

        private string[] GetTutorialItems()
        {
            return new string[]
            {
                Localization.Get("TutorialStep1"),
                Localization.Get("TutorialStep2"),
                Localization.Get("TutorialStep3"),
                Localization.Get("TutorialStep4"),
                Localization.Get("TutorialStep5"),
                Localization.Get("TutorialStep6"),
                Localization.Get("TutorialStep7"),
                Localization.Get("TutorialStep8"),
                Localization.Get("OptBack")
            };
        }

        private void HandleTutorialKeys(KeyEventArgs e)
        {
            string[] items = GetTutorialItems();
            if (e.KeyCode == Keys.Down)
            {
                _tutorialIdx = (_tutorialIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_tutorialIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _tutorialIdx = (_tutorialIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_tutorialIdx], true);
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                TransitionToMainMenu();
            }
        }

        private void HandleGameSelectKeys(KeyEventArgs e)
        {
            string[] keys = GetGameModeKeys();
            if (e.KeyCode == Keys.Down)
            {
                _gameModeIdx = (_gameModeIdx + 1) % keys.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(Localization.Get(keys[_gameModeIdx]), true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _gameModeIdx = (_gameModeIdx - 1 + keys.Length) % keys.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(Localization.Get(keys[_gameModeIdx]), true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                string selectedKey = keys[_gameModeIdx];
                if (selectedKey.EndsWith("Locked"))
                {
                    _sound.PlaySound(AudioMap.Badmove);
                    _speech.Speak(Localization.Get(selectedKey), true);
                    return;
                }

                _sound.PlaySound(AudioMap.ButtonPress);
                if (selectedKey == "BackToMain")
                {
                    TransitionToMainMenu();
                    return;
                }
                else if (selectedKey == "ModeQuest")
                {
                    _screen = GameScreen.QuestRelicScreen;
                    _relicIdx = 0;
                    _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTheme));
                    _speech.Speak(Localization.Get("QuestSelectTitle") + ". " + GetQuestRelicItems()[0], true);
                    return;
                }

                StartNewGame(selectedKey);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                TransitionToMainMenu();
            }
        }

        private string[] GetQuestRelicItems()
        {
            List<string> items = new List<string>();
            for (int i = 1; i <= 5; i++)
            {
                int relicIdx = i - 1;
                int done = _progress.CountCompletedInRelic(relicIdx);
                items.Add(Localization.Get("Relic" + i) + (done >= 8 ? Localization.Get("QuestCompletedMark") : " (" + done + " de 8)"));
            }
            items.Add(Localization.Get("OptBack"));
            return items.ToArray();
        }

        private void HandleQuestRelicKeys(KeyEventArgs e)
        {
            string[] items = GetQuestRelicItems();
            if (e.KeyCode == Keys.Down)
            {
                _relicIdx = (_relicIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.QuestMenuButtonMouseover1);
                _speech.Speak(items[_relicIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _relicIdx = (_relicIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.QuestMenuButtonMouseover1);
                _speech.Speak(items[_relicIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (_relicIdx == items.Length - 1)
                {
                    _sound.PlaySound(AudioMap.ButtonPress);
                    _screen = GameScreen.GameSelect;
                    _sound.PlayMusic(MusicMap.FileName(MusicMap.MainTheme));
                    _speech.Speak(Localization.Get("SelectMode") + Localization.Get(GetGameModeKeys()[_gameModeIdx]), true);
                }
                else
                {
                    _sound.PlaySound(AudioMap.QuestMenuButton1);
                    _sound.PlaySound(AudioMap.QuestMenuRelicRevealedObject);
                    _screen = GameScreen.QuestChallengeScreen;
                    _questChallengeIdx = 0;
                    _speech.Speak(GetQuestChallengeItems()[0], true);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                _screen = GameScreen.GameSelect;
                _sound.PlayMusic(MusicMap.FileName(MusicMap.MainTheme));
                _speech.Speak(Localization.Get("SelectMode") + Localization.Get(GetGameModeKeys()[_gameModeIdx]), true);
            }
        }

        private string[] GetQuestChallengeItems()
        {
            List<string> items = new List<string>();
            Engine.QuestMission[] missions = Engine.QuestManager.GetRelicMissions(_relicIdx);
            foreach (var m in missions)
            {
                string item = m.GetName();
                if (_progress.IsQuestMissionComplete(m.MissionIndex))
                    item += Localization.Get("QuestCompletedMark");
                items.Add(item);
            }
            items.Add(Localization.Get("OptBack"));
            return items.ToArray();
        }

        private void HandleQuestChallengeKeys(KeyEventArgs e)
        {
            string[] items = GetQuestChallengeItems();
            if (e.KeyCode == Keys.Down)
            {
                _questChallengeIdx = (_questChallengeIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.QuestMenuButtonMouseover1);
                _speech.Speak(items[_questChallengeIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _questChallengeIdx = (_questChallengeIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.QuestMenuButtonMouseover1);
                _speech.Speak(items[_questChallengeIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (_questChallengeIdx == items.Length - 1)
                {
                    _sound.PlaySound(AudioMap.QuestMenuButton1);
                    _screen = GameScreen.QuestRelicScreen;
                    _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTheme));
                    _speech.Speak(GetQuestRelicItems()[_relicIdx], true);
                }
                else
                {
                    Engine.QuestMission[] missions = Engine.QuestManager.GetRelicMissions(_relicIdx);
                    _activeQuest = missions[_questChallengeIdx];
                    _activeQuestIndex = _activeQuest.MissionIndex;
                    _activeQuestName = _activeQuest.GetName();
                    _sound.PlaySound(AudioMap.QuestOrb1);
                    _sound.PlaySound(AudioMap.QuestGet);
                    StartNewGame("ModeQuest");
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                _screen = GameScreen.QuestRelicScreen;
                _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTheme));
                _speech.Speak(GetQuestRelicItems()[_relicIdx], true);
            }
        }

        private string[] GetProfileSelectItems()
        {
            List<string> items = new List<string>();
            for (int i = 0; i < _profileMgr.Profiles.Count; i++)
            {
                string marker = (i == _profileMgr.CurrentProfileIndex) ? " (" + Localization.Get("StateEnabled") + ")" : "";
                items.Add(_profileMgr.Profiles[i].ProfileName + marker);
            }
            items.Add(Localization.Get("ProfileCreateNew"));
            items.Add(Localization.Get("ProfileDelete"));
            items.Add(Localization.Get("OptBack"));
            return items.ToArray();
        }

        private void HandleProfileSelectKeys(KeyEventArgs e)
        {
            string[] items = GetProfileSelectItems();
            if (e.KeyCode == Keys.Down)
            {
                _profileSelectIdx = (_profileSelectIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_profileSelectIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _profileSelectIdx = (_profileSelectIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_profileSelectIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                string selectedItem = items[_profileSelectIdx];

                if (selectedItem == Localization.Get("ProfileCreateNew"))
                {
                    _screen = GameScreen.ProfileInput;
                    _profileInputBuffer = "";
                    _speech.Speak(Localization.Get("CreateProfileTitle") + ". " + Localization.Get("EnterNamePrompt"), true);
                }
                else if (selectedItem == Localization.Get("ProfileDelete"))
                {
                    if (_profileMgr.Profiles.Count > 1)
                    {
                        _profileMgr.Profiles.RemoveAt(_profileMgr.CurrentProfileIndex);
                        _profileMgr.CurrentProfileIndex = 0;
                        _profileMgr.Save();
                        _badgeMgr = BadgeManager.Load(_profileMgr.CurrentProfile.ProfileName);
                        _profileSelectIdx = 0;
                        items = GetProfileSelectItems();
                        _speech.Speak(Localization.Get("ProfileSelectTitle") + ". " + items[0], true);
                    }
                    else if (_profileMgr.Profiles.Count == 1)
                    {
                        _profileMgr.Profiles.Clear();
                        _profileMgr.CurrentProfileIndex = 0;
                        _profileMgr.Save();
                        _screen = GameScreen.ProfileInput;
                        _profileInputBuffer = "";
                        _sound.PlaySound(AudioMap.VoiceWelcometobejeweled);
                        _speech.Speak(Localization.Get("CreateProfileTitle") + ". " + Localization.Get("EnterNamePrompt"), true);
                    }
                }
                else if (selectedItem == Localization.Get("OptBack"))
                {
                    TransitionToMainMenu();
                }
                else if (_profileSelectIdx < _profileMgr.Profiles.Count)
                {
                    _profileMgr.CurrentProfileIndex = _profileSelectIdx;
                    _profileMgr.Save();
                    _badgeMgr = BadgeManager.Load(_profileMgr.CurrentProfile.ProfileName);
                    TransitionToMainMenu(true);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                TransitionToMainMenu();
            }
        }

        private void StartNewGame(string modeKey)
        {
            _score = 0;
            _level = 1;
            _levelProgressPoints = 0;
            _cascadeChain = 0;
            _cursorX = 3;
            _cursorY = 3;
            _questGemsCleared = 0;
            _questButterfliesFreed = 0;
            _questNuggets = 0;
            _questGoldConverted = 0;
            _questBombsDestroyed = 0;
            _questMaxCascade = 0;
            _questHandsScored = 0;
            _questIceColumnsBroken = 0;
            _pokerSkulls = 0;
            _pokerSkullCharge = 0;
            _pokerCards.Clear();
            _shufflesRemaining = 3;
            _iceRiseCounter = 0;
            _iceRiseInterval = 4;
            _diamondDepthMeters = 0;
            _pokerHandBonus = 0;

            _board = new Board(new Random().Next());
            _screen = GameScreen.Playing;
            _currentModeKey = modeKey;

            // Never let Zen timers/sounds leak into other modes
            if (_zenMgr != null) _zenMgr.StopZenSession();

            _sound.PlaySound(AudioMap.VoiceGetready);
            Task.Delay(1000).ContinueWith(_ =>
            {
                try
                {
                    Invoke(new Action(() =>
                    {
                        if (IsDisposed || !IsHandleCreated) return;
                        if (_screen != GameScreen.Playing) return;
                        _sound.PlaySound(AudioMap.VoiceGo);
                    }));
                }
                catch { }
            });

            string startSpeech = null;
            if (modeKey == "ModeLightning")
            {
                _lightningTimeLeft = 60;
                _lightningMultiplier = 1;
                _lightningTankSeconds = 0;
                _lastHurrahActive = false;
                _lastHurrahScore = 0;
                _lightningTimer.Start();
                _sound.PlayMusic(MusicMap.FileName(MusicMap.Lightning));
                startSpeech = Localization.Get("LightningStarted");
            }
            else if (modeKey == "ModeZen")
            {
                _zenMgr.StartZenSession();
                startSpeech = Localization.Get("ZenStarted");
            }
            else if (modeKey == "ModePoker")
            {
                _sound.PlayMusic(MusicMap.FileName(MusicMap.Poker));
                startSpeech = Localization.Get("PokerStarted");
            }
            else if (modeKey == "ModeButterflies")
            {
                _board.InitializeButterfliesBoard();
                _sound.PlayMusic(MusicMap.FileName(MusicMap.Butterflies));
                startSpeech = Localization.Get("ButterfliesStarted") + " " + Localization.Get("ButterflyStart", _board.GetButterflyCount());
            }
            else if (modeKey == "ModeIceStorm")
            {
                for (int i = 0; i < 8; i++)
                {
                    _iceColumns[i] = 0;
                    _iceSkullTicks[i] = 0;
                }
                _iceRiseInterval = Math.Max(1, 6 - _level);
                _iceRiseCounter = 0;
                _lightningTimer.Start();
                _sound.PlayMusic(MusicMap.FileName(MusicMap.IceStorm));
                startSpeech = Localization.Get("IceStormStarted");
            }
            else if (modeKey == "ModeDiamondMine")
            {
                _diamondDepthMeters = 0;
                _lightningTimeLeft = 60;
                _lightningMultiplier = 1;
                _lightningTankSeconds = 0;
                _lightningTimer.Start();
                _board.InitializeDiamondMineBoard();
                _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestBuriedTreasure));
                startSpeech = Localization.Get("DiamondMineStarted");
            }
            else if (modeKey == "ModeQuest")
            {
                if (_activeQuest != null)
                {
                    switch (_activeQuest.Type)
                    {
                        case Engine.QuestType.Butterflies:
                            _board.InitializeButterfliesBoard();
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.Butterflies));
                            break;
                        case Engine.QuestType.DiamondMine:
                        case Engine.QuestType.GoldRush:
                            _board.InitializeDiamondMineBoard();
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestBuriedTreasure));
                            break;
                        case Engine.QuestType.TimeBomb:
                            // Authentic: the first board is already armed with bombs
                            _board.InitializeBoard(true);
                            _sound.PlaySound(AudioMap.BombAppears);
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTimeBombs));
                            _lightningTimer.Start();
                            break;
                        case Engine.QuestType.IceStorm:
                            for (int i = 0; i < 8; i++)
                            {
                                _iceColumns[i] = 0;
                                _iceSkullTicks[i] = 0;
                            }
                            _iceRiseInterval = Math.Max(1, 6 - _level);
                            _iceRiseCounter = 0;
                            _lightningTimer.Start();
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.IceStorm));
                            break;
                        case Engine.QuestType.Poker:
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.Poker));
                            break;
                        case Engine.QuestType.Avalanche:
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTurnByTurn));
                            break;
                        default:
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTakeYourTime));
                            break;
                    }
                }
                else
                {
                    _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTakeYourTime));
                }

                startSpeech = Localization.Get("QuestMissionIntro", _activeQuestName);
                if (_activeQuest != null && _activeQuest.Type == Engine.QuestType.Butterflies)
                    startSpeech += Localization.Get("ButterflyStart", _board.GetButterflyCount());
            }
            else
            {
                _sound.PlayMusic(MusicMap.FileName(MusicMap.ClassicPart1));
                startSpeech = Localization.Get("ClassicStarted");
            }

            // Sequence: "Get ready!" (audio) -> "Go!" (audio) -> mode intro (TTS)
            if (startSpeech != null)
            {
                string sp = startSpeech;
                _speech.Speak(sp, true);
            }
            Task.Delay(3000).ContinueWith(_ =>
            {
                try
                {
                    Invoke(new Action(() =>
                    {
                        if (IsDisposed || !IsHandleCreated) return;
                        if (_screen != GameScreen.Playing || _isSwapping) return;
                        AnnounceCurrentCell();
                    }));
                }
                catch { }
            });
        }

        private void HandlePlayingKeys(KeyEventArgs e)
        {
            // Swaps: W/A/S/D or Ctrl+arrow, symmetric in all four directions to match
            // the original layout (WASD keys also move in 3D shooters using the
            // same physical directions). Kept together so the plain keys never
            // drop through to the query keys below.
            if (e.KeyCode == Keys.W || (e.Control && e.KeyCode == Keys.Up)) PerformSwap(0, -1);
            else if (e.KeyCode == Keys.S || (e.Control && e.KeyCode == Keys.Down)) PerformSwap(0, 1);
            else if (e.KeyCode == Keys.A || (e.Control && e.KeyCode == Keys.Left)) PerformSwap(-1, 0);
            else if (e.KeyCode == Keys.D || (e.Control && e.KeyCode == Keys.Right)) PerformSwap(1, 0);

            else if (e.KeyCode == Keys.Left && _cursorX > 0) { _cursorX--; _sound.PlaySoundSpatial(AudioMap.Select, _cursorX, _cursorY); AnnounceCurrentCell(); }
            else if (e.KeyCode == Keys.Right && _cursorX < Board.Cols - 1) { _cursorX++; _sound.PlaySoundSpatial(AudioMap.Select, _cursorX, _cursorY); AnnounceCurrentCell(); }
            else if (e.KeyCode == Keys.Up && _cursorY > 0) { _cursorY--; _sound.PlaySoundSpatial(AudioMap.Select, _cursorX, _cursorY); AnnounceCurrentCell(); }
            else if (e.KeyCode == Keys.Down && _cursorY < Board.Rows - 1) { _cursorY++; _sound.PlaySoundSpatial(AudioMap.Select, _cursorX, _cursorY); AnnounceCurrentCell(); }

#if DEBUG
            else if (e.Shift && e.KeyCode == Keys.H)
            {
                _board.SetGem(_cursorX, _cursorY, new Gem(GemColor.Red, SpecialType.Hypercube));
                _sound.PlayHypercubeSweep(AudioMap.HypercubeCreate, _cursorX, _cursorY);
                _speech.Speak(Localization.Get("HypercubeCreatedCell"), true);
            }
            else if (e.Shift && e.KeyCode == Keys.F)
            {
                _board.SetGem(_cursorX, _cursorY, new Gem(GemColor.Red, SpecialType.Flame));
                _sound.PlaySound(AudioMap.PowergemCreated);
                _speech.Speak(Localization.Get("FlameCreatedCell"), true);
            }
            else if (e.Shift && e.KeyCode == Keys.S)
            {
                _board.SetGem(_cursorX, _cursorY, new Gem(GemColor.Red, SpecialType.Star));
                _sound.PlayStarGemLaser(AudioMap.LasergemCreated, _cursorX, _cursorY);
                _speech.Speak(Localization.Get("StarCreatedCell"), true);
            }
            else if (e.Shift && e.KeyCode == Keys.R)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                _board.InitializeBoard();
                _score = 0;
                _cascadeChain = 0;
                _speech.Speak(Localization.Get("PauseReset") + ". " + Localization.Get("ClassicStarted"), true);
            }
#endif
            else if (e.KeyCode == Keys.R)
            {
                if (_currentModeKey == "ModeLightning")
                    _speech.Speak(Localization.Get("LightningScoreAnnouncement", _score, _lightningTimeLeft, _lightningMultiplier * 5), true);
                else if (_currentModeKey == "ModeIceStorm")
                {
                    int iceHeight = _iceColumns[_cursorX];
                    if (iceHeight >= 8 && _iceSkullTicks[_cursorX] > 0)
                        _speech.Speak(Localization.Get("IceColumnCrestedStatus", _score, _iceSkullTicks[_cursorX]), true);
                    else
                        _speech.Speak(Localization.Get("IceColumnStatus", _score, iceHeight), true);
                }
                else if (_currentModeKey == "ModeButterflies")
                    _speech.Speak(Localization.Get("ButterflyStatus", _board.GetButterflyCount(), FormatColumns(_board.GetButterflyColumns())), true);
                else if (_currentModeKey == "ModeQuest")
                    _speech.Speak(Localization.Get("QuestActiveStatus", _activeQuestName, _score), true);
                else
                    _speech.Speak(Localization.Get("ScoreAnnouncement", _score, _level), true);
            }
            else if (e.KeyCode == Keys.C) { AnnounceCurrentCell(); }
            else if (e.KeyCode == Keys.Q)
            {
                AnnounceFullModeStatus();
            }
            else if (e.KeyCode == Keys.H)
            {
                MoveHint? hint = HintFinder.FindValidMove(_board);
                if (hint.HasValue)
                {
                    MoveHint h = hint.Value;
                    Gem gem = _board.GetGem(h.FromX, h.FromY);
                    string gemName = (gem != null) ? gem.GetNameLocalized() : Localization.Get("Gem");
                    string fromCell = string.Format("{0}{1}", (char)('A' + h.FromX), h.FromY + 1);

                    string dirStr = Localization.Get("DirRight");
                    if (h.ToX < h.FromX) dirStr = Localization.Get("DirLeft");
                    else if (h.ToY > h.FromY) dirStr = Localization.Get("DirDown");
                    else if (h.ToY < h.FromY) dirStr = Localization.Get("DirUp");

                    _sound.PlaySound(AudioMap.QuestNotify);
                    _speech.Speak(Localization.Get("HintFound", gemName, fromCell, dirStr), true);
                }
                else
                {
                    _sound.PlaySound(AudioMap.Badmove);
                    _speech.Speak(Localization.Get("NoHintFound"), true);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _lightningTimer.Stop();
                _screen = GameScreen.PauseMenu;
                _pauseIdx = 0;
                _sound.PlaySound(AudioMap.ButtonPress);
                _speech.Speak(Localization.Get("PauseTitle") + ". " + GetPauseMenuItems()[0], true);
            }
        }

        private string[] GetPauseMenuItems()
        {
            List<string> items = new List<string>
            {
                Localization.Get("PauseResume"),
                Localization.Get("PauseReset"),
                Localization.Get("PauseOptions")
            };
            if (_currentModeKey == "ModeZen")
            {
                items.Add(Localization.Get("ZenOptionsTitle"));
            }
            items.Add(Localization.Get("PauseQuit"));
            return items.ToArray();
        }

        private void HandlePauseMenuKeys(KeyEventArgs e)
        {
            string[] items = GetPauseMenuItems();
            if (e.KeyCode == Keys.Down)
            {
                _pauseIdx = (_pauseIdx + 1) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_pauseIdx], true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                _pauseIdx = (_pauseIdx - 1 + items.Length) % items.Length;
                _sound.PlaySound(AudioMap.ButtonMouseover);
                _speech.Speak(items[_pauseIdx], true);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                _sound.PlaySound(AudioMap.ButtonPress);
                string itemText = items[_pauseIdx];
                if (itemText == Localization.Get("PauseResume"))
                {
                    _screen = GameScreen.Playing;
                    if (UsesClockTimer()) _lightningTimer.Start();
                    AnnounceCurrentCell();
                }
                else if (itemText == Localization.Get("PauseReset"))
                {
                    // Rebuild the mode properly: correct board type, counters, timers and music
                    _sound.PlaySound(AudioMap.ButtonPress);
                    StartNewGame(_currentModeKey);
                }
                else if (itemText == Localization.Get("PauseOptions"))
                {
                    _screen = GameScreen.Options;
                    _optionsOriginScreen = GameScreen.PauseMenu;
                    _optionsIdx = 0;
                    _speech.Speak(Localization.Get("OptionsTitle") + ". " + GetOptionsMenuItems()[0], true);
                }
                else if (itemText == Localization.Get("ZenOptionsTitle"))
                {
                    _sound.PlaySound(AudioMap.ZenMenuopen);
                    _screen = GameScreen.ZenOptionsScreen;
                    _speech.Speak(Localization.Get("ZenOptionsTitle") + ". " + GetZenOptionsMenuItems()[0], true);
                }
                else if (itemText == Localization.Get("PauseQuit"))
                {
                    TransitionToMainMenu();
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _screen = GameScreen.Playing;
                if (UsesClockTimer()) _lightningTimer.Start();
                AnnounceCurrentCell();
            }
        }

        private async void PerformSwap(int dx, int dy)
        {
            if (_isSwapping) return;
            _isSwapping = true;

            // Snapshot the game context so the async continuation can detect
            // that the user paused / reset / restarted while it was running.
            GameScreen screenAtSwap = _screen;
            Board boardAtSwap = _board;
            string modeAtSwap = _currentModeKey;
            int fromX = _cursorX;
            int fromY = _cursorY;

            int targetX = _cursorX + dx;
            int targetY = _cursorY + dy;

            if (targetX < 0 || targetX >= Board.Cols || targetY < 0 || targetY >= Board.Rows)
            {
                _sound.PlaySound(AudioMap.Badmove);
                _speech.Speak(Localization.Get("EdgeReached"), true);
                _isSwapping = false;
                return;
            }

            try
            {
                bool success = _board.SwapGems(_cursorX, _cursorY, targetX, targetY);
                if (success)
            {
                _cursorX = targetX;
                _cursorY = targetY;

                // Swap confirmation sound (start rotate) glides from the origin to the
                // destination column (HRTF swipe), so the movement is heard.
                _sound.PlaySoundSpatialSweep(AudioMap.StartRotate, fromX, _cursorX, _cursorY);

                bool isButterfliesMode = _currentModeKey == "ModeButterflies" || (_currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.Butterflies);
                bool isAlchemyMode = _currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.Alchemy;
                bool isBombMode = _currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.TimeBomb;

                CascadeResult res = _board.ProcessMatchesAndGravity(_currentModeKey == "ModeLightning", isButterfliesMode, isAlchemyMode, isBombMode);
                if (res.AnyMatched)
                {
                    _cascadeChain++;
                    if (_cascadeChain > 7) _cascadeChain = 7;

                    bool levelUpVoicePlayed = false;

                    // Reaccion en cadena: cada nivel reproduce su combo (con tono) y
                    // luego el gem hit sonando mientras caen las gemas de ese nivel,
                    // espaciados con stepIntervalMs como en el Relampago original.
                    int levels = Math.Max(1, Math.Min(res.CascadeDepth, 7));
                    int teardropCount = Math.Min(res.TotalGemsDestroyed, 16);
                    if (teardropCount <= 0) teardropCount = 1;

                    int hitsPerLevel = Math.Max(1, Math.Min(3, teardropCount / levels));
                    int chainLevels = levels;
                    int chainHits = hitsPerLevel;
                    int cx = _cursorX, cy = _cursorY;
                    string chainMode = _currentModeKey;

                    // Official scoring per mode: Classic scales with the level,
                    // Lightning applies the 5x multiplier to everything except
                    // the Hypercube creation bonus (a flat 500 per cube) and
                    // adds a speed bonus that grows +100 per chained match from
                    // 200 up to 1000 points.
                    int lightningSpeedBonus = 0;
                    if (_currentModeKey == "ModeLightning")
                    {
                        lightningSpeedBonus = Math.Min(1000, 200 + (_cascadeChain - 1) * 100);
                    }

                    // Pre-calculate points for each cascade level
                    int[] stepAddedScores = new int[chainLevels];
                    for (int s = 0; s < chainLevels; s++)
                    {
                        int stepBase = (s < res.StepPoints.Count) ? res.StepPoints[s] : (res.BasePoints / chainLevels);
                        int stepHyper = (s < res.StepHypercubeCreationPoints.Count) ? res.StepHypercubeCreationPoints[s] : 0;
                        int sScore;
                        if (_currentModeKey == "ModeLightning")
                        {
                            sScore = (stepBase - stepHyper) * (_lightningMultiplier * 5) + stepHyper;
                            if (s == 0) sScore += lightningSpeedBonus;
                        }
                        else if (_currentModeKey == "ModeClassic")
                        {
                            sScore = stepBase * _level;
                        }
                        else
                        {
                            sScore = stepBase;
                        }

                        if (s == 0 && res.AnnihilatorUsed)
                        {
                            sScore += 2500;
                        }
                        stepAddedScores[s] = sScore;
                    }

                    if (res.AnnihilatorUsed)
                    {
                        _sound.PlaySound(AudioMap.Preblast);
                        _sound.PlaySoundSpatial(AudioMap.BombExplode, fromX, _cursorY);
                        _sound.PlaySound(AudioMap.Hyperspace);
                    }

#pragma warning disable 4014
                    Task.Run(async () =>
                    {
                        try
                        {
                            for (int lvl = 1; lvl <= chainLevels; lvl++)
                            {
                                int stepPts = stepAddedScores[lvl - 1];

                                // Sumar puntuación en tiempo real con cada nivel de combo
                                if (this.IsHandleCreated)
                                    this.BeginInvoke((MethodInvoker)delegate
                                    {
                                        try
                                        {
                                            if (_screen != screenAtSwap || !ReferenceEquals(_board, boardAtSwap) || _currentModeKey != modeAtSwap) return;
                                            
                                            _score += stepPts;

                                            if (_currentModeKey == "ModeLightning" && _lastHurrahActive)
                                            {
                                                _lastHurrahScore += stepPts;
                                                if (_lastHurrahScore > _progress.BestFrenzyScore)
                                                    _progress.BestFrenzyScore = _lastHurrahScore;
                                            }

                                            int rankBefore = RankSystem.GetRankLevel(_progress.TotalScore);
                                            _progress.TotalScore += stepPts;
                                            int rankAfter = RankSystem.GetRankLevel(_progress.TotalScore);

                                            if (rankAfter > rankBefore)
                                            {
                                                _sound.PlaySound(AudioMap.Rankup);
                                                _speech.Speak(Localization.Get("RankUpAnnouncement", RankSystem.GetRankTitle(_progress.TotalScore)), true);
                                                _profileMgr.Save();
                                            }

                                            if (_currentModeKey == "ModeClassic" || _currentModeKey == "ModeZen")
                                            {
                                                _levelProgressPoints += stepPts;
                                                int targetPoints = GetLevelTargetPoints(_level);
                                                if (_levelProgressPoints >= targetPoints)
                                                {
                                                    _levelProgressPoints -= targetPoints;
                                                    _level++;
                                                    int newLevel = _level;
                                                    _sound.PlaySound(AudioMap.VoiceLevelcomplete);

                                                    if (_currentModeKey == "ModeClassic")
                                                    {
                                                        int stage = ((_level - 1) % 4) + 1;
                                                        _sound.PlayMusic(MusicMap.FileName(MusicMap.ClassicParts[stage - 1]));
                                                        if (newLevel > _progress.ClassicLevel)
                                                        {
                                                            if (_progress.ClassicLevel < 5 && newLevel >= 5)
                                                            {
                                                                _sound.PlaySound(AudioMap.Secretunlocked);
                                                                _speech.Speak(Localization.Get("UnlockPoker"), true);
                                                            }
                                                            _progress.ClassicLevel = newLevel;
                                                        }
                                                    }
                                                    else if (_currentModeKey == "ModeZen")
                                                    {
                                                        if (!(_zenMgr.AmbientEnabled && _zenMgr.SelectedAmbient != AmbientType.None))
                                                        {
                                                            _sound.PlayMusic(Engine.ZenManager.GetZenTrackForLevel(_level));
                                                        }
                                                        if (newLevel > _progress.ZenLevel)
                                                        {
                                                            if (_progress.ZenLevel < 5 && newLevel >= 5)
                                                            {
                                                                _sound.PlaySound(AudioMap.Secretunlocked);
                                                                _speech.Speak(Localization.Get("UnlockButterflies"), true);
                                                            }
                                                            _progress.ZenLevel = newLevel;
                                                        }
                                                    }
                                                }
                                            }
                                            else if (_currentModeKey == "ModeIceStorm")
                                            {
                                                int newLevel = (_score / 5000) + 1;
                                                if (newLevel > _level)
                                                {
                                                    _level = newLevel;
                                                    _iceRiseInterval = Math.Max(1, 6 - _level);
                                                    _sound.PlaySound(AudioMap.VoiceLevelcomplete);
                                                }
                                            }
                                        }
                                        catch { }
                                    });

                                // Combo de este nivel de cadena (solo del 2 en adelante).
                                // En Relámpago sube de tono por nivel; en los demás modos
                                // los archivos combo_2..combo_7 ya suben armónicamente.
                                string comboSoundName = null;
                                float comboPitch = 1f;
                                if (lvl >= 2)
                                {
                                    if (chainMode == "ModeZen" && lvl <= 2)
                                        comboSoundName = AudioMap.ZenCombo2;
                                    else
                                        comboSoundName = AudioMap.ComboPrefix + lvl;
                                    comboPitch = (float)Math.Pow(2.0, (chainMode == "ModeLightning" ? (lvl - 1) : 0) / 12.0);
                                    string name = comboSoundName;
                                    float pitch = comboPitch;
                                    if (this.IsHandleCreated)
                                        this.BeginInvoke((MethodInvoker)delegate
                                        {
                                            try
                                            {
                                                if (chainMode == "ModeLightning")
                                                    _sound.PlaySoundPitch(name, pitch);
                                                else
                                                    _sound.PlaySound(name);
                                            }
                                            catch { }
                                        });
                                }

                                // Explosión de impacto de ESTE nivel: suena CON el combo
                                // (es el mismo evento, las gemas se rompen), de modo que la
                                // cadena de combos y las explosiones no se solapan. En el
                                // último nivel (clímax) suena la explosión grande y la
                                // creación de gema especial si aplica.
                                int impactGems = (lvl == chainLevels) ? res.TotalGemsDestroyed : Math.Min(res.TotalGemsDestroyed, 3);
                                if (this.IsHandleCreated)
                                    this.BeginInvoke((MethodInvoker)delegate
                                    {
                                        try
                                        {
                                            PlaySwapImpact(impactGems, cx, cy);
                                            if (lvl == chainLevels) PlaySwapSpecialCreation(res, cx, cy);
                                        }
                                        catch { }
                                    });

                                // Caídas de gemas por gravedad (sonido de gem_fall espacial).
                                for (int g = 0; g < chainHits; g++)
                                {
                                    if (this.IsHandleCreated)
                                        this.BeginInvoke((MethodInvoker)delegate
                                        {
                                            try { _sound.PlaySoundSpatial(AudioMap.GemFall, cx, cy); }
                                            catch { }
                                        });
                                    await Task.Delay(90);
                                }

                                // Cadencia natural de PopCap: pausa musical de 280 ms tras las
                                // caídas antes del siguiente nivel de cascada para una clara
                                // sucesión de acordes.
                                await Task.Delay(280);
                            }
                        }
                        catch { }
                    });
#pragma warning restore 4014

                    // Tick leve al emparejar gemas de tiempo (no con el multiplicador,
                    // que ya no tickea segundo a segundo). Es el unico momento en que
                    // debe oirse este sonido.
                    if (res.TimeGemsMatched > 0)
                        _sound.PlaySound(AudioMap.Tick);

                    // Revalidate: the user may have paused / reset / restarted while
                    // the swap was resolving asynchronously, so abandon stale state.
                    if (_screen != screenAtSwap || !ReferenceEquals(_board, boardAtSwap) || _currentModeKey != modeAtSwap)
                    {
                        return;
                    }

                    // Visual "lágrima": salpicaduras en las celdas por donde entran las gemas
                    int nowMs = Environment.TickCount;
                    int[] splashCols = (res.MatchedColumns != null && res.MatchedColumns.Count > 0)
                        ? res.MatchedColumns.ToArray() : new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                    for (int k = 0; k < teardropCount; k++)
                    {
                        int sCol = splashCols[k % splashCols.Length];
                        int sRow = (k < splashCols.Length) ? 0 : ((k / splashCols.Length) % 8);
                        _teardrops.Add(new TeardropSplash { Col = sCol, Row = sRow, StartMs = nowMs + k * 35 });
                    }
                    if (_teardrops.Count > 200) _teardrops.RemoveRange(0, _teardrops.Count - 200);

                    _progress.TotalGemsCleared += res.TotalGemsDestroyed;
                    _progress.TotalFlameGemsDestroyed += res.FlameDestroyed;
                    _progress.TotalStarGemsDestroyed += res.StarDestroyed;
                    _progress.TotalHypercubesDestroyed += res.HypercubeDestroyed;
                    _questGemsCleared += res.TotalGemsDestroyed;

                    if (_currentModeKey == "ModeLightning")
                    {
                        // Play speedmatch sound scaling with cascade depth (speedmatch1 to speedmatch9)
                        int speedIdx = Math.Min(9, Math.Max(1, res.CascadeDepth));
                        _sound.PlaySound(AudioMap.SpeedMatchPrefix + speedIdx);

                        if (res.CascadeDepth >= 4 || res.TotalGemsDestroyed >= 10)
                        {
                            _sound.PlaySound(AudioMap.VoiceBlazingspeed);
                        }
                    }
                    bool isPokerActive = _currentModeKey == "ModePoker" || (_currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.Poker);
                    bool isButterfliesActive = isButterfliesMode;
                    bool isIceStormActive = _currentModeKey == "ModeIceStorm" || (_currentModeKey == "ModeQuest" && _activeQuest != null && _activeQuest.Type == Engine.QuestType.IceStorm);
                    bool isDiamondMineActive = _currentModeKey == "ModeDiamondMine" || (_currentModeKey == "ModeQuest" && _activeQuest != null && (_activeQuest.Type == Engine.QuestType.DiamondMine || _activeQuest.Type == Engine.QuestType.GoldRush));

                    if (_currentModeKey == "ModeQuest" && _activeQuest != null)
                    {
                        // Track real mission progress
                        if (isButterfliesActive && res.ButterfliesFreed > 0)
                            _questButterfliesFreed += res.ButterfliesFreed;
                        if (res.GoldTilesConverted > 0)
                            _questGoldConverted += res.GoldTilesConverted;
                        if (res.BombsDestroyed > 0)
                            _questBombsDestroyed += res.BombsDestroyed;
                        if (res.NuggetsMined > 0)
                            _questNuggets += res.NuggetsMined;
                        if (res.CascadeDepth > _questMaxCascade)
                            _questMaxCascade = res.CascadeDepth;

                        switch (_activeQuest.Type)
                        {
                            case Engine.QuestType.Butterflies:
                                _sound.PlaySound(AudioMap.ButterflyAppear);
                                break;
                            case Engine.QuestType.Alchemy:
                                if (res.GoldTilesConverted > 0)
                                {
                                    _sound.PlaySound(AudioMap.AlchemyConvert);
                                    _speech.Speak(Localization.Get("GoldConvertedAnnounce", res.GoldTilesConverted), true);
                                }
                                break;
                            case Engine.QuestType.GoldRush:
                                if (res.NuggetsMined > 0)
                                {
                                    _sound.PlaySound(AudioMap.DiamondMineTreasurefind);
                                    _sound.PlaySound(AudioMap.SandstormTreasureReveal);
                                    _speech.Speak(Localization.Get("NuggetFound"), true);
                                }
                                break;
                            case Engine.QuestType.TimeBomb:
                                if (res.BombsDestroyed > 0)
                                {
                                    _sound.PlaySound(AudioMap.GemCountdownDestroyed);
                                    _sound.PlaySound(AudioMap.SkullBusted);
                                }
                                break;
                        }

                        // Mission completion per authentic objective
                        bool questCompleted = false;
                        switch (_activeQuest.Type)
                        {
                            case Engine.QuestType.Butterflies:
                                questCompleted = _questButterfliesFreed >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.GoldRush:
                                questCompleted = _questNuggets >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.Alchemy:
                                questCompleted = _questGoldConverted >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.TimeBomb:
                                questCompleted = _questBombsDestroyed >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.Avalanche:
                                questCompleted = _questMaxCascade >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.Poker:
                                questCompleted = _questHandsScored >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.IceStorm:
                                questCompleted = _questIceColumnsBroken >= _activeQuest.Objective;
                                break;
                            case Engine.QuestType.DiamondMine:
                                questCompleted = _diamondDepthMeters >= _activeQuest.Objective;
                                break;
                        }
                        if (questCompleted)
                        {
                            int relicDoneBefore = _progress.CountCompletedInRelic(_activeQuest.RelicIndex);
                            int relicsBefore = _progress.QuestRelicCount;
                            _progress.CompleteQuestMission(_activeQuestIndex);
                            int relicDone = _progress.CountCompletedInRelic(_activeQuest.RelicIndex);
                            if (relicDone == 4 && relicDoneBefore < 4)
                            {
                                _progress.QuestRelicCount++;
                            }
                            _profileMgr.Save();

                            // Heroes Welcome: the elite badge for restoring
                            // all five relicaries (100% of Quest).
                            bool allQuestsComplete = true;
                            for (int m = 0; m < 40; m++)
                            {
                                if (!_progress.IsQuestMissionComplete(m))
                                {
                                    allQuestsComplete = false;
                                    break;
                                }
                            }
                            if (allQuestsComplete)
                                AwardBadge("BadgeHeroes", BadgeTier.Platinum);

                            _sound.PlaySound(AudioMap.VoiceChallengecomplete);
                            _sound.PlaySound(AudioMap.QuestAwardWreath);
                            _sound.PlaySound(AudioMap.QuestMenuRelicCompleteObject);
                            _sound.PlaySound(AudioMap.QuestMenuRelicCompleteRumble);

                            _screen = GameScreen.QuestRelicScreen;
                            _relicIdx = _activeQuest.RelicIndex;
                            _sound.PlayMusic(MusicMap.FileName(MusicMap.QuestTheme));

                            // Authentic unlock: only the very first relicary
                            // (relic count going 0 -> 1) opens Diamond Mine.
                            // Once unlocked, later relicaries must NOT repeat
                            // the unlock announcement.
                            bool mineJustUnlocked = relicsBefore == 0 && _progress.QuestRelicCount >= 1;
                            if (mineJustUnlocked)
                            {
                                _sound.PlaySound(AudioMap.Secretunlocked);
                            }

                            string questAnnounce = mineJustUnlocked
                                ? Localization.Get("UnlockDiamondMine") + " " + Localization.Get("QuestCompleteAnnounce", _activeQuestName)
                                : Localization.Get("QuestCompleteAnnounce", _activeQuestName);
                            _speech.Speak(questAnnounce, true);
                            return;
                        }
                    }

                    if (isIceStormActive)
                    {
                        // Authentic rule: only the columns where the match happened melt.
                        // A vertical match shatters the whole ice column; horizontal and
                        // special-gem blasts only push the front down a bit. Melted top
                        // columns disarm their rising internal (skull) column.
                        List<int> skullsDisarmed = new List<int>();
                        int meltedThisMove = 0;
                        foreach (int col in res.MatchedColumns)
                        {
                            if (col < 0 || col >= 8 || _iceColumns[col] <= 0) continue;
                            bool hadSkull = _iceSkullTicks[col] > 0;
                            bool shattered = res.VerticalMatchedColumns.Contains(col) || res.HypercubeTriggered || res.HypercubeCreated > 0;
                            if (shattered)
                            {
                                _iceColumns[col] = 0;
                                _iceSkullTicks[col] = 0;
                                _questIceColumnsBroken++;
                                meltedThisMove++;
                                if (hadSkull) skullsDisarmed.Add(col);
                                _sound.PlaySound(AudioMap.IceColumnBreak);
                            }
                            else
                            {
                                _iceColumns[col] = Math.Max(0, _iceColumns[col] - 2);
                                if (_iceColumns[col] < 8) _iceSkullTicks[col] = 0;
                                if (hadSkull && _iceSkullTicks[col] == 0) skullsDisarmed.Add(col);
                                if (_iceColumns[col] == 0)
                                {
                                    _questIceColumnsBroken++;
                                    meltedThisMove++;
                                }
                                _sound.PlaySound(AudioMap.IceStormColumnCombo);
                                // Mismo esquema que las cadenas: el combo de columna y
                                // luego el gem fall (variante corta) de la caida, sin
                                // espera larga para no retrasar el juego.
                                _sound.PlaySoundSpatial(AudioMap.GemFall, col, _cursorY);
                                await Task.Delay(80);
                            }
                        }
                        // Ice Breaker badge: 5/8/12/15 column combos in one move
                        if (_currentModeKey == "ModeIceStorm")
                        {
                            if (meltedThisMove >= 15) AwardBadge("BadgeIceBreaker", BadgeTier.Platinum);
                            else if (meltedThisMove >= 12) AwardBadge("BadgeIceBreaker", BadgeTier.Gold);
                            else if (meltedThisMove >= 8) AwardBadge("BadgeIceBreaker", BadgeTier.Silver);
                            else if (meltedThisMove >= 5) AwardBadge("BadgeIceBreaker", BadgeTier.Bronze);
                        }
                        if (skullsDisarmed.Count > 0)
                        {
                            _speech.Speak(Localization.Get("IceSkullResolved", FormatColumns(skullsDisarmed)), false);
                        }
                    }

                    if (isButterfliesActive)
                    {
                        if (res.ButterfliesFreed > 0)
                        {
                            // Butterfly Bonanza badge: 4/6/8/10 butterflies in a single move
                            if (_currentModeKey == "ModeButterflies")
                            {
                                if (res.ButterfliesFreed >= 10) AwardBadge("BadgeButterflyBonanza", BadgeTier.Platinum);
                                else if (res.ButterfliesFreed >= 8) AwardBadge("BadgeButterflyBonanza", BadgeTier.Gold);
                                else if (res.ButterfliesFreed >= 6) AwardBadge("BadgeButterflyBonanza", BadgeTier.Silver);
                                else if (res.ButterfliesFreed >= 4) AwardBadge("BadgeButterflyBonanza", BadgeTier.Bronze);
                            }

                            _sound.PlaySoundSpatial(AudioMap.Butterflyescape, _cursorX, _cursorY);
                            _speech.Speak(Localization.Get("ButterflyFreed", res.ButterfliesFreed), true);
                        }

                        // Original rule: every match moves all butterflies up one row
                        _board.MoveButterfliesUp();

                        // Butterflies stream in from the bottom to replace the ones
                        // freed this turn, so the board never runs out (authentic
                        // Butterflies / Quest rule: freedom targets stay reachable).
                        {
                            int poolTarget = 6;
                            int guard = 0;
                            while (_board.GetButterflyCount() < poolTarget && guard < 12)
                            {
                                _board.SpawnButterflyAtBottom();
                                guard++;
                            }
                        }

                        // Check if a butterfly reached top row 0 (caught by spider)
                        if (_board.IsButterflyAtTop())
                        {
                            _lightningTimer.Stop();
                            _screen = GameScreen.GameOver;
                            // The spider strikes at the far top of the board
                            List<int> bfCols = _board.GetButterflyColumns();
                            int deathCol = (bfCols.Count > 0) ? bfCols[0] : _cursorX;
                            _sound.PlaySoundSpatial(AudioMap.ButterflyDeath1, deathCol, 0);
                            _sound.PlaySound(AudioMap.VoiceGameover);
                            if (_currentModeKey == "ModeButterflies" && _score > _progress.ButterfliesHighScore)
                            {
                                _progress.ButterfliesHighScore = _score;
                                _sound.PlaySound(AudioMap.Rankup);
                                _profileMgr.Save();
                            }
                            CheckSecretRecordsBadge();
                            _speech.Speak(Localization.Get("ButterflyCaught") + Localization.Get("GameOver", _score), true);
                            return;
                        }

                        // Warn when a butterfly is one move away from the spider
                        if (_board.IsButterflyInDanger())
                        {
                            // Sound the alarm from the column where the
                            // butterfly sits (row 1, right under the spider)
                            List<int> dangerCols = _board.GetButterflyDangerColumns();
                            int warnCol = (dangerCols.Count > 0) ? dangerCols[0] : _cursorX;
                            _sound.PlaySoundSpatial(AudioMap.ButterflyAppear, warnCol, 1);
                            _speech.Speak(Localization.Get("ButterflyDanger", FormatColumns(_board.GetButterflyDangerColumns())), true);
                        }
                    }

                    if (isPokerActive)
                    {
                        _sound.PlaySound(AudioMap.Carddeal);
                        foreach (var color in res.MatchedColors)
                        {
                            _pokerCards.Add(color);
                            _sound.PlaySound(AudioMap.Cardflip);
                        }

                        // Authentic: special gems destroyed in the cascade boost
                        // the hand being dealt (+100 per Flame, +250 per Star).
                        _pokerHandBonus += res.FlameDestroyed * 100 + res.StarDestroyed * 250;

                        if (_pokerCards.Count >= 5)
                        {
                            PokerHandType hand = PokerHandEvaluator.Evaluate(_pokerCards);
                            int handPts = PokerHandEvaluator.GetHandPoints(hand) + _pokerHandBonus;
                            // Authentic: only a High Card (worth nothing) drops a
                            // skull; every real hand scores.
                            bool isBadHand = hand == PokerHandType.HighCard;

                            if (isBadHand)
                            {
                                // Authentic: bad hands drop a skull on the table
                                _pokerSkulls++;
                                _sound.PlaySound(AudioMap.SkullcoinFlip);
                                _sound.PlaySound(AudioMap.Skullcoinlose);
                                _sound.PlaySound(AudioMap.SkullAppear);
                                _sound.PlaySound(AudioMap.Pokerchips);
                                _pokerCards.Clear();
                                _pokerHandBonus = 0;

                                if (_pokerSkulls >= 5)
                                {
                                    _lightningTimer.Stop();
                                    _screen = GameScreen.GameOver;
                                    _sound.PlaySound(AudioMap.SkullBuster);
                                    _sound.PlaySound(AudioMap.VoiceGameover);
                                    if (_currentModeKey == "ModePoker" && _score > _progress.PokerHighScore)
                                    {
                                        _progress.PokerHighScore = _score;
                                        _sound.PlaySound(AudioMap.Rankup);
                                        _profileMgr.Save();
                                    }
                                    CheckSecretRecordsBadge();
                                    _speech.Speak(Localization.Get("PokerSkullGameOver") + " " + Localization.Get("GameOver", _score), true);
                                    return;
                                }
                                _speech.Speak(Localization.Get("PokerSkullAnnounce", _pokerSkulls), true);
                            }
                            else
                            {
                                _score += handPts;
                                _questHandsScored++;

                                if (hand == PokerHandType.Flush)
                                {
                                    // The Gambler badge only counts Poker-mode flushes
                                    if (_currentModeKey == "ModePoker") _progress.TotalFlushes++;
                                    _sound.PlaySound(AudioMap.PokerFlush);
                                    _sound.PlaySound(AudioMap.Skullcoinwin);
                                }
                                else if (hand == PokerHandType.FullHouse)
                                {
                                    _sound.PlaySound(AudioMap.PokerFullhouse);
                                    _sound.PlaySound(AudioMap.Skullcoinlands);
                                }
                                else if (hand == PokerHandType.FourOfAKind)
                                {
                                    _sound.PlaySound(AudioMap.Poker4ofakind);
                                    _sound.PlaySound(AudioMap.Skullcoinwin);
                                }
                                else
                                {
                                    _sound.PlaySound(AudioMap.Pokerscore);
                                    _sound.PlaySound(AudioMap.SkullBuster);
                                }

                                _sound.PlaySound(AudioMap.Pokerchips);
                                _speech.Speak(Localization.Get("PokerHandScored", Localization.GetPokerHandName(hand), handPts), true);
                                _pokerCards.Clear();
                                _pokerHandBonus = 0;

                                // Skull Eliminator (juego original): cada mano buena
                                // llena la barra segun su valor; al 100% elimina una
                                // calavera. El Color (Flush) la elimina al instante.
                                if (hand == PokerHandType.Flush)
                                {
                                    if (_pokerSkulls > 0)
                                    {
                                        _pokerSkulls--;
                                        _pokerSkullCharge = 0;
                                        _sound.PlaySound(AudioMap.SkullBuster);
                                        _speech.Speak(Localization.Get("PokerSkullEliminated", _pokerSkulls), true);
                                    }
                                }
                                else
                                {
                                    int fill = PokerHandEvaluator.GetSkullEliminatorFill(hand);
                                    if (_pokerSkulls > 0)
                                    {
                                        _pokerSkullCharge += fill;
                                        if (_pokerSkullCharge >= PokerHandEvaluator.SkullEliminatorMax)
                                        {
                                            _pokerSkullCharge -= PokerHandEvaluator.SkullEliminatorMax;
                                            _pokerSkulls--;
                                            _sound.PlaySound(AudioMap.SkullBuster);
                                            _speech.Speak(Localization.Get("PokerSkullEliminated", _pokerSkulls), true);
                                        }
                                    }
                                    else
                                    {
                                        _pokerSkullCharge = 0;
                                    }
                                }
                            }
                        }
                    }

                    if (isDiamondMineActive)
                    {
                        if (res.NuggetsMined > 0)
                        {
                            _sound.PlaySound(AudioMap.DiamondMineTreasurefind);
                            _speech.Speak(Localization.Get("NuggetFound"), true);
                        }
                        else if (res.RockCleared > 0)
                        {
                            _sound.PlaySound(AudioMap.DiamondMineStoneCracked);
                            _sound.PlaySound(AudioMap.DiamondMineDigLineHit);
                        }
                        else if (res.DirtCleared > 0)
                        {
                            _sound.PlaySound(AudioMap.DiamondMineDirtCracked);
                            _sound.PlaySound(AudioMap.DiamondMineDig);
                        }

                        // If all dirt is cleared in screen, shift down and add depth + time
                        if (!_board.HasDirtRemaining())
                        {
                            _diamondDepthMeters += 10;
                            _lightningTimeLeft += 30;
                            // Relic Hunter badge: 5/8/12/15 artifacts dug in Diamond Mine
                            if (_currentModeKey == "ModeDiamondMine")
                            {
                                _progress.TotalArtifactsCollected++;
                                if (_progress.TotalArtifactsCollected >= 15) AwardBadge("BadgeRelicHunter", BadgeTier.Platinum);
                                else if (_progress.TotalArtifactsCollected >= 12) AwardBadge("BadgeRelicHunter", BadgeTier.Gold);
                                else if (_progress.TotalArtifactsCollected >= 8) AwardBadge("BadgeRelicHunter", BadgeTier.Silver);
                                else if (_progress.TotalArtifactsCollected >= 5) AwardBadge("BadgeRelicHunter", BadgeTier.Bronze);
                            }
                            _sound.PlaySound(AudioMap.DiamondMineTreasurefind);
                            _sound.PlaySound(AudioMap.DiamondMineTreasurefindDiamonds);
                            _sound.PlaySound(AudioMap.DiamondMineArtifactShowcase);
                            _sound.PlaySound(AudioMap.DiamondMineDigNotify);
                            _speech.Speak(Localization.Get("ArtifactFound", _diamondDepthMeters), true);

                            _board.ShiftDiamondMineDown();
                        }
                    }

                    if (res.ExtraTimeSeconds > 0) _lightningTankSeconds += res.ExtraTimeSeconds;

                    // (La explosion/creacion de gemas especiales de la jugada suena
                    // DESPUES de la cadena de combos, en PlaySwapExplosions, para que
                    // el combo que la provoca vaya primero. Ver bloque de cascada.)

                    // Announce voice praise based on total gems destroyed or cascade depth across ALL modes.
                    // Never on a level-up match: "Level Complete!" and then "Good!" makes no sense.
                    if (!levelUpVoicePlayed)
                    {
                        if (res.TotalGemsDestroyed >= 25 || res.CascadeDepth >= 6)
                        { _sound.PlaySound(AudioMap.VoiceUnbelievable); }
                        else if (res.TotalGemsDestroyed >= 20 || res.CascadeDepth >= 5)
                        { _sound.PlaySound(AudioMap.VoiceExtraordinary); }
                        else if (res.TotalGemsDestroyed >= 15 || res.CascadeDepth >= 4)
                        { _sound.PlaySound(AudioMap.VoiceSpectacular); }
                        else if (res.TotalGemsDestroyed >= 12 || res.CascadeDepth >= 3)
                        { _sound.PlaySound(AudioMap.VoiceAwesome); }
                        else if (res.TotalGemsDestroyed >= 8)
                        { _sound.PlaySound(AudioMap.VoiceExcellent); }
                        else if (res.TotalGemsDestroyed >= 5)
                        { _sound.PlaySound(AudioMap.VoiceGood); }
                    }

                    CheckBadgesEvaluation(res);

                    string matchAnnounceText = res.CascadeDepth > 1
                        ? Localization.Get("CascadeAnnounce", res.CascadeDepth, res.TotalGemsDestroyed, _score)
                        : Localization.Get("MatchAnnounce", res.TotalGemsDestroyed, _score);

                    // Double match (T / L shape): official 50-per-match bonus
                    // announced on top of the regular score sheet.
                    if (res.DoubleMatchBonus > 0)
                    {
                        _sound.PlaySound(AudioMap.Doubleset);
                        matchAnnounceText += " " + Localization.Get("MultipleMatchAnnounce", res.SimultaneousMatches, res.DoubleMatchBonus * 50);
                    }

                    if (lightningSpeedBonus > 0)
                    {
                        matchAnnounceText += " " + Localization.Get("SpeedBonusAnnounce", lightningSpeedBonus);
                    }

                    // The score announcement goes to the screen reader (NVDA/SAPI),
                    // which is independent from the game voices: speak it right away
                    // so the reader never waits for the "Level Complete" jingle.
                    _speech.Speak(matchAnnounceText, true);

                    // Check if any valid moves remain, otherwise scramble board
                    MoveHint? validHint = HintFinder.FindValidMove(_board);
                    if (!validHint.HasValue)
                    {
                        _sound.PlaySound(AudioMap.VoiceNomoremoves);
                        if (_currentModeKey == "ModeClassic")
                        {
                            // Authentic Classic: only 3 reshuffles, then game over
                            if (_shufflesRemaining > 0)
                            {
                                _shufflesRemaining--;
                                _sound.PlaySound(AudioMap.Scramble);
                                int left = _shufflesRemaining;
                                _speech.Speak(Localization.Get("ShuffleAnnounce", left), true);
                                _board.InitializeBoard();
                            }
                            else
                            {
                                _screen = GameScreen.GameOver;
                                _sound.PlaySound(AudioMap.VoiceGameover);
                                _speech.Speak(Localization.Get("NoShufflesLeft") + " " + Localization.Get("GameOver", _score), true);
                            }
                        }
                        else
                        {
                            _sound.PlaySound(AudioMap.Scramble);
                            _speech.Speak(Localization.Get("NoMoreMovesScramble"), true);
                            if (isDiamondMineActive)
                            {
                                // Keep the mine alive: rebuild dirt/rock + fresh
                                // nuggets so the Gold Rush quest can keep going.
                                _board.InitializeDiamondMineBoard();
                            }
                            else
                            {
                                // Preserve mode elements across a scramble: Time Bomb
                                // boards keep a fresh bomb field, and Butterflies boards
                                // re-spawn their butterfly pool right away.
                                _board.InitializeBoard(isBombMode);
                                if (isButterfliesMode)
                                {
                                    int poolTarget = 6;
                                    int guard = 0;
                                    while (_board.GetButterflyCount() < poolTarget && guard < 12)
                                    {
                                        _board.SpawnButterflyAtBottom();
                                        guard++;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _cascadeChain = 0;
                }
            }
            else
                {
                    _cascadeChain = 0;
                    _sound.PlaySound(AudioMap.Badmove);
                    _speech.Speak(Localization.Get("InvalidMove"), true);
                }
            }
            finally
            {
                _isSwapping = false;
            }
        }

        private void AwardBadge(string key, BadgeTier tier)
        {
            if (_badgeMgr.SetTierIfHigher(key, tier))
            {
                _badgeMgr.Save(_profileMgr.CurrentProfile.ProfileName);
                _sound.PlaySound(AudioMap.Badgeawarded);
                _sound.PlaySound(AudioMap.Badgefall);
                string bName = Localization.Get(key);
                string tName = Localization.Get(string.Format("Tier{0}", tier.ToString()));
                _speech.Speak(Localization.Get("BadgeUnlockedAnnounce", bName, tName), true);
            }
        }

        private void CheckBadgesEvaluation(CascadeResult res)
        {
            // Blaster Badge (30, 40, 50, 60 gems)
            if (res.TotalGemsDestroyed >= 60) AwardBadge("BadgeBlaster", BadgeTier.Platinum);
            else if (res.TotalGemsDestroyed >= 50) AwardBadge("BadgeBlaster", BadgeTier.Gold);
            else if (res.TotalGemsDestroyed >= 40) AwardBadge("BadgeBlaster", BadgeTier.Silver);
            else if (res.TotalGemsDestroyed >= 30) AwardBadge("BadgeBlaster", BadgeTier.Bronze);

            // Bejeweler Badge (Classic score: 50k, 150k, 300k, 500k)
            if (_currentModeKey == "ModeClassic")
            {
                if (_score >= 500000) AwardBadge("BadgeBejeweler", BadgeTier.Platinum);
                else if (_score >= 300000) AwardBadge("BadgeBejeweler", BadgeTier.Gold);
                else if (_score >= 150000) AwardBadge("BadgeBejeweler", BadgeTier.Silver);
                else if (_score >= 50000) AwardBadge("BadgeBejeweler", BadgeTier.Bronze);

                if (_progress.ClassicLevel >= 10) AwardBadge("BadgeLevelord", BadgeTier.Platinum);
            }

            // High Voltage Badge (Lightning score: 100k, 300k, 500k, 750k)
            if (_currentModeKey == "ModeLightning")
            {
                if (_score >= 750000) AwardBadge("BadgeHighVoltage", BadgeTier.Platinum);
                else if (_score >= 500000) AwardBadge("BadgeHighVoltage", BadgeTier.Gold);
                else if (_score >= 300000) AwardBadge("BadgeHighVoltage", BadgeTier.Silver);
                else if (_score >= 100000) AwardBadge("BadgeHighVoltage", BadgeTier.Bronze);
            }

            // Final Frenzy Badge (score during a Last Hurrah: 20k, 30k, 40k, 60k)
            int frenzy = _progress.BestFrenzyScore;
            if (frenzy >= 60000) AwardBadge("BadgeFinalFrenzy", BadgeTier.Platinum);
            else if (frenzy >= 40000) AwardBadge("BadgeFinalFrenzy", BadgeTier.Gold);
            else if (frenzy >= 30000) AwardBadge("BadgeFinalFrenzy", BadgeTier.Silver);
            else if (frenzy >= 20000) AwardBadge("BadgeFinalFrenzy", BadgeTier.Bronze);

            // Score badges per secret mode: the record ever reached, or the
            // running score when playing that mode right now.
            int pokerScore = Math.Max(_progress.PokerHighScore, _currentModeKey == "ModePoker" ? _score : 0);
            if (pokerScore >= 750000) AwardBadge("BadgeAnteUp", BadgeTier.Platinum);
            else if (pokerScore >= 500000) AwardBadge("BadgeAnteUp", BadgeTier.Gold);
            else if (pokerScore >= 300000) AwardBadge("BadgeAnteUp", BadgeTier.Silver);
            else if (pokerScore >= 100000) AwardBadge("BadgeAnteUp", BadgeTier.Bronze);

            // The Gambler Badge (flushes in Poker: 10, 30, 60, 100)
            if (_progress.TotalFlushes >= 100) AwardBadge("BadgeGambler", BadgeTier.Platinum);
            else if (_progress.TotalFlushes >= 60) AwardBadge("BadgeGambler", BadgeTier.Gold);
            else if (_progress.TotalFlushes >= 30) AwardBadge("BadgeGambler", BadgeTier.Silver);
            else if (_progress.TotalFlushes >= 10) AwardBadge("BadgeGambler", BadgeTier.Bronze);

            // Glacial Explorer Badge (Ice Storm score: 100k, 300k, 500k, 750k)
            int iceScore = Math.Max(_progress.IceStormHighScore, _currentModeKey == "ModeIceStorm" ? _score : 0);
            if (iceScore >= 750000) AwardBadge("BadgeGlacialExplorer", BadgeTier.Platinum);
            else if (iceScore >= 500000) AwardBadge("BadgeGlacialExplorer", BadgeTier.Gold);
            else if (iceScore >= 300000) AwardBadge("BadgeGlacialExplorer", BadgeTier.Silver);
            else if (iceScore >= 100000) AwardBadge("BadgeGlacialExplorer", BadgeTier.Bronze);

            // Diamond, Mine Badge (Diamond Mine score: 100k, 300k, 500k, 750k)
            int mineScore = Math.Max(_progress.DiamondMineHighScore, _currentModeKey == "ModeDiamondMine" ? _score : 0);
            if (mineScore >= 750000) AwardBadge("BadgeDiamondMine", BadgeTier.Platinum);
            else if (mineScore >= 500000) AwardBadge("BadgeDiamondMine", BadgeTier.Gold);
            else if (mineScore >= 300000) AwardBadge("BadgeDiamondMine", BadgeTier.Silver);
            else if (mineScore >= 100000) AwardBadge("BadgeDiamondMine", BadgeTier.Bronze);

            // Butterfly Monarch Badge (Butterflies score: 100k, 300k, 500k, 750k)
            int butterflyScore = Math.Max(_progress.ButterfliesHighScore, _currentModeKey == "ModeButterflies" ? _score : 0);
            if (butterflyScore >= 750000) AwardBadge("BadgeButterflyMonarch", BadgeTier.Platinum);
            else if (butterflyScore >= 500000) AwardBadge("BadgeButterflyMonarch", BadgeTier.Gold);
            else if (butterflyScore >= 300000) AwardBadge("BadgeButterflyMonarch", BadgeTier.Silver);
            else if (butterflyScore >= 100000) AwardBadge("BadgeButterflyMonarch", BadgeTier.Bronze);

            // Inferno Badge (lifetime flame gems: 50, 350, 1000, 2000)
            int flame = _progress.TotalFlameGemsDestroyed + res.FlameDestroyed;
            if (flame >= 2000) AwardBadge("BadgeInferno", BadgeTier.Platinum);
            else if (flame >= 1000) AwardBadge("BadgeInferno", BadgeTier.Gold);
            else if (flame >= 350) AwardBadge("BadgeInferno", BadgeTier.Silver);
            else if (flame >= 50) AwardBadge("BadgeInferno", BadgeTier.Bronze);

            // Stellar Badge (lifetime star gems: 25, 125, 400, 750)
            int stars = _progress.TotalStarGemsDestroyed + res.StarDestroyed;
            if (stars >= 750) AwardBadge("BadgeStellar", BadgeTier.Platinum);
            else if (stars >= 400) AwardBadge("BadgeStellar", BadgeTier.Gold);
            else if (stars >= 125) AwardBadge("BadgeStellar", BadgeTier.Silver);
            else if (stars >= 25) AwardBadge("BadgeStellar", BadgeTier.Bronze);

            // Chromatic Badge (lifetime hypercubes: 25, 125, 400, 750)
            int hypers = _progress.TotalHypercubesDestroyed + res.HypercubeDestroyed;
            if (hypers >= 750) AwardBadge("BadgeChromatic", BadgeTier.Platinum);
            else if (hypers >= 400) AwardBadge("BadgeChromatic", BadgeTier.Gold);
            else if (hypers >= 125) AwardBadge("BadgeChromatic", BadgeTier.Silver);
            else if (hypers >= 25) AwardBadge("BadgeChromatic", BadgeTier.Bronze);

            // Annihilator Badge (destroy the whole board with a hypercube swap)
            if (res.AnnihilatorUsed) AwardBadge("BadgeAnnihilator", BadgeTier.Platinum);
        }

        // Top Secret Badge: beat the high score of all four secret modes
        private void CheckSecretRecordsBadge()
        {
            if (_progress.PokerHighScore > 0 && _progress.ButterfliesHighScore > 0 &&
                _progress.IceStormHighScore > 0 && _progress.DiamondMineHighScore > 0)
            {
                AwardBadge("BadgeTopSecret", BadgeTier.Platinum);
            }
        }

        private string FormatColumns(List<int> columns)
        {
            List<string> letters = new List<string>();
            foreach (int c in columns)
                letters.Add(((char)('A' + c)).ToString());
            return string.Join(", ", letters.ToArray());
        }

        // Full status of the mode that is being played (press Q): Quest shows
        // exact mission progress; every other mode announces its own state.
        private void AnnounceFullModeStatus()
        {
            if (_currentModeKey == "ModeQuest" && _activeQuest != null)
            {
                System.Text.StringBuilder status = new System.Text.StringBuilder();
                status.Append(Localization.Get("QuestStatusTitle", _activeQuestName));
                status.Append(Localization.Get("QuestStatusScore", _score));

                switch (_activeQuest.Type)
                {
                    case Engine.QuestType.Butterflies:
                        status.Append(Localization.Get("QuestStatusButterflies", Math.Min(_questButterfliesFreed, _activeQuest.Objective), _activeQuest.Objective));
                        break;
                    case Engine.QuestType.GoldRush:
                        status.Append(Localization.Get("QuestStatusNuggets", Math.Min(_questNuggets, _activeQuest.Objective), _activeQuest.Objective));
                        break;
                    case Engine.QuestType.Alchemy:
                        status.Append(Localization.Get("QuestStatusGoldTiles", Math.Min(_questGoldConverted, _activeQuest.Objective), _activeQuest.Objective));
                        break;
case Engine.QuestType.TimeBomb:
                        status.Append(Localization.Get("QuestStatusBombsDestroyed", Math.Min(_questBombsDestroyed, _activeQuest.Objective), _activeQuest.Objective));
                        {
                            var bombInfo = _board.GetBombInfo();
                            int activeBombs = bombInfo.Count;
                            int lowestTimer = 99;
                            foreach (var b in bombInfo) lowestTimer = Math.Min(lowestTimer, b.Item3);
                            status.Append(Localization.Get("QuestStatusBombs", activeBombs, lowestTimer == 99 ? 0 : lowestTimer));
                        }
                        break;
                    case Engine.QuestType.Avalanche:
                        status.Append(Localization.Get("QuestStatusCascade", Math.Min(_questMaxCascade, _activeQuest.Objective), _activeQuest.Objective));
                        break;
                    case Engine.QuestType.Poker:
                        status.Append(Localization.Get("QuestStatusPokerHands", Math.Min(_questHandsScored, _activeQuest.Objective), _activeQuest.Objective));
                        status.Append(Localization.Get("QuestStatusSkulls", _pokerSkulls));
                        break;
                    case Engine.QuestType.IceStorm:
                        status.Append(Localization.Get("QuestStatusIceColumns", Math.Min(_questIceColumnsBroken, _activeQuest.Objective), _activeQuest.Objective));
                        break;
                    case Engine.QuestType.DiamondMine:
                        status.Append(Localization.Get("QuestStatusDepth", Math.Min(_diamondDepthMeters, _activeQuest.Objective), _activeQuest.Objective));
                        break;
                }

                _speech.Speak(status.ToString(), true);
                return;
            }

            if (_currentModeKey == "ModeQuest")
            {
                _speech.Speak(Localization.Get("QuestStatusInactive"), true);
                return;
            }

            switch (_currentModeKey)
            {
                case "ModeClassic":
                    _speech.Speak(Localization.Get("ClassicStatus", _score, _level, _levelProgressPoints, GetLevelTargetPoints(_level), _shufflesRemaining), true);
                    break;
                case "ModeLightning":
                    _speech.Speak(Localization.Get("LightningScoreAnnouncement", _score, _lightningTimeLeft, _lightningMultiplier * 5), true);
                    break;
                case "ModeZen":
                    _speech.Speak(Localization.Get("ZenStatus", _score, _level, _levelProgressPoints, GetLevelTargetPoints(_level)), true);
                    break;
                case "ModePoker":
                    _speech.Speak(Localization.Get("PokerStatus", _score, _pokerCards.Count, _pokerSkulls, _pokerSkullCharge), true);
                    break;
                case "ModeButterflies":
                    _speech.Speak(Localization.Get("ButterfliesModeStatus", _score, _board.GetButterflyCount(), FormatColumns(_board.GetButterflyColumns())), true);
                    break;
                case "ModeIceStorm":
                    {
                        int melted = 0;
                        List<int> danger = new List<int>();
                        List<int> cresting = new List<int>();
                        for (int c = 0; c < 8; c++)
                        {
                            if (_iceColumns[c] == 0) melted++;
                            if (_iceColumns[c] >= 7 && _iceColumns[c] < 8) danger.Add(c);
                            if (_iceColumns[c] >= 8 && _iceSkullTicks[c] > 0) cresting.Add(c);
                        }
                        string suffix = "";
                        if (cresting.Count > 0) suffix += Localization.Get("IceSkullSuffix", FormatColumns(cresting));
                        if (danger.Count > 0) suffix += Localization.Get("IceDangerSuffix", FormatColumns(danger));
                        _speech.Speak(Localization.Get("IceStormModeStatus", _score, melted, suffix).TrimEnd(), true);
                    }
                    break;
                case "ModeDiamondMine":
                    _speech.Speak(Localization.Get("DiamondMineStatus", _score, _diamondDepthMeters, _lightningTimeLeft), true);
                    break;
                default:
                    _speech.Speak(Localization.Get("ScoreAnnouncement", _score, _level), true);
                    break;
            }
        }

        private void AnnounceCurrentCell()
        {
            Gem g = _board.GetGem(_cursorX, _cursorY);
            string colLetter = ((char)('A' + _cursorX)).ToString();
            int rowNum = _cursorY + 1;

            string text;
            if (g != null)
                text = string.Format("{0}{1}: {2}", colLetter, rowNum, g.GetNameLocalized());
            else
                text = string.Format("{0}{1}: {2}", colLetter, rowNum, Localization.Get("Empty"));

            // Board accessibility: announce where this gem can be swapped,
            // like visual games highlight the swappable gems
            List<KeyValuePair<int, int>> moves = HintFinder.GetValidMovesFrom(_board, _cursorX, _cursorY);
            if (moves.Count > 0)
            {
                List<string> dirs = new List<string>();
                foreach (KeyValuePair<int, int> m in moves)
                {
                    if (m.Key == 1) dirs.Add(Localization.Get("DirRight"));
                    else if (m.Key == -1) dirs.Add(Localization.Get("DirLeft"));
                    else if (m.Value == 1) dirs.Add(Localization.Get("DirDown"));
                    else if (m.Value == -1) dirs.Add(Localization.Get("DirUp"));
                }
                text += ". " + Localization.Get("MoveHint", string.Join(Localization.Get("DirOr"), dirs.ToArray()));
            }

            _speech.Speak(text, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(Color.FromArgb(20, 20, 35));

            if (_screen == GameScreen.Loading) DrawLoading(g);
            else if (_screen == GameScreen.ProfileInput) DrawProfileInput(g);
            else if (_screen == GameScreen.MainMenu || _screen == GameScreen.GameSelect || _screen == GameScreen.Options || _screen == GameScreen.BadgesScreen || _screen == GameScreen.RecordsScreen || _screen == GameScreen.TutorialScreen || _screen == GameScreen.QuestRelicScreen || _screen == GameScreen.QuestChallengeScreen || _screen == GameScreen.ProfileSelectScreen || _screen == GameScreen.ZenOptionsScreen || _screen == GameScreen.PauseMenu || _screen == GameScreen.GameOver || _screen == GameScreen.AudioSchool) DrawMenu(g);
            else if (_screen == GameScreen.Playing) DrawBoard(g);
        }

        private void DrawProfileInput(Graphics g)
        {
            using (Font titleFont = new Font("Segoe UI", 26, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 16))
            {
                g.DrawString(Localization.Get("CreateProfileTitle"), titleFont, Brushes.Cyan, 180, 150);
                g.DrawString(Localization.Get("EnterNamePrompt"), subFont, Brushes.White, 180, 250);

                Rectangle box = new Rectangle(180, 320, 450, 45);
                g.DrawRectangle(Pens.Gold, box);
                g.DrawString(_profileInputBuffer + "_", subFont, Brushes.Yellow, 190, 328);
            }
        }

        private void DrawLoading(Graphics g)
        {
            if (_heatwaveLogo != null)
            {
                int lw = 150, lh = 150;
                g.DrawImage(_heatwaveLogo, (900 - lw) / 2, 15, lw, lh);
            }

            using (Font titleFont = new Font("Segoe UI", 28, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 16))
            {
                g.DrawString("BEJEWELED 3 ACCESIBLE", titleFont, Brushes.Cyan, 200, 150);
                g.DrawString(Localization.Get("LoadingTitle"), subFont, Brushes.Gold, 280, 250);

                Rectangle barOutline = new Rectangle(200, 320, 500, 30);
                g.DrawRectangle(Pens.White, barOutline);

                Rectangle barFill = new Rectangle(202, 322, (int)(496 * (_loadingProgress / 100.0)), 26);
                g.FillRectangle(Brushes.DeepSkyBlue, barFill);

                g.DrawString(string.Format("{0}%", _loadingProgress), subFont, Brushes.White, 430, 360);

                if (_loadingComplete)
                {
                    g.DrawString(Localization.Get("LoadingPrompt"), subFont, Brushes.Lime, 220, 430);
                }
            }
        }

        private void DrawMenu(Graphics g)
        {
            using (Font titleFont = new Font("Segoe UI", 30, FontStyle.Bold))
            using (Font menuFont = new Font("Segoe UI", 20))
            {
                string headerTitle = (_screen == GameScreen.GameOver)
                    ? Localization.Get("GameOver", _score)
                    : Localization.Get("AppTitle");
                g.DrawString(headerTitle, titleFont, (_screen == GameScreen.GameOver ? Brushes.Gold : Brushes.Cyan), 180, 80);

                string[] items;
                int currentIdx = _menuIdx;

                if (_screen == GameScreen.MainMenu)
                {
                    items = GetMainMenuItems();
                }
                else if (_screen == GameScreen.Options)
                {
                    items = GetOptionsMenuItems();
                    currentIdx = _optionsIdx;
                }
                else if (_screen == GameScreen.BadgesScreen)
                {
                    items = GetBadgeListItems();
                    currentIdx = _badgeIdx;
                }
                else if (_screen == GameScreen.RecordsScreen)
                {
                    items = GetRecordsItems();
                    currentIdx = _recordsIdx;
                }
                else if (_screen == GameScreen.TutorialScreen)
                {
                    items = GetTutorialItems();
                    currentIdx = _tutorialIdx;
                }
                else if (_screen == GameScreen.QuestRelicScreen)
                {
                    items = GetQuestRelicItems();
                    currentIdx = _relicIdx;
                }
                else if (_screen == GameScreen.QuestChallengeScreen)
                {
                    items = GetQuestChallengeItems();
                    currentIdx = _questChallengeIdx;
                }
                else if (_screen == GameScreen.ProfileSelectScreen)
                {
                    items = GetProfileSelectItems();
                    currentIdx = _profileSelectIdx;
                }
                else if (_screen == GameScreen.ZenOptionsScreen)
                {
                    items = GetZenOptionsMenuItems();
                    currentIdx = _zenOptionsIdx;
                }
                else if (_screen == GameScreen.PauseMenu)
                {
                    items = GetPauseMenuItems();
                    currentIdx = _pauseIdx;
                }
                else if (_screen == GameScreen.GameOver)
                {
                    items = GetGameOverItems();
                    currentIdx = _gameOverIdx;
                }
                else
                {
                    string[] keys = GetGameModeKeys();
                    items = new string[keys.Length];
                    for (int i = 0; i < keys.Length; i++) items[i] = Localization.Get(keys[i]);
                    currentIdx = _gameModeIdx;
                }

                for (int i = 0; i < items.Length; i++)
                {
                    Brush b = (i == currentIdx) ? Brushes.Yellow : Brushes.White;
                    string prefix = (i == currentIdx) ? "> " : "  ";
                    g.DrawString(prefix + items[i], menuFont, b, 200, 220 + (i * 45));
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _lightningTimer.Stop();
                _lightningTimer.Dispose();
                _loadingTimer.Stop();
                _loadingTimer.Dispose();
                _renderTimer.Stop();
                _renderTimer.Dispose();
                if (_zenMgr != null) { _zenMgr.StopZenSession(); _zenMgr.Dispose(); }
                SaveOptionsState();
                if (_profileMgr.CurrentProfile != null) _profileMgr.Save();
                if (_sound != null)
                {
                    _sound.MusicRechained -= Sound_MusicRechained;
                    _sound.Dispose();
                }
                if (_speech != null) _speech.Dispose();
                DisposeVisualAssets();
            }
            catch
            {
                // best-effort cleanup before the window closes
            }
            base.OnFormClosing(e);
        }

        private void DisposeVisualAssets()
        {
            if (_gemFrames != null)
            {
                foreach (var frames in _gemFrames)
                {
                    if (frames != null)
                    {
                        foreach (var b in frames) { if (b != null) b.Dispose(); }
                    }
                }
                _gemFrames = null;
            }
            if (_gemShadows != null)
            {
                foreach (var b in _gemShadows) { if (b != null) b.Dispose(); }
                _gemShadows = null;
            }
            if (_heatwaveLogo != null) { _heatwaveLogo.Dispose(); _heatwaveLogo = null; }
        }

        private void LoadVisualAssets(string baseDir)
        {
            try
            {
                string resDir = PickGemResolutionFolder(baseDir, 60);
                if (resDir == null) return;

                string normalDir = Path.Combine(resDir, "GemsNormal");
                string shadowDir = Path.Combine(resDir, "GemsShadow");

                string[] gemNames = Enum.GetNames(typeof(GemColor));
                _gemFrames = new Bitmap[gemNames.Length][];
                _gemShadows = new Bitmap[gemNames.Length];
                for (int i = 0; i < gemNames.Length; i++)
                {
                    string name = gemNames[i];
                    GemColor col = (GemColor)i;
                    Color tint = _gemColors.ContainsKey(col) ? _gemColors[col] : Color.White;
                    _gemFrames[i] = LoadGemFrames(Path.Combine(normalDir, name + ".png"), 54, tint);
                    _gemShadows[i] = LoadGemFrame(Path.Combine(shadowDir, name + ".png"), 58);
                }

                string heatwavePath = Path.Combine(resDir, "..", "NonResize", "heatwave.png");
                if (File.Exists(heatwavePath))
                {
                    _heatwaveLogo = new Bitmap(heatwavePath);
                }
            }
            catch { }
        }

        // Carga los 20 fotogramas animados de la gema (hoja 5x4) y aplica el tinte cromático
        // para que cada gema tenga su color vibrante y resplandor original.
        private static Bitmap[] LoadGemFrames(string path, int maxSize, Color tint)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using (Bitmap sheet = new Bitmap(path))
                {
                    int pitchW = Math.Max(1, sheet.Width / 5);
                    int pitchH = Math.Max(1, sheet.Height / 4);
                    Rectangle bounds = GetContentBounds(sheet, 0, 0, pitchW, pitchH);
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                        bounds = new Rectangle(0, 0, pitchW, pitchH);

                    Bitmap[] frames = new Bitmap[20];
                    for (int row = 0; row < 4; row++)
                    {
                        for (int col = 0; col < 5; col++)
                        {
                            int idx = row * 5 + col;
                            int srcX = col * pitchW + bounds.X;
                            int srcY = row * pitchH + bounds.Y;
                            if (srcX + bounds.Width <= sheet.Width && srcY + bounds.Height <= sheet.Height)
                            {
                                Rectangle frameRect = new Rectangle(srcX, srcY, bounds.Width, bounds.Height);
                                using (Bitmap sprite = sheet.Clone(frameRect, sheet.PixelFormat))
                                using (Bitmap scaled = ScaleToFit(sprite, maxSize, maxSize))
                                {
                                    frames[idx] = ApplyGemColorTint(scaled, tint);
                                }
                            }
                            else
                            {
                                frames[idx] = frames[0] != null ? new Bitmap(frames[0]) : null;
                            }
                        }
                    }
                    return frames;
                }
            }
            catch { return null; }
        }

        // Tinte cromático con realce de contraste: intensifica el color de la gema manteniendo
        // los brillos especulares blancos y los bordes tallados.
        private static Bitmap ApplyGemColorTint(Bitmap src, Color tint)
        {
            if (src == null) return null;
            Bitmap dst = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            float tr = tint.R / 255.0f;
            float tg = tint.G / 255.0f;
            float tb = tint.B / 255.0f;

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color px = src.GetPixel(x, y);
                    if (px.A <= 5)
                    {
                        dst.SetPixel(x, y, Color.Transparent);
                        continue;
                    }

                    // Luminancia del pixel de la escala de grises original
                    float lum = (px.R * 0.299f + px.G * 0.587f + px.B * 0.114f) / 255.0f;

                    // Mezcla aditiva y multiplicativa: color base enriquecido con brillo de facetas
                    float r = Math.Min(255f, (lum * tr * 1.6f + (float)Math.Pow(lum, 2.5) * 0.4f) * 255f);
                    float g = Math.Min(255f, (lum * tg * 1.6f + (float)Math.Pow(lum, 2.5) * 0.4f) * 255f);
                    float b = Math.Min(255f, (lum * tb * 1.6f + (float)Math.Pow(lum, 2.5) * 0.4f) * 255f);

                    dst.SetPixel(x, y, Color.FromArgb(px.A, (int)r, (int)g, (int)b));
                }
            }
            return dst;
        }

        private static Bitmap LoadGemFrame(string path, int maxSize)
        {
            if (!File.Exists(path)) return null;
            using (Bitmap sheet = new Bitmap(path))
            {
                int pitchW = Math.Max(1, sheet.Width / 5);
                int pitchH = Math.Max(1, sheet.Height / 4);
                Rectangle bounds = GetContentBounds(sheet, 0, 0, pitchW, pitchH);
                if (bounds.Width <= 0 || bounds.Height <= 0) return null;
                using (Bitmap sprite = sheet.Clone(bounds, sheet.PixelFormat))
                {
                    return ScaleToFit(sprite, maxSize, maxSize);
                }
            }
        }

        // Las carpetas 600/768/1200 son juegos de gemas para distintas resoluciones
        // de pantalla; se elige la que mas se aproxime al tamano de celda del tablero.
        private string PickGemResolutionFolder(string baseDir, int targetPitch)
        {
            string[] imagesCandidates = new string[]
            {
                Path.Combine(baseDir, "sounds", "images"),
                Path.Combine(baseDir, "..", "sounds", "images"),
                Path.Combine(baseDir, "..", "..", "sounds", "images"),
                Path.Combine(Environment.CurrentDirectory, "sounds", "images"),
                Path.Combine(Environment.CurrentDirectory, "..", "sounds", "images")
            };
            string imagesRoot = null;
            foreach (string c in imagesCandidates)
            {
                if (Directory.Exists(c)) { imagesRoot = c; break; }
            }
            if (imagesRoot == null) return null;

            int[] resolutions = { 600, 768, 1200 };
            string best = null;
            int bestScore = int.MaxValue;
            foreach (int res in resolutions)
            {
                string dir = Path.Combine(imagesRoot, res.ToString());
                string probe = Path.Combine(dir, "GemsNormal", "Red.png");
                if (!File.Exists(probe)) continue;
                int pitch;
                using (Bitmap sheet = new Bitmap(probe))
                {
                    pitch = sheet.Width / 5;
                }
                int score = Math.Abs(pitch - targetPitch);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = dir;
                }
            }
            return best;
        }

        private static Rectangle GetContentBounds(Bitmap bmp, int x0, int y0, int w, int h)
        {
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    if (bmp.GetPixel(x, y).A > 8)
                    {
                        if (x - x0 < minX) minX = x - x0;
                        if (x - x0 > maxX) maxX = x - x0;
                        if (y - y0 < minY) minY = y - y0;
                        if (y - y0 > maxY) maxY = y - y0;
                    }
                }
            }
            if (maxX < 0) return Rectangle.Empty;
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Bitmap ScaleToFit(Bitmap src, int maxW, int maxH)
        {
            double scale = Math.Min((double)maxW / src.Width, (double)maxH / src.Height);
            if (scale >= 1.0) return new Bitmap(src);
            int w = Math.Max(1, (int)(src.Width * scale));
            int h = Math.Max(1, (int)(src.Height * scale));
            Bitmap dst = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, w, h);
            }
            return dst;
        }

        private class TeardropSplash
        {
            public int Col;
            public int Row;
            public int StartMs;
            public int DurationMs = 260;
        }
        private readonly List<TeardropSplash> _teardrops = new List<TeardropSplash>();

        private void DrawBoard(Graphics g)
        {
            int tileSize = 60;
            int startX = 200;
            int startY = 80;

            _gemAnimTick++;

            using (Font font = new Font("Segoe UI", 16, FontStyle.Bold))
            {
                g.DrawString(string.Format("Mode: {0}", Localization.Get(_currentModeKey)), font, Brushes.Cyan, 20, 20);
                g.DrawString(string.Format("Score: {0}", _score), font, Brushes.Gold, 300, 20);

                if (_currentModeKey == "ModeLightning")
                {
                    g.DrawString(string.Format("Time: {0}s", _lightningTimeLeft), font, Brushes.OrangeRed, 650, 20);
                }
                else
                {
                    g.DrawString(string.Format("Level: {0}", _level), font, Brushes.Lime, 650, 20);
                }
            }

            for (int y = 0; y < Board.Rows; y++)
            {
                for (int x = 0; x < Board.Cols; x++)
                {
                    Rectangle rect = new Rectangle(startX + (x * tileSize), startY + (y * tileSize), tileSize - 4, tileSize - 4);
                    Gem gem = _board.GetGem(x, y);

                    // Fondo de celda sutil para contraste nítido del tablero
                    using (Brush cellBg = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    {
                        g.FillRectangle(cellBg, rect);
                    }

                    if (gem != null)
                    {
                        Bitmap[] frames = (_gemFrames != null && (int)gem.Color < _gemFrames.Length) ? _gemFrames[(int)gem.Color] : null;
                        if (frames != null && frames.Length > 0)
                        {
                            // Animación sutil y fluida: desfasada por posición de celda (x, y) para brillo orgánico
                            int frameIdx = ((_gemAnimTick / 3) + (x * 3) + (y * 5)) % frames.Length;
                            Bitmap gemImg = frames[frameIdx] ?? frames[0];

                            Bitmap shadowImg = (_gemShadows != null && (int)gem.Color < _gemShadows.Length) ? _gemShadows[(int)gem.Color] : null;
                            if (shadowImg != null)
                            {
                                int sw = Math.Min(shadowImg.Width, rect.Width - 2);
                                int sh = Math.Min(shadowImg.Height, rect.Height - 2);
                                g.DrawImage(shadowImg, rect.X + (rect.Width - sw) / 2, rect.Y + (rect.Height - sh) / 2 + 2, sw, sh);
                            }
                            if (gemImg != null)
                            {
                                int dw = Math.Min(gemImg.Width, rect.Width - 2);
                                int dh = Math.Min(gemImg.Height, rect.Height - 2);
                                int dx = rect.X + (rect.Width - dw) / 2;
                                int dy = rect.Y + (rect.Height - dh) / 2;
                                g.DrawImage(gemImg, dx, dy, dw, dh);
                            }
                        }
                        else
                        {
                            Color c = _gemColors.ContainsKey(gem.Color) ? _gemColors[gem.Color] : Color.Gray;
                            using (Brush b = new SolidBrush(c))
                            {
                                g.FillEllipse(b, rect);
                            }
                        }
                        DrawSpecialOverlay(g, gem, rect);
                    }

                    if (_selectedGemX == x && _selectedGemY == y)
                    {
                        using (Pen p = new Pen(Color.Lime, 4))
                        {
                            g.DrawRectangle(p, rect);
                        }
                    }
                    else if (x == _cursorX && y == _cursorY)
                    {
                        using (Pen p = new Pen(Color.Yellow, 4))
                        {
                            g.DrawRectangle(p, rect);
                        }
                    }

                    // Flechas visuales para jugadores normovisuales que indican hacia dónde se puede mover la gema
                    if ((x == _cursorX && y == _cursorY) || (_selectedGemX == x && _selectedGemY == y))
                    {
                        List<KeyValuePair<int, int>> validMoves = HintFinder.GetValidMovesFrom(_board, x, y);
                        foreach (KeyValuePair<int, int> move in validMoves)
                        {
                            DrawDirectionArrow(g, rect, move.Key, move.Value);
                        }
                    }
                }
            }

            // Efecto visual "lágrima": salpicaduras transitorias en las celdas que regeneran
            int now2 = Environment.TickCount;
            for (int i = _teardrops.Count - 1; i >= 0; i--)
            {
                TeardropSplash t = _teardrops[i];
                int age = now2 - t.StartMs;
                if (age < 0) continue;
                if (age > t.DurationMs) { _teardrops.RemoveAt(i); continue; }
                float p = age / (float)t.DurationMs;
                int cx = startX + t.Col * tileSize + tileSize / 2;
                int cy = startY + t.Row * tileSize + tileSize / 2;
                int alpha = (int)(255 * (1f - p));
                int r = 4 + (int)(14 * p);
                using (Brush b = new SolidBrush(Color.FromArgb(alpha, 130, 200, 255)))
                using (Pen pen = new Pen(Color.FromArgb(alpha, 200, 235, 255), 2))
                {
                    g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
                    g.DrawLine(pen, cx, cy - r, cx, cy - r - 10 - (int)(8 * p));
                }
            }
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_mouseEnabled) return;
            if (e.Button != MouseButtons.Left) return;

            // En pantalla de carga, un clic en cualquier momento avanza al menú principal
            if (_screen == GameScreen.Loading)
            {
                TransitionToMainMenu(true);
                return;
            }

            if (_screen == GameScreen.Playing)
            {
                int boardStartX = 200;
                int boardStartY = 80;
                int tileSize = 60;

                int cellX = (e.X - boardStartX) / tileSize;
                int cellY = (e.Y - boardStartY) / tileSize;

                if (cellX >= 0 && cellX < Board.Cols && cellY >= 0 && cellY < Board.Rows)
                {
                    _cursorX = cellX;
                    _cursorY = cellY;
                    _dragStartX = cellX;
                    _dragStartY = cellY;
                    _dragStartPixel = e.Location;
                    _isDragging = false;

                    // Si ya había una gema previamente seleccionada y se hace clic en una contigua, intercambiar
                    if (_selectedGemX >= 0 && _selectedGemY >= 0)
                    {
                        int dx = cellX - _selectedGemX;
                        int dy = cellY - _selectedGemY;
                        if (Math.Abs(dx) + Math.Abs(dy) == 1)
                        {
                            _cursorX = _selectedGemX;
                            _cursorY = _selectedGemY;
                            int targetDx = dx;
                            int targetDy = dy;
                            _selectedGemX = -1;
                            _selectedGemY = -1;
                            PerformSwap(targetDx, targetDy);
                            return;
                        }
                    }

                    // Seleccionar gema actual
                    _selectedGemX = cellX;
                    _selectedGemY = cellY;
                    _sound.PlaySound(AudioMap.Select);
                    AnnounceCurrentCell();
                }
                else
                {
                    _selectedGemX = -1;
                    _selectedGemY = -1;
                }
                return;
            }

            // Manejo de clics de menú y subpantallas
            HandleMenuMouseClick(e);
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseEnabled) return;

            // Arrastre en el tablero (Drag & Drop) solo cuando se mantiene presionado el botón izquierdo
            if (e.Button == MouseButtons.Left && _screen == GameScreen.Playing && _dragStartX >= 0)
            {
                int dxPx = e.X - _dragStartPixel.X;
                int dyPx = e.Y - _dragStartPixel.Y;
                int threshold = 16;

                if (!_isDragging && (Math.Abs(dxPx) > threshold || Math.Abs(dyPx) > threshold))
                {
                    _isDragging = true;
                    int swapDx = 0, swapDy = 0;
                    if (Math.Abs(dxPx) > Math.Abs(dyPx))
                        swapDx = dxPx > 0 ? 1 : -1;
                    else
                        swapDy = dyPx > 0 ? 1 : -1;

                    _cursorX = _dragStartX;
                    _cursorY = _dragStartY;
                    _selectedGemX = -1;
                    _selectedGemY = -1;
                    _dragStartX = -1;
                    _dragStartY = -1;
                    PerformSwap(swapDx, swapDy);
                }
                return;
            }

            // En los menús (fuera del tablero), permitir eco del ratón sólo si el ratón está habilitado
            if (e.Button == MouseButtons.None && _screen != GameScreen.Loading && _screen != GameScreen.Playing && _screen != GameScreen.ProfileInput)
            {
                int menuStartY = 220;
                int itemHeight = 45;
                int hoverIdx = (e.Y - menuStartY) / itemHeight;

                string[] items = GetCurrentMenuItems();
                if (items != null && hoverIdx >= 0 && hoverIdx < items.Length && e.X >= 100 && e.X <= 800)
                {
                    SetCurrentMenuIndex(hoverIdx);
                }
            }
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            _dragStartX = -1;
            _dragStartY = -1;
            _isDragging = false;
        }

        private string[] GetCurrentMenuItems()
        {
            if (_screen == GameScreen.MainMenu) return GetMainMenuItems();
            if (_screen == GameScreen.GameSelect)
            {
                string[] keys = GetGameModeKeys();
                string[] res = new string[keys.Length];
                for (int i = 0; i < keys.Length; i++) res[i] = Localization.Get(keys[i]);
                return res;
            }
            if (_screen == GameScreen.Options) return GetOptionsMenuItems();
            if (_screen == GameScreen.BadgesScreen) return GetBadgeListItems();
            if (_screen == GameScreen.RecordsScreen) return GetRecordsItems();
            if (_screen == GameScreen.TutorialScreen) return GetTutorialItems();
            if (_screen == GameScreen.QuestRelicScreen) return GetQuestRelicItems();
            if (_screen == GameScreen.QuestChallengeScreen) return GetQuestChallengeItems();
            if (_screen == GameScreen.ProfileSelectScreen) return GetProfileSelectItems();
            if (_screen == GameScreen.ZenOptionsScreen) return GetZenOptionsMenuItems();
            if (_screen == GameScreen.PauseMenu) return GetPauseMenuItems();
            if (_screen == GameScreen.GameOver) return GetGameOverItems();
            if (_screen == GameScreen.AudioSchool) return GetAudioSchoolItems();
            return null;
        }

        private void SetCurrentMenuIndex(int idx)
        {
            string[] items = GetCurrentMenuItems();
            if (items == null || idx < 0 || idx >= items.Length) return;

            bool changed = false;
            if (_screen == GameScreen.MainMenu && _menuIdx != idx) { _menuIdx = idx; changed = true; }
            else if (_screen == GameScreen.GameSelect && _gameModeIdx != idx) { _gameModeIdx = idx; changed = true; }
            else if (_screen == GameScreen.Options && _optionsIdx != idx) { _optionsIdx = idx; changed = true; }
            else if (_screen == GameScreen.BadgesScreen && _badgeIdx != idx) { _badgeIdx = idx; changed = true; }
            else if (_screen == GameScreen.RecordsScreen && _recordsIdx != idx) { _recordsIdx = idx; changed = true; }
            else if (_screen == GameScreen.TutorialScreen && _tutorialIdx != idx) { _tutorialIdx = idx; changed = true; }
            else if (_screen == GameScreen.QuestRelicScreen && _relicIdx != idx) { _relicIdx = idx; changed = true; }
            else if (_screen == GameScreen.QuestChallengeScreen && _questChallengeIdx != idx) { _questChallengeIdx = idx; changed = true; }
            else if (_screen == GameScreen.ProfileSelectScreen && _profileSelectIdx != idx) { _profileSelectIdx = idx; changed = true; }
            else if (_screen == GameScreen.ZenOptionsScreen && _zenOptionsIdx != idx) { _zenOptionsIdx = idx; changed = true; }
            else if (_screen == GameScreen.PauseMenu && _pauseIdx != idx) { _pauseIdx = idx; changed = true; }
            else if (_screen == GameScreen.GameOver && _gameOverIdx != idx) { _gameOverIdx = idx; changed = true; }
            else if (_screen == GameScreen.AudioSchool && _audioSchoolIdx != idx) { _audioSchoolIdx = idx; changed = true; }

            if (changed)
            {
                if (_screen == GameScreen.QuestRelicScreen || _screen == GameScreen.QuestChallengeScreen)
                    _sound.PlaySound(AudioMap.QuestMenuButtonMouseover1);
                else
                    _sound.PlaySound(AudioMap.ButtonMouseover);

                _speech.Speak(items[idx], true);
            }
        }

        private void HandleMenuMouseClick(MouseEventArgs e)
        {
            int menuStartY = 220;
            int itemHeight = 45;
            int clickedIdx = (e.Y - menuStartY) / itemHeight;

            string[] items = GetCurrentMenuItems();
            if (items != null && clickedIdx >= 0 && clickedIdx < items.Length && e.X >= 100 && e.X <= 800)
            {
                KeyEventArgs enterKey = new KeyEventArgs(Keys.Enter);
                if (_screen == GameScreen.MainMenu) { _menuIdx = clickedIdx; HandleMainMenuKeys(enterKey); }
                else if (_screen == GameScreen.GameSelect) { _gameModeIdx = clickedIdx; HandleGameSelectKeys(enterKey); }
                else if (_screen == GameScreen.Options) { _optionsIdx = clickedIdx; HandleOptionsKeys(enterKey); }
                else if (_screen == GameScreen.BadgesScreen) { _badgeIdx = clickedIdx; HandleBadgesKeys(enterKey); }
                else if (_screen == GameScreen.RecordsScreen) { _recordsIdx = clickedIdx; HandleRecordsKeys(enterKey); }
                else if (_screen == GameScreen.TutorialScreen) { _tutorialIdx = clickedIdx; HandleTutorialKeys(enterKey); }
                else if (_screen == GameScreen.QuestRelicScreen) { _relicIdx = clickedIdx; HandleQuestRelicKeys(enterKey); }
                else if (_screen == GameScreen.QuestChallengeScreen) { _questChallengeIdx = clickedIdx; HandleQuestChallengeKeys(enterKey); }
                else if (_screen == GameScreen.ProfileSelectScreen) { _profileSelectIdx = clickedIdx; HandleProfileSelectKeys(enterKey); }
                else if (_screen == GameScreen.ZenOptionsScreen) { _zenOptionsIdx = clickedIdx; HandleZenOptionsKeys(enterKey); }
                else if (_screen == GameScreen.PauseMenu) { _pauseIdx = clickedIdx; HandlePauseMenuKeys(enterKey); }
                else if (_screen == GameScreen.GameOver) { _gameOverIdx = clickedIdx; HandleGameOverKeys(enterKey); }
                else if (_screen == GameScreen.AudioSchool) { _audioSchoolIdx = clickedIdx; HandleAudioSchoolKeys(enterKey); }
            }
        }

        private void DrawSpecialOverlay(Graphics g, Gem gem, Rectangle rect)
        {
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            switch (gem.Special)
            {
                case SpecialType.Flame:
                    DrawStar(g, cx, cy, 14, 7, Color.OrangeRed);
                    break;
                case SpecialType.Star:
                    DrawStar(g, cx, cy, 14, 7, Color.Gold);
                    break;
                case SpecialType.Hypercube:
                    DrawDiamond(g, cx, cy, 13, Color.MediumPurple);
                    break;
                case SpecialType.Supernova:
                    DrawDiamond(g, cx, cy, 15, Color.White);
                    break;
                case SpecialType.Time5:
                    DrawBadge(g, cx, cy, "5");
                    break;
                case SpecialType.Time10:
                    DrawBadge(g, cx, cy, "10");
                    break;
                case SpecialType.Bomb:
                    using (Brush b = new SolidBrush(Color.FromArgb(200, 40, 40)))
                    using (Pen p = new Pen(Color.Black, 2))
                    {
                        g.FillEllipse(b, cx - 10, cy - 10, 20, 20);
                        g.DrawEllipse(p, cx - 10, cy - 10, 20, 20);
                    }
                    using (Font f = new Font("Segoe UI", 9, FontStyle.Bold))
                    using (SolidBrush tb = new SolidBrush(Color.White))
                    {
                        string str = gem.BombTimer.ToString();
                        SizeF sz = g.MeasureString(str, f);
                        g.DrawString(str, f, tb, cx - sz.Width / 2, cy - sz.Height / 2);
                    }
                    break;
                case SpecialType.Butterfly:
                    using (Brush b = new SolidBrush(Color.FromArgb(150, 200, 240)))
                    {
                        g.FillEllipse(b, cx - 9, cy - 9, 18, 18);
                    }
                    break;
                case SpecialType.PokerCard:
                    using (Brush b = new SolidBrush(Color.FromArgb(230, 230, 235)))
                    using (Pen p = new Pen(Color.DarkSlateGray, 2))
                    {
                        g.FillRectangle(b, cx - 12, cy - 16, 24, 32);
                        g.DrawRectangle(p, cx - 12, cy - 16, 24, 32);
                    }
                    break;
                case SpecialType.Dirt:
                    using (Brush b = new SolidBrush(Color.FromArgb(139, 90, 43)))
                    {
                        g.FillRectangle(b, rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
                    }
                    break;
                case SpecialType.HardRock:
                    using (Brush b = new SolidBrush(Color.FromArgb(120, 120, 128)))
                    {
                        g.FillRectangle(b, rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
                    }
                    break;
                case SpecialType.Gold:
                    using (Brush b = new SolidBrush(Color.Gold))
                    using (Pen p = new Pen(Color.DarkGoldenrod, 2))
                    {
                        g.FillEllipse(b, cx - 9, cy - 9, 18, 18);
                        g.DrawEllipse(p, cx - 9, cy - 9, 18, 18);
                    }
                    break;
                case SpecialType.GoldNugget:
                    using (Brush b = new SolidBrush(Color.FromArgb(255, 200, 60)))
                    using (Pen p = new Pen(Color.FromArgb(140, 90, 10), 2))
                    {
                        g.FillEllipse(b, cx - 10, cy - 10, 20, 20);
                        g.DrawEllipse(p, cx - 10, cy - 10, 20, 20);
                    }
                    break;
            }
        }

        private static void DrawDirectionArrow(Graphics g, Rectangle rect, int dx, int dy)
        {
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            int margin = 6;
            int arrowLen = 14;
            int arrowWidth = 7;

            Point tip, b1, b2;
            if (dx == 1) // Derecha
            {
                tip = new Point(rect.Right - margin, cy);
                b1 = new Point(tip.X - arrowLen, tip.Y - arrowWidth);
                b2 = new Point(tip.X - arrowLen, tip.Y + arrowWidth);
            }
            else if (dx == -1) // Izquierda
            {
                tip = new Point(rect.Left + margin, cy);
                b1 = new Point(tip.X + arrowLen, tip.Y - arrowWidth);
                b2 = new Point(tip.X + arrowLen, tip.Y + arrowWidth);
            }
            else if (dy == 1) // Abajo
            {
                tip = new Point(cx, rect.Bottom - margin);
                b1 = new Point(tip.X - arrowWidth, tip.Y - arrowLen);
                b2 = new Point(tip.X + arrowWidth, tip.Y - arrowLen);
            }
            else // Arriba
            {
                tip = new Point(cx, rect.Top + margin);
                b1 = new Point(tip.X - arrowWidth, tip.Y + arrowLen);
                b2 = new Point(tip.X + arrowWidth, tip.Y + arrowLen);
            }

            Point[] arrowPts = new Point[] { tip, b1, b2 };
            using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 0)))
            using (Pen borderPen = new Pen(Color.FromArgb(200, 0, 0, 0), 1.5f))
            {
                g.FillPolygon(fillBrush, arrowPts);
                g.DrawPolygon(borderPen, arrowPts);
            }
        }

        private void DrawStar(Graphics g, int cx, int cy, int outer, int inner, Color color)
        {
            PointF[] pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double ang = -Math.PI / 2 + i * Math.PI / 5;
                double r = (i % 2 == 0) ? outer : inner;
                pts[i] = new PointF((float)(cx + r * Math.Cos(ang)), (float)(cy + r * Math.Sin(ang)));
            }
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPolygon(b, pts);
            }
        }

        private void DrawDiamond(Graphics g, int cx, int cy, int r, Color color)
        {
            PointF[] pts = new PointF[]
            {
                new PointF(cx, cy - r), new PointF(cx + r, cy),
                new PointF(cx, cy + r), new PointF(cx - r, cy)
            };
            using (SolidBrush b = new SolidBrush(color))
            using (Pen p = new Pen(Color.White, 1))
            {
                g.FillPolygon(b, pts);
                g.DrawPolygon(p, pts);
            }
        }

        private void DrawBadge(Graphics g, int cx, int cy, string text)
        {
            using (Brush b = new SolidBrush(Color.FromArgb(40, 180, 90)))
            using (Pen p = new Pen(Color.White, 1))
            {
                g.FillEllipse(b, cx - 10, cy - 10, 20, 20);
                g.DrawEllipse(p, cx - 10, cy - 10, 20, 20);
            }
            using (Font f = new Font("Segoe UI", 8, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                SizeF sz = g.MeasureString(text, f);
                g.DrawString(text, f, tb, cx - sz.Width / 2, cy - sz.Height / 2);
            }
        }
    }
}
