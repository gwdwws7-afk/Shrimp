using UnityEngine;

namespace ThirdPersonController
{
    public static class Utilities
    {
        /// <summary>
        /// 执行 Is In Sector 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static bool IsInSector(Vector3 origin, Vector3 direction, Vector3 targetPosition, 
            float maxDistance, float sectorAngle)
        {
            Vector3 toTarget = targetPosition - origin;
            float distance = toTarget.magnitude;

            if (distance > maxDistance)
                return false;

            toTarget.Normalize();
            float angle = Vector3.Angle(direction, toTarget);

            return angle <= sectorAngle * 0.5f;
        }

        /// <summary>
        /// 执行 Smooth Damp Angle 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, 
            float smoothTime)
        {
            float delta = Mathf.DeltaAngle(current, target);
            target = current + delta;
            return Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime);
        }

        /// <summary>
        /// 获取Ground Height，集中读取当前状态，减少外部耦合。
        /// </summary>
        public static bool GetGroundHeight(Vector3 position, out float height, float maxDistance = 100f, 
            LayerMask groundLayer = default)
        {
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 
                maxDistance + 100f, groundLayer))
            {
                height = hit.point.y;
                return true;
            }

            height = position.y;
            return false;
        }

        /// <summary>
        /// 获取Slope Angle，集中读取当前状态，减少外部耦合。
        /// </summary>
        public static float GetSlopeAngle(Vector3 position, Vector3 direction, float checkDistance = 0.5f, 
            LayerMask groundLayer = default)
        {
            if (Physics.Raycast(position, direction, out RaycastHit hit, checkDistance, groundLayer))
            {
                return Vector3.Angle(hit.normal, Vector3.up);
            }

            return 0f;
        }

        /// <summary>
        /// 获取Bezier Point，集中读取当前状态，减少外部耦合。
        /// </summary>
        public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * p0 + 2f * oneMinusT * t * p1 + t * t * p2;
        }

        /// <summary>
        /// 执行 Flatten Vector 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static Vector3 FlattenVector(Vector3 vector)
        {
            vector.y = 0;
            return vector.normalized;
        }

        /// <summary>
        /// 执行 Angle Difference 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float AngleDifference(float angle1, float angle2)
        {
            float diff = (angle2 - angle1 + 180) % 360 - 180;
            return diff < -180 ? diff + 360 : diff;
        }

        /// <summary>
        /// 执行 Map 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// 执行 Ease Out Cubic 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>
        /// 执行 Ease In Cubic 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// 执行 Ease In Out Cubic 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>
        /// 执行 Elastic Out 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static float ElasticOut(float t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            float p = 0.3f;
            float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        /// <summary>
        /// 执行 Draw Debug Ray 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static void DrawDebugRay(Vector3 origin, Vector3 direction, Color color, float duration = 0f)
        {
            Debug.DrawRay(origin, direction, color, duration);
        }

        /// <summary>
        /// 执行 Format Time 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int ms = Mathf.FloorToInt((seconds * 1000f) % 1000f);
            return $"{minutes:00}:{secs:00}.{ms:000}";
        }

        /// <summary>
        /// 获取Random Horizontal Direction，集中读取当前状态，减少外部耦合。
        /// </summary>
        public static Vector3 GetRandomHorizontalDirection()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        /// <summary>
        /// 执行 Is In Camera View 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public static bool IsInCameraView(Camera camera, Vector3 worldPosition)
        {
            Vector3 viewportPos = camera.WorldToViewportPoint(worldPosition);
            return viewportPos.x >= 0 && viewportPos.x <= 1 && 
                   viewportPos.y >= 0 && viewportPos.y <= 1 && 
                   viewportPos.z > 0;
        }
    }
}
