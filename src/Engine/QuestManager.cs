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
        public int RelicIndex { get; set; }      // 0..9
        public int PositionInRelic { get; set; } // 0..3

        public string GetName()
        {
            string key = "QuestMission" + Type.ToString();
            return Localization.Get(key, Difficulty, Objective);
        }
    }

    // Authentic Quest structure: 10 relics x 4 missions, 8 mission types
    // with 5 levels of difficulty (40 missions total).
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

        private static readonly QuestType[] _evenRelicTypes = { QuestType.Butterflies, QuestType.GoldRush, QuestType.Alchemy, QuestType.TimeBomb };
        private static readonly QuestType[] _oddRelicTypes = { QuestType.Avalanche, QuestType.Poker, QuestType.IceStorm, QuestType.DiamondMine };

        private static readonly QuestMission[] _missions = new QuestMission[40];

        static QuestManager()
        {
            int index = 0;
            for (int relic = 0; relic < 10; relic++)
            {
                int difficulty = (relic / 2) + 1; // relics 0-1 -> 1, 2-3 -> 2 ... 8-9 -> 5
                QuestType[] types = (relic % 2 == 0) ? _evenRelicTypes : _oddRelicTypes;
                for (int pos = 0; pos < 4; pos++)
                {
                    QuestType t = types[pos];
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
