// =============================================================
// 描述：Mono单例基类
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.0
// =============================================================
using UnityEngine;

namespace FFramework.Architecture
{
    /// <summary>
    /// 修复的单例MonoBehaviour基类
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static bool isInitialized = false;

        public static T Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return null;
#endif
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        GameObject singleton = new GameObject(typeof(T).Name);
                        instance = singleton.AddComponent<T>();
#if UNITY_EDITOR
                        if (Application.isPlaying)
#endif
                        {
                            DontDestroyOnLoad(singleton);
                        }
                    }
                    if (!isInitialized)
                    {
                        (instance as SingletonMono<T>).InitializeSingleton();
                        isInitialized = true;
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            // 如果场景中有重复的单例对象，销毁自己
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 单例初始化时调用，子类可重写
        /// </summary>
        protected virtual void InitializeSingleton()
        {
            Debug.Log($"{typeof(T).Name} 单例初始化完成");
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                isInitialized = false;
            }
        }

        /// <summary>
        /// 手动初始化单例（如果需要在特定时机初始化）
        /// </summary>
        public static void EnsureInitialized()
        {
            var _ = Instance; // 访问Instance属性触发初始化
        }
    }
}