using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// 绑定属性 
    /// </summary>
    [Serializable]
    public class BindableProperty<T>
    {
        [SerializeField]
        private T value = default(T);
        //放置多线程死锁
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
        /// 注册事件 -> 自动清理
        /// </summary>
        public void Register(Action<T> onValueChange, bool autoUnRegister = true)
        {
            this.ValueChanged += onValueChange;
            // 手动调用一次
            ValueChanged?.Invoke(value);
            // 自动注销
            if (autoUnRegister && onValueChange?.Target is MonoBehaviour behaviour)
            {
                var target = behaviour.gameObject;
                // 添加自动注销组件
                var unregister = target.GetComponent<BindablePropertyAutoUnregister>();
                if (unregister == null)
                {
                    unregister = target.AddComponent<BindablePropertyAutoUnregister>();
                }
                unregister.OnDestroyed += () => this.UnRegister(onValueChange);
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

    // 自动注销组件
    public class BindablePropertyAutoUnregister : MonoBehaviour
    {
        public event Action OnDestroyed;

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}