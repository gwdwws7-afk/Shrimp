using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 连击系统调试器 - 用于在控制台显示连击信息
    /// 挂在玩家角色上即可
    /// </summary>
    public class ComboDebugger : MonoBehaviour
    {
        private PlayerCombat combat;
        private int lastCombo = 0;
        private bool wasBerserk = false;
        
        void Start()
        {
            combat = GetComponent<PlayerCombat>();
            if (combat == null)
            {
                Debug.LogError("ComboDebugger: 找不到 PlayerCombat 组件！");
                return;
            }
            
            // 订阅事件
            combat.OnComboChanged += OnComboChanged;
            combat.OnBerserkStateChanged += OnBerserkStateChanged;
            
            Debug.Log("✅ 连击调试器已启动 - 开始攻击敌人测试连击系统！");
            Debug.Log("连击等级：1-10(T1) | 11-30(T2) | 31-49(T3) | 50+(狂暴)");
        }
        
        void OnDestroy()
        {
            if (combat != null)
            {
                combat.OnComboChanged -= OnComboChanged;
                combat.OnBerserkStateChanged -= OnBerserkStateChanged;
            }
        }
        
        private void OnComboChanged(int combo)
        {
            if (combo > lastCombo)
            {
                // 连击增加
                string tierStr = GetTierString(combat.CurrentTier);
                Debug.Log($"⚔️ 连击: {combo} {tierStr}");
            }
            else if (combo == 0 && lastCombo > 0)
            {
                // 连击重置
                Debug.Log($"💨 连击重置！最高连击: {lastCombo}");
            }
            
            lastCombo = combo;
        }
        
        private void OnBerserkStateChanged(bool isActive)
        {
            if (isActive && !wasBerserk)
            {
                Debug.Log("🔥🔥🔥 深渊狂暴模式启动！持续3秒 🔥🔥🔥");
                Debug.Log("✨ 效果: 攻击范围x2 | 伤害x2 | 吸血10% | 无敌");
            }
            else if (!isActive && wasBerserk)
            {
                Debug.Log("💨 深渊狂暴模式结束");
            }
            
            wasBerserk = isActive;
        }
        
        private string GetTierString(ComboTier tier)
        {
            return tier switch
            {
                ComboTier.Tier1 => "[T1 +10%伤]",
                ComboTier.Tier2 => "[T2 +25%伤]",
                ComboTier.Tier3 => "[T3 +50%伤 +5%吸血]",
                ComboTier.Tier4 => "[🔥狂暴 +100%伤 +10%吸血]",
                _ => ""
            };
        }
        
        void Update()
        {
            // 按 Tab 键显示当前状态
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (combat != null)
                {
                    Debug.Log($"当前状态 - 连击: {combat.CurrentCombo} | 等级: {combat.CurrentTier} | 狂暴: {combat.IsBerserk}");
                }
            }
            
            // 按 R 键重置连击（测试用）
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("🔄 手动重置连击");
                // 通过反射或直接重置（如果需要）
            }
        }
    }
}
