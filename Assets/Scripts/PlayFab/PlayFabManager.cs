using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Core PlayFab session: bootstraps login and exposes the logged-in player's identity
    /// to the rest of the game. All other PlayFab services assume this has logged in first.
    /// </summary>
    public class PlayFabManager : MonoBehaviour
    {
        public static PlayFabManager Instance { get; private set; }

        public bool IsLoggedIn { get; private set; }
        public string PlayFabId { get; private set; }
        public bool IsNewlyCreatedAccount { get; private set; }

        public event Action<LoginResult> OnLoginSuccess;
        public event Action<PlayFabError> OnLoginFailed;

        [SerializeField] private bool loginOnAwake = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
            {
                Debug.LogError("[PlayFabManager] TitleId is not set. Configure it via Window > PlayFab > Editor Extensions, " +
                                "or set PlayFabSettings.staticSettings.TitleId before login.");
            }
        }

        private void Start()
        {
            if (loginOnAwake)
                LoginWithDevice();
        }

        /// <summary>
        /// Logs in using a stable per-device identifier. Good enough for Practice/local progress;
        /// swap in LoginWithEmailAddress / LoginWithGoogleAccount / etc. for full account linking later.
        /// </summary>
        public void LoginWithDevice()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetUserData = true,
                    GetUserVirtualCurrency = true,
                    GetPlayerStatistics = true
                }
            };

            PlayFabClientAPI.LoginWithCustomID(request, HandleLoginSuccess, HandleLoginFailure);
        }

        private void HandleLoginSuccess(LoginResult result)
        {
            IsLoggedIn = true;
            PlayFabId = result.PlayFabId;
            IsNewlyCreatedAccount = result.NewlyCreated;

            Debug.Log($"[PlayFabManager] Login succeeded. PlayFabId={PlayFabId} NewAccount={IsNewlyCreatedAccount}");
            OnLoginSuccess?.Invoke(result);
        }

        private void HandleLoginFailure(PlayFabError error)
        {
            IsLoggedIn = false;
            Debug.LogError($"[PlayFabManager] Login failed: {error.GenerateErrorReport()}");
            OnLoginFailed?.Invoke(error);
        }
    }
}
