using UnityEngine;

namespace ThirdPersonController
{
    public class CurrencyWallet : MonoBehaviour
    {
        public static CurrencyWallet Instance { get; private set; }

        [Header("Currency")]
        public string currencyLabel = "Credits";
        public bool showMessages = true;

        private int credits;

        public event System.Action<int> OnCreditsChanged;

        public int Credits => credits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadFromSave();
        }

        public static CurrencyWallet EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject walletObject = new GameObject("CurrencyWallet");
            return walletObject.AddComponent<CurrencyWallet>();
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            credits += amount;
            SaveToData();
            OnCreditsChanged?.Invoke(credits);

            if (showMessages)
            {
                GameEvents.ShowMessage($"+{amount} {currencyLabel}", 1.4f);
            }
        }

        public bool SpendCredits(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (credits < amount)
            {
                return false;
            }

            credits -= amount;
            SaveToData();
            OnCreditsChanged?.Invoke(credits);

            if (showMessages)
            {
                GameEvents.ShowMessage($"-{amount} {currencyLabel}", 1.2f);
            }

            return true;
        }

        public void SetCredits(int amount)
        {
            credits = Mathf.Max(0, amount);
            SaveToData();
            OnCreditsChanged?.Invoke(credits);
        }

        private void LoadFromSave()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                credits = Mathf.Max(0, SaveManager.Instance.CurrentData.credits);
            }
        }

        private void SaveToData()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.credits = credits;
            }
        }
    }
}
