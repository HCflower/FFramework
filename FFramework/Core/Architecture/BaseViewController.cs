// =============================================================
// 描述：ViewController基础类
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.0
// =============================================================
using FFramework.Utility;
using UnityEngine;

namespace FFramework.Architecture
{
    public abstract class BaseViewController : MonoBehaviour, IViewController
    {
        protected EventSystem eventSystem;
        protected ArchitectureManager architectureManager;

        public GameObject GameObject => gameObject;

        public virtual void Initialize()
        {
            eventSystem = EventSystem.Instance;
            architectureManager = ArchitectureManager.Instance;
            OnInitialize();
        }

        public virtual void Dispose()
        {
            OnDispose();
        }

        /// <summary>
        /// 子类重写初始化逻辑
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 子类重写销毁逻辑
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// 获取Model
        /// </summary>
        protected T GetModel<T>() where T : class, IModel
        {
            return architectureManager.GetModel<T>();
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        protected void SendEvent(string eventName)
        {
            eventSystem?.TriggerEvent(eventName);
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        protected void SendEvent<T>(string eventName, T parameter)
        {
            eventSystem?.TriggerEvent(eventName, parameter);
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        protected void RegisterEvent(string eventName, System.Action callback)
        {
            eventSystem?.RegisterEvent(eventName, callback);
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        protected void RegisterEvent<T>(string eventName, System.Action<T> callback)
        {
            eventSystem?.RegisterEvent(eventName, callback);
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }
    }
}