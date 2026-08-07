using System;

namespace Bejeweled3Accessible.Engine
{
    public static class RankSystem
    {
        // Los 131 titulos oficiales del Bejeweled 3 original (Novice -> Elder Bejewelian).
        private static readonly string[] _rankTitlesEn = new string[]
        {
            "Novice", "Apprentice", "Trainee", "Beginner", "Amateur",
            "Jr. Appraiser", "Appraiser", "Gem Polisher", "Gem Scraper", "Gem Grinder",
            "Jewel Thief", "Jewel Scavenger", "Gem Scrounger", "Jr. Gemfinder", "Gemfinder",
            "Master Gemfinder", "Jr. Jewelkeep", "Jewelkeep", "Master Jewelkeeper",
            "Gemhunter Lv 1", "Gemhunter Lv 2", "Gemhunter Lv 3", "Gemhunter Lv 4", "Gemhunter Lv 5",
            "Gemcrafter Lv 1", "Gemcrafter Lv 2", "Gemcrafter Lv 3", "Gemcrafter Lv 4", "Gemcrafter Lv 5",
            "Jr. Gemstalker", "Gemstalker", "Sr. Gemstalker",
            "Topaz Hunter", "Onyx Hunter", "Amethyst Hunter", "Ruby Hunter", "Emerald Hunter", "Opal Hunter", "Sapphire Hunter", "Diamond Hunter",
            "Topaz Blaster", "Onyx Blaster", "Amethyst Blaster", "Ruby Blaster", "Emerald Blaster", "Opal Blaster", "Sapphire Blaster", "Diamond Blaster",
            "Topaz Hoarder", "Onyx Hoarder", "Amethyst Hoarder", "Ruby Hoarder", "Emerald Hoarder", "Opal Hoarder", "Sapphire Hoarder", "Diamond Hoarder",
            "Topaz Master", "Onyx Master", "Amethyst Master", "Ruby Master", "Emerald Master", "Opal Master", "Sapphire Master", "Diamond Master",
            "Lapidary Lv 1", "Lapidary Lv 2", "Lapidary Lv 3", "Lapidary Lv 4", "Lapidary Lv 5",
            "Master Lapidary", "Supreme Lapidary",
            "Ruby Wizard", "Emerald Wizard", "Opal Wizard", "Sapphire Wizard", "Diamond Wizard",
            "Jeweled Wizard", "Jeweled Mage", "Jeweled Archmage",
            "Jewelcrafter", "Jewelforger",
            "Bronze Blitzer", "Silver Blitzer", "Gold Blitzer", "Platinum Blitzer",
            "Bronze Master", "Silver Master", "Gold Master", "Platinum Master",
            "Jr. Bejeweler", "Bejeweler", "Sr. Bejeweler", "Master Bejeweler",
            "Mega Bejeweler", "Hyper Bejeweler", "Ultra Bejeweler", "Prime Bejeweler", "Ultimate Bejeweler",
            "Bejeweled Regent", "Bejeweled Demigod", "Supreme Bejeweler",
            "Jewelmagus Lv 1", "Jewelmagus Lv 2", "Jewelmagus Lv 3", "Jewelmagus Lv 4", "Jewelmagus Lv 5",
            "Jewelmagus Lv 6", "Jewelmagus Lv 7", "Jewelmagus Lv 8", "Jewelmagus Lv 9", "Elder Jewelmagus",
            "Jewelknight Lv 1", "Jewelknight Lv 2", "Jewelknight Lv 3", "Jewelknight Lv 4", "Jewelknight Lv 5",
            "Jewelknight Lv 6", "Jewelknight Lv 7", "Jewelknight Lv 8", "Jewelknight Lv 9", "Elder Jewelknight",
            "Bejewelian Lv 1", "Bejewelian Lv 2", "Bejewelian Lv 3", "Bejewelian Lv 4", "Bejewelian Lv 5",
            "Bejewelian Lv 6", "Bejewelian Lv 7", "Bejewelian Lv 8", "Bejewelian Lv 9", "Elder Bejewelian"
        };

        private static readonly string[] _rankTitlesEs = new string[]
        {
            "Novato", "Aprendiz", "En Prácticas", "Principiante", "Aficionado",
            "Tasador Junior", "Tasador", "Pulidor de Gemas", "Raspador de Gemas", "Moledor de Gemas",
            "Ladrón de Joyas", "Rebuscador de Joyas", "Chamarilero de Gemas", "Buscador de Gemas Junior", "Buscador de Gemas",
            "Maestro Buscador de Gemas", "Guardián de Joyas Junior", "Guardián de Joyas", "Maestro Guardián de Joyas",
            "Cazador de Gemas Nv 1", "Cazador de Gemas Nv 2", "Cazador de Gemas Nv 3", "Cazador de Gemas Nv 4", "Cazador de Gemas Nv 5",
            "Artífice de Gemas Nv 1", "Artífice de Gemas Nv 2", "Artífice de Gemas Nv 3", "Artífice de Gemas Nv 4", "Artífice de Gemas Nv 5",
            "Rastreador de Gemas Junior", "Rastreador de Gemas", "Rastreador de Gemas Senior",
            "Cazador de Topacio", "Cazador de Ónix", "Cazador de Amatista", "Cazador de Rubí", "Cazador de Esmeralda", "Cazador de Ópalo", "Cazador de Zafiro", "Cazador de Diamante",
            "Destructor de Topacio", "Destructor de Ónix", "Destructor de Amatista", "Destructor de Rubí", "Destructor de Esmeralda", "Destructor de Ópalo", "Destructor de Zafiro", "Destructor de Diamante",
            "Acaparador de Topacio", "Acaparador de Ónix", "Acaparador de Amatista", "Acaparador de Rubí", "Acaparador de Esmeralda", "Acaparador de Ópalo", "Acaparador de Zafiro", "Acaparador de Diamante",
            "Maestro del Topacio", "Maestro del Ónix", "Maestro de la Amatista", "Maestro del Rubí", "Maestro de la Esmeralda", "Maestro del Ópalo", "Maestro del Zafiro", "Maestro del Diamante",
            "Lapidario Nv 1", "Lapidario Nv 2", "Lapidario Nv 3", "Lapidario Nv 4", "Lapidario Nv 5",
            "Maestro Lapidario", "Lapidario Supremo",
            "Mago del Rubí", "Mago de la Esmeralda", "Mago del Ópalo", "Mago del Zafiro", "Mago del Diamante",
            "Mago de las Joyas", "Hechicero de las Joyas", "Archimago de las Joyas",
            "Artesano de Joyas", "Forjador de Joyas",
            "Relámpago de Bronce", "Relámpago de Plata", "Relámpago de Oro", "Relámpago de Platino",
            "Maestro de Bronce", "Maestro de Plata", "Maestro de Oro", "Maestro de Platino",
            "Joya Junior", "Joyero", "Joya Senior", "Maestro Joyero",
            "Mega Joyero", "Hiper Joyero", "Ultra Joyero", "Joyero Prime", "Joyero Definitivo",
            "Regente de las Joyas", "Semidiós de las Joyas", "Joyero Supremo",
            "Joyamago Nv 1", "Joyamago Nv 2", "Joyamago Nv 3", "Joyamago Nv 4", "Joyamago Nv 5",
            "Joyamago Nv 6", "Joyamago Nv 7", "Joyamago Nv 8", "Joyamago Nv 9", "Anciano Joyamago",
            "Caballero Joyero Nv 1", "Caballero Joyero Nv 2", "Caballero Joyero Nv 3", "Caballero Joyero Nv 4", "Caballero Joyero Nv 5",
            "Caballero Joyero Nv 6", "Caballero Joyero Nv 7", "Caballero Joyero Nv 8", "Caballero Joyero Nv 9", "Anciano Caballero Joyero",
            "Bejeweliano Nv 1", "Bejeweliano Nv 2", "Bejeweliano Nv 3", "Bejeweliano Nv 4", "Bejeweliano Nv 5",
            "Bejeweliano Nv 6", "Bejeweliano Nv 7", "Bejeweliano Nv 8", "Bejeweliano Nv 9", "Anciano Bejeweliano"
        };

        public static int TitleCount
        {
            get { return _rankTitlesEn.Length; }
        }

        public static string GetRankTitle(int totalScore)
        {
            int level = GetRankLevel(totalScore);
            int idx = Math.Min(level - 1, TitleCount - 1);
            string title = (Localization.CurrentLanguage == Language.Spanish)
                ? _rankTitlesEs[idx]
                : _rankTitlesEn[idx];
            return string.Format(Localization.Get("RankTitleFormat"), level, title);
        }

        public static int GetRankLevel(int totalScore)
        {
            // La progresion del original: cada rango requiere 250.000 puntos
            // adicionales de puntuacion acumulada que el anterior.
            // Umbral para mostrar el nivel n: 125.000 * n * (n-1).
            if (totalScore <= 0) return 1;
            double levelDouble = (1.0 + Math.Sqrt(1.0 + 4.0 * ((double)totalScore / 125000.0))) / 2.0;
            int level = (int)Math.Floor(levelDouble);
            if (level < 1) level = 1;
            if (level > TitleCount) level = TitleCount;
            return level;
        }
    }
}
