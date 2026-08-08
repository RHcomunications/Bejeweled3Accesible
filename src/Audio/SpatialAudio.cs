using System;

namespace Bejeweled3Accessible.Audio
{
    // Selectable spatial-audio profiles. All of them share the same HRTF
    // column math below; they differ in how strongly the stage is rendered:
    //  - Stage2D: the full theatrical soundscape - row depth (volume, pitch
    //    and width), DX8 reverb on the music and the volume/frequency swell
    //    on glides.
    //  - CleanArcade (default): the original arcade character - crisp and
    //    dry. No music reverb, no depth darkening, every row at full
    //    presence; only the column pan and a pure lateral glide on swaps.
    //  - SimplePan: the bare minimum - just the left/right column pan, placed
    //    instantly (no glide animation), flat depth. Closest to a plain
    //    stereo game without any virtual stage.
    public enum SpatialProfile
    {
        Stage2D = 0,
        CleanArcade = 1,
        SimplePan = 2
    }

    // HRTF / spatial-audio mapping tailored to the 8x8 Bejeweled board.
    //
    // Design (game-first, not a generic DAFH-style curve):
    //  - Every board column A..H maps to a stereo pan position designed for an
    //    8-column board, so adjacent columns near the middle stay clearly
    //    separable by ear.
    //  - Rows add a DEPTH plane: the top of the board (row 0) is the far end
    //    of the stage and the bottom (row 7) is in front of the player. A gem
    //    in the back sounds quieter, slightly darker (lower pitch) and closer
    //    to the stereo center (narrower pan), like a stage receding in
    //    perspective; the front row sounds full, bright and wide.
    //  - Voices are ALWAYS centered: the speaker/announcer must stay in the
    //    middle, never tied to a gem column.
    //  - UI / non-positional SFX (menus, buttons, HUD) are centered too; a
    //    sound without a column must NEVER wander to one side.
    //  - Music stays centered and atmospheric (reverb handled by the engine);
    //    it never has a pan.
    //  - A gem "swipe" (swap/cascade) interpolates the pan smoothly from the
    //    source column to the destination column via EaseSweep, so movement is
    //    heard, not just the final position. The engine additionally swells
    //    the volume mid-flight (SweepPassBulge) so the gem seems to sweep past
    //    the listener, not just slide between two points.
    public static class SpatialAudio
    {
        // Softest hard cap so the extreme columns stay inside the stereo field
        // without banging the drivers.
        public const float MaxPan = 0.85f;

        // Number of columns of the board (Board.Cols mirror).
        public const int BoardColumns = 8;

        // Number of rows of the board (Board.Rows mirror).
        public const int BoardRows = 8;

        // Pan for the empty / non-positional case: dead center.
        public const float CenterPan = 0.0f;

        // The voice of the announcer is never spatialized; it is always centered.
        public const float VoicePan = 0.0f;

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
        // 1.00 front), so depth never fights with lateral separation.
        public static float DepthPanScale(float depth)
        {
            if (depth <= 0.0f) return 0.75f;
            if (depth >= 1.0f) return 1.00f;
            return 0.75f + 0.25f * depth;
        }

        // Pitch multiplier for a depth: far rows drop a touch darker, the
        // air/distance cue without any DSP (0.965 far .. 1.000 front).
        public static float DepthPitch(float depth)
        {
            if (depth <= 0.0f) return 0.965f;
            if (depth >= 1.0f) return 1.000f;
            return 0.965f + 0.035f * depth;
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

        public static float DepthPitchForRow(int row)
        {
            return (row < 0) ? 1.000f : DepthPitch(Depth(row, BoardRows));
        }

        // Full spatial pan of a sound at (col,row): lateral curve folded with
        // the depth plane. A non-positional sound (col < 0) stays centered and
        // a negative row never narrows it.
        public static float PanAt(int col, int row, int cols)
        {
            float lateral = Pan(col, cols);
            if (row < 0 || col < 0) return lateral;
            return lateral * DepthPanScaleForRow(row);
        }

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

        // Mid-flight "pass in front" swell: the gem gains a little presence as
        // it crosses the middle of its glide (1.0 at both ends, ~1.10 at 50%).
        // The engine applies it to the volume during a sweep.
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
    }
}
