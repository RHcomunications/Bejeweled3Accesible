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

        // Fuerza del HRTF (0..1): cuanto aplica ITD/ILD. 1 = HRTF completo;
        // valores menores aplanan la imagen lateral (menos HRTF) para que los
        // perfiles menos "teatrales" suenen mas centrados y secos, y el perfil
        // Objeto 3D (Atmos) quede como el mas espacial de todos. 0 = sin HRTF
        // (la fuente suena centrada; otro perfil se encarga del paneo plano).
        public float HrtfStrength = 1.0f;

        // Profundidad actual: 0 = lejos .. 1 = frente. Solo Stage2D la baja;
        // CleanArcade la deja en 1 (plana).
        public float Depth = 1.0f;

        // Hinchazon del glide (Stage2D): 1.0 en los extremos, ~1.10 al cruzar
        // el centro, aplicada al volumen.
        public float Bulge = 1.0f;

        // ---- Capa 3D (paradigma Dolby Atmos) ------------------------------
        // Ganancia de distancia (0..1): la calcula SpatialAudioEngine a partir
        // de la distancia real al listener (y el radio volumetrico). 1 = sin
        // cambio para los perfiles 2D.
        public float DistanceGain = 1.0f;

        // Tilt de elevacion (dB): atenuacion sutil por diferencia de altura.
        // 0 = sin tilt.
        public float ElevationTiltDb = 0.0f;

        // Corte del paso-bajo de absorcion de aire (Hz). 0 = sin filtrar
        // (transparente, perfiles 2D). El motor 3D lo fija segun la distancia.
        public float AirCutoffHz = 0.0f;

        // true cuando la pose la dicta el motor 3D (perfil Atmos): el volumen
        // de distancia lo aporta SOLO DistanceGain * tilt, sin la profundidad 2D
        // (Depth/Bulge) ni el hinchazon del glide. Asi el perfil 3D no duplica
        // la atenuacion de profundidad del tablero y suena con geometria real.
        public bool SpatialPose = false;

        private readonly float[] _delayL = new float[DelayLineSamples];
        private readonly float[] _delayR = new float[DelayLineSamples];
        private int _delayPos = 0;
        private float _lpL = 0.0f;
        private float _lpR = 0.0f;

        // Procesa `frames` muestras mono de `monoIn` y escribe `frames` frames
        // estereo intercalados (L,R,L,R...) en `stereoOut`. Pipeline: ITD + ILD
        // (posicion) * (volumen de profundidad 2D * distancia 3D * tilt) y, si
        // AirCutoffHz > 0, un paso-bajo de un polo (RC) por oido (absorcion de
        // aire, bilateral). Sin estado espectral cuando AirCutoffHz = 0.
        public void Process(float[] monoIn, int frames, float[] stereoOut)
        {
            float az = AzimuthDeg;
            float hrtf = HrtfStrength;
            // La fuerza del perfil (HrtfStrength) escala el HRTF completo
            // (ITD + ILD): hrtf=1 HRTF al maximo (Atmos, el mas espacial y con
            // el paneo mas abierto), hrtf menor perfiles mas planos/secs. La ILD
            // nunca llega a 0 (los perfiles mantienen un paneo minimo para que
            // las columnas sigan ubicandose izquierda/derecha).
            float itd = SpatialAudio.ItdSamples(az, SampleRate) * hrtf;
            float farGainRaw = SpatialAudio.FarEarGain(az);
            float farGain = 1.0f - (1.0f - farGainRaw) * hrtf;
            float elevGain = SpatialAudio.DbToLinear(ElevationTiltDb);
            // Perfil 3D (Atmos): la atenuacion de distancia es SOLO DistanceGain
            // (geometria real) * tilt. Los perfiles 2D usan la profundidad del
            // tablero (Depth/Bulge) y dejan DistanceGain en 1.
            float depthVol = SpatialPose ? 1.0f : SpatialAudio.DepthVolume(Depth);
            float bulge = SpatialPose ? 1.0f : Bulge;
            float dist = depthVol * bulge * DistanceGain * elevGain;

            // La fuente a la derecha (az > 0) oye antes el oido derecho.
            bool farIsLeft = az > 0.0f;

            // Coeficiente del paso-bajo RC de aire. 0 (o >= Nyquist) = bypass.
            float a = AirLpCoeff(AirCutoffHz);

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

                float l, r;
                if (farIsLeft)
                {
                    l = outFar;
                    r = outNear;
                }
                else
                {
                    l = outNear;
                    r = outFar;
                }

                if (a <= 0.0f)
                {
                    stereoOut[s++] = l;
                    stereoOut[s++] = r;
                }
                else
                {
                    _lpL += a * (l - _lpL);
                    _lpR += a * (r - _lpR);
                    stereoOut[s++] = _lpL;
                    stereoOut[s++] = _lpR;
                }
            }
        }

        // Coeficiente del paso-bajo de un polo (RC) para el corte fc. Devuelve
        // 0 cuando el corte es 0 (sin filtrar) o supera ~Nyquist (bypass
        // transparente): asi los perfiles 2D no sufren ningun procesado.
        private float AirLpCoeff(float cutoffHz)
        {
            if (cutoffHz <= 0.0f) return 0.0f;
            float c = Math.Min(cutoffHz, SampleRate * 0.49f);
            return 1.0f - (float)Math.Exp(-2.0 * Math.PI * c / SampleRate);
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