using System.Collections.Generic;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 有限状态机
    /// </summary>
    public class FSMStateMachine<T> where T : class
    {
        // 当前状态
        private IState<T> currentState;
        // 状态机的拥有者
        private T owner;
        // 存储状态实例的字典
        private Dictionary<Type, IState<T>> stateCache = new Dictionary<Type, IState<T>>();

        public FSMStateMachine(T owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 设置默认状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void SetDefault<TState>() where TState : IState<T>, new()
        {
            var stateType = typeof(TState);
            if (!stateCache.TryGetValue(stateType, out var defaultState))
            {
                defaultState = new TState();
                // 如果需要初始化 owner，可在这里处理
                if (defaultState is StateBase<T> stateBase)
                {
                    stateBase.Init(owner);
                }
                stateCache[stateType] = defaultState;
            }
            currentState = defaultState;
            currentState?.OnEnter(this);
        }

        /// <summary>
        /// 设置默认状态
        /// </summary>
        /// <param name="defaultState">默认状态</param>
        public void SetDefault(IState<T> defaultState)
        {
            // 自动初始化 owner
            if (defaultState is StateBase<T> stateBase)
            {
                stateBase.Init(owner);
            }
            currentState = defaultState;
            currentState?.OnEnter(this);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        public void ChangeState<TState>() where TState : IState<T>, new()
        {
            var stateType = typeof(TState);
            if (currentState != null && currentState.GetType() == stateType) return;

            if (!stateCache.TryGetValue(stateType, out var newState))
            {
                newState = new TState();
                if (newState is StateBase<T> stateBase)
                {
                    stateBase.Init(owner);
                }
                stateCache[stateType] = newState;
            }

            currentState?.OnExit(this);
            currentState = newState;
            currentState.OnEnter(this);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="newState">新状态</param>
        public void ChangeState(IState<T> newState)
        {
            if (currentState == newState) return;
            if (newState == null) return;

            // 自动初始化 owner
            if (newState is StateBase<T> stateBase)
            {
                stateBase.Init(owner);
            }

            currentState?.OnExit(this);
            currentState = newState;
            currentState.OnEnter(this);
        }

        // 手动调用更新
        public void Update() => currentState?.OnUpdate(this);
        // 固定更新
        public void FixedUpdate() => currentState?.OnFixedUpdate(this);
        // 延迟更新
        public void LateUpdate() => currentState?.OnLateUpdate(this);
        // 获取当前状态类型（用于调试）
        public Type GetCurrentStateType() => currentState?.GetType();
    }
}