using UnityEngine;

namespace ThirdPersonController
{
    public class EnemyWaveMember : MonoBehaviour
    {
        private StrongholdController owner;
        private int waveIndex;
        private bool isElite;
        private bool hasReported;

        public void Initialize(StrongholdController stronghold, int wave, bool elite)
        {
            owner = stronghold;
            waveIndex = wave;
            isElite = elite;
            hasReported = false;
        }

        private void OnDisable()
        {
            ReportDestroyed();
        }

        private void OnDestroy()
        {
            ReportDestroyed();
        }

        private void ReportDestroyed()
        {
            if (hasReported)
            {
                return;
            }

            hasReported = true;
            if (owner != null)
            {
                owner.NotifyEnemyDestroyed(waveIndex, isElite);
            }
        }
    }
}
