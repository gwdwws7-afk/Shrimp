using UnityEngine;

namespace ThirdPersonController
{
    public class TalentUnlockController : MonoBehaviour
    {
        [Header("References")]
        public TalentTree talentTree;
        public PlayerCombat combat;
        public PlayerMusouSystem musou;

        [Header("Combo Unlocks")]
        public int baseComboSteps = 1;
        public string comboStep2NodeId = "combo_2";
        public string comboStep3NodeId = "combo_3";

        [Header("Musou Unlock")]
        public string musouUnlockNodeId = "musou_unlock";

        private void Awake()
        {
            if (talentTree == null)
            {
                talentTree = GetComponent<TalentTree>();
            }

            if (combat == null)
            {
                combat = GetComponent<PlayerCombat>();
            }

            if (musou == null)
            {
                musou = GetComponent<PlayerMusouSystem>();
            }
        }

        private void OnEnable()
        {
            if (talentTree != null)
            {
                talentTree.OnTalentChanged += ApplyUnlocks;
            }

            ApplyUnlocks();
        }

        private void OnDisable()
        {
            if (talentTree != null)
            {
                talentTree.OnTalentChanged -= ApplyUnlocks;
            }
        }

        private void ApplyUnlocks()
        {
            if (talentTree == null)
            {
                return;
            }

            int steps = Mathf.Max(1, baseComboSteps);
            if (IsUnlocked(comboStep2NodeId))
            {
                steps = Mathf.Max(steps, 2);
            }

            if (IsUnlocked(comboStep3NodeId))
            {
                steps = Mathf.Max(steps, 3);
            }

            if (combat != null)
            {
                combat.SetMaxComboStepsUnlocked(steps);
            }

            if (musou != null)
            {
                musou.SetUnlocked(IsUnlocked(musouUnlockNodeId));
            }
        }

        private bool IsUnlocked(string nodeId)
        {
            return talentTree != null && !string.IsNullOrEmpty(nodeId) && talentTree.IsUnlocked(nodeId);
        }
    }
}
