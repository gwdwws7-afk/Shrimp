using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能管理器：处理输入、释放、冷却与装备槽位。
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [Header("设置")]
        public SkillBase[] skills = new SkillBase[6]; // 运行时配置项，用于驱动模块行为并保持可调性。

        [Header("自动加载")]
        public bool autoLoadFromResources = true;
        public SkillLoadoutConfig loadoutConfig;
        public string defaultLoadoutResourcePath = "Skills/DefaultSkillLoadout";
        public string resourcesFolder = "Skills";
        
        [Header("按键绑定")]
        public KeyCode[] skillKeys = new KeyCode[6] 
        { 
            KeyCode.Q, KeyCode.W, KeyCode.E, 
            KeyCode.R, KeyCode.T, KeyCode.F 
        };

        [Header("Input Buffer")]
        public float skillBufferTime = 0.3f;
        
        [Header("设置")]
        public Transform playerTransform;
        public StaminaSystem staminaSystem;
        public PlayerInputHandler inputHandler;
        public PlayerActionController actionController;
        public PlayerInputBuffer inputBuffer;
        public SkillTimelineController timelineController;
        
        private SkillBase activeSkill;
        private Transform activeCaster;
        private bool startupLogged;
        private bool resourceAuditLogged;

        // 技能释放原点（默认在角色前上方）
        public Vector3 CastOrigin
        {
            get
            {
                Transform origin = playerTransform != null ? playerTransform : transform;
                if (origin == null)
                {
                    return Vector3.zero;
                }

                return origin.position + origin.forward * 0.5f + Vector3.up * 1f;
            }
        }
        
        private void Awake()
        {
            EnsureRuntimeReferences();

            if (autoLoadFromResources)
            {
                AutoLoadSkills();
            }
        }

        private void OnEnable()
        {
            EnsureRuntimeReferences();

            if (actionController != null)
            {
                actionController.OnActionInterrupted += HandleActionInterrupted;
            }

            if (timelineController != null)
            {
                timelineController.OnTimelineEnded += HandleTimelineEnded;
            }

            LogStartupStatus();
        }

        private void OnDisable()
        {
            if (actionController != null)
            {
                actionController.OnActionInterrupted -= HandleActionInterrupted;
            }

            if (timelineController != null)
            {
                timelineController.OnTimelineEnded -= HandleTimelineEnded;
            }
        }
        
        private void Update()
        {
            EnsureRuntimeReferences();

            // 每帧推进所有技能冷却
            
            // 处理技能输入与输入缓冲
            HandleInput();
        }

        private void AutoLoadSkills()
        {
            if (skills == null || skills.Length == 0)
            {
                skills = new SkillBase[6];
            }

            SkillLoadoutConfig config = loadoutConfig;
            if (config == null && !string.IsNullOrEmpty(defaultLoadoutResourcePath))
            {
                config = Resources.Load<SkillLoadoutConfig>(defaultLoadoutResourcePath);
            }

            string folder = config != null && !string.IsNullOrEmpty(config.resourcesFolder)
                ? config.resourcesFolder
                : resourcesFolder;
            string[] names = config != null ? config.skillResourceNames : null;

            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                {
                    continue;
                }

                string resourceName = names != null && names.Length > i
                    ? names[i]
                    : string.Empty;

                if (string.IsNullOrEmpty(resourceName))
                {
                    continue;
                }

                string path = string.IsNullOrEmpty(folder)
                    ? resourceName
                    : $"{folder}/{resourceName}";
                SkillBase loaded = Resources.Load<SkillBase>(path);
                if (loaded != null)
                {
                    skills[i] = loaded;
                }
                else
                {
                    Debug.LogWarning($"[SkillManager] Missing skill asset at Resources/{path}");
                }
            }

            SanitizeLoadedSkillTexts();
        }
        
        /// <summary>
        /// 更新所有技能冷却计时。
        /// </summary>
        private void UpdateAllCooldowns()
        {
            if (skills == null || skills.Length == 0)
            {
                return;
            }

            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                {
                    skills[i].UpdateCooldown(Time.deltaTime);
                }
            }
        }
        
        /// <summary>
        /// 读取按键并尝试释放技能。
        /// </summary>
        private void HandleInput()
        {
            if (skillKeys == null || skillKeys.Length == 0)
            {
                return;
            }

            int slotCount = skills != null && skills.Length > 0
                ? Mathf.Min(skillKeys.Length, skills.Length)
                : skillKeys.Length;

            for (int i = 0; i < slotCount; i++)
            {
                if (IsSkillPressedThisFrame(i))
                {
                    if (inputBuffer != null)
                    {
                        inputBuffer.BufferAction(BufferedActionType.Skill, skillBufferTime, i);
                    }
                    else
                    {
                        TryUseSkill(i);
                    }
                }
            }

            if (inputBuffer != null)
            {
                TryConsumeBufferedSkills();
            }
        }

        private bool IsSkillPressedThisFrame(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            if (inputHandler != null)
            {
                if (inputHandler.WasSkillPressedThisFrame(slotIndex))
                {
                    return true;
                }
            }

            if (skillKeys == null || slotIndex >= skillKeys.Length)
            {
                return false;
            }

            return PlayerInputHandler.ReadUnifiedKeyDown(skillKeys[slotIndex]);
        }

        private void TryConsumeBufferedSkills()
        {
            if (inputBuffer == null || skills == null || skills.Length == 0)
            {
                return;
            }

            for (int i = 0; i < skills.Length; i++)
            {
                if (inputBuffer.TryGet(BufferedActionType.Skill, out _, i))
                {
                    if (TryUseSkill(i))
                    {
                        inputBuffer.TryConsume(BufferedActionType.Skill, out _, i);
                    }
                }
            }
        }
        
        /// <summary>
        /// 尝试释放指定槽位技能。
        /// </summary>
        public bool TryUseSkill(int index)
        {
            EnsureRuntimeReferences();

            if (skills == null || skills.Length == 0)
            {
                return false;
            }

            if (index < 0 || index >= skills.Length) return false;
            
            SkillBase skill = skills[index];
            if (skill == null)
            {
                Debug.Log($"[SkillManager] Skill slot {index} is empty.");
                return false;
            }

            if (actionController != null && !actionController.CanStartAction(PlayerActionState.Skill))
            {
                return false;
            }
            
            // 前置校验：冷却、耐力、状态等
            if (!skill.CanExecute(playerTransform, staminaSystem))
            {
                return false;
            }
            
            if (actionController != null)
            {
                ActionInterruptMask interruptMask = skill.interruptible
                    ? (ActionInterruptMask.Dodge | ActionInterruptMask.Block)
                    : ActionInterruptMask.None;
                if (!actionController.TryStartAction(
                    PlayerActionState.Skill,
                    ActionPriority.Skill,
                    skill.GetActionDuration(),
                    skill.lockMovement,
                    skill.lockRotation,
                    true,
                    skill.interruptible,
                    interruptMask))
                {
                    return false;
                }
            }

            // 计算目标点（用于指向型技能）
            Vector3 targetPosition = GetTargetPosition();
            
            SetActiveSkill(skill, playerTransform);

// 围绕 时间线 执行该步骤，用于保证流程状态与后续分支一致。
            skill.ExecuteWithTimeline(playerTransform, targetPosition, timelineController);
            
            // 消耗耐力，失败则回滚动作状态
            if (!skill.ConsumeStamina(staminaSystem, playerTransform))
            {
                if (actionController != null)
                {
                    actionController.EndAction(PlayerActionState.Skill);
                }
                return false;
            }
            
// 启动技能冷却，用于限制触发频率并平衡节奏。
            skill.StartCooldown(playerTransform);
            
            Debug.Log($"[SkillManager] Cast skill: {skill.skillName}");
            
            return true;
        }
        
        /// <summary>
        /// 获取当前技能目标点。
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            // 默认取角色前方固定距离
            Transform origin = playerTransform != null ? playerTransform : transform;
            if (origin == null)
            {
                return Vector3.zero;
            }

            return origin.position + origin.forward * 10f;
        }
        
        /// <summary>
        /// 装备技能到指定槽位。
        /// </summary>
        public void EquipSkill(int slotIndex, SkillBase skill)
        {
            if (skills == null || skills.Length == 0)
            {
                skills = new SkillBase[6];
            }

            if (slotIndex >= 0 && slotIndex < skills.Length)
            {
                skills[slotIndex] = skill;
                string skillName = skill != null ? skill.skillName : "<null>";
                Debug.Log($"[SkillManager] Equipped {skillName} to slot {slotIndex}.");
            }
        }
        
        /// <summary>
        /// 获取Skill Cooldown Progress，集中读取当前状态，减少外部耦合。
        /// </summary>
        public float GetSkillCooldownProgress(int index)
        {
            if (skills == null || index < 0 || index >= skills.Length || skills[index] == null)
                return 0f;
            
            return skills[index].GetCooldownProgress();
        }
        
        /// <summary>
        /// 执行 Is Skill Ready 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public bool IsSkillReady(int index)
        {
            if (skills == null || index < 0 || index >= skills.Length || skills[index] == null)
                return false;
            
            return skills[index].isReady;
        }
        
        /// <summary>
        /// 获取Skill Icon，集中读取当前状态，减少外部耦合。
        /// </summary>
        public Sprite GetSkillIcon(int index)
        {
            if (skills == null || index < 0 || index >= skills.Length || skills[index] == null)
                return null;
            
            return skills[index].icon;
        }
        
        /// <summary>
        /// 重置所有技能冷却（调试/特殊奖励）。
        /// </summary>
        public void ResetAllCooldowns()
        {
            if (skills == null || skills.Length == 0)
            {
                return;
            }

            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    skill.cooldownTimer = 0;
                    skill.isReady = true;
                    skill.cooldownDuration = 0f;
                }
            }
            Debug.Log("[SkillManager] Reset all skill cooldowns.");
        }
        
        /// <summary>
        /// 刷新Skill，减少延迟与状态不同步。
        /// </summary>
        public void RefreshSkill(int index)
        {
            if (skills != null && index >= 0 && index < skills.Length && skills[index] != null)
            {
                skills[index].cooldownTimer = 0;
                skills[index].isReady = true;
                skills[index].cooldownDuration = 0f;
                GameEvents.SkillReady(skills[index].skillName);
            }
        }

        public void NotifySkillEnded(SkillBase skill)
        {
            if (skill == null || activeSkill != skill)
            {
                return;
            }

            ClearActiveSkill();
        }

        private void SetActiveSkill(SkillBase skill, Transform caster)
        {
            activeSkill = skill;
            activeCaster = caster;
        }

        private void ClearActiveSkill()
        {
            activeSkill = null;
            activeCaster = null;
        }

        private void HandleTimelineEnded()
        {
            if (activeSkill == null)
            {
                return;
            }

            if (activeSkill.endsOnRecovery)
            {
                ClearActiveSkill();
            }
        }

        private void HandleActionInterrupted(PlayerActionState fromState, PlayerActionState toState)
        {
            if (fromState != PlayerActionState.Skill)
            {
                return;
            }

            CancelActiveSkill();
        }

        private void CancelActiveSkill()
        {
            if (activeSkill == null)
            {
                if (timelineController != null)
                {
                    timelineController.CancelTimeline(false);
                }
                return;
            }

            if (timelineController != null && timelineController.IsActive)
            {
                timelineController.CancelTimeline(false);
            }

            activeSkill.OnInterrupted(activeCaster);
            ClearActiveSkill();
        }

        private void EnsureRuntimeReferences()
        {
            if (playerTransform == null)
            {
                playerTransform = transform;
            }

            if (staminaSystem == null)
            {
                staminaSystem = GetComponent<StaminaSystem>();
            }

            if (inputHandler == null)
            {
                inputHandler = GetComponent<PlayerInputHandler>();
            }

            if (actionController == null)
            {
                actionController = GetComponent<PlayerActionController>();
            }

            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<PlayerInputBuffer>();
            }

            if (timelineController == null)
            {
                timelineController = GetComponent<SkillTimelineController>();
            }

            if (skills == null || skills.Length == 0)
            {
                skills = new SkillBase[6];
            }

            if (skillKeys == null || skillKeys.Length == 0)
            {
                skillKeys = new[]
                {
                    KeyCode.Q, KeyCode.W, KeyCode.E,
                    KeyCode.R, KeyCode.T, KeyCode.F
                };
            }
        }

        private void LogStartupStatus()
        {
            if (startupLogged)
            {
                return;
            }

            startupLogged = true;
            int loadedSkills = 0;
            int slotCount = skills != null ? skills.Length : 0;
            if (skills != null)
            {
                for (int i = 0; i < skills.Length; i++)
                {
                    if (skills[i] != null)
                    {
                        loadedSkills++;
                    }
                }
            }

            Debug.Log($"[SkillManager] Startup | player={(playerTransform != null)} stamina={(staminaSystem != null)} input={(inputHandler != null)} action={(actionController != null)} buffer={(inputBuffer != null)} timeline={(timelineController != null)} skills={loadedSkills}/{slotCount} autoLoad={autoLoadFromResources}");

            if (!resourceAuditLogged)
            {
                resourceAuditLogged = true;
                LogSkillResourceAudit();
            }
        }

        private void SanitizeLoadedSkillTexts()
        {
            if (skills == null || skills.Length == 0)
            {
                return;
            }

            for (int i = 0; i < skills.Length; i++)
            {
                SkillBase skill = skills[i];
                if (skill == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(skill.skillName) || LooksLikeMojibake(skill.skillName))
                {
                    skill.skillName = GetFallbackSkillName(skill);
                }

                if (string.IsNullOrWhiteSpace(skill.description) || LooksLikeMojibake(skill.description))
                {
                    skill.description = GetFallbackSkillDescription(skill);
                }
            }
        }

        private static bool LooksLikeMojibake(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // Common mojibake fingerprints observed in current project logs/resources.
            return text.Contains("�")
                || text.Contains("闁")
                || text.Contains("鍒")
                || text.Contains("閲")
                || text.Contains("鎶")
                || text.Contains("馃")
                || text.Contains("閳")
                || text.Contains("纭");
        }

        private static string GetFallbackSkillName(SkillBase skill)
        {
            if (skill is WhirlwindSkill) return "Whirlwind";
            if (skill is ShockwaveSkill) return "Shockwave";
            if (skill is DashAttackSkill) return "Dash Attack";
            if (skill is BerserkSkill) return "Berserk";
            if (skill is PullSkill) return "Pull";
            if (skill is UltimateSkill) return "Ultimate Judgment";
            return "Skill";
        }

        private static string GetFallbackSkillDescription(SkillBase skill)
        {
            if (skill is WhirlwindSkill) return "Spin attack that repeatedly damages nearby enemies.";
            if (skill is ShockwaveSkill) return "Forward cone shockwave that damages and stuns enemies.";
            if (skill is DashAttackSkill) return "Dash through enemies and deal damage along the path.";
            if (skill is BerserkSkill) return "Boost combat stats for a short duration.";
            if (skill is PullSkill) return "Pull nearby enemies and slam them on landing.";
            if (skill is UltimateSkill) return "Massive area burst that knocks back and stuns enemies.";
            return "Skill effect description.";
        }

        private void LogSkillResourceAudit()
        {
            if (skills == null || skills.Length == 0)
            {
                return;
            }

            for (int i = 0; i < skills.Length; i++)
            {
                SkillBase skill = skills[i];
                if (skill == null)
                {
                    continue;
                }

                bool missingIcon = skill.icon == null;
                bool missingAudio = skill.castSound == null && skill.hitSound == null && skill.impactSound == null;
                bool missingFx = skill.effectPrefab == null && skill.castEffectPrefab == null && skill.impactEffectPrefab == null;

                if (!missingIcon && !missingAudio && !missingFx)
                {
                    continue;
                }

                Debug.LogWarning(
                    $"[SkillManager] Resource gap | slot={i} skill={skill.skillName} " +
                    $"iconMissing={missingIcon} audioMissing={missingAudio} fxMissing={missingFx}");
            }
        }
    }
}
