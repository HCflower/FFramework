using UnityEngine;

namespace FFramework.Architecture
{
    /// <summary>
    /// 单例Mono类
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        private static T instance;
        private static bool applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                if (instance == null)
                {
                    // 先在场景中查找是否已存在
                    instance = FindObjectOfType<T>();

                    if (instance == null)
                    {
                        // 场景中不存在，自动创建实例
                        GameObject go = new GameObject(typeof(T).Name);
                        instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                        Debug.Log($"[Singleton] 自动创建单例: {typeof(T).Name}");
                    }
                    else
                    {
                        // 场景中已存在，确保不被销毁
                        DontDestroyOnLoad(instance.gameObject);
                        Debug.Log($"[Singleton] 发现场景中的单例: {typeof(T).Name}");
                    }
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
                Debug.Log($"[Singleton] 初始化单例: {typeof(T).Name}");
            }
            else if (instance != this)
            {
                Debug.LogWarning($"[Singleton] 发现重复的单例实例: {typeof(T).Name}，销毁重复项");
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

        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }
    }
}