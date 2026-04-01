using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// StaminaSystem 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// 负责耐力消耗、恢复、力竭状态与相关事件广播。
    /// </summary>
    public class StaminaSystem : MonoBehaviour
    {
        [Header("耐力设置")]
        public float maxStamina = 100f;
        public float currentStamina;
        
        [Header("恢复设置")]
        public float recoveryRate = 15f; // 每秒恢复量，用于控制资源回流速度并稳定续航。
        public float recoveryDelay = 1f;           // 触发消耗后恢复延迟（秒）
        
        [Header("Costs")]
        public float heavyAttackCost = 20f; // 重击消耗，用于建立资源取舍并强化战斗决策。
        public float dodgeCost = 15f; // 闪避消耗，用于建立资源取舍并强化战斗决策。
        public float blockCostPerSecond = 5f; // 格挡每秒消耗，用于建立资源取舍并强化战斗决策。
        public float sprintCostPerSecond = 10f; // 冲刺每秒消耗，用于建立资源取舍并强化战斗决策。
        
        [Header("State")]
        public bool isExhausted = false; // 运行时状态标记，用于快速分支判定与流程保护。
        public float exhaustionDuration = 2f; // 力竭持续时间，用于定义效果生效窗口。

        [Header("力竭恢复")]
        public bool allowRecoveryWhileExhausted = true;

        [Header("Debug")]
        public bool showEditorDebugOverlay = false;
        
// 对外事件，用于模块解耦并同步关键节点。
        public System.Action<float, float> OnStaminaChanged;
        public System.Action OnStaminaDepleted;
        public System.Action OnExhaustionEnd;
        
        private float recoveryTimer = 0f;
        private float exhaustionTimer = 0f;
        private bool canRecover = true;
        
        public float StaminaPercent => currentStamina / maxStamina;
        public bool HasStamina => currentStamina > 0 && !isExhausted;

        public void ApplyMaxStamina(float newMaxStamina, bool keepPercent)
        {
            if (newMaxStamina < 1f)
            {
                newMaxStamina = 1f;
            }

            float percent = keepPercent ? StaminaPercent : 1f;
            maxStamina = newMaxStamina;
            currentStamina = Mathf.Clamp(maxStamina * percent, 0f, maxStamina);
            NotifyStaminaChanged();
        }
        
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
        /// 处理耐力自动恢复逻辑。
        /// </summary>
        private void HandleRecovery()
        {
            if (!canRecover || isExhausted) return;
            
// 冷却中不恢复，用于限制触发频率并平衡节奏。
            if (recoveryTimer > 0)
            {
                recoveryTimer -= Time.deltaTime;
                return;
            }
            
// 线性恢复到上限，用于控制资源回流速度并稳定续航。
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
        /// 处理力竭倒计时与恢复。
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
                Debug.Log("Exhaustion ended, stamina can recover.");
            }
        }
        
        /// <summary>
        /// 执行 Consume Stamina 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        /// <param name="amount">本次消耗值</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeStamina(float amount)
        {
            if (isExhausted)
            {
                Debug.Log("[Stamina] Character is exhausted; stamina cannot be consumed.");
                return false;
            }
            
            if (currentStamina < amount)
            {
                // 不足以支付消耗，直接进入力竭
                EnterExhaustion();
                return false;
            }
            
            currentStamina -= amount;
            recoveryTimer = recoveryDelay; // 重置恢复延迟，用于控制资源回流速度并稳定续航。
            
            NotifyStaminaChanged();
            
            // 消耗后若耗尽，进入力竭
            if (currentStamina <= 0)
            {
                EnterExhaustion();
            }
            
            return true;
        }
        
        /// <summary>
        /// 进入力竭状态并触发事件。
        /// </summary>
        private void EnterExhaustion()
        {
            isExhausted = true;
            exhaustionTimer = exhaustionDuration;
            currentStamina = 0;
            
            OnStaminaDepleted?.Invoke();
            GameEvents.StaminaDepleted();
            
            Debug.Log("Stamina depleted, entered exhaustion state.");
        }
        
        /// <summary>
        /// 外部恢复耐力（药水、技能等奖励）。
        /// </summary>
        public void RecoverStamina(float amount)
        {
            if (isExhausted && !allowRecoveryWhileExhausted) return;

            if (isExhausted)
            {
                ClearExhaustion();
            }
            
            float oldStamina = currentStamina;
            currentStamina += amount;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            
            if (currentStamina != oldStamina)
            {
                NotifyStaminaChanged();
            }
        }
        
        /// <summary>
        /// 执行 Recover All Stamina 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void RecoverAllStamina()
        {
            if (isExhausted && !allowRecoveryWhileExhausted) return;

            if (isExhausted)
            {
                ClearExhaustion();
            }
            
            currentStamina = maxStamina;
            NotifyStaminaChanged();
        }
        
        /// <summary>
        /// 检查是否有足够耐力。
        /// </summary>
        public bool HasEnoughStamina(float amount)
        {
            return !isExhausted && currentStamina >= amount;
        }
        
        /// <summary>
        /// 校验动作是否可执行（耐力维度）。
        /// </summary>
        public bool CanPerformAction(float cost)
        {
            return HasEnoughStamina(cost);
        }
        
        /// <summary>
        /// 执行 Notify Stamina Changed 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        private void NotifyStaminaChanged()
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            GameEvents.StaminaChanged(currentStamina, maxStamina);
        }

        private void ClearExhaustion()
        {
            isExhausted = false;
            exhaustionTimer = 0f;
            canRecover = true;
            OnExhaustionEnd?.Invoke();
        }
        
        #region 渚挎嵎鏂规硶
        
        /// <summary>
        /// 执行 Consume Heavy Attack 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public bool ConsumeHeavyAttack() => ConsumeStamina(heavyAttackCost);
        
        /// <summary>
        /// 执行 Consume Dodge 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public bool ConsumeDodge() => ConsumeStamina(dodgeCost);
        
        /// <summary>
        /// 封装格挡耐力消耗（按帧）。
        /// </summary>
        public bool ConsumeBlock(float deltaTime)
        {
            return ConsumeStamina(blockCostPerSecond * deltaTime);
        }
        
        /// <summary>
        /// 封装冲刺耐力消耗（按帧）。
        /// </summary>
        public bool ConsumeSprint(float deltaTime)
        {
            return ConsumeStamina(sprintCostPerSecond * deltaTime);
        }
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!Application.isEditor || !showEditorDebugOverlay) return;
            
            // 编辑器下显示实时耐力状态
            GUILayout.BeginArea(new Rect(10, Screen.height - 60, 200, 50));
            GUILayout.Label($"耐力: {currentStamina:F0}/{maxStamina}");
            GUILayout.Label($"状态: {(isExhausted ? "力竭" : "正常")}");
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
