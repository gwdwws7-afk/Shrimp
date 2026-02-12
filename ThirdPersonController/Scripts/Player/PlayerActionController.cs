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

        private float stateTimer = 0f;
        private bool autoReturnToLocomotion = false;
        private bool lockMovement = false;
        private bool lockRotation = false;
        private bool interruptible = true;
        private ActionInterruptMask interruptMask = ActionInterruptMask.All;

        public PlayerActionState CurrentState => currentState;
        public bool IsMovementLocked => lockMovement;
        public bool IsRotationLocked => lockRotation;

        public event System.Action<PlayerActionState, PlayerActionState> OnStateChanged;
        public event System.Action<PlayerActionState, PlayerActionState> OnActionInterrupted;

        private void Update()
        {
            if (stateTimer > 0f)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f && autoReturnToLocomotion)
                {
                    SetState(PlayerActionState.Locomotion, ActionPriority.Low, 0f, false, false, true, false, ActionInterruptMask.All);
                }
            }
        }

        public bool CanStartAction(PlayerActionState state)
        {
            if (currentState == PlayerActionState.Dead)
            {
                return false;
            }

            if (state == PlayerActionState.Dead)
            {
                return true;
            }

            if (state == PlayerActionState.Hit)
            {
                return true;
            }

            if (state == PlayerActionState.Attack)
            {
                return currentState == PlayerActionState.Locomotion || currentState == PlayerActionState.Attack;
            }

            if (state == PlayerActionState.Block)
            {
                return currentState == PlayerActionState.Locomotion || currentState == PlayerActionState.Attack;
            }

            if (state == PlayerActionState.Dodge)
            {
                return currentState != PlayerActionState.Dodge && currentState != PlayerActionState.Dead;
            }

            if (state == PlayerActionState.Skill)
            {
                return currentState == PlayerActionState.Locomotion || currentState == PlayerActionState.Attack;
            }

            if (state == PlayerActionState.Hit)
            {
                return currentState != PlayerActionState.Dead;
            }

            return currentState == PlayerActionState.Locomotion;
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
            if (!forceInterrupt && !CanStartAction(state))
            {
                return false;
            }

            if (currentState == PlayerActionState.Dead && state != PlayerActionState.Dead)
            {
                return false;
            }

            if (currentState != PlayerActionState.Locomotion && currentState != state)
            {
                if (!forceInterrupt)
                {
                    if (!allowInterrupt || !interruptible || priority <= currentPriority)
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
                OnActionInterrupted?.Invoke(interruptedState, state);
                return true;
            }

            SetState(state, priority, minDuration, lockMove, lockRot, autoReturn, allowInterrupt, allowedInterrupts);
            return true;
        }

        public void EndAction(PlayerActionState state)
        {
            if (currentState != state)
            {
                return;
            }

            SetState(PlayerActionState.Locomotion, ActionPriority.Low, 0f, false, false, true, false, ActionInterruptMask.All);
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
            currentPriority = priority;
            stateTimer = Mathf.Max(0f, minDuration);
            autoReturnToLocomotion = autoReturn;
            lockMovement = lockMove;
            lockRotation = lockRot;
            interruptible = canInterrupt;
            interruptMask = allowedInterrupts;

            if (previousState != currentState)
            {
                OnStateChanged?.Invoke(previousState, currentState);
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
