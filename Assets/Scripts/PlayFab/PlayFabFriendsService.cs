using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Backs the Friends screen (Friends / Recent tabs). PlayFab's AddFriend call links two
    /// accounts immediately — there is no built-in pending-request state, so the mockup's
    /// "Requests" / "Invites" tabs need a custom store (e.g. a CloudScript function backed by
    /// a Title/Entity data table) before they can show anything other than already-added friends.
    /// </summary>
    public static class PlayFabFriendsService
    {
        public static event Action<List<FriendInfo>> OnFriendsListLoaded;
        public static event Action<PlayFabError> OnError;

        public static void LoadFriends()
        {
            var request = new GetFriendsListRequest
            {
                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
            };

            PlayFabClientAPI.GetFriendsList(request, result => OnFriendsListLoaded?.Invoke(result.Friends), HandleError);
        }

        /// <summary>Adds a friend immediately by exact PlayFab title display name.</summary>
        public static void AddFriendByDisplayName(string displayName, Action onSuccess = null)
        {
            var request = new AddFriendRequest { FriendTitleDisplayName = displayName };
            PlayFabClientAPI.AddFriend(request, _ => onSuccess?.Invoke(), HandleError);
        }

        public static void RemoveFriend(string friendPlayFabId, Action onSuccess = null)
        {
            var request = new RemoveFriendRequest { FriendPlayFabId = friendPlayFabId };
            PlayFabClientAPI.RemoveFriend(request, _ => onSuccess?.Invoke(), HandleError);
        }

        // TODO(challenge-a-friend): sending a 1v1 challenge invite needs realtime delivery
        // (PlayFab PubSub / a multiplayer server / push notification), not just a data write.
        // Stubbed here so the UI has a call site to wire up once that transport is chosen.
        public static void ChallengeFriend(string friendPlayFabId)
        {
            Debug.LogWarning($"[PlayFabFriendsService] ChallengeFriend({friendPlayFabId}) not implemented yet — " +
                              "needs a realtime invite/matchmaking transport.");
        }

        private static void HandleError(PlayFabError error)
        {
            Debug.LogError($"[PlayFabFriendsService] {error.GenerateErrorReport()}");
            OnError?.Invoke(error);
        }
    }
}
