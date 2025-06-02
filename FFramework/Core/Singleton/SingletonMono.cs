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
        private static readonly object Lock = new object();
        private static bool isApplicationQuitting = false;
        private static T mInstance;
        public static T Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Returning null.");
                    return null;
                }

                lock (Lock)
                {
                    if (mInstance == null)
                    {
                        mInstance = FindObjectOfType<T>();

                        if (mInstance == null)
                        {
                            GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                            mInstance = singletonObject.AddComponent<T>();
                            Debug.Log($"[Singleton] An instance of {typeof(T)} was created.");
                        }
                    }

                    return mInstance;
                }
            }
        }

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
        }

        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }
    }
}