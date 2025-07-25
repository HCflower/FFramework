using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 高性能有限状态机
    /// 支持状态缓存、条件转换、异步状态切换等特性
    /// </summary>
    public class FSMStateMachine : IDisposable
    {
        #region 私有字段

        private IState currentState;
        private IState previousState;

        // 状态缓存池，避免频繁创建销毁状态对象
        private readonly Dictionary<Type, IState> stateCache = new Dictionary<Type, IState>();

        // 状态转换表，提高转换查找性能
        private readonly Dictionary<Type, Dictionary<Type, Func<bool>>> transitionTable = new Dictionary<Type, Dictionary<Type, Func<bool>>>();

        // 全局转换条件（任意状态都可以转换的条件）
        private readonly Dictionary<Type, Func<bool>> globalTransitions = new Dictionary<Type, Func<bool>>();

        // 性能统计
        private float lastStateChangeTime;
        private int stateChangeCount;

        // 状态机是否正在运行
        private bool isRunning = true;

        // 延迟状态切换队列（避免在Update中直接切换状态）
        private IState pendingState;
        private bool hasPendingStateChange;

        #endregion

        #region 公共属性

        /// <summary>当前状态</summary>
        public IState CurrentState => currentState;

        /// <summary>上一个状态</summary>
        public IState PreviousState => previousState;

        /// <summary>状态机是否正在运行</summary>
        public bool IsRunning => isRunning;

        /// <summary>状态切换次数</summary>
        public int StateChangeCount => stateChangeCount;

        /// <summary>最后一次状态切换时间</summary>
        public float LastStateChangeTime => lastStateChangeTime;

        #endregion

        #region 核心方法

        /// <summary>
        /// 启动状态机并设置初始状态
        /// </summary>
        public void Start<T>() where T : IState, new()
        {
            Start(GetOrCreateState<T>());
        }

        /// <summary>
        /// 启动状态机并设置初始状态
        /// </summary>
        public void Start(IState initialState)
        {
            if (initialState == null)
            {
                Debug.LogError("FSMStateMachine: 初始状态不能为空");
                return;
            }

            isRunning = true;
            currentState = initialState;
            currentState.OnEnter();
            lastStateChangeTime = Time.time;
            stateChangeCount = 1;
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Stop()
        {
            if (currentState != null)
            {
                currentState.OnExit();
                previousState = currentState;
                currentState = null;
            }
            isRunning = false;
        }

        /// <summary>
        /// 暂停状态机
        /// </summary>
        public void Pause()
        {
            isRunning = false;
        }

        /// <summary>
        /// 恢复状态机
        /// </summary>
        public void Resume()
        {
            isRunning = true;
        }

        /// <summary>
        /// 直接切换状态（立即执行）
        /// </summary>
        public bool ChangeState<T>() where T : IState, new()
        {
            return ChangeState(GetOrCreateState<T>());
        }

        /// <summary>
        /// 直接切换状态（立即执行）
        /// </summary>
        public bool ChangeState(IState newState)
        {
            if (!isRunning || newState == null || newState == currentState)
                return false;

            return InternalChangeState(newState);
        }

        /// <summary>
        /// 延迟切换状态（在下一帧执行，避免在Update中切换状态导致的问题）
        /// </summary>
        public void ChangeStateDeferred<T>() where T : IState, new()
        {
            ChangeStateDeferred(GetOrCreateState<T>());
        }

        /// <summary>
        /// 延迟切换状态（在下一帧执行）
        /// </summary>
        public void ChangeStateDeferred(IState newState)
        {
            if (!isRunning || newState == null || newState == currentState)
                return;

            pendingState = newState;
            hasPendingStateChange = true;
        }

        /// <summary>
        /// 回到上一个状态
        /// </summary>
        public bool GoToPreviousState()
        {
            return previousState != null && ChangeState(previousState);
        }

        #endregion

        #region 更新方法

        /// <summary>
        /// 主更新方法
        /// </summary>
        public void Update()
        {
            if (!isRunning) return;

            // 处理延迟状态切换
            ProcessPendingStateChange();

            // 检查自动转换条件
            CheckAutoTransitions();

            // 更新当前状态
            currentState?.OnUpdate();
        }

        /// <summary>
        /// 固定更新
        /// </summary>
        public void FixedUpdate()
        {
            if (!isRunning) return;
            currentState?.OnFixedUpdate();
        }

        /// <summary>
        /// 延迟更新
        /// </summary>
        public void LateUpdate()
        {
            if (!isRunning) return;
            currentState?.OnLateUpdate();
        }

        #endregion

        #region 状态转换配置

        /// <summary>
        /// 添加状态转换条件
        /// </summary>
        public void AddTransition<TFrom, TTo>(Func<bool> condition)
            where TFrom : IState
            where TTo : IState
        {
            Type fromType = typeof(TFrom);
            Type toType = typeof(TTo);

            if (!transitionTable.ContainsKey(fromType))
                transitionTable[fromType] = new Dictionary<Type, Func<bool>>();

            transitionTable[fromType][toType] = condition;
        }

        /// <summary>
        /// 添加全局转换条件（任意状态都可以转换）
        /// </summary>
        public void AddGlobalTransition<TTo>(Func<bool> condition) where TTo : IState
        {
            globalTransitions[typeof(TTo)] = condition;
        }

        /// <summary>
        /// 移除状态转换条件
        /// </summary>
        public void RemoveTransition<TFrom, TTo>()
            where TFrom : IState
            where TTo : IState
        {
            Type fromType = typeof(TFrom);
            Type toType = typeof(TTo);

            if (transitionTable.ContainsKey(fromType))
                transitionTable[fromType].Remove(toType);
        }

        /// <summary>
        /// 移除全局转换条件
        /// </summary>
        public void RemoveGlobalTransition<TTo>() where TTo : IState
        {
            globalTransitions.Remove(typeof(TTo));
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或创建状态实例（使用缓存池）
        /// </summary>
        private T GetOrCreateState<T>() where T : IState, new()
        {
            Type stateType = typeof(T);

            if (!stateCache.ContainsKey(stateType))
                stateCache[stateType] = new T();

            return (T)stateCache[stateType];
        }

        /// <summary>
        /// 内部状态切换逻辑
        /// </summary>
        private bool InternalChangeState(IState newState)
        {
            if (currentState == newState) return false;

            try
            {
                // 退出当前状态
                currentState?.OnExit();

                // 记录状态切换
                previousState = currentState;
                currentState = newState;
                lastStateChangeTime = Time.time;
                stateChangeCount++;

                // 进入新状态
                currentState?.OnEnter();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"FSMStateMachine: 状态切换时发生错误: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理延迟状态切换
        /// </summary>
        private void ProcessPendingStateChange()
        {
            if (hasPendingStateChange && pendingState != null)
            {
                InternalChangeState(pendingState);
                pendingState = null;
                hasPendingStateChange = false;
            }
        }

        /// <summary>
        /// 检查自动转换条件
        /// </summary>
        private void CheckAutoTransitions()
        {
            if (currentState == null) return;

            Type currentStateType = currentState.GetType();

            // 检查全局转换条件
            foreach (var globalTransition in globalTransitions)
            {
                if (globalTransition.Value != null && globalTransition.Value.Invoke())
                {
                    var targetState = GetStateFromCache(globalTransition.Key);
                    if (targetState != null && targetState != currentState)
                    {
                        ChangeStateDeferred(targetState);
                        return;
                    }
                }
            }

            // 检查当前状态的转换条件
            if (transitionTable.ContainsKey(currentStateType))
            {
                foreach (var transition in transitionTable[currentStateType])
                {
                    if (transition.Value != null && transition.Value.Invoke())
                    {
                        var targetState = GetStateFromCache(transition.Key);
                        if (targetState != null)
                        {
                            ChangeStateDeferred(targetState);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从缓存中获取状态实例
        /// </summary>
        private IState GetStateFromCache(Type stateType)
        {
            stateCache.TryGetValue(stateType, out IState state);
            return state;
        }

        #endregion

        #region 调试和工具方法

        /// <summary>
        /// 获取当前状态类型（用于调试）
        /// </summary>
        public Type GetCurrentStateType()
        {
            return currentState?.GetType();
        }

        /// <summary>
        /// 获取当前状态名称
        /// </summary>
        public string GetCurrentStateName()
        {
            return currentState?.GetType().Name ?? "None";
        }

        /// <summary>
        /// 检查是否在指定状态
        /// </summary>
        public bool IsInState<T>() where T : IState
        {
            return currentState != null && currentState.GetType() == typeof(T);
        }

        /// <summary>
        /// 检查是否可以转换到指定状态
        /// </summary>
        public bool CanTransitionTo<T>() where T : IState
        {
            if (currentState == null) return true;

            Type currentType = currentState.GetType();
            Type targetType = typeof(T);

            // 检查是否有转换条件定义
            if (transitionTable.ContainsKey(currentType))
                return transitionTable[currentType].ContainsKey(targetType);

            // 检查全局转换
            return globalTransitions.ContainsKey(targetType);
        }

        /// <summary>
        /// 获取状态机统计信息
        /// </summary>
        public string GetStatistics()
        {
            return $"当前状态: {GetCurrentStateName()}, " +
                   $"状态切换次数: {stateChangeCount}, " +
                   $"缓存状态数: {stateCache.Count}, " +
                   $"运行时间: {(Time.time - lastStateChangeTime):F2}s";
        }

        /// <summary>
        /// 清理状态缓存
        /// </summary>
        public void ClearStateCache()
        {
            foreach (var state in stateCache.Values)
            {
                if (state is IDisposable disposable)
                    disposable.Dispose();
            }
            stateCache.Clear();
        }

        #endregion

        #region IDisposable 实现

        private bool disposed = false;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 停止状态机
                    Stop();

                    // 清理状态缓存
                    ClearStateCache();

                    // 清理转换表
                    transitionTable.Clear();
                    globalTransitions.Clear();
                }
                disposed = true;
            }
        }

        ~FSMStateMachine()
        {
            Dispose(false);
        }

        #endregion
    }
}