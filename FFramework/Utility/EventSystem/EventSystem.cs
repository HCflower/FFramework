using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// 事件系统 - 支持任意类型参数的事件中心
    /// </summary>
    public class EventSystem : SingletonMono<EventSystem>
    {
        /// <summary>
        /// 监听者信息
        /// </summary>
        public class ListenerInfo
        {
            public string MethodName;
            public string ClassName;
            public string AssemblyName;
            public object Target;
            public Type TargetType;
            public Delegate Callback;

            public ListenerInfo(Delegate callback)
            {
                this.Callback = callback;
                this.MethodName = callback.Method.Name;
                this.TargetType = callback.Target?.GetType();
                this.ClassName = TargetType?.Name ?? "静态方法";
                this.AssemblyName = TargetType?.Assembly.GetName().Name ?? "Unknown";
                this.Target = callback.Target;
            }

            public override string ToString()
            {
                if (Target != null && Target is MonoBehaviour mono)
                {
                    return $"{ClassName}.{MethodName} (GameObject: {mono.gameObject.name})";
                }
                return $"{ClassName}.{MethodName}";
            }

            public string GetDetailedInfo()
            {
                string targetInfo = "静态方法";
                if (Target != null)
                {
                    if (Target is MonoBehaviour mono)
                    {
                        targetInfo = $"MonoBehaviour on GameObject '{mono.gameObject.name}'";
                    }
                    else
                    {
                        targetInfo = $"实例对象 ({ClassName})";
                    }
                }

                return $"类: {ClassName}, 方法: {MethodName}, 目标: {targetInfo}, 程序集: {AssemblyName}";
            }
        }

        /// <summary>
        /// 事件信息基类
        /// </summary>
        private abstract class EventInfoBase
        {
            public string eventName;
            public abstract void Invoke(object parameter);
            public abstract bool TryRemove(Delegate callback);
            public abstract int GetListenerCount();
            public abstract bool IsEmpty();
            public abstract void Clear();
            public abstract List<ListenerInfo> GetListeners();
        }

        /// <summary>
        /// 无参数事件信息
        /// </summary>
        private class VoidEventInfo : EventInfoBase
        {
            private event Action callback;

            public void AddCallback(Action action)
            {
                callback += action;
            }

            public override void Invoke(object parameter)
            {
                callback?.Invoke();
            }

            public override bool TryRemove(Delegate callbackToRemove)
            {
                if (callbackToRemove is Action action)
                {
                    callback -= action;
                    return true;
                }
                return false;
            }

            public override int GetListenerCount()
            {
                return callback?.GetInvocationList().Length ?? 0;
            }

            public override bool IsEmpty()
            {
                return callback == null;
            }

            public override void Clear()
            {
                callback = null;
            }

            public override List<ListenerInfo> GetListeners()
            {
                List<ListenerInfo> listeners = new List<ListenerInfo>();
                if (callback != null)
                {
                    foreach (Delegate del in callback.GetInvocationList())
                    {
                        listeners.Add(new ListenerInfo(del));
                    }
                }
                return listeners;
            }
        }

        /// <summary>
        /// 泛型事件信息
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        private class GenericEventInfo<T> : EventInfoBase
        {
            private event Action<T> callback;

            public void AddCallback(Action<T> action)
            {
                callback += action;
            }

            public override void Invoke(object parameter)
            {
                if (parameter is T typedParam)
                {
                    callback?.Invoke(typedParam);
                }
                else if (parameter == null && !typeof(T).IsValueType)
                {
                    callback?.Invoke(default(T));
                }
                else
                {
                    Debug.LogError($"EventSystem: 参数类型不匹配，期望 {typeof(T)}, 实际 {parameter?.GetType()}");
                }
            }

            public override bool TryRemove(Delegate callbackToRemove)
            {
                if (callbackToRemove is Action<T> action)
                {
                    callback -= action;
                    return true;
                }
                return false;
            }

            public override int GetListenerCount()
            {
                return callback?.GetInvocationList().Length ?? 0;
            }

            public override bool IsEmpty()
            {
                return callback == null;
            }

            public override void Clear()
            {
                callback = null;
            }

            public override List<ListenerInfo> GetListeners()
            {
                List<ListenerInfo> listeners = new List<ListenerInfo>();
                if (callback != null)
                {
                    foreach (Delegate del in callback.GetInvocationList())
                    {
                        listeners.Add(new ListenerInfo(del));
                    }
                }
                return listeners;
            }
        }

        // 存储事件的字典：事件名 -> 事件信息
        private Dictionary<string, EventInfoBase> eventDict = new Dictionary<string, EventInfoBase>();

        // 缓存空事件列表，避免在遍历时修改字典
        private List<string> emptyEvents = new List<string>();

        #region 无参数事件

        /// <summary>
        /// 注册无参数事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void RegisterEvent(string eventName, Action callback)
        {
            if (!ValidateEventRegistration(eventName, callback)) return;

            if (!eventDict.TryGetValue(eventName, out EventInfoBase eventInfo))
            {
                eventInfo = new VoidEventInfo() { eventName = eventName };
                eventDict[eventName] = eventInfo;
            }

            if (eventInfo is VoidEventInfo voidEventInfo)
            {
                voidEventInfo.AddCallback(callback);
            }
            else
            {
                Debug.LogError($"EventSystem: 事件 {eventName} 类型不匹配，已存在其他类型的事件");
            }
        }

        /// <summary>
        /// 注销无参数事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void UnregisterEvent(string eventName, Action callback)
        {
            if (!ValidateEventUnregistration(eventName, callback)) return;

            if (eventDict.TryGetValue(eventName, out EventInfoBase eventInfo))
            {
                if (eventInfo.TryRemove(callback))
                {
                    if (eventInfo.IsEmpty())
                    {
                        eventDict.Remove(eventName);
                    }
                }
                else
                {
                    Debug.LogWarning($"EventSystem: 事件 {eventName} 类型不匹配或回调不存在");
                }
            }
            else
            {
                Debug.LogWarning($"EventSystem: 尝试注销不存在的事件 {eventName}");
            }
        }

        /// <summary>
        /// 触发无参数事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void TriggerEvent(string eventName)
        {
            TriggerEventInternal(eventName, null);
        }

        #endregion

        #region 泛型事件

        /// <summary>
        /// 注册泛型事件
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void RegisterEvent<T>(string eventName, Action<T> callback)
        {
            if (!ValidateEventRegistration(eventName, callback)) return;

            if (!eventDict.TryGetValue(eventName, out EventInfoBase eventInfo))
            {
                eventInfo = new GenericEventInfo<T>() { eventName = eventName };
                eventDict[eventName] = eventInfo;
            }

            if (eventInfo is GenericEventInfo<T> genericEventInfo)
            {
                genericEventInfo.AddCallback(callback);
            }
            else
            {
                Debug.LogError($"EventSystem: 事件 {eventName} 类型不匹配，期望 {typeof(T)}, 实际 {eventInfo.GetType()}");
            }
        }

        /// <summary>
        /// 注销泛型事件
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void UnregisterEvent<T>(string eventName, Action<T> callback)
        {
            if (!ValidateEventUnregistration(eventName, callback)) return;

            if (eventDict.TryGetValue(eventName, out EventInfoBase eventInfo))
            {
                if (eventInfo.TryRemove(callback))
                {
                    if (eventInfo.IsEmpty())
                    {
                        eventDict.Remove(eventName);
                    }
                }
                else
                {
                    Debug.LogWarning($"EventSystem: 事件 {eventName} 类型不匹配或回调不存在");
                }
            }
            else
            {
                Debug.LogWarning($"EventSystem: 尝试注销不存在的事件 {eventName}");
            }
        }

        /// <summary>
        /// 触发泛型事件
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="parameter">事件参数</param>
        public void TriggerEvent<T>(string eventName, T parameter)
        {
            TriggerEventInternal(eventName, parameter);
        }

        #endregion

        #region object参数事件（向后兼容）

        /// <summary>
        /// 注册object参数事件（向后兼容）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void RegisterEvent(string eventName, Action<object> callback)
        {
            RegisterEvent<object>(eventName, callback);
        }

        /// <summary>
        /// 注销object参数事件（向后兼容）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void UnregisterEvent(string eventName, Action<object> callback)
        {
            UnregisterEvent<object>(eventName, callback);
        }

        /// <summary>
        /// 触发object参数事件（向后兼容）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="parameter">事件参数</param>
        public void TriggerEvent(string eventName, object parameter)
        {
            TriggerEventInternal(eventName, parameter);
        }

        #endregion

        #region 高级功能

        /// <summary>
        /// 批量注册事件
        /// </summary>
        /// <param name="events">事件字典</param>
        public void RegisterEvents(Dictionary<string, Action> events)
        {
            foreach (var kvp in events)
            {
                RegisterEvent(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 批量注册泛型事件
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="events">事件字典</param>
        public void RegisterEvents<T>(Dictionary<string, Action<T>> events)
        {
            foreach (var kvp in events)
            {
                RegisterEvent(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 一次性事件（触发后自动注销）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void RegisterOnceEvent(string eventName, Action callback)
        {
            Action onceCallback = null;
            onceCallback = () =>
            {
                callback?.Invoke();
                UnregisterEvent(eventName, onceCallback);
            };
            RegisterEvent(eventName, onceCallback);
        }

        /// <summary>
        /// 一次性泛型事件（触发后自动注销）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">事件回调</param>
        public void RegisterOnceEvent<T>(string eventName, Action<T> callback)
        {
            Action<T> onceCallback = null;
            onceCallback = (param) =>
            {
                callback?.Invoke(param);
                UnregisterEvent(eventName, onceCallback);
            };
            RegisterEvent(eventName, onceCallback);
        }

        /// <summary>
        /// 延迟触发事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="delay">延迟时间（秒）</param>
        public void TriggerEventDelayed(string eventName, float delay)
        {
            StartCoroutine(TriggerEventDelayedCoroutine(eventName, null, delay));
        }

        /// <summary>
        /// 延迟触发泛型事件
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="parameter">事件参数</param>
        /// <param name="delay">延迟时间（秒）</param>
        public void TriggerEventDelayed<T>(string eventName, T parameter, float delay)
        {
            StartCoroutine(TriggerEventDelayedCoroutine(eventName, parameter, delay));
        }

        /// <summary>
        /// 延迟触发事件协程
        /// </summary>
        private System.Collections.IEnumerator TriggerEventDelayedCoroutine(string eventName, object parameter, float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerEventInternal(eventName, parameter);
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 内部触发事件方法
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="parameter">事件参数</param>
        private void TriggerEventInternal(string eventName, object parameter)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("EventSystem: 事件名称不能为空");
                return;
            }

            if (eventDict.TryGetValue(eventName, out EventInfoBase eventInfo) && !eventInfo.IsEmpty())
            {
                try
                {
                    eventInfo.Invoke(parameter);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventSystem: 事件 {eventName} 执行时发生异常: {ex.Message}\n{ex.StackTrace}");
                }
            }
            // 移除了"尝试触发不存在或无监听者的事件"的警告，因为这在正常使用中很常见
        }

        /// <summary>
        /// 验证事件注册参数
        /// </summary>
        private bool ValidateEventRegistration(string eventName, Delegate callback)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("EventSystem: 事件名称不能为空");
                return false;
            }

            if (callback == null)
            {
                Debug.LogError("EventSystem: 回调函数不能为空");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证事件注销参数
        /// </summary>
        private bool ValidateEventUnregistration(string eventName, Delegate callback)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("EventSystem: 事件名称不能为空");
                return false;
            }

            if (callback == null)
            {
                Debug.LogError("EventSystem: 回调函数不能为空");
                return false;
            }

            return true;
        }

        #endregion

        #region 监听者追踪和调试方法

        /// <summary>
        /// 获取指定事件的所有监听者信息
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>监听者信息列表</returns>
        public List<ListenerInfo> GetEventListeners(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !eventDict.TryGetValue(eventName, out EventInfoBase eventInfo))
            {
                return new List<ListenerInfo>();
            }

            return eventInfo.GetListeners();
        }

        /// <summary>
        /// 打印指定事件的监听者信息（调试用）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void DebugPrintEventListeners(string eventName)
        {
            var listeners = GetEventListeners(eventName);
            if (listeners.Count == 0)
            {
                Debug.Log($"事件 '{eventName}' 没有监听者");
                return;
            }

            Debug.Log($"<color=yellow>=== 事件 '{eventName}' 的监听者信息 ===</color>");
            for (int i = 0; i < listeners.Count; i++)
            {
                Debug.Log($"ID:{i + 1}.{listeners[i].GetDetailedInfo()}");
            }
            Debug.Log("<color=yellow>=== 监听者信息结束 ===</color>");
        }

        /// <summary>
        /// 注销指定事件的所有监听者
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void UnregisterAllEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("EventSystem: 事件名称不能为空");
                return;
            }

            if (eventDict.ContainsKey(eventName))
            {
                eventDict[eventName].Clear();
                eventDict.Remove(eventName);
            }
        }

        /// <summary>
        /// 清理所有事件
        /// </summary>
        public void ClearAllEvents()
        {
            foreach (var eventInfo in eventDict.Values)
            {
                eventInfo.Clear();
            }
            eventDict.Clear();
        }

        /// <summary>
        /// 检查事件是否存在
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>是否存在</returns>
        public bool HasEvent(string eventName)
        {
            return !string.IsNullOrEmpty(eventName) &&
                   eventDict.TryGetValue(eventName, out EventInfoBase eventInfo) &&
                   !eventInfo.IsEmpty();
        }

        /// <summary>
        /// 获取事件监听者数量
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns>监听者数量</returns>
        public int GetListenerCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !eventDict.TryGetValue(eventName, out EventInfoBase eventInfo))
            {
                return 0;
            }

            return eventInfo.GetListenerCount();
        }

        /// <summary>
        /// 定期清理空事件
        /// </summary>
        public void CleanupEmptyEvents()
        {
            emptyEvents.Clear();

            foreach (var kvp in eventDict.ToArray())
            {
                if (kvp.Value.IsEmpty())
                {
                    emptyEvents.Add(kvp.Key);
                }
            }

            foreach (string eventName in emptyEvents)
            {
                eventDict.Remove(eventName);
            }
        }

        /// <summary>
        /// 获取所有事件名称（调试用）
        /// </summary>
        /// <returns>事件名称数组</returns>
        public string[] GetAllEventNames()
        {
            return eventDict.Keys.Where(key => !eventDict[key].IsEmpty()).ToArray();
        }

        /// <summary>
        /// 打印所有事件信息（调试用）
        /// </summary>
        public void DebugPrintAllEvents()
        {
            Debug.Log("<color=yellow>=== EventSystem 所有事件信息 ===</color>");
            foreach (var kvp in eventDict)
            {
                if (!kvp.Value.IsEmpty())
                {
                    string eventType = kvp.Value.GetType().Name;
                    if (eventType.Contains("GenericEventInfo"))
                    {
                        var genericType = kvp.Value.GetType().GetGenericArguments().FirstOrDefault();
                        eventType = $"Generic<{genericType?.Name ?? "Unknown"}>";
                    }

                    var listeners = kvp.Value.GetListeners();
                    string listenerNames = string.Join(", ", listeners.Select(l => l.ToString()));

                    Debug.Log($"事件: {kvp.Key}, 类型: {eventType}, 监听者数量: {kvp.Value.GetListenerCount()}, 监听者: [{listenerNames}]");
                }
            }
            Debug.Log("<color=yellow>=== EventSystem 事件信息结束 ===</color>");
        }

        /// <summary>
        /// 查找指定GameObject上的所有事件监听
        /// </summary>
        /// <param name="gameObject">目标GameObject</param>
        /// <returns>监听的事件列表</returns>
        public List<string> FindEventsListenedByGameObject(GameObject gameObject)
        {
            List<string> listenedEvents = new List<string>();

            foreach (var kvp in eventDict)
            {
                var listeners = kvp.Value.GetListeners();
                foreach (var listener in listeners)
                {
                    if (listener.Target is MonoBehaviour mono && mono.gameObject == gameObject)
                    {
                        listenedEvents.Add(kvp.Key);
                        break;
                    }
                }
            }

            return listenedEvents;
        }

        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        /// <returns>事件统计信息</returns>
        public EventStatistics GetEventStatistics()
        {
            var stats = new EventStatistics();
            stats.TotalEvents = eventDict.Count;
            stats.TotalListeners = eventDict.Values.Sum(e => e.GetListenerCount());

            foreach (var kvp in eventDict)
            {
                if (kvp.Value is VoidEventInfo)
                {
                    stats.VoidEvents++;
                }
                else
                {
                    stats.GenericEvents++;
                }
            }

            return stats;
        }

        #endregion

        protected override void OnDestroy()
        {
            ClearAllEvents();
        }
    }

    /// <summary>
    /// 事件统计信息
    /// </summary>
    public struct EventStatistics
    {
        public int TotalEvents;
        public int TotalListeners;
        public int VoidEvents;
        public int GenericEvents;

        public override string ToString()
        {
            return $"总事件: {TotalEvents}, 总监听者: {TotalListeners}, 无参事件: {VoidEvents}, 泛型事件: {GenericEvents}";
        }
    }
}