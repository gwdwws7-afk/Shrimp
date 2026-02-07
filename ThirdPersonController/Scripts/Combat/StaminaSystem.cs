using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 耐力系统 - 管理耐力消耗和恢复
    /// 用于重攻击、闪避、格挡、冲刺等动作
    /// </summary>
    public class StaminaSystem : MonoBehaviour
    {
        [Header("耐力设置")]
        public float maxStamina = 100f;
        public float currentStamina;
        
        [Header("恢复设置")]
        public float recoveryRate = 15f;           // 每秒恢复量
        public float recoveryDelay = 1f;           // 消耗后多久开始恢复
        
        [Header("消耗设置")]
        public float heavyAttackCost = 20f;        // 重攻击消耗
        public float dodgeCost = 15f;              // 闪避消耗
        public float blockCostPerSecond = 5f;      // 格挡每秒消耗
        public float sprintCostPerSecond = 10f;    // 冲刺每秒消耗
        
        [Header("状态")]
        public bool isExhausted = false;           // 是否力竭
        public float exhaustionDuration = 2f;      // 力竭持续时间
        
        // 事件
        public System.Action<float, float> OnStaminaChanged;
        public System.Action OnStaminaDepleted;
        public System.Action OnExhaustionEnd;
        
        private float recoveryTimer = 0f;
        private float exhaustionTimer = 0f;
        private bool canRecover = true;
        
        public float StaminaPercent => currentStamina / maxStamina;
        public bool HasStamina => currentStamina > 0 && !isExhausted;
        
        private void Awake()
        {
            currentStamina = maxStamina;
        }
        
        private void Update()
        {
            HandleRecovery();
            HandleExhaustion();
        }
        
        /// <summary>
        /// 处理耐力恢复
        /// </summary>
        private void HandleRecovery()
        {
            if (!canRecover || isExhausted) return;
            
            // 延迟恢复
            if (recoveryTimer > 0)
            {
                recoveryTimer -= Time.deltaTime;
                return;
            }
            
            // 恢复耐力
            if (currentStamina < maxStamina)
            {
                float oldStamina = currentStamina;
                currentStamina += recoveryRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
                
                if (currentStamina != oldStamina)
                {
                    NotifyStaminaChanged();
                }
            }
        }
        
        /// <summary>
        /// 处理力竭状态
        /// </summary>
        private void HandleExhaustion()
        {
            if (!isExhausted) return;
            
            exhaustionTimer -= Time.deltaTime;
            if (exhaustionTimer <= 0)
            {
                isExhausted = false;
                canRecover = true;
                OnExhaustionEnd?.Invoke();
                Debug.Log("💨 力竭状态结束，可以恢复耐力了");
            }
        }
        
        /// <summary>
        /// 消耗耐力
        /// </summary>
        /// <param name="amount">消耗量</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeStamina(float amount)
        {
            if (isExhausted)
            {
                Debug.Log("⚠️ 力竭状态，无法消耗耐力");
                return false;
            }
            
            if (currentStamina < amount)
            {
                // 耐力不足，进入力竭
                EnterExhaustion();
                return false;
            }
            
            currentStamina -= amount;
            recoveryTimer = recoveryDelay;  // 重置恢复延迟
            
            NotifyStaminaChanged();
            
            // 耐力耗尽
            if (currentStamina <= 0)
            {
                EnterExhaustion();
            }
            
            return true;
        }
        
        /// <summary>
        /// 进入力竭状态
        /// </summary>
        private void EnterExhaustion()
        {
            isExhausted = true;
            exhaustionTimer = exhaustionDuration;
            currentStamina = 0;
            
            OnStaminaDepleted?.Invoke();
            GameEvents.StaminaDepleted();
            
            Debug.Log("😫 耐力耗尽！进入力竭状态");
        }
        
        /// <summary>
        /// 恢复耐力（外部调用，如药水、技能）
        /// </summary>
        public void RecoverStamina(float amount)
        {
            if (isExhausted) return;  // 力竭状态不能恢复
            
            float oldStamina = currentStamina;
            currentStamina += amount;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            
            if (currentStamina != oldStamina)
            {
                NotifyStaminaChanged();
            }
        }
        
        /// <summary>
        /// 完全恢复耐力
        /// </summary>
        public void RecoverAllStamina()
        {
            if (isExhausted) return;
            
            currentStamina = maxStamina;
            NotifyStaminaChanged();
        }
        
        /// <summary>
        /// 检查是否有足够耐力
        /// </summary>
        public bool HasEnoughStamina(float amount)
        {
            return !isExhausted && currentStamina >= amount;
        }
        
        /// <summary>
        /// 获取当前可用状态
        /// </summary>
        public bool CanPerformAction(float cost)
        {
            return HasEnoughStamina(cost);
        }
        
        /// <summary>
        /// 通知耐力变化
        /// </summary>
        private void NotifyStaminaChanged()
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            GameEvents.StaminaChanged(currentStamina, maxStamina);
        }
        
        #region 便捷方法
        
        /// <summary>
        /// 消耗重攻击耐力
        /// </summary>
        public bool ConsumeHeavyAttack() => ConsumeStamina(heavyAttackCost);
        
        /// <summary>
        /// 消耗闪避耐力
        /// </summary>
        public bool ConsumeDodge() => ConsumeStamina(dodgeCost);
        
        /// <summary>
        /// 消耗格挡耐力（每帧调用）
        /// </summary>
        public bool ConsumeBlock(float deltaTime)
        {
            return ConsumeStamina(blockCostPerSecond * deltaTime);
        }
        
        /// <summary>
        /// 消耗冲刺耐力（每帧调用）
        /// </summary>
        public bool ConsumeSprint(float deltaTime)
        {
            return ConsumeStamina(sprintCostPerSecond * deltaTime);
        }
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!Application.isEditor) return;
            
            // 编辑器模式下显示耐力信息
            GUILayout.BeginArea(new Rect(10, Screen.height - 60, 200, 50));
            GUILayout.Label($"耐力: {currentStamina:F0}/{maxStamina}");
            GUILayout.Label($"状态: {(isExhausted ? "力竭" : "正常")}");
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
