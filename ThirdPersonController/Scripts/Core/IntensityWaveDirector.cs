using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class IntensityWaveDirector : WaveSpawnDirector
    {
        [Header("Target Intensity")]
        public float targetKillsPerMinute = 120f;
        public float intensityWindowSeconds = 20f;

        [Header("Spawn Multipliers")]
        public float minCountMultiplier = 0.85f;
        public float maxCountMultiplier = 2.1f;
        public float minIntervalMultiplier = 0.45f;
        public float maxIntervalMultiplier = 1.2f;
        public float waveRampPerWave = 0.12f;
        public float maxTotalCountMultiplier = 2.4f;

        [Header("Momentum Boosts")]
        public int comboForMaxBonus = 80;
        public float comboIntensityBonus = 0.2f;
        public float musouIntensityBonus = 0.15f;

        [Header("Elite Trigger")]
        public float eliteRemainingScaleAtLow = 1.2f;
        public float eliteRemainingScaleAtHigh = 0.7f;

        [Header("Composition Profiles")]
        public List<WaveArchetypeProfile> waveProfiles = new List<WaveArchetypeProfile>();

        [Header("Event Pacing")]
        public List<WaveEventTuning> eventTunings = new List<WaveEventTuning>();

        private readonly List<float> killTimestamps = new List<float>();
        private int currentCombo;
        private bool musouActive;

        private void OnEnable()
        {
            EnsureDefaults();
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnComboCountChanged += HandleComboChanged;
            GameEvents.OnMusouStateChanged += HandleMusouChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnComboCountChanged -= HandleComboChanged;
            GameEvents.OnMusouStateChanged -= HandleMusouChanged;
        }

        private void Update()
        {
            TrimKillWindow();
        }

        public override int AdjustSpawnCount(StrongholdController controller, StrongholdWave wave, WaveSpawnGroup group,
            int waveIndex, bool isElite, int baseCount)
        {
            if (baseCount <= 0)
            {
                return 0;
            }

            float intensity = GetIntensity();
            float countMultiplier = Mathf.Lerp(minCountMultiplier, maxCountMultiplier, intensity);
            float ramp = 1f + Mathf.Max(0, waveIndex) * waveRampPerWave;
            float totalMultiplier = Mathf.Clamp(countMultiplier * ramp, minCountMultiplier, maxTotalCountMultiplier);
            float compositionMultiplier = GetArchetypeMultiplier(group, waveIndex);
            if (compositionMultiplier <= 0f)
            {
                return 0;
            }

            int adjusted = Mathf.RoundToInt(baseCount * totalMultiplier * compositionMultiplier);
            return Mathf.Max(1, adjusted);
        }

        public override int AdjustEventSpawnCount(StrongholdController controller, StrongholdWave wave, WaveEvent waveEvent,
            WaveSpawnGroup group, int waveIndex, int baseCount)
        {
            int adjusted = AdjustSpawnCount(controller, wave, group, waveIndex, false, baseCount);
            float tuningMultiplier = GetEventMultiplier(waveEvent, tuning => tuning.countMultiplier);
            return Mathf.Max(0, Mathf.RoundToInt(adjusted * tuningMultiplier));
        }

        public override float AdjustSpawnInterval(StrongholdController controller, StrongholdWave wave, WaveSpawnGroup group,
            int waveIndex, bool isElite, float baseInterval)
        {
            if (baseInterval <= 0f)
            {
                return 0f;
            }

            float intensity = GetIntensity();
            float intervalMultiplier = Mathf.Lerp(maxIntervalMultiplier, minIntervalMultiplier, intensity);
            float ramp = 1f + Mathf.Max(0, waveIndex) * waveRampPerWave;
            float adjusted = baseInterval * (intervalMultiplier / Mathf.Clamp(ramp, 1f, 1.6f));
            return Mathf.Max(0.05f, adjusted);
        }

        public override float AdjustEventSpawnInterval(StrongholdController controller, StrongholdWave wave, WaveEvent waveEvent,
            WaveSpawnGroup group, int waveIndex, float baseInterval)
        {
            float adjusted = AdjustSpawnInterval(controller, wave, group, waveIndex, false, baseInterval);
            float tuningMultiplier = GetEventMultiplier(waveEvent, tuning => tuning.intervalMultiplier);
            return Mathf.Max(0.05f, adjusted * tuningMultiplier);
        }

        public override int AdjustEliteTriggerRemaining(StrongholdController controller, StrongholdWave wave,
            int waveIndex, int baseRemaining)
        {
            if (baseRemaining <= 0)
            {
                return baseRemaining;
            }

            float intensity = GetIntensity();
            float scale = Mathf.Lerp(eliteRemainingScaleAtLow, eliteRemainingScaleAtHigh, intensity);
            return Mathf.Max(1, Mathf.RoundToInt(baseRemaining * scale));
        }

        private float GetIntensity()
        {
            float kpm = GetKillsPerMinute();
            float baseIntensity = targetKillsPerMinute <= 0f ? 0.5f : Mathf.Clamp01(kpm / targetKillsPerMinute);
            float comboFactor = comboForMaxBonus > 0 ? Mathf.Clamp01((float)currentCombo / comboForMaxBonus) : 0f;
            float comboBonus = comboFactor * comboIntensityBonus;
            float musouBonus = musouActive ? musouIntensityBonus : 0f;
            return Mathf.Clamp01(baseIntensity + comboBonus + musouBonus);
        }

        private float GetKillsPerMinute()
        {
            if (intensityWindowSeconds <= 0.1f)
            {
                return 0f;
            }

            TrimKillWindow();
            float kills = killTimestamps.Count;
            return kills / intensityWindowSeconds * 60f;
        }

        private void TrimKillWindow()
        {
            if (killTimestamps.Count == 0)
            {
                return;
            }

            float cutoff = Time.time - Mathf.Max(1f, intensityWindowSeconds);
            for (int i = killTimestamps.Count - 1; i >= 0; i--)
            {
                if (killTimestamps[i] >= cutoff)
                {
                    break;
                }

                killTimestamps.RemoveAt(i);
            }
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            killTimestamps.Add(Time.time);
        }

        private void HandleComboChanged(int combo)
        {
            currentCombo = combo;
        }

        private void HandleMusouChanged(bool active)
        {
            musouActive = active;
        }

        private void EnsureDefaults()
        {
            if (waveProfiles == null || waveProfiles.Count == 0)
            {
                waveProfiles = new List<WaveArchetypeProfile>
                {
                    new WaveArchetypeProfile
                    {
                        waveIndex = 0,
                        gruntMultiplier = 1.15f,
                        rusherMultiplier = 0.95f,
                        tankMultiplier = 0.85f,
                        eliteMultiplier = 0.9f,
                        rangedMultiplier = 0f,
                        controllerMultiplier = 0f,
                        suicideMultiplier = 0f
                    },
                    new WaveArchetypeProfile
                    {
                        waveIndex = 1,
                        gruntMultiplier = 1f,
                        rusherMultiplier = 1f,
                        tankMultiplier = 0.95f,
                        eliteMultiplier = 1f,
                        rangedMultiplier = 0.7f,
                        controllerMultiplier = 0.4f,
                        suicideMultiplier = 0f
                    },
                    new WaveArchetypeProfile
                    {
                        waveIndex = 2,
                        gruntMultiplier = 0.95f,
                        rusherMultiplier = 0.95f,
                        tankMultiplier = 1f,
                        eliteMultiplier = 1.05f,
                        rangedMultiplier = 0.8f,
                        controllerMultiplier = 0.7f,
                        suicideMultiplier = 0.5f
                    },
                    new WaveArchetypeProfile
                    {
                        waveIndex = 3,
                        gruntMultiplier = 0.9f,
                        rusherMultiplier = 0.9f,
                        tankMultiplier = 1.05f,
                        eliteMultiplier = 1.1f,
                        rangedMultiplier = 0.85f,
                        controllerMultiplier = 0.85f,
                        suicideMultiplier = 0.75f
                    }
                };
            }

            if (eventTunings == null || eventTunings.Count == 0)
            {
                eventTunings = new List<WaveEventTuning>
                {
                    new WaveEventTuning
                    {
                        eventType = WaveEventType.Reinforcement,
                        countMultiplier = 0.95f,
                        intervalMultiplier = 0.9f
                    },
                    new WaveEventTuning
                    {
                        eventType = WaveEventType.Chase,
                        countMultiplier = 0.85f,
                        intervalMultiplier = 0.8f
                    },
                    new WaveEventTuning
                    {
                        eventType = WaveEventType.HoldPoint,
                        countMultiplier = 0.9f,
                        intervalMultiplier = 1.1f
                    },
                    new WaveEventTuning
                    {
                        eventType = WaveEventType.ProtectTarget,
                        countMultiplier = 1f,
                        intervalMultiplier = 1f
                    }
                };
            }
        }

        private float GetArchetypeMultiplier(WaveSpawnGroup group, int waveIndex)
        {
            if (group == null)
            {
                return 1f;
            }

            EnemyArchetype archetype = group.archetypeOverride;
            if (archetype == null && group.prefab != null)
            {
                EnemyArchetypeConfigurator configurator = group.prefab.GetComponent<EnemyArchetypeConfigurator>();
                if (configurator != null)
                {
                    archetype = configurator.archetype;
                }
            }

            string archetypeId = archetype != null ? archetype.archetypeId : string.Empty;
            WaveArchetypeProfile profile = ResolveProfile(waveIndex);
            return profile != null ? profile.GetMultiplier(archetypeId) : 1f;
        }

        private WaveArchetypeProfile ResolveProfile(int waveIndex)
        {
            if (waveProfiles == null || waveProfiles.Count == 0)
            {
                return null;
            }

            WaveArchetypeProfile fallback = null;
            for (int i = 0; i < waveProfiles.Count; i++)
            {
                WaveArchetypeProfile profile = waveProfiles[i];
                if (profile == null)
                {
                    continue;
                }

                if (profile.waveIndex == waveIndex)
                {
                    return profile;
                }

                if (profile.waveIndex <= waveIndex)
                {
                    if (fallback == null || profile.waveIndex > fallback.waveIndex)
                    {
                        fallback = profile;
                    }
                }
            }

            return fallback;
        }

        private float GetEventMultiplier(WaveEvent waveEvent, System.Func<WaveEventTuning, float> selector)
        {
            if (waveEvent == null || eventTunings == null || selector == null)
            {
                return 1f;
            }

            for (int i = 0; i < eventTunings.Count; i++)
            {
                WaveEventTuning tuning = eventTunings[i];
                if (tuning != null && tuning.eventType == waveEvent.eventType)
                {
                    return Mathf.Max(0f, selector(tuning));
                }
            }

            return 1f;
        }
    }

    [System.Serializable]
    public class WaveArchetypeProfile
    {
        public int waveIndex = 0;
        public float gruntMultiplier = 1f;
        public float rusherMultiplier = 1f;
        public float tankMultiplier = 1f;
        public float eliteMultiplier = 1f;
        public float rangedMultiplier = 1f;
        public float controllerMultiplier = 1f;
        public float suicideMultiplier = 1f;

        public float GetMultiplier(string archetypeId)
        {
            string id = string.IsNullOrEmpty(archetypeId) ? string.Empty : archetypeId.Trim().ToLowerInvariant();
            switch (id)
            {
                case "grunt":
                    return gruntMultiplier;
                case "rusher":
                    return rusherMultiplier;
                case "tank":
                    return tankMultiplier;
                case "elite":
                    return eliteMultiplier;
                case "ranged":
                    return rangedMultiplier;
                case "controller":
                    return controllerMultiplier;
                case "suicide":
                    return suicideMultiplier;
                default:
                    return 1f;
            }
        }
    }

    [System.Serializable]
    public class WaveEventTuning
    {
        public WaveEventType eventType = WaveEventType.Reinforcement;
        public float countMultiplier = 1f;
        public float intervalMultiplier = 1f;
    }
}
