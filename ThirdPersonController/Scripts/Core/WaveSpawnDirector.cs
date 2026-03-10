using UnityEngine;

namespace ThirdPersonController
{
    public class WaveSpawnDirector : MonoBehaviour
    {
        public virtual int AdjustSpawnCount(StrongholdController controller, StrongholdWave wave, WaveSpawnGroup group,
            int waveIndex, bool isElite, int baseCount)
        {
            return baseCount;
        }

        public virtual int AdjustEventSpawnCount(StrongholdController controller, StrongholdWave wave, WaveEvent waveEvent,
            WaveSpawnGroup group, int waveIndex, int baseCount)
        {
            return AdjustSpawnCount(controller, wave, group, waveIndex, false, baseCount);
        }

        public virtual float AdjustSpawnInterval(StrongholdController controller, StrongholdWave wave, WaveSpawnGroup group,
            int waveIndex, bool isElite, float baseInterval)
        {
            return baseInterval;
        }

        public virtual float AdjustEventSpawnInterval(StrongholdController controller, StrongholdWave wave, WaveEvent waveEvent,
            WaveSpawnGroup group, int waveIndex, float baseInterval)
        {
            return AdjustSpawnInterval(controller, wave, group, waveIndex, false, baseInterval);
        }

        public virtual int AdjustEliteTriggerRemaining(StrongholdController controller, StrongholdWave wave,
            int waveIndex, int baseRemaining)
        {
            return baseRemaining;
        }
    }
}
