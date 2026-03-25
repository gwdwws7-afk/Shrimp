using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class InputHintConsistencyP0RegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object obj = createdObjects[i];
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void MainMenuController_P0Subtitle_UsesUnifiedActionBindingLabel()
        {
            GameObject root = new GameObject("InputHintP0_MainMenu");
            createdObjects.Add(root);

            PlayerInputHandler input = root.AddComponent<PlayerInputHandler>();
            MainMenuController menu = root.AddComponent<MainMenuController>();
            menu.inputHandler = input;
            menu.startActionName = "MenuConfirm";
            menu.startKey = KeyCode.Return;
            menu.subtitleText = "Press Enter to Start";

            string subtitle = InvokePrivateString(menu, "GetSubtitleText");
            string expectedLabel = input.GetActionBindingLabel(menu.startActionName, menu.startKey);
            if (string.IsNullOrEmpty(expectedLabel))
            {
                expectedLabel = PlayerInputHandler.GetFriendlyKeyLabel(menu.startKey);
            }

            Assert.IsFalse(string.IsNullOrEmpty(subtitle));
            Assert.IsTrue(subtitle.Contains(expectedLabel), $"Subtitle should include unified action label '{expectedLabel}'.");
        }

        [Test]
        public void UIHudHints_P0DisplayHints_UseSameActionLabelsAsInputLayer()
        {
            GameObject root = new GameObject("InputHintP0_HudHints");
            createdObjects.Add(root);

            PlayerInputHandler input = root.AddComponent<PlayerInputHandler>();
            UI_HudHints hudHints = root.AddComponent<UI_HudHints>();
            hudHints.inputHandler = input;
            hudHints.useDynamicInputHints = true;

            List<string> hints = InvokePrivateList(hudHints, "BuildDisplayHints");
            Assert.NotNull(hints);
            Assert.GreaterOrEqual(hints.Count, 3, "Dynamic HUD hints should include core economy/talent/quick-slot lines.");

            string economy = ResolveExpectedActionLabel(input, "ToggleEconomy", KeyCode.Y, includeGamepad: true);
            string talent = ResolveExpectedActionLabel(input, "ToggleTalent", KeyCode.U, includeGamepad: true);
            string slot1 = ResolveExpectedActionLabel(input, "QuickSlot1", KeyCode.Alpha1, includeGamepad: false);

            Assert.IsTrue(ContainsLabel(hints, economy), $"HUD hints should include economy label '{economy}'.");
            Assert.IsTrue(ContainsLabel(hints, talent), $"HUD hints should include talent label '{talent}'.");
            Assert.IsTrue(ContainsLabel(hints, slot1), $"HUD hints should include quick slot label '{slot1}'.");
        }

        [Test]
        public void UISkillBar_P0BindingLabels_StayConsistentWithInputLayer()
        {
            GameObject root = new GameObject("InputHintP0_SkillBar");
            createdObjects.Add(root);

            PlayerInputHandler input = root.AddComponent<PlayerInputHandler>();
            UI_SkillBar skillBar = root.AddComponent<UI_SkillBar>();
            skillBar.inputHandler = input;
            skillBar.useDynamicInputHints = true;
            skillBar.includeGamepadOnSkillHints = false;
            skillBar.includeGamepadOnAttackHint = true;

            MethodInfo resolveSkillBinding = typeof(UI_SkillBar).GetMethod("ResolveSkillBindingLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo buildAttackHint = typeof(UI_SkillBar).GetMethod("BuildAttackInputHintLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(resolveSkillBinding);
            Assert.NotNull(buildAttackHint);

            for (int i = 0; i < 6; i++)
            {
                string actionName = $"Skill{i + 1}";
                KeyCode fallback = GetDefaultSkillFallbackKey(i);
                string expected = ResolveExpectedActionLabel(input, actionName, fallback, includeGamepad: false);
                string actual = resolveSkillBinding.Invoke(skillBar, new object[] { input, i }) as string;
                Assert.AreEqual(expected, actual, $"Skill slot {i} label should match unified input label.");
            }

            string attackHint = buildAttackHint.Invoke(skillBar, new object[] { input }) as string;
            string light = ResolveExpectedActionLabel(input, "Attack", KeyCode.Mouse0, includeGamepad: true);
            string heavy = ResolveExpectedActionLabel(input, "HeavyAttack", KeyCode.Mouse1, includeGamepad: true);

            Assert.IsFalse(string.IsNullOrEmpty(attackHint));
            Assert.IsTrue(attackHint.Contains(light), $"Attack hint should include light attack label '{light}'.");
            Assert.IsTrue(attackHint.Contains(heavy), $"Attack hint should include heavy attack label '{heavy}'.");
        }

        private static bool ContainsLabel(List<string> hints, string label)
        {
            if (hints == null || string.IsNullOrEmpty(label))
            {
                return false;
            }

            for (int i = 0; i < hints.Count; i++)
            {
                if (!string.IsNullOrEmpty(hints[i]) && hints[i].Contains(label))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveExpectedActionLabel(PlayerInputHandler input, string actionName, KeyCode fallback, bool includeGamepad)
        {
            string label = input.GetActionBindingLabel(actionName, fallback, includeGamepad);
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }

            return PlayerInputHandler.GetFriendlyKeyLabel(fallback);
        }

        private static string InvokePrivateString(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            return method.Invoke(target, null) as string;
        }

        private static List<string> InvokePrivateList(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} should exist.");
            return method.Invoke(target, null) as List<string>;
        }

        private static KeyCode GetDefaultSkillFallbackKey(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return KeyCode.Q;
                case 1: return KeyCode.W;
                case 2: return KeyCode.C;
                case 3: return KeyCode.R;
                case 4: return KeyCode.T;
                case 5: return KeyCode.F;
                default: return KeyCode.None;
            }
        }
    }
}
