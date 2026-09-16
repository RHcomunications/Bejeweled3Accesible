using System;

namespace Bejeweled3Accessible.Audio
{
    // --- Entornos Acústicos Temáticos de Bejeweled 3 (Binaural Room Impulses) ---
    public enum AudioEnvironment
    {
        CrystalTemple,      // Clásico, Menús, Relicarios (cámara de mármol y cuarzo)
        UndergroundCavern,  // Mina de Diamantes (caverna profunda de roca)
        GlacialChamber,     // Tormenta de Hielo (acústica helada con reflejos cristalinos)
        TwilightGarden,     // Mariposas (jardín abierto crepuscular y aireado)
        Sanctuary,          // Zen (santuario meditativo envolvente)
        EnergyConduit,      // Relámpago (conducto elemental reactivo)
        VictorianSalon      // Poker (salón íntimo de paño y madera noble)
    }

    public enum AudioObjectClass
    {
        BoardGem,           // Gemas del tablero (swap, selección, caída)
        CascadeMatch,       // Destrucciones y cascadas
        SpecialGem,         // Creación/activación de Fuego, Estrella, Hipercubo, Supernova
        AnnouncerVoice,     // Voces arcade del anunciador (centradas)
        AmbientBed,         // Cama musical y paisajes de naturaleza (Bed Objects directos)
        UINavigation        // Sonidos de menú y cursor
    }

    public struct EnvironmentAcoustics
    {
        public float ReverbMix;         // dB de envío ambiental (-96..0)
        public float ReverbTime;        // ms de decaimiento
        public float HighFreqRTRatio;   // ratio de agudos en la reverb (0.1..0.99)
        public float LowPassCutoff;     // corte de agudos por absorción de sala (Hz)
        public float StereoWidth;       // anchura estéreo acústica (1.0..1.2)
        public float PresenceGain;      // dB de realce tímbrico
        public float PresenceFreq;      // Hz de frecuencia central
    }

    // Estructura de metadatos de Objeto de Audio Espacial (al estilo Dolby Atmos)
    public struct SpatialAudioObject
    {
        public float X;                 // Posición horizontal (-1.0 Izquierda .. +1.0 Derecha)
        public float Y;                 // Profundidad (0.0 Frente .. 1.0 Fondo)
        public float Z;                 // Elevación (0.0 Base .. 1.0 Techo)
        public float DirectPan;         // Paneo estéreo BASS (-0.85 .. +0.85)
        public float DirectVolume;      // Ganancia directa (0.65 .. 1.0)
        public float DrrReverbMix;      // Nivel de mezcla de sala desacoplada (dB)
        public float DrrReverbTime;     // Tiempo de sala (ms)
        public float DrrHighFreqRatio;  // Ratio de agudos de la sala
        public AudioObjectClass Category;
    }

    // Modelo de Audio Espacial Basado en Objetos:
    // Cada sonido emitido en el tablero es un objeto con posición 3D (X, Y, Z).
    // La señal directa (Direct Sound) conserva el 100% de su pegada, brillo y
    // transitorios originales (cero sonido a piedra o carbón). El entorno se
    // genera mediante un envío de sala desacoplado (DRR - Direct-to-Reverberant Ratio).
    public static class SpatialAudio
    {
        public const float MaxPan = 0.85f;
        public const int BoardColumns = 8;
        public const int BoardRows = 8;
        public const float CenterPan = 0.0f;
        public const float VoicePan = 0.0f;

        // Columna -> pan en [-MaxPan, +MaxPan]. col<0 o fuera de rango -> centro.
        public static float Pan(int col, int cols)
        {
            if (cols <= 1) return CenterPan;
            if (col < 0 || col >= cols) return CenterPan;

            float t = (col - (cols - 1) / 2.0f) / ((cols - 1) / 2.0f); // -1..+1
            if (Math.Abs(t) < 0.0001f) return CenterPan;

            float sign = Math.Sign(t);
            // Curva logarítmica: abre los extremos del tablero (columnas 0 y 7)
            // acentuando la separación estéreo sin desfases.
            float mag = (float)Math.Pow(Math.Abs(t), 1.4);
            return MaxPan * sign * mag;
        }

        public static float PanColumn(int col)
        {
            return Pan(col, BoardColumns);
        }

        // Fila -> profundidad 0 (frente/cerca) .. 1 (fondo/lejos). fila<0 -> frente.
        public static float DepthForRow(int row)
        {
            if (row < 0) return 0.0f;
            if (row >= BoardRows) row = BoardRows - 1;
            if (row <= 0) return 1.0f;
            return (float)(BoardRows - 1 - row) / (BoardRows - 1);
        }

        // Fila -> elevación 0 (suelo) .. 1 (techo/caída).
        public static float ElevationForRow(int row)
        {
            if (row < 0) return 0.0f;
            if (row >= BoardRows) return 0.0f;
            return (float)(BoardRows - 1 - row) / (BoardRows - 1);
        }

        // Easing smoothstep para el barrido lateral/profundidad.
        public static float EaseSweep(float t)
        {
            if (t <= 0.0f) return 0.0f;
            if (t >= 1.0f) return 1.0f;
            return t * t * (3.0f - 2.0f * t);
        }

        public static float SweepPan(float fromPan, float toPan, float progress)
        {
            return fromPan + (toPan - fromPan) * EaseSweep(progress);
        }

        // Ganancia de volumen por distancia: frente 1.0, fondo 0.72.
        public static float VolumeForDepth(float depthFar)
        {
            if (depthFar <= 0.0f) return 1.0f;
            if (depthFar >= 1.0f) return 0.72f;
            return 1.0f - 0.28f * depthFar;
        }

        public static EnvironmentAcoustics GetEnvironmentAcoustics(AudioEnvironment env)
        {
            switch (env)
            {
                case AudioEnvironment.UndergroundCavern:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -26.0f,
                        ReverbTime = 900.0f,
                        HighFreqRTRatio = 0.40f,
                        LowPassCutoff = 8000.0f,
                        StereoWidth = 1.10f,
                        PresenceGain = 1.5f,
                        PresenceFreq = 220.0f
                    };
                case AudioEnvironment.GlacialChamber:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -27.0f,
                        ReverbTime = 700.0f,
                        HighFreqRTRatio = 0.85f,
                        LowPassCutoff = 19000.0f,
                        StereoWidth = 1.12f,
                        PresenceGain = 1.5f,
                        PresenceFreq = 7000.0f
                    };
                case AudioEnvironment.TwilightGarden:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -30.0f,
                        ReverbTime = 500.0f,
                        HighFreqRTRatio = 0.70f,
                        LowPassCutoff = 19000.0f,
                        StereoWidth = 1.10f,
                        PresenceGain = 1.0f,
                        PresenceFreq = 3500.0f
                    };
                case AudioEnvironment.Sanctuary:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -28.0f,
                        ReverbTime = 1000.0f,
                        HighFreqRTRatio = 0.65f,
                        LowPassCutoff = 18000.0f,
                        StereoWidth = 1.10f,
                        PresenceGain = 1.0f,
                        PresenceFreq = 1200.0f
                    };
                case AudioEnvironment.EnergyConduit:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -29.0f,
                        ReverbTime = 550.0f,
                        HighFreqRTRatio = 0.80f,
                        LowPassCutoff = 20000.0f,
                        StereoWidth = 1.08f,
                        PresenceGain = 1.5f,
                        PresenceFreq = 4000.0f
                    };
                case AudioEnvironment.VictorianSalon:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -31.0f,
                        ReverbTime = 400.0f,
                        HighFreqRTRatio = 0.40f,
                        LowPassCutoff = 16000.0f,
                        StereoWidth = 1.04f,
                        PresenceGain = 0.8f,
                        PresenceFreq = 1800.0f
                    };
                case AudioEnvironment.CrystalTemple:
                default:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -28.0f,
                        ReverbTime = 800.0f,
                        HighFreqRTRatio = 0.75f,
                        LowPassCutoff = 20000.0f,
                        StereoWidth = 1.08f,
                        PresenceGain = 1.0f,
                        PresenceFreq = 5500.0f
                    };
            }
        }

        // Construcción del objeto de audio espacial con cálculo de DRR desacoplado
        public static SpatialAudioObject CreateAudioObject(int col, int row, AudioEnvironment env, AudioObjectClass category = AudioObjectClass.BoardGem)
        {
            float pan = (col < 0) ? CenterPan : PanColumn(col);
            float depth = (row < 0) ? 0.0f : DepthForRow(row);
            float elevation = (row < 0) ? 0.0f : ElevationForRow(row);
            var acoustics = GetEnvironmentAcoustics(env);

            float directPan = Math.Max(-1.0f, Math.Min(1.0f, pan * acoustics.StereoWidth));
            float directVol = VolumeForDepth(depth);

            // DRR (Direct-to-Reverberant Ratio): el envío de sala aumenta sutilmente
            // con la profundidad del objeto (+1 a +3 dB de reflejos en filas de fondo),
            // mientras que el sonido directo mantiene su ataque 100% brillante.
            float roomMix = acoustics.ReverbMix + (depth * 2.5f);

            return new SpatialAudioObject
            {
                X = (col < 0) ? 0f : ((col - 3.5f) / 3.5f),
                Y = depth,
                Z = elevation,
                DirectPan = directPan,
                DirectVolume = directVol,
                DrrReverbMix = roomMix,
                DrrReverbTime = acoustics.ReverbTime,
                DrrHighFreqRatio = acoustics.HighFreqRTRatio,
                Category = category
            };
        }
    }
}
