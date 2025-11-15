using System.Collections.Generic;
using FFramework.Utility;
using UnityEngine;
using System;

namespace FFramework.Architecture
{
    /// <summary>
    /// 架构管理器 - 管理Model和ViewController的生命周期
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ArchitectureManager : SingletonMono<ArchitectureManager>
    {
        // 存储所有Models
        private Dictionary<Type, IModel> models = new Dictionary<Type, IModel>();

        // 存储所有ViewControllers
        private Dictionary<string, IViewController> viewControllers = new Dictionary<string, IViewController>();

        // 事件系统引用
        private EventSystem eventSystem;

        protected override void Awake()
        {
            base.Awake();
            eventSystem = EventSystem.Instance;
        }

        #region Model管理

        /// <summary>
        /// 注册Model
        /// </summary>
        public T RegisterModel<T>() where T : class, IModel, new()
        {
            Debug.Log($"RegisterModel InstanceID: {GetInstanceID()} Type: {typeof(T).Name}");
            Type modelType = typeof(T);

            if (models.ContainsKey(modelType))
            {
                Debug.LogWarning($"Model {modelType.Name} 已经存在");
                return models[modelType] as T;
            }

            T model = new T();
            models[modelType] = model;
            model.Initialize();

            Debug.Log($"注册Model: {modelType.Name}");
            return model;
        }

        /// <summary>
        /// 获取Model
        /// </summary>
        public T GetModel<T>() where T : class, IModel
        {
            Debug.Log($"GetModel InstanceID: {GetInstanceID()} Type: {typeof(T).Name}");
            Type modelType = typeof(T);

            if (models.TryGetValue(modelType, out IModel model))
            {
                return model as T;
            }

            Debug.LogWarning($"Model {modelType.Name} 未找到");
            return null;
        }

        /// <summary>
        /// 注销Model
        /// </summary>
        public void UnregisterModel<T>() where T : class, IModel
        {
            Type modelType = typeof(T);

            if (models.TryGetValue(modelType, out IModel model))
            {
                model.Dispose();
                models.Remove(modelType);
                Debug.Log($"注销Model: {modelType.Name}");
            }
        }

        #endregion

        #region ViewController管理

        /// <summary>
        /// 注册ViewController
        /// </summary>
        public T RegisterViewController<T>(GameObject gameObject) where T : MonoBehaviour, IViewController
        {
            string viewName = typeof(T).Name;

            if (viewControllers.ContainsKey(viewName))
            {
                Debug.LogWarning($"ViewController {viewName} 已经存在");
                return viewControllers[viewName] as T;
            }

            T viewController = gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

            viewControllers[viewName] = viewController;
            viewController.Initialize();

            Debug.Log($"注册ViewController: {viewName}");
            return viewController;
        }

        /// <summary>
        /// 获取ViewController
        /// </summary>
        public T GetViewController<T>() where T : class, IViewController
        {
            string viewName = typeof(T).Name;

            if (viewControllers.TryGetValue(viewName, out IViewController viewController))
            {
                return viewController as T;
            }

            Debug.LogWarning($"ViewController {viewName} 未找到");
            return null;
        }

        /// <summary>
        /// 注销ViewController
        /// </summary>
        public void UnregisterViewController<T>() where T : IViewController
        {
            string viewName = typeof(T).Name;

            if (viewControllers.TryGetValue(viewName, out IViewController viewController))
            {
                viewController.Dispose();
                viewControllers.Remove(viewName);
                Debug.Log($"注销ViewController: {viewName}");
            }
        }

        /// <summary>
        /// 注销ViewController（通过名称）
        /// </summary>
        public void UnregisterViewController(string viewName)
        {
            if (viewControllers.TryGetValue(viewName, out IViewController viewController))
            {
                viewController.Dispose();
                viewControllers.Remove(viewName);
                Debug.Log($"注销ViewController: {viewName}");
            }
        }

        #endregion

        #region 快捷访问方法

        /// <summary>
        /// 发送事件（快捷方法）
        /// </summary>
        public void SendEvent(string eventName)
        {
            eventSystem.TriggerEvent(eventName);
        }

        /// <summary>
        /// 发送事件（快捷方法）
        /// </summary>
        public void SendEvent<T>(string eventName, T parameter)
        {
            eventSystem.TriggerEvent(eventName, parameter);
        }

        /// <summary>
        /// 注册事件（快捷方法）
        /// </summary>
        public void RegisterEvent(string eventName, Action callback)
        {
            eventSystem.RegisterEvent(eventName, callback);
        }

        /// <summary>
        /// 注册事件（快捷方法）
        /// </summary>
        public void RegisterEvent<T>(string eventName, Action<T> callback)
        {
            eventSystem.RegisterEvent(eventName, callback);
        }

        #endregion

        protected override void OnDestroy()
        {
            // 清理所有Models
            foreach (var model in models.Values)
            {
                model.Dispose();
            }
            models.Clear();

            // 清理所有ViewControllers
            foreach (var viewController in viewControllers.Values)
            {
                viewController.Dispose();
            }
            viewControllers.Clear();

            base.OnDestroy();
        }
    }
}