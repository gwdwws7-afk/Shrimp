using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float sprintSpeed = 10f;
        public float crouchSpeed = 2.5f;
        public float rotationSpeed = 10f;
        public float acceleration = 10f;
        public float deceleration = 10f;

        [Header("Jump Settings")]
        [Tooltip("跳跃高度（米）")]
        public float jumpHeight = 2f;
        [Tooltip("跳跃按键缓冲时间（秒）")]
        public float jumpBufferTime = 0.2f;
        [Tooltip("土狼时间 - 离开地面后仍可跳跃的时间")]
        public float coyoteTime = 0.15f;
        [Tooltip("下落重力倍率（越大下落越快）")]
        public float fallMultiplier = 5f;
        [Tooltip("低高度跳跃倍率（按空格时间短跳得低）")]
        public float lowJumpMultiplier = 3f;
        [Tooltip("最大下落速度")]
        public float maxFallSpeed = -20f;

        [Header("Ground Check")]
        public Transform groundCheck;
        [Tooltip("地面检测半径")]
        public float groundCheckRadius = 0.4f;
        [Tooltip("地面检测距离（从脚底向下）")]
        public float groundCheckDistance = 0.2f;
        public LayerMask groundLayer;

        [Header("Crouch Settings")]
        public float crouchHeight = 1f;
        public float standHeight = 1.8f;
        public float crouchTransitionSpeed = 10f;

        private Rigidbody rb;
        private PlayerInputHandler input;
        private CapsuleCollider capsuleCollider;
        private Animator animator;
        private PlayerCombat combat;
        private PlayerActionController actionController;

        private bool isGrounded;
        private bool wasGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private bool isJumping;
        private bool suppressJumpUntilRelease;

        // 计时器
        private float jumpBufferTimer;
        private float coyoteTimeTimer;
        
        private Vector3 moveDirection;
        private Vector3 currentVelocity;
        private float targetSpeed;
        private float currentHeight;

        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public bool IsJumping => isJumping;
        public float CurrentSpeed => currentVelocity.magnitude;
        public Vector3 MoveDirection => moveDirection;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<PlayerInputHandler>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            animator = GetComponent<Animator>();
            combat = GetComponent<PlayerCombat>();
            actionController = GetComponent<PlayerActionController>();

            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // 确保使用合适的重力（Unity默认是 -9.81）
            if (Physics.gravity.y > -5f)
            {
                Physics.gravity = new Vector3(0, -9.81f, 0);
                Debug.Log("[PlayerMovement] 重力已设置为 -9.81");
            }
            
            currentHeight = standHeight;
            if (capsuleCollider != null)
            {
                capsuleCollider.height = standHeight;
                capsuleCollider.center = new Vector3(0, standHeight / 2, 0);
            }
        }

        private void Update()
        {
            HandleInput();
            CheckGround();
            HandleJumpBuffer();
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleJump();
            HandleCrouch();
            ApplyBetterGravity();
        }

        private void HandleInput()
        {
            Vector2 moveInput = input.MoveInput;
            
            // 计算移动方向（相对于相机）
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            // 确定目标速度
            if (input.CrouchPressed)
            {
                targetSpeed = crouchSpeed;
                isCrouching = true;
                isSprinting = false;
            }
            else if (input.SprintPressed && moveInput.magnitude > 0.1f && !isCrouching)
            {
                targetSpeed = sprintSpeed;
                isSprinting = true;
                isCrouching = false;
            }
            else
            {
                targetSpeed = walkSpeed;
                isSprinting = false;
                isCrouching = false;
            }
        }

        private void HandleMovement()
        {
            if (actionController != null && actionController.IsMovementLocked)
            {
                currentVelocity = Vector3.zero;
                rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
                return;
            }

            if (moveDirection.magnitude > 0.1f)
            {
                // 平滑加速
                currentVelocity = Vector3.MoveTowards(currentVelocity, 
                    moveDirection * targetSpeed, acceleration * Time.fixedDeltaTime);

                // 旋转朝向移动方向
                if (actionController == null || !actionController.IsRotationLocked)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 
                        rotationSpeed * Time.fixedDeltaTime));
                }
            }
            else
            {
                // 平滑减速
                currentVelocity = Vector3.MoveTowards(currentVelocity, 
                    Vector3.zero, deceleration * Time.fixedDeltaTime);
            }

            // 应用速度（保持Y轴速度）
            rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);
        }

        private void HandleJumpBuffer()
        {
            if (actionController != null && actionController.CurrentState != PlayerActionState.Locomotion)
            {
                if (input.JumpPressed)
                {
                    suppressJumpUntilRelease = true;
                }
                jumpBufferTimer = 0f;
                return;
            }

            if (combat != null && combat.IsAttacking)
            {
                if (input.JumpPressed)
                {
                    suppressJumpUntilRelease = true;
                }
                jumpBufferTimer = 0f;
                return;
            }

            if (!input.JumpHeld)
            {
                suppressJumpUntilRelease = false;
            }

            // 土狼时间 - 离开地面后短时间内仍可跳跃
            if (isGrounded)
            {
                coyoteTimeTimer = coyoteTime;
            }
            else if (coyoteTimeTimer > 0)
            {
                coyoteTimeTimer -= Time.deltaTime;
            }

            // 跳跃缓冲 - 在落地前按空格可以立即跳跃
            if (input.JumpPressed)
            {
                if (!suppressJumpUntilRelease && (isGrounded || coyoteTimeTimer > 0f) && !isCrouching)
                {
                    jumpBufferTimer = jumpBufferTime;
                }
                else
                {
                    suppressJumpUntilRelease = true;
                }
            }
            else if (jumpBufferTimer > 0)
            {
                jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void HandleJump()
        {
            if (actionController != null && actionController.CurrentState != PlayerActionState.Locomotion)
            {
                return;
            }

            if (combat != null && combat.IsAttacking)
            {
                return;
            }

            bool canJumpNow = (isGrounded || coyoteTimeTimer > 0) && !isCrouching;
            
            // 如果有跳跃缓冲且可以跳跃
            if (jumpBufferTimer > 0 && canJumpNow)
            {
                PerformJump();
                jumpBufferTimer = 0;
                coyoteTimeTimer = 0;
            }
        }

        private void PerformJump()
        {
            isJumping = true;
            
            // 使用速度计算跳跃（更精确）
            // v = sqrt(2 * g * h)
            float jumpVelocity = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            
            // 触发动画
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Jump");
            }
        }

        private void ApplyBetterGravity()
        {
            // 始终应用重力（当不在地面时）
            if (!isGrounded)
            {
                if (rb.velocity.y < 0)
                {
                    // 下落时应用更强的重力
                    float downwardForce = Physics.gravity.y * (fallMultiplier - 1);
                    rb.AddForce(Vector3.up * downwardForce, ForceMode.Acceleration);
                }
                else if (rb.velocity.y > 0 && !input.JumpPressed)
                {
                    // 跳跃按键松开后应用更强的重力（低跳跃）
                    float downwardForce = Physics.gravity.y * (lowJumpMultiplier - 1);
                    rb.AddForce(Vector3.up * downwardForce, ForceMode.Acceleration);
                }
                
                // 限制最大下落速度
                if (rb.velocity.y < maxFallSpeed)
                {
                    rb.velocity = new Vector3(rb.velocity.x, maxFallSpeed, rb.velocity.z);
                }
            }
        }

        private void CheckGround()
        {
            wasGrounded = isGrounded;
            
            if (groundCheck == null)
            {
                // 如果没有groundCheck，使用射线检测
                isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, 
                    Vector3.down, groundCheckDistance + 0.1f, groundLayer);
            }
            else
            {
                // 使用球体检测（更可靠）
                isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
                
                // 如果球体检测失败，尝试射线检测
                if (!isGrounded)
                {
                    isGrounded = Physics.Raycast(groundCheck.position, 
                        Vector3.down, groundCheckDistance, groundLayer);
                }
            }

            // 着陆时重置跳跃状态
            if (isGrounded && !wasGrounded)
            {
                isJumping = false;
            }
        }

        private void HandleCrouch()
        {
            float targetHeight = isCrouching ? crouchHeight : standHeight;
            
            if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
            {
                currentHeight = Mathf.Lerp(currentHeight, targetHeight, 
                    crouchTransitionSpeed * Time.fixedDeltaTime);
                
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = currentHeight;
                    capsuleCollider.center = new Vector3(0, currentHeight / 2, 0);
                }
            }
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;
            if (animator.runtimeAnimatorController == null) return;

            float horizontalSpeed = currentVelocity.magnitude;
            if (rb != null)
            {
                Vector3 velocity = rb.velocity;
                velocity.y = 0f;
                horizontalSpeed = velocity.magnitude;
            }

            if (isGrounded && moveDirection.magnitude > 0.1f && horizontalSpeed < 0.1f)
            {
                horizontalSpeed = targetSpeed;
            }

            animator.SetFloat("Speed", horizontalSpeed / sprintSpeed);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsCrouching", isCrouching);
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制地面检测范围
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 checkPos = groundCheck != null ? groundCheck.position : transform.position;
            Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
            
            // 绘制射线检测
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(checkPos, Vector3.down * groundCheckDistance);
        }
    }
}
