using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 格挡与闪避核心系统。
    /// 负责输入处理、状态切换、耐力消耗与完美格挡判定。
    /// </summary>
    public class BlockDodgeSystem : MonoBehaviour
    {
        [Header("格挡设置")]
        public float blockDamageReduction = 0.8f; // 运行时配置项，用于驱动模块行为并保持可调性。
        public float perfectBlockWindow = 0.2f;       // 完美格挡判定窗口（秒）
        public float perfectBlockCooldown = 1f; // 完美格挡冷却（秒），用于限制触发频率并平衡节奏。

        [Header("闪避设置")]
        public float dodgeDistance = 4f; // 闪避位移距离，用于约束判定覆盖面并避免越界命中。
        public float dodgeDuration = 0.4f; // 闪避持续时长，用于定义效果生效窗口。
        public float dodgeCooldown = 0.8f; // 闪避冷却时间，用于限制触发频率并平衡节奏。
        public float invincibilityDuration = 0.3f; // 闪避无敌时长，用于定义效果生效窗口。

        [Header("Input")]
        public KeyCode blockKey = KeyCode.Mouse2; // 格挡按键，用于输入映射并支持后续重绑定。
        public KeyCode dodgeKey = KeyCode.LeftAlt; // 闪避按键，用于输入映射并支持后续重绑定。

        [Header("Input Buffer")]
        public float blockBufferTime = 0.25f;
        public float dodgeBufferTime = 0.25f;

        [Header("References")]
        public Transform playerTransform;
        public Rigidbody playerRigidbody;
        public Animator animator;

// 当前防御状态，用于对外暴露当前流程阶段。
        public bool IsBlocking { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool CanPerfectBlock { get; private set; }

// 外部依赖组件，用于缓存依赖并减少重复查找。
        private StaminaSystem staminaSystem;
        private PlayerInputHandler inputHandler;
        private PlayerActionController actionController;
        private PlayerInputBuffer inputBuffer;

// 计时器状态，用于对外暴露当前流程阶段。
        private float blockStartTime = 0f;
        private float dodgeCooldownTimer = 0f;
        private float perfectBlockCooldownTimer = 0f;

// 对外事件，用于模块解耦并同步关键节点。
        public System.Action OnBlockStart;
        public System.Action OnBlockEnd;
        public System.Action<bool> OnPerfectBlock; // 完美格挡结果回调，用于模块解耦并同步关键节点。
        public System.Action<Vector3> OnDodge; // 闪避触发回调（方向），用于模块解耦并同步关键节点。

        private void Awake()
        {
            staminaSystem = GetComponent<StaminaSystem>();
            inputHandler = GetComponent<PlayerInputHandler>();
            actionController = GetComponent<PlayerActionController>();
            inputBuffer = GetComponent<PlayerInputBuffer>();

            if (playerTransform == null)
                playerTransform = transform;
            if (playerRigidbody == null)
                playerRigidbody = GetComponent<Rigidbody>();
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Update()
        {
            HandleCooldowns();
            HandleBlockInput();
            HandleDodgeInput();
        }

        private void HandleCooldowns()
        {
            if (dodgeCooldownTimer > 0)
                dodgeCooldownTimer -= Time.deltaTime;

            if (perfectBlockCooldownTimer > 0)
                perfectBlockCooldownTimer -= Time.deltaTime;
            else
                CanPerfectBlock = true;
        }

        #region 格挡逻辑

        private void HandleBlockInput()
        {
            if (IsDodging) return;  // 闪避中不允许进入格挡
            bool blockPressed = inputHandler != null
                ? inputHandler.BlockHeld
                : PlayerInputHandler.ReadUnifiedKey(blockKey);

            if (inputBuffer != null)
            {
                if (blockPressed)
                {
                    inputBuffer.BufferAction(BufferedActionType.Block, blockBufferTime);
                }
                else
                {
                    inputBuffer.ClearAction(BufferedActionType.Block);
                }

                if (!IsBlocking && inputBuffer.HasAction(BufferedActionType.Block))
                {
                    if (StartBlock())
                    {
                        inputBuffer.TryConsume(BufferedActionType.Block, out _);
                    }
                }
            }
            else
            {
                if (blockPressed && !IsBlocking)
                {
                    StartBlock();
                }
            }

            if (!blockPressed && IsBlocking)
            {
                EndBlock();
            }

            // 持续格挡时按秒消耗耐力
            if (IsBlocking)
            {
                if (!staminaSystem.ConsumeBlock(Time.deltaTime))
                {
                    EndBlock();  // 耐力不足，强制结束格挡
                }
            }
        }

        private bool StartBlock()
        {
            if (actionController != null)
            {
                if (!actionController.TryStartAction(
                    PlayerActionState.Block,
                    ActionPriority.Block,
                    0f,
                    true,
                    false,
                    false,
                    true,
                    ActionInterruptMask.Dodge))
                {
                    return false;
                }
            }

            IsBlocking = true;
            blockStartTime = Time.time;
            CanPerfectBlock = false;  // 进入格挡时先关闭完美格挡窗口

// 同步动画状态，用于对外暴露当前流程阶段。
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "IsBlocking"))
                {
                    animator.SetBool("IsBlocking", true);
                }
            }

            OnBlockStart?.Invoke();

            // 延迟开启完美格挡判定窗口
            Invoke(nameof(EnablePerfectBlock), 0.1f);

            return true;
        }

        private void EnablePerfectBlock()
        {
            if (IsBlocking)
            {
                CanPerfectBlock = true;
            }
        }

        private void EndBlock()
        {
            IsBlocking = false;
            CanPerfectBlock = false;

            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Block);
            }

            // 同步关闭格挡动画状态
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "IsBlocking"))
                {
                    animator.SetBool("IsBlocking", false);
                }
            }

            OnBlockEnd?.Invoke();
        }

        /// <summary>
        /// 处理格挡时受到的伤害结算。
        /// </summary>
        /// <param name="damage">原始伤害值</param>
        /// <returns>格挡后最终伤害</returns>
        public int ProcessBlockDamage(int damage)
        {
            if (!IsBlocking) return damage;

            // 判定是否命中完美格挡窗口
            bool isPerfectBlock = CanPerfectBlock &&
                                 (Time.time - blockStartTime) <= perfectBlockWindow;

            if (isPerfectBlock)
            {
                // 完美格挡：触发反击并免伤
                OnPerfectBlock?.Invoke(true);
                PerformPerfectBlockCounter();
                return 0;
            }
            else
            {
                // 普通格挡：按减伤比例结算
                OnPerfectBlock?.Invoke(false);
                return Mathf.RoundToInt(damage * (1f - blockDamageReduction));
            }
        }

        /// <summary>
        /// 触发完美格挡后的反击表现与冷却。
        /// </summary>
        private void PerformPerfectBlockCounter()
        {
// 触发反击动画，用于强化动作反馈并统一表现节奏。
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "CounterAttack"))
                {
                    animator.SetTrigger("CounterAttack");
                }
            }

// 记录调试日志，用于开发阶段快速定位时序问题。
            Debug.Log("Perfect block counter triggered.");

            // 关闭完美格挡并进入冷却
            CanPerfectBlock = false;
            perfectBlockCooldownTimer = perfectBlockCooldown;
        }

        #endregion

        #region 闪避逻辑

        private void HandleDodgeInput()
        {
            if (IsBlocking || IsDodging) return;  // 防御动作中禁止再次闪避
            if (dodgeCooldownTimer > 0) return; // 闪避仍在冷却，用于限制触发频率并平衡节奏。
            bool dodgePressed = inputHandler != null
                ? inputHandler.DodgePressed
                : PlayerInputHandler.ReadUnifiedKeyDown(dodgeKey);

            // 检测闪避输入并写入缓冲
            if (dodgePressed)
            {
                Vector3 dodgeDirection = GetDodgeDirection();

                if (dodgeDirection != Vector3.zero)
                {
                    if (inputBuffer != null)
                    {
                        inputBuffer.BufferAction(BufferedActionType.Dodge, dodgeBufferTime, -1, dodgeDirection);
                    }
                    else
                    {
                        TryDodge(dodgeDirection);
                    }
                }
            }

            if (inputBuffer != null && inputBuffer.TryGet(BufferedActionType.Dodge, out BufferedActionEntry entry))
            {
                if (CanDodge() && HasDodgeStamina())
                {
                    Vector3 direction = entry.hasDirection ? entry.direction : GetDodgeDirection();
                    if (TryDodge(direction))
                    {
                        inputBuffer.TryConsume(BufferedActionType.Dodge, out _);
                    }
                }
            }
        }

        /// <summary>
        /// 获取闪避方向（优先输入方向，回退为后撤）。
        /// </summary>
        private Vector3 GetDodgeDirection()
        {
            Vector3 direction = Vector3.zero;

            // 读取移动输入并转换为世界方向
            if (inputHandler != null)
            {
                Vector2 input = inputHandler.MoveInput;
                if (input.magnitude > 0.1f)
                {
                    // 本地输入方向转世界方向
                    direction = new Vector3(input.x, 0, input.y).normalized;
                    direction = playerTransform.TransformDirection(direction);
                }
            }

            // 无输入时默认向后闪避
            if (direction == Vector3.zero)
            {
                direction = -playerTransform.forward;
            }

            direction.y = 0;
            return direction.normalized;
        }

        /// <summary>
        /// 尝试执行闪避动作（含动作锁与耐力校验）。
        /// </summary>
        private bool TryDodge(Vector3 direction)
        {
            if (actionController != null)
            {
                if (!actionController.TryStartAction(
                    PlayerActionState.Dodge,
                    ActionPriority.Dodge,
                    dodgeDuration,
                    true,
                    true,
                    true,
                    true,
                    ActionInterruptMask.None))
                {
                    return false;
                }
            }

            // 闪避需要先通过耐力消耗校验
            if (!staminaSystem.ConsumeDodge())
            {
                Debug.Log("Not enough stamina to dodge.");
                if (actionController != null)
                {
                    actionController.EndAction(PlayerActionState.Dodge);
                }
                return false;
            }

            StartCoroutine(DodgeCoroutine(direction));
            return true;
        }

        private bool HasDodgeStamina()
        {
            if (staminaSystem == null)
            {
                return true;
            }

            return staminaSystem.HasEnoughStamina(staminaSystem.dodgeCost);
        }

        private IEnumerator DodgeCoroutine(Vector3 direction)
        {
            IsDodging = true;
            dodgeCooldownTimer = dodgeCooldown;

// 触发闪避动画，用于强化动作反馈并统一表现节奏。
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "Dodge"))
                {
                    animator.SetTrigger("Dodge");
                }
            }

// 闪避起始进入无敌，用于保障关键动作窗口的公平性。
            IsInvincible = true;

// 施加位移冲量，用于控制机动性并保持手感一致。
            if (playerRigidbody != null)
            {
                playerRigidbody.AddForce(direction * dodgeDistance * 5f, ForceMode.Impulse);
            }

            OnDodge?.Invoke(direction);

            Debug.Log($"Dodge direction: {direction}");

// 无敌结束，用于保障关键动作窗口的公平性。
            yield return new WaitForSeconds(invincibilityDuration);
            IsInvincible = false;

            // 闪避动作结束并解除状态
            yield return new WaitForSeconds(dodgeDuration - invincibilityDuration);
            IsDodging = false;
            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Dodge);
            }
        }

        #endregion

        #region 閸忣剙鍙￠幒銉ュ經

        /// <summary>
        /// 判断Block，提供前置校验供调用方决策。
        /// </summary>
        public bool CanBlock()
        {
            bool canAction = actionController == null || actionController.CanStartAction(PlayerActionState.Block);
            return canAction && !IsDodging && staminaSystem.HasStamina;
        }

        /// <summary>
        /// 判断Dodge，提供前置校验供调用方决策。
        /// </summary>
        public bool CanDodge()
        {
            bool canAction = actionController == null || actionController.CanStartAction(PlayerActionState.Dodge);
            return canAction && !IsBlocking && !IsDodging && dodgeCooldownTimer <= 0 && staminaSystem.HasStamina;
        }

        /// <summary>
        /// 执行 Force Cancel Block 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ForceCancelBlock()
        {
            if (IsBlocking)
            {
                EndBlock();
            }
        }

        /// <summary>
        /// 执行 Force Cancel Dodge 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ForceCancelDodge()
        {
            if (IsDodging)
            {
                StopAllCoroutines();
                IsDodging = false;
                IsInvincible = false;
                if (actionController != null)
                {
                    actionController.EndAction(PlayerActionState.Dodge);
                }
            }
        }

        private bool HasAnimatorParameter(Animator target, string parameterName)
        {
            if (target == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            foreach (var param in target.parameters)
            {
                if (param.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
