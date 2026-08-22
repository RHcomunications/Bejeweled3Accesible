using System;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Audio
{
    // Objeto acustico independiente en el espacio (paradigma Dolby Atmos).
    //
    // Cada evento sonoro del juego es un OBJETO con metadatos de posicion,
    // velocidad, elevacion y dispersion angular. El SpatialAudioEngine actualiza
    // su pose cada frame (60 FPS) y escribe en su BinauralRenderer la posicion
    // 3D resultante (azimut, distancia, absorcion de aire, tilt de elevacion),
    // de modo que el renderer solo aplica retardo/ganancia/filtro sobre la
    // senal original del objeto: el timbre viaja intacto (principio de objeto).
    public sealed class SpatialAudioObject
    {
        // Posicion mundial en metros (X lateral, Y altura, Z profundidad).
        public Vector3 Position;

        // Velocidad en m/s: el motor la integra cada frame para mover el objeto.
        public Vector3 Velocity;

        // Dispersion angular del objeto en grados: fuentes extensas (cuerpos
        // volumetricos) tienen dispersion ancha; las puntuales, estrecha.
        public float AngleSpreadDeg;

        // true = fuente de area/cuerpo extenso (explosion de cascadas, power-up
        // de tablero): mantiene presencia en un radio amplio (minDistance mayor).
        public bool IsVolumetric;

        // Radios de atenuacion por distancia (metros).
        public double MinDistance = SpatialAudio.PointMinDistance;
        public double MaxDistance = SpatialAudio.PointMaxDistance;

        // Overrides de calibracion (>= 0 fuerzan el valor, ignorando la
        // distancia real). Se usan en la Escuela de Audio para que la demo
        // "lejos con aire" se oiga con el filtro de aire a ganancia plena,
        // sin que la atenuacion por distancia la silencie antes de forma
        // prematura.
        public double DistanceGainOverride = -1.0;
        public double AirCutoffOverride = -1.0;

        // Exponente de la atenuacion por distancia (1 = lineal, como siempre).
        // El perfil Objeto 3D (Atmos) lo sube (>1) para que la diferencia de
        // volumen frente->fondo sea evidente y la profundidad se oiga de verdad,
        // no solo como un leve realce. La Escuela de Audio lo deja en 1.
        public double DistanceGainExponent = 1.0;

        // Override del tilt de elevacion (dB). El centinela -999 indica
        // "automatico" (calculado por la diferencia de altura). Se usa en la
        // Escuela de Audio para exagerar la demo de altura (suelo/gema/aerea) y
        // que sea perceptible: un objeto real solo se atenúa ~4 dB entre suelo y
        // zona aerea, demasiado sutil. Los valores validos pueden ser negativos
        // (fuente por encima), por eso el centinela es -999 y no < 0.
        public double ElevationTiltOverride = -999.0;

        // Renderer binaural que el motor reprograma con la pose 3D calculada.
        // Lo asigna quien crea el objeto (SoundEngine) antes de registrarlo.
        public BinauralRenderer Renderer;

        // Animacion de swipe lateral: el motor interpola X de From->To durante
        // SweepDurationMs (mismo esquema de EaseSweep que el glide 2D).
        public double SweepFromX;
        public double SweepToX;
        public int SweepDurationMs;
        public int SweepElapsedMs;

        public bool Active = true;

        public SpatialAudioObject() { }

        public SpatialAudioObject(Vector3 position, double minDistance, double maxDistance)
        {
            Position = position;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }

        // Integra la velocidad en el tiempo dt (segundos).
        public void Integrate(double dt)
        {
            Position.X += Velocity.X * dt;
            Position.Y += Velocity.Y * dt;
            Position.Z += Velocity.Z * dt;
        }
    }

    // Listener (el jugador / tablero). Posicion y orientacion que el motor
    // consulta cada frame para calcular la pose relativa de cada objeto.
    public sealed class SpatialAudioListener
    {
        public Vector3 Position = new Vector3(0.0, 1.0, 0.0);
        public double YawDeg = 0.0; // 0 = mira hacia +Z (al tablero)
    }

    // Gestor del mundo 3D: mantiene el listener y la lista de objetos activos,
    // y por cada frame (Update) integra la velocidad, anima los swipes y
    // recalcula la pose de cada objeto (azimut, distancia, absorcion de aire,
    // tilt de elevacion) escribiendola en su BinauralRenderer. Releasing un
    // objeto lo quita de la lista para liberar su DSP/canal cuando termina.
    public sealed class SpatialAudioEngine
    {
        public static readonly SpatialAudioEngine Instance = new SpatialAudioEngine();

        private readonly object _lock = new object();
        private readonly List<SpatialAudioObject> _objects = new List<SpatialAudioObject>();

        public SpatialAudioListener Listener = new SpatialAudioListener();

        private SpatialAudioEngine() { }

        public void Add(SpatialAudioObject obj)
        {
            if (obj == null) return;
            lock (_lock) { _objects.Add(obj); }
        }

        public void Release(SpatialAudioObject obj)
        {
            if (obj == null) return;
            lock (_lock)
            {
                obj.Active = false;
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] == obj) { _objects.RemoveAt(i); break; }
                }
            }
        }

        public int ActiveCount
        {
            get { lock (_lock) { return _objects.Count; } }
        }

        // Actualiza todos los objetos activos. dt en segundos (tipico 1/60).
        public void Update(double dt)
        {
            double yaw = Listener.YawDeg * Math.PI / 180.0;
            double cosY = Math.Cos(yaw);
            double sinY = Math.Sin(yaw);

            lock (_lock)
            {
                for (int i = 0; i < _objects.Count; i++)
                {
                    SpatialAudioObject obj = _objects[i];
                    if (!obj.Active) continue;

                    // 1) Integracion de la velocidad.
                    obj.Integrate(dt);

                    // 2) Swipe lateral: interpola X con EaseSweep.
                    if (obj.SweepDurationMs > 0 && obj.SweepElapsedMs < obj.SweepDurationMs)
                    {
                        obj.SweepElapsedMs += (int)(dt * 1000.0);
                        double t = (double)obj.SweepElapsedMs / (double)obj.SweepDurationMs;
                        if (t > 1.0) t = 1.0;
                        double e = SpatialAudio.EaseSweep((float)t);
                        obj.Position.X = obj.SweepFromX + (obj.SweepToX - obj.SweepFromX) * e;
                        if (obj.SweepElapsedMs >= obj.SweepDurationMs) obj.SweepDurationMs = 0;
                    }

                    // 3) Vector relativo al listener (con orientacion/yaw).
                    Vector3 rel = obj.Position - Listener.Position;
                    double rx = rel.X * cosY - rel.Z * sinY;
                    double rz = rel.X * sinY + rel.Z * cosY;

                    // 4) Pose 3D -> parametros del renderer.
                    double az = SpatialAudio.AzimuthFromRelative(rx, rz);
                    double dist = rel.Length();
                    double air = (obj.AirCutoffOverride >= 0.0) ? obj.AirCutoffOverride
                                : SpatialAudio.AirAbsorptionCutoffHz(dist);
                    double dg = (obj.DistanceGainOverride >= 0.0) ? obj.DistanceGainOverride
                                : SpatialAudio.DistanceGainFor(dist, obj.MinDistance, obj.MaxDistance);
                    if (obj.DistanceGainExponent != 1.0 && dg > 0.0 && dg <= 1.0)
                        dg = Math.Pow(dg, obj.DistanceGainExponent);
                    double tilt = (obj.ElevationTiltOverride > -900.0) ? obj.ElevationTiltOverride
                                : SpatialAudio.ElevationTiltDb(obj.Position.Y, Listener.Position.Y);

                    if (obj.Renderer != null)
                    {
                        obj.Renderer.AzimuthDeg = (float)az;
                        obj.Renderer.AirCutoffHz = (float)air;
                        obj.Renderer.DistanceGain = (float)dg;
                        obj.Renderer.ElevationTiltDb = (float)tilt;
                    }
                }
            }
        }
    }
}