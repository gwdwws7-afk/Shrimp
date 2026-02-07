using UnityEngine;
using System.Collections;

namespace ThirdPersonController
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerClimb : MonoBehaviour
    {
        [Header("Climb Detection")]
        public float climbCheckDistance = 0.6f;
        public float climbCheckHeight = 1.5f;
        public float maxClimbHeight = 3f;
        public float minClimbHeight = 0.5f;
        public LayerMask climbableLayers;

        [Header("Climb Settings")]
        public float climbSpeed = 2f;
        public float vaultSpeed = 3f;
        public float climbCooldown = 0.5f;
        public bool autoClimb = true;

        [Header("Animation")]
        public string climbAnimTrigger = "Climb";
        public string vaultAnimTrigger = "Vault";

        [Header("Debug")]
        public bool showDebugGizmos = true;

        private PlayerMovement playerMovement;
        private PlayerInputHandler input;
        private Rigidbody rb;
        private Animator animator;

        private bool isClimbing = false;
        private bool canClimb = true;
        private float climbCooldownTimer;
        private Vector3 climbTarget;
        private RaycastHit climbHit;

        public bool IsClimbing => isClimbing;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputHandler>();
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            HandleCooldowns();

            if (!isClimbing && canClimb)
            {
                CheckForClimbableSurface();
            }
        }

        private void HandleCooldowns()
        {
            if (!canClimb)
            {
                climbCooldownTimer -= Time.deltaTime;
                if (climbCooldownTimer <= 0f)
                {
                    canClimb = true;
                }
            }
        }

        private void CheckForClimbableSurface()
        {
            if (!autoClimb && !input.InteractPressed) return;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 forward = transform.forward;

            // Check for wall in front
            if (Physics.Raycast(origin, forward, out climbHit, climbCheckDistance, climbableLayers))
            {
                // Check if top of wall is reachable
                Vector3 topCheckOrigin = transform.position + Vector3.up * climbCheckHeight;
                
                // Cast forward to find the top edge
                if (Physics.Raycast(topCheckOrigin, forward, out RaycastHit topHit, climbCheckDistance, climbableLayers))
                {
                    // Wall is too high
                    return;
                }

                // Raycast down from above to find the top surface
                Vector3 downCheckOrigin = topCheckOrigin + forward * climbCheckDistance;
                if (Physics.Raycast(downCheckOrigin, Vector3.down, out RaycastHit downHit, climbCheckHeight, climbableLayers))
                {
                    float height = downHit.point.y - transform.position.y;
                    
                    if (height >= minClimbHeight && height <= maxClimbHeight)
                    {
                        // Calculate climb target
                        climbTarget = downHit.point + Vector3.up * 0.1f;
                        
                        // Check if there's enough space on top
                        Vector3 spaceCheckOrigin = climbTarget + Vector3.up * 1f;
                        if (!Physics.CheckSphere(spaceCheckOrigin, 0.4f, climbableLayers))
                        {
                            StartClimb(height);
                        }
                    }
                }
            }
        }

        private void StartClimb(float height)
        {
            isClimbing = true;
            canClimb = false;
            climbCooldownTimer = climbCooldown;

            // Disable normal movement
            playerMovement.enabled = false;
            rb.isKinematic = true;

            // Determine if it's a vault (low obstacle) or climb (high wall)
            bool isVault = height < 1.2f;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(isVault ? "Vault" : "Climb");
            }

            StartCoroutine(PerformClimb(climbTarget, isVault));
        }

        private IEnumerator PerformClimb(Vector3 targetPos, bool isVault)
        {
            Vector3 startPos = transform.position;
            float climbDuration = isVault ? 1f / vaultSpeed : 1f / climbSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < climbDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / climbDuration;
                
                // Smooth interpolation with easing
                t = Mathf.SmoothStep(0, 1, t);
                
                // Move to target position
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                
                // Add some arc to the movement for more natural feel
                if (!isVault)
                {
                    float heightOffset = Mathf.Sin(t * Mathf.PI) * 0.3f;
                    currentPos.y += heightOffset;
                }

                transform.position = currentPos;
                
                // Rotate to face the wall during climb
                Vector3 lookDir = -climbHit.normal;
                lookDir.y = 0;
                if (lookDir.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, 
                        Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                }

                yield return null;
            }

            // Ensure final position
            transform.position = targetPos;

            // Re-enable movement
            isClimbing = false;
            rb.isKinematic = false;
            playerMovement.enabled = true;
            rb.velocity = Vector3.zero;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Draw climb detection rays
            Gizmos.color = Color.blue;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawRay(origin, transform.forward * climbCheckDistance);

            Vector3 topOrigin = transform.position + Vector3.up * climbCheckHeight;
            Gizmos.DrawRay(topOrigin, transform.forward * climbCheckDistance);

            Gizmos.color = Color.green;
            Vector3 downOrigin = topOrigin + transform.forward * climbCheckDistance;
            Gizmos.DrawRay(downOrigin, Vector3.down * climbCheckHeight);

            // Draw climb target
            if (isClimbing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(climbTarget, 0.2f);
            }
        }
    }
}
