using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// StateMachine 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public abstract class StateMachine<T> : MonoBehaviour where T : StateMachine<T>
    {
        protected State<T> currentState;

        public State<T> CurrentState => currentState;

        protected virtual void Update()
        {
            currentState?.UpdateState((T)this);
        }

        protected virtual void FixedUpdate()
        {
            currentState?.FixedUpdateState((T)this);
        }

        public virtual void ChangeState(State<T> newState)
        {
            if (currentState != null)
            {
                currentState.ExitState((T)this);
            }

            currentState = newState;

            if (currentState != null)
            {
                currentState.EnterState((T)this);
            }
        }
    }

    /// <summary>
    /// StateMachine 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public abstract class State<T> where T : StateMachine<T>
    {
        public abstract void EnterState(T owner);
        public abstract void UpdateState(T owner);
        public abstract void FixedUpdateState(T owner);
        public abstract void ExitState(T owner);
    }
}
