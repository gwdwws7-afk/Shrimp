using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThirdPersonController.Tests
{
    public class CombatRound1RegressionTests
    {
        private GameObject runtimeRoot;
        private ScriptableObject runtimeSkill;
        private ScriptableObject runtimeSkillSecondary;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            if (runtimeSkill != null)
            {
                Object.DestroyImmediate(runtimeSkill);
                runtimeSkill = null;
            }

            if (runtimeSkillSecondary != null)
            {
                Object.DestroyImmediate(runtimeSkillSecondary);
                runtimeSkillSecondary = null;
            }

            if (runtimeRoot != null)
            {
                Object.DestroyImmediate(runtimeRoot);
                runtimeRoot = null;
            }
        }

        [Test]
        public void PlayerCombat_RegisterHit_IncrementsAndCapsCombo()
        {
            runtimeRoot = new GameObject("Round1_PlayerCombat");
            PlayerCombat combat = runtimeRoot.AddComponent<PlayerCombat>();
            combat.attackSounds = new AudioClip[0];
            combat.maxComboCount = 3;
            combat.berserkThreshold = 2;

            combat.RegisterHit(10);
            Assert.AreEqual(1, combat.CurrentCombo, "First hit should start combo at 1.");
            Assert.IsFalse(combat.IsBerserk, "Berserk should not start before threshold.");

            combat.RegisterHit(10);
            Assert.AreEqual(2, combat.CurrentCombo, "Second hit should continue combo.");
            Assert.IsTrue(combat.IsBerserk, "Combo reaching threshold should trigger berserk.");

            combat.RegisterHit(10);
            combat.RegisterHit(10);
            Assert.AreEqual(3, combat.CurrentCombo, "Combo should cap at configured maxComboCount.");
        }

        [Test]
        public void Stamina_InsufficientConsume_EntersExhaustionAndBlocksFurtherConsumption()
        {
            runtimeRoot = new GameObject("Round1_Stamina");
            StaminaSystem stamina = runtimeRoot.AddComponent<StaminaSystem>();
            stamina.maxStamina = 100f;
            stamina.currentStamina = 10f;
            stamina.heavyAttackCost = 20f;
            stamina.dodgeCost = 5f;

            bool heavySucceeded = stamina.ConsumeHeavyAttack();
            bool dodgeSucceeded = stamina.ConsumeDodge();

            Assert.IsFalse(heavySucceeded, "Heavy attack should fail when stamina is insufficient.");
            Assert.IsFalse(dodgeSucceeded, "Any consume during exhaustion should be blocked.");
            Assert.IsTrue(stamina.isExhausted, "Insufficient consume should enter exhaustion state.");
            Assert.AreEqual(0f, stamina.currentStamina, 0.001f, "Exhaustion should clamp stamina to zero.");
        }

        [UnityTest]
        public IEnumerator SkillManager_TryUseSkill_SuccessConsumesStaminaAndStartsCooldown()
        {
            runtimeRoot = new GameObject("Round1_SkillSuccess");
            StaminaSystem stamina = runtimeRoot.AddComponent<StaminaSystem>();
            PlayerActionController actionController = runtimeRoot.AddComponent<PlayerActionController>();
            SkillManager skillManager = runtimeRoot.AddComponent<SkillManager>();
            skillManager.autoLoadFromResources = false;
            skillManager.skills = new SkillBase[6];

            TestSkill testSkill = CreateTestSkill("Round1_SuccessSkill", staminaCost: 20f, cooldown: 6f, preConsumeDrain: 0f);
            runtimeSkill = testSkill;
            skillManager.skills[0] = testSkill;

            stamina.maxStamina = 100f;
            stamina.currentStamina = 100f;

            yield return null;

            bool used = skillManager.TryUseSkill(0);

            Assert.IsTrue(used, "Skill should cast successfully with enough stamina.");
            Assert.AreEqual(80f, stamina.currentStamina, 0.001f, "Successful cast should consume configured stamina.");
            Assert.IsFalse(testSkill.isReady, "Successful cast should start cooldown.");
            Assert.Greater(testSkill.cooldownTimer, 0f, "Cooldown timer should be greater than zero after cast.");
            Assert.AreEqual(PlayerActionState.Skill, actionController.CurrentState, "Action controller should enter Skill state.");
        }

        [Test]
        public void ActionController_SkillInterruptedByDodge_EmitsInterruptEvent()
        {
            runtimeRoot = new GameObject("Round1_ActionInterrupt");
            PlayerActionController actionController = runtimeRoot.AddComponent<PlayerActionController>();

            int interruptCount = 0;
            PlayerActionState interruptedFrom = PlayerActionState.Locomotion;
            PlayerActionState interruptedTo = PlayerActionState.Locomotion;
            actionController.OnActionInterrupted += (from, to) =>
            {
                interruptCount++;
                interruptedFrom = from;
                interruptedTo = to;
            };

            bool startedSkill = actionController.TryStartAction(
                PlayerActionState.Skill,
                ActionPriority.Skill,
                minDuration: 0.6f,
                lockMove: true,
                lockRot: true,
                autoReturn: true,
                allowInterrupt: true,
                allowedInterrupts: ActionInterruptMask.Dodge);

            bool startedDodge = actionController.TryStartAction(
                PlayerActionState.Dodge,
                ActionPriority.Dodge,
                minDuration: 0.2f,
                lockMove: true,
                lockRot: false,
                autoReturn: true,
                allowInterrupt: true,
                allowedInterrupts: ActionInterruptMask.All);

            Assert.IsTrue(startedSkill, "Setup should enter Skill state.");
            Assert.IsTrue(startedDodge, "Dodge should interrupt Skill when mask allows it.");
            Assert.AreEqual(1, interruptCount, "Interrupt event should fire once.");
            Assert.AreEqual(PlayerActionState.Skill, interruptedFrom, "Interrupt source state should be Skill.");
            Assert.AreEqual(PlayerActionState.Dodge, interruptedTo, "Interrupt target state should be Dodge.");
            Assert.AreEqual(PlayerActionState.Dodge, actionController.CurrentState, "Current state should become Dodge after interrupt.");
        }

        [Test]
        public void DashAttackSkill_HitGate_PreventsRepeatHitOnSameTargetRoot()
        {
            DashAttackSkill dashSkill = ScriptableObject.CreateInstance<DashAttackSkill>();
            runtimeSkill = dashSkill;

            MethodInfo registerMethod = typeof(DashAttackSkill).GetMethod("RegisterDashHitTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo hasMethod = typeof(DashAttackSkill).GetMethod("HasHitTargetThisDash", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(registerMethod, "Expected private register method for dash hit gate.");
            Assert.NotNull(hasMethod, "Expected private contains method for dash hit gate.");

            runtimeRoot = new GameObject("DashTargetRoot");
            GameObject childA = new GameObject("ChildA");
            GameObject childB = new GameObject("ChildB");
            childA.transform.SetParent(runtimeRoot.transform);
            childB.transform.SetParent(runtimeRoot.transform);

            Collider colliderA = childA.AddComponent<BoxCollider>();
            Collider colliderB = childB.AddComponent<BoxCollider>();

            bool before = (bool)hasMethod.Invoke(dashSkill, new object[] { colliderA });
            Assert.IsFalse(before, "Target should not be marked before first hit.");

            registerMethod.Invoke(dashSkill, new object[] { colliderA });

            bool firstSeen = (bool)hasMethod.Invoke(dashSkill, new object[] { colliderA });
            bool siblingSeen = (bool)hasMethod.Invoke(dashSkill, new object[] { colliderB });
            Assert.IsTrue(firstSeen, "Original collider should be marked after first hit.");
            Assert.IsTrue(siblingSeen, "Sibling collider under same target root should be considered already hit.");
        }

        [Test]
        public void PlayerCombat_SkillDamageBuffMultiplier_IsIncludedInMainDamageMultiplier()
        {
            runtimeRoot = new GameObject("Round2_BerserkMultiplier");
            PlayerCombat combat = runtimeRoot.AddComponent<PlayerCombat>();
            combat.attackSounds = new AudioClip[0];
            combat.maxComboCount = 10;
            combat.berserkThreshold = 50;

            // Make combo enter Tier1 so base multiplier is not exactly 1.
            combat.RegisterHit(10);

            MethodInfo getDamageMultiplier = typeof(PlayerCombat).GetMethod("GetDamageMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(getDamageMultiplier, "Expected PlayerCombat.GetDamageMultiplier private method.");

            float baseMultiplier = (float)getDamageMultiplier.Invoke(combat, null);
            combat.SetSkillDamageBuffMultiplier(1.2f);
            float buffedMultiplier = (float)getDamageMultiplier.Invoke(combat, null);
            combat.ClearSkillDamageBuffMultiplier();
            float clearedMultiplier = (float)getDamageMultiplier.Invoke(combat, null);

            Assert.AreEqual(baseMultiplier * 1.2f, buffedMultiplier, 0.001f, "External skill damage buff should multiply main damage chain.");
            Assert.AreEqual(baseMultiplier, clearedMultiplier, 0.001f, "Clearing external skill damage buff should restore baseline multiplier.");
        }

        [Test]
        public void SkillManager_SanitizeLoadedSkillTexts_ReplacesCorruptedNameAndDescription()
        {
            runtimeRoot = new GameObject("Round2_SkillTextSanitize");
            SkillManager skillManager = runtimeRoot.AddComponent<SkillManager>();
            skillManager.autoLoadFromResources = false;
            skillManager.skills = new SkillBase[6];

            TestSkill corruptedSkill = CreateTestSkill("Corrupted�Name", staminaCost: 5f, cooldown: 3f, preConsumeDrain: 0f);
            corruptedSkill.description = "Corrupted�Description";
            runtimeSkill = corruptedSkill;
            skillManager.skills[0] = corruptedSkill;

            MethodInfo sanitizeMethod = typeof(SkillManager).GetMethod("SanitizeLoadedSkillTexts", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(sanitizeMethod, "Expected private sanitize method for skill text cleanup.");
            sanitizeMethod.Invoke(skillManager, null);

            Assert.AreEqual("Skill", corruptedSkill.skillName, "Corrupted skill name should be replaced by fallback text.");
            Assert.AreEqual("Skill effect description.", corruptedSkill.description, "Corrupted description should be replaced by fallback text.");
        }

        [Test]
        public void SkillManager_HandleInput_UsesPlayerInputHandlerSkillState()
        {
            runtimeRoot = new GameObject("Round2_SkillInputBridge");
            StaminaSystem stamina = runtimeRoot.AddComponent<StaminaSystem>();
            PlayerActionController actionController = runtimeRoot.AddComponent<PlayerActionController>();
            PlayerInputHandler inputHandler = runtimeRoot.AddComponent<PlayerInputHandler>();
            SkillManager skillManager = runtimeRoot.AddComponent<SkillManager>();

            skillManager.autoLoadFromResources = false;
            skillManager.skills = new SkillBase[6];
            TestSkill testSkill = CreateTestSkill("Round2_InputBridgeSkill", staminaCost: 10f, cooldown: 4f, preConsumeDrain: 0f);
            runtimeSkill = testSkill;
            skillManager.skills[0] = testSkill;
            skillManager.inputHandler = inputHandler;
            skillManager.actionController = actionController;
            skillManager.staminaSystem = stamina;

            stamina.maxStamina = 100f;
            stamina.currentStamina = 100f;

            FieldInfo pressedField = typeof(PlayerInputHandler).GetField("skillPressedThisFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(pressedField, "Expected internal skill pressed state on PlayerInputHandler.");
            bool[] pressedStates = (bool[])pressedField.GetValue(inputHandler);
            pressedStates[0] = true;

            MethodInfo handleInputMethod = typeof(SkillManager).GetMethod("HandleInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(handleInputMethod, "Expected private input handling method on SkillManager.");
            handleInputMethod.Invoke(skillManager, null);

            Assert.IsFalse(testSkill.isReady, "Skill should be consumed when PlayerInputHandler reports slot press.");
            Assert.Greater(testSkill.cooldownTimer, 0f, "Skill cooldown should start after bridged input cast.");
            Assert.AreEqual(PlayerActionState.Skill, actionController.CurrentState, "Action controller should enter skill state from input bridge.");
        }

        [Test]
        public void SkillManager_ResourceAudit_CountsMissingSkillAssets()
        {
            runtimeRoot = new GameObject("Round2_SkillResourceAudit");
            SkillManager skillManager = runtimeRoot.AddComponent<SkillManager>();
            skillManager.autoLoadFromResources = false;
            skillManager.skills = new SkillBase[6];

            TestSkill missing = CreateTestSkill("MissingResSkill", staminaCost: 5f, cooldown: 3f, preConsumeDrain: 0f);
            runtimeSkill = missing;

            TestSkill complete = CreateTestSkill("CompleteResSkill", staminaCost: 5f, cooldown: 3f, preConsumeDrain: 0f);
            runtimeSkillSecondary = complete;

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            AudioClip clip = AudioClip.Create("dummy", 128, 1, 44100, false);

            complete.icon = sprite;
            complete.castSound = clip;
            complete.effectPrefab = runtimeRoot;

            skillManager.skills[0] = missing;
            skillManager.skills[1] = complete;

            skillManager.RefreshSkillResourceAudit();

            Assert.AreEqual(1, skillManager.MissingAnyResourceSkillCount, "Only one skill should have resource gaps.");
            Assert.AreEqual(1, skillManager.MissingIconSkillCount, "Only missing skill should count toward icon gap.");
            Assert.AreEqual(1, skillManager.MissingAudioSkillCount, "Only missing skill should count toward audio gap.");
            Assert.AreEqual(1, skillManager.MissingFxSkillCount, "Only missing skill should count toward fx gap.");

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(clip);
        }

        [UnityTest]
        public IEnumerator SkillManager_Update_AdvancesCooldownAndRestoresReadyState()
        {
            runtimeRoot = new GameObject("Round2_CooldownUpdate");
            SkillManager skillManager = runtimeRoot.AddComponent<SkillManager>();
            skillManager.autoLoadFromResources = false;
            skillManager.skills = new SkillBase[6];

            TestSkill skill = CreateTestSkill("CooldownTickSkill", staminaCost: 5f, cooldown: 0.05f, preConsumeDrain: 0f);
            runtimeSkill = skill;
            skill.isReady = false;
            skill.cooldownDuration = 0.05f;
            skill.cooldownTimer = 0.03f;
            skillManager.skills[0] = skill;

            yield return null;
            float afterOneFrame = skill.cooldownTimer;
            Assert.Less(afterOneFrame, 0.03f, "SkillManager.Update should tick cooldown each frame.");

            yield return new WaitForSeconds(0.08f);
            Assert.IsTrue(skill.isReady, "Cooldown tick should eventually restore ready state.");
            Assert.AreEqual(0f, skill.cooldownTimer, 0.001f, "Cooldown timer should clamp to zero when ready.");
        }

        [Test]
        public void SkillManager_GetSkillIcon_WhenMissing_ReturnsRuntimeFallback()
        {
            runtimeRoot = new GameObject("Round2_IconFallback");
            SkillManager skillManager = runtimeRoot.AddComponent<SkillManager>();
            skillManager.autoLoadFromResources = false;
            skillManager.skills = new SkillBase[6];

            TestSkill skill = CreateTestSkill("IconFallbackSkill", staminaCost: 5f, cooldown: 3f, preConsumeDrain: 0f);
            runtimeSkill = skill;
            skill.icon = null;
            skillManager.skills[0] = skill;

            Sprite first = skillManager.GetSkillIcon(0);
            Sprite second = skillManager.GetSkillIcon(0);

            Assert.NotNull(first, "SkillManager should provide runtime fallback icon when asset icon is missing.");
            Assert.AreSame(first, second, "Fallback icon should be reused instead of recreating per call.");
            Assert.AreEqual("SkillManagerFallbackIcon", first.name);
        }

        [Test]
        public void SkillBase_ResolveClipForPlayback_UsesFallbackWhenAudioMissing()
        {
            TestSkill skill = CreateTestSkill("AudioFallbackSkill", staminaCost: 5f, cooldown: 3f, preConsumeDrain: 0f);
            runtimeSkill = skill;

            MethodInfo resolveMethod = typeof(SkillBase).GetMethod("ResolveClipForPlayback", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(resolveMethod, "Expected internal clip resolver for audio fallback.");

            System.Type cueType = resolveMethod.GetParameters()[1].ParameterType;
            object castCue = System.Enum.ToObject(cueType, 0);

            skill.useFallbackAudioWhenMissing = true;
            AudioClip fallbackClip = (AudioClip)resolveMethod.Invoke(skill, new object[] { null, castCue });
            Assert.NotNull(fallbackClip, "Missing audio should resolve to generated fallback clip when enabled.");

            skill.useFallbackAudioWhenMissing = false;
            AudioClip disabledClip = (AudioClip)resolveMethod.Invoke(skill, new object[] { null, castCue });
            Assert.IsNull(disabledClip, "Fallback audio should not be generated when feature is disabled.");
        }

        [Test]
        public void DashAttackSkill_OnInterrupted_ClearsHitGateState()
        {
            DashAttackSkill dashSkill = ScriptableObject.CreateInstance<DashAttackSkill>();
            runtimeSkill = dashSkill;

            runtimeRoot = new GameObject("Round2_DashInterrupt");
            Collider collider = runtimeRoot.AddComponent<BoxCollider>();

            MethodInfo registerMethod = typeof(DashAttackSkill).GetMethod("RegisterDashHitTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo hasMethod = typeof(DashAttackSkill).GetMethod("HasHitTargetThisDash", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(registerMethod);
            Assert.NotNull(hasMethod);

            registerMethod.Invoke(dashSkill, new object[] { collider });
            bool beforeInterrupt = (bool)hasMethod.Invoke(dashSkill, new object[] { collider });
            Assert.IsTrue(beforeInterrupt, "Target should be marked before interruption.");

            dashSkill.OnInterrupted(runtimeRoot.transform);

            bool afterInterrupt = (bool)hasMethod.Invoke(dashSkill, new object[] { collider });
            Assert.IsFalse(afterInterrupt, "Interruption should clear dash hit gate marks.");
        }

        [Test]
        public void UltimateSkill_OnInterrupted_RestoresTimeScale()
        {
            UltimateSkill ultimateSkill = ScriptableObject.CreateInstance<UltimateSkill>();
            runtimeSkill = ultimateSkill;
            runtimeRoot = new GameObject("Round2_UltimateInterrupt");

            FieldInfo slowMotionFlag = typeof(UltimateSkill).GetField("slowMotionActive", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(slowMotionFlag, "Expected private slow-motion runtime flag.");

            Time.timeScale = 0.3f;
            slowMotionFlag.SetValue(ultimateSkill, true);

            ultimateSkill.OnInterrupted(runtimeRoot.transform);

            bool isSlowMotionActive = (bool)slowMotionFlag.GetValue(ultimateSkill);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "Interrupt should always restore timescale.");
            Assert.IsFalse(isSlowMotionActive, "Interrupt should clear slow-motion runtime flag.");
        }

        [Test]
        public void WhirlwindSkill_OnInterrupted_ClearsRuntimeReferences()
        {
            WhirlwindSkill whirlwindSkill = ScriptableObject.CreateInstance<WhirlwindSkill>();
            runtimeSkill = whirlwindSkill;
            runtimeRoot = new GameObject("Round2_WhirlwindInterrupt");
            DummyRunner runner = runtimeRoot.AddComponent<DummyRunner>();
            Coroutine runningRoutine = runner.StartCoroutine(runner.HoldRoutine());

            FieldInfo runnerField = typeof(WhirlwindSkill).GetField("activeRunner", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo routineField = typeof(WhirlwindSkill).GetField("whirlwindRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(runnerField);
            Assert.NotNull(routineField);

            runnerField.SetValue(whirlwindSkill, runner);
            routineField.SetValue(whirlwindSkill, runningRoutine);

            whirlwindSkill.OnInterrupted(runtimeRoot.transform);

            Assert.IsNull(runnerField.GetValue(whirlwindSkill), "Interrupt should clear cached runner.");
            Assert.IsNull(routineField.GetValue(whirlwindSkill), "Interrupt should clear cached whirlwind routine.");
        }

        [Test]
        public void BerserkSkill_OnEnable_AssignsBurstCategoryAndDefenseDuration()
        {
            BerserkSkill berserkSkill = ScriptableObject.CreateInstance<BerserkSkill>();
            runtimeSkill = berserkSkill;

            Assert.AreEqual(SkillCategory.Burst, berserkSkill.category, "Berserk should default to Burst category.");
            Assert.Greater(berserkSkill.damageReductionDuration, 0f, "Berserk should initialize damage reduction duration.");
        }

        [Test]
        public void ShockwaveSkill_OnEnable_AssignsCrowdControlCategory()
        {
            ShockwaveSkill shockwaveSkill = ScriptableObject.CreateInstance<ShockwaveSkill>();
            runtimeSkill = shockwaveSkill;

            Assert.AreEqual(SkillCategory.CrowdControl, shockwaveSkill.category, "Shockwave should default to CrowdControl category.");
        }

        [Test]
        public void PullSkill_OnEnable_AssignsGatherCategory()
        {
            PullSkill pullSkill = ScriptableObject.CreateInstance<PullSkill>();
            runtimeSkill = pullSkill;

            Assert.AreEqual(SkillCategory.Gather, pullSkill.category, "Pull should default to Gather category.");
        }

        [UnityTest]
        public IEnumerator SkillTimeline_FallbackInvokesImpactAndRecoveryOnce_WithCoarseFrameWait()
        {
            runtimeRoot = new GameObject("Round1_Timeline");
            SkillTimelineController timeline = runtimeRoot.AddComponent<SkillTimelineController>();

            int impactCount = 0;
            int recoveryCount = 0;
            int endedCount = 0;
            timeline.OnTimelineEnded += () => endedCount++;

            timeline.BeginTimeline(
                impactDelay: 0.02f,
                recoveryDelay: 0.02f,
                impactAction: () => impactCount++,
                recoveryAction: () => recoveryCount++);

            // Wait longer than impact + recovery to emulate low-FPS coarse stepping.
            yield return new WaitForSeconds(0.12f);

            Assert.AreEqual(1, impactCount, "Impact callback should trigger exactly once.");
            Assert.AreEqual(1, recoveryCount, "Recovery callback should trigger exactly once.");
            Assert.AreEqual(1, endedCount, "Timeline end event should trigger exactly once.");
            Assert.IsFalse(timeline.IsActive, "Timeline should be inactive after fallback completion.");
        }

        private static TestSkill CreateTestSkill(string name, float staminaCost, float cooldown, float preConsumeDrain)
        {
            TestSkill skill = ScriptableObject.CreateInstance<TestSkill>();
            skill.skillName = name;
            skill.staminaCost = staminaCost;
            skill.cooldown = cooldown;
            skill.castDuration = 0.25f;
            skill.impactDelay = 0f;
            skill.recoveryDelay = 0f;
            skill.useAnimationEvents = false;
            skill.preConsumeDrain = preConsumeDrain;
            skill.isReady = true;
            skill.cooldownTimer = 0f;
            skill.cooldownDuration = 0f;
            return skill;
        }

        private class TestSkill : SkillBase
        {
            public float preConsumeDrain;

            public override void Execute(Transform caster, Vector3 targetPosition)
            {
                if (preConsumeDrain <= 0f || caster == null)
                {
                    return;
                }

                StaminaSystem stamina = caster.GetComponent<StaminaSystem>();
                if (stamina != null)
                {
                    stamina.ConsumeStamina(preConsumeDrain);
                }
            }
        }

        private class DummyRunner : MonoBehaviour
        {
            public IEnumerator HoldRoutine()
            {
                while (true)
                {
                    yield return null;
                }
            }
        }
    }
}
