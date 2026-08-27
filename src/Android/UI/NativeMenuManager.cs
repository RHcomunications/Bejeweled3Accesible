using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Bejeweled3Accessible.Audio;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;
using Bejeweled3Accessible.AndroidApp.Audio;
using Bejeweled3Accessible.AndroidApp.Update;

namespace Bejeweled3Accessible.AndroidApp.UI
{
    public class NativeMenuManager
    {
        private readonly MainActivity _activity;
        private readonly TalkBackBridge _talkBack;
        private readonly AndroidSoundEngine _sound;
        private readonly ProfileManager _profileMgr;
        private readonly GameOptions _options;
        private BadgeManager _badgeMgr;

        private int _relicIdx = 0;
        private readonly Action<string> _onStartGame;

        public NativeMenuManager(MainActivity activity, TalkBackBridge talkBack, AndroidSoundEngine sound, Action<string> onStartGame)
        {
            _activity = activity;
            _talkBack = talkBack;
            _sound = sound;
            _onStartGame = onStartGame;

            _profileMgr = ProfileManager.Load();
            _options = GameOptions.Load();

            if (_sound != null && _options != null)
            {
                _sound.MusicVol = _options.MusicVolume;
                _sound.SfxVol = _options.SoundVolume;
                _sound.VoiceVol = _options.VoiceVolume;
                _sound.BinauralEnabled = _options.BinauralEnabled;
            }

            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "Jugador 1";
            _badgeMgr = BadgeManager.Load(profName);
        }

        private GameProgress Progress => _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.Progress : new GameProgress();

        private ScrollView CreateBaseLayout(string title, out LinearLayout container)
        {
            float density = _activity.Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density < 1.0f) density = 1.0f;

            ScrollView scrollView = new ScrollView(_activity)
            {
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
                FillViewport = true
            };
            scrollView.SetBackgroundColor(Color.Rgb(15, 15, 25));

            container = new LinearLayout(_activity)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            };
            container.SetPadding((int)(20 * density), (int)(30 * density), (int)(20 * density), (int)(30 * density));

            TextView titleView = new TextView(_activity)
            {
                Text = title,
                TextSize = 24,
                Typeface = Typeface.DefaultBold,
                Gravity = GravityFlags.CenterHorizontal,
                ImportantForAccessibility = ImportantForAccessibility.Yes
            };
            titleView.SetTextColor(Color.White);
            titleView.SetPadding(0, 0, 0, (int)(20 * density));
            container.AddView(titleView);

            scrollView.AddView(container);
            return scrollView;
        }

        private Button CreateMenuButton(string text, string contentDesc, Action onClick)
        {
            float density = _activity.Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density < 1.0f) density = 1.0f;

            Button btn = new Button(_activity)
            {
                Text = text,
                TextSize = 18,
                ContentDescription = string.IsNullOrWhiteSpace(contentDesc) ? text : contentDesc,
                Focusable = true,
                Clickable = true,
                ImportantForAccessibility = ImportantForAccessibility.Yes
            };

            btn.SetTextColor(Color.White);
            btn.SetBackgroundColor(Color.Rgb(40, 50, 80));

            var lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            lp.SetMargins(0, (int)(8 * density), 0, (int)(8 * density));
            btn.LayoutParameters = lp;
            btn.SetPadding((int)(16 * density), (int)(16 * density), (int)(16 * density), (int)(16 * density));

            btn.Click += (s, e) =>
            {
                _sound?.PlaySound(AudioMap.ButtonPress);
                onClick?.Invoke();
            };

            return btn;
        }

        public void ShowLoadingScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("LoadingTitle"), out var container);

            TextView prompt = new TextView(_activity)
            {
                Text = Localization.Get("LoadingPrompt"),
                TextSize = 18,
                Gravity = GravityFlags.CenterHorizontal
            };
            prompt.SetTextColor(Color.LightGray);
            container.AddView(prompt);

            _activity.SetContentView(scroll);
            _sound?.PlayMusic(MusicMap.Intro);
            _talkBack?.Speak("Cargando Bejeweled 3 Accesible. Preparando menú principal...", true);

            scroll.PostDelayed(() =>
            {
                ShowMainMenu(true);
            }, 3000);
        }

        public void ShowMainMenu(bool atStartup = false)
        {
            _activity.SetDesiredOrientation(false);

            if (_profileMgr.Profiles.Count == 0)
            {
                // Primera ejecucion (arranque de la app): el "Welcome to Bejeweled 3"
                // suena solo al INICIAR el juego, no al volver al menu desde una partida.
                if (atStartup)
                {
                    _sound?.PlayMusic(MusicMap.MainTheme);
                    _sound?.PlaySound(AudioMap.VoiceWelcometobejeweled);
                }
                PromptCreateProfile();
                return;
            }

            string profName = _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.ProfileName : "";
            var scroll = CreateBaseLayout(Localization.Get("AppTitle"), out var container);

            _sound?.PlayMusic(MusicMap.MainTheme);
            // "Welcome back" solo al INICIAR el juego (arranque), igual que Windows
            // (TransitionToMainMenu(true) en MusicRechained). Nunca al volver al menu.
            if (atStartup)
                _sound?.PlaySound(AudioMap.VoiceWelcomeback);

            container.AddView(CreateMenuButton(Localization.Get("MenuPlay"), "", () => ShowGameSelect()));
            container.AddView(CreateMenuButton(Localization.Get("MenuBadges"), "", () => ShowBadgesScreen()));
            container.AddView(CreateMenuButton(Localization.Get("MenuRecords"), "", () => ShowRecordsScreen()));
            container.AddView(CreateMenuButton(Localization.Get("MenuTutorial"), "", () => ShowTutorialScreen()));
            container.AddView(CreateMenuButton(Localization.Get("MenuChangeUser", profName), "", () => ShowProfileSelectScreen()));
            container.AddView(CreateMenuButton(Localization.Get("MenuLanguage"), "", () =>
            {
                Localization.ToggleLanguage();
                _options.SelectedLanguage = Localization.CurrentLanguage;
                _options.Save();
                ShowMainMenu();
            }));
            container.AddView(CreateMenuButton(Localization.Get("MenuOptions"), "", () => ShowOptionsScreen()));
            container.AddView(CreateMenuButton(Localization.Get("MenuAudioSchool"), "", () => ShowAudioSchoolScreen()));
            container.AddView(CreateMenuButton(Localization.Get("MenuUpdateCheck"), "", () =>
            {
                _talkBack?.Speak(Localization.Get("UpdateChecking"), true);
                Task.Run(async () =>
                {
                    var info = await AndroidAutoUpdater.CheckForUpdatesAsync();
                    _activity.RunOnUiThread(() =>
                    {
                        if (info.IsNewer)
                        {
                            string msg = Localization.Get("UpdateFoundNoNotes", AndroidAutoUpdater.CurrentVersion, info.Tag);
                            _sound?.PlaySound(AudioMap.Rankup);
                            _talkBack?.Speak(msg + ". Abriendo enlace de descarga...", true);
                            AndroidAutoUpdater.OpenDownloadOrRelease(_activity, info);
                        }
                        else
                        {
                            string msg = Localization.Get("UpdateNone", AndroidAutoUpdater.CurrentVersion);
                            _sound?.PlaySound(AudioMap.ButtonPress);
                            _talkBack?.Speak(msg, true);
                        }
                    });
                });
            }));
            container.AddView(CreateMenuButton(Localization.Get("MenuExit"), "", () =>
            {
                _sound?.PlaySound(AudioMap.VoiceGoodbye);
                _talkBack?.Speak(Localization.CurrentLanguage == Language.Spanish ? "¡Adiós!" : "Goodbye!", true);
                scroll.PostDelayed(() => { System.Environment.Exit(0); }, 1000);
            }));

            _activity.SetContentView(scroll);
            _talkBack?.Speak(Localization.Get("AppTitle") + ". " + Localization.Get("MenuPlay"), true);
        }

        public void ShowGameSelect()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("MenuPlay"), out var container);

            container.AddView(CreateMenuButton(Localization.Get("ModeClassic"), "", () => _onStartGame?.Invoke("ModeClassic")));
            container.AddView(CreateMenuButton(Localization.Get("ModeLightning"), "", () => _onStartGame?.Invoke("ModeLightning")));
            container.AddView(CreateMenuButton(Localization.Get("ModeZen"), "", () => ShowZenOptionsScreen()));
            container.AddView(CreateMenuButton(Localization.Get("ModeQuest"), "", () => ShowQuestRelicScreen()));

            if (Progress.IsPokerUnlocked)
                container.AddView(CreateMenuButton(Localization.Get("ModePoker"), "", () => _onStartGame?.Invoke("ModePoker")));
            if (Progress.IsButterfliesUnlocked)
                container.AddView(CreateMenuButton(Localization.Get("ModeButterflies"), "", () => _onStartGame?.Invoke("ModeButterflies")));
            if (Progress.IsIceStormUnlocked)
                container.AddView(CreateMenuButton(Localization.Get("ModeIceStorm"), "", () => _onStartGame?.Invoke("ModeIceStorm")));
            if (Progress.IsDiamondMineUnlocked)
                container.AddView(CreateMenuButton(Localization.Get("ModeDiamondMine"), "", () => _onStartGame?.Invoke("ModeDiamondMine")));

            container.AddView(CreateMenuButton(Localization.Get("BackToMain"), "", () => ShowMainMenu()));

            _activity.SetContentView(scroll);
            _talkBack?.Speak(Localization.Get("MenuPlay"), true);
        }

        public void ShowZenOptionsScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("ModeZen"), out var container);

            string ambStr = _options.ZenAmbient != (int)AmbientType.None ? AmbientHelper.GetAmbientName((AmbientType)_options.ZenAmbient) : Localization.Get("StateDisabled");
            string manStr = _options.ZenMantras ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled");
            string breathStr = _options.ZenBreath ? Localization.Get("StateEnabled") : Localization.Get("StateDisabled");

            container.AddView(CreateMenuButton("▶ " + Localization.Get("MenuPlay"), "", () => _onStartGame?.Invoke("ModeZen")));

            container.AddView(CreateMenuButton(Localization.Get("ZenOptAmbient", ambStr), "", () =>
            {
                int maxAmb = Enum.GetValues(typeof(AmbientType)).Length;
                _options.ZenAmbient = (_options.ZenAmbient + 1) % maxAmb;
                _options.Save();
                ShowZenOptionsScreen();
            }));

            container.AddView(CreateMenuButton(Localization.Get("ZenOptMantras", manStr), "", () =>
            {
                _options.ZenMantras = !_options.ZenMantras;
                _options.Save();
                ShowZenOptionsScreen();
            }));

            container.AddView(CreateMenuButton(Localization.Get("ZenOptBreath", breathStr), "", () =>
            {
                _options.ZenBreath = !_options.ZenBreath;
                _options.Save();
                ShowZenOptionsScreen();
            }));

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowGameSelect()));

            _activity.SetContentView(scroll);
        }

        public void ShowQuestRelicScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("ModeQuest"), out var container);

            for (int i = 1; i <= 5; i++)
            {
                int relicIdx = i - 1;
                int done = Progress.CountCompletedInRelic(relicIdx);
                string title = Localization.Get("Relic" + i) + (done >= 8 ? Localization.Get("QuestCompletedMark") : " (" + done + " de 8)");

                int currentRelic = relicIdx;
                container.AddView(CreateMenuButton(title, "", () =>
                {
                    _relicIdx = currentRelic;
                    ShowQuestChallengeScreen();
                }));
            }

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowGameSelect()));

            _activity.SetContentView(scroll);
        }

        public void ShowQuestChallengeScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("Relic" + (_relicIdx + 1)), out var container);

            QuestMission[] missions = QuestManager.GetRelicMissions(_relicIdx);
            foreach (var m in missions)
            {
                string title = m.GetName();
                if (Progress.IsQuestMissionComplete(m.MissionIndex))
                    title += Localization.Get("QuestCompletedMark");

                container.AddView(CreateMenuButton(title, "", () =>
                {
                    _onStartGame?.Invoke("ModeQuest");
                }));
            }

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowQuestRelicScreen()));

            _activity.SetContentView(scroll);
        }

        public void ShowOptionsScreen(bool fromPause = false)
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("MenuOptions"), out var container);

            container.AddView(CreateMenuButton(Localization.Get("OptSoundVol", _sound != null ? _sound.SfxVol : 100), "", () =>
            {
                _options.SoundVolume = (_options.SoundVolume + 10) % 110;
                if (_options.SoundVolume == 0 && _sound.SfxVol == 100) _options.SoundVolume = 10;
                _sound.SfxVol = _options.SoundVolume;
                _sound.PlaySound(AudioMap.Select);
                _options.Save();
                ShowOptionsScreen(fromPause);
            }));

            container.AddView(CreateMenuButton(Localization.Get("OptMusicVol", _sound != null ? _sound.MusicVol : 80), "", () =>
            {
                _options.MusicVolume = (_options.MusicVolume + 10) % 110;
                if (_options.MusicVolume == 0 && _sound.MusicVol == 100) _options.MusicVolume = 10;
                _sound.MusicVol = _options.MusicVolume;
                _sound.UpdateMusicVolume();
                _sound.PlaySound(AudioMap.Select);
                _options.Save();
                ShowOptionsScreen(fromPause);
            }));

            container.AddView(CreateMenuButton(Localization.Get("OptVoiceVol", _sound != null ? _sound.VoiceVol : 100), "", () =>
            {
                _options.VoiceVolume = (_options.VoiceVolume + 10) % 110;
                if (_options.VoiceVolume == 0 && _sound.VoiceVol == 100) _options.VoiceVolume = 10;
                _sound.VoiceVol = _options.VoiceVolume;
                _sound.PlaySound(AudioMap.VoiceAwesome);
                _options.Save();
                ShowOptionsScreen(fromPause);
            }));

            container.AddView(CreateMenuButton(Localization.Get("OptBinaural", (_sound != null && _sound.BinauralEnabled) ? Localization.Get("StateOn") : Localization.Get("StateOff")), "", () =>
            {
                _options.BinauralEnabled = !_options.BinauralEnabled;
                _sound.BinauralEnabled = _options.BinauralEnabled;
                _sound.PlaySound(AudioMap.Select);
                _options.Save();
                ShowOptionsScreen(fromPause);
            }));

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () =>
            {
                _options.Save();
                if (fromPause) ShowPauseMenu();
                else ShowMainMenu();
            }));

            _activity.SetContentView(scroll);
        }

        public void ShowPauseMenu()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("PauseTitle"), out var container);

            container.AddView(CreateMenuButton(Localization.Get("PauseResume"), "", () =>
            {
                _activity.ResumeGame();
            }));

            container.AddView(CreateMenuButton(Localization.Get("PauseRestart"), "", () =>
            {
                _activity.RestartGame();
            }));

            container.AddView(CreateMenuButton(Localization.Get("PauseOptions"), "", () => ShowOptionsScreen(true)));

            container.AddView(CreateMenuButton(Localization.Get("PauseMainMenu"), "", () =>
            {
                _sound?.StopMusic();
                ShowMainMenu();
            }));

            _activity.SetContentView(scroll);
            _talkBack?.Speak(Localization.Get("PauseTitle"), true);
        }

        public void ShowBadgesScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("MenuBadges"), out var container);

            string[] keys = new string[]
            {
                "BadgeInferno", "BadgeStellar", "BadgeChromatic", "BadgeBlaster",
                "BadgeBejeweler", "BadgeFinalFrenzy", "BadgeHighVoltage", "BadgeAnteUp",
                "BadgeRelicHunter", "BadgeButterflyMonarch"
            };

            foreach (var k in keys)
            {
                BadgeTier t = _badgeMgr.GetTier(k);
                string tierStr = Localization.Get("Tier" + t.ToString());
                string line = string.Format("{0}: {1}", Localization.Get(k), tierStr);
                container.AddView(CreateMenuButton(line, "", () =>
                {
                    _talkBack?.Speak(line, true);
                }));
            }

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowMainMenu()));

            _activity.SetContentView(scroll);
        }

        public void ShowRecordsScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("MenuRecords"), out var container);

            var stats = Progress;
            string[] records = new string[]
            {
                string.Format("{0}: Nivel {1}", Localization.Get("ModeClassic"), stats.ClassicLevel),
                string.Format("{0}: {1}", Localization.Get("ModeLightning"), stats.LightningHighScore),
                string.Format("{0}: Nivel {1}", Localization.Get("ModeZen"), stats.ZenLevel),
                string.Format("{0}: {1}", Localization.Get("TotalGemsCleared"), stats.TotalGemsCleared)
            };

            foreach (var r in records)
            {
                container.AddView(CreateMenuButton(r, "", () => _talkBack?.Speak(r, true)));
            }

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowMainMenu()));

            _activity.SetContentView(scroll);
        }

        public void ShowTutorialScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("MenuTutorial"), out var container);

            string[] steps = new string[]
            {
                Localization.Get("TutorialStep1"),
                Localization.Get("TutorialStep2"),
                Localization.Get("TutorialStep3"),
                Localization.Get("TutorialStep4")
            };

            foreach (var s in steps)
            {
                container.AddView(CreateMenuButton(s, "", () => _talkBack?.Speak(s, true)));
            }

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowMainMenu()));

            _activity.SetContentView(scroll);
        }

        public void ShowProfileSelectScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("ProfileSelectTitle"), out var container);

            for (int i = 0; i < _profileMgr.Profiles.Count; i++)
            {
                string marker = (i == _profileMgr.CurrentProfileIndex) ? " (" + Localization.Get("StateEnabled") + ")" : "";
                string name = _profileMgr.Profiles[i].ProfileName;
                int currentIdx = i;

                container.AddView(CreateMenuButton(name + marker, "", () =>
                {
                    _profileMgr.CurrentProfileIndex = currentIdx;
                    _profileMgr.Save();
                    _badgeMgr = BadgeManager.Load(name);
                    _talkBack?.Speak(string.Format("Perfil seleccionado: {0}", name), true);
                    ShowMainMenu();
                }));
            }

            container.AddView(CreateMenuButton(Localization.Get("ProfileCreateNew"), "", () => PromptCreateProfile()));
            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowMainMenu()));

            _activity.SetContentView(scroll);
        }

        public void ShowAudioSchoolScreen()
        {
            _activity.SetDesiredOrientation(false);
            var scroll = CreateBaseLayout(Localization.Get("MenuAudioSchool"), out var container);

            bool en = Localization.CurrentLanguage == Language.English;
            Func<string, string, string> L = (es, e) => en ? e : es;
            string[] cols = { "A", "B", "C", "D", "E", "F", "G", "H" };

            for (int i = 0; i < 8; i++)
            {
                int colIdx = i;
                string text = L(string.Format("Columna {0} (izquierda a derecha)", cols[i]), string.Format("Column {0} (left to right)", cols[i]));
                container.AddView(CreateMenuButton(text, "", () =>
                {
                    float pan = SpatialAudio.PanColumn(colIdx);
                    _sound?.PlaySoundSpatialPan(pan, 0.0f, AudioMap.Select);
                }));
            }

            container.AddView(CreateMenuButton(Localization.Get("OptBack"), "", () => ShowMainMenu()));

            _activity.SetContentView(scroll);
        }

        public void PromptCreateProfile()
        {
            _activity.RunOnUiThread(() =>
            {
                var builder = new AlertDialog.Builder(_activity);
                builder.SetTitle(Localization.Get("ProfileCreateNew"));
                builder.SetMessage(Localization.Get("EnterNamePrompt"));

                var input = new EditText(_activity)
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
                    ShowMainMenu();
                });

                builder.SetNegativeButton(Localization.Get("OptBack"), (sender, args) =>
                {
                    if (_profileMgr.Profiles.Count > 0) ShowMainMenu();
                });

                builder.Show();
            });
        }
    }
}
