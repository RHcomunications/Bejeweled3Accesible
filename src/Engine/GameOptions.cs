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
        public bool BinauralEnabled { get; set; }
        public bool MouseEnabled { get; set; }

        public GameOptions()
        {
            MusicVolume = 80;
            SoundVolume = 100;
            VoiceVolume = 100;
            SelectedLanguage = Language.Spanish;
            ZenAmbient = (int)AmbientType.None;
            ZenMantras = true;
            ZenBreath = true;
            BinauralEnabled = true;
            MouseEnabled = true;
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
