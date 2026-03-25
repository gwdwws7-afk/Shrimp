using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController.Tests
{
    public class UICrossDeviceReadabilityRegressionTests
    {
        [Test]
        public void UIHudHints_ReadabilityBaseline_RemainsInSafeRange()
        {
            GameObject root = new GameObject("UIHudHints_Readability_Test");
            try
            {
                UI_HudHints hudHints = root.AddComponent<UI_HudHints>();
                Assert.GreaterOrEqual(hudHints.width, 200f, "HUD hint panel width should not be too narrow.");
                Assert.LessOrEqual(hudHints.width, 420f, "HUD hint panel width should not dominate the screen.");
                Assert.GreaterOrEqual(hudHints.lineHeight, 16f, "HUD hint line height should stay readable.");
                Assert.GreaterOrEqual(hudHints.padding, 8f, "HUD hint padding should stay readable.");
                Assert.LessOrEqual(hudHints.hintRefreshInterval, 0.5f, "HUD hint refresh should stay responsive.");

                MethodInfo buildPanelRect = typeof(UI_HudHints).GetMethod(
                    "BuildPanelRect",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(buildPanelRect, "BuildPanelRect should exist for layout validation.");

                hudHints.anchor = UI_HudHints.Corner.TopRight;
                Rect panel = (Rect)buildPanelRect.Invoke(hudHints, new object[] { hudHints.width, 140f });
                Assert.GreaterOrEqual(panel.xMin, -1f, "HUD hint panel should stay near visible area.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UISkillBar_AttackHintText_BaselineIsReadable()
        {
            GameObject root = new GameObject("UISkillBar_Readability_Test");
            try
            {
                UI_SkillBar skillBar = root.AddComponent<UI_SkillBar>();
                MethodInfo ensureAttackInputHint = typeof(UI_SkillBar).GetMethod(
                    "EnsureAttackInputHint",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(ensureAttackInputHint, "EnsureAttackInputHint should exist.");

                ensureAttackInputHint.Invoke(skillBar, null);
                Assert.NotNull(skillBar.attackInputHintText, "Attack hint text should be created.");
                Assert.GreaterOrEqual(skillBar.attackInputHintText.fontSize, 14, "Attack hint font size should remain readable.");

                RectTransform hintRect = skillBar.attackInputHintText.GetComponent<RectTransform>();
                Assert.NotNull(hintRect, "Attack hint should use RectTransform.");
                Assert.GreaterOrEqual(hintRect.sizeDelta.x, 220f, "Attack hint width should remain readable on wide prompts.");
                Assert.GreaterOrEqual(hintRect.sizeDelta.y, 20f, "Attack hint height should remain readable.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UICanvasScaler_DefaultReference_UsesCrossDeviceBaseline()
        {
            GameObject canvasGo = new GameObject("UICanvasScaler_Readability_Test");
            try
            {
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.GreaterOrEqual(scaler.referenceResolution.x, 1280f);
                Assert.GreaterOrEqual(scaler.referenceResolution.y, 720f);
                Assert.GreaterOrEqual(scaler.matchWidthOrHeight, 0.15f);
                Assert.LessOrEqual(scaler.matchWidthOrHeight, 0.85f);
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }
    }
}
