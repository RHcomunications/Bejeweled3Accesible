using System;

namespace Bejeweled3Accessible.Audio
{
    // Selectable spatial-audio profiles. All of them share the same column
    // math; they differ in how strongly the stage is rendered:
    //  - Stage2D: the full theatrical soundscape - binaural HRTF with depth
    //    (distance volume + air absorption) and the volume swell on glides.
    //  - CleanArcade (default): the original arcade character - crisp and
    //    dry. Binaural HRTF with the lateral cues only (ITD, ILD, head
    //    shadow); every row at full presence, flat distance.
    //  - SimplePan: the bare minimum - just the left/right pan (no HRTF),
    //    placed instantly (no glide animation), flat depth. Closest to a
    //    plain stereo game without any virtual stage.
    public enum SpatialProfile
    {
        Stage2D = 0,
        CleanArcade = 1,
        SimplePan = 2,
        // Objeto 3D (paradigma Dolby Atmos): cada sonido es un objeto acustico
        // independiente en el espacio (X,Y,Z + velocidad + elevacion + flag
        // volumetrico). El renderer aplica ITD + ILD + atenuacion por distancia
        // + absorcion de aire (low-pass real) + tilt de elevacion.
        Atmos3D = 3
    }

    // Vector tridimensional en metros (precision doble; el renderer trabaja en float).
    public struct Vector3
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3(double x, double y, double z) { X = x; Y = y; Z = z; }

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
    }

    // Binaural HRTF / spatial-audio math for the 8x8 Bejeweled board.
    //
    // PRINCIPIO DOLBY DE SONIDO ORIENTADO A OBJETOS: cada efecto del tablero
    // es un OBJETO sonoro que viaja por la escena CON SU SENAL INTACTA. La
    // posicion se aplica con las pistas fisiologicas que NO alteran el timbre:
    //  - Every board column A..H maps to an AZIMUTH angle (-75°..+75°). The
    //    renderer places a sound there with two cues: ITD (interaural time
    //    difference, Woodworth's law: the far ear hears later) and ILD
    //    (interaural level difference: the far ear hears quieter). No
    //    filtering, no shelves, no low-passes: the signal is never reshaped.
    //  - Rows add a DEPTH plane: the top of the board (row 0) is the far end
    //    of the stage and the bottom (row 7) is in front of the player. A gem
    //    in the back sounds quieter (distance = level, like Dolby), with the
    //    timbre untouched. Depth NEVER changes pitch: the real game sounds
    //    must stay in tune, exactly as PopCap mixed them.
    //  - The music (the real .mo3 module) stays centered, dry and untouched:
    //    it carries PopCap's own mix, and the HRTF never processes it.
    //  - Voices are ALWAYS centered: the speaker/announcer must stay in the
    //    middle, never tied to a gem column.
    //  - UI / non-positional SFX (menus, buttons, HUD) are centered too; a
    //    sound without a column must NEVER wander to one side.
    //  - A gem "swipe" (swap/cascade) interpolates the azimuth smoothly from
    //    the source column to the destination column via EaseSweep, so the
    //    movement is heard, not just the final position. Stage2D additionally
    //    swells the volume mid-flight (SweepPassBulge) so the gem seems to
    //    sweep past the listener.
    public static class SpatialAudio
    {
        // Softest hard cap so the extreme columns stay inside the stereo field
        // without banging the drivers (used by the SimplePan profile and the
        // non-binaural fallback only).
        public const float MaxPan = 0.85f;

        // Number of columns of the board (Board.Cols mirror).
        public const int BoardColumns = 8;

        // Number of rows of the board (Board.Rows mirror).
        public const int BoardRows = 8;

        // Pan for the empty / non-positional case: dead center.
        public const float CenterPan = 0.0f;

        // The voice of the announcer is never spatialized; it is always centered.
        public const float VoicePan = 0.0f;

        // ---- Binaural HRTF layer -----------------------------------------

        // Azimuth of the extreme columns: ±75°. Beyond ~75° ITD saturates and
        // front/back confusions appear, so the stage spans exactly this cone.
        public const float MaxAzimuthDeg = 75.0f;

        // Mean adult head radius and sound speed, for Woodworth's ITD law.
        public const float HeadRadiusM = 0.0875f;
        public const float SoundSpeedMps = 343.0f;

        // Maps a board column (0..cols-1) to an azimuth in degrees in
        // [-MaxAzimuthDeg, +MaxAzimuthDeg]. col < 0 or col >= cols => center.
        // The mapping uses a perceptual exponent < 1 so the inner columns
        // (C/D/E and F) crawl apart from each other while the outer columns
        // still reach their places, matching how the braille board is read.
        public static float AzimuthDeg(int col)
        {
            if (col < 0 || col >= BoardColumns) return 0.0f;
            float t = (col - (BoardColumns - 1) / 2.0f) / ((BoardColumns - 1) / 2.0f); // -1..+1
            if (Math.Abs(t) < 0.0001f) return 0.0f;
            float sign = Math.Sign(t);
            float mag = (float)Math.Pow(Math.Abs(t), 0.68);
            return MaxAzimuthDeg * sign * mag;
        }

        // Interaural time difference (Woodworth-Schlosberg): the far ear hears
        // the sound (a/c)(sin(theta) + theta) seconds later. 0 at the front,
        // ~0.58 ms at ±75°.
        public static float ItdSeconds(float azDeg)
        {
            float t = Math.Abs(azDeg) * (float)(Math.PI / 180.0);
            return (HeadRadiusM / SoundSpeedMps) * (float)(Math.Sin(t) + t);
        }

        // ITD expressed in samples at the given sample rate (fractional).
        public static float ItdSamples(float azDeg, float sampleRate)
        {
            return ItdSeconds(azDeg) * sampleRate;
        }

        // Interaural level difference in dB: how much quieter the far ear is.
        // Grows with the angle (0 at the front, ~5.3 dB at ±75°). Gentler than
        // a measured full-band ILD so the object keeps its presence.
        public static float IldDb(float azDeg)
        {
            float s = (float)Math.Sin(Math.Abs(azDeg) * (float)(Math.PI / 180.0));
            return 5.5f * (float)Math.Pow(s, 1.15f);
        }

        // Linear gain of the far ear for the ILD cue (1.0 at the front,
        // ~0.545 at ±75°). Pure level: the signal is identical, only quieter.
        // The near ear always keeps the original character.
        public static float FarEarGain(float azDeg)
        {
            return (float)Math.Pow(10.0, -IldDb(azDeg) / 20.0);
        }

        // ---- Lateral pan layer (SimplePan / non-binaural fallback) -------

        // Maps a board column (0..cols-1) to a pan value in [-MaxPan, +MaxPan].
        //  - col < 0 or col >= cols  => center (no position).
        //  - The mapping uses a perceptual exponent < 1 so the inner columns
        //    (C/D/E and F) crawl apart from each other while the outer columns
        //    still reach their places, matching how the braille board is read.
        public static float Pan(int col, int cols)
        {
            if (cols <= 1) return CenterPan;
            if (col < 0 || col >= cols) return CenterPan;

            // normalize to -1 (col 0) .. +1 (col cols-1), centering the axis.
            float t = (col - (cols - 1) / 2.0f) / ((cols - 1) / 2.0f); // -1..+1
            if (Math.Abs(t) < 0.0001f) return CenterPan;

            float sign = Math.Sign(t);
            float mag = (float)Math.Pow(Math.Abs(t), 0.68);
            return MaxPan * sign * mag;
        }

        // ---- Depth plane (rows) ------------------------------------------

        // Normalized depth of a board row: 0 = far (top of the board) and
        // 1 = near (front row). Out-of-range rows clamp to the nearest depth.
        public static float Depth(int row, int rows)
        {
            if (rows <= 1) return 1.0f;
            if (row <= 0) return 0.0f;
            if (row >= rows - 1) return 1.0f;
            return row / (float)(rows - 1);
        }

        // Volume multiplier for a depth: far rows are quieter so distance
        // reads at a glance (0.80 far .. 1.00 front).
        public static float DepthVolume(float depth)
        {
            if (depth <= 0.0f) return 0.80f;
            if (depth >= 1.0f) return 1.00f;
            return 0.80f + 0.20f * depth;
        }

        // Pan-width multiplier for a depth: far rows collapse toward the
        // stereo center like a stage receding in perspective (0.75 far ..
        // 1.00 front). Used by the SimplePan profile only.
        public static float DepthPanScale(float depth)
        {
            if (depth <= 0.0f) return 0.75f;
            if (depth >= 1.0f) return 1.00f;
            return 0.75f + 0.25f * depth;
        }

        // Row-based wrappers used by the engine; a negative row (non-positional
        // UI sound) stays neutral at the front plane.
        public static float DepthVolumeForRow(int row)
        {
            return (row < 0) ? 1.00f : DepthVolume(Depth(row, BoardRows));
        }

        public static float DepthPanScaleForRow(int row)
        {
            return (row < 0) ? 1.00f : DepthPanScale(Depth(row, BoardRows));
        }

        // Full spatial pan of a sound at (col,row): lateral curve folded with
        // the depth plane (SimplePan layer). A non-positional sound (col < 0)
        // stays centered and a negative row never narrows it.
        public static float PanAt(int col, int row, int cols)
        {
            float lateral = Pan(col, cols);
            if (row < 0 || col < 0) return lateral;
            return lateral * DepthPanScaleForRow(row);
        }

        // ---- Glide animation (swipe) -------------------------------------

        // Smoothstep easing for the swipe animation: no abrupt jump at start
        // or landing, just a clean lateral glide A->B.
        public static float EaseSweep(float t)
        {
            if (t <= 0.0f) return 0.0f;
            if (t >= 1.0f) return 1.0f;
            return t * t * (3.0f - 2.0f * t);
        }

        // Current pan of an animated swipe at normalized progress (0..1).
        public static float SweepPan(float fromPan, float toPan, float progress)
        {
            return fromPan + (toPan - fromPan) * EaseSweep(progress);
        }

        // Current azimuth of an animated swipe at normalized progress (0..1).
        public static float SweepAzimuth(float fromAz, float toAz, float progress)
        {
            return fromAz + (toAz - fromAz) * EaseSweep(progress);
        }

        // Mid-flight "pass in front" swell: the gem gains a little presence as
        // it crosses the middle of its glide (1.0 at both ends, ~1.10 at 50%).
        // The engine applies it to the volume during a sweep (Stage2D only).
        public static float SweepPassBulge(float progress)
        {
            if (progress <= 0.0f || progress >= 1.0f) return 1.0f;
            return 1.0f + 0.10f * (float)Math.Sin(Math.PI * progress);
        }

// Convenience: pan for a board column (defaults to 8 columns).
        public static float PanColumn(int col)
        {
            return Pan(col, BoardColumns);
        }

        // ---- Mundo 3D (paradigma Dolby Atmos) ----------------------------

        // El tablero se coloca en un plano vertical frente al jugador. El
        // listener (el jugador) mira hacia +Z, a 1 m de altura, en el borde
        // frontal. Con esta escala las distancias de juego quedan por debajo
        // de ~14 m, donde la absorcion de aire es transparente (el juego suena
        // nítido); la absorcion solo se oye en fuentes lejanas (> 14 m) o en la
        // calibracion "Escuela de Audio".
        public static readonly Vector3 ListenerPosition = new Vector3(0.0, 1.0, 0.0);
        public const double CellSpacingMeters = 1.0;      // separacion lateral entre columnas
        public const double FrontRowZMeters = 2.0;        // fila 7 (frente) a 2 m del listener
        public const double RowDepthMeters = 1.0;         // cada fila hacia atras suma 1 m
        public const double GemElevationMeters = 1.0;     // plano de gema = altura del oido
        public const double AerialElevationMeters = 2.5;  // zona aerea (explosiones, power-ups)

        // Radios de atenuacion por distancia: una fuente puntual (gema) decae
        // antes; una fuente volumetrica (cuerpo extenso) mantiene presencia en
        // un radio mucho mayor, por eso "suena grande".
        public const double PointMinDistance = 1.5;
        public const double PointMaxDistance = 16.0;
        public const double VolumetricMinDistance = 5.0;
        public const double VolumetricMaxDistance = 40.0;

        // Convierte una celda del tablero a su posicion mundial (metros).
        public static Vector3 WorldFromCell(int col, int row, double elevationMeters)
        {
            double x = (col - (BoardColumns - 1) / 2.0) * CellSpacingMeters;
            double z = FrontRowZMeters + (BoardRows - 1 - row) * RowDepthMeters;
            return new Vector3(x, elevationMeters, z);
        }

        // Azimut (grados) de un vector relativo al listener: 0 = frente, + = derecha.
        public static float AzimuthFromRelative(double relX, double relZ)
        {
            return (float)(Math.Atan2(relX, relZ) * 180.0 / Math.PI);
        }

        // Absorcion de aire (corte del paso-bajo) en funcion de la distancia:
        // 20 kHz por debajo de ~14 m; rolloff exponencial hasta ~1.2 kHz a 50 m;
        // sigue bajando hasta un piso de 300 Hz mas alla de 50 m.
        public static float AirAbsorptionCutoffHz(double distanceMeters)
        {
            if (distanceMeters <= 14.0) return 20000.0f;
            double hz;
            if (distanceMeters <= 50.0)
            {
                double t = (distanceMeters - 14.0) / (50.0 - 14.0);
                hz = 20000.0 * Math.Pow(1200.0 / 20000.0, t);
            }
            else
            {
                double t = Math.Min((distanceMeters - 50.0) / (100.0 - 50.0), 1.0);
                hz = 1200.0 * Math.Pow(300.0 / 1200.0, t);
            }
            return (float)Math.Max(hz, 300.0);
        }

        // Ganancia de atenuacion por distancia (0..1): 1 dentro de minDistance,
        // rolloff lineal hasta 0 en maxDistance. Las fuentes volumetricas usan
        // minDistance mayor, asi suenan "grandes" en un radio amplio.
        public static float DistanceGainFor(double distanceMeters, double minDistance, double maxDistance)
        {
            if (distanceMeters <= minDistance) return 1.0f;
            if (distanceMeters >= maxDistance) return 0.0f;
            return (float)(1.0 - (distanceMeters - minDistance) / (maxDistance - minDistance));
        }

        // Tilt de elevacion: atenuacion sutil (dB) segun la diferencia de altura
        // entre la fuente y el listener. La fuente por encima se atenúa un poco,
        // reforzando la percepcion vertical del objeto. Tope +/-4 dB.
        public static float ElevationTiltDb(double sourceY, double listenerY)
        {
            double diff = sourceY - listenerY; // + = por encima del listener
            double db = -1.2 * diff;
            if (db < -4.0) db = -4.0;
            if (db > 4.0) db = 4.0;
            return (float)db;
        }

        public static float DbToLinear(float db)
        {
            return (float)Math.Pow(10.0, db / 20.0);
        }
    }
}