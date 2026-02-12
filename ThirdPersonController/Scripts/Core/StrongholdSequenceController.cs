using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class StrongholdSequenceController : MonoBehaviour
    {
        public List<StrongholdController> strongholds = new List<StrongholdController>();
        public bool autoStartFirst = true;

        private int currentIndex = -1;

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
                ActivateNextStronghold();
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
