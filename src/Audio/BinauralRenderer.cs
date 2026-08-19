using System;

namespace Bejeweled3Accessible.Audio
{
    // Render binaural de un efecto posicionado en el tablero.
    //
    // MODELO DE OBJETO EN EL ESPACIO (estilo Dolby): cada efecto es un objeto
    // sonoro que se coloca en la escena SIN destruir su timbre. El sonido
    // original (la mezcla de PopCap) se conserva intacto: NADA de pasos-bajo
    // de oscurecimiento. La posición se aplica con las pistas fisiológicas
    // mínimas:
    //  - ITD: retardo interaural (ley de Woodworth) aplicado al oído lejano
    //    con un delay line fraccional (interpolación lineal): la fuente se
    //    oye antes en el oído cercano (hasta ~0.58 ms de diferencia a ±75°).
    //  - ILD + sombra sutil: el oído lejano suena más bajo (hasta ~-5.3 dB) y
    //    con un estante (shelf) de agudos muy suave (hasta ~-2.5 dB por
    //    encima de 4 kHz a ±75°), como la sombra acústica real de la cabeza:
    //    el timbre se mantiene brillante, nunca "opaco".
    //  - Distancia (Stage2D): SOLO volumen (0.80 lejos .. 1.00 cerca). La
    //    distancia en el mundo real es nivel, no ecualización: sin absorción
    //    de aire que apague los agudos. NUNCA se cambia el tono: los sonidos
    //    reales del juego se escuchan afinados, tal cuál los mezcló PopCap.
    // El renderer es exclusivo de los efectos posicionados del tablero: la
    // música (el módulo real) y las voces nunca pasan por aquí, se escuchan
    // centradas, secas y sin procesar.
    //
    // La pose (AzimuthDeg, Depth, Bulge) la escribe el hilo del motor (timers
    // de swipe) y la lee el hilo de audio de BASS en cada bloque: los cambios
    // se aplican por bloque, sin interrupciones ni glitches.
    public sealed class BinauralRenderer
    {
        // Tasa de muestreo del stream que ve el DSP: 44.1 kHz por defecto.
        // Los OGG reales del juego a 22.05 kHz se reproducen a su tasa nativa
        // (la bass.dll reducida no resamplea vía BASS_ATTRIB_FREQ), así que el
        // renderer se configura con la tasa real del fichero y la matemática
        // de ITD sigue siendo correcta.
        public float SampleRate = 44100.0f;

        // Longitud del delay line por oído: cubre la ITD máxima (~0.58 ms a
        // ±75°, ~26 muestras a 44.1 kHz) con margen.
        private const int DelayLineSamples = 64;

        // Frecuencia del estante (shelf) de sombra de cabeza del oído lejano.
        private const float ShadowShelfHz = 4000.0f;

        // Azimuth actual en grados: -75 (izquierda) .. +75 (derecha), 0 = frente.
        public float AzimuthDeg;

        // Profundidad actual: 0 = lejos .. 1 = frente. Solo Stage2D la baja;
        // CleanArcade la deja en 1 (plana).
        public float Depth = 1.0f;

        // Hinchazón del glide (Stage2D): 1.0 en los extremos, ~1.10 al cruzar
        // el centro, aplicada al volumen.
        public float Bulge = 1.0f;

        private readonly float[] _delayL = new float[DelayLineSamples];
        private readonly float[] _delayR = new float[DelayLineSamples];
        private int _delayPos = 0;
        private float _shadowLp = 0.0f;

        // Procesa `frames` muestras mono de `monoIn` y escribe `frames` frames
        // estéreo intercalados (L,R,L,R...) en `stereoOut` (debe tener al menos
        // 2*frames elementos).
        public void Process(float[] monoIn, int frames, float[] stereoOut)
        {
            float az = AzimuthDeg;
            float itd = SpatialAudio.ItdSamples(az, SampleRate);
            float farGain = SpatialAudio.FarEarGain(az);
            float shadowGain = SpatialAudio.FarEarShadowGain(az);
            float dist = SpatialAudio.DepthVolume(Depth) * Bulge;

            // La fuente a la derecha (az > 0) oye antes el oído derecho.
            bool farIsLeft = az > 0.0f;

            // Coeficiente del paso-bajo de un polo que alimenta el estante de
            // sombra: el oído lejano resta una fracción (shadowGain) de su
            // contenido por encima de 4 kHz, nunca un oscurecimiento completo.
            float a = ShelfLpCoeff(ShadowShelfHz);

            int s = 0;
            for (int i = 0; i < frames; i++)
            {
                float x = monoIn[i];

                _delayL[_delayPos] = x;
                _delayR[_delayPos] = x;

                float near = x;
                float far = ReadDelayed(_delayPos, itd);
                _delayPos = (_delayPos + 1) % DelayLineSamples;

                _shadowLp += a * (far - _shadowLp);
                float farShadowed = far - shadowGain * (far - _shadowLp);

                float outFar = farShadowed * farGain * dist;
                float outNear = near * dist;

                if (farIsLeft)
                {
                    stereoOut[s++] = outFar;
                    stereoOut[s++] = outNear;
                }
                else
                {
                    stereoOut[s++] = outNear;
                    stereoOut[s++] = outFar;
                }
            }
        }

        // Retardo fraccional: lee la muestra a `itdSamples` muestras de la
        // posición de escritura, interpolando linealmente entre las dos taps
        // adyacentes (la ITD no cae en una muestra entera).
        private float ReadDelayed(int writePos, float itdSamples)
        {
            if (itdSamples <= 0.0f) return _delayL[writePos];
            int whole = (int)itdSamples;
            float frac = itdSamples - whole;

            int i0 = ((writePos - whole) + DelayLineSamples) % DelayLineSamples;
            int i1 = (i0 - 1 + DelayLineSamples) % DelayLineSamples;

            float a = _delayL[i0];
            float b = _delayL[i1];
            return a + (b - a) * frac;
        }

        // Coeficiente del paso-bajo de un polo que alimenta el estante de
        // sombra de cabeza, para el corte fc a SampleRate.
        private float ShelfLpCoeff(float cutoffHz)
        {
            if (cutoffHz <= 0.0f) return 1.0f;
            float c = Math.Min(cutoffHz, SampleRate * 0.45f);
            return 1.0f - (float)Math.Exp(-2.0 * Math.PI * c / SampleRate);
        }
    }
}