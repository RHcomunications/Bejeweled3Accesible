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
    }
}
