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

        private int currentIndex = -1;

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
            for (int i = 0; i < strongholds.Count; i++)
            {
                if (strongholds[i] == null)
                {
                    continue;
                }

                strongholds[i].SetActive(false);
                strongholds[i].OnStrongholdCompleted += HandleStrongholdCompleted;
            }
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
            for (int i = 0; i < strongholds.Count; i++)
            {
                if (strongholds[i] == null)
                {
                    continue;
                }

                strongholds[i].OnStrongholdCompleted -= HandleStrongholdCompleted;
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
