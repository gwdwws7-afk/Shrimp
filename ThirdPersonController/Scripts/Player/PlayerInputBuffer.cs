using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum BufferedActionType
    {
        Attack,
        Block,
        Dodge,
        Skill
    }

    public struct BufferedActionEntry
    {
        public BufferedActionType action;
        public int index;
        public float expiresAt;
        public bool hasDirection;
        public Vector3 direction;
    }

    public class PlayerInputBuffer : MonoBehaviour
    {
        [Header("Buffer Times")]
        public float attackBufferTime = 0.3f;
        public float blockBufferTime = 0.25f;
        public float dodgeBufferTime = 0.25f;
        public float skillBufferTime = 0.3f;

        [Header("Clear Rules")]
        public bool clearOnHit = true;
        public bool clearOnDead = true;
        public bool clearOnDodge = true;

        private readonly List<BufferedActionEntry> bufferedActions = new List<BufferedActionEntry>();
        private PlayerActionController actionController;

        private void Awake()
        {
            actionController = GetComponent<PlayerActionController>();
            if (actionController != null)
            {
                actionController.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (actionController != null)
            {
                actionController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            PruneExpired();
        }

        public void BufferAction(BufferedActionType action, float durationOverride = -1f, int index = -1, Vector3? direction = null)
        {
            float duration = durationOverride > 0f ? durationOverride : GetDefaultBufferTime(action);
            if (duration <= 0f)
            {
                return;
            }

            float expiresAt = Time.time + duration;
            int existingIndex = FindEntryIndex(action, index);
            if (existingIndex >= 0)
            {
                BufferedActionEntry entry = bufferedActions[existingIndex];
                entry.expiresAt = expiresAt;
                if (direction.HasValue)
                {
                    entry.hasDirection = true;
                    entry.direction = direction.Value;
                }
                bufferedActions[existingIndex] = entry;
                return;
            }

            BufferedActionEntry newEntry = new BufferedActionEntry
            {
                action = action,
                index = index,
                expiresAt = expiresAt,
                hasDirection = direction.HasValue,
                direction = direction ?? default
            };

            bufferedActions.Add(newEntry);
        }

        public bool HasAction(BufferedActionType action, int index = -1)
        {
            PruneExpired();
            return FindEntryIndex(action, index) >= 0;
        }

        public bool TryGet(BufferedActionType action, out BufferedActionEntry entry, int index = -1)
        {
            PruneExpired();
            int foundIndex = FindEntryIndex(action, index);
            if (foundIndex < 0)
            {
                entry = default;
                return false;
            }

            entry = bufferedActions[foundIndex];
            return true;
        }

        public bool TryConsume(BufferedActionType action, out BufferedActionEntry entry, int index = -1)
        {
            PruneExpired();
            int foundIndex = FindEntryIndex(action, index);
            if (foundIndex < 0)
            {
                entry = default;
                return false;
            }

            entry = bufferedActions[foundIndex];
            bufferedActions.RemoveAt(foundIndex);
            return true;
        }

        public void ClearAction(BufferedActionType action, int index = -1)
        {
            if (bufferedActions.Count == 0)
            {
                return;
            }

            for (int i = bufferedActions.Count - 1; i >= 0; i--)
            {
                BufferedActionEntry entry = bufferedActions[i];
                if (entry.action != action)
                {
                    continue;
                }
                if (index >= 0 && entry.index != index)
                {
                    continue;
                }
                bufferedActions.RemoveAt(i);
            }
        }

        public void ClearAll()
        {
            bufferedActions.Clear();
        }

        private void PruneExpired()
        {
            if (bufferedActions.Count == 0)
            {
                return;
            }

            float now = Time.time;
            for (int i = bufferedActions.Count - 1; i >= 0; i--)
            {
                if (bufferedActions[i].expiresAt <= now)
                {
                    bufferedActions.RemoveAt(i);
                }
            }
        }

        private int FindEntryIndex(BufferedActionType action, int index)
        {
            for (int i = 0; i < bufferedActions.Count; i++)
            {
                BufferedActionEntry entry = bufferedActions[i];
                if (entry.action != action)
                {
                    continue;
                }
                if (index >= 0 && entry.index != index)
                {
                    continue;
                }
                return i;
            }
            return -1;
        }

        private float GetDefaultBufferTime(BufferedActionType action)
        {
            switch (action)
            {
                case BufferedActionType.Attack:
                    return attackBufferTime;
                case BufferedActionType.Block:
                    return blockBufferTime;
                case BufferedActionType.Dodge:
                    return dodgeBufferTime;
                case BufferedActionType.Skill:
                    return skillBufferTime;
                default:
                    return 0f;
            }
        }

        private void HandleStateChanged(PlayerActionState previousState, PlayerActionState newState)
        {
            if ((clearOnHit && newState == PlayerActionState.Hit)
                || (clearOnDead && newState == PlayerActionState.Dead)
                || (clearOnDodge && newState == PlayerActionState.Dodge))
            {
                ClearAll();
            }
        }
    }
}
