using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public class PlayerProfile
    {
        public string ProfileName { get; set; }
        public GameProgress Progress { get; set; }

        public PlayerProfile()
        {
            ProfileName = "Jugador 1";
            Progress = new GameProgress();
        }

        public PlayerProfile(string name)
        {
            ProfileName = name;
            Progress = new GameProgress();
        }
    }

    public class ProfileManager
    {
        public List<PlayerProfile> Profiles { get; set; }
        public int CurrentProfileIndex { get; set; }

        public ProfileManager()
        {
            Profiles = new List<PlayerProfile>();
            CurrentProfileIndex = 0;
        }

        public PlayerProfile CurrentProfile
        {
            get
            {
                if (Profiles.Count == 0)
                {
                    return null;
                }
                if (CurrentProfileIndex < 0 || CurrentProfileIndex >= Profiles.Count) CurrentProfileIndex = 0;
                return Profiles[CurrentProfileIndex];
            }
        }

        public static string OverrideDataDirectory { get; set; }

        private static string GetFilePath()
        {
            return StoragePaths.GetPath(OverrideDataDirectory, "profiles.xml");
        }

        public void Save()
        {
            try
            {
                string path = GetFilePath();
                XmlSerializer serializer = new XmlSerializer(typeof(ProfileManager));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex) { PersistenceLog.Write(ex, "profiles.xml"); }
        }

        public static ProfileManager Load()
        {
            try
            {
                string path = GetFilePath();
                if (File.Exists(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ProfileManager));
                    using (StreamReader reader = new StreamReader(path))
                    {
                        return (ProfileManager)serializer.Deserialize(reader);
                    }
                }
            }
            catch { }

            return new ProfileManager();
        }
    }
}
