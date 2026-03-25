using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class InputHintUiRefreshRegressionTests
    {
        [Test]
        public void UISkillBar_PromptDeviceSwitch_TriggersImmediateHintRefresh()
        {
            GameObject inputGo = new GameObject("Input_ForSkillBarRefresh");
            GameObject uiGo = new GameObject("UI_SkillBar_Refresh");
            try
            {
                PlayerInputHandler input = inputGo.AddComponent<PlayerInputHandler>();

                UI_SkillBar skillBar = uiGo.AddComponent<UI_SkillBar>();
                skillBar.enabled = false;
                skillBar.inputHandler = input;
                skillBar.enabled = true;

                float before = skillBar.LastInputHintRefreshUnscaledTime;
                input.ForcePromptDeviceForTests(InputPromptDevice.Gamepad);
                float after = skillBar.LastInputHintRefreshUnscaledTime;

                Assert.Greater(after, before, "Prompt switch should trigger immediate skill-hint refresh.");
            }
            finally
            {
                Object.DestroyImmediate(uiGo);
                Object.DestroyImmediate(inputGo);
            }
        }
    }
}
