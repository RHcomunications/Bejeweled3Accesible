using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Bejeweled3Accessible.Accessibility;
using Bejeweled3Accessible.Audio;

namespace Bejeweled3Accessible.Engine
{
    public enum AmbientType { None, Coastal, Crickets, Forest, OceanSurf, RainLeaves, Waterfall }

    public class ZenManager : IDisposable
    {
        private readonly NvdaSpeech _speech;
        private readonly Timer _mantraTimer;
        private readonly Timer _breathTimer;

        private int _mantraIndex = 0;
        private List<Tuple<AffirmationTheme, int>> _mantraOrder = new List<Tuple<AffirmationTheme, int>>();
        private int _mantraPos = 0;

        public bool AmbientEnabled { get; set; }
        public AmbientType SelectedAmbient { get; set; }
        public bool MantrasEnabled { get; set; }
        public bool BreathModulationEnabled { get; set; }

        private readonly SoundEngine _sound;

        public ZenManager(string baseDir, NvdaSpeech speech, SoundEngine sound)
        {
            _speech = speech;
            _sound = sound;

            AmbientEnabled = false;
            SelectedAmbient = AmbientType.None;
            MantrasEnabled = true;
            BreathModulationEnabled = true;

            _mantraTimer = new Timer { Interval = 20000 };
            _mantraTimer.Tick += MantraTimer_Tick;

            _breathTimer = new Timer { Interval = 5000 };
            _breathTimer.Tick += BreathTimer_Tick;
        }

        public static string GetZenTrackForLevel(int level)
        {
            if (level < 1) level = 1;
            int zenPart = ((level - 1) % 4) + 1;
            switch (zenPart)
            {
                case 2: return MusicMap.FileName(MusicMap.ZenPart2);
                case 3: return MusicMap.FileName(MusicMap.ZenPart3);
                case 4: return MusicMap.FileName(MusicMap.ZenPart4);
                default: return MusicMap.FileName(MusicMap.ZenPart1);
            }
        }

        public static string GetAmbientTrack(AmbientType ambient)
        {
            switch (ambient)
            {
                case AmbientType.Coastal: return MusicMap.FileName(MusicMap.AmbientCoastal);
                case AmbientType.Crickets: return MusicMap.FileName(MusicMap.AmbientCrickets);
                case AmbientType.Forest: return MusicMap.FileName(MusicMap.AmbientForest);
                case AmbientType.OceanSurf: return MusicMap.FileName(MusicMap.AmbientOceanSurf);
                case AmbientType.RainLeaves: return MusicMap.FileName(MusicMap.AmbientRainLeaves);
                case AmbientType.Waterfall: return MusicMap.FileName(MusicMap.AmbientWaterfall);
                default: return "";
            }
        }

        public static string GetAmbientName(AmbientType ambient)
        {
            switch (ambient)
            {
                case AmbientType.Coastal: return Localization.Get("AmbientCoastal");
                case AmbientType.Crickets: return Localization.Get("AmbientCrickets");
                case AmbientType.Forest: return Localization.Get("AmbientForest");
                case AmbientType.OceanSurf: return Localization.Get("AmbientOceanSurf");
                case AmbientType.RainLeaves: return Localization.Get("AmbientRainLeaves");
                case AmbientType.Waterfall: return Localization.Get("AmbientWaterfall");
                default: return Localization.Get("AmbientNone");
            }
        }

        public void StartZenSession(int level = 1)
        {
            StopZenSession();

            if (AmbientEnabled && SelectedAmbient != AmbientType.None)
            {
                _sound.StopMusic();
                PlayAmbientTrack();
            }
            else
            {
                StopAmbientTrack();
                _sound.PlayMusic(GetZenTrackForLevel(level));
            }

            UpdateZenSessionState();
        }

        public void UpdateZenSessionState()
        {
            if (MantrasEnabled)
            {
                if (!_mantraTimer.Enabled) _mantraTimer.Start();
            }
            else
            {
                _mantraTimer.Stop();
            }

            if (BreathModulationEnabled)
            {
                if (!_breathTimer.Enabled)
                {
                    _boxState = BoxBreathState.Inhale;
                    _breathTimer.Start();
                }
            }
            else
            {
                _breathTimer.Stop();
            }
        }

        private void PlayAmbientTrack()
        {
            StopAmbientTrack();
            try
            {
                string musicFileName = GetAmbientTrack(SelectedAmbient);
                if (!string.IsNullOrEmpty(musicFileName))
                {
                    _sound.PlayMusic(musicFileName);
                }
            }
            catch { }
        }

        private void StopAmbientTrack()
        {
            _sound.StopMusic();
        }

        private void MantraTimer_Tick(object sender, EventArgs e)
        {
            if (!MantrasEnabled) return;
            if (_mantraPos >= _mantraOrder.Count)
            {
                _mantraOrder = Affirmations.BuildOrder(new Random());
                _mantraPos = 0;
            }
            if (_mantraOrder.Count == 0) return;
            Tuple<AffirmationTheme, int> m = _mantraOrder[_mantraPos];
            _mantraPos++;
            _mantraIndex++;
            _speech.Speak(Affirmations.Get(m.Item1, m.Item2), false);
        }

        private enum BoxBreathState { Inhale, HoldIn, Exhale, HoldOut }
        private BoxBreathState _boxState = BoxBreathState.Inhale;

        private void BreathTimer_Tick(object sender, EventArgs e)
        {
            if (!BreathModulationEnabled) return;

            switch (_boxState)
            {
                case BoxBreathState.Inhale:
                    PlayBreathSound("breath_in");
                    _speech.Speak(Localization.Get("ZenBreathInhale"), false);
                    _boxState = BoxBreathState.HoldIn;
                    break;

                case BoxBreathState.HoldIn:
                    _speech.Speak(Localization.Get("ZenBreathHoldIn"), false);
                    _boxState = BoxBreathState.Exhale;
                    break;

                case BoxBreathState.Exhale:
                    PlayBreathSound("breath_out");
                    _speech.Speak(Localization.Get("ZenBreathExhale"), false);
                    _boxState = BoxBreathState.HoldOut;
                    break;

                case BoxBreathState.HoldOut:
                    _speech.Speak(Localization.Get("ZenBreathHoldOut"), false);
                    _boxState = BoxBreathState.Inhale;
                    break;
            }
        }

        private void PlayBreathSound(string soundName)
        {
            _sound.PlaySound(soundName);
        }

        public void StopZenSession()
        {
            _mantraTimer.Stop();
            _breathTimer.Stop();
            StopAmbientTrack();
        }

        public void Dispose()
        {
            StopZenSession();
            _mantraTimer.Dispose();
            _breathTimer.Dispose();
        }
    }
}
