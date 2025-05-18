using UnityEngine;

namespace FFramework
{
    /// <summary>
    /// 单例 MonoBehaviour 基类
    /// </summary>
    public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        // 控制是否使用可以被销毁
        protected bool IsDontDestroyOnLoad = true;
        private static T mInstance;
        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = FindObjectOfType<T>();
                    if (mInstance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        mInstance = singletonObject.AddComponent<T>();
                    }
                }
                return mInstance;
            }
        }

        protected virtual void Awake()
        {
            if (IsDontDestroyOnLoad) DontDestroyOnLoad(this);
            if (mInstance != null) DestroyImmediate(this.gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (mInstance == this)
            {
                mInstance = null;
            }
        }
    }
}