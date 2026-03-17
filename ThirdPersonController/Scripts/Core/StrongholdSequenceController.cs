using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class StrongholdSequenceController : MonoBehaviour
    {
        public List<StrongholdController> strongholds = new List<StrongholdController>();
        public bool autoStartFirst = true;

        [Header("Completion")]
        public bool triggerLevelCompleteOnFinish = true;
        public bool triggerVictoryOnFinish = true;
        public int levelId = 1;

        [Header("Boss Gate")]
        public bool deferCompletionUntilBoss = false;
        public BossSpawnPoint bossSpawnPoint;

        private int currentIndex = -1;
        private bool waitingForBoss;
        private bool strongholdsCompleted;
        private bool bossDefeated;
        private bool levelCompleted;

        public StrongholdController ActiveStronghold
        {
            get
            {
                if (currentIndex < 0 || currentIndex >= strongholds.Count)
                {
                    return null;
                }

                return strongholds[currentIndex];
            }
        }

        private void Awake()
        {
            BindStrongholds(true);
            BindBoss(true);
        }

        private void Start()
        {
            if (autoStartFirst)
            {
                ActivateNextStronghold();
            }
        }

        private void OnDestroy()
        {
            BindStrongholds(false);
            BindBoss(false);
        }

        public void ConfigureStrongholds(List<StrongholdController> newStrongholds)
        {
            BindStrongholds(false);
            strongholds = newStrongholds ?? new List<StrongholdController>();
            currentIndex = -1;
            BindStrongholds(true);
        }

        public void ConfigureBossGate(bool deferUntilBoss, BossSpawnPoint spawnPoint)
        {
            BindBoss(false);
            deferCompletionUntilBoss = deferUntilBoss;
            bossSpawnPoint = spawnPoint;
            waitingForBoss = false;
            BindBoss(true);
        }

        private void BindStrongholds(bool bind)
        {
            if (strongholds == null)
            {
                strongholds = new List<StrongholdController>();
                return;
            }

            for (int i = 0; i < strongholds.Count; i++)
            {
                StrongholdController stronghold = strongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                if (bind)
                {
                    stronghold.SetActive(false);
                    stronghold.OnStrongholdCompleted += HandleStrongholdCompleted;
                }
                else
                {
                    stronghold.OnStrongholdCompleted -= HandleStrongholdCompleted;
                }
            }
        }

        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            if (stronghold == null)
            {
                return;
            }

            if (currentIndex >= 0 && currentIndex < strongholds.Count && strongholds[currentIndex] == stronghold)
            {
                if (currentIndex >= strongholds.Count - 1)
                {
                    HandleSequenceCompleted();
                }
                else
                {
                    ActivateNextStronghold();
                }
            }
        }

        private void HandleSequenceCompleted()
        {
            strongholdsCompleted = true;
            if (deferCompletionUntilBoss && bossSpawnPoint != null)
            {
                if (bossDefeated || bossSpawnPoint.IsDefeated)
                {
                    CompleteLevel();
                    return;
                }

                StartBossGate();
                return;
            }

            CompleteLevel();
        }

        private void StartBossGate()
        {
            if (waitingForBoss)
            {
                return;
            }

            waitingForBoss = true;
            bossSpawnPoint.SpawnBoss();
        }

        private void HandleBossDefeated(BossSpawnPoint spawnPoint)
        {
            bossDefeated = true;
            waitingForBoss = false;
            if (deferCompletionUntilBoss && !strongholdsCompleted)
            {
                return;
            }

            CompleteLevel();
        }

        private void BindBoss(bool bind)
        {
            if (bossSpawnPoint != null)
            {
                if (bind)
                {
                    bossSpawnPoint.OnBossDefeated += HandleBossDefeated;
                    bossDefeated = bossSpawnPoint.IsDefeated;
                }
                else
                {
                    bossSpawnPoint.OnBossDefeated -= HandleBossDefeated;
                }
            }
        }

        private void CompleteLevel()
        {
            if (levelCompleted)
            {
                return;
            }

            levelCompleted = true;
            if (triggerLevelCompleteOnFinish)
            {
                GameEvents.LevelCompleted(levelId);
            }

            if (triggerVictoryOnFinish)
            {
                GameEvents.GameOver(true);
            }
        }

        public void ActivateNextStronghold()
        {
            int nextIndex = currentIndex + 1;
            if (nextIndex < 0 || nextIndex >= strongholds.Count)
            {
                return;
            }

            ActivateStronghold(nextIndex);
        }

        public void ActivateStronghold(int index)
        {
            if (index < 0 || index >= strongholds.Count)
            {
                return;
            }

            if (currentIndex >= 0 && currentIndex < strongholds.Count)
            {
                StrongholdController current = strongholds[currentIndex];
                if (current != null)
                {
                    current.SetActive(false);
                }
            }

            StrongholdController target = strongholds[index];
            if (target != null)
            {
                target.SetActive(true);
            }

            currentIndex = index;
        }
    }
}
