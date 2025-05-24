using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework
{
    // 事件注销接口
    public interface IUnRegister
    {
        void UnRegister();
    }

    /// <summary>
    /// 绑定属性 
    /// </summary>
    [Serializable]
    public class BindableProperty<T>
    {
        [SerializeField]
        private T value = default(T);
        //防止多线程死锁
        private readonly object locked = new object();
        public T Value
        {
            get => value;
            set
            {
                lock (locked)
                {
                    if (!EqualityComparer<T>.Default.Equals(value, this.value))
                    {
                        this.value = value;
                        ValueChanged?.Invoke(value);
#if UNITY_EDITOR
                        // 确保在编辑器中修改值时也能更新
                        if (UnityEditor.EditorApplication.isPlaying == false)
                        {
                            UnityEditor.EditorUtility.SetDirty(UnityEngine.Object.FindObjectOfType<UnityEngine.MonoBehaviour>());
                        }
#endif
                    }
                }
            }
        }

        public BindableProperty(T defaultValue = default(T))
        {
            value = defaultValue;
        }

        // 属性值变化事件
        private event Action<T> ValueChanged;

        /// <summary>
        /// 注册事件  
        /// onValueChange -> 值变化事件
        /// isInit -> 是否初始化调用
        /// autoUnRegister -> 是否自动注销
        /// </summary>
        public IUnRegister Register(Action<T> onValueChange, bool isInit = true)
        {
            this.ValueChanged += onValueChange;
            // 手动调用一次
            if (isInit) ValueChanged?.Invoke(value);
            // 创建注销器
            var unRegister = new BindablePropertyUnRegister<T>(this, onValueChange);
            return unRegister;
        }

        /// <summary>
        /// 绑定属性注销结构体
        /// </summary>
        public struct BindablePropertyUnRegister<U> : IUnRegister
        {
            private BindableProperty<U> bindableProperty;
            private Action<U> onValueChange;

            public BindablePropertyUnRegister(BindableProperty<U> bindableProperty, Action<U> onValueChange)
            {
                this.bindableProperty = bindableProperty;
                this.onValueChange = onValueChange;
            }

            public void UnRegister()
            {
                bindableProperty.UnRegister(onValueChange);
            }
        }

        /// <summary>
        /// 注销事件
        /// </summary>
        public void UnRegister(Action<T> onValueChange)
        {
            if (onValueChange != null) this.ValueChanged -= onValueChange;
        }

        /// <summary>
        /// 注销所有值修改事件 
        /// </summary>
        public void UnregisterAll()
        {
            ValueChanged = null;
        }

        //操作符重载 a.Value == b.value; -> a == b;;
        public static implicit operator T(BindableProperty<T> bindableProperty)
        {
            return bindableProperty.Value;
        }

        //字符串转换
        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// 绑定属性扩展方法
    /// </summary>
    public static class BindablePropertyExtensions
    {
        /// <summary>
        /// 当GameObject销毁时自动注销事件
        /// </summary>
        public static void UnRegisterWhenGameObjectDestroy(this IUnRegister unRegister, GameObject gameObject)
        {
            if (gameObject == null) return;

            // 添加自动注销组件
            var autoUnregister = gameObject.GetComponent<BindablePropertyAutoUnregister>();
            if (autoUnregister == null)
            {
                autoUnregister = gameObject.AddComponent<BindablePropertyAutoUnregister>();
            }

            autoUnregister.AddUnRegister(unRegister);
        }
    }

    // 自动注销组件
    public class BindablePropertyAutoUnregister : MonoBehaviour
    {
        private HashSet<IUnRegister> unRegisters = new HashSet<IUnRegister>();

        /// <summary>
        /// 添加注销器
        /// </summary>
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
}