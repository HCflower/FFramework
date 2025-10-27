using UnityEngine;

namespace FFramework
{
    /// <summary>
    /// 单例Mono类
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    // 自动创建实例
                    GameObject go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
                InitializeSingleton();
            }
            else if (instance != this)
            {
                Debug.LogWarning($"[Singleton] Multiple instances of {typeof(T)} found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void InitializeSingleton()
        {
            // 可选的初始化逻辑
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}