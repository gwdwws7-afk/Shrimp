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

        public virtual float AdjustSpawnInterval(StrongholdController controller, StrongholdWave wave, WaveSpawnGroup group,
            int waveIndex, bool isElite, float baseInterval)
        {
            return baseInterval;
        }

        public virtual int AdjustEliteTriggerRemaining(StrongholdController controller, StrongholdWave wave,
            int waveIndex, int baseRemaining)
        {
            return baseRemaining;
        }
    }
}
