using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class HitStopManager : MonoBehaviour
    {
        [Header("HitStop")]
        public float defaultHitStopDuration = 0.06f;
        public float maxHitStopDuration = 0.12f;

        private static HitStopManager instance;
        private Coroutine hitStopRoutine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public static void Trigger(float duration)
        {
            if (instance == null)
            {
                return;
            }

            instance.ApplyHitStop(duration);
        }

        public void ApplyHitStop(float duration)
        {
            float clamped = Mathf.Clamp(duration, 0f, maxHitStopDuration);

            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
            }

            hitStopRoutine = StartCoroutine(HitStopRoutine(clamped));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            hitStopRoutine = null;
        }
    }
}
