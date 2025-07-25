using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 状态基类，提供常用功能和性能优化(
    /// </summary>)
    public abstract class BaseState : IState, IDisposable
    {
        #region 生命周期标记

        /// <summary>状态是否已进入</summary>
        protected bool isEntered = false;

        /// <summary>状态进入时间</summary>
        protected float enterTime;

        /// <summary>状态持续时间</summary>
        public float StateDuration => isEntered ? Time.time - enterTime : 0f;

        #endregion

        #region 状态机引用

        /// <summary>状态机引用</summary>
        protected FSMStateMachine stateMachine;

        /// <summary>
        /// 设置状态机引用
        /// </summary>
        public virtual void SetStateMachine(FSMStateMachine fsm)
        {
            stateMachine = fsm;
        }

        #endregion

        #region IState 实现

        /// <summary>
        /// 进入状态
        /// </summary>
        public virtual void OnEnter()
        {
            isEntered = true;
            enterTime = Time.time;

            OnEnterState();
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public virtual void OnUpdate()
        {
            if (!isEntered) return;

            OnUpdateState();
        }

        /// <summary>
        /// 固定更新
        /// </summary>
        public virtual void OnFixedUpdate()
        {
            if (!isEntered) return;

            OnFixedUpdateState();
        }

        /// <summary>
        /// 延迟更新
        /// </summary>
        public virtual void OnLateUpdate()
        {
            if (!isEntered) return;

            OnLateUpdateState();
        }

        /// <summary>
        /// 退出状态
        /// </summary>
        public virtual void OnExit()
        {
            OnExitState();

            isEntered = false;
        }

        #endregion

        #region 抽象方法（子类必须实现）

        /// <summary>
        /// 进入状态时的具体逻辑
        /// </summary>
        protected abstract void OnEnterState();

        /// <summary>
        /// 状态更新时的具体逻辑
        /// </summary>
        protected abstract void OnUpdateState();

        /// <summary>
        /// 退出状态时的具体逻辑
        /// </summary>
        protected abstract void OnExitState();

        #endregion

        #region 虚方法（子类可选择重写）

        /// <summary>
        /// 固定更新时的具体逻辑
        /// </summary>
        protected virtual void OnFixedUpdateState() { }

        /// <summary>
        /// 延迟更新时的具体逻辑
        /// </summary>
        protected virtual void OnLateUpdateState() { }

        #endregion

        #region 便利方法

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        protected void ChangeState<T>() where T : IState, new()
        {
            stateMachine?.ChangeState<T>();
        }

        /// <summary>
        /// 延迟切换到指定状态
        /// </summary>
        protected void ChangeStateDeferred<T>() where T : IState, new()
        {
            stateMachine?.ChangeStateDeferred<T>();
        }

        /// <summary>
        /// 检查是否在指定状态
        /// </summary>
        protected bool IsInState<T>() where T : IState
        {
            return stateMachine?.IsInState<T>() ?? false;
        }

        /// <summary>
        /// 回到上一个状态
        /// </summary>
        protected bool GoToPreviousState()
        {
            return stateMachine?.GoToPreviousState() ?? false;
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
                    OnDispose();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// 状态释放时的清理逻辑
        /// </summary>
        protected virtual void OnDispose() { }

        ~BaseState()
        {
            Dispose(false);
        }

        #endregion

        #region 调试

        /// <summary>
        /// 获取状态信息
        /// </summary>
        public virtual string GetStateInfo()
        {
            return $"{GetType().Name} - 持续时间: {StateDuration:F2}s, 已进入: {isEntered}";
        }

        #endregion
    }
}
