using FFramework.Utility;

namespace FFramework.Architecture
{
    /// <summary>
    /// Model基础类
    /// </summary>
    public abstract class BaseModel : IModel
    {
        protected EventSystem eventSystem;

        public virtual void Initialize()
        {
            eventSystem = EventSystem.Instance;
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
    }
}