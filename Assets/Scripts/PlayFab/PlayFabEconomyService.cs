using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MemonPatta.PlayFabServices
{
    /// <summary>
    /// Virtual currency balance (room entry fees, rewards) and Premium membership entitlement.
    /// Uses the classic Client/Catalog inventory model (GetUserInventory + PurchaseItem). If the
    /// project later adopts PlayFab's newer Economy v2 service, swap these calls for
    /// PlayFabEconomyAPI equivalents (GetInventoryItems / PurchaseInventoryItems) — the SDK module
    /// is already installed (Assets/PlayFabSDK/Economy).
    /// </summary>
    public static class PlayFabEconomyService
    {
        public static event Action<Dictionary<string, int>> OnVirtualCurrencyLoaded;
        public static event Action<bool> OnPremiumStatusLoaded;
        public static event Action<PlayFabError> OnError;

        public static Dictionary<string, int> CachedCurrency { get; private set; } = new Dictionary<string, int>();
        public static bool IsPremium { get; private set; }

        // Must match the Catalog Item ID configured for the Premium membership item in the PlayFab dashboard.
        private const string PremiumCatalogItemId = "premium_membership";

        public static void LoadCurrencyBalance()
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
            {
                CachedCurrency = new Dictionary<string, int>(result.VirtualCurrency);
                OnVirtualCurrencyLoaded?.Invoke(CachedCurrency);

                IsPremium = false;
                foreach (var item in result.Inventory)
                {
                    if (item.ItemId == PremiumCatalogItemId)
                    {
                        IsPremium = true;
                        break;
                    }
                }
                OnPremiumStatusLoaded?.Invoke(IsPremium);
            }, HandleError);
        }

        /// <summary>Deducts a room entry fee locally-tracked currency (e.g. joining "Pro Room" for 1,000).</summary>
        public static void SpendCurrency(string currencyCode, int amount, Action onSuccess = null, Action<PlayFabError> onInsufficientFunds = null)
        {
            var request = new SubtractUserVirtualCurrencyRequest { VirtualCurrency = currencyCode, Amount = amount };
            PlayFabClientAPI.SubtractUserVirtualCurrency(request, result =>
            {
                CachedCurrency[currencyCode] = result.Balance;
                onSuccess?.Invoke();
            }, error =>
            {
                if (error.Error == PlayFabErrorCode.InsufficientFunds) onInsufficientFunds?.Invoke(error);
                HandleError(error);
            });
        }

        public static void GrantCurrency(string currencyCode, int amount, Action onSuccess = null)
        {
            var request = new AddUserVirtualCurrencyRequest { VirtualCurrency = currencyCode, Amount = amount };
            PlayFabClientAPI.AddUserVirtualCurrency(request, result =>
            {
                CachedCurrency[currencyCode] = result.Balance;
                onSuccess?.Invoke();
            }, HandleError);
        }

        /// <summary>Purchases the Premium membership catalog item using in-game currency.</summary>
        public static void PurchasePremium(string priceCurrencyCode, int price, Action onSuccess = null)
        {
            var request = new PurchaseItemRequest
            {
                ItemId = PremiumCatalogItemId,
                VirtualCurrency = priceCurrencyCode,
                Price = price
            };

            PlayFabClientAPI.PurchaseItem(request, _ =>
            {
                IsPremium = true;
                OnPremiumStatusLoaded?.Invoke(true);
                onSuccess?.Invoke();
            }, HandleError);
        }

        private static void HandleError(PlayFabError error)
        {
            Debug.LogError($"[PlayFabEconomyService] {error.GenerateErrorReport()}");
            OnError?.Invoke(error);
        }
    }
}
