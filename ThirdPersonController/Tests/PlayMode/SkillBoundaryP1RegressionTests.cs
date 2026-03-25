using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class SkillBoundaryP1RegressionTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        private sealed class TestSkill : SkillBase
        {
            public int executeCount;

            public override void Execute(Transform caster, Vector3 targetPosition)
            {
                executeCount++;
            }
        }

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
            Time.timeScale = 1f;
        }

        [Test]
        public void SkillBase_CooldownLifecycle_TransitionsToReady()
        {
            TestSkill skill = ScriptableObject.CreateInstance<TestSkill>();
            createdObjects.Add(skill);
            skill.cooldown = 5f;
            skill.isReady = true;

            skill.StartCooldown(null);
            Assert.IsFalse(skill.isReady, "StartCooldown should mark skill as not ready.");
            Assert.AreEqual(5f, skill.cooldownTimer, 0.0001f);
            Assert.AreEqual(5f, skill.cooldownDuration, 0.0001f);
            Assert.AreEqual(1f, skill.GetCooldownProgress(), 0.0001f);

            skill.UpdateCooldown(2f);
            Assert.IsFalse(skill.isReady);
            Assert.AreEqual(3f, skill.cooldownTimer, 0.0001f);

            skill.UpdateCooldown(3.5f);
            Assert.IsTrue(skill.isReady, "Cooldown should complete after enough delta time.");
            Assert.AreEqual(0f, skill.cooldownTimer, 0.0001f);
            Assert.AreEqual(0f, skill.GetCooldownProgress(), 0.0001f);
        }

        [Test]
        public void SkillManager_TryUseSkill_InvalidOrEmptySlot_ReturnsFalse()
        {
            GameObject go = new GameObject("SkillBoundary_Manager_InvalidSlot");
            createdObjects.Add(go);

            SkillManager manager = go.AddComponent<SkillManager>();
            manager.autoLoadFromResources = false;
            manager.playerTransform = go.transform;
            manager.skills = new SkillBase[2];

            Assert.IsFalse(manager.TryUseSkill(-1), "Negative slot should be rejected.");
            Assert.IsFalse(manager.TryUseSkill(2), "Out-of-range slot should be rejected.");
            Assert.IsFalse(manager.TryUseSkill(0), "Empty skill slot should be rejected.");
        }

        [Test]
        public void SkillManager_TryUseSkill_ExecutesAndStartsCooldown()
        {
            GameObject go = new GameObject("SkillBoundary_Manager_Success");
            createdObjects.Add(go);

            SkillManager manager = go.AddComponent<SkillManager>();
            manager.autoLoadFromResources = false;
            manager.playerTransform = go.transform;

            TestSkill skill = ScriptableObject.CreateInstance<TestSkill>();
            createdObjects.Add(skill);
            skill.skillName = "P1_TestSkill";
            skill.cooldown = 2f;
            skill.staminaCost = 0f;
            skill.castDuration = 0f;
            skill.impactDelay = 0f;
            skill.recoveryDelay = 0f;
            skill.isReady = true;

            manager.skills = new SkillBase[1];
            manager.skills[0] = skill;

            bool firstCast = manager.TryUseSkill(0);
            Assert.IsTrue(firstCast, "Ready skill should cast successfully.");
            Assert.AreEqual(1, skill.executeCount, "Execute should run exactly once on successful cast.");
            Assert.IsFalse(skill.isReady, "Successful cast should start cooldown.");
            Assert.Greater(skill.cooldownTimer, 0f);

            bool secondCast = manager.TryUseSkill(0);
            Assert.IsFalse(secondCast, "Skill should be blocked while cooldown is active.");
            Assert.AreEqual(1, skill.executeCount, "Cooldown gate should prevent duplicate executes.");
        }

        [Test]
        public void DashAttackSkill_OnInterrupted_RestoresMovementAndClearsDashHitGate()
        {
            DashAttackSkill skill = ScriptableObject.CreateInstance<DashAttackSkill>();
            createdObjects.Add(skill);

            GameObject caster = new GameObject("SkillBoundary_DashCaster");
            createdObjects.Add(caster);
            caster.AddComponent<Rigidbody>();
            caster.AddComponent<PlayerInputHandler>();
            PlayerMovement movement = caster.AddComponent<PlayerMovement>();
            movement.enabled = false;

            SetPrivateField(skill, "cachedMovement", movement);
            HashSet<int> dashHitIds = GetPrivateField<HashSet<int>>(skill, "dashHitTargetIds");
            dashHitIds.Add(1001);
            dashHitIds.Add(1002);

            skill.OnInterrupted(caster.transform);

            Assert.IsTrue(movement.enabled, "Dash interrupt should restore PlayerMovement component.");
            Assert.AreEqual(0, dashHitIds.Count, "Dash hit gate should reset on interrupt.");
            Assert.IsNull(GetPrivateField<object>(skill, "cachedMovement"));
        }

        [Test]
        public void UltimateSkill_OnInterrupted_RestoresTimeScale()
        {
            UltimateSkill skill = ScriptableObject.CreateInstance<UltimateSkill>();
            createdObjects.Add(skill);

            GameObject caster = new GameObject("SkillBoundary_UltimateCaster");
            createdObjects.Add(caster);

            Time.timeScale = 0.25f;
            SetPrivateField(skill, "slowMotionActive", true);

            skill.OnInterrupted(caster.transform);

            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "Interrupted ultimate should always restore time scale.");
            Assert.IsFalse(GetPrivateField<bool>(skill, "slowMotionActive"));
        }

        [Test]
        public void Skills_DefaultCategory_AssignedWhenUnset()
        {
            DashAttackSkill dash = ScriptableObject.CreateInstance<DashAttackSkill>();
            ShockwaveSkill shockwave = ScriptableObject.CreateInstance<ShockwaveSkill>();
            PullSkill pull = ScriptableObject.CreateInstance<PullSkill>();
            WhirlwindSkill whirlwind = ScriptableObject.CreateInstance<WhirlwindSkill>();
            BerserkSkill berserk = ScriptableObject.CreateInstance<BerserkSkill>();
            UltimateSkill ultimate = ScriptableObject.CreateInstance<UltimateSkill>();
            createdObjects.Add(dash);
            createdObjects.Add(shockwave);
            createdObjects.Add(pull);
            createdObjects.Add(whirlwind);
            createdObjects.Add(berserk);
            createdObjects.Add(ultimate);

            Assert.AreEqual(SkillCategory.Mobility, dash.category);
            Assert.AreEqual(SkillCategory.CrowdControl, shockwave.category);
            Assert.AreEqual(SkillCategory.Gather, pull.category);
            Assert.AreEqual(SkillCategory.Burst, whirlwind.category);
            Assert.AreEqual(SkillCategory.Burst, berserk.category);
            Assert.AreEqual(SkillCategory.Burst, ultimate.category);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} should exist.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} should exist.");
            return (T)field.GetValue(target);
        }
    }
}
