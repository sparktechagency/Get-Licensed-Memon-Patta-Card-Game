using System;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Plain data shapes serialized to/from PlayFab UserData JSON blobs and statistics.
    /// Keep these in sync with whatever CloudScript functions end up validating them server-side.
    /// </summary>
    [Serializable]
    public class PlayerProfileData
    {
        public string DisplayName;
        public int Level = 1;
        public int CurrentXP;
        public int XPToNextLevel = 1000;
        public string AvatarId = "default";
        public string Country = "";
    }

    [Serializable]
    public class DailyChallengeData
    {
        public string ChallengeId;
        public string Description;
        public int Progress;
        public int Target;
        public int XPReward;
        public bool Claimed;
        public string DateIssuedUtc;
    }

    [Serializable]
    public class WeeklyChallengeData
    {
        public string ChallengeId;
        public string Description;
        public int Progress;
        public int Target;
        public int XPReward;
        public bool Claimed;
        public string WeekStartUtc;
    }

    [Serializable]
    public class AchievementProgressData
    {
        public string AchievementId;
        public string DisplayName;
        public string Description;
        public int Progress;
        public int Target;
        public bool Unlocked;
        public string UnlockedAtUtc;
    }

    [Serializable]
    public class AchievementListWrapper
    {
        // PlayFab UserData values are single strings, so a list needs a wrapper to (de)serialize with JsonUtility.
        public AchievementProgressData[] Achievements;
    }

    public static class PlayFabDataKeys
    {
        // UserData keys (arbitrary key/value, not used for leaderboard ranking)
        public const string Profile = "PlayerProfile";
        public const string DailyChallenge = "DailyChallenge";
        public const string WeeklyChallenge = "WeeklyChallenge";
        public const string Achievements = "Achievements";

        // PlayFab Statistics names (server-aggregated, used for leaderboards)
        public const string StatPlayerLevel = "PlayerLevel";
        public const string StatTotalWins = "TotalWins";
        public const string StatWinStreak = "WinStreak";

        // Virtual currency codes (configure to match the Economy/Currency setup in the PlayFab dashboard)
        public const string CurrencyCoins = "CR";
    }
}
