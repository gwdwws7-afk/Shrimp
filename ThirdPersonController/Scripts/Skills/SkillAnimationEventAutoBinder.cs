using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class SkillAnimationEventAutoBinder : MonoBehaviour
    {
        [System.Serializable]
        public class ClipBinding
        {
            public string clipNameContains;
            public float impactDelay;
            public float recoveryDelay;
        }

        public Animator animator;
        public bool bindOnStart = true;
        public List<ClipBinding> bindings = new List<ClipBinding>();

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            if (bindOnStart)
            {
                BindAnimationEvents();
            }
        }

        public void BindAnimationEvents()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                ClipBinding binding = bindings[i];
                if (binding == null || string.IsNullOrEmpty(binding.clipNameContains))
                {
                    continue;
                }

                for (int c = 0; c < clips.Length; c++)
                {
                    AnimationClip clip = clips[c];
                    if (clip == null)
                    {
                        continue;
                    }

                    if (!clip.name.Contains(binding.clipNameContains))
                    {
                        continue;
                    }

                    TryAddEvents(clip, binding.impactDelay, binding.recoveryDelay);
                }
            }
        }

        private void TryAddEvents(AnimationClip clip, float impactDelay, float recoveryDelay)
        {
            float impactTime = Mathf.Clamp(impactDelay, 0f, clip.length);
            float recoveryTime = Mathf.Clamp(impactDelay + recoveryDelay, 0f, clip.length);

            List<AnimationEvent> events = new List<AnimationEvent>(clip.events ?? new AnimationEvent[0]);
            bool hasImpact = HasEvent(events, "SkillImpactEvent");
            bool hasRecovery = HasEvent(events, "SkillRecoveryEvent");

            if (!hasImpact)
            {
                events.Add(new AnimationEvent
                {
                    functionName = "SkillImpactEvent",
                    time = impactTime
                });
            }

            if (!hasRecovery)
            {
                events.Add(new AnimationEvent
                {
                    functionName = "SkillRecoveryEvent",
                    time = recoveryTime
                });
            }

            try
            {
                clip.events = events.ToArray();
            }
            catch
            {
                // Imported clips might be read-only; ignore in runtime.
            }
        }

        private bool HasEvent(List<AnimationEvent> events, string functionName)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].functionName == functionName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
