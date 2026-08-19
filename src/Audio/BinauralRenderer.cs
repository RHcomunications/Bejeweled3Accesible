using System;

namespace Bejeweled3Accessible.Audio
{
    // Render binaural de un efecto posicionado en el tablero.
    //
    // Modelo HRTF paramétrico (sin datos medidos, sin DSP externo): coloca un
    // sonido mono en un azimuth (columna) y una profundidad (fila) usando las
    // pistas fisiológicas del oído humano:
    //  - ITD: retardo interaural (ley de Woodworth) aplicado al oído lejano
    //    con un delay line fraccional (interpolación lineal): la fuente se
    //    oye antes en el oído cercano (hasta ~0.58 ms de diferencia a ±75°).
    //  - ILD + sombra de cabeza: el oído lejano suena más bajo (hasta ~-6.7 dB)
    //    y con un paso-bajo cuyo corte baja cuanto más lateral está la fuente,
    //    como la sombra acústica que proyecta la cabeza.
    //  - Distancia (Stage2D): volumen + absorción de aire (paso-bajo en AMBOS
    //    oídos). NUNCA se cambia el tono: los sonidos reales del juego se
    //    escuchan afinados, tal cuál los mezcló PopCap.
    // El renderer es exclusivo de los efectos posicionados del tablero: la
    // música (el módulo real) y las voces nunca pasan por aquí, se escuchan
    // centradas, secas y sin procesar.
    //
    // La pose (AzimuthDeg, Depth, Bulge) la escribe el hilo del motor (timers
    // de swipe) y la lee el hilo de audio de BASS en cada bloque: los cambios
    // se aplican por bloque, sin interrupciones ni glitches.
    public sealed class BinauralRenderer
    {
        // Sample rate del stream de salida (el motor mezcla a 44.1 kHz).
        public const float SampleRate = 44100.0f;

        // Longitud del delay line por oído: cubre la ITD máxima (~0.58 ms a
        // ±75°, ~26 muestras a 44.1 kHz) con margen.
        private const int DelayLineSamples = 64;

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
        private float _lpFar = 0.0f;
        private float _lpNear = 0.0f;

        // Procesa `frames` muestras mono de `monoIn` y escribe `frames` frames
        // estéreo intercalados (L,R,L,R...) en `stereoOut` (debe tener al menos
        // 2*frames elementos).
        public void Process(float[] monoIn, int frames, float[] stereoOut)
        {
            float az = AzimuthDeg;
            float thetaRad = Math.Abs(az) * (float)(Math.PI / 180.0);

            // La fuente a la derecha (az > 0) oye antes el oído derecho.
            bool farIsLeft = az > 0.0f;

            float itd = SpatialAudio.ItdSamples(az, SampleRate);
            float farGain = SpatialAudio.FarEarGain(az);
            float farCutoff = SpatialAudio.HeadShadowCutoffHz(az);
            float airCutoff = SpatialAudio.AirCutoffHz(Depth);

            // El oído lejano combina sombra de cabeza + absorción de aire; el
            // cercano solo absorción de aire (la distancia afecta a ambos).
            float aFar = OnePoleCoeff(Math.Min(farCutoff, airCutoff));
            float aNear = OnePoleCoeff(airCutoff);

            float dist = SpatialAudio.DepthVolume(Depth) * Bulge;

            int s = 0;
            for (int i = 0; i < frames; i++)
            {
                float x = monoIn[i];

                _delayL[_delayPos] = x;
                _delayR[_delayPos] = x;

                float near = x;
                float far = ReadDelayed(_delayPos, itd);
                _delayPos = (_delayPos + 1) % DelayLineSamples;

                _lpFar += aFar * (far - _lpFar);
                _lpNear += aNear * (near - _lpNear);

                float outFar = _lpFar * farGain * dist;
                float outNear = _lpNear * dist;

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

        // Coeficiente del paso-bajo de un polo para el corte fc a SampleRate.
        private static float OnePoleCoeff(float cutoffHz)
        {
            if (cutoffHz <= 0.0f) return 1.0f;
            float c = Math.Min(cutoffHz, SampleRate * 0.45f);
            return 1.0f - (float)Math.Exp(-2.0 * Math.PI * c / SampleRate);
        }
    }
}