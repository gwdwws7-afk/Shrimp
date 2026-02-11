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
                    SetState(PlayerActionState.Locomotion, ActionPriority.Low, 0f, false, false, true, false);
                }
            }
        }

        public bool CanStartAction(PlayerActionState state)
        {
            if (currentState == PlayerActionState.Dead)
            {
                return false;
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
            bool allowInterrupt)
        {
            if (!CanStartAction(state))
            {
                return false;
            }

            if (currentState != PlayerActionState.Locomotion && currentState != state)
            {
                if (!allowInterrupt || !interruptible || priority <= currentPriority)
                {
                    return false;
                }

                PlayerActionState interruptedState = currentState;
                SetState(state, priority, minDuration, lockMove, lockRot, autoReturn, allowInterrupt);
                OnActionInterrupted?.Invoke(interruptedState, state);
                return true;
            }

            SetState(state, priority, minDuration, lockMove, lockRot, autoReturn, allowInterrupt);
            return true;
        }

        public void EndAction(PlayerActionState state)
        {
            if (currentState != state)
            {
                return;
            }

            SetState(PlayerActionState.Locomotion, ActionPriority.Low, 0f, false, false, true, false);
        }

        private void SetState(
            PlayerActionState state,
            ActionPriority priority,
            float minDuration,
            bool lockMove,
            bool lockRot,
            bool autoReturn,
            bool canInterrupt)
        {
            PlayerActionState previousState = currentState;
            currentState = state;
            currentPriority = priority;
            stateTimer = Mathf.Max(0f, minDuration);
            autoReturnToLocomotion = autoReturn;
            lockMovement = lockMove;
            lockRotation = lockRot;
            interruptible = canInterrupt;

            if (previousState != currentState)
            {
                OnStateChanged?.Invoke(previousState, currentState);
            }
        }
    }
}
