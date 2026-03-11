using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace ThirdPersonController
{
    /// <summary>
    /// Player input handler.
    /// Uses Input Actions when Input System is enabled, falls back to Legacy Input otherwise.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public static PlayerInputHandler ActiveInstance { get; private set; }

        [Header("Input Settings (Legacy Fallback)")]
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public string mouseXAxis = "Mouse X";
        public string mouseYAxis = "Mouse Y";

        [Header("Key Bindings (Legacy Fallback)")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public KeyCode crouchKey = KeyCode.LeftControl;
        public KeyCode attackKey = KeyCode.Mouse0;
        public KeyCode heavyAttackKey = KeyCode.Mouse1;
        public KeyCode interactKey = KeyCode.E;
        public KeyCode blockKey = KeyCode.Mouse2;
        public KeyCode dodgeKey = KeyCode.LeftAlt;
        public KeyCode skill1Key = KeyCode.Q;
        public KeyCode skill2Key = KeyCode.W;
        public KeyCode skill3Key = KeyCode.E;
        public KeyCode skill4Key = KeyCode.R;
        public KeyCode skill5Key = KeyCode.T;
        public KeyCode skill6Key = KeyCode.F;
        public KeyCode musouKey = KeyCode.V;
        public KeyCode quickSlot1Key = KeyCode.Alpha1;
        public KeyCode quickSlot2Key = KeyCode.Alpha2;
        public KeyCode quickSlot3Key = KeyCode.Alpha3;

#if ENABLE_INPUT_SYSTEM
        [Header("Input Actions")]
        public bool useInputActions = true;
        public float mouseLookScale = 1f;
        public float gamepadLookScale = 140f;
        public string rebindSaveKey = "ThirdPersonController.PlayerInput.Rebinds";
#endif

        [Header("Cursor Settings")]
        public bool lockCursor = true;

        // Input state properties
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintPressed { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool HeavyAttackPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool BlockHeld { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool MusouPressed { get; private set; }
        public float MouseScrollDelta { get; private set; }
        public float GamepadZoomAxis { get; private set; }
        private readonly bool[] skillPressedThisFrame = new bool[6];
        private readonly bool[] quickSlotPressedThisFrame = new bool[3];

#if ENABLE_INPUT_SYSTEM
        private InputActionMap gameplayActionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction attackAction;
        private InputAction heavyAttackAction;
        private InputAction interactAction;
        private InputAction blockAction;
        private InputAction dodgeAction;
        private InputAction musouAction;
        private InputAction[] skillActions;
        private InputAction[] quickSlotActions;
        private InputActionRebindingExtensions.RebindingOperation rebindOperation;
#endif

        private void Awake()
        {
            ActiveInstance = this;

#if ENABLE_INPUT_SYSTEM
            if (useInputActions)
            {
                BuildInputActions();
                LoadBindingOverrides();
            }
#endif
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (useInputActions)
            {
                gameplayActionMap?.Enable();
            }
#endif

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (useInputActions && gameplayActionMap != null)
            {
                ReadInputActions();
                HandleCursorToggleInputActions();
                return;
            }
#endif

            ReadLegacyInput();
            HandleCursorToggleLegacy();
        }

        private void ReadLegacyInput()
        {
            float horizontal = Input.GetAxisRaw(horizontalAxis);
            float vertical = Input.GetAxisRaw(verticalAxis);
            MoveInput = new Vector2(horizontal, vertical).normalized;

            float mouseX = Input.GetAxis(mouseXAxis);
            float mouseY = Input.GetAxis(mouseYAxis);
            LookInput = new Vector2(mouseX, mouseY);

            JumpPressed = Input.GetKeyDown(jumpKey);
            JumpHeld = Input.GetKey(jumpKey);
            SprintPressed = Input.GetKey(sprintKey);
            CrouchPressed = Input.GetKey(crouchKey);
            AttackPressed = Input.GetKeyDown(attackKey);
            HeavyAttackPressed = Input.GetKeyDown(heavyAttackKey);
            InteractPressed = Input.GetKeyDown(interactKey);
            BlockHeld = Input.GetKey(blockKey);
            DodgePressed = Input.GetKeyDown(dodgeKey);
            MusouPressed = Input.GetKeyDown(musouKey);

            skillPressedThisFrame[0] = Input.GetKeyDown(skill1Key);
            skillPressedThisFrame[1] = Input.GetKeyDown(skill2Key);
            skillPressedThisFrame[2] = Input.GetKeyDown(skill3Key);
            skillPressedThisFrame[3] = Input.GetKeyDown(skill4Key);
            skillPressedThisFrame[4] = Input.GetKeyDown(skill5Key);
            skillPressedThisFrame[5] = Input.GetKeyDown(skill6Key);

            quickSlotPressedThisFrame[0] = Input.GetKeyDown(quickSlot1Key);
            quickSlotPressedThisFrame[1] = Input.GetKeyDown(quickSlot2Key);
            quickSlotPressedThisFrame[2] = Input.GetKeyDown(quickSlot3Key);

            MouseScrollDelta = Input.GetAxis("Mouse ScrollWheel");
            GamepadZoomAxis = 0f;
        }

        private void HandleCursorToggleLegacy()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursorLock();
            }
        }

#if ENABLE_INPUT_SYSTEM
        private void BuildInputActions()
        {
            if (gameplayActionMap != null)
            {
                return;
            }

            gameplayActionMap = new InputActionMap("Gameplay");

            moveAction = gameplayActionMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = gameplayActionMap.AddAction("Look", InputActionType.Value);
            lookAction.AddBinding("<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jumpAction = gameplayActionMap.AddAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            sprintAction = gameplayActionMap.AddAction("Sprint", InputActionType.Button);
            sprintAction.AddBinding("<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");

            crouchAction = gameplayActionMap.AddAction("Crouch", InputActionType.Button);
            crouchAction.AddBinding("<Keyboard>/leftCtrl");
            crouchAction.AddBinding("<Gamepad>/rightStickPress");

            attackAction = gameplayActionMap.AddAction("Attack", InputActionType.Button);
            attackAction.AddBinding("<Mouse>/leftButton");
            attackAction.AddBinding("<Gamepad>/rightTrigger");

            heavyAttackAction = gameplayActionMap.AddAction("HeavyAttack", InputActionType.Button);
            heavyAttackAction.AddBinding("<Mouse>/rightButton");
            heavyAttackAction.AddBinding("<Gamepad>/leftTrigger");

            interactAction = gameplayActionMap.AddAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonWest");

            blockAction = gameplayActionMap.AddAction("Block", InputActionType.Button);
            blockAction.AddBinding("<Mouse>/middleButton");
            blockAction.AddBinding("<Gamepad>/leftShoulder");

            dodgeAction = gameplayActionMap.AddAction("Dodge", InputActionType.Button);
            dodgeAction.AddBinding("<Keyboard>/leftAlt");
            dodgeAction.AddBinding("<Gamepad>/buttonEast");

            musouAction = gameplayActionMap.AddAction("Musou", InputActionType.Button);
            musouAction.AddBinding("<Keyboard>/v");
            musouAction.AddBinding("<Gamepad>/buttonNorth");

            skillActions = new InputAction[6];
            for (int i = 0; i < skillActions.Length; i++)
            {
                skillActions[i] = gameplayActionMap.AddAction($"Skill{i + 1}", InputActionType.Button);
            }

            skillActions[0].AddBinding("<Keyboard>/q");
            skillActions[1].AddBinding("<Keyboard>/w");
            skillActions[2].AddBinding("<Keyboard>/e");
            skillActions[3].AddBinding("<Keyboard>/r");
            skillActions[4].AddBinding("<Keyboard>/t");
            skillActions[5].AddBinding("<Keyboard>/f");

            // Basic gamepad mapping for skill triggers.
            skillActions[0].AddBinding("<Gamepad>/dpad/up");
            skillActions[1].AddBinding("<Gamepad>/dpad/right");
            skillActions[2].AddBinding("<Gamepad>/dpad/down");
            skillActions[3].AddBinding("<Gamepad>/dpad/left");
            skillActions[4].AddBinding("<Gamepad>/rightShoulder");
            skillActions[5].AddBinding("<Gamepad>/leftShoulder");

            quickSlotActions = new InputAction[3];
            for (int i = 0; i < quickSlotActions.Length; i++)
            {
                quickSlotActions[i] = gameplayActionMap.AddAction($"QuickSlot{i + 1}", InputActionType.Button);
            }

            quickSlotActions[0].AddBinding("<Keyboard>/1");
            quickSlotActions[1].AddBinding("<Keyboard>/2");
            quickSlotActions[2].AddBinding("<Keyboard>/3");
        }

        private void ReadInputActions()
        {
            MoveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

            Vector2 rawLook = lookAction.ReadValue<Vector2>();
            bool usingGamepad = lookAction.activeControl != null && lookAction.activeControl.device is Gamepad;
            if (usingGamepad)
            {
                // Stick input needs time scaling to match mouse-like angular velocity.
                LookInput = rawLook * gamepadLookScale * Time.unscaledDeltaTime;
            }
            else
            {
                LookInput = rawLook * mouseLookScale;
            }

            JumpPressed = jumpAction.WasPressedThisFrame();
            JumpHeld = jumpAction.IsPressed();
            SprintPressed = sprintAction.IsPressed();
            CrouchPressed = crouchAction.IsPressed();
            AttackPressed = attackAction.WasPressedThisFrame();
            HeavyAttackPressed = heavyAttackAction.WasPressedThisFrame();
            InteractPressed = interactAction.WasPressedThisFrame();
            BlockHeld = blockAction.IsPressed();
            DodgePressed = dodgeAction.WasPressedThisFrame();
            MusouPressed = musouAction != null && musouAction.WasPressedThisFrame();

            if (skillActions != null)
            {
                for (int i = 0; i < skillPressedThisFrame.Length; i++)
                {
                    skillPressedThisFrame[i] = skillActions[i] != null && skillActions[i].WasPressedThisFrame();
                }
            }

            if (quickSlotActions != null)
            {
                for (int i = 0; i < quickSlotPressedThisFrame.Length; i++)
                {
                    quickSlotPressedThisFrame[i] = quickSlotActions[i] != null && quickSlotActions[i].WasPressedThisFrame();
                }
            }

            MouseScrollDelta = Mouse.current != null
                ? Mouse.current.scroll.ReadValue().y * 0.01f
                : 0f;

            if (Gamepad.current != null)
            {
                GamepadZoomAxis = Gamepad.current.rightTrigger.ReadValue() - Gamepad.current.leftTrigger.ReadValue();
            }
            else
            {
                GamepadZoomAxis = 0f;
            }
        }

        private void HandleCursorToggleInputActions()
        {
            bool keyboardToggle = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool gamepadToggle = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            if (keyboardToggle || gamepadToggle)
            {
                ToggleCursorLock();
            }
        }

        public bool StartInteractiveRebind(string actionName, int bindingIndex = -1, Action<string> onComplete = null, Action onCancel = null)
        {
            if (!useInputActions || gameplayActionMap == null || string.IsNullOrEmpty(actionName))
            {
                return false;
            }

            InputAction action = gameplayActionMap.FindAction(actionName, false);
            if (action == null)
            {
                return false;
            }

            if (bindingIndex < 0)
            {
                bindingIndex = GetFirstRebindableBindingIndex(action);
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return false;
            }

            DisposeRebindOperation();
            action.Disable();

            rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .OnCancel(op =>
                {
                    action.Enable();
                    DisposeRebindOperation();
                    onCancel?.Invoke();
                })
                .OnComplete(op =>
                {
                    action.Enable();
                    string displayName = action.GetBindingDisplayString(bindingIndex);
                    SaveBindingOverrides();
                    DisposeRebindOperation();
                    onComplete?.Invoke(displayName);
                });

            rebindOperation.Start();
            return true;
        }

        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            if (!useInputActions || gameplayActionMap == null || string.IsNullOrEmpty(actionName))
            {
                return string.Empty;
            }

            InputAction action = gameplayActionMap.FindAction(actionName, false);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return string.Empty;
            }

            return action.GetBindingDisplayString(bindingIndex);
        }

        public bool ResetBindingOverride(string actionName, int bindingIndex = -1)
        {
            if (!useInputActions || gameplayActionMap == null || string.IsNullOrEmpty(actionName))
            {
                return false;
            }

            InputAction action = gameplayActionMap.FindAction(actionName, false);
            if (action == null)
            {
                return false;
            }

            if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
            {
                action.RemoveBindingOverride(bindingIndex);
            }
            else
            {
                action.RemoveAllBindingOverrides();
            }

            SaveBindingOverrides();
            return true;
        }

        public void SaveBindingOverrides()
        {
            if (!useInputActions || gameplayActionMap == null || string.IsNullOrEmpty(rebindSaveKey))
            {
                return;
            }

            string json = gameplayActionMap.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(rebindSaveKey, json);
            PlayerPrefs.Save();
        }

        public void LoadBindingOverrides()
        {
            if (!useInputActions || gameplayActionMap == null || string.IsNullOrEmpty(rebindSaveKey))
            {
                return;
            }

            string json = PlayerPrefs.GetString(rebindSaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            gameplayActionMap.LoadBindingOverridesFromJson(json, true);
        }

        public void ClearBindingOverrides()
        {
            if (!useInputActions || gameplayActionMap == null)
            {
                return;
            }

            gameplayActionMap.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(rebindSaveKey))
            {
                PlayerPrefs.DeleteKey(rebindSaveKey);
                PlayerPrefs.Save();
            }
        }

        private int GetFirstRebindableBindingIndex(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (!binding.isComposite && !binding.isPartOfComposite)
                {
                    return i;
                }
            }

            return -1;
        }

        private void DisposeRebindOperation()
        {
            if (rebindOperation == null)
            {
                return;
            }

            rebindOperation.Dispose();
            rebindOperation = null;
        }
#endif

        private void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public bool WasSkillPressedThisFrame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillPressedThisFrame.Length)
            {
                return false;
            }

            return skillPressedThisFrame[slotIndex];
        }

        public bool WasQuickSlotPressedThisFrame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= quickSlotPressedThisFrame.Length)
            {
                return false;
            }

            return quickSlotPressedThisFrame[slotIndex];
        }

        public bool WasMusouPressedThisFrame()
        {
            return MusouPressed;
        }

        public bool WasUnifiedKeyPressedThisFrame(KeyCode key)
        {
            return ReadUnifiedKeyDown(key);
        }

        public bool IsUnifiedKeyHeld(KeyCode key)
        {
            return ReadUnifiedKey(key);
        }

        public float ReadMouseScrollDelta()
        {
            return MouseScrollDelta;
        }

        public float ReadGamepadZoomAxis()
        {
            return GamepadZoomAxis;
        }

        public static bool ReadUnifiedKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (TryReadInputSystemButton(key, out ButtonControl control))
            {
                return control.wasPressedThisFrame;
            }
#endif
            return Input.GetKeyDown(key);
        }

        public static bool ReadUnifiedKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (TryReadInputSystemButton(key, out ButtonControl control))
            {
                return control.isPressed;
            }
#endif
            return Input.GetKey(key);
        }

        public static float ReadUnifiedMouseScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.scroll.ReadValue().y * 0.01f;
            }
#endif
            return Input.GetAxis("Mouse ScrollWheel");
        }

        public static float ReadUnifiedGamepadZoomAxis()
        {
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
            {
                return Gamepad.current.rightTrigger.ReadValue() - Gamepad.current.leftTrigger.ReadValue();
            }
#endif
            return 0f;
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryReadInputSystemButton(KeyCode key, out ButtonControl button)
        {
            button = null;

            if (Mouse.current != null)
            {
                switch (key)
                {
                    case KeyCode.Mouse0:
                        button = Mouse.current.leftButton;
                        return true;
                    case KeyCode.Mouse1:
                        button = Mouse.current.rightButton;
                        return true;
                    case KeyCode.Mouse2:
                        button = Mouse.current.middleButton;
                        return true;
                }
            }

            if (Keyboard.current == null)
            {
                return false;
            }

            if (!TryMapKeyCodeToInputSystemKey(key, out Key mapped))
            {
                return false;
            }

            button = Keyboard.current[mapped];
            return button != null;
        }

        private static bool TryMapKeyCodeToInputSystemKey(KeyCode keyCode, out Key mapped)
        {
            switch (keyCode)
            {
                case KeyCode.A: mapped = Key.A; return true;
                case KeyCode.B: mapped = Key.B; return true;
                case KeyCode.C: mapped = Key.C; return true;
                case KeyCode.D: mapped = Key.D; return true;
                case KeyCode.E: mapped = Key.E; return true;
                case KeyCode.F: mapped = Key.F; return true;
                case KeyCode.G: mapped = Key.G; return true;
                case KeyCode.H: mapped = Key.H; return true;
                case KeyCode.I: mapped = Key.I; return true;
                case KeyCode.J: mapped = Key.J; return true;
                case KeyCode.K: mapped = Key.K; return true;
                case KeyCode.L: mapped = Key.L; return true;
                case KeyCode.M: mapped = Key.M; return true;
                case KeyCode.N: mapped = Key.N; return true;
                case KeyCode.O: mapped = Key.O; return true;
                case KeyCode.P: mapped = Key.P; return true;
                case KeyCode.Q: mapped = Key.Q; return true;
                case KeyCode.R: mapped = Key.R; return true;
                case KeyCode.S: mapped = Key.S; return true;
                case KeyCode.T: mapped = Key.T; return true;
                case KeyCode.U: mapped = Key.U; return true;
                case KeyCode.V: mapped = Key.V; return true;
                case KeyCode.W: mapped = Key.W; return true;
                case KeyCode.X: mapped = Key.X; return true;
                case KeyCode.Y: mapped = Key.Y; return true;
                case KeyCode.Z: mapped = Key.Z; return true;
                case KeyCode.Alpha0: mapped = Key.Digit0; return true;
                case KeyCode.Alpha1: mapped = Key.Digit1; return true;
                case KeyCode.Alpha2: mapped = Key.Digit2; return true;
                case KeyCode.Alpha3: mapped = Key.Digit3; return true;
                case KeyCode.Alpha4: mapped = Key.Digit4; return true;
                case KeyCode.Alpha5: mapped = Key.Digit5; return true;
                case KeyCode.Alpha6: mapped = Key.Digit6; return true;
                case KeyCode.Alpha7: mapped = Key.Digit7; return true;
                case KeyCode.Alpha8: mapped = Key.Digit8; return true;
                case KeyCode.Alpha9: mapped = Key.Digit9; return true;
                case KeyCode.Space: mapped = Key.Space; return true;
                case KeyCode.Tab: mapped = Key.Tab; return true;
                case KeyCode.Return: mapped = Key.Enter; return true;
                case KeyCode.Escape: mapped = Key.Escape; return true;
                case KeyCode.Delete: mapped = Key.Delete; return true;
                case KeyCode.LeftShift: mapped = Key.LeftShift; return true;
                case KeyCode.RightShift: mapped = Key.RightShift; return true;
                case KeyCode.LeftControl: mapped = Key.LeftCtrl; return true;
                case KeyCode.RightControl: mapped = Key.RightCtrl; return true;
                case KeyCode.LeftAlt: mapped = Key.LeftAlt; return true;
                case KeyCode.RightAlt: mapped = Key.RightAlt; return true;
                default:
                    mapped = Key.None;
                    return false;
            }
        }
#endif

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (useInputActions)
            {
                gameplayActionMap?.Disable();
            }

            DisposeRebindOperation();
#endif

            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            DisposeRebindOperation();
            gameplayActionMap?.Disable();
#endif
        }
    }
}
