using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 技能基类 - ScriptableObject
    /// 所有技能都继承此类
    /// </summary>
    public abstract class SkillBase : ScriptableObject
    {
        [Header("基础信息")]
        public string skillName = "新技能";
        public string description = "技能描述";
        public Sprite icon;
        public KeyCode keyBinding;
        
        [Header("冷却与消耗")]
        public float cooldown = 10f;
        public float staminaCost = 20f;
        
        [Header("伤害与效果")]
        public int damage = 50;
        public float range = 5f;
        public float effectDuration = 2f;
        
        [Header("视觉效果")]
        public GameObject effectPrefab;
        public AudioClip castSound;
        public AudioClip hitSound;
        
        // 运行时数据（不保存到ScriptableObject）
        [System.NonSerialized]
        public float cooldownTimer = 0f;
        [System.NonSerialized]
        public bool isReady = true;
        
        /// <summary>
        /// 执行技能
        /// </summary>
        /// <param name="caster">施法者（玩家）</param>
        /// <param name="targetPosition">目标位置</param>
        public abstract void Execute(Transform caster, Vector3 targetPosition);
        
        /// <summary>
        /// 检查是否可以释放技能
        /// </summary>
        public virtual bool CanExecute(Transform caster, StaminaSystem stamina)
        {
            // 检查冷却
            if (!isReady) return false;
            
            // 检查耐力
            if (stamina != null && !stamina.HasEnoughStamina(staminaCost))
            {
                GameEvents.ShowMessage("耐力不足！", 1f);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 开始冷却
        /// </summary>
        public virtual void StartCooldown()
        {
            cooldownTimer = cooldown;
            isReady = false;
            
            // 触发技能使用事件
            GameEvents.SkillUsed(skillName, cooldown);
        }
        
        /// <summary>
        /// 更新冷却（每帧调用）
        /// </summary>
        public virtual void UpdateCooldown(float deltaTime)
        {
            if (!isReady && cooldownTimer > 0)
            {
                cooldownTimer -= deltaTime;
                if (cooldownTimer <= 0)
                {
                    cooldownTimer = 0;
                    isReady = true;
                    
                    // 触发冷却完成事件
                    GameEvents.SkillReady(skillName);
                }
            }
        }
        
        /// <summary>
        /// 获取冷却进度（0-1）
        /// </summary>
        public float GetCooldownProgress()
        {
            if (isReady) return 0f;
            return cooldownTimer / cooldown;
        }
        
        /// <summary>
        /// 播放特效
        /// </summary>
        protected void SpawnEffect(Vector3 position, Quaternion rotation)
        {
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab, position, rotation);
                Destroy(effect, effectDuration);
            }
        }
        
        /// <summary>
        /// 播放音效
        /// </summary>
        protected void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position);
            }
        }
        
        /// <summary>
        /// 消耗耐力
        /// </summary>
        protected bool ConsumeStamina(StaminaSystem stamina)
        {
            if (stamina == null) return true;
            return stamina.ConsumeStamina(staminaCost);
        }
    }
}
