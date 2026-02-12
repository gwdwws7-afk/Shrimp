using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class SkillTimelineController : MonoBehaviour
    {
        private System.Action impactAction;
        private System.Action recoveryAction;
        private float impactDelay;
        private float recoveryDelay;
        private bool impactTriggered;
        private bool recoveryTriggered;
        private Coroutine fallbackRoutine;

        public void BeginTimeline(float impactDelay, float recoveryDelay, System.Action impactAction, System.Action recoveryAction)
        {
            this.impactDelay = Mathf.Max(0f, impactDelay);
            this.recoveryDelay = Mathf.Max(0f, recoveryDelay);
            this.impactAction = impactAction;
            this.recoveryAction = recoveryAction;
            impactTriggered = false;
            recoveryTriggered = false;

            if (fallbackRoutine != null)
            {
                StopCoroutine(fallbackRoutine);
            }

            fallbackRoutine = StartCoroutine(FallbackRoutine());
        }

        public void SkillImpactEvent()
        {
            TriggerImpact();
        }

        public void SkillRecoveryEvent()
        {
            TriggerRecovery();
        }

        private void TriggerImpact()
        {
            if (impactTriggered)
            {
                return;
            }

            impactTriggered = true;
            impactAction?.Invoke();
        }

        private void TriggerRecovery()
        {
            if (recoveryTriggered)
            {
                return;
            }

            recoveryTriggered = true;
            recoveryAction?.Invoke();
        }

        private IEnumerator FallbackRoutine()
        {
            if (impactDelay > 0f)
            {
                yield return new WaitForSeconds(impactDelay);
            }

            TriggerImpact();

            if (recoveryDelay > 0f)
            {
                yield return new WaitForSeconds(recoveryDelay);
            }

            TriggerRecovery();
            fallbackRoutine = null;
        }
    }
}
