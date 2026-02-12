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
        private bool isActive;

        public bool IsActive => isActive;

        public event System.Action OnTimelineEnded;

        public void BeginTimeline(float impactDelay, float recoveryDelay, System.Action impactAction, System.Action recoveryAction)
        {
            this.impactDelay = Mathf.Max(0f, impactDelay);
            this.recoveryDelay = Mathf.Max(0f, recoveryDelay);
            this.impactAction = impactAction;
            this.recoveryAction = recoveryAction;
            impactTriggered = false;
            recoveryTriggered = false;
            isActive = true;

            if (fallbackRoutine != null)
            {
                StopCoroutine(fallbackRoutine);
            }

            fallbackRoutine = StartCoroutine(FallbackRoutine());
        }

        public void CancelTimeline(bool invokeRecovery = true)
        {
            if (!isActive)
            {
                return;
            }

            if (fallbackRoutine != null)
            {
                StopCoroutine(fallbackRoutine);
                fallbackRoutine = null;
            }

            if (invokeRecovery && !recoveryTriggered)
            {
                recoveryTriggered = true;
                recoveryAction?.Invoke();
            }

            EndTimeline();
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
            EndTimeline();
        }

        private void EndTimeline()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            impactAction = null;
            recoveryAction = null;
            OnTimelineEnded?.Invoke();
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
