using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Audio
{
    public class SoundEngine : IDisposable
    {
        // BASS P/Invoke Declarations
        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_Init(int device, uint freq, uint flags, IntPtr win, IntPtr cls);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_Free();

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_LastError();

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_StreamCreateFile(bool mem, IntPtr file, long offset, long length, uint flags);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelPlay(int handle, bool restart);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelStop(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_StreamFree(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelSetAttribute(int handle, int attrib, float value);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelGetAttribute(int handle, int attrib, ref float value);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern long BASS_ChannelGetLength(int handle, uint mode);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern long BASS_ChannelGetPosition(int handle, uint pos);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern double BASS_ChannelBytes2Seconds(int handle, long pos);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_ChannelIsActive(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_ChannelSetFX(int handle, uint type, int priority);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelRemoveFX(int handle, int fx);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_FXSetParameters(int handle, IntPtr par);

        private const uint BASS_SAMPLE_LOOP = 4;
        private const uint BASS_FX_DX8_REVERB = 8;
        private const int BASS_ATTRIB_FREQ = 1;
        private const int BASS_ATTRIB_VOL = 2;
        private const int BASS_ATTRIB_PAN = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct BASS_DX8_REVERB
        {
            public float fInGain;
            public float fReverbMix;
            public float fReverbTime;
            public float fHighFreqRttHFRatio;
        }

        private readonly string _soundDir;
        private readonly string _musicDir;
        private readonly string _logDir;
        private readonly PacReader _audioPac;

        private bool _bassReady = false;

        private int _currentMusicChannel = 0;
        private int _musicReverbFx = 0;
        private GCHandle _pinnedMusicBytes;
        private readonly List<ActiveSfx> _activeSfxList = new List<ActiveSfx>();

        // Music crossfade state. The crossfade overlaps the incoming track with
        // the outgoing one (the new channel fades in while the old fades out),
        // so changing songs never leaves a silent gap - like the original game,
        // whose .mo3 tracks chain into each other automatically.
        private const int FADE_STEPS = 8;
        private const int FADE_TICK_MS = 25; // ~200 ms total crossfade
        private System.Threading.Timer _musicFadeTimer;
        private readonly object _musicLock = new object();
        private string _pendingMusicFile = null;
        private int _pendingMusicChannel = 0;
        private GCHandle _pendingMusicPin;
        private bool _fadingOut = false;
        private float _fadeVol = 0.0f;
        private float _fadeVolPerStep = 0.0f;

        public bool SpatialBinauralEnabled { get; set; }

        // Active spatial profile. When SpatialBinauralEnabled is off every
        // profile collapses to centered, flat audio.
        public SpatialProfile SpatialProfile { get; set; }

        // Queue a voice without ever cutting the one that is sounding now.
        private void EnqueueVoice(VoiceRequest req)
        {
            req.DurationMs = GetVoiceDurationMs(req.SoundName);

            lock (_voiceLock)
            {
                if (_voiceQueue.Count >= VOICE_QUEUE_MAX)
                {
                    VoiceRequest dropped = _voiceQueue.Dequeue();
                    _voiceLastError = "descarte de cola '" + dropped.SoundName + "' por limite";
                    _voiceTrace += "X(" + dropped.SoundName + ")";
                }
                _voiceQueue.Enqueue(req);
                _voiceTrace += "E(" + req.SoundName + ")";

                if (_voicePumpTimer == null)
                {
                    _voicePumpTimer = new System.Threading.Timer(VoicePumpTick, null, VOICE_PUMP_MS, VOICE_PUMP_MS);
                }
            }
        }

        // The pump starts the next queued voice as soon as the current one has
        // finished, respecting a minimum silent gap so locutions never overlap.
        private void VoicePumpTick(object state)
        {
            lock (_voiceLock)
            {
                if (_activeVoiceHandle != 0)
                {
                    if (DateTime.UtcNow < _activeVoiceEndAt) return;

                    long playedBytes = 0;
                    try { playedBytes = BASS_ChannelGetPosition(_activeVoiceHandle, BASS_POS_BYTE); } catch { }
                    _voiceHistory.Add(new VoicePlayback
                    {
                        SoundName = _activeVoiceName,
                        StartMs = _activeVoiceStartMs,
                        EndMs = _activeVoiceStartMs + _activeVoicePlannedMs,
                        DurationMs = (int)Math.Max(1, _activeVoicePlannedMs - VOICE_MIN_GAP_MS),
                        LengthBytes = _activeVoiceLengthBytes,
                        PlayedBytes = playedBytes
                    });

                    try { BASS_StreamFree(_activeVoiceHandle); } catch { }
                    if (_activeVoicePin.IsAllocated)
                    {
                        try { _activeVoicePin.Free(); } catch { }
                    }
                    _activeVoiceHandle = 0;
                    _lastVoiceEnd = DateTime.UtcNow;
                }

                if (_voiceQueue.Count == 0)
                {
                    if (_activeVoiceHandle == 0 && _voicePumpTimer != null)
                    {
                        try { _voicePumpTimer.Dispose(); } catch { }
                        _voicePumpTimer = null;
                    }
                    return;
                }

                if ((DateTime.UtcNow - _lastVoiceEnd).TotalMilliseconds < VOICE_MIN_GAP_MS) return;

                while (_voiceQueue.Count > 0)
                {
                    VoiceRequest req = _voiceQueue.Dequeue();
                    if (StartVoice(req)) break;
                }
            }
        }

        private bool StartVoice(VoiceRequest req)
        {
            byte[] audioBytes = null;
            try
            {
                audioBytes = LoadAudioBytes(req.SoundName);
                if (audioBytes == null || audioBytes.Length == 0)
                {
                    _voiceLastError = "sin audio para '" + req.SoundName + "'";
                    return false;
                }
            }
            catch (Exception ex)
            {
                _voiceLastError = "carga '" + req.SoundName + "': " + ex.Message;
                return false;
            }

            try
            {
                GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
                int handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, audioBytes.Length, 0);

                if (handle == 0)
                {
                    _voiceLastError = "BASS_StreamCreateFile(0) para '" + req.SoundName + "'";
                    LogAudioError("Voz '" + req.SoundName + "' StreamCreateFile fallo, err=" + BASS_LastError());
                    if (pinned.IsAllocated) pinned.Free();
                    return false;
                }

                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, (float)VoiceVol / 100.0f);

                // Bejeweled-adapted HRTF: voices ALWAYS centered - the speaker
                // stays in the middle regardless of the gem they announce.
                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, SpatialAudio.VoicePan);

                if (Math.Abs(req.Pitch - 1.0f) > 0.01f)
                {
                    float currentFreq = 44100.0f;
                    if (BASS_ChannelGetAttribute(handle, BASS_ATTRIB_FREQ, ref currentFreq))
                    {
                        BASS_ChannelSetAttribute(handle, BASS_ATTRIB_FREQ, currentFreq * req.Pitch);
                    }
                }

                BASS_ChannelPlay(handle, true);
                _activeVoiceHandle = handle;
                _activeVoicePin = pinned;
                _activeVoiceName = req.SoundName;
                _activeVoiceStartMs = _voiceClock.ElapsedMilliseconds;
                _activeVoicePlannedMs = (req.DurationMs > 0 ? req.DurationMs : VOICE_DEFAULT_MS) + VOICE_MIN_GAP_MS;
                _activeVoiceEndAt = DateTime.UtcNow.AddMilliseconds(_activeVoicePlannedMs);
                try
                {
                    _activeVoiceLengthBytes = BASS_ChannelGetLength(handle, BASS_POS_BYTE);
                    // Cache the real measured duration here (pump thread) so repeat
                    // GetVoiceDurationMs calls never decode the OGG on the UI thread.
                    if (_activeVoiceLengthBytes > 0)
                    {
                        double secs = BASS_ChannelBytes2Seconds(handle, _activeVoiceLengthBytes);
                        if (secs > 0.0)
                        {
                            int durMs = (int)(secs * 1000.0);
                            if (durMs > 0) CacheVoiceDuration(req.SoundName, durMs);
                        }
                    }
                }
                catch { }
                return true;
            }
            catch (Exception ex)
            {
                lock (_voiceLock) { _voiceLastError = "start '" + req.SoundName + "': " + ex.Message; }
                return false;
            }
        }

        private struct ActiveSfx
        {
            public int Handle;
            public GCHandle Pin;
            public bool IsVoice;
        }

        private struct VoiceRequest
        {
            public string SoundName;
            public int Col;
            public int Row;
            public float Pitch;
            public bool UseSpatial;
            public int DurationMs;
        }

        // Voice scheduler: game voices (voice_*.ogg) never cut each other off
        // and never overlap. The duration of each locution is measured with BASS
        // and the next queued voice waits until the current one has finished
        // plus a minimum silent gap, so every locution is heard in full.
        private const int VOICE_PUMP_MS = 40;
        private const int VOICE_MIN_GAP_MS = 120;
        private const int VOICE_QUEUE_MAX = 6;
        private const int VOICE_DEFAULT_MS = 1600;
        private const uint BASS_POS_BYTE = 0;
        private readonly object _voiceLock = new object();
        private readonly Queue<VoiceRequest> _voiceQueue = new Queue<VoiceRequest>();
        private readonly Dictionary<string, int> _voiceDurationCache = new Dictionary<string, int>();
        private volatile bool _preloadStarted = false;
        private readonly List<VoicePlayback> _voiceHistory = new List<VoicePlayback>();
        private readonly System.Diagnostics.Stopwatch _voiceClock = System.Diagnostics.Stopwatch.StartNew();
        private System.Threading.Timer _voicePumpTimer;
        private int _activeVoiceHandle = 0;
        private GCHandle _activeVoicePin;
        private DateTime _activeVoiceEndAt = DateTime.MinValue;
        private DateTime _lastVoiceEnd = DateTime.MinValue;
        private string _activeVoiceName = "";
        private long _activeVoiceStartMs = 0;
        private long _activeVoicePlannedMs = 0;
        private long _activeVoiceLengthBytes = 0;

        // Animated lateral glide for gem movements (swap / cascade). A single
        // shared timer advances every pending sweep so channels never pile up.
        private const int PAN_SWEEP_MS = 180;
        private const int PAN_SWEEP_TICK_MS = 24;
        private readonly object _panSweepLock = new object();
        private readonly List<PanSweep> _panSweeps = new List<PanSweep>();
        private System.Threading.Timer _panSweepTimer;
        private readonly System.Diagnostics.Stopwatch _panClock = System.Diagnostics.Stopwatch.StartNew();

        private struct PanSweep
        {
            public int Handle;
            public float FromPan;
            public float ToPan;
            public float VolBase;
            public float FreqBase;
            public long StartMs;
        }

        // Diagnostic of a played locution: schedule (math) + real audio position
        // at the moment it finished, so tests can prove full playback.
        public struct VoicePlayback
        {
            public string SoundName;
            public long StartMs;
            public long EndMs;
            public int DurationMs;
            public long LengthBytes;
            public long PlayedBytes;
            public bool FullyPlayed
            {
                get { return LengthBytes > 0 && PlayedBytes >= (long)((double)LengthBytes * 0.98); }
            }
        }

        // Full playback history of the last voices (for tests/diagnostics).
        public VoicePlayback[] GetVoicePlaybackHistory()
        {
            lock (_voiceLock)
            {
                return _voiceHistory.ToArray();
            }
        }

        private string _voiceLastError = "";
        public string VoiceLastError
        {
            get { lock (_voiceLock) { return _voiceLastError; } }
        }
        public int VoicePendingCount
        {
            get { lock (_voiceLock) { return _voiceQueue.Count; } }
        }
        private string _voiceTrace = "";
        public string VoiceTrace
        {
            get { lock (_voiceLock) { return _voiceTrace; } }
        }

        public string SoundDir { get { return _soundDir; } }

        // File name of the track currently sounding (or being crossfaded in).
        public string MusicNowPlaying
        {
            get
            {
                lock (_musicLock)
                {
                    return _pendingMusicFile ?? _currentMusicFile;
                }
            }
        }

        // Fired when a track reaches its natural end and is re-chained by the
        // monitor (on the monitor's worker thread). The loading screen uses it
        // to auto-advance once the intro has played through.
        public event EventHandler MusicRechained;

        // Diagnóstico para la suite de tests: true cuando el monitor de
        // encadenado está operativo (sin fade/crossfade en curso). El bug de
        // regresión dejaba _fadingOut colgado en true tras el primer crossfade,
        // congelando el encadenado de toda la música.
        internal bool MusicLoopArmed
        {
            get
            {
                lock (_musicLock)
                {
                    return _currentMusicChannel != 0
                        && _pendingMusicChannel == 0
                        && !_fadingOut
                        && _musicFadeTimer == null;
                }
            }
        }

        internal bool MusicChannelActive
        {
            get
            {
                lock (_musicLock)
                {
                    if (_currentMusicChannel == 0) return false;
                    try { return BASS_ChannelIsActive(_currentMusicChannel) != 0; }
                    catch { return false; }
                }
            }
        }

        public int MusicVol { get; set; }
        public int SfxVol { get; set; }
        public int VoiceVol { get; set; }

        public SoundEngine(string baseDir)
        {
            MusicVol = 80;
            SfxVol = 100;
            VoiceVol = 100;
            SpatialBinauralEnabled = true;
            SpatialProfile = SpatialProfile.CleanArcade;

            string candidateSoundDir1 = Path.Combine(baseDir, "sounds");
            string candidateSoundDir2 = Path.Combine(baseDir, "sounds", "sounds");

            if (Directory.Exists(candidateSoundDir2))
                _soundDir = candidateSoundDir2;
            else
                _soundDir = candidateSoundDir1;

            _musicDir = Path.Combine(baseDir, "music");

            string audioPacPath = Path.Combine(baseDir, "audio.pac");
            _audioPac = new PacReader(audioPacPath);

            // Initialize BASS Audio Engine (-1 default device, 44100Hz)
            try
            {
                _logDir = AudioManagerResolveLogDir();
                _bassReady = BASS_Init(-1, 44100, 0, IntPtr.Zero, IntPtr.Zero);
                if (!_bassReady) LogAudioError("BASS_Init fallo: err=" + BASS_LastError());
            }
            catch (DllNotFoundException ex)
            {
                _bassReady = false;
                LogAudioError("BASS_Init DllNotFound: " + ex.Message);
            }
            catch (Exception ex)
            {
                _bassReady = false;
                LogAudioError("BASS_Init fallo: " + ex.Message);
            }
        }

        private static string AudioManagerResolveLogDir()
        {
            try { return Engine.StoragePaths.ResolveDataDirectory(Engine.GameProgress.OverrideDataDirectory); }
            catch { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private void LogAudioError(string msg)
        {
            try
            {
                if (_logDir == null) return;
                string logPath = Path.Combine(_logDir, "audio_errors.log");
                File.AppendAllText(logPath,
                    string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}\r\n", DateTime.Now, msg));
            }
            catch { }
        }

        private void CleanFinishedSfxChannels()
        {
            if (!_bassReady) return;
            lock (_activeSfxList)
            {
                for (int i = _activeSfxList.Count - 1; i >= 0; i--)
                {
                    ActiveSfx sfx = _activeSfxList[i];
                    try
                    {
                        if (BASS_ChannelIsActive(sfx.Handle) == 0) // Stopped or finished
                        {
                            if (sfx.Pin.IsAllocated)
                            {
                                try { sfx.Pin.Free(); } catch { }
                            }
                            BASS_StreamFree(sfx.Handle);
                            _activeSfxList.RemoveAt(i);
                        }
                    }
                    catch { }
                }
            }
        }

        // Voices never overlap: playing a voice (or a screen-reader announcement)
        // stops any other voice that is currently sounding.
        public void StopActiveVoices()
        {
            lock (_voiceLock)
            {
                try { if (_activeVoiceHandle != 0) BASS_ChannelStop(_activeVoiceHandle); } catch { }
                try { if (_activeVoiceHandle != 0) BASS_StreamFree(_activeVoiceHandle); } catch { }
                if (_activeVoicePin.IsAllocated)
                {
                    try { _activeVoicePin.Free(); } catch { }
                }
                _activeVoiceHandle = 0;
                _activeVoiceEndAt = DateTime.MinValue;
                _voiceQueue.Clear();
                if (_voicePumpTimer != null)
                {
                    try { _voicePumpTimer.Dispose(); } catch { }
                    _voicePumpTimer = null;
                }
            }
        }

        // True while a locution is sounding or waiting in the queue.
        public bool IsVoiceBusy
        {
            get
            {
                lock (_voiceLock)
                {
                    if (_activeVoiceHandle != 0 && DateTime.UtcNow < _activeVoiceEndAt) return true;
                    return _voiceQueue.Count > 0;
                }
            }
        }

        // Measured duration of a locution in milliseconds, cached per name.
        public int GetVoiceDurationMs(string soundName)
        {
            int cached;
            lock (_voiceDurationCache)
            {
                if (_voiceDurationCache.TryGetValue(soundName, out cached))
                {
                    return cached;
                }
            }

            int durMs = VOICE_DEFAULT_MS;
            try
            {
                byte[] audioBytes = LoadAudioBytes(soundName);
                if (audioBytes != null && audioBytes.Length > 0)
                {
                GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
                int handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, audioBytes.Length, BASS_SAMPLE_LOOP);
                    if (handle != 0)
                    {
                        long bytes = BASS_ChannelGetLength(handle, 0);
                        if (bytes > 0)
                        {
                            double secs = BASS_ChannelBytes2Seconds(handle, bytes);
                            if (secs > 0.0) durMs = (int)(secs * 1000.0);
                            if (durMs < 50) durMs = 50;
                        }
                        BASS_StreamFree(handle);
                    }
                    if (pinned.IsAllocated) pinned.Free();
                }
            }
            catch { }

            lock (_voiceDurationCache)
            {
                _voiceDurationCache[soundName] = durMs;
            }
            return durMs;
        }

        // Thread-safe helper used both by GetVoiceDurationMs (UI thread) and by the
        // pump thread when a voice finishes, so repeat lookups never re-decode OGG.
        private void CacheVoiceDuration(string soundName, int durMs)
        {
            lock (_voiceDurationCache)
            {
                _voiceDurationCache[soundName] = durMs;
            }
        }

        // Populates the voice-duration cache without blocking the UI thread.
        // Called once at startup so the first Speak() of any known voice does not
        // need to decode its OGG file synchronously on the form's thread.
        public void PreloadVoiceDurations()
        {
            if (!_bassReady) return;
            if (_preloadStarted) return;
            _preloadStarted = true;
            try
            {
                foreach (string f in Directory.GetFiles(_soundDir, "voice_*.ogg"))
                {
                    GetVoiceDurationMs(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch { }
        }

        private byte[] LoadAudioBytes(string soundName)
        {
            byte[] audioBytes = null;
            if (_audioPac != null) audioBytes = _audioPac.GetFileBytes(soundName + ".ogg");
            if (audioBytes == null)
            {
                string oggPath = Path.Combine(_soundDir, soundName + ".ogg");
                if (File.Exists(oggPath)) audioBytes = File.ReadAllBytes(oggPath);
            }
            return audioBytes;
        }

        public void PlaySound(string soundName)
        {
            PlaySoundPitch(soundName, 1.0f);
        }

        private void SchedulePanSweep(int handle, float fromPan, float toPan, float volBase, float freqBase)
        {
            lock (_panSweepLock)
            {
                _panSweeps.RemoveAll(s => s.Handle == handle);
                _panSweeps.Add(new PanSweep
                {
                    Handle = handle,
                    FromPan = fromPan,
                    ToPan = toPan,
                    VolBase = volBase,
                    FreqBase = freqBase,
                    StartMs = _panClock.ElapsedMilliseconds
                });

                if (_panSweepTimer == null)
                {
                    _panSweepTimer = new System.Threading.Timer(PanSweepTick, null, PAN_SWEEP_TICK_MS, PAN_SWEEP_TICK_MS);
                }
            }
        }

        private void PanSweepTick(object state)
        {
            lock (_panSweepLock)
            {
                long now = _panClock.ElapsedMilliseconds;
                for (int i = _panSweeps.Count - 1; i >= 0; i--)
                {
                    PanSweep s = _panSweeps[i];
                    try
                    {
                        if (BASS_ChannelIsActive(s.Handle) == 0)
                        {
                            _panSweeps.RemoveAt(i);
                            continue;
                        }
                    }
                    catch
                    {
                        _panSweeps.RemoveAt(i);
                        continue;
                    }

                    float elapsed = now - s.StartMs;
                    if (elapsed >= PAN_SWEEP_MS)
                    {
                        try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_PAN, s.ToPan); } catch { }
                        try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_VOL, s.VolBase); } catch { }
                        try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_FREQ, s.FreqBase); } catch { }
                        _panSweeps.RemoveAt(i);
                        continue;
                    }

                    float progress = elapsed / (float)PAN_SWEEP_MS;
                    float pan = SpatialAudio.SweepPan(s.FromPan, s.ToPan, progress);
                    try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_PAN, pan); } catch { }

                    if (SpatialProfile == SpatialProfile.Stage2D)
                    {
                        // The gem swells a little and brightens as it crosses
                        // the middle of the glide, like it is sweeping past the
                        // player. The clean profiles glide at a constant volume
                        // and pitch so nothing ever sounds "swollen".
                        float bulge = SpatialAudio.SweepPassBulge(progress);
                        try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_VOL, s.VolBase * bulge); } catch { }
                        try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_FREQ, s.FreqBase * (1.0f + 0.015f * (float)Math.Sin(Math.PI * progress))); } catch { }
                    }
                    else
                    {
                        try { BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_VOL, s.VolBase); } catch { }
                    }
                }

                if (_panSweeps.Count == 0 && _panSweepTimer != null)
                {
                    // Stop the shared sweep timer when there is nothing to glide.
                    // Checked inside with the lock; a new PlaySoundSpatialSweep
                    // creates a fresh timer if it adds a sweep later.
                    if ((_panSweeps.Count == 0))
                    {
                        try { _panSweepTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                        _panSweepTimer = null;
                    }
                }
            }
        }

        public void PlaySoundSpatial(string soundName, int col, int row, float pitchMultiplier = 1.0f)
        {
            bool isVoice = soundName.StartsWith("voice_");
            if (isVoice)
            {
                // Voices are independent from SFX: they obey VoiceVol only
                if (VoiceVol <= 0) return;
                EnqueueVoice(new VoiceRequest { SoundName = soundName, Col = col, Row = row, Pitch = pitchMultiplier, UseSpatial = true });
                return;
            }

            if (SfxVol <= 0) return;

            CleanFinishedSfxChannels();
            StartSfxStream(soundName, col, row, pitchMultiplier, -1);
        }

        // Same as PlaySoundSpatial but the channel glides laterally from the
        // source column to the destination column (a "swipe"), so a gem swap
        // or cascade is heard moving, not just its final position.
        public void PlaySoundSpatialSweep(string soundName, int fromX, int toX, int row, float pitchMultiplier = 1.0f)
        {
            bool isVoice = soundName.StartsWith("voice_");
            if (isVoice)
            {
                // Voices stay centered regardless of the spatial drag.
                if (VoiceVol <= 0) return;
                EnqueueVoice(new VoiceRequest { SoundName = soundName, Col = toX, Row = row, Pitch = pitchMultiplier, UseSpatial = true });
                return;
            }

            if (SfxVol <= 0) return;

            CleanFinishedSfxChannels();
            StartSfxStream(soundName, fromX, row, pitchMultiplier, toX);
        }

        // Creates a one-shot BASS stream for a sound effect and hands the channel
        // to the _activeSfxList. Every failure path frees the handle + GCHandle so
        // nothing leaks when BASS reports an error after StreamCreateFile.
private void StartSfxStream(string soundName, int col, float pitchMultiplier)
        {
            StartSfxStream(soundName, col, -1, pitchMultiplier, -1);
        }

        // sweepToCol >= 0 desplaza el canal lateralmente desde la columna de
        // origen `col` hasta esa columna (glide A->B), para que el movimiento
        // de la gema se oiga y no solo su posicion final. `row` alimenta el
        // plano de profundidad del HRTF (fila 0 = lejos, fila 7 = frente).
        private void StartSfxStream(string soundName, int col, int row, float pitchMultiplier, int sweepToCol)
        {
            if (!_bassReady) return;
            try
            {
                byte[] audioBytes = LoadAudioBytes(soundName);
                if (audioBytes == null || audioBytes.Length == 0) return;

                GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
                int handle = 0;
                try
                {
                    // No AUTOFREE: CleanFinishedSfxChannels owns the channel lifecycle.
                    handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, audioBytes.Length, 0);
                    if (handle == 0)
                    {
                        LogAudioError("SFX '" + soundName + "' StreamCreateFile fallo, err=" + BASS_LastError());
                        return;
                    }

                    // Depth plane: a gem in the back rows (0..2) is quieter,
                    // darker and closer to the center; the front rows keep the
                    // full presence. Non-positional sounds (row < 0) stay flat.
                    // The depth plane is a Stage2D theatrical effect: the clean
                    // profiles keep every row at full presence and pan only by
                    // column, so no gem is ever "pushed away" from the player.
                    bool stage2dDepth = (SpatialProfile == SpatialProfile.Stage2D);
                    float depthVol = stage2dDepth ? SpatialAudio.DepthVolumeForRow(row) : 1.0f;
                    float panScale = stage2dDepth ? SpatialAudio.DepthPanScaleForRow(row) : 1.0f;
                    float depthPitch = stage2dDepth ? SpatialAudio.DepthPitchForRow(row) : 1.0f;

                    BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, (float)SfxVol / 100.0f * depthVol);

                    // Bejeweled-adapted HRTF: a sound without a column stays
                    // centered (col=-1 => SpatialAudio.PanColumn) and a slide
                    // animates the pan from the source column to the target.
                    float fromPan = 0.0f;
                    if (SpatialBinauralEnabled)
                    {
                        fromPan = SpatialAudio.PanColumn(col) * panScale;
                        BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, fromPan);
                    }
                    else
                    {
                        BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, 0.0f);
                    }

                    float effectivePitch = pitchMultiplier * depthPitch;
                    float currentFreq = 44100.0f;
                    if (Math.Abs(effectivePitch - 1.0f) > 0.01f)
                    {
                        if (BASS_ChannelGetAttribute(handle, BASS_ATTRIB_FREQ, ref currentFreq))
                        {
                            BASS_ChannelSetAttribute(handle, BASS_ATTRIB_FREQ, currentFreq * effectivePitch);
                        }
                    }
                    else if (BASS_ChannelGetAttribute(handle, BASS_ATTRIB_FREQ, ref currentFreq))
                    {
                        // Just read the real frequency for the swipe's doppler.
                    }

                    BASS_ChannelPlay(handle, true);

                    lock (_activeSfxList)
                    {
                        // Clean up any channels that have already finished so the
                        // cap below never needs to cut a sound mid-playback.
                        for (int i = _activeSfxList.Count - 1; i >= 0; i--)
                        {
                            if (BASS_ChannelIsActive(_activeSfxList[i].Handle) == 0)
                            {
                                ActiveSfx done = _activeSfxList[i];
                                _activeSfxList.RemoveAt(i);
                                if (done.Pin.IsAllocated) { try { done.Pin.Free(); } catch { } }
                                try { BASS_StreamFree(done.Handle); } catch { }
                            }
                        }

                        if (_activeSfxList.Count >= 25)
                        {
                            // Cap reached with 25+ sounds still playing: stop the
                            // oldest effect so the newest sound is always heard.
                            // A short cut is less damaging for an accessible game
                            // than silently swallowing the requested sound.
                            ActiveSfx oldest = _activeSfxList[0];
                            _activeSfxList.RemoveAt(0);
                            try { BASS_ChannelStop(oldest.Handle); } catch { }
                            if (oldest.Pin.IsAllocated) { try { oldest.Pin.Free(); } catch { } }
                            try { BASS_StreamFree(oldest.Handle); } catch { }
                            _activeSfxList.Add(new ActiveSfx { Handle = handle, Pin = pinned, IsVoice = false });
                        }
                        else
                        {
                            _activeSfxList.Add(new ActiveSfx { Handle = handle, Pin = pinned, IsVoice = false });
                        }
                    }

                    // Animate the pan toward the destination column so the gem
                    // movement is heard as a lateral glide (swipe), not a click.
                    // The glide also swells volume/frequency at mid-flight.
                    if (SpatialBinauralEnabled && sweepToCol >= 0 && sweepToCol != col)
                    {
                        float toPan = SpatialAudio.PanColumn(sweepToCol) * panScale;
                        if (SpatialProfile == SpatialProfile.SimplePan)
                        {
                            // Simple profile: place the sound at the destination
                            // column instantly, no animated glide at all.
                            BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, toPan);
                        }
                        else
                        {
                            SchedulePanSweep(handle, fromPan, toPan, (float)SfxVol / 100.0f * depthVol, currentFreq);
                        }
                    }
                }
                catch
                {
                    // Failed half-way through: release the stream and the pin so
                    // the BASS handle / GCHandle cannot linger for the whole session.
                    if (handle != 0)
                    {
                        try { BASS_StreamFree(handle); } catch { }
                    }
                    if (pinned.IsAllocated)
                    {
                        try { pinned.Free(); } catch { }
                    }
                }
            }
            catch { }
        }

        public void PlaySoundPitch(string soundName, float pitchMultiplier)
        {
            bool isVoice = soundName.StartsWith("voice_");
            if (isVoice)
            {
                // Voices are independent from SFX volume
                if (VoiceVol <= 0) return;
                EnqueueVoice(new VoiceRequest { SoundName = soundName, Pitch = pitchMultiplier, UseSpatial = false });
                return;
            }

            if (SfxVol <= 0) return;

            CleanFinishedSfxChannels();
            StartSfxStream(soundName, -1, -1, pitchMultiplier, -1);
        }

        public void PlayMusic(string musicFileName)
        {
            lock (_musicLock)
            {
                StopFadeTimer();
                EnsureMusicMonitor();
                if (_currentMusicChannel == 0)
                {
                    StartMusic(musicFileName);
                    return;
                }

                // Overlap crossfade: start the new track immediately while the
                // old one fades out, so there is never a silent gap between
                // songs (the original chains its .mo3 tracks automatically).
                if (_pendingMusicChannel != 0)
                {
                    FreeChannel(_pendingMusicChannel, _pendingMusicPin);
                }
                _pendingMusicFile = musicFileName;
                _pendingMusicChannel = CreateMusicChannel(musicFileName, out _pendingMusicPin);
                if (_pendingMusicChannel == 0)
                {
                    _pendingMusicFile = null;
                    return;
                }
                _fadingOut = true;
                _fadeVol = (float)MusicVol / 100.0f;
                _fadeVolPerStep = _fadeVol / FADE_STEPS;
                _musicFadeTimer = new System.Threading.Timer(MusicFadeTick, null, FADE_TICK_MS, FADE_TICK_MS);
            }
        }

        private void MusicFadeTick(object state)
        {
            lock (_musicLock)
            {
                if (_currentMusicChannel == 0)
                {
                    FinishMusicSwitch();
                    return;
                }

                if (_fadingOut)
                {
                    // Lower the outgoing track while the pending one rises.
                    _fadeVol -= _fadeVolPerStep;
                    if (_fadeVol <= 0.01f)
                    {
                        _fadeVol = 0.0f;
                        try { BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, 0.0f); } catch { }
                        FinishMusicSwitch();
                        return;
                    }
                    try { BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, _fadeVol); } catch { }
                    if (_pendingMusicChannel != 0)
                    {
                        float up = (float)MusicVol / 100.0f - _fadeVol;
                        try { BASS_ChannelSetAttribute(_pendingMusicChannel, BASS_ATTRIB_VOL, up); } catch { }
                    }
                }
                else
                {
                    _fadeVol += _fadeVolPerStep;
                    if (_fadeVol >= (float)MusicVol / 100.0f)
                    {
                        try { BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, (float)MusicVol / 100.0f); } catch { }
                        // Never self-cancel with the waiting Timer.Dispose(WaitHandle):
                        // that would wait for this very callback to finish = deadlock.
                        StopFadeTimer();
                        return;
                    }
                    try { BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, _fadeVol); } catch { }
                }
            }
        }

        private void FinishMusicSwitch()
        {
            StopFadeTimer();
            _fadingOut = false;
            try
            {
                if (_currentMusicChannel != 0)
                {
                    BASS_ChannelStop(_currentMusicChannel);
                    BASS_StreamFree(_currentMusicChannel);
                    _currentMusicChannel = 0;
                }

                if (_pinnedMusicBytes.IsAllocated)
                {
                    try { _pinnedMusicBytes.Free(); } catch { }
                }
            }
            catch { }

            if (_pendingMusicChannel != 0)
            {
                _currentMusicChannel = _pendingMusicChannel;
                _pinnedMusicBytes = _pendingMusicPin;
                _pendingMusicChannel = 0;
                if (_pendingMusicFile != null)
                {
                    _currentMusicFile = _pendingMusicFile;
                }
            }
            _pendingMusicFile = null;
        }

        // Cancels a pending fade WITHOUT waiting for a running callback. This is
        // the only safe way to cancel from inside MusicFadeTick itself: the
        // waiting Timer.Dispose(WaitHandle) would block until that callback
        // finishes, but the callback is the one calling us -> deadlock.
        private void StopFadeTimer()
        {
            System.Threading.Timer timer = _musicFadeTimer;
            _musicFadeTimer = null;
            if (timer != null)
            {
                try { timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                try { timer.Dispose(); } catch { }
            }
        }

        private void StartMusic(string musicFileName)
        {
            _currentMusicFile = musicFileName;
            int handle = CreateMusicChannel(musicFileName, out _pinnedMusicBytes);
            if (handle == 0)
            {
                _currentMusicFile = null;
                return;
            }
            _currentMusicChannel = handle;

            // Fade-in ramp
            _fadingOut = false;
            _fadeVol = 0.0f;
            _fadeVolPerStep = (float)MusicVol / 100.0f / FADE_STEPS;
            _musicFadeTimer = new System.Threading.Timer(MusicFadeTick, null, FADE_TICK_MS, FADE_TICK_MS);

            EnsureMusicMonitor();
        }

        // Creates a ready-to-play music stream at zero volume. The track is
        // re-chained with a crossfade when it approaches its end (like the
        // original .mo3 files which chain segments automatically); the native
        // BASS loop is a safety net in case the monitor ever misses a chain.
        private int CreateMusicChannel(string musicFileName, out GCHandle pin)
        {
            pin = new GCHandle();
            try
            {
                byte[] audioBytes = null;

                // 1. Load from encrypted audio.pac directly in RAM
                if (_audioPac != null)
                {
                    audioBytes = _audioPac.GetFileBytes(musicFileName);
                }

                // 2. Fallback to unencrypted folder
                if (audioBytes == null)
                {
                    string musicPath = Path.Combine(_musicDir, musicFileName);
                    if (File.Exists(musicPath)) audioBytes = File.ReadAllBytes(musicPath);
                }

                if (audioBytes == null || audioBytes.Length == 0) return 0;

                GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
                int handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, audioBytes.Length, 0);

                if (handle == 0)
                {
                    LogAudioError("Musica StreamCreateFile fallo, err=" + BASS_LastError());
                    if (pinned.IsAllocated)
                    {
                        try { pinned.Free(); } catch { }
                    }
                    return 0;
                }

                // Fade-in starts from silence
                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, 0.0f);
                if (SpatialBinauralEnabled && SpatialProfile == SpatialProfile.Stage2D)
                {
                    // Enveloping 3D atmospheric binaural reverb soundscape for background music
                    BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, 0.0f);

                    int reverbFx = BASS_ChannelSetFX(handle, BASS_FX_DX8_REVERB, 0);
                    if (reverbFx != 0)
                    {
                        BASS_DX8_REVERB rev = new BASS_DX8_REVERB
                        {
                            fInGain = 0.0f,
                            fReverbMix = -5.0f,
                            fReverbTime = 1400.0f,
                            fHighFreqRttHFRatio = 0.001f
                        };
                        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(rev));
                        Marshal.StructureToPtr(rev, ptr, false);
                        BASS_FXSetParameters(reverbFx, ptr);
                        Marshal.FreeHGlobal(ptr);
                    }
                }
                BASS_ChannelPlay(handle, true);

                pin = pinned;
                return handle;
            }
            catch
            {
                if (pin.IsAllocated)
                {
                    try { pin.Free(); } catch { }
                }
                return 0;
            }
        }

        private void FreeChannel(int handle, GCHandle pin)
        {
            try
            {
                if (handle != 0)
                {
                    BASS_ChannelStop(handle);
                    BASS_StreamFree(handle);
                }
            }
            catch { }
            if (pin.IsAllocated)
            {
                try { pin.Free(); } catch { }
            }
        }

        // Chains the current track into itself (or the next one) with a short
        // overlap crossfade as it approaches its end. The last ~0.7-1 s of the
        // converted tracks is silence, which a raw BASS_SAMPLE_LOOP would play
        // as a pause on every repeat; crossfading over that tail keeps the
        // music continuous, like the original's .mo3 auto-chain segments.
        private const int MUSIC_CHAIN_LOOKAHEAD_MS = 1200;
        private System.Threading.Timer _musicMonitorTimer;
        private string _currentMusicFile = null;

        private void EnsureMusicMonitor()
        {
            if (_musicMonitorTimer == null)
            {
                _musicMonitorTimer = new System.Threading.Timer(MusicMonitorTick, null, 200, 200);
            }
        }

        private void MusicMonitorTick(object state)
        {
            lock (_musicLock)
            {
                if (_currentMusicChannel == 0 || _currentMusicFile == null) return;
                if (_pendingMusicChannel != 0 || _fadingOut || _musicFadeTimer != null) return;

                try
                {
                    // Safety net: if the channel reached its natural end without
                    // being re-chained (short track, hiccup), replay it now.
                    if (BASS_ChannelIsActive(_currentMusicChannel) == 0)
                    {
                        PlayMusic(_currentMusicFile);
                        if (MusicRechained != null)
                        {
                            try { MusicRechained(this, EventArgs.Empty); } catch { }
                        }
                        return;
                    }

                    long len = BASS_ChannelGetLength(_currentMusicChannel, BASS_POS_BYTE);
                    if (len <= 0) return;
                    long pos = BASS_ChannelGetPosition(_currentMusicChannel, BASS_POS_BYTE);
                    double remainingSec = BASS_ChannelBytes2Seconds(_currentMusicChannel, len - pos);
                    if (remainingSec < 0.0) return;
                    if (remainingSec <= MUSIC_CHAIN_LOOKAHEAD_MS / 1000.0)
                    {
                        // Re-chain the same track with an overlap crossfade so
                        // the silent tail of the file is never heard.
                        PlayMusic(_currentMusicFile);
                        if (MusicRechained != null)
                        {
                            try { MusicRechained(this, EventArgs.Empty); } catch { }
                        }
                    }
                }
                catch { }
            }
        }

        public void UpdateSpatialAudioState()
        {
            if (_currentMusicChannel != 0)
            {
                try
                {
                    if (_musicReverbFx != 0)
                    {
                        BASS_ChannelRemoveFX(_currentMusicChannel, _musicReverbFx);
                        _musicReverbFx = 0;
                    }

                    if (SpatialBinauralEnabled && SpatialProfile == SpatialProfile.Stage2D)
                    {
                        _musicReverbFx = BASS_ChannelSetFX(_currentMusicChannel, BASS_FX_DX8_REVERB, 0);
                        if (_musicReverbFx != 0)
                        {
                            BASS_DX8_REVERB rev = new BASS_DX8_REVERB
                            {
                                fInGain = 0.0f,
                                fReverbMix = -5.0f,
                                fReverbTime = 1400.0f,
                                fHighFreqRttHFRatio = 0.001f
                            };
                            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(rev));
                            Marshal.StructureToPtr(rev, ptr, false);
                            BASS_FXSetParameters(_musicReverbFx, ptr);
                            Marshal.FreeHGlobal(ptr);
                        }
                    }
                }
                catch { }
            }
        }

        public void UpdateMusicVolume()
        {
            if (_currentMusicChannel != 0)
            {
                try
                {
                    // Don't override the volume while a fade is in progress
                    if (_musicFadeTimer == null)
                        BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, (float)MusicVol / 100.0f);
                }
                catch { }
            }
        }

        public void StopMusic()
        {
            lock (_musicLock)
            {
                _pendingMusicFile = null;
                _currentMusicFile = null;
                StopFadeTimer();

                FreeChannel(_pendingMusicChannel, _pendingMusicPin);
                _pendingMusicChannel = 0;

                try
                {
                    if (_currentMusicChannel != 0)
                    {
                        BASS_ChannelStop(_currentMusicChannel);
                        BASS_StreamFree(_currentMusicChannel);
                        _currentMusicChannel = 0;
                    }

                    if (_pinnedMusicBytes.IsAllocated)
                    {
                        try { _pinnedMusicBytes.Free(); } catch { }
                    }
                }
                catch { }
            }
        }

        public void Dispose()
        {
            StopMusic();

            if (_musicMonitorTimer != null)
            {
                try { _musicMonitorTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                try { _musicMonitorTimer.Dispose(); } catch { }
                _musicMonitorTimer = null;
            }

            System.Threading.Timer pumpTimer = null;
            lock (_voiceLock)
            {
                try { if (_activeVoiceHandle != 0) BASS_ChannelStop(_activeVoiceHandle); } catch { }
                try { if (_activeVoiceHandle != 0) BASS_StreamFree(_activeVoiceHandle); } catch { }
                if (_activeVoicePin.IsAllocated)
                {
                    try { _activeVoicePin.Free(); } catch { }
                }
                _activeVoiceHandle = 0;
                _activeVoiceEndAt = DateTime.MinValue;
                _voiceQueue.Clear();
                pumpTimer = _voicePumpTimer;
                _voicePumpTimer = null;
                if (pumpTimer != null)
                {
                    // Stop new callbacks first; the wait below happens OUTSIDE
                    // the lock so an in-flight VoicePumpTick (which also takes
                    // _voiceLock) can finish instead of deadlocking with us.
                    try { pumpTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                }
            }

            // Dispose(WaitHandle) also waits for a callback that is currently
            // running, so a callback can never touch BASS after BASS_Free() below.
            if (pumpTimer != null)
            {
                try
                {
                    using (var evt = new System.Threading.ManualResetEvent(false))
                    {
                        pumpTimer.Dispose(evt);
                        evt.WaitOne();
                    }
                }
                catch { }
            }

            // Stop the pan-sweep timer before BASS is freed; channels are
            // already disposed by the ActiveSfx loop, so just drop the timer.
            if (_panSweepTimer != null)
            {
                try { _panSweepTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                try { _panSweepTimer.Dispose(); } catch { }
                _panSweepTimer = null;
            }
            lock (_panSweepLock) { _panSweeps.Clear(); }

            try
            {
                lock (_activeSfxList)
                {
                    foreach (var sfx in _activeSfxList)
                    {
                        if (sfx.Pin.IsAllocated)
                        {
                            try { sfx.Pin.Free(); } catch { }
                        }
                        BASS_StreamFree(sfx.Handle);
                    }
                    _activeSfxList.Clear();
                }
                BASS_Free();
            }
            catch { }

            if (_audioPac != null)
            {
                try { _audioPac.Dispose(); } catch { }
            }
        }
    }
}
