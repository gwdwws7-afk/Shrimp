using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// Unified pooled lifetime controller for VFX objects.
    /// Keeps lifecycle allocation-free by avoiding per-spawn coroutine creation.
    /// </summary>
    public class PooledTimedDespawn : MonoBehaviour, IPoolable
    {
        private float remainingLifetime = 0f;
        private bool lifetimeArmed = false;
        private ParticleSystem[] cachedParticleSystems;

        public void Arm(float requestedLifetime)
        {
            CacheParticleSystems();
            RestartParticleSystems();

            float resolvedLifetime = requestedLifetime > 0f
                ? requestedLifetime
                : EstimateParticleLifetime();

            remainingLifetime = Mathf.Max(0.02f, resolvedLifetime);
            lifetimeArmed = true;
        }

        public void OnSpawned()
        {
            CacheParticleSystems();
            remainingLifetime = 0f;
            lifetimeArmed = false;
        }

        public void OnDespawned()
        {
            lifetimeArmed = false;
            remainingLifetime = 0f;
            StopParticleSystems();
        }

        private void Update()
        {
            if (!lifetimeArmed)
            {
                return;
            }

            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime > 0f)
            {
                return;
            }

            lifetimeArmed = false;
            ObjectPoolManager.Despawn(gameObject);
        }

        private void CacheParticleSystems()
        {
            if (cachedParticleSystems != null)
            {
                return;
            }

            cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void RestartParticleSystems()
        {
            if (cachedParticleSystems == null)
            {
                return;
            }

            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                ParticleSystem ps = cachedParticleSystems[i];
                if (ps == null)
                {
                    continue;
                }

                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void StopParticleSystems()
        {
            if (cachedParticleSystems == null)
            {
                return;
            }

            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                ParticleSystem ps = cachedParticleSystems[i];
                if (ps == null)
                {
                    continue;
                }

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private float EstimateParticleLifetime()
        {
            if (cachedParticleSystems == null || cachedParticleSystems.Length == 0)
            {
                return 1f;
            }

            float maxLifetime = 0.1f;
            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                ParticleSystem ps = cachedParticleSystems[i];
                if (ps == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = ps.main;
                float startLifetimeMax = GetStartLifetimeMax(main);
                float life = Mathf.Max(0.05f, main.duration + startLifetimeMax);
                maxLifetime = Mathf.Max(maxLifetime, life);
            }

            return maxLifetime;
        }

        private static float GetStartLifetimeMax(ParticleSystem.MainModule main)
        {
            switch (main.startLifetime.mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    return Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);
                case ParticleSystemCurveMode.TwoCurves:
                    return Mathf.Max(
                        GetCurveEndValue(main.startLifetime.curveMin),
                        GetCurveEndValue(main.startLifetime.curveMax));
                default:
                    return main.startLifetime.constant;
            }
        }

        private static float GetCurveEndValue(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            return curve.keys[curve.length - 1].value;
        }
    }
}
