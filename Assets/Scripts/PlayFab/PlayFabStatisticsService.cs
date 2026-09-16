using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Submits match results as PlayFab Statistics and reads them back for the
    /// Statistics screen and the Global/Friends leaderboards.
    /// </summary>
    public static class PlayFabStatisticsService
    {
        public static event Action<List<PlayerLeaderboardEntry>> OnGlobalLeaderboardLoaded;
        public static event Action<List<PlayerLeaderboardEntry>> OnFriendsLeaderboardLoaded;
        public static event Action<List<PlayerLeaderboardEntry>> OnCountryLeaderboardLoaded;
        public static event Action<PlayFabError> OnError;

        public static void SubmitStatistic(string statisticName, int value)
        {
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = statisticName, Value = value }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request, _ => { }, HandleError);
        }

        /// <summary>Call after a match ends to record a win and bump the win streak.</summary>
        public static void ReportMatchResult(bool won, int currentStreak)
        {
            if (won)
            {
                SubmitStatistic(PlayFabDataKeys.StatTotalWins, GetCachedStatOrZero(PlayFabDataKeys.StatTotalWins) + 1);
                SubmitStatistic(PlayFabDataKeys.StatWinStreak, currentStreak + 1);
            }
            else
            {
                SubmitStatistic(PlayFabDataKeys.StatWinStreak, 0);
            }
        }

        private static readonly Dictionary<string, int> _statCache = new Dictionary<string, int>();
        private static int GetCachedStatOrZero(string name) => _statCache.TryGetValue(name, out var v) ? v : 0;

        public static void LoadOwnStatistics()
        {
            var request = new GetPlayerStatisticsRequest
            {
                StatisticNames = new List<string>
                {
                    PlayFabDataKeys.StatPlayerLevel,
                    PlayFabDataKeys.StatTotalWins,
                    PlayFabDataKeys.StatWinStreak
                }
            };

            PlayFabClientAPI.GetPlayerStatistics(request, result =>
            {
                _statCache.Clear();
                foreach (var stat in result.Statistics)
                    _statCache[stat.StatisticName] = stat.Value;
            }, HandleError);
        }

        /// <summary>Top N players worldwide, ranked by total wins.</summary>
        public static void LoadGlobalLeaderboard(int maxResults = 20)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = PlayFabDataKeys.StatTotalWins,
                StartPosition = 0,
                MaxResultsCount = maxResults,
                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
            };

            PlayFabClientAPI.GetLeaderboard(request, result => OnGlobalLeaderboardLoaded?.Invoke(result.Leaderboard), HandleError);
        }

        /// <summary>Leaderboard scoped to the caller's PlayFab friends list.</summary>
        public static void LoadFriendsLeaderboard(int maxResults = 20)
        {
            var request = new GetFriendLeaderboardRequest
            {
                StatisticName = PlayFabDataKeys.StatTotalWins,
                MaxResultsCount = maxResults,
                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
            };

            PlayFabClientAPI.GetFriendLeaderboard(request, result => OnFriendsLeaderboardLoaded?.Invoke(result.Leaderboard), HandleError);
        }

        /// <summary>
        /// PlayFab has no native "leaderboard by country" query. This pulls a larger slice of the
        /// global board and filters client-side by the Country stored in each player's profile data
        /// — fine for prototyping, but should move to a CloudScript/Azure Function aggregation for
        /// real scale (fetching + filtering client-side won't paginate correctly at high player counts).
        /// </summary>
        public static void LoadCountryLeaderboard(string countryCode, int sampleSize = 200, int maxResults = 20)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = PlayFabDataKeys.StatTotalWins,
                StartPosition = 0,
                MaxResultsCount = sampleSize,
                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
            };

            PlayFabClientAPI.GetLeaderboard(request, result =>
            {
                // TODO: Country isn't part of PlayerProfileViewConstraints; resolving it per-entry
                // requires either storing it in a Statistic-adjacent lookup or a CloudScript call.
                // Placeholder: returns the unfiltered sample until that lookup is wired up.
                var trimmed = result.Leaderboard.Take(maxResults).ToList();
                OnCountryLeaderboardLoaded?.Invoke(trimmed);
            }, HandleError);
        }

        private static void HandleError(PlayFabError error)
        {
            Debug.LogError($"[PlayFabStatisticsService] {error.GenerateErrorReport()}");
            OnError?.Invoke(error);
        }
    }
}
