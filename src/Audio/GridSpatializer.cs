using System;

namespace Bejeweled3Accessible.Audio
{
    // Render espacial del tablero como una SALA de audio objeto (estilo Dolby
    // Atmos / "Tru 2 Life"): cada sonido es un objeto ubicado en una sala del
    // tamano del tablero (8 columnas x 8 filas). Se oye el sonido directo mas
    // los reflejos tempranos de las 4 paredes, cuyo retardo y nivel dependen de
    // la posicion real del objeto en la sala. Asi la profundidad y el "cuerpo"
    // salen de la geometria (como en una sala real), no de un lowpass de aire ni
    // de un reverb generico, que eran los que daban el tono plastico/carbonoso.
    // La musica y las voces NO pasan por aqui (secas y centradas).
    public sealed class GridSpatializer
    {
        // Tasa de muestreo del stream que ve el DSP (igual que la del fichero).
        public float SampleRate = 44100.0f;

        // Pan actual: -1 (izquierda) .. +1 (derecha), 0 = centro.
        public float Pan = 0.0f;

        // Profundidad actual: 0 (frente/cerca) .. 1 (fondo/lejos).
        public float Depth = 0.0f;

        // Ganancia maestra extra (el volumen de SFX lo pone BASS por fuera).
        public float Volume = 1.0f;

        // Columna/fila del tablero (0..7). Si no se indican, se derivan de
        // Pan/Depth (para la musica-ambiente, que usa solo Pan/Depth).
        public int Col = -1;
        public int Row = -1;

        // Dimensiones de la sala (metros), acopladas al tablero. Oyente al frente,
        // centrado. Esto define el tamano percibido de la "sala de las gemas".
        private const float RoomW = 4.0f;   // ancho  <-> columnas
        private const float RoomD = 4.0f;   // profundidad <-> filas
        private const float ListenerY = 1.2f;
        private const float SourceY = 1.0f;
        private const float SpeedOfSound = 343.0f;
        private const float WallReflect = 0.5f;   // coef. de reflexion de pared
        private const int MaxDelay = 4096;        // hasta ~29 m a 48 kHz
        private const int Reflections = 4;        // izq, der, fondo, frente

        private float[] _tapDelayS = new float[Reflections];
        private float[] _tapGain = new float[Reflections];
        private float[] _tapLpCoeff = new float[Reflections];
        private float[] _tapLpState = new float[Reflections];
        private float[][] _tapBuf = new float[Reflections][];
        private int _wp = 0;
        private bool _prepared = false;
        private float _directAtt = 1.0f;
        private float _wetNorm = 1.0f;

        public GridSpatializer()
        {
            for (int i = 0; i < Reflections; i++) _tapBuf[i] = new float[MaxDelay];
        }

        private void EnsurePrepared()
        {
            if (_prepared) return;
            _prepared = true;

            float x, z;
            if (Col >= 0 && Row >= 0)
            {
                x = (Col + 0.5f) / 8.0f * RoomW;
                z = SpatialAudio.DepthForRow(Row) * RoomD;
            }
            else
            {
                x = (Pan / SpatialAudio.MaxPan * 0.5f + 0.5f) * RoomW;
                z = Depth * RoomD;
            }

            float lx = RoomW * 0.5f;
            float dy = SourceY - ListenerY;
            float d = (float)Math.Sqrt((x - lx) * (x - lx) + dy * dy + z * z);
            _directAtt = 1.0f / (1.0f + 0.20f * d);

            // Fuentes imagen de las 4 paredes (modelo de caja de zapatos).
            float[] imgX = new float[Reflections];
            float[] imgZ = new float[Reflections];
            imgX[0] = -x;              imgZ[0] = z;               // pared izquierda
            imgX[1] = 2.0f * RoomW - x; imgZ[1] = z;             // pared derecha
            imgX[2] = x;              imgZ[2] = 2.0f * RoomD - z; // pared fondo
            imgX[3] = x;              imgZ[3] = -z;              // pared frente

            float sumGain = 0.0f;
            for (int i = 0; i < Reflections; i++)
            {
                float di = (float)Math.Sqrt((imgX[i] - lx) * (imgX[i] - lx) + dy * dy + imgZ[i] * imgZ[i]);
                float pathDiff = (di - d) / SpeedOfSound * SampleRate; // muestras
                if (pathDiff > 1.0f && pathDiff < MaxDelay)
                {
                    _tapDelayS[i] = pathDiff;
                    _tapGain[i] = (d / di) * WallReflect;
                    // Rebote de pared: sin lowpass que opaque el sonido (el cuerpo
                    // sale de la geometria, no de filtrar). Corte alto y fijo.
                    float cutoff = 20000.0f;
                    float cc = Math.Min(cutoff, SampleRate * 0.49f);
                    _tapLpCoeff[i] = 1.0f - (float)Math.Exp(-2.0 * Math.PI * cc / SampleRate);
                }
                else
                {
                    _tapGain[i] = 0.0f;
                    _tapDelayS[i] = 1.0f;
                }
                sumGain += _tapGain[i];
            }

            // Normaliza para no saturar con los rebotes (evita distorsion).
            _wetNorm = 1.0f / (1.0f + 0.7f * sumGain);
        }

        // Procesa `frames` muestras mono de `monoIn` y escribe `frames` frames
        // estereo intercalados (L,R,L,R...) en `stereoOut`. Pipeline: objeto en
        // sala -> directo paneado + reflejos tempranos paneados -> anchura.
        public void Process(float[] monoIn, int frames, float[] stereoOut)
        {
            EnsurePrepared();

            float t = (Pan + 1.0f) * 0.5f;            // 0..1
            float lg = (float)Math.Cos(t * Math.PI * 0.5f);
            float rg = (float)Math.Sin(t * Math.PI * 0.5f);

            float vol = Volume * _directAtt;
            // Anchura estereo por profundidad: frente 1.0, fondo 1.25.
            float width = 1.0f + 0.25f * Depth;

            int s = 0;
            for (int i = 0; i < frames; i++)
            {
                float x = monoIn[i];

                // Escribe la muestra en las lineas de retardo de los rebotes.
                for (int r = 0; r < Reflections; r++)
                    _tapBuf[r][_wp] = x;

                // Sonido directo paneado.
                float l = x * lg * vol;
                float rr = x * rg * vol;

                // Reflejos tempranos de las paredes.
                for (int rp = 0; rp < Reflections; rp++)
                {
                    if (_tapGain[rp] <= 0.0f) continue;
                    int idx = _wp - (int)_tapDelayS[rp];
                    if (idx < 0) idx += MaxDelay;
                    float delayed = _tapBuf[rp][idx];
                    _tapLpState[rp] += _tapLpCoeff[rp] * (delayed - _tapLpState[rp]);
                    float rv = _tapLpState[rp] * _tapGain[rp];
                    l += rv * lg;
                    rr += rv * rg;
                }

                // Anchura estereo.
                float mid = (l + rr) * 0.5f;
                float side = (l - rr) * 0.5f;
                l = mid + side * width;
                rr = mid - side * width;

                l *= _wetNorm;
                rr *= _wetNorm;

                if (l > 1.0f) l = 1.0f; else if (l < -1.0f) l = -1.0f;
                if (rr > 1.0f) rr = 1.0f; else if (rr < -1.0f) rr = -1.0f;

                stereoOut[s++] = l;
                stereoOut[s++] = rr;

                if (++_wp >= MaxDelay) _wp = 0;
            }
        }
    }
}
