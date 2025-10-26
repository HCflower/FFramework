using UnityEngine;

namespace FFramework
{
    /// <summary>
    /// 单例 MonoBehaviour 基类
    /// </summary>
    public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        // 控制是否使用可以被销毁
        [SerializeField] protected bool IsDontDestroyOnLoad = true;
        private static T mInstance;
        private static readonly object Lock = new object();
        private static bool isApplicationQuitting = false;

        public static T Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Returning null.");
                    return null;
                }

                // 双重检查锁定模式
                if (mInstance == null)
                {
                    lock (Lock)
                    {
                        if (mInstance == null)
                        {
                            // 确保在主线程中执行
                            if (!UnityThread.IsMainThread())
                            {
                                Debug.LogError($"[Singleton] Cannot create instance of {typeof(T)} on non-main thread.");
                                return null;
                            }

                            mInstance = FindObjectOfType<T>();

                            if (mInstance == null)
                            {
                                GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                                mInstance = singletonObject.AddComponent<T>();
                                Debug.Log($"[Singleton] An instance of {typeof(T)} was created.");
                            }
                        }
                    }
                }

                return mInstance;
            }
        }

        /// <summary>
        /// 检查实例是否存在（不会创建新实例）
        /// </summary>
        public static bool HasInstance => mInstance != null;

        protected virtual void Awake()
        {
            if (mInstance != null && mInstance != this)
            {
                Debug.LogWarning($"[Singleton] Another instance of {typeof(T)} already exists! Destroying this duplicate.");
                Destroy(gameObject);
                return;
            }

            mInstance = this as T;

            if (IsDontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                Debug.Log($"<color=yellow>[Singleton] Setting {typeof(T)} to DontDestroyOnLoad.</color>");
            }

            OnSingletonAwake();
        }

        /// <summary>
        /// 单例初始化时调用，子类可重写
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (mInstance == this)
            {
                mInstance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }
    }

    /// <summary>
    /// Unity主线程检查工具类
    /// </summary>
    public static class UnityThread
    {
        private static int mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;
        }
    }
}