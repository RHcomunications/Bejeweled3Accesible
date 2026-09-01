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
        private static extern int BASS_StreamCreate(uint freq, uint chans, uint flags,
            [MarshalAs(UnmanagedType.FunctionPtr)] BassStreamProc proc, IntPtr user);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelPlay(int handle, bool restart);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelStop(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_StreamFree(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelSetAttribute(int handle, int attrib, float value);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelSlideAttribute(int handle, int attrib, float value, int length);

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

        private const uint BASS_SAMPLE_LOOP = 4;
        private const uint BASS_SAMPLE_FLOAT = 0x100;
        private const int BASS_ATTRIB_FREQ = 1;
        private const int BASS_ATTRIB_VOL = 2;
        private const int BASS_ATTRIB_PAN = 3;

        // --- BASS FX (add-on bass_fx.dll): pitch natural sin resample ---
        // Solo se usa si el DLL esta disponible; si no, el juego degrada a las
        // variantes pre-renderizadas con rubberband (gem_hit_p*).
        [DllImport("bass_fx.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_FX_GetVersion();

        private const int BASS_ATTRIB_TEMPO_PITCH = 0x10001;

        private static int _bassFxState = 0; // 0=no probado, 1=OK, -1=fallo
        internal bool BassFxAvailable
        {
            get
            {
                if (_bassFxState == 0)
                {
                    try { int v = BASS_FX_GetVersion(); _bassFxState = (v != 0) ? 1 : -1; }
                    catch { _bassFxState = -1; }
                }
                return _bassFxState == 1;
            }
        }

        // Callback con el que BASS pide PCM de la musica del modulo real
        // (BASS_StreamCreate con STREAMPROC). Devuelve bytes escritos.
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint BassStreamProc(int handle, IntPtr buffer, uint length, IntPtr user);

        // Callback DSP de BASS: recibe el buffer de PCM ya mezclado del canal y
        // lo sustituye por la salida del GridSpatializer (musica como ambiente).
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void DspProc(int handle, int channel, IntPtr buffer, int length, IntPtr user);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelGetInfo(int handle, out BassChannelInfo info);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelSetDSP(int handle, [MarshalAs(UnmanagedType.FunctionPtr)] DspProc proc, IntPtr user, int priority);

        [StructLayout(LayoutKind.Sequential)]
        private struct BassChannelInfo
        {
            public int freq;
            public int chans;
            public int flags;
            public uint ctype;
            public IntPtr filename;
        }

        // Efectos DX8 de BASS (baSS core, no requieren bass_fx.dll) usados para
        // robustecer el audio binaural: ecualizacion por fila, reverb maestro
        // de "vacío espacial" y compresión/limitación para absorber picos.
        private const int BASS_FX_DX8_COMPRESSOR = 1;
        private const int BASS_FX_DX8_PARAMEQ = 7;
        private const int BASS_FX_DX8_REVERB = 8;

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_ChannelSetFX(int handle, int type, int priority);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_FXSetParameters(int fx, IntPtr paramsPtr);

        [StructLayout(LayoutKind.Sequential)]
        private struct BassDx8Parameq
        {
            public float fCenter;     // frecuencia central (Hz)
            public float fBandwidth;  // ancho de banda (semitonos, >= 1)
            public float fGain;       // ganancia (dB, -15..+15)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BassDx8Reverb
        {
            public float fInGain;
            public float fReverbMix;  // mezcla (dB, -96..0; -96 = nada)
            public float fReverbTime; // decaimiento (ms)
            public float fHighFreqRTRatio;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BassDx8Compressor
        {
            public float fGain;
            public float fAttack;
            public float fRelease;
            public float fThreshold;
            public float fRatio;
            public float fPredelay;
        }

        // Audio binaural (GridSpatializer + FX de atmósfera). Se puede
        // deshabilitar desde Opciones: al apagarlo, la música y los SFX suenan
        // secos y centrados como el audio clásico del juego.
        public bool BinauralEnabled { get; set; }

        private readonly string _soundDir;
        private readonly string _musicDir;
        private readonly string _logDir;
        private readonly PacReader _audioPac;

        private bool _bassReady = false;

        private int _currentMusicChannel = 0;
        private GCHandle _pinnedMusicBytes;
        private readonly List<ActiveSfx> _activeSfxList = new List<ActiveSfx>();

        // Reproduccion del modulo real (Bejeweled3_suite.mo3): libopenmpt
        // decodifica y entrega el PCM a BASS_StreamCreate (STREAMPROC), de modo
        // que ducking, fades y reverb siguen aplicandose igual que a cualquier
        // otra pista. El callback solo marca un flag: los eventos MusicRechained
        // se disparan desde el monitor para no llamar a BASS desde su propio hilo.
        private ModuleMusicPlayer _currentModulePlayer;
        private ModuleMusicPlayer _pendingModulePlayer;
        private readonly BassStreamProc _moduleStreamProc;
        private volatile bool _moduleEventPending;

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

        // Musica como ambiente: la pista se espacializa con el mismo
        // GridSpatializer binaural que los SFX del tablero, pero tratando los
        // canales L y R como dos fuentes simetricas para conservar el estereo
        // original y envolver al juego con aire/profundidad en lugar de sonar
        // plana y centrada. Se instala un DSP sobre el canal de musica (modulo
        // y archivo); el crossfade y el reencadenado no se ven afectados.
        private float _musicOriginalVolume = 1.0f;
        private System.Threading.Timer _musicDuckTimer;

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
                    // No cortar la voz si BASS sigue reproduciendo (p.ej. con
                    // pitch < 1 la voz dura mas de lo planificado). Timeout de
                    // seguridad de 3 s para no colgar el pump si el canal se muere.
                    bool stillPlaying = false;
                    try { stillPlaying = BASS_ChannelIsActive(_activeVoiceHandle) != 0; } catch { }
                    if (stillPlaying && DateTime.UtcNow < _activeVoiceEndAt.AddMilliseconds(3000)) return;

                    long playedBytes = 0;
                    try { playedBytes = BASS_ChannelGetPosition(_activeVoiceHandle, BASS_POS_BYTE); } catch { }
                    // Si BASS ya no esta activo, la voz llego a su final natural:
                    // contabilizarla como reproducida por completo (el ultimo buffer
                    // puede quedar unas muestras por debajo del 100%).
                    if (!stillPlaying && _activeVoiceLengthBytes > 0) playedBytes = _activeVoiceLengthBytes;
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
                        // Nothing left to speak: let the music come back.
                        SetDuckTarget(1.0f);
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
                // The announcer is on air: duck the music while she talks.
                SetDuckTarget(MUSIC_DUCK_FACTOR);
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
            public SpatialSfxSource Spatial;
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

        // (El posicionamiento del tablero es un modelo unico siempre activo;
        // no hay perfiles ni motor de objetos 3D ni DSP de atmosfera musical.)

        private struct PanSweep
        {
            public int SourceHandle;
            public float FromPan;
            public float ToPan;
            public long StartMs;
            public float FromDepth;
            public float ToDepth;
            public SpatialSfxSource Source;
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

            // Callback de BASS_StreamCreate para la musica del modulo: se
            // conserva en un campo para que el GC nunca lo recolecte.
            _moduleStreamProc = ModuleStreamProc;

            // Audio espacial binaural eliminado: la musica suena plana y centrada
            // (paneo estereo simple), sin envoltura de sala. Los SFX se panearan
            // por columna segun BinauralEnabled (simple, no agresivo).
            BinauralEnabled = true;
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
                            if (sfx.Spatial != null)
                            {
                                // La fuente espacial libera el decodificador, el
                                // pin del OGG y el stream de salida.
                                try { sfx.Spatial.Dispose(); } catch { }
                            }
                            else
                            {
                                if (sfx.Pin.IsAllocated)
                                {
                                    try { sfx.Pin.Free(); } catch { }
                                }
                                BASS_StreamFree(sfx.Handle);
                            }
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
            SetDuckTarget(1.0f);
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

        // Music ducking: while a locution (voice_*) is sounding, the music
        // backs off (35% of its current volume, so it stays clearly audible
        // under the announcer instead of almost disappearing) and returns
        // gently. The ramps are intentionally smooth: a slow attack glides
        // down without a "drop" and a much slower release avoids the music
        // popping back up when the locution ends.
        private const float MUSIC_DUCK_FACTOR = 0.35f;
        private const int DUCK_TICK_MS = 25;
        private const float DUCK_ATTACK_STEP = 0.05f;
        private const float DUCK_RELEASE_STEP = 0.02f;
        private readonly object _duckLock = new object();
        private float _duckCurrent = 1.0f;
        private float _duckTarget = 1.0f;
        private System.Threading.Timer _duckTimer;

        // True while the music should be (or is being) lowered for a locution.
        internal bool MusicDucked
        {
            get { lock (_duckLock) { return _duckTarget < 0.99f; } }
        }

        private void SetDuckTarget(float target)
        {
            lock (_duckLock)
            {
                _duckTarget = target;
                if (_duckTimer == null && Math.Abs(_duckCurrent - _duckTarget) > 0.001f)
                {
                    _duckTimer = new System.Threading.Timer(DuckTick, null, DUCK_TICK_MS, DUCK_TICK_MS);
                }
            }
        }

        private void DuckTick(object state)
        {
            float apply = 1.0f;
            lock (_duckLock)
            {
                if (_duckCurrent < _duckTarget)
                    _duckCurrent = Math.Min(_duckTarget, _duckCurrent + DUCK_RELEASE_STEP);
                else if (_duckCurrent > _duckTarget)
                    _duckCurrent = Math.Max(_duckTarget, _duckCurrent - DUCK_ATTACK_STEP);

                if (Math.Abs(_duckCurrent - _duckTarget) < 0.001f)
                {
                    _duckCurrent = _duckTarget;
                    if (_duckTimer != null)
                    {
                        try { _duckTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                        try { _duckTimer.Dispose(); } catch { }
                        _duckTimer = null;
                    }
                }
                apply = _duckCurrent;
            }

            lock (_musicLock)
            {
                if (_currentMusicChannel != 0 && _musicFadeTimer == null)
                {
                    try { BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, MusicChannelVolume(apply)); } catch { }
                }
            }
        }

        // The music channel volume including the current duck factor. Every
        // place that sets the music volume (duck tick, volume changes, fades)
        // goes through here so the duck is never lost when a track change
        // lands while a locution is on air.
        private float MusicChannelVolume(float duck)
        {
            return (float)MusicVol / 100.0f * duck;
        }

        // Current duck level, 1.0 = full music volume, MUSIC_DUCK_FACTOR = deep.
        internal float DuckCurrentLevel
        {
            get { lock (_duckLock) { return _duckCurrent; } }
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
                    // Pure measurement handle: no BASS_SAMPLE_LOOP (the stream is
                    // never played, only measured and freed immediately).
                    int handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, audioBytes.Length, 0);
                    if (handle != 0)
                    {
                        long bytes = BASS_ChannelGetLength(handle, BASS_POS_BYTE);
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

        // Glide espacial: anima Pan (L/R) y Profundidad (frente/fondo) del
        // renderizador de la fuente. Sustituye tanto el paneo clasico como el
        // antiguo glide binaural (azimuth). Siempre espacial, sin perfiles.
        private void ScheduleSfxSweep(SpatialSfxSource source, float fromPan, float fromDepth, float toPan, float toDepth)
        {
            lock (_panSweepLock)
            {
                _panSweeps.RemoveAll(s => s.SourceHandle == source.OutputHandle);
                _panSweeps.Add(new PanSweep
                {
                    SourceHandle = source.OutputHandle,
                    FromPan = fromPan,
                    ToPan = toPan,
                    FromDepth = fromDepth,
                    ToDepth = toDepth,
                    StartMs = _panClock.ElapsedMilliseconds,
                    Source = source
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
                        if (BASS_ChannelIsActive(s.SourceHandle) == 0)
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
                        try { BASS_ChannelSlideAttribute(s.SourceHandle, BASS_ATTRIB_PAN, s.ToPan, 0); } catch { }
                        _panSweeps.RemoveAt(i);
                        continue;
                    }

                    float progress = elapsed / (float)PAN_SWEEP_MS;
                    if (progress > 1.0f) progress = 1.0f; else if (progress < 0.0f) progress = 0.0f;
                    float pan = SpatialAudio.SweepPan(s.FromPan, s.ToPan, progress);
                    try { BASS_ChannelSlideAttribute(s.SourceHandle, BASS_ATTRIB_PAN, pan, PAN_SWEEP_TICK_MS); } catch { }
                }

                if (_panSweeps.Count == 0 && _panSweepTimer != null)
                {
                    // Stop the shared sweep timer when there is nothing to glide.
                    // A new PlaySoundSpatialSweep creates a fresh timer if it
                    // adds a sweep later.
                    try { _panSweepTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                    try { _panSweepTimer.Dispose(); } catch { }
                    _panSweepTimer = null;
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

        // Reproduce un efecto posicionado por PAN (L/R, -1..+1) y PROFUNDIDAD
        // (fila: 0 frente .. 1 fondo). Unico modelo espacial, siempre activo y
        // sin perfiles. Lo usa la Escuela de Audio para demostrar el paneo.
        public void PlaySoundSpatialPan(float pan, float depthFar, string soundName, float pitchMultiplier = 1.0f)
        {
            if (SfxVol <= 0) return;
            CleanFinishedSfxChannels();

            byte[] audioBytes = LoadAudioBytes(soundName);
            if (audioBytes == null || audioBytes.Length == 0) return;
            GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
            try
            {
                float depth = (depthFar < 0.0f) ? 0.0f : depthFar;
                SpatialSfxSource source = new SpatialSfxSource(this, audioBytes, pinned, -1, pan, depth, -1, BinauralEnabled);
                int handle = source.OutputHandle;
                if (handle == 0)
                {
                    try { source.Dispose(); } catch { }
                    return;
                }
                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, (float)SfxVol / 100.0f);
                if (Math.Abs(pitchMultiplier - 1.0f) > 0.01f)
                {
                    float currentFreq = 44100.0f;
                    if (BASS_ChannelGetAttribute(handle, BASS_ATTRIB_FREQ, ref currentFreq))
                        BASS_ChannelSetAttribute(handle, BASS_ATTRIB_FREQ, currentFreq * pitchMultiplier);
                }
                BASS_ChannelPlay(handle, true);
                RegisterSfx(source, handle, pinned);
            }
            catch
            {
                if (pinned.IsAllocated) { try { pinned.Free(); } catch { } }
            }
        }

        // Barrido espacial: desliza PAN y PROFUNDIDAD de A a B, para que la
        // Escuela de Audio demuestre el movimiento lateral (L<->R) y de
        // profundidad (frente<->fondo).
        public void PlaySoundSpatialSweepPan(float fromPan, float toPan, float fromDepth, float toDepth, string soundName, float pitchMultiplier = 1.0f)
        {
            if (SfxVol <= 0) return;
            CleanFinishedSfxChannels();

            byte[] audioBytes = LoadAudioBytes(soundName);
            if (audioBytes == null || audioBytes.Length == 0) return;
            GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
            try
            {
                SpatialSfxSource source = new SpatialSfxSource(this, audioBytes, pinned, -1, fromPan, fromDepth, -1, BinauralEnabled);
                int handle = source.OutputHandle;
                if (handle == 0)
                {
                    try { source.Dispose(); } catch { }
                    return;
                }
                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, (float)SfxVol / 100.0f);
                if (Math.Abs(pitchMultiplier - 1.0f) > 0.01f)
                {
                    float currentFreq = 44100.0f;
                    if (BASS_ChannelGetAttribute(handle, BASS_ATTRIB_FREQ, ref currentFreq))
                        BASS_ChannelSetAttribute(handle, BASS_ATTRIB_FREQ, currentFreq * pitchMultiplier);
                }
                BASS_ChannelPlay(handle, true);
                RegisterSfx(source, handle, pinned);
                if (BinauralEnabled) ScheduleSfxSweep(source, fromPan, fromDepth, toPan, toDepth);
            }
            catch
            {
                if (pinned.IsAllocated) { try { pinned.Free(); } catch { } }
            }
        }

        // Registra una fuente espacial en la lista activa: limpia canales ya
        // terminados y, si se supera el tope, corta el mas antiguo para que el
        // sonido nuevo siempre suene. Libera el decodificador/pin del cortado.
        private void RegisterSfx(SpatialSfxSource source, int handle, GCHandle pinned)
        {
            lock (_activeSfxList)
            {
                for (int i = _activeSfxList.Count - 1; i >= 0; i--)
                {
                    if (BASS_ChannelIsActive(_activeSfxList[i].Handle) == 0)
                    {
                        ActiveSfx done = _activeSfxList[i];
                        _activeSfxList.RemoveAt(i);
                        if (done.Spatial != null) { try { done.Spatial.Dispose(); } catch { } }
                        else
                        {
                            if (done.Pin.IsAllocated) { try { done.Pin.Free(); } catch { } }
                            try { BASS_StreamFree(done.Handle); } catch { }
                        }
                    }
                }
                if (_activeSfxList.Count >= 25)
                {
                    ActiveSfx oldest = _activeSfxList[0];
                    _activeSfxList.RemoveAt(0);
                    try { BASS_ChannelStop(oldest.Handle); } catch { }
                    if (oldest.Spatial != null) { try { oldest.Spatial.Dispose(); } catch { } }
                    else
                    {
                        if (oldest.Pin.IsAllocated) { try { oldest.Pin.Free(); } catch { } }
                        try { BASS_StreamFree(oldest.Handle); } catch { }
                    }
                }
                _activeSfxList.Add(new ActiveSfx { Handle = handle, Pin = pinned, IsVoice = false, Spatial = source });
            }
        }

        // Creates a one-shot BASS stream for a sound effect and hands the channel
        // to the _activeSfxList. Every failure path frees the handle + GCHandle so
        // nothing leaks when BASS reports an error after StreamCreateFile.
        private void StartSfxStream(string soundName, int col, float pitchMultiplier, float? fxSemitones = null)
        {
            StartSfxStream(soundName, col, -1, pitchMultiplier, -1, fxSemitones);
        }

        // sweepToCol >= 0 desplaza el canal lateralmente desde la columna de
        // origen `col` hasta esa columna (glide A->B), para que el movimiento
        // de la gema se oiga y no solo su posicion final. `row` alimenta el
        // plano de profundidad (fila 0 = lejos, fila 7 = frente). Siempre
        // espacial (sin perfiles): pan por columna + profundidad por fila, con
        // volumen/aire/anchura ya aplicados por el GridSpatializer.
        // fxSemitones (en semitonos) usa BASS FX para un pitch natural; si es
        // null, el pitch se hace por resample (BASS_ATTRIB_FREQ) cuando aplica.
        private void StartSfxStream(string soundName, int col, int row, float pitchMultiplier, int sweepToCol, float? fxSemitones = null)
        {
            if (!_bassReady) return;
            byte[] audioBytes = LoadAudioBytes(soundName);
            if (audioBytes == null || audioBytes.Length == 0) return;

            GCHandle pinned = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
            try
            {
                float pan = SpatialAudio.PanColumn(col);
                float depth = (row < 0) ? 0.0f : SpatialAudio.DepthForRow(row);
                SpatialSfxSource source = new SpatialSfxSource(this, audioBytes, pinned, col, pan, depth, row, BinauralEnabled, fxSemitones);
                int handle = source.OutputHandle;
                if (handle == 0)
                {
                    try { source.Dispose(); } catch { }
                    return;
                }

                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, (float)SfxVol / 100.0f);
                if (Math.Abs(pitchMultiplier - 1.0f) > 0.01f && !fxSemitones.HasValue)
                {
                    float currentFreq = 44100.0f;
                    if (BASS_ChannelGetAttribute(handle, BASS_ATTRIB_FREQ, ref currentFreq))
                        BASS_ChannelSetAttribute(handle, BASS_ATTRIB_FREQ, currentFreq * pitchMultiplier);
                }
                BASS_ChannelPlay(handle, true);
                RegisterSfx(source, handle, pinned);

                // Animate the pan toward the destination column so the gem
                // movement is heard as a lateral glide (swipe); the depth stays
                // fixed on the same row.
                if (BinauralEnabled && sweepToCol >= 0 && sweepToCol != col)
                {
                    ScheduleSfxSweep(source, pan, depth, SpatialAudio.PanColumn(sweepToCol), depth);
                }
            }
            catch
            {
                if (pinned.IsAllocated) { try { pinned.Free(); } catch { } }
            }
        }

        // (El antiguo camino binaural/HRTF por perfil fue eliminado: ahora todo
        // efecto del tablero usa el unico render espacial GridSpatializer.)

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

        // Reproduce el golpe de gema en cascada con pitch natural usando las
        // variantes pre-renderizadas con rubberband (gem_hit_pN): conservan el
        // timbre original (sin el lowpass que introduce BASS FX al hacer
        // time-stretch) y suben un semitono por nivel de cascada (acotado a 1 octava).
        public void PlayGemHitSweep(int fromX, int toX, int row, float semitones)
        {
            if (SfxVol <= 0) return;
            CleanFinishedSfxChannels();

            int s = (int)Math.Round(semitones);
            if (s < 0) s = 0; else if (s > 12) s = 12;
            PlaySoundSpatialSweep("gem_hit_p" + s.ToString(), fromX, toX, row, 1.0f);
        }

        public void PlayMusic(string musicFileName)
        {
            float duck = DuckCurrentLevel;
            lock (_musicLock)
            {
                StopFadeTimer();
                EnsureMusicMonitor();

                // El modulo ya suena en esta misma cancion (p. ej. las 4 partes
                // de Clasico o Zen comparten offset): no reiniciar la musica,
                // el propio modulo la hace evolucionar como en el juego real.
                int order = MusicMap.OrderForFile(musicFileName);
                if (order >= 0 && _currentMusicChannel != 0 && _currentModulePlayer != null
                    && _pendingMusicChannel == 0 && _currentMusicFile != null
                    && MusicMap.OrderForFile(_currentMusicFile) == order && MusicChannelActive)
                {
                    return;
                }

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
                    if (_pendingModulePlayer != null)
                    {
                        try { _pendingModulePlayer.Dispose(); } catch { }
                        _pendingModulePlayer = null;
                    }
                }
                _pendingMusicFile = musicFileName;
                _pendingMusicChannel = CreateChannelFor(musicFileName, out _pendingMusicPin, out _pendingModulePlayer);
                if (_pendingMusicChannel == 0)
                {
                    _pendingMusicFile = null;
                    _pendingModulePlayer = null;
                    return;
                }
                _fadingOut = true;
                _fadeVol = MusicChannelVolume(duck);
                _fadeVolPerStep = _fadeVol / FADE_STEPS;
                _musicFadeTimer = new System.Threading.Timer(MusicFadeTick, null, FADE_TICK_MS, FADE_TICK_MS);
            }
        }

        private void MusicFadeTick(object state)
        {
            float duck = DuckCurrentLevel;
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
                        // Si el duck baja durante el crossfade, MusicChannelVolume(duck)
                        // puede quedarse por debajo de _fadeVol: acotar a [0, target].
                        float target = MusicChannelVolume(duck);
                        float up = Math.Max(0.0f, target - _fadeVol);
                        try { BASS_ChannelSetAttribute(_pendingMusicChannel, BASS_ATTRIB_VOL, up); } catch { }
                    }
                }
                else
                {
                    _fadeVol += _fadeVolPerStep;
                    if (_fadeVol >= MusicChannelVolume(duck))
                    {
                        try { BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, MusicChannelVolume(duck)); } catch { }
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

            if (_currentModulePlayer != null)
            {
                try { _currentModulePlayer.Dispose(); } catch { }
                _currentModulePlayer = null;
            }

            if (_pendingMusicChannel != 0)
            {
                _currentMusicChannel = _pendingMusicChannel;
                _pinnedMusicBytes = _pendingMusicPin;
                _currentModulePlayer = _pendingModulePlayer;
                _pendingModulePlayer = null;
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
            int handle = CreateChannelFor(musicFileName, out _pinnedMusicBytes, out _currentModulePlayer);
            if (handle == 0)
            {
                _currentMusicFile = null;
                if (_currentModulePlayer != null)
                {
                    try { _currentModulePlayer.Dispose(); } catch { }
                    _currentModulePlayer = null;
                }
                return;
            }
            _currentMusicChannel = handle;

            // Fade-in ramp (respects an active duck so a voice speaking when a
            // track starts is never covered by full-volume music).
            _fadingOut = false;
            _fadeVol = 0.0f;
            _fadeVolPerStep = MusicChannelVolume(DuckCurrentLevel) / FADE_STEPS;
            _musicFadeTimer = new System.Threading.Timer(MusicFadeTick, null, FADE_TICK_MS, FADE_TICK_MS);

            EnsureMusicMonitor();
        }

        // Crea el canal de musica para una pista: las del modulo real (01-23)
        // se reproducen con libopenmpt sobre un push-stream; las ambientales
        // (24-29) son ficheros normales cargados desde el PAC o la carpeta.
        private int CreateChannelFor(string musicFileName, out GCHandle pin, out ModuleMusicPlayer player)
        {
            pin = new GCHandle();
            player = null;
            int order = MusicMap.OrderForFile(musicFileName);
            if (order >= 0) return CreateModuleChannel(order, out player);
            return CreateFileMusicChannel(musicFileName, out pin);
        }

        // Push-stream de la musica del modulo saltando a `order`. El relleno
        // de PCM lo hace ModuleFillTick (50 ms por tick, buffer BASS 500 ms).
        private int CreateModuleChannel(int order, out ModuleMusicPlayer player)
        {
            player = null;
            try
            {
                byte[] mo3Bytes = null;

                // 1. Load from encrypted audio.pac directly in RAM
                if (_audioPac != null)
                {
                    mo3Bytes = _audioPac.GetFileBytes(MusicMap.ModuleFile);
                }

                // 2. Fallback to unencrypted folder
                if (mo3Bytes == null)
                {
                    string modulePath = Path.Combine(_musicDir, MusicMap.ModuleFile);
                    if (File.Exists(modulePath)) mo3Bytes = File.ReadAllBytes(modulePath);
                }

                if (mo3Bytes == null || mo3Bytes.Length == 0)
                {
                    LogAudioError("Modulo MO3 no disponible: " + MusicMap.ModuleFile);
                    return 0;
                }

                player = ModuleMusicPlayer.TryCreate(mo3Bytes);
                if (player == null || !player.IsValid)
                {
                    LogAudioError("libopenmpt no pudo abrir el modulo");
                    if (player != null)
                    {
                        try { player.Dispose(); } catch { }
                    }
                    player = null;
                    return 0;
                }
                player.SeekTo(order, MusicMap.NextOffsetAfter(order));

                // Stream con callback: BASS pide el PCM al hilo de audio y el
                // relleno es automatico (el push-stream no existe en esta build).
                int handle = BASS_StreamCreate(ModuleMusicPlayer.SampleRate, 2, BASS_SAMPLE_FLOAT,
                    _moduleStreamProc, player.UserToken);
                if (handle == 0)
                {
                    LogAudioError("Stream musica modulo fallo, err=" + BASS_LastError());
                    try { player.Dispose(); } catch { }
                    player = null;
                    return 0;
                }

                // Fade-in starts from silence. La pista se espacializa como
                // ambiente (GridSpatializer) para que acompane al juego con
                // envoltura estereo y aire, en lugar de sonar plana y centrada.
                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, 0.0f);
                AttachMusicSpatializer(handle);
                BASS_ChannelPlay(handle, true);
                return handle;
            }
            catch (Exception ex)
            {
                LogAudioError("CreateModuleChannel excepcion: " + ex.GetType().Name + ": " + ex.Message);
                if (player != null)
                {
                    try { player.Dispose(); } catch { }
                }
                player = null;
                return 0;
            }
        }

        // Callback de BASS_StreamCreate: se ejecuta en el hilo de audio de BASS
        // cuando el stream necesita PCM. Solo decodifica y escribe el buffer;
        // detecta el avance de seccion (p. ej. el intro termina y empieza el
        // menu) y el final del modulo completo (~62 min) marcando un flag que el
        // monitor de musica convierte en MusicRechained fuera del hilo de BASS.
        private uint ModuleStreamProc(int handle, IntPtr buffer, uint length, IntPtr user)
        {
            ModuleMusicPlayer player;
            try
            {
                GCHandle token = GCHandle.FromIntPtr(user);
                if (!token.IsAllocated) return 0;
                player = token.Target as ModuleMusicPlayer;
            }
            catch { return 0; }
            if (player == null) return 0;

            bool replayed;
            int frames = player.ReadInterleaved(buffer, (int)Math.Min(length / 8, (uint)ModuleMusicPlayer.MaxFrames), out replayed);
            if (replayed || player.UpdateSectionAdvance()) _moduleEventPending = true;
            return (uint)(frames * 8);
        }

        // Creates a ready-to-play music stream at zero volume for a file-based
        // track (ambientales 24-29). The track is re-chained with a crossfade
        // when it approaches its end (like the original .mo3 files which chain
        // segments automatically); the native BASS loop is a safety net in case
        // the monitor ever misses a chain.
        private int CreateFileMusicChannel(string musicFileName, out GCHandle pin)
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

                // Fade-in starts from silence. La pista se espacializa como
                // ambiente (GridSpatializer) para que acompane al juego con
                // envoltura estereo y aire, en lugar de sonar plana y centrada.
                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_VOL, 0.0f);
                AttachMusicSpatializer(handle);
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

        private static void SetFxParams<T>(int fx, T s) where T : struct
        {
            int size = Marshal.SizeOf(s);
            IntPtr p = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(s, p, false);
                BASS_FXSetParameters(fx, p);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        // Ecualizacion por fila del tablero: las filas frontales (0-2) ganan
        // brillo (+3 dB a 8 kHz) y las filas lejanas (5-7) reciben un corte
        // progresivo desde 4 kHz para reforzar la sensacion de profundidad.
        // NO se usa pitch: solo se moldea el tono del SFX.
        private void ApplyRowEq(int handle, int row)
        {
            // Sin EQ de fila: el GridSpatializer ya da profundidad por atenuacion
            // de distancia. No modificamos el timbre de los sonidos originales.
            return;
        }

        // Reverb ambiental sutil ("vacío espacial") + compresión/limitación para
        // absorber los picos de cascadas multiples y la gema Supernova.
        private void ApplyAtmosphereFx(int handle)
        {
            if (!BinauralEnabled) return;
            try
            {
                int rv = BASS_ChannelSetFX(handle, BASS_FX_DX8_REVERB, 1);
                if (rv != 0)
                {
                    BassDx8Reverb rev = new BassDx8Reverb();
                    rev.fInGain = 0f;
                    rev.fReverbMix = -32f;   // cola difusa corta y brillante (sala)
                    rev.fReverbTime = 600f;
                    rev.fHighFreqRTRatio = 0.7f;
                    SetFxParams(rv, rev);
                }
            }
            catch { }
        }

        internal void ApplySpatialFx(int handle, int row)
        {
            if (!BinauralEnabled) return;
            ApplyRowEq(handle, row);
            ApplyAtmosphereFx(handle);
        }

        // Barrido de giro 360 del hipercubo: el zumbido recorre el paneo
        // de -1 (izquierda) a +1 (derecha) y de regreso en bucle durante 1.5 s.
        public void PlayHypercubeSweep(string soundName, int col, int row)
        {
            if (!BinauralEnabled) { PlaySound(soundName); return; }

            try
            {
                byte[] data = LoadAudioBytes(soundName);
                if (data == null) { PlaySound(soundName); return; }

                GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
                int handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, data.Length, 0);
                if (handle == 0)
                {
                    if (pinned.IsAllocated) try { pinned.Free(); } catch { }
                    PlaySound(soundName);
                    return;
                }

                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, 0.0f);
                BASS_ChannelPlay(handle, true);
                DuckMusicVolume(0.3f, 500);

                var state = new SpatialSweepState
                {
                    Handle = handle,
                    Pinned = pinned,
                    Started = DateTime.Now,
                    DurationMs = 1500,
                    SweepPan = true,
                    SweepEq = false
                };
                StartSweep(state);
                // Después de la animación de 1.5 s, recuperar el volumen de la música.
                RestoreMusicVolume(1500);
            }
            catch
            {
                try { PlaySound(soundName); } catch { }
            }
        }

        // Rayo laser de la estrella: dos sonidos simultaneos. (1) barrido rapido
        // de paneo Izquierda->Derecha (fila) y (2) barrido de EQ de agudo a grave
        // (columna). Simula el disparo del laser estelar.
        public void PlayStarGemLaser(string soundName, int col, int row)
        {
            if (!BinauralEnabled) { PlaySound(soundName); return; }

            try
            {
                byte[] data = LoadAudioBytes(soundName);
                if (data == null) { PlaySound(soundName); return; }

                GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
                int handle = BASS_StreamCreateFile(true, pinned.AddrOfPinnedObject(), 0, data.Length, 0);
                if (handle == 0)
                {
                    if (pinned.IsAllocated) try { pinned.Free(); } catch { }
                    PlaySound(soundName);
                    return;
                }

                BASS_ChannelSetAttribute(handle, BASS_ATTRIB_PAN, -1.0f);

                int eqFx = BASS_ChannelSetFX(handle, BASS_FX_DX8_PARAMEQ, 0);
                BASS_ChannelPlay(handle, true);

                var state = new SpatialSweepState
                {
                    Handle = handle,
                    Pinned = pinned,
                    Started = DateTime.Now,
                    DurationMs = 450,
                    SweepPan = true,
                    SweepEq = true,
                    EqFx = eqFx
                };
                StartSweep(state);
            }
            catch
            {
                try { PlaySound(soundName); } catch { }
            }
        }

        private class SpatialSweepState
        {
            public int Handle;
            public GCHandle Pinned;
            public DateTime Started;
            public int DurationMs;
            public bool SweepPan;
            public bool SweepEq;
            public int EqFx;
            public System.Threading.Timer Timer;
        }

        private readonly List<SpatialSweepState> _activeSpatialSweeps = new List<SpatialSweepState>();

        private void StartSweep(SpatialSweepState state)
        {
            lock (_activeSpatialSweeps)
            {
                _activeSpatialSweeps.Add(state);
            }
            state.Timer = new System.Threading.Timer(SweepTick, state, 0, 25);
        }

        private void SweepTick(object obj)
        {
            SpatialSweepState s = (SpatialSweepState)obj;
            try
            {
                double elapsed = (DateTime.Now - s.Started).TotalMilliseconds;
                double t = elapsed / s.DurationMs;
                if (t >= 1.0)
                {
                    lock (_activeSpatialSweeps)
                    {
                        _activeSpatialSweeps.Remove(s);
                    }
                    if (s.Timer != null) { try { s.Timer.Dispose(); } catch { } s.Timer = null; }
                    try { BASS_ChannelStop(s.Handle); } catch { }
                    try { BASS_StreamFree(s.Handle); } catch { }
                    if (s.Pinned.IsAllocated) try { s.Pinned.Free(); } catch { }
                    return;
                }

                if (s.SweepPan)
                {
                    // -1 -> +1 -> -1 a lo largo de la duracion (una oscilacion).
                    float pan = (float)Math.Sin(t * Math.PI * 2.0);
                    BASS_ChannelSetAttribute(s.Handle, BASS_ATTRIB_PAN, pan);
                }

                if (s.SweepEq && s.EqFx != 0)
                {
                    // Barrido de EQ de agudo (12 kHz) a grave (500 Hz).
                    float center = (float)(12000.0 - 11500.0 * t);
                    BassDx8Parameq eq = new BassDx8Parameq();
                    eq.fCenter = center;
                    eq.fBandwidth = 1.2f;
                    eq.fGain = 0f;
                    SetFxParams(s.EqFx, eq);
                }
            }
            catch
            {
                if (s.Timer != null) { try { s.Timer.Dispose(); } catch { } s.Timer = null; }
            }
        }

        // El audio espacial binaural se elimino: la musica suena plana y centrada
        // (paneo estereo simple), sin envoltura de sala.
        private void AttachMusicSpatializer(int musicHandle)
        {
            return;
        }

        // Reverb de cola para la musica-ambiente: la MISMA sala que los SFX
        // (corta y brillante), para que musica y gemas compartan un unico
        // espacio acustico. Sin compresor, para no matar la dinamica.
        private void ApplyMusicAtmosphere(int musicHandle)
        {
            if (!BinauralEnabled) return;
            try
            {
                int rv = BASS_ChannelSetFX(musicHandle, BASS_FX_DX8_REVERB, 1);
                if (rv != 0)
                {
                    BassDx8Reverb rev = new BassDx8Reverb();
                    rev.fInGain = 0f;
                    rev.fReverbMix = -20f;   // cola de la misma sala que los SFX
                    rev.fReverbTime = 1200f;
                    rev.fHighFreqRTRatio = 0.65f;
                    SetFxParams(rv, rev);
                }
            }
            catch { }
        }

        // Atenuación suave del volumen de la música (sidechain/ducking).
        // Desliza el volumen hacia el nivel objetivo en durationMs milisegundos,
        // y programa una recuperación automática al volumen original después de ese
        // mismo lapso (con un pequeño margen extra para que no corte el efecto).
        public void DuckMusicVolume(float targetVolume, int durationMs)
        {
            if (_musicDuckTimer != null) { try { _musicDuckTimer.Dispose(); } catch { } _musicDuckTimer = null; }
            try
            {
                float currentVol = 0f;
                BASS_ChannelGetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, ref currentVol);
                _musicOriginalVolume = currentVol;
                BASS_ChannelSlideAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, targetVolume, durationMs);
                _musicDuckTimer = new System.Threading.Timer(s =>
                {
                    try
                    {
                        BASS_ChannelSlideAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, _musicOriginalVolume, durationMs);
                    }
                    catch { }
                    _musicDuckTimer = null;
                }, null, durationMs + 200, -1);
            }
            catch { }
        }

        // Recupera el volumen original de la música deslizando suavemente
        // de vuelta al volumen original en durationMs milisegundos.
        public void RestoreMusicVolume(int durationMs)
        {
            if (_musicDuckTimer != null) { try { _musicDuckTimer.Dispose(); } catch { } _musicDuckTimer = null; }
            try
            {
                if (_currentMusicChannel != 0)
                {
                    BASS_ChannelSlideAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, _musicOriginalVolume, durationMs);
                }
            }
            catch { }
        }

        // DSP de ambiente: sustituye el buffer estereo de la musica por la salida
        // del GridSpatializer tratando L y R como dos fuentes simetricas (mismo
        // depth, pan opuesto). Asi la musica acompana al juego con envoltura
        // estereo y aire, sin colapsarse a mono.
        // (El DSP de ambiente binaural se elimino: la musica ya no se espacializa.)

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
                // El callback del modulo marca un flag (avance de seccion o
                // final de la suite); el evento se dispara aqui, fuera del hilo
                // de audio de BASS, para no llamar a PlayMusic desde el callback.
                if (_moduleEventPending)
                {
                    _moduleEventPending = false;
                    if (MusicRechained != null)
                    {
                        try { MusicRechained(this, EventArgs.Empty); } catch { }
                    }
                }
                if (_currentMusicChannel == 0 || _currentMusicFile == null) return;
                // La musica del modulo se encadena sola (vuelve al inicio de la
                // cancion al final del modulo y avanza de seccion); el monitor
                // de pistas de fichero no debe tocarla.
                if (_currentModulePlayer != null) return;
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

        public void UpdateMusicVolume()
        {
            if (_currentMusicChannel != 0)
            {
                try
                {
                    // Don't override the volume while a fade is in progress
                    if (_musicFadeTimer == null)
                    {
                        float duck;
                        lock (_duckLock) { duck = _duckCurrent; }
                        BASS_ChannelSetAttribute(_currentMusicChannel, BASS_ATTRIB_VOL, MusicChannelVolume(duck));
                    }
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
                if (_pendingModulePlayer != null)
                {
                    try { _pendingModulePlayer.Dispose(); } catch { }
                    _pendingModulePlayer = null;
                }

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

                if (_currentModulePlayer != null)
                {
                    try { _currentModulePlayer.Dispose(); } catch { }
                    _currentModulePlayer = null;
                }
            }
        }

        public void Dispose()
        {
            StopMusic();

            System.Threading.Timer duckTimer = null;
            lock (_duckLock)
            {
                duckTimer = _duckTimer;
                _duckTimer = null;
            }
            if (duckTimer != null)
            {
                try { duckTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                try { duckTimer.Dispose(); } catch { }
            }

            if (_musicDuckTimer != null)
            {
                try { _musicDuckTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite); } catch { }
                try { _musicDuckTimer.Dispose(); } catch { }
                _musicDuckTimer = null;
            }

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

            lock (_activeSpatialSweeps)
            {
                foreach (var s in _activeSpatialSweeps)
                {
                    if (s.Timer != null) { try { s.Timer.Dispose(); } catch { } s.Timer = null; }
                    try { BASS_ChannelStop(s.Handle); } catch { }
                    try { BASS_StreamFree(s.Handle); } catch { }
                    if (s.Pinned.IsAllocated) try { s.Pinned.Free(); } catch { }
                }
                _activeSpatialSweeps.Clear();
            }

            try
            {
                lock (_activeSfxList)
                {
                    foreach (var sfx in _activeSfxList)
                    {
                        if (sfx.Spatial != null)
                        {
                            // La fuente espacial ya libera el stream (OutputHandle)
                            // y el pin del OGG, asi que no los tocamos aqui.
                            try { sfx.Spatial.Dispose(); } catch { }
                        }
                        else
                        {
                            if (sfx.Pin.IsAllocated)
                            {
                                try { sfx.Pin.Free(); } catch { }
                            }
                            BASS_StreamFree(sfx.Handle);
                        }
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

    // Cadena de reproducción espacial de un efecto del tablero:
    // BASS_StreamCreateFile reproduce el OGG a su tasa nativa y un DSP
    // instalado en el canal sustituye el buffer estéreo por la salida del
    // GridSpatializer (pan L/R + profundidad frente/fondo). Unico modelo,
    // siempre activo, sin perfiles.
    internal sealed class SpatialSfxSource : IDisposable
    {
        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_StreamCreateFile(bool mem, IntPtr file, long offset, long length, uint flags);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelGetInfo(int handle, out BassChannelInfo info);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelSetDSP(int handle, [MarshalAs(UnmanagedType.FunctionPtr)] DspProc proc, IntPtr user, int priority);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_StreamFree(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_ChannelGetData(int handle, [In, Out] float[] buffer, int length);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_StreamCreate(int freq, int chans, uint flags, IntPtr proc, IntPtr user);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_StreamPutData(int handle, IntPtr buffer, int length);

        private const uint BASS_STREAM_DECODE = 0x20000;

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelStop(int handle);

        [DllImport("bass.dll", CharSet = CharSet.Auto)]
        private static extern bool BASS_ChannelSetAttribute(int handle, int attrib, float value);

        [DllImport("bass_fx.dll", CharSet = CharSet.Auto)]
        private static extern int BASS_FX_TempoCreate(int chan, int flags);

        private const int BASS_FX_FREESOURCE = 0x10000;
        private const int BASS_ATTRIB_TEMPO_PITCH = 0x10001;
        private const int BASS_ATTRIB_PAN = 3;
        private const uint BASS_SAMPLE_FLOAT = 0x100;

        [StructLayout(LayoutKind.Sequential)]
        private struct BassChannelInfo
        {
            public int freq;
            public int chans;
            public int flags;
            public uint ctype;
            public IntPtr filename;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void DspProc(int handle, int channel, IntPtr buffer, int length, IntPtr user);

        public int OutputHandle { get; private set; }

        private readonly SoundEngine _engine;
        private readonly bool _binaural;
        private readonly GCHandle _pin;      // OGG en RAM: vive con la fuente
        private GCHandle _wavPin;            // WAV estéreo en RAM (mono->estéreo): vive con la fuente
        private readonly GCHandle _selfPin;  // token para el callback de BASS
        private readonly int _inChans;

        public SpatialSfxSource(SoundEngine engine, byte[] oggBytes, GCHandle pin, int col, float pan, float depth, int row, bool binaural, float? fxSemitones = null)
        {
            _engine = engine;
            _binaural = binaural;
            _pin = pin;
            // El renderer necesita un buffer de salida ESTEREO para poder panezar.
            // Los OGG del juego pueden ser mono (p.ej. gem_hit): los subimos a
            // estéreo duplicando el canal, asi el DSP no corrompe la mitad del
            // buffer (que era el defecto que hacia sonar las gemas "de piedra").
            OutputHandle = BuildStereoStream(pin.AddrOfPinnedObject(), oggBytes.Length, out _wavPin);
            if (OutputHandle == 0) throw new InvalidOperationException("BASS stream fallo");

            // Pitch natural con BASS FX (sin resample): si el add-on esta
            // disponible y se pidio semitonos, envolvemos el decode en un
            // stream de tempo y fijamos el pitch. Si falla, sonamos secos.
            if (fxSemitones.HasValue && _engine.BassFxAvailable)
            {
                int fx = BASS_FX_TempoCreate(OutputHandle, BASS_FX_FREESOURCE);
                if (fx != 0)
                {
                    BASS_ChannelSetAttribute(fx, BASS_ATTRIB_TEMPO_PITCH, fxSemitones.Value);
                    OutputHandle = fx;
                }
            }

            BassChannelInfo info;
            if (!BASS_ChannelGetInfo(OutputHandle, out info))
            {
                BASS_StreamFree(OutputHandle);
                OutputHandle = 0;
                throw new InvalidOperationException("BASS_ChannelGetInfo fallo");
            }
            _inChans = (info.chans == 1) ? 1 : 2;

            // Audio espacial simple: paneo estereo por columna (clasico
            // Bejeweled) cuando BinauralEnabled esta activo. Sin sala/rebotes
            // ni EQ de profundidad: el binaural se elimino por agresivo.
            _selfPin = default(GCHandle);
            if (_binaural && Math.Abs(pan) > 0.0001f)
            {
                try { BASS_ChannelSetAttribute(OutputHandle, BASS_ATTRIB_PAN, pan); } catch { }
            }
        }

        // Stream directo desde el OGG (el camino que siempre suena). El DSP se
        // encarga de dejar el mono centrado y sin silencio; los estéreo se panearan.
        private static int BuildStereoStream(IntPtr oggPtr, int oggLen, out GCHandle wavPin)
        {
            wavPin = default(GCHandle);
            return BASS_StreamCreateFile(true, oggPtr, 0, oggLen, BASS_SAMPLE_FLOAT);
        }

        // (El callback DSP binaural se elimino: el paneo ahora lo hace BASS directamente.)

        public void Dispose()
        {
            if (OutputHandle != 0)
            {
                try { BASS_ChannelStop(OutputHandle); } catch { }
                try { BASS_StreamFree(OutputHandle); } catch { }
                OutputHandle = 0;
            }
            if (_pin.IsAllocated)
            {
                try { _pin.Free(); } catch { }
            }
            if (_wavPin.IsAllocated)
            {
                try { _wavPin.Free(); } catch { }
            }
            if (_selfPin.IsAllocated)
            {
                try { _selfPin.Free(); } catch { }
            }
        }
    }
}
