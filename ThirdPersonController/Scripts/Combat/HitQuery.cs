using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public static class HitQuery
    {
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

            Collider[] hits = Physics.OverlapSphere(center, radius, layerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
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
            if (range <= 0f && radius <= 0f)
            {
                return 0;
            }

            float searchRadius = Mathf.Max(range, radius);
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

            Collider[] hits = Physics.OverlapSphere(center, searchRadius, layerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || results.Contains(hit))
                {
                    continue;
                }

                Vector3 toTarget = hit.bounds.center - center;
                Vector3 flatToTarget = Flatten(toTarget);
                float distance = flatToTarget.magnitude;
                if (distance <= 0.001f || distance > range)
                {
                    continue;
                }

                float angleToTarget = Vector3.Angle(flatForward, flatToTarget / distance);
                if (angleToTarget > angle * 0.5f)
                {
                    continue;
                }

                if (obstructionMask != 0)
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
            RaycastHit[] hits = Physics.BoxCastAll(from, halfExtents, direction, Quaternion.LookRotation(direction),
                distance, layerMask);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
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
    }
}
