using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 格挡闪避系统 - 处理防御性动作
    /// 鼠标中键格挡，空格+方向闪避
    /// </summary>
    public class BlockDodgeSystem : MonoBehaviour
    {
        [Header("格挡设置")]
        public float blockDamageReduction = 0.8f;     // 格挡减伤80%
        public float perfectBlockWindow = 0.2f;       // 完美格挡窗口（秒）
        public float perfectBlockCooldown = 1f;       // 完美格挡CD
        
        [Header("闪避设置")]
        public float dodgeDistance = 4f;              // 闪避距离
        public float dodgeDuration = 0.4f;            // 闪避持续时间
        public float dodgeCooldown = 0.8f;            // 闪避冷却
        public float invincibilityDuration = 0.3f;    // 无敌帧持续时间
        
        [Header("输入")]
        public KeyCode blockKey = KeyCode.Mouse2;     // 鼠标中键格挡
        public KeyCode dodgeKey = KeyCode.LeftAlt;    // 左Alt闪避

        [Header("Input Buffer")]
        public float blockBufferTime = 0.25f;
        public float dodgeBufferTime = 0.25f;
        
        [Header("参考")]
        public Transform playerTransform;
        public Rigidbody playerRigidbody;
        public Animator animator;
        
        // 状态
        public bool IsBlocking { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool CanPerfectBlock { get; private set; }
        
        // 组件
        private StaminaSystem staminaSystem;
        private PlayerInputHandler inputHandler;
        private PlayerActionController actionController;
        private PlayerInputBuffer inputBuffer;
        
        // 计时器
        private float blockStartTime = 0f;
        private float dodgeCooldownTimer = 0f;
        private float perfectBlockCooldownTimer = 0f;
        
        // 事件
        public System.Action OnBlockStart;
        public System.Action OnBlockEnd;
        public System.Action<bool> OnPerfectBlock;  // 参数：是否成功
        public System.Action<Vector3> OnDodge;      // 参数：闪避方向
        
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
        
        #region 格挡系统
        
        private void HandleBlockInput()
        {
            if (IsDodging) return;  // 闪避时不能格挡
            
            bool blockPressed = Input.GetKey(blockKey);

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
            
            // 格挡时消耗耐力
            if (IsBlocking)
            {
                if (!staminaSystem.ConsumeBlock(Time.deltaTime))
                {
                    EndBlock();  // 耐力不足，结束格挡
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
            CanPerfectBlock = false;  // 刚按下时不能完美格挡
            
            // 动画
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "IsBlocking"))
                {
                    animator.SetBool("IsBlocking", true);
                }
            }
            
            OnBlockStart?.Invoke();
            
            // 短暂延迟后才能完美格挡（防止按住即完美格挡）
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
            
            // 动画
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
        /// 处理格挡伤害
        /// </summary>
        /// <param name="damage">原始伤害</param>
        /// <returns>格挡后的伤害</returns>
        public int ProcessBlockDamage(int damage)
        {
            if (!IsBlocking) return damage;
            
            // 检查是否是完美格挡
            bool isPerfectBlock = CanPerfectBlock && 
                                 (Time.time - blockStartTime) <= perfectBlockWindow;
            
            if (isPerfectBlock)
            {
                // 完美格挡 - 完全免疫伤害并反击
                OnPerfectBlock?.Invoke(true);
                PerformPerfectBlockCounter();
                return 0;
            }
            else
            {
                // 普通格挡 - 减伤
                OnPerfectBlock?.Invoke(false);
                return Mathf.RoundToInt(damage * (1f - blockDamageReduction));
            }
        }
        
        /// <summary>
        /// 完美格挡反击
        /// </summary>
        private void PerformPerfectBlockCounter()
        {
            // 触发反击动画
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "CounterAttack"))
                {
                    animator.SetTrigger("CounterAttack");
                }
            }
            
            // 反击效果（可以造成伤害、眩晕敌人等）
            Debug.Log("⚡ 完美格挡反击！");
            
            // 进入冷却
            CanPerfectBlock = false;
            perfectBlockCooldownTimer = perfectBlockCooldown;
        }
        
        #endregion
        
        #region 闪避系统
        
        private void HandleDodgeInput()
        {
            if (IsBlocking || IsDodging) return;  // 格挡或闪避时不能再次闪避
            if (dodgeCooldownTimer > 0) return;   // 冷却中
            
            // 检查空格键 + 方向键
            if (Input.GetKeyDown(dodgeKey))
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
        /// 获取闪避方向
        /// </summary>
        private Vector3 GetDodgeDirection()
        {
            Vector3 direction = Vector3.zero;
            
            // 优先使用输入方向
            if (inputHandler != null)
            {
                Vector2 input = inputHandler.MoveInput;
                if (input.magnitude > 0.1f)
                {
                    // 将输入转换为世界空间方向
                    direction = new Vector3(input.x, 0, input.y).normalized;
                    direction = playerTransform.TransformDirection(direction);
                }
            }
            
            // 如果没有输入，向后闪避
            if (direction == Vector3.zero)
            {
                direction = -playerTransform.forward;
            }
            
            direction.y = 0;
            return direction.normalized;
        }
        
        /// <summary>
        /// 尝试闪避
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

            // 检查耐力
            if (!staminaSystem.ConsumeDodge())
            {
                Debug.Log("⚠️ 耐力不足，无法闪避");
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
            
            // 闪避动画
            if (animator != null)
            {
                if (HasAnimatorParameter(animator, "Dodge"))
                {
                    animator.SetTrigger("Dodge");
                }
            }
            
            // 无敌帧
            IsInvincible = true;
            
            // 应用闪避力
            if (playerRigidbody != null)
            {
                playerRigidbody.AddForce(direction * dodgeDistance * 5f, ForceMode.Impulse);
            }
            
            OnDodge?.Invoke(direction);
            
            Debug.Log($"💨 向 {direction} 闪避！");
            
            // 无敌帧持续一段时间
            yield return new WaitForSeconds(invincibilityDuration);
            IsInvincible = false;
            
            // 闪避动作完成后
            yield return new WaitForSeconds(dodgeDuration - invincibilityDuration);
            IsDodging = false;
            if (actionController != null)
            {
                actionController.EndAction(PlayerActionState.Dodge);
            }
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 检查是否可以格挡
        /// </summary>
        public bool CanBlock()
        {
            bool canAction = actionController == null || actionController.CanStartAction(PlayerActionState.Block);
            return canAction && !IsDodging && staminaSystem.HasStamina;
        }
        
        /// <summary>
        /// 检查是否可以闪避
        /// </summary>
        public bool CanDodge()
        {
            bool canAction = actionController == null || actionController.CanStartAction(PlayerActionState.Dodge);
            return canAction && !IsBlocking && !IsDodging && dodgeCooldownTimer <= 0 && staminaSystem.HasStamina;
        }
        
        /// <summary>
        /// 强制取消格挡（如被眩晕）
        /// </summary>
        public void ForceCancelBlock()
        {
            if (IsBlocking)
            {
                EndBlock();
            }
        }
        
        /// <summary>
        /// 强制取消闪避（如撞墙）
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
