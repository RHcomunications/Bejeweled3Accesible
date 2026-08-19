using System;

namespace Bejeweled3Accessible.Audio
{
    // Render binaural de un efecto posicionado en el tablero.
    //
    // PRINCIPIO DOLBY DE SONIDO ORIENTADO A OBJETOS: cada efecto del tablero
    // es un OBJETO sonoro que viaja por la escena CON SU SENAL INTACTA. El
    // renderer NUNCA procesa el espectro del objeto: ni pasos-bajo, ni
    // estantes, ni ninguna ecualizacion. La posicion se aplica con las dos
    // unicas pistas que no alteran el timbre:
    //  - ITD: retardo interaural (ley de Woodworth) aplicado al oido lejano
    //    con un delay line fraccional (interpolacion lineal): la fuente se
    //    oye antes en el oido cercano (hasta ~0.58 ms de diferencia a +-75).
    //  - ILD: el oido lejano suena mas bajo (hasta ~-5.3 dB), un simple
    //    volumen: la senal es identica, solo mas atenuada.
    //  - Distancia (Stage2D): SOLO volumen (0.80 lejos .. 1.00 cerca). La
    //    distancia en el mundo real es nivel, no ecualizacion.
    // El resultado es la muestra original de PopCap al 100% de su brillo:
    // nada de opacidad, porque no hay nada que pueda oscurecerla.
    //
    // La musica (el modulo real) y las voces nunca pasan por aqui: se
    // escuchan centradas, secas y sin procesar.
    //
    // La pose (AzimuthDeg, Depth, Bulge) la escribe el hilo del motor (timers
    // de swipe) y la lee el hilo de audio de BASS en cada bloque: los cambios
    // se aplican por bloque, sin interrupciones ni glitches.
    public sealed class BinauralRenderer
    {
        // Tasa de muestreo del stream que ve el DSP: 44.1 kHz por defecto.
        // Los OGG reales del juego a 22.05 kHz se reproducen a su tasa nativa
        // (la bass.dll reducida no resamplea via BASS_ATTRIB_FREQ), asi que el
        // renderer se configura con la tasa real del fichero y la matematica
        // de ITD sigue siendo correcta.
        public float SampleRate = 44100.0f;

        // Longitud del delay line por oido: cubre la ITD maxima (~0.58 ms a
        // +-75, ~26 muestras a 44.1 kHz) con margen.
        private const int DelayLineSamples = 64;

        // Azimuth actual en grados: -75 (izquierda) .. +75 (derecha), 0 = frente.
        public float AzimuthDeg;

        // Profundidad actual: 0 = lejos .. 1 = frente. Solo Stage2D la baja;
        // CleanArcade la deja en 1 (plana).
        public float Depth = 1.0f;

        // Hinchazon del glide (Stage2D): 1.0 en los extremos, ~1.10 al cruzar
        // el centro, aplicada al volumen.
        public float Bulge = 1.0f;

        private readonly float[] _delayL = new float[DelayLineSamples];
        private readonly float[] _delayR = new float[DelayLineSamples];
        private int _delayPos = 0;

        // Procesa `frames` muestras mono de `monoIn` y escribe `frames` frames
        // estereo intercalados (L,R,L,R...) en `stereoOut` (debe tener al menos
        // 2*frames elementos). Sin estado espectral: solo retardo y ganancia.
        public void Process(float[] monoIn, int frames, float[] stereoOut)
        {
            float az = AzimuthDeg;
            float itd = SpatialAudio.ItdSamples(az, SampleRate);
            float farGain = SpatialAudio.FarEarGain(az);
            float dist = SpatialAudio.DepthVolume(Depth) * Bulge;

            // La fuente a la derecha (az > 0) oye antes el oido derecho.
            bool farIsLeft = az > 0.0f;

            int s = 0;
            for (int i = 0; i < frames; i++)
            {
                float x = monoIn[i];

                _delayL[_delayPos] = x;
                _delayR[_delayPos] = x;

                float near = x;
                float far = ReadDelayed(_delayPos, itd);
                _delayPos = (_delayPos + 1) % DelayLineSamples;

                float outFar = far * farGain * dist;
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
        // posicion de escritura, interpolando linealmente entre las dos taps
        // adyacentes (la ITD no cae en una muestra entera). La interpolacion
        // lineal entre taps contiguos es esencialmente transparente: no filtra.
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
    }
}