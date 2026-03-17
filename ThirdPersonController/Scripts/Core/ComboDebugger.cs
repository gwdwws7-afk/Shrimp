using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// Runtime combo debugger for quick combat feedback checks.
    /// </summary>
    public class ComboDebugger : MonoBehaviour
    {
        [Header("Input")]
        public string statusActionName = "DebugComboStatus";
        public string resetHintActionName = "DebugComboResetHint";
        public KeyCode statusKey = KeyCode.Tab;
        public KeyCode resetHintKey = KeyCode.F7;
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

            string statusBinding = ResolveBindingLabel(statusActionName, statusKey);
            string resetBinding = ResolveBindingLabel(resetHintActionName, resetHintKey);
            Debug.Log($"[ComboDebugger] Started. {statusBinding}=status, {resetBinding}=reset hint.");
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
            PlayerInputHandler handler = ResolveInputHandler();
            bool statusPressed = handler != null && handler.WasActionPressedThisFrame(statusActionName, statusKey);
            if (statusPressed && combat != null)
            {
                Debug.Log($"[ComboDebugger] Combo={combat.CurrentCombo} Tier={combat.CurrentTier} Berserk={combat.IsBerserk}");
            }

            bool resetHintPressed = handler != null && handler.WasActionPressedThisFrame(resetHintActionName, resetHintKey);
            if (resetHintPressed)
            {
                Debug.Log("[ComboDebugger] Manual combo reset shortcut is not wired.");
            }
        }

        private PlayerInputHandler ResolveInputHandler()
        {
            if (inputHandler != null)
            {
                return inputHandler;
            }

            inputHandler = PlayerInputHandler.ResolveActiveInstance();
            return inputHandler;
        }

        private string ResolveBindingLabel(string actionName, KeyCode fallbackKey)
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null)
            {
                return PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey);
            }

            if (!handler.AreDebugHotkeysEnabled())
            {
                return "Disabled";
            }

            string binding = handler.GetActionBindingLabel(actionName, fallbackKey, includeGamepad: false);
            return string.IsNullOrEmpty(binding)
                ? PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey)
                : binding;
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
