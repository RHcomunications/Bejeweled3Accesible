// MusicMap.cs - Mapa canonico de las 29 pistas de musica de Bejeweled 3 (PopCap).
// Nomenclatura intacta: el valor de cada constante es el nombre EXACTO de la pista
// en music\ SIN la extension .mp3 (el helper FileName la anade). La numeracion
// estricta 01-29 elimina cualquier colision de subcadenas.
namespace Bejeweled3Accessible.Audio
{
    public static class MusicMap
    {
        public const int TrackCount = 29;

        public const string Mp3Extension = ".mp3";

        public static string FileName(string trackKey)
        {
            return trackKey + Mp3Extension;
        }

        public static readonly string[] AllTrackKeys = new string[]
        {
            // MENUS Y TEMA PRINCIPAL
            "01 - Intro",
            "02 - Bejeweled 3 Theme",
            // MODO: CLASICO
            "03 - Classic Mode - Part 1",
            "04 - Classic Mode - Part 2",
            "05 - Classic Mode - Part 3",
            "06 - Classic Mode - Part 4",
            // MODOS ESPECIALES / SECRETOS
            "07 - Lightning (aka Blitz)",
            "08 - Butterflies",
            "09 - Poker",
            "10 - Ice Storm",
            // MODO: ZEN
            "11 - Zen - Part 1",
            "12 - Zen - Part 2 - Schein Zwei",
            "13 - Zen - Part 3 - The Return",
            "14 - Zen - Part 4",
            // MODO: QUEST
            "15 - Quest Theme",
            "16 - Buried Treasure",
            "17 - Take Your Time",
            "18 - Turn by Turn",
            "19 - Time Bombs",
            "20 - Quest Finale",
            // FINALES Y BONUS TRACKS
            "21 - Gems of Glass (bonus track)",
            "22 - Final Turn",
            "23 - Bejeweled 3 Remix Medley",
            // SONIDOS AMBIENTALES (ZEN)
            "24 - Coastal",
            "25 - Crickets",
            "26 - Forest",
            "27 - Ocean Surf",
            "28 - Rain Leaves",
            "29 - Waterfall",
        };

        // Progresion dinamica de las 4 partes del modo Clasico (indice = stage - 1).
        public static readonly string[] ClassicParts = new string[]
        {
            "03 - Classic Mode - Part 1",
            "04 - Classic Mode - Part 2",
            "05 - Classic Mode - Part 3",
            "06 - Classic Mode - Part 4",
        };

        #region Menus y Tema Principal
        public const string Intro = "01 - Intro";
        public const string MainTheme = "02 - Bejeweled 3 Theme";
        #endregion

        #region Modo: Clasico
        public const string ClassicPart1 = "03 - Classic Mode - Part 1";
        public const string ClassicPart2 = "04 - Classic Mode - Part 2";
        public const string ClassicPart3 = "05 - Classic Mode - Part 3";
        public const string ClassicPart4 = "06 - Classic Mode - Part 4";
        #endregion

        #region Modos Especiales / Secretos
        public const string Lightning = "07 - Lightning (aka Blitz)";
        public const string Butterflies = "08 - Butterflies";
        public const string Poker = "09 - Poker";
        public const string IceStorm = "10 - Ice Storm";
        #endregion

        #region Modo: Zen
        public const string ZenPart1 = "11 - Zen - Part 1";
        public const string ZenPart2 = "12 - Zen - Part 2 - Schein Zwei";
        public const string ZenPart3 = "13 - Zen - Part 3 - The Return";
        public const string ZenPart4 = "14 - Zen - Part 4";
        #endregion

        #region Modo: Quest
        public const string QuestTheme = "15 - Quest Theme";
        public const string QuestBuriedTreasure = "16 - Buried Treasure";
        public const string QuestTakeYourTime = "17 - Take Your Time";
        public const string QuestTurnByTurn = "18 - Turn by Turn";
        public const string QuestTimeBombs = "19 - Time Bombs";
        public const string QuestFinale = "20 - Quest Finale";
        #endregion

        #region Finales y Bonus Tracks
        public const string GemsOfGlass = "21 - Gems of Glass (bonus track)";
        public const string FinalTurn = "22 - Final Turn";
        public const string RemixMedley = "23 - Bejeweled 3 Remix Medley";
        #endregion

        #region Sonidos Ambientales (Zen)
        public const string AmbientCoastal = "24 - Coastal";
        public const string AmbientCrickets = "25 - Crickets";
        public const string AmbientForest = "26 - Forest";
        public const string AmbientOceanSurf = "27 - Ocean Surf";
        public const string AmbientRainLeaves = "28 - Rain Leaves";
        public const string AmbientWaterfall = "29 - Waterfall";
        #endregion
    }
}
