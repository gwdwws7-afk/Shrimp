using UnityEngine;

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
        public float gamepadZoomSpeed = 8f;
        [Range(0f, 0.95f)]
        public float gamepadZoomDeadzone = 0.15f;
        public bool invertGamepadZoom = false;

        [Header("Auto Recenter")]
        public bool autoRecenter = true;
        public float recenterDelay = 1.2f;
        public float recenterYawSpeed = 120f;
        public float recenterPitch = 10f;
        public float targetReacquireInterval = 0.5f;

        [Header("Gamepad Look")]
        [Range(0f, 0.95f)]
        public float gamepadLookDeadzone = 0.15f;
        [Range(1f, 3f)]
        public float gamepadLookExponent = 1.6f;
        public float gamepadYawSpeed = 220f;
        public float gamepadPitchSpeed = 180f;

        [Header("Collision Settings")]
        public LayerMask collisionLayers;
        public float collisionRadius = 0.3f;
        public float collisionSmoothTime = 0.05f;
        public float occlusionEnterSmoothTime = 0.03f;
        public float occlusionExitSmoothTime = 0.15f;
        public float occlusionReleaseDelay = 0.08f;
        public float occlusionDistanceDeadband = 0.02f;

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
        private float lookIdleTimer;
        private float targetSearchTimer;
        private float occlusionReleaseTimer;
        private bool isOccluded;

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
            if (target == null)
            {
                TryRecoverTarget();
                return;
            }

            if (input == null)
            {
                input = target.GetComponent<PlayerInputHandler>();
            }

            HandleInput();
            ApplyAutoRecenter();
            CalculateRotation();
            HandleCollision();
            UpdatePosition();
        }

        private void HandleInput()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null) return;

            Vector2 lookInput = GetLookDelta(handler);
            if (!IsFinite(lookInput.x) || !IsFinite(lookInput.y))
            {
                lookInput = Vector2.zero;
            }

            if (lookInput.sqrMagnitude > 0.000001f)
            {
                lookIdleTimer = 0f;
            }
            else
            {
                lookIdleTimer += Time.unscaledDeltaTime;
            }

            targetYaw += lookInput.x;

            float pitchInput = lookInput.y * (invertY ? 1 : -1);
            targetPitch = Mathf.Clamp(targetPitch + pitchInput, minVerticalAngle, maxVerticalAngle);

            float scrollInput = handler.ReadMouseScrollDelta();
            float zoomDelta = -scrollInput * zoomSpeed;

            float triggerAxisRaw = handler.ReadGamepadZoomAxis();
            if (Mathf.Abs(triggerAxisRaw) > 0.00001f)
            {
                float triggerAxis = triggerAxisRaw;
                triggerAxis = ApplySignedDeadzone(triggerAxis, gamepadZoomDeadzone);
                if (invertGamepadZoom)
                {
                    triggerAxis = -triggerAxis;
                }

                zoomDelta += triggerAxis * gamepadZoomSpeed * Time.unscaledDeltaTime;
            }

            if (Mathf.Abs(zoomDelta) > 0.00001f)
            {
                targetDistance = Mathf.Clamp(targetDistance + zoomDelta, minDistance, maxDistance);
            }
        }

        private void CalculateRotation()
        {
            if (!IsFinite(targetYaw))
            {
                targetYaw = currentYaw;
            }

            if (!IsFinite(targetPitch))
            {
                targetPitch = currentPitch;
            }

            // Smoothly interpolate to target rotation
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

            if (!IsFinite(currentYaw))
            {
                currentYaw = targetYaw;
            }

            if (!IsFinite(currentPitch))
            {
                currentPitch = targetPitch;
            }
        }

        private void HandleCollision()
        {
            if (!IsFinite(currentDistance))
            {
                currentDistance = defaultDistance;
            }

            if (!IsFinite(targetDistance))
            {
                targetDistance = defaultDistance;
            }

            float desiredDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            Vector3 targetPosition = target.position + offset;
            Vector3 desiredCameraPos = CalculateCameraPosition(targetPosition, desiredDistance);

            // Check for collision
            RaycastHit hit;
            Vector3 directionVector = desiredCameraPos - targetPosition;
            Vector3 directionToCamera = directionVector.sqrMagnitude > 0.0001f ? directionVector.normalized : Vector3.back;
            float distanceToTarget = Vector3.Distance(targetPosition, desiredCameraPos);
            float resolvedDistance = desiredDistance;
            bool hitOcclusion = Physics.SphereCast(targetPosition, collisionRadius, directionToCamera, out hit,
                distanceToTarget, collisionLayers);

            if (hitOcclusion)
            {
                // Adjust distance to avoid collision
                float adjustedDistance = hit.distance - collisionRadius;
                resolvedDistance = Mathf.Clamp(adjustedDistance, minDistance, maxDistance);
                occlusionReleaseTimer = occlusionReleaseDelay;
                isOccluded = true;
            }
            else
            {
                if (occlusionReleaseTimer > 0f)
                {
                    occlusionReleaseTimer -= Time.unscaledDeltaTime;
                    resolvedDistance = Mathf.Min(currentDistance, desiredDistance);
                }
                else
                {
                    isOccluded = false;
                }
            }

            bool useOcclusionSmoothing = hitOcclusion || occlusionReleaseTimer > 0f || isOccluded;
            float enterSmooth = Mathf.Max(0.0001f, occlusionEnterSmoothTime > 0f ? occlusionEnterSmoothTime : collisionSmoothTime);
            float exitSmooth = Mathf.Max(0.0001f, occlusionExitSmoothTime > 0f ? occlusionExitSmoothTime : collisionSmoothTime);
            float smoothTime = useOcclusionSmoothing ? enterSmooth : exitSmooth;

            currentDistance = Mathf.SmoothDamp(currentDistance, resolvedDistance, ref distanceVelocity, smoothTime);
            if (Mathf.Abs(currentDistance - resolvedDistance) <= occlusionDistanceDeadband)
            {
                currentDistance = resolvedDistance;
            }
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
            float safePitch = SanitizeAngle(currentPitch, 0f);
            float safeYaw = SanitizeAngle(currentYaw, 0f);
            float safeDistance = SanitizeDistance(distance);
            Quaternion rotation = Quaternion.Euler(safePitch, safeYaw, 0f);
            Vector3 negDistance = new Vector3(0, 0, -safeDistance);
            return targetPos + rotation * negDistance;
        }

        private float SanitizeAngle(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private float SanitizeDistance(float value)
        {
            if (!IsFinite(value))
            {
                value = defaultDistance;
            }

            return Mathf.Clamp(value, minDistance, maxDistance);
        }

        private bool IsFinite(float value)
        {
            return !(float.IsNaN(value) || float.IsInfinity(value));
        }

        private Vector2 GetLookDelta(PlayerInputHandler handler)
        {
            if (handler == null)
            {
                return Vector2.zero;
            }

            return handler.LookInput * mouseSensitivity;
        }

        private Vector2 ApplyStickCurve(Vector2 stick)
        {
            float magnitude = stick.magnitude;
            if (magnitude <= gamepadLookDeadzone)
            {
                return Vector2.zero;
            }

            float normalized = Mathf.Clamp01((magnitude - gamepadLookDeadzone) / Mathf.Max(0.0001f, 1f - gamepadLookDeadzone));
            float curved = Mathf.Pow(normalized, gamepadLookExponent);
            return stick.normalized * curved;
        }

        private float ApplySignedDeadzone(float value, float deadzone)
        {
            float abs = Mathf.Abs(value);
            if (abs <= deadzone)
            {
                return 0f;
            }

            float normalized = Mathf.Clamp01((abs - deadzone) / Mathf.Max(0.0001f, 1f - deadzone));
            return Mathf.Sign(value) * normalized;
        }

        private void ApplyAutoRecenter()
        {
            if (!autoRecenter || target == null)
            {
                return;
            }

            if (lookIdleTimer < recenterDelay)
            {
                return;
            }

            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float desiredYaw = target.eulerAngles.y;
            targetYaw = Mathf.MoveTowardsAngle(targetYaw, desiredYaw, recenterYawSpeed * dt);
            targetPitch = Mathf.MoveTowards(targetPitch, recenterPitch, recenterYawSpeed * 0.5f * dt);
        }

        private void TryRecoverTarget()
        {
            targetSearchTimer -= Time.unscaledDeltaTime;
            if (targetSearchTimer > 0f)
            {
                return;
            }

            targetSearchTimer = Mathf.Max(0.1f, targetReacquireInterval);
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                SetTarget(taggedPlayer.transform);
                ResetCamera();
                return;
            }

            PlayerInputHandler fallbackPlayer = FindObjectOfType<PlayerInputHandler>();
            if (fallbackPlayer != null)
            {
                SetTarget(fallbackPlayer.transform);
                ResetCamera();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                input = target.GetComponent<PlayerInputHandler>();
                lookIdleTimer = 0f;
            }
        }

        private PlayerInputHandler ResolveInputHandler()
        {
            if (input != null)
            {
                return input;
            }

            if (target != null)
            {
                input = target.GetComponent<PlayerInputHandler>();
                if (input != null)
                {
                    return input;
                }
            }

            input = PlayerInputHandler.ResolveActiveInstance();
            return input;
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
