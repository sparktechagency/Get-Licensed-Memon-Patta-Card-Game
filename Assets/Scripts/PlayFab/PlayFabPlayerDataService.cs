using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Reads/writes the player's profile (display name, level, XP, avatar) used by the
    /// Home screen top bar and the Profile screen's Stats Overview / Account Info sections.
    /// </summary>
    public static class PlayFabPlayerDataService
    {
        public static event Action<PlayerProfileData> OnProfileLoaded;
        public static event Action<PlayFabError> OnProfileError;

        public static PlayerProfileData CachedProfile { get; private set; }

        public static void LoadProfile()
        {
            var request = new GetUserDataRequest { Keys = new List<string> { PlayFabDataKeys.Profile } };
            PlayFabClientAPI.GetUserData(request, OnGetUserData, OnError);
        }

        private static void OnGetUserData(GetUserDataResult result)
        {
            PlayerProfileData profile = null;

            if (result.Data != null && result.Data.TryGetValue(PlayFabDataKeys.Profile, out var record) && !string.IsNullOrEmpty(record.Value))
            {
                try { profile = JsonUtility.FromJson<PlayerProfileData>(record.Value); }
                catch (Exception e) { Debug.LogError($"[PlayFabPlayerDataService] Failed to parse profile JSON: {e.Message}"); }
            }

            if (profile == null)
            {
                // First login: seed a default profile and persist it immediately.
                profile = new PlayerProfileData
                {
                    DisplayName = "Player",
                    Level = 1,
                    CurrentXP = 0,
                    XPToNextLevel = 1000,
                    AvatarId = "default",
                    Country = ""
                };
                SaveProfile(profile);
            }

            CachedProfile = profile;
            OnProfileLoaded?.Invoke(profile);
        }

        public static void SaveProfile(PlayerProfileData profile)
        {
            CachedProfile = profile;

            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { PlayFabDataKeys.Profile, JsonUtility.ToJson(profile) }
                },
                Permission = UserDataPermission.Public // Public so friends/leaderboard views can read display data later
            };

            PlayFabClientAPI.UpdateUserData(request, _ => { }, OnError);
        }

        /// <summary>
        /// Awards XP client-side and levels up locally, then persists. NOTE: for a real economy this
        /// should be validated in CloudScript so clients can't just grant themselves XP.
        /// </summary>
        public static void AddXP(int amount)
        {
            if (CachedProfile == null)
            {
                Debug.LogWarning("[PlayFabPlayerDataService] AddXP called before profile was loaded.");
                return;
            }

            CachedProfile.CurrentXP += amount;
            while (CachedProfile.CurrentXP >= CachedProfile.XPToNextLevel)
            {
                CachedProfile.CurrentXP -= CachedProfile.XPToNextLevel;
                CachedProfile.Level++;
                CachedProfile.XPToNextLevel = Mathf.RoundToInt(CachedProfile.XPToNextLevel * 1.15f);
            }

            SaveProfile(CachedProfile);
            PlayFabStatisticsService.SubmitStatistic(PlayFabDataKeys.StatPlayerLevel, CachedProfile.Level);
        }

        public static void UpdateDisplayName(string newName)
        {
            var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newName };
            PlayFabClientAPI.UpdateUserTitleDisplayName(request, result =>
            {
                if (CachedProfile != null)
                {
                    CachedProfile.DisplayName = result.DisplayName;
                    SaveProfile(CachedProfile);
                }
            }, OnError);
        }

        private static void OnError(PlayFabError error)
        {
            Debug.LogError($"[PlayFabPlayerDataService] {error.GenerateErrorReport()}");
            OnProfileError?.Invoke(error);
        }
    }
}
