using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// 类型事件系统接口
    /// </summary>
    public interface ITypeEventSystem
    {
        public void Send<T>() where T : new();                          //发送事件
        public void Send<T>(T Event);                                   //发送事件
        public IUnRegister Register<T>(Action<T> onEvent);              //注册事件
        public void UnRegister<T>(Action<T> onEvent);                   //取消注册事件
    }

    //注销事件接口
    public interface IUnRegister
    {
        public void UnRegister();
    }

    //用于存储事件系统和事件回调的引用，以便后续注销
    public struct TypeEventSystemUnRegister<T> : IUnRegister
    {
        public ITypeEventSystem TypeEventSystem;
        public Action<T> OnEvent;
        public void UnRegister()
        {
            TypeEventSystem.UnRegister<T>(OnEvent);
            TypeEventSystem = null;
            OnEvent = null;
        }
    }

    public class UnRegisterOnDestoryTrigger : MonoBehaviour
    {
        private HashSet<IUnRegister> unRegisters = new HashSet<IUnRegister>();

        public void AddUnRegister(IUnRegister unRegister)
        {
            unRegisters.Add(unRegister);
        }

        private void OnDestroy()
        {
            foreach (var unRegister in unRegisters)
            {
                unRegister.UnRegister();
            }
            unRegisters.Clear();
        }
    }

    public static class UnRegisterExtension
    {
        /// <summary>
        /// 在物体销毁时注销事件
        /// 自动附加 UnRegisterOnDestoryTrigger
        /// </summary>
        public static void UnRegisterWhenGameObjectDestory(this IUnRegister unRegister, GameObject gameObject)
        {
            UnRegisterOnDestoryTrigger trigger = gameObject.GetComponent<UnRegisterOnDestoryTrigger>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<UnRegisterOnDestoryTrigger>();
            }
            trigger.AddUnRegister(unRegister);
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
    public class TypeEventSystem : ITypeEventSystem
    {
        public interface IRegisters { }

        public class Registers<T> : IRegisters
        {
            public Action<T> OnEvent = Event => { };
        }

        //事件注册字典
        private Dictionary<Type, IRegisters> registerDic = new Dictionary<Type, IRegisters>();

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
            Type type = typeof(T);
            IRegisters registers;
            if (registerDic.TryGetValue(TypeHelper<T>.Type, out registers))
            {
                ((Registers<T>)registers).OnEvent(Event);
            }
        }

        /// <summary>
        /// 事件注册
        /// </summary>
        public IUnRegister Register<T>(Action<T> onEvent)
        {
            Type type = typeof(T);
            IRegisters registers;

            if (!registerDic.TryGetValue(TypeHelper<T>.Type, out registers))
            {
                registers = new Registers<T>();
                registerDic.Add(type, registers);
            }
            ((Registers<T>)registers).OnEvent += onEvent;

            return new TypeEventSystemUnRegister<T>
            {
                TypeEventSystem = this,
                OnEvent = onEvent
            };
        }

        /// <summary>
        /// 事件注销
        /// </summary>
        public void UnRegister<T>(Action<T> onEvent)
        {
            Type type = typeof(T);
            IRegisters registers;
            if (registerDic.TryGetValue(TypeHelper<T>.Type, out registers))
            {
                ((Registers<T>)registers).OnEvent -= onEvent;
            }
        }
    }
}