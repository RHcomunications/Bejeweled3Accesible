using System;
using System.Runtime.InteropServices;
using System.Threading;
using Android.Media;
using Bejeweled3Accessible.Audio;

namespace Bejeweled3Accessible.AndroidApp.Audio
{
    // AndroidModulePlayer.cs - Reproduccion del modulo musical real del juego
    // (Bejeweled3_suite.mo3) en Android usando libopenmpt (el mismo decodificador
    // de referencia que Windows) y escribiendo el PCM directamente en un
    // AudioTrack. Asi Android suena identico a Windows: un unico modulo de 62
    // minutos con los saltos de orden reales (MusicMap.ModuleOffsets) y las
    // transiciones continuas entre modos, en lugar de 29 mp3 cortados.
    //
    // Degradacion elegante: si libopenmpt.so no esta empaquetado, IsAvailable
    // devuelve false y AndroidSoundEngine cae a los mp3 por separado (MediaPlayer)
    // sin romper nada. Basta con dejar libopenmpt.so en libs/<abi>/ y referenciarlo
    // en el csproj para activar el modulo.
    public sealed class AndroidModulePlayer : IDisposable
    {
        // --- libopenmpt C API (sondeada en libopenmpt.so para Android) ---
        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr openmpt_module_create_from_memory2(byte[] filedata, uint filesize,
            IntPtr logfunc, IntPtr loguser, IntPtr errfunc, IntPtr erruser,
            out int error, out IntPtr errorMessage, IntPtr ctls);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl)]
        private static extern void openmpt_module_destroy(IntPtr mod);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl)]
        private static extern int openmpt_module_get_current_order(IntPtr mod);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl)]
        private static extern double openmpt_module_set_position_order_row(IntPtr mod, int order, int row);

        [DllImport("libopenmpt", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint openmpt_module_read_float_stereo(IntPtr mod, int samplerate, uint count,
            float[] left, float[] right);

        public const int SampleRate = 44100;
        public const int MaxFrames = SampleRate / 2; // ~500 ms por lectura

        private static bool _nativeChecked = false;
        private static bool _nativeOk = true;

        public static bool IsAvailable
        {
            get
            {
                if (!_nativeChecked)
                {
                    _nativeChecked = true;
                    // Sondea resolviendo la libreria nativa con datos invalidos:
                    // si libopenmpt.so falta, el P/Invoke lanza DllNotFound/TypeLoad
                    // (se captura y marca como no disponible); si esta presente,
                    // la creacion falla de forma controlada pero la libreria cargo.
                    TryCreate(new byte[1]);
                }
                return _nativeOk;
            }
        }

        private IntPtr _mod = IntPtr.Zero;
        private readonly float[] _left = new float[MaxFrames];
        private readonly float[] _right = new float[MaxFrames];

        private int _startOrder = 0;
        private int _nextOrder = -1;
        private bool _ended = false;
        private bool _sectionAdvanced = false;

        private AudioTrack _track;
        private Thread _thread;
        private volatile bool _playing = false;
        private float _volume = 0.85f;
        private readonly object _lock = new object();

        public static AndroidModulePlayer TryCreate(byte[] mo3Bytes)
        {
            if (mo3Bytes == null || mo3Bytes.Length == 0) return null;
            try
            {
                var player = new AndroidModulePlayer();
                int error;
                IntPtr errorMessage;
                player._mod = openmpt_module_create_from_memory2(mo3Bytes, (uint)mo3Bytes.Length,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out error, out errorMessage, IntPtr.Zero);
                if (player._mod == IntPtr.Zero)
                {
                    player.Dispose();
                    return null;
                }
                return player;
            }
            catch (Exception ex)
            {
                if (ex is DllNotFoundException || ex is TypeLoadException)
                    _nativeOk = false;
                Android.Util.Log.Error("BejeweledAudio", "libopenmpt TryCreate fallo: " + ex.GetType().Name);
                return null;
            }
        }

        public bool IsValid
        {
            get { return _mod != IntPtr.Zero; }
        }

        public int CurrentOrder
        {
            get { return _mod == IntPtr.Zero ? -1 : openmpt_module_get_current_order(_mod); }
        }

        // Salta al inicio de la cancion (order del mapa real) y recuerda la
        // siguiente cancion conocida para el avance de seccion.
        public void SeekTo(int order, int nextOrder)
        {
            if (_mod == IntPtr.Zero) return;
            _startOrder = order;
            _nextOrder = nextOrder;
            _ended = false;
            _sectionAdvanced = false;
            openmpt_module_set_position_order_row(_mod, order, 0);
        }

        // Lee hasta maxFrames frames estereo en los buffers float. Si el modulo
        // completo (62 min) llega a su fin, rebobina al inicio de la cancion y
        // avisa con replayed para que el bucle siga sonando en cadena.
        public int ReadFloat(float[] left, float[] right, out bool replayed)
        {
            replayed = false;
            if (_mod == IntPtr.Zero) return 0;

            int max = Math.Min(left.Length, MaxFrames);
            uint got = openmpt_module_read_float_stereo(_mod, SampleRate, (uint)max, left, right);
            if (got == 0)
            {
                if (!_ended)
                {
                    _ended = true;
                    openmpt_module_set_position_order_row(_mod, _startOrder, 0);
                    got = openmpt_module_read_float_stereo(_mod, SampleRate, (uint)max, left, right);
                    replayed = true;
                }
                if (got == 0) return 0;
            }
            _ended = false;
            return (int)got;
        }

        // Avance de seccion: cuando la reproduccion continua cruza el offset de la
        // siguiente cancion del mapa real, devuelve true una unica vez.
        public bool UpdateSectionAdvance()
        {
            if (_mod == IntPtr.Zero || _sectionAdvanced || _nextOrder < 0) return false;
            if (openmpt_module_get_current_order(_mod) >= _nextOrder)
            {
                _sectionAdvanced = true;
                return true;
            }
            return false;
        }

        public void SetVolume(float vol)
        {
            _volume = vol;
            lock (_lock)
            {
                if (_track != null)
                {
                    try { _track.SetStereoVolume(vol, vol); } catch { }
                }
            }
        }

        public void Start()
        {
            if (_mod == IntPtr.Zero) return;
            lock (_lock)
            {
                if (_playing) return;
                try
                {
                    int minBuf = AudioTrack.GetMinBufferSize(SampleRate, ChannelOut.Stereo, Encoding.Pcm16bit);
                    int bufSize = Math.Max(minBuf, SampleRate * 2);
                    _track = new AudioTrack(Android.Media.Stream.Music, SampleRate,
                        ChannelOut.Stereo, Encoding.Pcm16bit, bufSize, AudioTrackMode.Stream);
                    _track.SetStereoVolume(_volume, _volume);
                    _track.Play();
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("BejeweledAudio", "AudioTrack init fallo: " + ex.Message);
                    return;
                }
            }
            _playing = true;
            _thread = new Thread(PlayLoop) { IsBackground = true };
            _thread.Priority = ThreadPriority.BelowNormal;
            _thread.Start();
        }

        public void Stop()
        {
            _playing = false;
            lock (_lock)
            {
                if (_track != null)
                {
                    try { if (_track.PlayState == PlayState.Playing) _track.Stop(); } catch { }
                    try { _track.Release(); } catch { }
                    _track = null;
                }
            }
        }

        public bool IsPlaying
        {
            get { return _playing; }
        }

        private void PlayLoop()
        {
            var pcm = new short[MaxFrames * 2];
            while (_playing)
            {
                bool replayed;
                int frames = ReadFloat(_left, _right, out replayed);
                if (frames <= 0)
                {
                    Thread.Sleep(20);
                    continue;
                }

                for (int i = 0; i < frames; i++)
                {
                    pcm[i * 2] = ClampShort(_left[i]);
                    pcm[i * 2 + 1] = ClampShort(_right[i]);
                }

                lock (_lock)
                {
                    if (_track != null)
                    {
                        try { _track.Write(pcm, 0, frames * 2); } catch { }
                    }
                }

                if (replayed || UpdateSectionAdvance())
                {
                    // El modulo avanzo de seccion: sigue sonando en cadena (el
                    // juego puede reaccionar si lo desea; por ahora continuamos).
                }
            }
        }

        private static short ClampShort(float v)
        {
            if (v > 1.0f) v = 1.0f;
            else if (v < -1.0f) v = -1.0f;
            return (short)(v * 32767.0f);
        }

        public void Dispose()
        {
            Stop();
            if (_mod != IntPtr.Zero)
            {
                try { openmpt_module_destroy(_mod); } catch { }
                _mod = IntPtr.Zero;
            }
        }
    }
}
