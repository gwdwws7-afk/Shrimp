using UnityEngine;

namespace ThirdPersonController
{
    public enum PlayerActionState
    {
        Locomotion,
        Attack,
        Block,
        Dodge,
        Skill,
        Hit,
        Dead
    }

    public enum ActionPriority
    {
        Low = 0,
        Attack = 10,
        Block = 20,
        Skill = 25,
        Dodge = 30,
        Hit = 40,
        Dead = 100
    }

    [System.Flags]
    public enum ActionInterruptMask
    {
        None = 0,
        Attack = 1 << 0,
        Block = 1 << 1,
        Dodge = 1 << 2,
        Skill = 1 << 3,
        Hit = 1 << 4,
        Dead = 1 << 5,
        All = Attack | Block | Dodge | Skill | Hit | Dead
    }

    public class PlayerActionController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private PlayerActionState currentState = PlayerActionState.Locomotion;
        [SerializeField] private ActionPriority currentPriority = ActionPriority.Low;

        [Header("Safety")]
        [SerializeField] private bool enableStateWatchdog = true;
        [SerializeField] private bool logStateTransitions = false;
        [SerializeField] private float watchdogGraceSeconds = 0.25f;
        [SerializeField] private float attackFallbackDuration = 2f;
        [SerializeField] private float dodgeFallbackDuration = 1.5f;
        [SerializeField] private float skillFallbackDuration = 3f;
        [SerializeField] private float hitFallbackDuration = 1.25f;

        private float stateTimer = 0f;
        private float stateStartTime = 0f;
        private float currentStateMinDuration = 0f;
        private bool autoReturnToLocomotion = false;
        private bool lockMovement = false;
        private bool lockRotation = false;
        private bool interruptible = true;
        private ActionInterruptMask interruptMask = ActionInterruptMask.All;

        public PlayerActionState CurrentState => currentState;
        public bool IsMovementLocked => lockMovement;
        public bool IsRotationLocked => lockRotation;
        public float StateElapsedTime => Time.unscaledTime - stateStartTime;

        public event System.Action<PlayerActionState, PlayerActionState> OnStateChanged;
        public event System.Action<PlayerActionState, PlayerActionState> OnActionInterrupted;

        private void Update()
        {
            if (stateTimer > 0f)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f && autoReturnToLocomotion)
                {
                    ReturnToLocomotion("timer");
                }
            }

            RunStateWatchdog();
        }

        public bool CanStartAction(PlayerActionState state)
        {
            if (state == currentState)
            {
                return state != PlayerActionState.Dead;
            }

            if (state == PlayerActionState.Locomotion)
            {
                return currentState != PlayerActionState.Dead;
            }

            if (currentState == PlayerActionState.Dead)
            {
                return false;
            }

            if (state == PlayerActionState.Dead)
            {
                return true;
            }

            if (!IsTransitionAllowedByMatrix(currentState, state))
            {
                return false;
            }

            if (currentState == PlayerActionState.Locomotion)
            {
                return true;
            }

            if (!interruptible)
            {
                return false;
            }

            return IsInterruptAllowed(state);
        }

        public bool TryStartAction(
            PlayerActionState state,
            ActionPriority priority,
            float minDuration,
            bool lockMove,
            bool lockRot,
            bool autoReturn,
            bool allowInterrupt,
            ActionInterruptMask allowedInterrupts,
            bool forceInterrupt = false)
        {
            if (currentState == PlayerActionState.Dead && state != PlayerActionState.Dead)
            {
                return false;
            }

            if (!forceInterrupt && !CanStartAction(state))
            {
                return false;
            }

            bool isInterruptTransition = currentState != PlayerActionState.Locomotion && currentState != state;
            if (isInterruptTransition && !forceInterrupt)
            {
                if (priority <= currentPriority)
                {
                    return false;
                }

                if (!IsInterruptAllowed(state))
                {
                    return false;
                }
            }

            PlayerActionState interruptedState = currentState;
            SetState(state, priority, minDuration, lockMove, lockRot, autoReturn, allowInterrupt, allowedInterrupts);

            if (isInterruptTransition)
            {
                OnActionInterrupted?.Invoke(interruptedState, state);
            }

            return true;
        }

        public void EndAction(PlayerActionState state)
        {
            if (currentState != state)
            {
                return;
            }

            ReturnToLocomotion("end-action");
        }

        private void SetState(
            PlayerActionState state,
            ActionPriority priority,
            float minDuration,
            bool lockMove,
            bool lockRot,
            bool autoReturn,
            bool canInterrupt,
            ActionInterruptMask allowedInterrupts)
        {
            PlayerActionState previousState = currentState;

            currentState = state;
            stateStartTime = Time.unscaledTime;
            currentStateMinDuration = Mathf.Max(0f, minDuration);

            if (state == PlayerActionState.Locomotion)
            {
                currentPriority = ActionPriority.Low;
                stateTimer = 0f;
                autoReturnToLocomotion = false;
                lockMovement = false;
                lockRotation = false;
                interruptible = true;
                interruptMask = ActionInterruptMask.All;
            }
            else
            {
                currentPriority = priority;
                stateTimer = currentStateMinDuration;
                autoReturnToLocomotion = autoReturn;
                lockMovement = lockMove;
                lockRotation = lockRot;
                interruptible = canInterrupt;
                interruptMask = allowedInterrupts;
            }

            if (previousState != currentState)
            {
                if (logStateTransitions)
                {
                    Debug.Log($"[Action] {previousState} -> {currentState} | prio={currentPriority} lockM={lockMovement} lockR={lockRotation} timer={stateTimer:0.00}");
                }

                OnStateChanged?.Invoke(previousState, currentState);
            }
        }

        private void ReturnToLocomotion(string reason)
        {
            if (currentState == PlayerActionState.Dead)
            {
                return;
            }

            if (currentState == PlayerActionState.Locomotion)
            {
                // Defensive cleanup in case external code mutated lock flags.
                lockMovement = false;
                lockRotation = false;
                stateTimer = 0f;
                autoReturnToLocomotion = false;
                currentPriority = ActionPriority.Low;
                interruptible = true;
                interruptMask = ActionInterruptMask.All;
                return;
            }

            if (logStateTransitions)
            {
                Debug.Log($"[Action] {currentState} -> Locomotion ({reason})");
            }

            SetState(PlayerActionState.Locomotion, ActionPriority.Low, 0f, false, false, false, true, ActionInterruptMask.All);
        }

        private void RunStateWatchdog()
        {
            if (!enableStateWatchdog)
            {
                return;
            }

            if (currentState == PlayerActionState.Locomotion
                || currentState == PlayerActionState.Block
                || currentState == PlayerActionState.Dead)
            {
                return;
            }

            float fallback = GetFallbackDuration(currentState);
            if (fallback <= 0f)
            {
                return;
            }

            float limit = Mathf.Max(currentStateMinDuration + Mathf.Max(0f, watchdogGraceSeconds), fallback);
            if (StateElapsedTime <= limit)
            {
                return;
            }

            Debug.LogWarning($"[Action] Watchdog recovered state {currentState} after {StateElapsedTime:0.00}s (limit {limit:0.00}s).");
            ReturnToLocomotion("watchdog-timeout");
        }

        private float GetFallbackDuration(PlayerActionState state)
        {
            switch (state)
            {
                case PlayerActionState.Attack:
                    return attackFallbackDuration;
                case PlayerActionState.Dodge:
                    return dodgeFallbackDuration;
                case PlayerActionState.Skill:
                    return skillFallbackDuration;
                case PlayerActionState.Hit:
                    return hitFallbackDuration;
                default:
                    return 0f;
            }
        }

        private bool IsTransitionAllowedByMatrix(PlayerActionState from, PlayerActionState to)
        {
            if (from == to)
            {
                return true;
            }

            switch (from)
            {
                case PlayerActionState.Locomotion:
                    return to == PlayerActionState.Attack
                        || to == PlayerActionState.Block
                        || to == PlayerActionState.Dodge
                        || to == PlayerActionState.Skill
                        || to == PlayerActionState.Hit
                        || to == PlayerActionState.Dead;

                case PlayerActionState.Attack:
                    return to == PlayerActionState.Attack
                        || to == PlayerActionState.Block
                        || to == PlayerActionState.Dodge
                        || to == PlayerActionState.Skill
                        || to == PlayerActionState.Hit
                        || to == PlayerActionState.Locomotion
                        || to == PlayerActionState.Dead;

                case PlayerActionState.Block:
                    return to == PlayerActionState.Dodge
                        || to == PlayerActionState.Hit
                        || to == PlayerActionState.Locomotion
                        || to == PlayerActionState.Dead;

                case PlayerActionState.Dodge:
                    return to == PlayerActionState.Hit
                        || to == PlayerActionState.Locomotion
                        || to == PlayerActionState.Dead;

                case PlayerActionState.Skill:
                    return to == PlayerActionState.Block
                        || to == PlayerActionState.Dodge
                        || to == PlayerActionState.Hit
                        || to == PlayerActionState.Locomotion
                        || to == PlayerActionState.Dead;

                case PlayerActionState.Hit:
                    return to == PlayerActionState.Hit
                        || to == PlayerActionState.Locomotion
                        || to == PlayerActionState.Dead;

                case PlayerActionState.Dead:
                    return to == PlayerActionState.Dead;

                default:
                    return false;
            }
        }

        private bool IsInterruptAllowed(PlayerActionState state)
        {
            ActionInterruptMask stateMask = GetMaskForState(state);
            return (interruptMask & stateMask) != 0;
        }

        private ActionInterruptMask GetMaskForState(PlayerActionState state)
        {
            switch (state)
            {
                case PlayerActionState.Attack:
                    return ActionInterruptMask.Attack;
                case PlayerActionState.Block:
                    return ActionInterruptMask.Block;
                case PlayerActionState.Dodge:
                    return ActionInterruptMask.Dodge;
                case PlayerActionState.Skill:
                    return ActionInterruptMask.Skill;
                case PlayerActionState.Hit:
                    return ActionInterruptMask.Hit;
                case PlayerActionState.Dead:
                    return ActionInterruptMask.Dead;
                default:
                    return ActionInterruptMask.None;
            }
        }
    }
}
