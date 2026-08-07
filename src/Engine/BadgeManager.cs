using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public enum BadgeTier { Locked, Bronze, Silver, Gold, Platinum }

    public class BadgeStatus
    {
        public string Key { get; set; }
        public BadgeTier Tier { get; set; }

        public BadgeStatus()
        {
            Key = "";
            Tier = BadgeTier.Locked;
        }

        public BadgeStatus(string key, BadgeTier tier)
        {
            Key = key;
            Tier = tier;
        }
    }

    public class BadgeManager
    {
        public List<BadgeStatus> Badges { get; set; }

        public BadgeManager()
        {
            Badges = new List<BadgeStatus>();
        }

        public BadgeTier GetTier(string key)
        {
            foreach (BadgeStatus b in Badges)
            {
                if (b.Key == key) return b.Tier;
            }
            return BadgeTier.Locked;
        }

        public bool SetTierIfHigher(string key, BadgeTier newTier)
        {
            for (int i = 0; i < Badges.Count; i++)
            {
                if (Badges[i].Key == key)
                {
                    if (newTier > Badges[i].Tier)
                    {
                        Badges[i].Tier = newTier;
                        return true;
                    }
                    return false;
                }
            }
            Badges.Add(new BadgeStatus(key, newTier));
            return newTier > BadgeTier.Locked;
        }

        public static string OverrideDataDirectory { get; set; }

        private static string GetFilePath(string profileName)
        {
            string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            return StoragePaths.GetPath(OverrideDataDirectory, string.Format("badges_{0}.xml", safeName));
        }

        public void Save(string profileName)
        {
            try
            {
                string path = GetFilePath(profileName);
                XmlSerializer serializer = new XmlSerializer(typeof(BadgeManager));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex) { PersistenceLog.Write(ex, "badges.xml"); }
        }

        public static BadgeManager Load(string profileName)
        {
            try
            {
                string path = GetFilePath(profileName);
                if (File.Exists(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(BadgeManager));
                    using (StreamReader reader = new StreamReader(path))
                    {
                        return (BadgeManager)serializer.Deserialize(reader);
                    }
                }
            }
            catch { }

            return new BadgeManager();
        }
    }
}
