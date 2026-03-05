using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能管理器 - 管理所有技能的释放和冷却
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [Header("技能槽位")]
        public SkillBase[] skills = new SkillBase[6]; // Q/W/E/R/T/F

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
        
        [Header("参考")]
        public Transform playerTransform;
        public StaminaSystem staminaSystem;
        public PlayerInputHandler inputHandler;
        public PlayerActionController actionController;
        public PlayerInputBuffer inputBuffer;
        public SkillTimelineController timelineController;
        
        private SkillBase activeSkill;
        private Transform activeCaster;

        // 技能释放原点（通常是从玩家位置稍微前方）
        public Vector3 CastOrigin => playerTransform.position + playerTransform.forward * 0.5f + Vector3.up * 1f;
        
        private void Awake()
        {
            if (playerTransform == null)
                playerTransform = transform;
            
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();
            
            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();

            if (actionController == null)
                actionController = GetComponent<PlayerActionController>();

            if (inputBuffer == null)
                inputBuffer = GetComponent<PlayerInputBuffer>();

            if (timelineController == null)
                timelineController = GetComponent<SkillTimelineController>();

            if (autoLoadFromResources)
            {
                AutoLoadSkills();
            }
        }

        private void OnEnable()
        {
            if (actionController != null)
            {
                actionController.OnActionInterrupted += HandleActionInterrupted;
            }

            if (timelineController != null)
            {
                timelineController.OnTimelineEnded += HandleTimelineEnded;
            }
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
            // 更新所有技能冷却
            UpdateAllCooldowns();
            
            // 处理输入
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
        }
        
        /// <summary>
        /// 更新所有技能冷却
        /// </summary>
        private void UpdateAllCooldowns()
        {
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                {
                    skills[i].UpdateCooldown(Time.deltaTime);
                }
            }
        }
        
        /// <summary>
        /// 处理技能输入
        /// </summary>
        private void HandleInput()
        {
            for (int i = 0; i < skillKeys.Length; i++)
            {
                if (Input.GetKeyDown(skillKeys[i]))
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

        private void TryConsumeBufferedSkills()
        {
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
        /// 尝试使用技能
        /// </summary>
        public bool TryUseSkill(int index)
        {
            if (index < 0 || index >= skills.Length) return false;
            
            SkillBase skill = skills[index];
            if (skill == null)
            {
                Debug.Log($"技能槽 {index} 为空");
                return false;
            }

            if (actionController != null && !actionController.CanStartAction(PlayerActionState.Skill))
            {
                return false;
            }
            
            // 检查是否可以释放
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

            // 获取目标位置（玩家前方）
            Vector3 targetPosition = GetTargetPosition();
            
            SetActiveSkill(skill, playerTransform);

            // 执行技能
            skill.ExecuteWithTimeline(playerTransform, targetPosition, timelineController);
            
            // 消耗耐力
            if (!skill.ConsumeStamina(staminaSystem, playerTransform))
            {
                if (actionController != null)
                {
                    actionController.EndAction(PlayerActionState.Skill);
                }
                return false;
            }
            
            // 开始冷却
            skill.StartCooldown(playerTransform);
            
            Debug.Log($"✨ 释放技能: {skill.skillName}");
            
            return true;
        }
        
        /// <summary>
        /// 获取目标位置
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            // 简单实现：玩家前方一定距离
            return playerTransform.position + playerTransform.forward * 10f;
        }
        
        /// <summary>
        /// 装备技能到指定槽位
        /// </summary>
        public void EquipSkill(int slotIndex, SkillBase skill)
        {
            if (slotIndex >= 0 && slotIndex < skills.Length)
            {
                skills[slotIndex] = skill;
                Debug.Log($"装备技能 {skill.skillName} 到槽位 {slotIndex}");
            }
        }
        
        /// <summary>
        /// 获取技能的冷却进度
        /// </summary>
        public float GetSkillCooldownProgress(int index)
        {
            if (index < 0 || index >= skills.Length || skills[index] == null)
                return 0f;
            
            return skills[index].GetCooldownProgress();
        }
        
        /// <summary>
        /// 检查技能是否就绪
        /// </summary>
        public bool IsSkillReady(int index)
        {
            if (index < 0 || index >= skills.Length || skills[index] == null)
                return false;
            
            return skills[index].isReady;
        }
        
        /// <summary>
        /// 获取技能图标
        /// </summary>
        public Sprite GetSkillIcon(int index)
        {
            if (index < 0 || index >= skills.Length || skills[index] == null)
                return null;
            
            return skills[index].icon;
        }
        
        /// <summary>
        /// 重置所有技能冷却（用于测试或特殊效果）
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    skill.cooldownTimer = 0;
                    skill.isReady = true;
                    skill.cooldownDuration = 0f;
                }
            }
            Debug.Log("🔄 所有技能冷却已重置");
        }
        
        /// <summary>
        /// 强制刷新一个技能（如装备效果）
        /// </summary>
        public void RefreshSkill(int index)
        {
            if (index >= 0 && index < skills.Length && skills[index] != null)
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
    }
}
