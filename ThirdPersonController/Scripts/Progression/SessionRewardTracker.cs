using UnityEngine;

namespace ThirdPersonController
{
    public class SessionRewardTracker : MonoBehaviour
    {
        [Header("References")]
        public TalentTree talentTree;
        public PearlInventory inventory;
        public CurrencyWallet wallet;

        [Header("Session Baseline")]
        public int startTalentPoints;
        public int startPearls;
        public int startCredits;

        [Header("Session Gains")]
        public int lastGainedTalentPoints;
        public int lastGainedPearls;
        public int lastGainedCredits;

        private bool hasBaseline;

        private void Awake()
        {
            if (talentTree == null)
            {
                talentTree = FindObjectOfType<TalentTree>();
            }

            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }

            if (wallet == null)
            {
                wallet = FindObjectOfType<CurrencyWallet>();
                if (wallet == null)
                {
                    wallet = CurrencyWallet.EnsureInstance();
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLevelStarted += HandleLevelStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= HandleLevelStarted;
        }

        private void HandleLevelStarted(int levelId)
        {
            CaptureStart();
        }

        public void CaptureStart()
        {
            startTalentPoints = talentTree != null ? talentTree.availablePoints : 0;
            startPearls = inventory != null && inventory.ownedPearls != null ? inventory.ownedPearls.Count : 0;
            startCredits = wallet != null ? wallet.Credits : 0;
            lastGainedTalentPoints = 0;
            lastGainedPearls = 0;
            lastGainedCredits = 0;
            hasBaseline = true;
        }

        public void CaptureEnd()
        {
            if (!hasBaseline)
            {
                CaptureStart();
            }

            int currentTalentPoints = talentTree != null ? talentTree.availablePoints : 0;
            int currentPearls = inventory != null && inventory.ownedPearls != null ? inventory.ownedPearls.Count : 0;
            int currentCredits = wallet != null ? wallet.Credits : 0;

            lastGainedTalentPoints = Mathf.Max(0, currentTalentPoints - startTalentPoints);
            lastGainedPearls = Mathf.Max(0, currentPearls - startPearls);
            lastGainedCredits = Mathf.Max(0, currentCredits - startCredits);
        }
    }
}
