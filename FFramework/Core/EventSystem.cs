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
        public void Send<T>() where T : new();                          //发送事件
        public void Send<T>(T Event);                                   //发送特定类型的事件
        public IRemoveListener AddListener<T>(Action<T> onEvent);       //添加事件监听
        public void RemoveListener<T>(Action<T> onEvent);               //取消注册事件
    }

    //移除事件监听接口
    public interface IRemoveListener
    {
        public void RemoveListener();
    }

    //用于存储事件系统和事件回调的引用，以便后续注销
    public struct EventSystemUnRegister<T> : IRemoveListener
    {
        public IEventSystem TypeEventSystem;
        public Action<T> OnEvent;
        public void RemoveListener()
        {
            TypeEventSystem.RemoveListener<T>(OnEvent);
            TypeEventSystem = null;
            OnEvent = null;
        }
    }

    // 替代 typeof(T) 的高性能方案
    public static class TypeHelper<T>
    {
        public static readonly Type Type = typeof(T);
    }

    /// <summary>
    /// 事件系统的核心实现，管理事件的注册、注销和触发
    /// </summary>
    public class EventSystem : IEventSystem
    {
        public interface IAddListeners { }

        public class AddListeners<T> : IAddListeners
        {
            public Action<T> OnEvent = Event => { };
        }

        //事件注册字典
        private Dictionary<Type, IAddListeners> registerDic = new Dictionary<Type, IAddListeners>();

        /// <summary>
        /// 发送事件
        /// T -> 事件类型
        /// </summary>
        public void Send<T>() where T : new()
        {
            T Event = new T();
            Send<T>(Event);
        }

        /// <summary>
        /// 发送指定事件
        /// T -> 事件类型
        /// </summary>
        public void Send<T>(T Event)
        {
            Type type = TypeHelper<T>.Type;
            IAddListeners registers;
            if (registerDic.TryGetValue(type, out registers))
            {
                ((AddListeners<T>)registers).OnEvent(Event);
            }
        }

        /// <summary>
        /// 添加监听事件
        /// </summary>
        public IRemoveListener AddListener<T>(Action<T> onEvent)
        {
            Type type = TypeHelper<T>.Type;
            IAddListeners registers;

            if (!registerDic.TryGetValue(type, out registers))
            {
                registers = new AddListeners<T>();
                registerDic.Add(type, registers);
            }
            ((AddListeners<T>)registers).OnEvent += onEvent;

            return new EventSystemUnRegister<T>
            {
                TypeEventSystem = this,
                OnEvent = onEvent
            };
        }

        /// <summary>
        /// 移除监听事件
        /// </summary>
        public void RemoveListener<T>(Action<T> onEvent)
        {
            IAddListeners registers;
            if (registerDic.TryGetValue(TypeHelper<T>.Type, out registers))
            {
                ((AddListeners<T>)registers).OnEvent -= onEvent;
            }
        }
    }

    /// <summary>
    /// 自动移除监听事件扩展
    /// </summary>
    public static class EventSystemExtension
    {
        /// <summary>
        /// 在物体销毁时移除监听事件
        /// 自动附加 EventSystemAutoRemoveListener
        /// </summary>
        public static void RemoveListenerWhenGameObjectDestory(this IRemoveListener unRegister, GameObject gameObject)
        {
            EventSystemAutoRemoveListener trigger = gameObject.GetComponent<EventSystemAutoRemoveListener>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<EventSystemAutoRemoveListener>();
            }
            trigger.AddUnRegister(unRegister);
        }
    }

    /// <summary>
    /// 移除监听事件类
    /// </summary>
    public class EventSystemAutoRemoveListener : MonoBehaviour
    {
        private HashSet<IRemoveListener> unRegisters = new HashSet<IRemoveListener>();

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
}