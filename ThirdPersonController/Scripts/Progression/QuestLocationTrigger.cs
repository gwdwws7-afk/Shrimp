using UnityEngine;

namespace ThirdPersonController
{
    public class QuestLocationTrigger : MonoBehaviour
    {
        public string locationId = "";
        public bool triggerOnce = true;
        public string playerTag = "Player";

        private bool triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered && triggerOnce)
            {
                return;
            }

            if (!other.CompareTag(playerTag) && other.GetComponentInParent<PlayerCombat>() == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(locationId))
            {
                return;
            }

            triggered = true;
            GameEvents.LocationReached(locationId);
        }
    }
}
