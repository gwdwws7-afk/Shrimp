using UnityEngine;
using System.Collections;

namespace ThirdPersonController
{
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public Vector3 offset = new Vector3(0, 1.5f, 0);

        [Header("Rotation Settings")]
        public float mouseSensitivity = 3f;
        public float rotationSmoothTime = 0.1f;
        public float minVerticalAngle = -30f;
        public float maxVerticalAngle = 60f;

        [Header("Distance Settings")]
        public float defaultDistance = 5f;
        public float minDistance = 2f;
        public float maxDistance = 10f;
        public float zoomSpeed = 5f;

        [Header("Collision Settings")]
        public LayerMask collisionLayers;
        public float collisionRadius = 0.3f;
        public float collisionSmoothTime = 0.05f;

        [Header("Camera Settings")]
        public bool lockCursor = true;
        public bool invertY = false;

        private float currentYaw;
        private float currentPitch;
        private float targetYaw;
        private float targetPitch;
        private float yawVelocity;
        private float pitchVelocity;

        private float currentDistance;
        private float targetDistance;
        private float distanceVelocity;

        private Camera cam;
        private PlayerInputHandler input;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (target != null)
            {
                input = target.GetComponent<PlayerInputHandler>();
            }

            currentDistance = defaultDistance;
            targetDistance = defaultDistance;

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            CalculateRotation();
            HandleCollision();
            UpdatePosition();
        }

        private void HandleInput()
        {
            if (input == null) return;

            Vector2 lookInput = input.LookInput;
            
            // Update target rotation based on mouse input
            targetYaw += lookInput.x * mouseSensitivity;
            
            float pitchInput = lookInput.y * mouseSensitivity * (invertY ? 1 : -1);
            targetPitch = Mathf.Clamp(targetPitch + pitchInput, minVerticalAngle, maxVerticalAngle);

            // Handle zoom with scroll wheel (传统 Input)
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                targetDistance -= scrollInput * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }

        private void CalculateRotation()
        {
            // Smoothly interpolate to target rotation
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);
        }

        private void HandleCollision()
        {
            Vector3 targetPosition = target.position + offset;
            Vector3 desiredCameraPos = CalculateCameraPosition(targetPosition, currentDistance);

            // Check for collision
            RaycastHit hit;
            Vector3 directionToCamera = (desiredCameraPos - targetPosition).normalized;
            float distanceToTarget = Vector3.Distance(targetPosition, desiredCameraPos);

            if (Physics.SphereCast(targetPosition, collisionRadius, directionToCamera, out hit, 
                distanceToTarget, collisionLayers))
            {
                // Adjust distance to avoid collision
                float adjustedDistance = hit.distance - collisionRadius;
                targetDistance = Mathf.Clamp(adjustedDistance, minDistance, maxDistance);
            }
            else
            {
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }

            // Smoothly adjust current distance
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, 
                ref distanceVelocity, collisionSmoothTime);
        }

        private void UpdatePosition()
        {
            Vector3 targetPosition = target.position + offset;
            Vector3 cameraPosition = CalculateCameraPosition(targetPosition, currentDistance);

            transform.position = cameraPosition;
            transform.LookAt(targetPosition);
        }

        private Vector3 CalculateCameraPosition(Vector3 targetPos, float distance)
        {
            // Calculate camera position based on rotation and distance
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 negDistance = new Vector3(0, 0, -distance);
            return targetPos + rotation * negDistance;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                input = target.GetComponent<PlayerInputHandler>();
            }
        }

        public void ResetCamera()
        {
            if (target != null)
            {
                currentYaw = target.eulerAngles.y;
                targetYaw = currentYaw;
                currentPitch = 10f;
                targetPitch = 10f;
            }
        }

        private void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 targetPos = target.position + offset;
                Vector3 cameraPos = CalculateCameraPosition(targetPos, currentDistance);
                Gizmos.DrawLine(targetPos, cameraPos);
                Gizmos.DrawWireSphere(cameraPos, collisionRadius);
            }
        }
    }
}
