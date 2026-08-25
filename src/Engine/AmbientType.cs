namespace Bejeweled3Accessible.Engine
{
    public enum AmbientType
    {
        None,
        Coastal,
        Crickets,
        Forest,
        OceanSurf,
        RainLeaves,
        Waterfall
    }

    public static class AmbientHelper
    {
        public static string GetAmbientName(AmbientType ambient)
        {
            switch (ambient)
            {
                case AmbientType.Coastal: return Localization.Get("AmbientCoastal");
                case AmbientType.Crickets: return Localization.Get("AmbientCrickets");
                case AmbientType.Forest: return Localization.Get("AmbientForest");
                case AmbientType.OceanSurf: return Localization.Get("AmbientOceanSurf");
                case AmbientType.RainLeaves: return Localization.Get("AmbientRainLeaves");
                case AmbientType.Waterfall: return Localization.Get("AmbientWaterfall");
                default: return Localization.Get("AmbientNone");
            }
        }

        public static string GetAmbientTrack(AmbientType ambient)
        {
            switch (ambient)
            {
                case AmbientType.Coastal: return Bejeweled3Accessible.Audio.MusicMap.FileName(Bejeweled3Accessible.Audio.MusicMap.AmbientCoastal);
                case AmbientType.Crickets: return Bejeweled3Accessible.Audio.MusicMap.FileName(Bejeweled3Accessible.Audio.MusicMap.AmbientCrickets);
                case AmbientType.Forest: return Bejeweled3Accessible.Audio.MusicMap.FileName(Bejeweled3Accessible.Audio.MusicMap.AmbientForest);
                case AmbientType.OceanSurf: return Bejeweled3Accessible.Audio.MusicMap.FileName(Bejeweled3Accessible.Audio.MusicMap.AmbientOceanSurf);
                case AmbientType.RainLeaves: return Bejeweled3Accessible.Audio.MusicMap.FileName(Bejeweled3Accessible.Audio.MusicMap.AmbientRainLeaves);
                case AmbientType.Waterfall: return Bejeweled3Accessible.Audio.MusicMap.FileName(Bejeweled3Accessible.Audio.MusicMap.AmbientWaterfall);
                default: return "";
            }
        }
    }
}
