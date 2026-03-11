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
        [Tooltip("说明")]
        public float jumpHeight = 2f;
        [Tooltip("说明")]
        public float jumpBufferTime = 0.2f;
        [Tooltip("土狼时间 - 离开地面后仍可跳跃的时间")]
        public float coyoteTime = 0.15f;
        [Tooltip("下落重力倍率（越大下落越快）")]
        public float fallMultiplier = 5f;
        [Tooltip("说明")]
        public float lowJumpMultiplier = 3f;
        [Tooltip("鏈€澶т笅钀介€熷害")]
        public float maxFallSpeed = -20f;

        [Header("Ground Check")]
        public Transform groundCheck;
        [Tooltip("说明")]
        public float groundCheckRadius = 0.4f;
        [Tooltip("鍦伴潰妫€娴嬭窛绂伙紙浠庤剼搴曞悜涓嬶級")]
        public float groundCheckDistance = 0.2f;
        public LayerMask groundLayer;

        [Header("Crouch Settings")]
        public float crouchHeight = 1f;
        public float standHeight = 1.8f;
        public float crouchTransitionSpeed = 10f;
        public LayerMask standBlockLayers = ~0;

        [Header("Animation")]
        public float speedDampTime = 0.12f;
        public float speedStopThreshold = 0.15f;

        [Header("Status Effects")]
        public float minSlowMultiplier = 0.2f;

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

// 时序参数，用于控制触发节奏并防止状态抖动。
        private float jumpBufferTimer;
        private float coyoteTimeTimer;
        
        private Vector3 moveDirection;
        private Vector3 currentVelocity;
        private float targetSpeed;
        private float currentHeight;
        private float externalSpeedMultiplier = 1f;
        private float externalSpeedTimer = 0f;
        private readonly Collider[] standUpHits = new Collider[8];

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
            
// 围绕 currentHeight 执行该步骤，用于保持上下文语义一致。
            
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
            UpdateExternalSpeed();
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
            if (input == null)
            {
                return;
            }

            Vector2 moveInput = input.MoveInput;
            
// 围绕 镜头 执行该步骤，用于保持上下文语义一致。
            Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
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
                isSprinting = false;
                if (isCrouching && !CanStandUp())
                {
                    targetSpeed = crouchSpeed;
                    isCrouching = true;
                }
                else
                {
                    targetSpeed = walkSpeed;
                    isCrouching = false;
                }
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

            float effectiveSpeed = targetSpeed * externalSpeedMultiplier;
            if (moveDirection.magnitude > 0.1f)
            {
// 围绕 currentVelocity 执行该步骤，用于保证流程状态与后续分支一致。
                currentVelocity = Vector3.MoveTowards(currentVelocity, 
                    moveDirection * effectiveSpeed, acceleration * Time.fixedDeltaTime);

// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
                if (actionController == null || !actionController.IsRotationLocked)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 
                        rotationSpeed * Time.fixedDeltaTime));
                }
            }
            else
            {
// 围绕 currentVelocity 执行该步骤，用于保证流程状态与后续分支一致。
                currentVelocity = Vector3.MoveTowards(currentVelocity, 
                    Vector3.zero, deceleration * Time.fixedDeltaTime);
            }

// 围绕 rb 执行该步骤，用于保证流程状态与后续分支一致。
            rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);
        }

        private void UpdateExternalSpeed()
        {
            if (externalSpeedTimer > 0f)
            {
                externalSpeedTimer -= Time.deltaTime;
                if (externalSpeedTimer <= 0f)
                {
                    externalSpeedMultiplier = 1f;
                }
            }
        }

        public void ApplyMoveSlow(float multiplier, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            float clamped = Mathf.Clamp(multiplier, minSlowMultiplier, 1f);
            externalSpeedMultiplier = Mathf.Min(externalSpeedMultiplier, clamped);
            externalSpeedTimer = Mathf.Max(externalSpeedTimer, duration);
        }

        private void HandleJumpBuffer()
        {
            if (input == null)
            {
                jumpBufferTimer = 0f;
                return;
            }

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

// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (isGrounded)
            {
                coyoteTimeTimer = coyoteTime;
            }
            else if (coyoteTimeTimer > 0)
            {
                coyoteTimeTimer -= Time.deltaTime;
            }

// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 float 执行该步骤，用于保证流程状态与后续分支一致。
// 围绕 float 执行该步骤，用于保证流程状态与后续分支一致。
            float jumpVelocity = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Jump");
            }
        }

        private void ApplyBetterGravity()
        {
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (!isGrounded)
            {
                if (rb.velocity.y < 0)
                {
// 围绕 float 执行该步骤，用于保持上下文语义一致。
                    float downwardForce = Physics.gravity.y * (fallMultiplier - 1);
                    rb.AddForce(Vector3.up * downwardForce, ForceMode.Acceleration);
                }
                else if (rb.velocity.y > 0 && !input.JumpHeld)
                {
// 围绕 float 执行该步骤，用于保持上下文语义一致。
                    float downwardForce = Physics.gravity.y * (lowJumpMultiplier - 1);
                    rb.AddForce(Vector3.up * downwardForce, ForceMode.Acceleration);
                }
                
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
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
// 围绕 isGrounded 执行该步骤，用于保证流程状态与后续分支一致。
                isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, 
                    Vector3.down, groundCheckDistance + 0.1f, groundLayer);
            }
            else
            {
// 围绕 isGrounded 执行该步骤，用于保证流程状态与后续分支一致。
                isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
                
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
                if (!isGrounded)
                {
                    isGrounded = Physics.Raycast(groundCheck.position, 
                        Vector3.down, groundCheckDistance, groundLayer);
                }
            }

// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (isGrounded && !wasGrounded)
            {
                isJumping = false;
            }
        }

        private bool CanStandUp()
        {
            if (capsuleCollider == null)
            {
                return true;
            }

            float radius = Mathf.Max(0.01f, capsuleCollider.radius * 0.95f);
            Vector3 bottom = transform.position + Vector3.up * radius;
            Vector3 top = transform.position + Vector3.up * Mathf.Max(radius, standHeight - radius);
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                standUpHits,
                standBlockLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = standUpHits[i];
                if (hit == null)
                {
                    continue;
                }

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
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

            if (moveDirection.magnitude <= 0.1f && horizontalSpeed <= speedStopThreshold)
            {
                horizontalSpeed = 0f;
            }

            float normalizedSpeed = sprintSpeed > 0f ? horizontalSpeed / sprintSpeed : 0f;
            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

            if (isGrounded && moveDirection.magnitude > 0.1f)
            {
                float instantSpeed = sprintSpeed > 0f ? targetSpeed / sprintSpeed : normalizedSpeed;
                animator.SetFloat("Speed", Mathf.Clamp01(instantSpeed));
            }
            else
            {
                animator.SetFloat("Speed", normalizedSpeed, speedDampTime, Time.deltaTime);
            }
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsCrouching", isCrouching);
        }

        private void OnDrawGizmosSelected()
        {
// 围绕 Gizmos 执行该步骤，用于保持上下文语义一致。
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 checkPos = groundCheck != null ? groundCheck.position : transform.position;
            Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
            
// 围绕 Gizmos 执行该步骤，用于保持上下文语义一致。
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(checkPos, Vector3.down * groundCheckDistance);
        }
    }
}

