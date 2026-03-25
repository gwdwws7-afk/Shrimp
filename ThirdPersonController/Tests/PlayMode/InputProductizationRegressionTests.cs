using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class InputProductizationRegressionTests
    {
        [Test]
        public void PlayerInputHandler_SystemToggleActions_HaveGamepadHoldBindings()
        {
            GameObject go = new GameObject("Input_Productization");
            try
            {
                PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
                object map = GetGameplayMapOrIgnore(input);
                if (map == null)
                {
                    return;
                }

                AssertActionHasBinding(map, "ToggleEconomy", "<Gamepad>/select", expectHoldInteraction: true);
                AssertActionHasBinding(map, "ToggleTalent", "<Gamepad>/start", expectHoldInteraction: true);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerInputHandler_ActionLabel_ContainsKeyboardAndGamepadForSystemToggles()
        {
            GameObject go = new GameObject("Input_Label");
            try
            {
                PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
                object map = GetGameplayMapOrIgnore(input);
                if (map == null)
                {
                    return;
                }

                string economy = input.GetActionBindingLabel("ToggleEconomy", KeyCode.Y, includeGamepad: true);
                string talent = input.GetActionBindingLabel("ToggleTalent", KeyCode.U, includeGamepad: true);

                Assert.IsFalse(string.IsNullOrEmpty(economy), "Economy binding label should not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(talent), "Talent binding label should not be empty.");
                Assert.IsTrue(economy.Contains("/"), "Economy label should include keyboard and gamepad display.");
                Assert.IsTrue(talent.Contains("/"), "Talent label should include keyboard and gamepad display.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerInputHandler_CoreCombatActions_HaveGamepadBindings()
        {
            GameObject go = new GameObject("Input_GamepadCoverage");
            try
            {
                PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
                object map = GetGameplayMapOrIgnore(input);
                if (map == null)
                {
                    return;
                }

                string[] requiredGamepadActions =
                {
                    "Move", "Look", "Jump", "Sprint", "Crouch",
                    "Attack", "HeavyAttack", "Interact", "Block", "Dodge", "Musou",
                    "Skill1", "Skill2", "Skill3", "Skill4", "Skill5", "Skill6",
                    "MenuConfirm", "MenuRetry", "MenuCancel",
                    "ToggleEconomy", "ToggleTalent"
                };

                for (int i = 0; i < requiredGamepadActions.Length; i++)
                {
                    string actionName = requiredGamepadActions[i];
                    Assert.IsTrue(
                        input.ActionHasGamepadBinding(actionName),
                        $"Expected gamepad binding for action: {actionName}");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerInputHandler_KeyboardAndMouseCoverage_RemainsComplete()
        {
            GameObject go = new GameObject("Input_DeviceCoverage");
            try
            {
                PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
                object map = GetGameplayMapOrIgnore(input);
                if (map == null)
                {
                    return;
                }

                string[] requiredKeyboardActions =
                {
                    "Move", "Jump", "Sprint", "Crouch",
                    "Interact", "Dodge", "Musou",
                    "Skill1", "Skill2", "Skill3", "Skill4", "Skill5", "Skill6",
                    "QuickSlot1", "QuickSlot2", "QuickSlot3",
                    "MenuConfirm", "MenuRetry", "MenuCancel", "QuitMenu",
                    "ToggleEconomy", "ToggleTalent", "ToggleHints"
                };

                for (int i = 0; i < requiredKeyboardActions.Length; i++)
                {
                    string actionName = requiredKeyboardActions[i];
                    Assert.IsTrue(
                        input.ActionHasKeyboardBinding(actionName),
                        $"Expected keyboard binding for action: {actionName}");
                }

                Assert.IsTrue(input.ActionHasMouseBinding("Look"), "Look should keep mouse binding.");
                Assert.IsTrue(input.ActionHasMouseBinding("Attack"), "Attack should keep mouse binding.");
                Assert.IsTrue(input.ActionHasMouseBinding("HeavyAttack"), "HeavyAttack should keep mouse binding.");
                Assert.IsTrue(input.ActionHasMouseBinding("Block"), "Block should keep mouse binding.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerInputHandler_ActionLabels_SkillAndAttackHints_AreReadable()
        {
            GameObject go = new GameObject("Input_LabelCoverage");
            try
            {
                PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
                object map = GetGameplayMapOrIgnore(input);
                if (map == null)
                {
                    return;
                }

                KeyCode[] skillFallbacks =
                {
                    KeyCode.Q,
                    KeyCode.W,
                    KeyCode.C,
                    KeyCode.R,
                    KeyCode.T,
                    KeyCode.F
                };

                for (int i = 0; i < skillFallbacks.Length; i++)
                {
                    string actionName = $"Skill{i + 1}";
                    string label = input.GetActionBindingLabel(actionName, skillFallbacks[i], includeGamepad: false);
                    Assert.IsFalse(string.IsNullOrEmpty(label), $"Skill binding label should not be empty: {actionName}");
                }

                string attackLabel = input.GetActionBindingLabel("Attack", KeyCode.Mouse0, includeGamepad: true);
                string heavyLabel = input.GetActionBindingLabel("HeavyAttack", KeyCode.Mouse1, includeGamepad: true);
                string toggleHintsLabel = input.GetActionBindingLabel("ToggleHints", KeyCode.H, includeGamepad: false);

                Assert.IsFalse(string.IsNullOrEmpty(attackLabel), "Attack binding label should not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(heavyLabel), "HeavyAttack binding label should not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(toggleHintsLabel), "ToggleHints binding label should not be empty.");
                Assert.IsTrue(attackLabel.Contains("/"), "Attack label should expose keyboard/mouse and gamepad display.");
                Assert.IsTrue(heavyLabel.Contains("/"), "HeavyAttack label should expose keyboard/mouse and gamepad display.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerInputHandler_PromptDevice_EventFiresOnSwitch()
        {
            GameObject go = new GameObject("Input_PromptDeviceEvent");
            try
            {
                PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
                int eventCount = 0;
                InputPromptDevice lastDevice = InputPromptDevice.Unknown;
                input.OnPromptDeviceChanged += device =>
                {
                    eventCount++;
                    lastDevice = device;
                };

                input.ForcePromptDeviceForTests(InputPromptDevice.Gamepad);
                input.ForcePromptDeviceForTests(InputPromptDevice.KeyboardMouse);

                Assert.GreaterOrEqual(eventCount, 2, "Prompt device switch should emit event for subscribers.");
                Assert.AreEqual(InputPromptDevice.KeyboardMouse, lastDevice);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static object GetGameplayMapOrIgnore(PlayerInputHandler input)
        {
            FieldInfo field = typeof(PlayerInputHandler).GetField("gameplayActionMap", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Assert.Ignore("InputActionMap backend is not compiled in this build.");
                return null;
            }

            object map = field.GetValue(input);
            if (map == null)
            {
                Assert.Ignore("Gameplay input map unavailable at runtime.");
                return null;
            }

            return map;
        }

        private static void AssertActionHasBinding(
            object map,
            string actionName,
            string expectedPath,
            bool expectHoldInteraction)
        {
            object action = FindAction(map, actionName);
            Assert.NotNull(action, $"Action should exist: {actionName}");

            object bindings = action.GetType().GetProperty("bindings")?.GetValue(action);
            Assert.NotNull(bindings, $"Action {actionName} should expose bindings.");

            PropertyInfo countProp = bindings.GetType().GetProperty("Count");
            PropertyInfo itemProp = bindings.GetType().GetProperty("Item");
            Assert.NotNull(countProp);
            Assert.NotNull(itemProp);

            int count = (int)countProp.GetValue(bindings);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                object binding = itemProp.GetValue(bindings, new object[] { i });
                if (binding == null)
                {
                    continue;
                }

                string path = binding.GetType().GetProperty("path")?.GetValue(binding) as string;
                if (!string.Equals(path, expectedPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (expectHoldInteraction)
                {
                    string interactions = binding.GetType().GetProperty("interactions")?.GetValue(binding) as string;
                    Assert.IsTrue(
                        !string.IsNullOrEmpty(interactions)
                        && interactions.IndexOf("Hold", System.StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Binding {actionName} {expectedPath} should use Hold interaction.");
                }

                found = true;
                break;
            }

            Assert.IsTrue(found, $"Expected binding not found: {actionName} -> {expectedPath}");
        }

        private static object FindAction(object map, string actionName)
        {
            MethodInfo find = map.GetType().GetMethod("FindAction", new[] { typeof(string), typeof(bool) });
            Assert.NotNull(find, "InputActionMap.FindAction should exist.");
            return find.Invoke(map, new object[] { actionName, false });
        }
    }
}
