using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 基础状态机类，可用于玩家或敌人的状态管理
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
    /// 状态基类
    /// </summary>
    public abstract class State<T> where T : StateMachine<T>
    {
        public abstract void EnterState(T owner);
        public abstract void UpdateState(T owner);
        public abstract void FixedUpdateState(T owner);
        public abstract void ExitState(T owner);
    }
}
