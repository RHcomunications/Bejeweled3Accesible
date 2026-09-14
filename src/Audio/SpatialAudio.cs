using System;

namespace Bejeweled3Accessible.Audio
{
    // Modelo de audio espacial unico y generado para este juego: cada efecto
    // del tablero se coloca con dos parametros derivados de su celda:
    //   - Pan (L/R): la columna (A..H) se reparte de izquierda a derecha.
    //   - Profundidad (frente->fondo): la fila; las filas traseras suenan mas
    //     lejanas (mas quietas, mas opacas y ligeramente mas amplias).
    // No hay perfiles ni conmutadores: el posicionamiento esta siempre activo
    // y es el mismo para todo el juego. La musica y las voces se escuchan
    // centradas y secas.
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
            // Curva logaritmica: exponente > 1 aplana el centro y empuja los
            // extremos del tablero (columnas 0 y 7) hacia el paneo maximo,
            // acentuando la separacion estereo donde el jugador lo percibe.
            float mag = (float)Math.Pow(Math.Abs(t), 1.4);
            return MaxPan * sign * mag;
        }

        // Pan de una columna del tablero (8 columnas por defecto).
        public static float PanColumn(int col)
        {
            return Pan(col, BoardColumns);
        }

        // Fila -> lejania 0 (frente/cerca) .. 1 (fondo/lejos). fila<0 -> frente.
        public static float DepthForRow(int row)
        {
            if (row < 0) return 0.0f;
            if (row >= BoardRows) row = BoardRows - 1;
            if (row <= 0) return 1.0f;
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

        // Ganancia de volumen por profundidad: frente 1.0, fondo 0.65.
        public static float VolumeForDepth(float depthFar)
        {
            if (depthFar <= 0.0f) return 1.0f;
            if (depthFar >= 1.0f) return 0.65f;
            return 1.0f - 0.35f * depthFar;
        }

        // Corte del paso-bajo de "aire" por profundidad: frente 20 kHz
        // (transparente), fondo ~6 kHz (opaca la lejania). Exponencial.
        public static float AirCutoffForDepth(float depthFar)
        {
            if (depthFar <= 0.0f) return 20000.0f;
            if (depthFar >= 1.0f) return 6000.0f;
            return 20000.0f * (float)Math.Pow(0.3, depthFar);
        }

        // Anchura estereo por profundidad: frente 1.0 (natural), fondo 1.3.
        public static float WidthForDepth(float depthFar)
        {
            if (depthFar <= 0.0f) return 1.0f;
            if (depthFar >= 1.0f) return 1.3f;
            return 1.0f + 0.3f * depthFar;
        }

        // --- Entornos Acústicos Temáticos de Bejeweled 3 ---
        public enum AudioEnvironment
        {
            CrystalTemple,      // Clásico, Menús, Relicarios (cámara de mármol y cristal)
            UndergroundCavern,  // Mina de Diamantes (caverna profunda de roca)
            GlacialChamber,     // Tormenta de Hielo (acústica helada con reflejos brillantes)
            TwilightGarden,     // Mariposas (jardín abierto crepuscular y elevación)
            Sanctuary,          // Zen (santuario envolvente de meditación)
            EnergyConduit,      // Relámpago (conducto elemental de alta energía)
            VictorianSalon      // Poker (salón íntimo de paño y madera noble)
        }

        public struct EnvironmentAcoustics
        {
            public float ReverbMix;         // dB (-96..0)
            public float ReverbTime;        // ms (decaimiento)
            public float HighFreqRTRatio;   // ratio de agudos en la reverb (0.1..0.99)
            public float LowPassCutoff;     // corte de agudos por absorción de sala (Hz)
            public float StereoWidth;       // anchura estéreo del espacio (0.8..1.5)
            public float PresenceGain;      // dB de realce tímbrico (-6..+6)
            public float PresenceFreq;      // Hz de frecuencia central de presencia
        }

        public static EnvironmentAcoustics GetEnvironmentAcoustics(AudioEnvironment env)
        {
            switch (env)
            {
                case AudioEnvironment.UndergroundCavern:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -14.0f,
                        ReverbTime = 1800.0f,
                        HighFreqRTRatio = 0.35f,
                        LowPassCutoff = 6500.0f,
                        StereoWidth = 1.15f,
                        PresenceGain = 3.0f,
                        PresenceFreq = 250.0f
                    };
                case AudioEnvironment.GlacialChamber:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -16.0f,
                        ReverbTime = 800.0f,
                        HighFreqRTRatio = 0.90f,
                        LowPassCutoff = 20000.0f,
                        StereoWidth = 1.25f,
                        PresenceGain = 2.5f,
                        PresenceFreq = 7500.0f
                    };
                case AudioEnvironment.TwilightGarden:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -26.0f,
                        ReverbTime = 600.0f,
                        HighFreqRTRatio = 0.60f,
                        LowPassCutoff = 20000.0f,
                        StereoWidth = 1.35f,
                        PresenceGain = 1.0f,
                        PresenceFreq = 4000.0f
                    };
                case AudioEnvironment.Sanctuary:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -16.0f,
                        ReverbTime = 1500.0f,
                        HighFreqRTRatio = 0.50f,
                        LowPassCutoff = 18000.0f,
                        StereoWidth = 1.20f,
                        PresenceGain = 1.0f,
                        PresenceFreq = 1000.0f
                    };
                case AudioEnvironment.EnergyConduit:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -20.0f,
                        ReverbTime = 700.0f,
                        HighFreqRTRatio = 0.80f,
                        LowPassCutoff = 20000.0f,
                        StereoWidth = 1.20f,
                        PresenceGain = 3.5f,
                        PresenceFreq = 3500.0f
                    };
                case AudioEnvironment.VictorianSalon:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -28.0f,
                        ReverbTime = 400.0f,
                        HighFreqRTRatio = 0.40f,
                        LowPassCutoff = 16000.0f,
                        StereoWidth = 1.00f,
                        PresenceGain = 0.5f,
                        PresenceFreq = 2000.0f
                    };
                case AudioEnvironment.CrystalTemple:
                default:
                    return new EnvironmentAcoustics
                    {
                        ReverbMix = -18.0f,
                        ReverbTime = 1200.0f,
                        HighFreqRTRatio = 0.70f,
                        LowPassCutoff = 20000.0f,
                        StereoWidth = 1.00f,
                        PresenceGain = 1.5f,
                        PresenceFreq = 5500.0f
                    };
            }
        }

        // Elevación por fila: filas superiores (0-2) tienen más brillo de "caída desde el techo",
        // filas inferiores (6-7) tienen más cuerpo sobre el pedestal de piedra.
        public static float ElevationTrebleBoost(int row)
        {
            if (row < 0 || row >= BoardRows) return 0.0f;
            if (row <= 2) return (3 - row) * 1.0f; // +1..+3 dB en filas altas
            return 0.0f;
        }
    }
}
