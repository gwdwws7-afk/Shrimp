using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public static class HitQuery
    {
        private const int InitialColliderBufferSize = 128;
        private const int InitialHitBufferSize = 128;
        private const int MaxBufferSize = 4096;

        private static Collider[] colliderBuffer = new Collider[InitialColliderBufferSize];
        private static RaycastHit[] hitBuffer = new RaycastHit[InitialHitBufferSize];

        public static int OverlapSphere(Vector3 center, float radius, LayerMask layerMask, List<Collider> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (radius <= 0f)
            {
                return 0;
            }

            int hitCount = OverlapSphereNonAlloc(center, radius, layerMask);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = colliderBuffer[i];
                if (hit == null || results.Contains(hit))
                {
                    continue;
                }

                results.Add(hit);
            }

            return results.Count;
        }

        public static int OverlapCone(Vector3 center, Vector3 forward, float range, float angle,
            float radius, LayerMask layerMask, List<Collider> results, LayerMask obstructionMask)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            float clampedRange = Mathf.Max(0f, range);
            float clampedRadius = Mathf.Max(0f, radius);
            if (clampedRange <= 0f && clampedRadius <= 0f)
            {
                return 0;
            }

            float searchRadius = Mathf.Max(clampedRange, clampedRadius);
            if (searchRadius <= 0f)
            {
                return 0;
            }

            Vector3 flatForward = Flatten(forward);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.forward;
            }
            flatForward.Normalize();

            int hitCount = OverlapSphereNonAlloc(center, searchRadius, layerMask);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = colliderBuffer[i];
                if (hit == null || results.Contains(hit))
                {
                    continue;
                }

                Vector3 toTarget = hit.bounds.center - center;
                Vector3 flatToTarget = Flatten(toTarget);
                float distance = flatToTarget.magnitude;
                if (distance <= 0.001f && clampedRadius <= 0f)
                {
                    continue;
                }

                bool inRadius = clampedRadius > 0f && distance <= clampedRadius;
                bool inCone = false;
                if (clampedRange > 0f && distance <= clampedRange)
                {
                    if (angle >= 360f)
                    {
                        inCone = true;
                    }
                    else if (angle > 0f && distance > 0.001f)
                    {
                        float angleToTarget = Vector3.Angle(flatForward, flatToTarget / distance);
                        inCone = angleToTarget <= angle * 0.5f;
                    }
                }

                if (!inRadius && !inCone)
                {
                    continue;
                }

                if (obstructionMask != 0 && distance > 0.001f)
                {
                    Vector3 origin = center + Vector3.up;
                    if (Physics.Raycast(origin, flatToTarget.normalized, distance, obstructionMask))
                    {
                        continue;
                    }
                }

                results.Add(hit);
            }

            return results.Count;
        }

        public static int BoxCastPath(Vector3 from, Vector3 to, Vector3 halfExtents, LayerMask layerMask,
            List<Collider> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return 0;
            }

            direction /= distance;
            int hitCount = BoxCastNonAlloc(from, halfExtents, direction, distance, layerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hitBuffer[i].collider;
                if (hit == null || results.Contains(hit))
                {
                    continue;
                }

                results.Add(hit);
            }

            return results.Count;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static int OverlapSphereNonAlloc(Vector3 center, float radius, LayerMask layerMask)
        {
            EnsureColliderBufferCapacity(InitialColliderBufferSize);

            int hitCount;
            while (true)
            {
                hitCount = Physics.OverlapSphereNonAlloc(
                    center,
                    radius,
                    colliderBuffer,
                    layerMask,
                    QueryTriggerInteraction.UseGlobal);

                if (hitCount < colliderBuffer.Length || colliderBuffer.Length >= MaxBufferSize)
                {
                    break;
                }

                EnsureColliderBufferCapacity(Mathf.Min(colliderBuffer.Length * 2, MaxBufferSize));
            }

            return Mathf.Min(hitCount, colliderBuffer.Length);
        }

        private static int BoxCastNonAlloc(
            Vector3 origin,
            Vector3 halfExtents,
            Vector3 direction,
            float distance,
            LayerMask layerMask)
        {
            EnsureHitBufferCapacity(InitialHitBufferSize);

            Quaternion orientation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction)
                : Quaternion.identity;

            int hitCount;
            while (true)
            {
                hitCount = Physics.BoxCastNonAlloc(
                    origin,
                    halfExtents,
                    direction,
                    hitBuffer,
                    orientation,
                    distance,
                    layerMask,
                    QueryTriggerInteraction.UseGlobal);

                if (hitCount < hitBuffer.Length || hitBuffer.Length >= MaxBufferSize)
                {
                    break;
                }

                EnsureHitBufferCapacity(Mathf.Min(hitBuffer.Length * 2, MaxBufferSize));
            }

            return Mathf.Min(hitCount, hitBuffer.Length);
        }

        private static void EnsureColliderBufferCapacity(int requiredSize)
        {
            if (colliderBuffer == null)
            {
                colliderBuffer = new Collider[Mathf.Max(InitialColliderBufferSize, requiredSize)];
                return;
            }

            if (colliderBuffer.Length >= requiredSize)
            {
                return;
            }

            System.Array.Resize(ref colliderBuffer, requiredSize);
        }

        private static void EnsureHitBufferCapacity(int requiredSize)
        {
            if (hitBuffer == null)
            {
                hitBuffer = new RaycastHit[Mathf.Max(InitialHitBufferSize, requiredSize)];
                return;
            }

            if (hitBuffer.Length >= requiredSize)
            {
                return;
            }

            System.Array.Resize(ref hitBuffer, requiredSize);
        }
    }
}
