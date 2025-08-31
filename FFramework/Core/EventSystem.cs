using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// 类型事件系统接口
    /// </summary>
    public interface IEventSystem
    {
        void Send<T>() where T : new();
        void Send<T>(T Event);
        IRemoveListener AddListener<T>(Action<T> onEvent);
        void RemoveListener<T>(Action<T> onEvent);

        // 带事件ID的发送 -用于网络
        void Send<T>(object eventId, T Event);
        IRemoveListener AddListener<T>(object eventId, Action<T> onEvent);
        void RemoveListener<T>(object eventId, Action<T> onEvent);
    }

    /// <summary>
    /// 移除事件监听接口
    /// </summary>
    public interface IRemoveListener
    {
        void RemoveListener();
    }

    /// <summary>
    /// 用于存储事件系统和事件回调的引用（不带事件ID）
    /// </summary>
    public struct EventSystemUnRegister<T> : IRemoveListener
    {
        public IEventSystem TypeEventSystem;
        public Action<T> OnEvent;

        public void RemoveListener()
        {
            TypeEventSystem?.RemoveListener(OnEvent);
            TypeEventSystem = null;
            OnEvent = null;
        }
    }

    /// <summary>
    /// 用于存储事件系统和事件回调的引用（带事件ID）
    /// </summary>
    public struct EventSystemUnRegisterWithId<T> : IRemoveListener
    {
        public IEventSystem TypeEventSystem;
        public object EventId;
        public Action<T> OnEvent;

        public void RemoveListener()
        {
            TypeEventSystem?.RemoveListener(EventId, OnEvent);
            TypeEventSystem = null;
            EventId = null;
            OnEvent = null;
        }
    }

    /// <summary>
    /// 替代 typeof(T) 的高性能方案
    /// </summary>
    public static class TypeHelper<T>
    {
        public static readonly Type Type = typeof(T);
    }

    /// <summary>
    /// 事件监听器容器接口
    /// </summary>
    public interface IEventListeners { }

    /// <summary>
    /// 特定类型的事件监听器容器
    /// </summary>
    public class EventListeners<T> : IEventListeners
    {
        public Action<T> OnEvent = _ => { };
    }

    /// <summary>
    /// 事件系统的核心实现
    /// </summary>
    public class EventSystem : IEventSystem
    {
        #region 字段声明

        // 不带事件ID的注册字典
        private readonly Dictionary<Type, IEventListeners> registerDic = new Dictionary<Type, IEventListeners>();

        // 带事件ID的注册字典（类型+事件ID）
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, Delegate>> idRegisterDic =
            new ConcurrentDictionary<Type, ConcurrentDictionary<object, Delegate>>();

        #endregion

        #region 不带事件ID的方法实现

        public void Send<T>() where T : new()
        {
            Send(new T());
        }

        public void Send<T>(T Event)
        {
            Type type = TypeHelper<T>.Type;
            if (registerDic.TryGetValue(type, out var listeners) && listeners is EventListeners<T> typedListeners)
            {
                typedListeners.OnEvent(Event);
            }
        }

        public IRemoveListener AddListener<T>(Action<T> onEvent)
        {
            Type type = TypeHelper<T>.Type;

            if (!registerDic.TryGetValue(type, out var listeners))
            {
                listeners = new EventListeners<T>();
                registerDic.Add(type, listeners);
            }

            var typedListeners = (EventListeners<T>)listeners;
            typedListeners.OnEvent += onEvent;

            return new EventSystemUnRegister<T>
            {
                TypeEventSystem = this,
                OnEvent = onEvent
            };
        }

        public void RemoveListener<T>(Action<T> onEvent)
        {
            Type type = TypeHelper<T>.Type;
            if (registerDic.TryGetValue(type, out var listeners) && listeners is EventListeners<T> typedListeners)
            {
                typedListeners.OnEvent -= onEvent;
            }
        }

        #endregion

        #region 带事件ID的方法实现

        public void Send<T>(object eventId, T Event)
        {
            Type type = TypeHelper<T>.Type;
            if (idRegisterDic.TryGetValue(type, out var eventDic) &&
                eventDic.TryGetValue(eventId, out var delegateObj))
            {
                (delegateObj as Action<T>)?.Invoke(Event);
            }
        }

        public IRemoveListener AddListener<T>(object eventId, Action<T> onEvent)
        {
            Type type = TypeHelper<T>.Type;
            var eventDic = idRegisterDic.GetOrAdd(type, _ => new ConcurrentDictionary<object, Delegate>());

            eventDic.AddOrUpdate(eventId, onEvent, (_, existing) =>
                Delegate.Combine(existing, onEvent));

            return new EventSystemUnRegisterWithId<T>
            {
                TypeEventSystem = this,
                EventId = eventId,
                OnEvent = onEvent
            };
        }

        public void RemoveListener<T>(object eventId, Action<T> onEvent)
        {
            Type type = TypeHelper<T>.Type;
            if (idRegisterDic.TryGetValue(type, out var eventDic) &&
                eventDic.TryGetValue(eventId, out var currentDelegate))
            {
                var updatedDelegate = Delegate.Remove(currentDelegate, onEvent);
                if (updatedDelegate == null)
                {
                    eventDic.TryRemove(eventId, out _);
                }
                else
                {
                    eventDic[eventId] = updatedDelegate;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 自动移除监听器组件
    /// </summary>
    public class EventSystemAutoRemoveListener : MonoBehaviour
    {
        private readonly HashSet<IRemoveListener> unRegisters = new HashSet<IRemoveListener>();

        public void AddUnRegister(IRemoveListener unRegister)
        {
            unRegisters.Add(unRegister);
        }

        private void OnDestroy()
        {
            foreach (var unRegister in unRegisters)
            {
                unRegister.RemoveListener();
            }
            unRegisters.Clear();
        }
    }

    /// <summary>
    /// 事件系统扩展方法
    /// </summary>
    public static class EventSystemExtension
    {
        /// <summary>
        /// 在物体销毁时自动移除监听事件
        /// </summary>
        public static void RemoveListenerWhenGameObjectDestroy(this IRemoveListener unRegister, GameObject gameObject)
        {
            var trigger = gameObject.GetComponent<EventSystemAutoRemoveListener>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<EventSystemAutoRemoveListener>();
            }
            trigger.AddUnRegister(unRegister);
        }
    }
}