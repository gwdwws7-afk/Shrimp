using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// Runtime combo debugger for quick combat feedback checks.
    /// </summary>
    public class ComboDebugger : MonoBehaviour
    {
        [Header("Input")]
        public KeyCode statusKey = KeyCode.Tab;
        public KeyCode resetHintKey = KeyCode.R;
        public PlayerInputHandler inputHandler;

        private PlayerCombat combat;
        private int lastCombo;
        private bool wasBerserk;

        private void Start()
        {
            combat = GetComponent<PlayerCombat>();
            if (inputHandler == null)
            {
                inputHandler = GetComponent<PlayerInputHandler>();
            }

            if (combat == null)
            {
                Debug.LogError("[ComboDebugger] PlayerCombat is missing.");
                return;
            }

            combat.OnComboChanged += OnComboChanged;
            combat.OnBerserkStateChanged += OnBerserkStateChanged;

            Debug.Log("[ComboDebugger] Started. Hit targets to inspect combo transitions.");
        }

        private void OnDestroy()
        {
            if (combat == null)
            {
                return;
            }

            combat.OnComboChanged -= OnComboChanged;
            combat.OnBerserkStateChanged -= OnBerserkStateChanged;
        }

        private void Update()
        {
            bool statusPressed = inputHandler != null
                ? inputHandler.WasUnifiedKeyPressedThisFrame(statusKey)
                : PlayerInputHandler.ReadUnifiedKeyDown(statusKey);
            if (statusPressed && combat != null)
            {
                Debug.Log($"[ComboDebugger] Combo={combat.CurrentCombo} Tier={combat.CurrentTier} Berserk={combat.IsBerserk}");
            }

            bool resetHintPressed = inputHandler != null
                ? inputHandler.WasUnifiedKeyPressedThisFrame(resetHintKey)
                : PlayerInputHandler.ReadUnifiedKeyDown(resetHintKey);
            if (resetHintPressed)
            {
                Debug.Log("[ComboDebugger] Manual combo reset shortcut is not wired.");
            }
        }

        private void OnComboChanged(int combo)
        {
            if (combo > lastCombo)
            {
                Debug.Log($"[ComboDebugger] Combo increased to {combo} {GetTierString(combat.CurrentTier)}");
            }
            else if (combo == 0 && lastCombo > 0)
            {
                Debug.Log($"[ComboDebugger] Combo dropped from {lastCombo} to 0.");
            }

            lastCombo = combo;
        }

        private void OnBerserkStateChanged(bool isActive)
        {
            if (isActive && !wasBerserk)
            {
                Debug.Log("[ComboDebugger] Berserk started.");
            }
            else if (!isActive && wasBerserk)
            {
                Debug.Log("[ComboDebugger] Berserk ended.");
            }

            wasBerserk = isActive;
        }

        private static string GetTierString(ComboTier tier)
        {
            return tier switch
            {
                ComboTier.Tier1 => "[T1]",
                ComboTier.Tier2 => "[T2]",
                ComboTier.Tier3 => "[T3]",
                ComboTier.Tier4 => "[T4]",
                _ => string.Empty
            };
        }
    }
}
