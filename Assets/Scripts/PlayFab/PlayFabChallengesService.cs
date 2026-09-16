using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Daily/Weekly challenge progress ("Win 2 Rounds", +150 XP) and achievement unlocks
    /// (First Win, Card Master, Unstoppable, Pro Player). Stored as UserData JSON blobs.
    /// NOTE: progress/claim here is client-authoritative for now — a cheating client could grant
    /// itself rewards. Move ClaimDailyChallenge/UnlockAchievement into CloudScript once available.
    /// </summary>
    public static class PlayFabChallengesService
    {
        public static event Action<DailyChallengeData> OnDailyChallengeLoaded;
        public static event Action<WeeklyChallengeData> OnWeeklyChallengeLoaded;
        public static event Action<AchievementProgressData[]> OnAchievementsLoaded;
        public static event Action<PlayFabError> OnError;

        public static DailyChallengeData CachedDaily { get; private set; }
        public static WeeklyChallengeData CachedWeekly { get; private set; }
        public static AchievementProgressData[] CachedAchievements { get; private set; } = Array.Empty<AchievementProgressData>();

        public static void LoadAll()
        {
            var request = new GetUserDataRequest
            {
                Keys = new List<string> { PlayFabDataKeys.DailyChallenge, PlayFabDataKeys.WeeklyChallenge, PlayFabDataKeys.Achievements }
            };
            PlayFabClientAPI.GetUserData(request, OnGetUserData, HandleError);
        }

        private static void OnGetUserData(GetUserDataResult result)
        {
            CachedDaily = ParseOrDefault(result, PlayFabDataKeys.DailyChallenge, DefaultDaily());
            CachedWeekly = ParseOrDefault(result, PlayFabDataKeys.WeeklyChallenge, DefaultWeekly());

            if (result.Data != null && result.Data.TryGetValue(PlayFabDataKeys.Achievements, out var achRecord) && !string.IsNullOrEmpty(achRecord.Value))
            {
                var wrapper = JsonUtility.FromJson<AchievementListWrapper>(achRecord.Value);
                CachedAchievements = wrapper?.Achievements ?? DefaultAchievements();
            }
            else
            {
                CachedAchievements = DefaultAchievements();
                SaveAchievements(CachedAchievements);
            }

            OnDailyChallengeLoaded?.Invoke(CachedDaily);
            OnWeeklyChallengeLoaded?.Invoke(CachedWeekly);
            OnAchievementsLoaded?.Invoke(CachedAchievements);
        }

        private static T ParseOrDefault<T>(GetUserDataResult result, string key, T fallback) where T : class
        {
            if (result.Data != null && result.Data.TryGetValue(key, out var record) && !string.IsNullOrEmpty(record.Value))
            {
                try { return JsonUtility.FromJson<T>(record.Value); }
                catch (Exception e) { Debug.LogError($"[PlayFabChallengesService] Failed to parse {key}: {e.Message}"); }
            }
            SaveBlob(key, fallback);
            return fallback;
        }

        private static DailyChallengeData DefaultDaily() => new DailyChallengeData
        {
            ChallengeId = "daily_win_rounds",
            Description = "Win 2 Rounds",
            Progress = 0,
            Target = 2,
            XPReward = 150,
            Claimed = false,
            DateIssuedUtc = DateTime.UtcNow.Date.ToString("o")
        };

        private static WeeklyChallengeData DefaultWeekly() => new WeeklyChallengeData
        {
            ChallengeId = "weekly_win_rounds",
            Description = "Win 10 Rounds",
            Progress = 0,
            Target = 10,
            XPReward = 500,
            Claimed = false,
            WeekStartUtc = DateTime.UtcNow.Date.ToString("o")
        };

        private static AchievementProgressData[] DefaultAchievements() => new[]
        {
            new AchievementProgressData { AchievementId = "first_win", DisplayName = "First Win", Description = "Win your first round", Progress = 0, Target = 1, Unlocked = false },
            new AchievementProgressData { AchievementId = "card_master", DisplayName = "Card Master", Description = "Win 10 rounds", Progress = 0, Target = 10, Unlocked = false },
            new AchievementProgressData { AchievementId = "unstoppable", DisplayName = "Unstoppable", Description = "Win 5 rounds in a row", Progress = 0, Target = 5, Unlocked = false },
            new AchievementProgressData { AchievementId = "pro_player", DisplayName = "Pro Player", Description = "Reach level 10", Progress = 0, Target = 10, Unlocked = false },
        };

        /// <summary>Call this whenever a round is won, e.g. from match-end logic.</summary>
        public static void ReportRoundWon(int currentWinStreak, int playerLevel)
        {
            if (CachedDaily != null && !CachedDaily.Claimed)
            {
                CachedDaily.Progress = Mathf.Min(CachedDaily.Progress + 1, CachedDaily.Target);
                SaveBlob(PlayFabDataKeys.DailyChallenge, CachedDaily);
            }

            if (CachedWeekly != null && !CachedWeekly.Claimed)
            {
                CachedWeekly.Progress = Mathf.Min(CachedWeekly.Progress + 1, CachedWeekly.Target);
                SaveBlob(PlayFabDataKeys.WeeklyChallenge, CachedWeekly);
            }

            UpdateAchievementProgress("first_win", 1);
            UpdateAchievementProgress("card_master", CachedAchievements != null ? GetProgress("card_master") + 1 : 1);
            UpdateAchievementProgress("unstoppable", currentWinStreak);
            UpdateAchievementProgress("pro_player", playerLevel);
        }

        private static int GetProgress(string achievementId)
        {
            foreach (var a in CachedAchievements)
                if (a.AchievementId == achievementId) return a.Progress;
            return 0;
        }

        private static void UpdateAchievementProgress(string achievementId, int newProgress)
        {
            if (CachedAchievements == null) return;
            foreach (var a in CachedAchievements)
            {
                if (a.AchievementId != achievementId || a.Unlocked) continue;
                a.Progress = Mathf.Max(a.Progress, newProgress);
                if (a.Progress >= a.Target)
                {
                    a.Progress = a.Target;
                    a.Unlocked = true;
                    a.UnlockedAtUtc = DateTime.UtcNow.ToString("o");
                }
            }
            SaveAchievements(CachedAchievements);
        }

        public static void ClaimDailyChallenge()
        {
            if (CachedDaily == null || CachedDaily.Claimed || CachedDaily.Progress < CachedDaily.Target) return;

            CachedDaily.Claimed = true;
            SaveBlob(PlayFabDataKeys.DailyChallenge, CachedDaily);
            PlayFabPlayerDataService.AddXP(CachedDaily.XPReward);
        }

        public static void ClaimWeeklyChallenge()
        {
            if (CachedWeekly == null || CachedWeekly.Claimed || CachedWeekly.Progress < CachedWeekly.Target) return;

            CachedWeekly.Claimed = true;
            SaveBlob(PlayFabDataKeys.WeeklyChallenge, CachedWeekly);
            PlayFabPlayerDataService.AddXP(CachedWeekly.XPReward);
        }

        private static void SaveAchievements(AchievementProgressData[] achievements)
        {
            SaveBlob(PlayFabDataKeys.Achievements, new AchievementListWrapper { Achievements = achievements });
        }

        private static void SaveBlob<T>(string key, T value)
        {
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { key, JsonUtility.ToJson(value) } },
                Permission = UserDataPermission.Private
            };
            PlayFabClientAPI.UpdateUserData(request, _ => { }, HandleError);
        }

        private static void HandleError(PlayFabError error)
        {
            Debug.LogError($"[PlayFabChallengesService] {error.GenerateErrorReport()}");
            OnError?.Invoke(error);
        }
    }
}
