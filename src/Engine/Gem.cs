using System;

namespace Bejeweled3Accessible.Engine
{
    public enum GemColor
    {
        Red,        // Ruby
        Yellow,     // Topaz
        Green,      // Emerald
        Blue,       // Sapphire
        Purple,     // Amethyst
        White,      // Diamond
        Orange      // Amber
    }

    public enum SpecialType
    {
        None,
        Flame,        // Match-4 (3x3 blast)
        Star,         // Legacy: kept for old saved boards (never created)
        Hypercube,    // Match-6 (Clears all gems of selected color)
        Supernova,    // Match-5 or L/T shape (3 rows and 3 columns blast)
        Time5,        // Lightning Mode +5s
        Time10,       // Lightning Mode +10s
        Butterfly,    // Butterfly Mode gem
        Bomb,         // Bomb hazard (countdown turns)
        PokerCard,    // Poker Mode gem card
        Dirt,         // Diamond Mine soft dirt tile
        HardRock,     // Diamond Mine hard rock tile (1 adjacent match)
        Gold,         // Alchemy quest: tile converted to gold
        GoldNugget    // Gold Rush quest: nugget hidden in the dirt
    }

    public class Gem
    {
        public GemColor Color { get; set; }
        public SpecialType Special { get; set; }
        public int BombTimer { get; set; }
        public bool IsButterfly { get; set; }
        public int RockDurability { get; set; }

        public Gem(GemColor color, SpecialType special = SpecialType.None, int bombTimer = 0, bool isButterfly = false)
        {
            Color = color;
            Special = special;
            BombTimer = bombTimer;
            IsButterfly = isButterfly;
            RockDurability = (special == SpecialType.HardRock || special == SpecialType.GoldNugget) ? 1 : ((special == SpecialType.Dirt) ? 1 : 0);
            if (Special == SpecialType.Bomb && BombTimer <= 0) BombTimer = 15;
        }

        public Gem Clone()
        {
            Gem g = new Gem(Color, Special, BombTimer, IsButterfly);
            g.RockDurability = RockDurability;
            return g;
        }

        public string GetNameLocalized()
        {
            if (Special == SpecialType.Dirt) return Localization.Get("TileDirt");
            if (Special == SpecialType.HardRock) return Localization.Get("TileHardRock", RockDurability);
            if (Special == SpecialType.GoldNugget) return Localization.Get("TileGoldNugget");
            if (Special == SpecialType.Hypercube) return Localization.Get("Hypercube");

            string shapeStr = "";
            switch (Color)
            {
                case GemColor.Red: shapeStr = Localization.Get("ShapeRuby"); break;
                case GemColor.Yellow: shapeStr = Localization.Get("ShapeTopaz"); break;
                case GemColor.Green: shapeStr = Localization.Get("ShapeEmerald"); break;
                case GemColor.Blue: shapeStr = Localization.Get("ShapeSapphire"); break;
                case GemColor.Purple: shapeStr = Localization.Get("ShapeAmethyst"); break;
                case GemColor.White: shapeStr = Localization.Get("ShapeDiamond"); break;
                case GemColor.Orange: shapeStr = Localization.Get("ShapeAmber"); break;
            }

            if (IsButterfly) return Localization.Get("ButterflyShape", shapeStr);
            if (Special == SpecialType.Supernova) return Localization.Get("SupernovaShape", shapeStr);
            if (Special == SpecialType.Flame) return Localization.Get("FlameShape", shapeStr);
            if (Special == SpecialType.Star) return Localization.Get("StarShape", shapeStr);
            if (Special == SpecialType.Gold) return Localization.Get("GoldShape", shapeStr);
            if (Special == SpecialType.Time5) return Localization.Get("Time5Shape", shapeStr);
            if (Special == SpecialType.Time10) return Localization.Get("Time10Shape", shapeStr);
            if (Special == SpecialType.Bomb) return Localization.Get("BombShape", shapeStr, BombTimer);

            return shapeStr;
        }
    }
}
