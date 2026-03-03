using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class ComboMomentumRewardSystem : MonoBehaviour
    {
        [Header("Milestones")]
        public List<int> comboMilestones = new List<int> { 30, 60, 90 };
        public bool resetOnComboDrop = true;

        [Header("Rewards")]
        public float musouGainPerMilestone = 22f;
        public float staminaGainPerMilestone = 18f;
        public bool showMessages = true;

        private PlayerMusouSystem musouSystem;
        private StaminaSystem staminaSystem;
        private int nextMilestoneIndex;
        private int lastCombo;

        private void Awake()
        {
            musouSystem = FindObjectOfType<PlayerMusouSystem>();
            staminaSystem = FindObjectOfType<StaminaSystem>();
        }

        private void OnEnable()
        {
            GameEvents.OnComboCountChanged += HandleComboChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnComboCountChanged -= HandleComboChanged;
        }

        private void HandleComboChanged(int combo)
        {
            if (comboMilestones == null || comboMilestones.Count == 0)
            {
                return;
            }

            if (resetOnComboDrop && combo < lastCombo)
            {
                ResetMilestones(combo);
            }

            lastCombo = combo;
            while (nextMilestoneIndex < comboMilestones.Count && combo >= comboMilestones[nextMilestoneIndex])
            {
                GrantMilestoneReward(comboMilestones[nextMilestoneIndex]);
                nextMilestoneIndex++;
            }
        }

        private void ResetMilestones(int combo)
        {
            nextMilestoneIndex = 0;
            for (int i = 0; i < comboMilestones.Count; i++)
            {
                if (combo < comboMilestones[i])
                {
                    nextMilestoneIndex = i;
                    return;
                }
            }
            nextMilestoneIndex = comboMilestones.Count;
        }

        private void GrantMilestoneReward(int milestone)
        {
            if (musouSystem != null && musouGainPerMilestone > 0f)
            {
                musouSystem.AddMusou(musouGainPerMilestone);
            }

            if (staminaSystem != null && staminaGainPerMilestone > 0f)
            {
                staminaSystem.RecoverStamina(staminaGainPerMilestone);
            }

            if (showMessages)
            {
                string message = $"Combo {milestone}! +{musouGainPerMilestone:F0} Musou";
                GameEvents.ShowMessage(message, 1.4f);
            }
        }
    }
}
