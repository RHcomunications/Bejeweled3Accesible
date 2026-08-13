// AudioMap.cs - Mapa canonico de los 189 sonidos de Bejeweled 3 (PopCap).
// Nomenclatura intacta: el valor de cada constante es el nombre EXACTO del fichero
// en sounds\ sin extension .ogg, respetando mayusculas originales (ej. Diamond_Mine_Death).
// Generado desde el listado real del disco; ver test AudioMap: cobertura completa en TestRunner.cs.
namespace Bejeweled3Accessible.Audio
{
    public static class AudioMap
    {
        public const int SoundCount = 189;

        // Prefijos para series numeradas: usar la concatenacion completa de la clave
        // (ej. AudioMap.SpeedMatchPrefix + nivel) para no confundir el fallback del motor.
        public const string ComboPrefix = "combo_";
        public const string SpeedMatchPrefix = "speedmatch";
        public const string HyperspaceGemLandPrefix = "hyperspace_gem_land_";
        public const string HyperspaceGemLandZenPrefix = "hyperspace_gem_land_zen_";
        public const string HyperspaceShatterPrefix = "hyperspace_shatter_";
        public const string MultiplierUp2Prefix = "multiplier_up2_";

        public static readonly string[] AllSoundKeys = new string[]
        {
            // INTERFAZ Y TABLERO BASE
            "alchemy_convert",
            "background_change",
            "backtomain",
            "badgeawarded",
            "badgefall",
            "badmove",
            "button_mouseleave",
            "button_mouseover",
            "button_press",
            "button_release",
            "clickflyin",
            "combo_1",
            "combo_2",
            "combo_3",
            "combo_4",
            "combo_5",
            "combo_6",
            "combo_7",
            "countdown_warning",
            "doubleset",
            "earthquake",
            "gem_hit",
            "gem_shatters",
            "menuspin",
            "pulleys",
            "rank_countup",
            "rankup",
            "replay_popup",
            "select",
            "sin500",
            "small_explode",
            "start_rotate",
            "tick",
            "tooltip",
            // VOCES DEL NARRADOR
            "voice_awesome",
            "voice_blazingspeed",
            "voice_challengecomplete",
            "voice_excellent",
            "voice_extraordinary",
            "voice_gameover",
            "voice_getready",
            "voice_go",
            "voice_good",
            "voice_goodbye",
            "voice_levelcomplete",
            "voice_nomoremoves",
            "voice_spectacular",
            "voice_thirtyseconds",
            "voice_timeup",
            "voice_unbelievable",
            "voice_welcomeback",
            "voice_welcometobejeweled",
            // MODO: DIAMOND MINE
            "diamond_mine_artifact_showcase",
            "diamond_mine_bigstone_cracked",
            "Diamond_Mine_Death",
            "diamond_mine_dig",
            "diamond_mine_dig_line_hit",
            "diamond_mine_dig_line_hit_mega",
            "diamond_mine_dig_notify",
            "diamond_mine_dirt_cracked",
            "diamond_mine_stone_cracked",
            "diamond_mine_treasurefind",
            "diamond_mine_treasurefind_diamonds",
            // MODO: ICE STORM
            "cold wind",
            "ice_column_appears",
            "ice_column_break",
            "Ice_Storm_ColumnCombo",
            "Ice_Storm_ColumnCombo_Mega",
            "Ice_Storm_Final_Thud",
            "Ice_Storm_GameOver",
            "Ice_Storm_Multipler_Up",
            "Ice_Storm_Steam_Build_Up",
            "Ice_Storm_Steam_Valve",
            "Ice_Storm_Wind",
            "ice_warning",
            // MODO: POKER
            "carddeal",
            "cardflip",
            "poker_4ofakind",
            "poker_flush",
            "poker_fullhouse",
            "pokerchips",
            "pokerscore",
            "skull_appear",
            "skull_busted",
            "skull_buster",
            "skullcoin_flip",
            "skullcoinlands",
            "skullcoinlose",
            "skullcoinwin",
            // MODO: ZEN
            "breath_in",
            "breath_out",
            "zen_checkoff",
            "zen_checkon",
            "zen_combo_2",
            "zen_dropdownbutton",
            "zen_mantra1",
            "zen_menuclose",
            "zen_menuexpand",
            "zen_menuopen",
            "zen_menushrink",
            "zen_necklace_1",
            "zen_necklace_2",
            "zen_necklace_3",
            "zen_necklace_4",
            // MODO: BUTTERFLIES
            "butterfly_appear",
            "butterfly_death1",
            "butterflyescape",
            // MODO: LIGHTNING (RELAMPAGO)
            "bomb_appears",
            "bomb_explode",
            "electro_explode",
            "electro_path",
            "electro_path2",
            "firework_crackle",
            "firework_launch",
            "firework_thump",
            "gem_countdown_destroyed",
            "lightning_energize",
            "lightning_humloop",
            "lightning_tube_fill_10",
            "lightning_tube_fill_5",
            "preblast",
            "speedmatch1",
            "speedmatch2",
            "speedmatch3",
            "speedmatch4",
            "speedmatch5",
            "speedmatch6",
            "speedmatch7",
            "speedmatch8",
            "speedmatch9",
            "timebombexplode",
            "tower_hits_top1",
            // QUEST Y MODOS SECRETOS
            "quest_award_wreath",
            "quest_get",
            "quest_menu_button_mouseover1",
            "quest_menu_button1",
            "quest_notify",
            "quest_orb1",
            "quest_orb3",
            "Quest_Sandstorm_cover",
            "Quest_Sandstorm_reveal",
            "QuestMenu_RelicComplete_object",
            "QuestMenu_RelicComplete_rumble",
            "QuestMenu_RelicRevealed_object",
            "QuestMenu_RelicRevealed_rumble",
            "rewind",
            "sandstorm_treasure_reveal",
            "scramble",
            "secretmouseover1",
            "secretmouseover2",
            "secretmouseover3",
            "secretmouseover4",
            "secretunlocked",
            // GEMAS ESPECIALES Y POWER-UPS
            "coin_created",
            "coinappear",
            "flamebonus",
            "flameloop",
            "flamespeed1",
            "hypercube_create",
            "hyperspace",
            "hyperspace_gem_land_1",
            "hyperspace_gem_land_2",
            "hyperspace_gem_land_3",
            "hyperspace_gem_land_4",
            "hyperspace_gem_land_5",
            "hyperspace_gem_land_6",
            "hyperspace_gem_land_7",
            "hyperspace_gem_land_zen_1",
            "hyperspace_gem_land_zen_2",
            "hyperspace_gem_land_zen_3",
            "hyperspace_gem_land_zen_4",
            "hyperspace_gem_land_zen_5",
            "hyperspace_gem_land_zen_6",
            "hyperspace_gem_land_zen_7",
            "hyperspace_shatter_1",
            "hyperspace_shatter_2",
            "hyperspace_shatter_zen",
            "lasergem_created",
            "powergem_created",
            // BONIFICACIONES Y MULTIPLICADORES
            "multiplier_appears",
            "multiplier_hurrahed",
            "multiplier_up2_1",
            "multiplier_up2_2",
            "multiplier_up2_3",
            "multiplier_up2_4",
            "timebonus_10",
            "timebonus_5",
            "timebonus_appears_10",
            "timebonus_appears_5",
        };

        #region Interfaz y Tablero Base
        public const string AlchemyConvert = "alchemy_convert";
        public const string BackgroundChange = "background_change";
        public const string Backtomain = "backtomain";
        public const string Badgeawarded = "badgeawarded";
        public const string Badgefall = "badgefall";
        public const string Badmove = "badmove";
        public const string ButtonMouseleave = "button_mouseleave";
        public const string ButtonMouseover = "button_mouseover";
        public const string ButtonPress = "button_press";
        public const string ButtonRelease = "button_release";
        public const string Clickflyin = "clickflyin";
        public const string Combo1 = "combo_1";
        public const string Combo2 = "combo_2";
        public const string Combo3 = "combo_3";
        public const string Combo4 = "combo_4";
        public const string Combo5 = "combo_5";
        public const string Combo6 = "combo_6";
        public const string Combo7 = "combo_7";
        public const string CountdownWarning = "countdown_warning";
        public const string Doubleset = "doubleset";
        public const string Earthquake = "earthquake";
        public const string GemHit = "gem_hit";
        public const string GemShatters = "gem_shatters";
        public const string Menuspin = "menuspin";
        public const string Pulleys = "pulleys";
        public const string RankCountup = "rank_countup";
        public const string Rankup = "rankup";
        public const string ReplayPopup = "replay_popup";
        public const string Select = "select";
        public const string Sin500 = "sin500";
        public const string SmallExplode = "small_explode";
        public const string StartRotate = "start_rotate";
        public const string Tick = "tick";
        public const string Tooltip = "tooltip";
        #endregion

        #region Voces del Narrador
        public const string VoiceAwesome = "voice_awesome";
        public const string VoiceBlazingspeed = "voice_blazingspeed";
        public const string VoiceChallengecomplete = "voice_challengecomplete";
        public const string VoiceExcellent = "voice_excellent";
        public const string VoiceExtraordinary = "voice_extraordinary";
        public const string VoiceGameover = "voice_gameover";
        public const string VoiceGetready = "voice_getready";
        public const string VoiceGo = "voice_go";
        public const string VoiceGood = "voice_good";
        public const string VoiceGoodbye = "voice_goodbye";
        public const string VoiceLevelcomplete = "voice_levelcomplete";
        public const string VoiceNomoremoves = "voice_nomoremoves";
        public const string VoiceSpectacular = "voice_spectacular";
        public const string VoiceThirtyseconds = "voice_thirtyseconds";
        public const string VoiceTimeup = "voice_timeup";
        public const string VoiceUnbelievable = "voice_unbelievable";
        public const string VoiceWelcomeback = "voice_welcomeback";
        public const string VoiceWelcometobejeweled = "voice_welcometobejeweled";
        #endregion

        #region Modo: Diamond Mine
        public const string DiamondMineArtifactShowcase = "diamond_mine_artifact_showcase";
        public const string DiamondMineBigstoneCracked = "diamond_mine_bigstone_cracked";
        public const string DiamondMineDeath = "Diamond_Mine_Death";
        public const string DiamondMineDig = "diamond_mine_dig";
        public const string DiamondMineDigLineHit = "diamond_mine_dig_line_hit";
        public const string DiamondMineDigLineHitMega = "diamond_mine_dig_line_hit_mega";
        public const string DiamondMineDigNotify = "diamond_mine_dig_notify";
        public const string DiamondMineDirtCracked = "diamond_mine_dirt_cracked";
        public const string DiamondMineStoneCracked = "diamond_mine_stone_cracked";
        public const string DiamondMineTreasurefind = "diamond_mine_treasurefind";
        public const string DiamondMineTreasurefindDiamonds = "diamond_mine_treasurefind_diamonds";
        #endregion

        #region Modo: Ice Storm
        public const string ColdWind = "cold wind";
        public const string IceColumnAppears = "ice_column_appears";
        public const string IceColumnBreak = "ice_column_break";
        public const string IceStormColumnCombo = "Ice_Storm_ColumnCombo";
        public const string IceStormColumnComboMega = "Ice_Storm_ColumnCombo_Mega";
        public const string IceStormFinalThud = "Ice_Storm_Final_Thud";
        public const string IceStormGameOver = "Ice_Storm_GameOver";
        public const string IceStormMultiplerUp = "Ice_Storm_Multipler_Up";
        public const string IceStormSteamBuildUp = "Ice_Storm_Steam_Build_Up";
        public const string IceStormSteamValve = "Ice_Storm_Steam_Valve";
        public const string IceStormWind = "Ice_Storm_Wind";
        public const string IceWarning = "ice_warning";
        #endregion

        #region Modo: Poker
        public const string Carddeal = "carddeal";
        public const string Cardflip = "cardflip";
        public const string Poker4ofakind = "poker_4ofakind";
        public const string PokerFlush = "poker_flush";
        public const string PokerFullhouse = "poker_fullhouse";
        public const string Pokerchips = "pokerchips";
        public const string Pokerscore = "pokerscore";
        public const string SkullAppear = "skull_appear";
        public const string SkullBusted = "skull_busted";
        public const string SkullBuster = "skull_buster";
        public const string SkullcoinFlip = "skullcoin_flip";
        public const string Skullcoinlands = "skullcoinlands";
        public const string Skullcoinlose = "skullcoinlose";
        public const string Skullcoinwin = "skullcoinwin";
        #endregion

        #region Modo: Zen
        public const string BreathIn = "breath_in";
        public const string BreathOut = "breath_out";
        public const string ZenCheckoff = "zen_checkoff";
        public const string ZenCheckon = "zen_checkon";
        public const string ZenCombo2 = "zen_combo_2";
        public const string ZenDropdownbutton = "zen_dropdownbutton";
        public const string ZenMantra1 = "zen_mantra1";
        public const string ZenMenuclose = "zen_menuclose";
        public const string ZenMenuexpand = "zen_menuexpand";
        public const string ZenMenuopen = "zen_menuopen";
        public const string ZenMenushrink = "zen_menushrink";
        public const string ZenNecklace1 = "zen_necklace_1";
        public const string ZenNecklace2 = "zen_necklace_2";
        public const string ZenNecklace3 = "zen_necklace_3";
        public const string ZenNecklace4 = "zen_necklace_4";
        #endregion

        #region Modo: Butterflies
        public const string ButterflyAppear = "butterfly_appear";
        public const string ButterflyDeath1 = "butterfly_death1";
        public const string Butterflyescape = "butterflyescape";
        #endregion

        #region Modo: Lightning (Relampago)
        public const string BombAppears = "bomb_appears";
        public const string BombExplode = "bomb_explode";
        public const string ElectroExplode = "electro_explode";
        public const string ElectroPath = "electro_path";
        public const string ElectroPath2 = "electro_path2";
        public const string FireworkCrackle = "firework_crackle";
        public const string FireworkLaunch = "firework_launch";
        public const string FireworkThump = "firework_thump";
        public const string GemCountdownDestroyed = "gem_countdown_destroyed";
        public const string LightningEnergize = "lightning_energize";
        public const string LightningHumloop = "lightning_humloop";
        public const string LightningTubeFill10 = "lightning_tube_fill_10";
        public const string LightningTubeFill5 = "lightning_tube_fill_5";
        public const string Preblast = "preblast";
        public const string Speedmatch1 = "speedmatch1";
        public const string Speedmatch2 = "speedmatch2";
        public const string Speedmatch3 = "speedmatch3";
        public const string Speedmatch4 = "speedmatch4";
        public const string Speedmatch5 = "speedmatch5";
        public const string Speedmatch6 = "speedmatch6";
        public const string Speedmatch7 = "speedmatch7";
        public const string Speedmatch8 = "speedmatch8";
        public const string Speedmatch9 = "speedmatch9";
        public const string Timebombexplode = "timebombexplode";
        public const string TowerHitsTop1 = "tower_hits_top1";
        #endregion

        #region Quest y Modos Secretos
        public const string QuestAwardWreath = "quest_award_wreath";
        public const string QuestGet = "quest_get";
        public const string QuestMenuButtonMouseover1 = "quest_menu_button_mouseover1";
        public const string QuestMenuButton1 = "quest_menu_button1";
        public const string QuestNotify = "quest_notify";
        public const string QuestOrb1 = "quest_orb1";
        public const string QuestOrb3 = "quest_orb3";
        public const string QuestSandstormCover = "Quest_Sandstorm_cover";
        public const string QuestSandstormReveal = "Quest_Sandstorm_reveal";
        public const string QuestMenuRelicCompleteObject = "QuestMenu_RelicComplete_object";
        public const string QuestMenuRelicCompleteRumble = "QuestMenu_RelicComplete_rumble";
        public const string QuestMenuRelicRevealedObject = "QuestMenu_RelicRevealed_object";
        public const string QuestMenuRelicRevealedRumble = "QuestMenu_RelicRevealed_rumble";
        public const string Rewind = "rewind";
        public const string SandstormTreasureReveal = "sandstorm_treasure_reveal";
        public const string Scramble = "scramble";
        public const string Secretmouseover1 = "secretmouseover1";
        public const string Secretmouseover2 = "secretmouseover2";
        public const string Secretmouseover3 = "secretmouseover3";
        public const string Secretmouseover4 = "secretmouseover4";
        public const string Secretunlocked = "secretunlocked";
        #endregion

        #region Gemas Especiales y Power-Ups
        public const string CoinCreated = "coin_created";
        public const string Coinappear = "coinappear";
        public const string Flamebonus = "flamebonus";
        public const string Flameloop = "flameloop";
        public const string Flamespeed1 = "flamespeed1";
        public const string HypercubeCreate = "hypercube_create";
        public const string Hyperspace = "hyperspace";
        public const string HyperspaceGemLand1 = "hyperspace_gem_land_1";
        public const string HyperspaceGemLand2 = "hyperspace_gem_land_2";
        public const string HyperspaceGemLand3 = "hyperspace_gem_land_3";
        public const string HyperspaceGemLand4 = "hyperspace_gem_land_4";
        public const string HyperspaceGemLand5 = "hyperspace_gem_land_5";
        public const string HyperspaceGemLand6 = "hyperspace_gem_land_6";
        public const string HyperspaceGemLand7 = "hyperspace_gem_land_7";
        public const string HyperspaceGemLandZen1 = "hyperspace_gem_land_zen_1";
        public const string HyperspaceGemLandZen2 = "hyperspace_gem_land_zen_2";
        public const string HyperspaceGemLandZen3 = "hyperspace_gem_land_zen_3";
        public const string HyperspaceGemLandZen4 = "hyperspace_gem_land_zen_4";
        public const string HyperspaceGemLandZen5 = "hyperspace_gem_land_zen_5";
        public const string HyperspaceGemLandZen6 = "hyperspace_gem_land_zen_6";
        public const string HyperspaceGemLandZen7 = "hyperspace_gem_land_zen_7";
        public const string HyperspaceShatter1 = "hyperspace_shatter_1";
        public const string HyperspaceShatter2 = "hyperspace_shatter_2";
        public const string HyperspaceShatterZen = "hyperspace_shatter_zen";
        public const string LasergemCreated = "lasergem_created";
        public const string PowergemCreated = "powergem_created";
        #endregion

        #region Bonificaciones y Multiplicadores
        public const string MultiplierAppears = "multiplier_appears";
        public const string MultiplierHurrahed = "multiplier_hurrahed";
        public const string MultiplierUp21 = "multiplier_up2_1";
        public const string MultiplierUp22 = "multiplier_up2_2";
        public const string MultiplierUp23 = "multiplier_up2_3";
        public const string MultiplierUp24 = "multiplier_up2_4";
        public const string Timebonus10 = "timebonus_10";
        public const string Timebonus5 = "timebonus_5";
        public const string TimebonusAppears10 = "timebonus_appears_10";
        public const string TimebonusAppears5 = "timebonus_appears_5";
        #endregion

    }
}
