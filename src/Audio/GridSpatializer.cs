using System;

namespace Bejeweled3Accessible.Audio
{
    // Render espacial del tablero: coloca un efecto con dos parametros
    // (Pan y Profundidad) en estereo. Es un modelo generado y especifico para
    // este juego, sin HRTF ni perfiles:
    //   - Pan (L/R): reparte la columna de izquierda a derecha (pan equal-power).
    //   - Profundidad: las filas traseras suenan mas lejanas (volumen menor,
    //     paso-bajo de "aire" mas cerrado y estereo ligeramente mas amplio).
    // La musica y las voces no pasan por aqui: se escuchan centradas y secas.
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

        private float _lpL = 0.0f;
        private float _lpR = 0.0f;

        // Procesa `frames` muestras mono de `monoIn` y escribe `frames` frames
        // estereo intercalados (L,R,L,R...) en `stereoOut`. Pipeline: pan
        // equal-power * (volumen de profundidad * aire * anchura estereo).
        public void Process(float[] monoIn, int frames, float[] stereoOut)
        {
            float t = (Pan + 1.0f) * 0.5f;            // 0..1
            float lg = (float)Math.Cos(t * Math.PI * 0.5f);
            float rg = (float)Math.Sin(t * Math.PI * 0.5f);

            float vol = SpatialAudio.VolumeForDepth(Depth) * Volume;
            float cut = SpatialAudio.AirCutoffForDepth(Depth);
            float width = SpatialAudio.WidthForDepth(Depth);
            float a = AirLpCoeff(cut);

            int s = 0;
            for (int i = 0; i < frames; i++)
            {
                float x = monoIn[i];
                float l = x * lg;
                float r = x * rg;

                if (a > 0.0f)
                {
                    _lpL += a * (l - _lpL);
                    _lpR += a * (r - _lpR);
                    l = _lpL;
                    r = _lpR;
                }

                float mid = (l + r) * 0.5f;
                float side = (l - r) * 0.5f;
                l = mid + side * width;
                r = mid - side * width;

                l *= vol;
                r *= vol;

                if (l > 1.0f) l = 1.0f; else if (l < -1.0f) l = -1.0f;
                if (r > 1.0f) r = 1.0f; else if (r < -1.0f) r = -1.0f;

                stereoOut[s++] = l;
                stereoOut[s++] = r;
            }
        }

        // Coeficiente del paso-bajo RC de aire. 0 (o >= Nyquist) = bypass
        // transparente: asi el frente suena nítido y el fondo se opaca.
        private float AirLpCoeff(float cutoffHz)
        {
            if (cutoffHz <= 0.0f) return 0.0f;
            float c = Math.Min(cutoffHz, SampleRate * 0.49f);
            return 1.0f - (float)Math.Exp(-2.0 * Math.PI * c / SampleRate);
        }
    }
}
