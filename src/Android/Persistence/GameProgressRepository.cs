using System;
using Android.Content;
using Bejeweled3Accessible.Engine;

namespace Bejeweled3Accessible.AndroidApp.Persistence
{
    public class GameProgressRepository
    {
        private const string PrefsName = "bejeweled3_progress_prefs";
        private readonly ISharedPreferences _prefs;
        private readonly ProfileManager _profileMgr;

        public GameProgressRepository(Context context)
        {
            _prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            _profileMgr = ProfileManager.Load();
            SyncFromProfile();
        }

        private GameProgress CurrentProgress => _profileMgr.CurrentProfile != null ? _profileMgr.CurrentProfile.Progress : new GameProgress();

        public int ClassicLevel
        {
            get => _prefs.GetInt("classic_level", CurrentProgress.ClassicLevel);
            set
            {
                _prefs.Edit().PutInt("classic_level", value).Apply();
                CurrentProgress.ClassicLevel = value;
                _profileMgr.Save();
            }
        }

        public int ZenLevel
        {
            get => _prefs.GetInt("zen_level", CurrentProgress.ZenLevel);
            set
            {
                _prefs.Edit().PutInt("zen_level", value).Apply();
                CurrentProgress.ZenLevel = value;
                _profileMgr.Save();
            }
        }

        public int LightningHighScore
        {
            get => _prefs.GetInt("lightning_high_score", CurrentProgress.LightningHighScore);
            set
            {
                _prefs.Edit().PutInt("lightning_high_score", value).Apply();
                CurrentProgress.LightningHighScore = value;
                _profileMgr.Save();
            }
        }

        public int PokerHighScore
        {
            get => _prefs.GetInt("poker_high_score", CurrentProgress.PokerHighScore);
            set
            {
                _prefs.Edit().PutInt("poker_high_score", value).Apply();
                CurrentProgress.PokerHighScore = value;
                _profileMgr.Save();
            }
        }

        public int ButterfliesHighScore
        {
            get => _prefs.GetInt("butterflies_high_score", CurrentProgress.ButterfliesHighScore);
            set
            {
                _prefs.Edit().PutInt("butterflies_high_score", value).Apply();
                CurrentProgress.ButterfliesHighScore = value;
                _profileMgr.Save();
            }
        }

        public int IceStormHighScore
        {
            get => _prefs.GetInt("ice_storm_high_score", CurrentProgress.IceStormHighScore);
            set
            {
                _prefs.Edit().PutInt("ice_storm_high_score", value).Apply();
                CurrentProgress.IceStormHighScore = value;
                _profileMgr.Save();
            }
        }

        public int DiamondMineHighScore
        {
            get => _prefs.GetInt("diamond_mine_high_score", CurrentProgress.DiamondMineHighScore);
            set
            {
                _prefs.Edit().PutInt("diamond_mine_high_score", value).Apply();
                CurrentProgress.DiamondMineHighScore = value;
                _profileMgr.Save();
            }
        }

        public int QuestRelicCount
        {
            get => _prefs.GetInt("quest_relic_count", CurrentProgress.QuestRelicCount);
            set
            {
                _prefs.Edit().PutInt("quest_relic_count", value).Apply();
                CurrentProgress.QuestRelicCount = value;
                _profileMgr.Save();
            }
        }

        public int TotalGemsCleared
        {
            get => _prefs.GetInt("total_gems_cleared", CurrentProgress.TotalGemsCleared);
            set
            {
                _prefs.Edit().PutInt("total_gems_cleared", value).Apply();
                CurrentProgress.TotalGemsCleared = value;
                _profileMgr.Save();
            }
        }

        public int TotalScore
        {
            get => _prefs.GetInt("total_score", CurrentProgress.TotalScore);
            set
            {
                _prefs.Edit().PutInt("total_score", value).Apply();
                CurrentProgress.TotalScore = value;
                _profileMgr.Save();
            }
        }

        // Reglas de desbloqueo canónicas del juego original Bejeweled 3
        public bool IsPokerUnlocked => ClassicLevel >= 5;
        public bool IsButterfliesUnlocked => ZenLevel >= 5;
        public bool IsIceStormUnlocked => LightningHighScore >= 100000;
        public bool IsDiamondMineUnlocked => QuestRelicCount >= 1;

        public bool IsModeUnlocked(string modeKey)
        {
            switch (modeKey)
            {
                case "ModeClassic":
                case "ModeLightning":
                case "ModeZen":
                case "ModeQuest":
                    return true;
                case "ModePoker":
                    return IsPokerUnlocked;
                case "ModeButterflies":
                    return IsButterfliesUnlocked;
                case "ModeIceStorm":
                    return IsIceStormUnlocked;
                case "ModeDiamondMine":
                    return IsDiamondMineUnlocked;
                default:
                    return !modeKey.EndsWith("Locked");
            }
        }

        public string GetLockedKeyForMode(string modeKey)
        {
            if (IsModeUnlocked(modeKey)) return modeKey;
            return modeKey + "Locked";
        }

        public void SyncFromProfile()
        {
            var p = CurrentProgress;
            var editor = _prefs.Edit();
            editor.PutInt("classic_level", p.ClassicLevel);
            editor.PutInt("zen_level", p.ZenLevel);
            editor.PutInt("lightning_high_score", p.LightningHighScore);
            editor.PutInt("poker_high_score", p.PokerHighScore);
            editor.PutInt("butterflies_high_score", p.ButterfliesHighScore);
            editor.PutInt("ice_storm_high_score", p.IceStormHighScore);
            editor.PutInt("diamond_mine_high_score", p.DiamondMineHighScore);
            editor.PutInt("quest_relic_count", p.QuestRelicCount);
            editor.PutInt("total_gems_cleared", p.TotalGemsCleared);
            editor.PutInt("total_score", p.TotalScore);
            editor.Apply();
        }

        public void SaveProgress(GameProgress progress)
        {
            if (progress == null) return;
            var editor = _prefs.Edit();
            editor.PutInt("classic_level", progress.ClassicLevel);
            editor.PutInt("zen_level", progress.ZenLevel);
            editor.PutInt("lightning_high_score", progress.LightningHighScore);
            editor.PutInt("poker_high_score", progress.PokerHighScore);
            editor.PutInt("butterflies_high_score", progress.ButterfliesHighScore);
            editor.PutInt("ice_storm_high_score", progress.IceStormHighScore);
            editor.PutInt("diamond_mine_high_score", progress.DiamondMineHighScore);
            editor.PutInt("quest_relic_count", progress.QuestRelicCount);
            editor.PutInt("total_gems_cleared", progress.TotalGemsCleared);
            editor.PutInt("total_score", progress.TotalScore);
            editor.Apply();

            _profileMgr.Save();
        }
    }
}
