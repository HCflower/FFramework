using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Architecture
{
    /// <summary>
    /// 游戏公共MonoBehavior - 提供全局生命周期事件和自动注销功能
    /// </summary>
    public class GameMonoBehavior : SingletonMono<GameMonoBehavior>
    {
        #region 生命周期事件
        private event Action updateEvent;
        private event Action fixedUpdateEvent;
        private event Action lateUpdateEvent;
        #endregion

        #region 自动注销管理
        private class EventInfo
        {
            public Component component;
            public Action callback;
            public int eventType; // 0=Update, 1=FixedUpdate, 2=LateUpdate

            public bool IsValid() => component != null;
        }

        private readonly List<EventInfo> eventInfos = new List<EventInfo>();
        #endregion

        #region Unity生命周期
        private void Update()
        {
            CleanupInvalidEvents();
            updateEvent?.Invoke();
        }

        private void FixedUpdate()
        {
            fixedUpdateEvent?.Invoke();
        }

        private void LateUpdate()
        {
            lateUpdateEvent?.Invoke();
        }
        #endregion

        #region 事件注册（自动注销版本）
        /// <summary>
        /// 注册Update事件（组件销毁时自动注销）
        /// </summary>
        public void RegisterUpdate(Component component, Action callback)
        {
            if (component == null || callback == null) return;

            updateEvent += callback;
            eventInfos.Add(new EventInfo { component = component, callback = callback, eventType = 0 });
        }

        /// <summary>
        /// 注册FixedUpdate事件（组件销毁时自动注销）
        /// </summary>
        public void RegisterFixedUpdate(Component component, Action callback)
        {
            if (component == null || callback == null) return;

            fixedUpdateEvent += callback;
            eventInfos.Add(new EventInfo { component = component, callback = callback, eventType = 1 });
        }

        /// <summary>
        /// 注册LateUpdate事件（组件销毁时自动注销）
        /// </summary>
        public void RegisterLateUpdate(Component component, Action callback)
        {
            if (component == null || callback == null) return;

            lateUpdateEvent += callback;
            eventInfos.Add(new EventInfo { component = component, callback = callback, eventType = 2 });
        }
        #endregion

        #region 手动注册（需要手动注销）
        /// <summary>
        /// 注册Update事件（需要手动注销）
        /// </summary>
        public void RegisterUpdate(Action callback)
        {
            if (callback != null) updateEvent += callback;
        }

        /// <summary>
        /// 注销Update事件
        /// </summary>
        public void UnRegisterUpdate(Action callback)
        {
            if (callback != null) updateEvent -= callback;
        }

        /// <summary>
        /// 注册FixedUpdate事件（需要手动注销）
        /// </summary>
        public void RegisterFixedUpdate(Action callback)
        {
            if (callback != null) fixedUpdateEvent += callback;
        }

        /// <summary>
        /// 注销FixedUpdate事件
        /// </summary>
        public void UnRegisterFixedUpdate(Action callback)
        {
            if (callback != null) fixedUpdateEvent -= callback;
        }

        /// <summary>
        /// 注册LateUpdate事件（需要手动注销）
        /// </summary>
        public void RegisterLateUpdate(Action callback)
        {
            if (callback != null) lateUpdateEvent += callback;
        }

        /// <summary>
        /// 注销LateUpdate事件
        /// </summary>
        public void UnRegisterLateUpdate(Action callback)
        {
            if (callback != null) lateUpdateEvent -= callback;
        }
        #endregion

        #region 内部实现
        /// <summary>
        /// 清理无效事件（组件被销毁时）
        /// </summary>
        private void CleanupInvalidEvents()
        {
            for (int i = eventInfos.Count - 1; i >= 0; i--)
            {
                var info = eventInfos[i];
                if (!info.IsValid())
                {
                    // 从对应事件中移除
                    switch (info.eventType)
                    {
                        case 0: updateEvent -= info.callback; break;
                        case 1: fixedUpdateEvent -= info.callback; break;
                        case 2: lateUpdateEvent -= info.callback; break;
                    }
                    eventInfos.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 清理所有事件
        /// </summary>
        public void ClearAllEvents()
        {
            updateEvent = null;
            fixedUpdateEvent = null;
            lateUpdateEvent = null;
            eventInfos.Clear();
        }
        #endregion

        protected override void OnDestroy()
        {
            ClearAllEvents();
        }
    }

    /// <summary>
    /// 扩展方法 - 简化使用
    /// </summary>
    public static class GameMonoBehaviorExtensions
    {
        public static void RegisterUpdate(this MonoBehaviour mono, Action callback)
        {
            GameMonoBehavior.Instance.RegisterUpdate(mono, callback);
        }

        public static void RegisterFixedUpdate(this MonoBehaviour mono, Action callback)
        {
            GameMonoBehavior.Instance.RegisterFixedUpdate(mono, callback);
        }

        public static void RegisterLateUpdate(this MonoBehaviour mono, Action callback)
        {
            GameMonoBehavior.Instance.RegisterLateUpdate(mono, callback);
        }
    }
}

