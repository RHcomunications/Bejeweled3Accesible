// ModuleMusicPlayer.cs - Reproduccion del modulo musical real del juego.
//
// El Bejeweled 3 original no usa pistas sueltas: toda su musica vive en un
// unico modulo "Bejeweled3_suite.mo3" (extraido de main.pak, 62 minutos,
// 214 ordenes) y el juego salta entre offsets de la lista de ordenes segun
// el contexto (music.xml real). BASS no decodifica MO3 (ni siquiera el
// bass.dll del propio juego, que lo reproduce con su reproductor interno),
// asi que aqui se usa libopenmpt (BSD-3, el decodificador de referencia)
// como fuente de PCM y un push-stream BASS como salida: el resto del motor
// (ducking, fades, volumen, reverb) sigue funcionando igual que con archivos.
using System;
using System.Runtime.InteropServices;

namespace Bejeweled3Accessible.Audio
{
    public sealed class ModuleMusicPlayer : IDisposable
    {
        // --- libopenmpt C API (DLL x86, __cdecl) ---
        [DllImport("libopenmpt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr openmpt_module_create_from_memory2(byte[] filedata, uint filesize,
            IntPtr logfunc, IntPtr loguser, IntPtr errfunc, IntPtr erruser,
            out int error, out IntPtr errorMessage, IntPtr ctls);

        [DllImport("libopenmpt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void openmpt_module_destroy(IntPtr mod);

        [DllImport("libopenmpt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int openmpt_module_get_current_order(IntPtr mod);

        [DllImport("libopenmpt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double openmpt_module_set_position_order_row(IntPtr mod, int order, int row);

        [DllImport("libopenmpt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint openmpt_module_read_float_stereo(IntPtr mod, int samplerate, uint count,
            float[] left, float[] right);

        public const int SampleRate = 44100;

        // Tamano maximo de un trozo de PCM pedido por el callback de BASS
        // (500 ms = el buffer por defecto de BASS_StreamCreate).
        public const int MaxFrames = SampleRate / 2;

        private IntPtr _mod = IntPtr.Zero;
        private GCHandle _pin;
        private GCHandle _token;
        private readonly float[] _left = new float[MaxFrames];
        private readonly float[] _right = new float[MaxFrames];
        private readonly float[] _interleaved = new float[MaxFrames * 2];

        private int _startOrder = 0;
        private int _nextOrder = -1;
        private bool _ended = false;
        private bool _sectionAdvanced = false;

        // Token opaco con el que el callback de BASS recupera este objeto
        // (se pasa como `user` a BASS_StreamCreate). Se libera en Dispose.
        public IntPtr UserToken
        {
            get { return GCHandle.ToIntPtr(_token); }
        }

        public static ModuleMusicPlayer TryCreate(byte[] mo3Bytes)
        {
            try
            {
                ModuleMusicPlayer player = new ModuleMusicPlayer();
                player._pin = GCHandle.Alloc(mo3Bytes, GCHandleType.Pinned);
                player._token = GCHandle.Alloc(player);

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
            catch
            {
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

        // Salta al inicio de la cancion (`order`, del mapa real) y recuerda la
        // siguiente cancion conocida (`nextOrder`, -1 si no hay) para avisarle
        // al juego cuando la reproduccion continua sale de la seccion.
        public void SeekTo(int order, int nextOrder)
        {
            if (_mod == IntPtr.Zero) return;
            _startOrder = order;
            _nextOrder = nextOrder;
            _ended = false;
            _sectionAdvanced = false;
            openmpt_module_set_position_order_row(_mod, order, 0);
        }

        // Lee hasta maxFrames frames estéreo e intercala L/R escribiendo
        // directamente en `dest` (el buffer que BASS pasó al callback).
        // Devuelve los frames escritos. Si el modulo llega a su final (la
        // suite completa de 62 minutos), vuelve al inicio de la cancion y
        // avisa con `replayed=true` para que el motor encadene el evento.
        public int ReadInterleaved(IntPtr dest, int maxFrames, out bool replayed)
        {
            replayed = false;
            if (_mod == IntPtr.Zero) return 0;

            int max = Math.Min(maxFrames, MaxFrames);
            uint got = openmpt_module_read_float_stereo(_mod, SampleRate, (uint)max, _left, _right);
            if (got == 0)
            {
                if (!_ended)
                {
                    _ended = true;
                    openmpt_module_set_position_order_row(_mod, _startOrder, 0);
                    replayed = true;
                }
                return 0;
            }
            _ended = false;

            for (int i = 0; i < got; i++)
            {
                _interleaved[i * 2] = _left[i];
                _interleaved[i * 2 + 1] = _right[i];
            }
            System.Runtime.InteropServices.Marshal.Copy(_interleaved, 0, dest, (int)got * 2);
            return (int)got;
        }

        // Avance de seccion: cuando la reproduccion continua cruza el offset de
        // la siguiente cancion del mapa real (p. ej. LoadingScreen -> MainMenu),
        // devuelve true una unica vez. El juego lo usa para que la pantalla de
        // carga avance sola, como hace el original con sus eventos Switch.
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

        public void Dispose()
        {
            if (_mod != IntPtr.Zero)
            {
                try { openmpt_module_destroy(_mod); } catch { }
                _mod = IntPtr.Zero;
            }
            if (_pin.IsAllocated)
            {
                try { _pin.Free(); } catch { }
            }
            if (_token.IsAllocated)
            {
                try { _token.Free(); } catch { }
            }
        }
    }
}
