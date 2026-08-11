using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public enum QuestType
    {
        Butterflies,
        GoldRush,
        Alchemy,
        TimeBomb,
        Avalanche,
        Poker,
        IceStorm,
        DiamondMine
    }

    public class QuestMission
    {
        public QuestType Type { get; set; }
        public int Difficulty { get; set; }      // 1..5
        public int Objective { get; set; }
        public int MissionIndex { get; set; }    // 0..39
        public int RelicIndex { get; set; }      // 0..4
        public int PositionInRelic { get; set; } // 0..7

        public string GetName()
        {
            string key = "QuestMission" + Type.ToString();
            return Localization.Get(key, Difficulty, Objective);
        }
    }

    // Authentic Quest structure: 5 relicaries x 8 mini-quests (40 missions
    // total). Every relicary holds all 8 quest types once, in a rotating
    // order, and difficulty grows with the relicary (difficulty = relic + 1).
    // Revealing a relicary takes 4 of its 8 missions; restoring it takes all 8.
    public static class QuestManager
    {
        private static readonly int[] ButterfliesObjectives = { 5, 8, 10, 12, 15 };
        private static readonly int[] GoldRushObjectives = { 2, 3, 4, 5, 6 };
        private static readonly int[] AlchemyObjectives = { 10, 15, 20, 25, 30 };
        private static readonly int[] TimeBombObjectives = { 3, 4, 6, 8, 10 };
        private static readonly int[] AvalancheObjectives = { 3, 4, 5, 6, 8 };
        private static readonly int[] PokerObjectives = { 1, 2, 3, 4, 5 };
        private static readonly int[] IceStormObjectives = { 2, 4, 6, 8, 10 };
        private static readonly int[] DiamondMineObjectives = { 10, 20, 30, 40, 50 };

        private static readonly QuestMission[] _missions = new QuestMission[40];

        static QuestManager()
        {
            int index = 0;
            for (int relic = 0; relic < 5; relic++)
            {
                int difficulty = relic + 1; // relicaries 0..4 -> difficulty 1..5
                for (int pos = 0; pos < 8; pos++)
                {
                    QuestType t = (QuestType)((pos + relic) % 8);
                    int objective = GetObjective(t, difficulty);
                    _missions[index] = new QuestMission
                    {
                        Type = t,
                        Difficulty = difficulty,
                        Objective = objective,
                        MissionIndex = index,
                        RelicIndex = relic,
                        PositionInRelic = pos
                    };
                    index++;
                }
            }
        }

        private static int GetObjective(QuestType t, int difficulty)
        {
            if (difficulty < 1) difficulty = 1;
            if (difficulty > 5) difficulty = 5;
            switch (t)
            {
                case QuestType.Butterflies: return ButterfliesObjectives[difficulty - 1];
                case QuestType.GoldRush: return GoldRushObjectives[difficulty - 1];
                case QuestType.Alchemy: return AlchemyObjectives[difficulty - 1];
                case QuestType.TimeBomb: return TimeBombObjectives[difficulty - 1];
                case QuestType.Avalanche: return AvalancheObjectives[difficulty - 1];
                case QuestType.Poker: return PokerObjectives[difficulty - 1];
                case QuestType.IceStorm: return IceStormObjectives[difficulty - 1];
                default: return DiamondMineObjectives[difficulty - 1];
            }
        }

        public static QuestMission[] Missions { get { return _missions; } }

        public static QuestMission[] GetRelicMissions(int relicIndex)
        {
            List<QuestMission> list = new List<QuestMission>();
            for (int i = 0; i < 40; i++)
            {
                if (_missions[i].RelicIndex == relicIndex) list.Add(_missions[i]);
            }
            return list.ToArray();
        }
    }
}
