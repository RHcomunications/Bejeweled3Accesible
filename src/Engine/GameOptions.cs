using System;
using System.IO;
using System.Xml.Serialization;
using Bejeweled3Accessible.Audio;

namespace Bejeweled3Accessible.Engine
{
    public class GameOptions
    {
        public int MusicVolume { get; set; }
        public int SoundVolume { get; set; }
        public int VoiceVolume { get; set; }
        public Language SelectedLanguage { get; set; }
        public int ZenAmbient { get; set; }
        public bool ZenMantras { get; set; }
        public bool ZenBreath { get; set; }

        // Nullable so an options.xml written before these settings existed
        // deserializes to null and falls back to the current defaults
        // (CleanArcade profile, binaural on) instead of the Stage2D/off
        // implicit zero-values.
        public int? SpatialProfile { get; set; }
        public bool? SpatialBinauralEnabled { get; set; }

        public GameOptions()
        {
            MusicVolume = 80;
            SoundVolume = 100;
            VoiceVolume = 100;
            SelectedLanguage = Language.Spanish;
            ZenAmbient = (int)AmbientType.None;
            ZenMantras = true;
            ZenBreath = true;
            SpatialProfile = (int)Audio.SpatialProfile.CleanArcade;
            SpatialBinauralEnabled = true;
        }

        public int EffectiveSpatialProfile
        {
            get { return SpatialProfile ?? (int)Audio.SpatialProfile.CleanArcade; }
        }

        public bool EffectiveSpatialBinauralEnabled
        {
            get { return SpatialBinauralEnabled ?? true; }
        }

        public static string OverrideDataDirectory { get; set; }

        private static string GetFilePath()
        {
            return StoragePaths.GetPath(OverrideDataDirectory, "options.xml");
        }

        public void Save()
        {
            try
            {
                string path = GetFilePath();
                XmlSerializer serializer = new XmlSerializer(typeof(GameOptions));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex) { PersistenceLog.Write(ex, "options.xml"); }
        }

        public static GameOptions Load()
        {
            try
            {
                string path = GetFilePath();
                if (File.Exists(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(GameOptions));
                    using (StreamReader reader = new StreamReader(path))
                    {
                        return (GameOptions)serializer.Deserialize(reader);
                    }
                }
            }
            catch { }

            return new GameOptions();
        }
    }
}
