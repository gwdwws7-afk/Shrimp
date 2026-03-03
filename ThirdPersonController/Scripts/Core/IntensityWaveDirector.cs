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

        private readonly List<float> killTimestamps = new List<float>();
        private int currentCombo;
        private bool musouActive;

        private void OnEnable()
        {
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
            int adjusted = Mathf.RoundToInt(baseCount * totalMultiplier);
            return Mathf.Max(1, adjusted);
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
    }
}
