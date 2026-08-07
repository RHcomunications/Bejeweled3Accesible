using System;
using System.IO;
using System.Xml.Serialization;

namespace Bejeweled3Accessible.Engine
{
    public class GameProgress
    {
        public int ClassicLevel { get; set; }
        public int ZenLevel { get; set; }
        public int LightningHighScore { get; set; }
        public int PokerHighScore { get; set; }
        public int ButterfliesHighScore { get; set; }
        public int IceStormHighScore { get; set; }
        public int DiamondMineHighScore { get; set; }
        public int QuestRelic1Completed { get; set; }
        public int TotalScore { get; set; }
        public int TotalGemsCleared { get; set; }
        public int TotalFlameGemsDestroyed { get; set; }
        public int TotalStarGemsDestroyed { get; set; }
        public int TotalHypercubesDestroyed { get; set; }
        public bool[] QuestMissions { get; set; }

        public bool IsPokerUnlocked { get { return ClassicLevel >= 5; } }
        public bool IsButterfliesUnlocked { get { return ZenLevel >= 5; } }
        public bool IsIceStormUnlocked { get { return LightningHighScore >= 100000; } }
        public bool IsDiamondMineUnlocked { get { return QuestRelic1Completed >= 4; } }

        public static string OverrideDataDirectory { get; set; }

        public GameProgress()
        {
            ClassicLevel = 1;
            ZenLevel = 1;
            LightningHighScore = 0;
            PokerHighScore = 0;
            ButterfliesHighScore = 0;
            IceStormHighScore = 0;
            DiamondMineHighScore = 0;
            QuestRelic1Completed = 0;
            TotalScore = 0;
            TotalGemsCleared = 0;
            TotalFlameGemsDestroyed = 0;
            TotalStarGemsDestroyed = 0;
            TotalHypercubesDestroyed = 0;
            QuestMissions = new bool[40];
        }

        public bool IsQuestMissionComplete(int missionIndex)
        {
            if (missionIndex < 0 || missionIndex >= 40) return false;
            if (QuestMissions == null) QuestMissions = new bool[40];
            return QuestMissions[missionIndex];
        }

        public void CompleteQuestMission(int missionIndex)
        {
            if (missionIndex < 0 || missionIndex >= 40) return;
            if (QuestMissions == null) QuestMissions = new bool[40];
            if (!QuestMissions[missionIndex])
            {
                QuestMissions[missionIndex] = true;
                Save();
            }
        }

        public int CountCompletedInRelic(int relicIndex)
        {
            int count = 0;
            QuestMission[] missions = QuestManager.GetRelicMissions(relicIndex);
            foreach (var m in missions)
            {
                if (IsQuestMissionComplete(m.MissionIndex)) count++;
            }
            return count;
        }

        private static string GetFilePath()
        {
            return StoragePaths.GetPath(OverrideDataDirectory, "progress.xml");
        }

        public void Save()
        {
            try
            {
                string path = GetFilePath();
                XmlSerializer serializer = new XmlSerializer(typeof(GameProgress));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving progress: " + ex.Message);
                PersistenceLog.Write(ex, "progress.xml");
            }
        }

        public static GameProgress Load()
        {
            try
            {
                string path = GetFilePath();
                if (File.Exists(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(GameProgress));
                    using (StreamReader reader = new StreamReader(path))
                    {
                        return (GameProgress)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading progress: " + ex.Message);
            }

            return new GameProgress();
        }
    }
}
