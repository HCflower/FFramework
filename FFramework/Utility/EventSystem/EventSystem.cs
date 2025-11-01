using System.Collections.Generic;
using FFramework.Architecture;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using System;

namespace FFramework.Utility
{
    /// <summary>
    /// 简化的事件系统 - 支持任意类型参数的事件中心
    /// </summary>
    public class EventSystem : SingletonMono<EventSystem>
    {
        #region 内部类

        /// <summary>
        /// 事件信息基类
        /// </summary>
        public abstract class EventInfoBase
        {
            public TriggerInfo LastTrigger { get; set; }

            public abstract void Invoke(object parameter, TriggerInfo triggerInfo);
            public abstract bool IsEmpty();
            public abstract void Clear();
            public abstract List<ListenerInfo> GetListeners();
            public abstract int GetListenerCount();
            public abstract void CleanupInvalidListeners();
            public abstract bool TryRemove(Delegate callback);
        }

        /// <summary>
        /// 无参事件信息
        /// </summary>
        public class EventInfo : EventInfoBase
        {
            private readonly List<ListenerInfo> listeners = new List<ListenerInfo>();

            public override void Invoke(object parameter, TriggerInfo triggerInfo)
            {
                LastTrigger = triggerInfo;

                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    var listener = listeners[i];
                    if (!listener.IsActive || !listener.IsTargetValid())
                    {
                        listeners.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        (listener.Callback as Action)?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"EventSystem: 执行回调时发生异常: {ex.Message}");
                    }
                }
            }

            public override bool IsEmpty() => listeners.Count == 0;
            public override void Clear() => listeners.Clear();
            public override List<ListenerInfo> GetListeners() => listeners.ToList();
            public override int GetListenerCount() => listeners.Count;

            public override void CleanupInvalidListeners()
            {
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    if (!listeners[i].IsTargetValid())
                    {
                        listeners.RemoveAt(i);
                    }
                }
            }

            public override bool TryRemove(Delegate callback)
            {
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(listeners[i].Callback, callback))
                    {
                        listeners.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            public void AddListener(Action callback)
            {
                listeners.Add(new ListenerInfo(callback));
            }
        }

        /// <summary>
        /// 泛型事件信息
        /// </summary>
        public class EventInfo<T> : EventInfoBase
        {
            private readonly List<ListenerInfo> listeners = new List<ListenerInfo>();

            public override void Invoke(object parameter, TriggerInfo triggerInfo)
            {
                LastTrigger = triggerInfo;

                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    var listener = listeners[i];
                    if (!listener.IsActive || !listener.IsTargetValid())
                    {
                        listeners.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        (listener.Callback as Action<T>)?.Invoke((T)parameter);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"EventSystem: 执行回调时发生异常: {ex.Message}");
                    }
                }
            }

            public override bool IsEmpty() => listeners.Count == 0;
            public override void Clear() => listeners.Clear();
            public override List<ListenerInfo> GetListeners() => listeners.ToList();
            public override int GetListenerCount() => listeners.Count;

            public override void CleanupInvalidListeners()
            {
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    if (!listeners[i].IsTargetValid())
                    {
                        listeners.RemoveAt(i);
                    }
                }
            }

            public override bool TryRemove(Delegate callback)
            {
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(listeners[i].Callback, callback))
                    {
                        listeners.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            public void AddListener(Action<T> callback)
            {
                listeners.Add(new ListenerInfo(callback));
            }
        }

        /// <summary>
        /// 触发者信息 - 用于定位事件触发位置
        /// </summary>
        public class TriggerInfo
        {
            public string CallerMethod { get; set; }
            public string CallerClass { get; set; }
            public DateTime TriggerTime { get; set; }
            public object TriggerContext { get; set; }

            public TriggerInfo(object context = null)
            {
                TriggerTime = DateTime.Now;
                TriggerContext = context;
                ExtractCallerInfo();
            }

            private void ExtractCallerInfo()
            {
                var st = new StackTrace(false);
                var frames = st.GetFrames();

                foreach (var f in frames)
                {
                    var m = f.GetMethod();
                    var t = m?.DeclaringType;
                    if (t == null) continue;

                    // 跳过 EventSystem 相关类
                    if (t == typeof(EventSystem) ||
                        t.DeclaringType == typeof(EventSystem) ||
                        t.FullName?.Contains("EventSystem") == true)
                        continue;

                    // 跳过 Unity 系统类
                    var ns = t.Namespace ?? "";
                    if (ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor"))
                        continue;

                    string methodName = m.Name;
                    string className = t.Name;

                    // 处理编译器生成的闭包类
                    if (className.StartsWith("<>c") || className.Contains("DisplayClass"))
                    {
                        var outerType = t.DeclaringType;
                        if (outerType != null)
                        {
                            className = outerType.Name;
                        }
                    }

                    // 还原编译器生成的匿名方法名
                    if (methodName.StartsWith("<") && methodName.Contains(">"))
                    {
                        int start = methodName.IndexOf('<') + 1;
                        int end = methodName.IndexOf('>');
                        if (end > start)
                            methodName = methodName.Substring(start, end - start);
                    }

                    CallerClass = className;
                    CallerMethod = methodName;
                    return;
                }

                CallerClass = "Unknown";
                CallerMethod = "Unknown";
            }

            public string GetLocationInfo()
            {
                return $"{CallerClass}.{CallerMethod}()";
            }

            public string GetShortInfo()
            {
                return GetLocationInfo();
            }

            public string GetDetailedInfo()
            {
                var contextInfo = TriggerContext switch
                {
                    null => "无上下文",
                    MonoBehaviour mono when mono != null => $"MonoBehaviour: {mono.name}",
                    GameObject go when go != null => $"GameObject: {go.name}",
                    UnityEngine.UI.Button btn when btn != null => $"按钮: {btn.name}",
                    UnityEngine.Component comp when comp != null => $"组件: {comp.name} ({comp.GetType().Name})",
                    string str => $"字符串: {str}",
                    _ => $"对象: {TriggerContext.GetType().Name}"
                };

                return $"触发位置: {GetLocationInfo()}\n" +
                       $"触发时间: {TriggerTime:HH:mm:ss.fff}\n" +
                       $"上下文: {contextInfo}";
            }
        }

        /// <summary>
        /// 监听者信息
        /// </summary>
        public class ListenerInfo
        {
            public object Target { get; private set; }
            public Delegate Callback { get; private set; }
            public bool IsActive { get; set; } = true;

            public ListenerInfo(Delegate callback)
            {
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
                Target = callback.Target;
            }

            public bool IsTargetValid()
            {
                return Target switch
                {
                    null => true,
                    UnityEngine.Object unityObj => unityObj != null,
                    _ => true
                };
            }

            public string GetShortInfo()
            {
                string targetName = Target switch
                {
                    MonoBehaviour mono when mono != null => mono.gameObject.name,
                    MonoBehaviour => "[已销毁]",
                    null => "[静态]",
                    _ => "[实例]"
                };
                return $"{Callback.Method.Name}({targetName})";
            }
        }

        #endregion

        #region 字段

        private readonly Dictionary<string, EventInfoBase> _eventDict = new();

        #endregion

        #region 公共API - 事件注册

        /// <summary>
        /// 注册无参事件
        /// </summary>
        public void RegisterEvent(string eventName, Action callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;

            var eventInfo = GetOrCreateEventInfo<EventInfo>(eventName);
            eventInfo?.AddListener(callback);
        }

        /// <summary>
        /// 注册泛型事件
        /// </summary>
        public void RegisterEvent<T>(string eventName, Action<T> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;

            var eventInfo = GetOrCreateEventInfo<EventInfo<T>>(eventName);
            eventInfo?.AddListener(callback);
        }

        /// <summary>
        /// 注销无参事件
        /// </summary>
        public void UnregisterEvent(string eventName, Action callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;
            TryRemoveCallback(eventName, callback);
        }

        /// <summary>
        /// 注销泛型事件
        /// </summary>
        public void UnregisterEvent<T>(string eventName, Action<T> callback)
        {
            if (string.IsNullOrEmpty(eventName) || callback == null) return;
            TryRemoveCallback(eventName, callback);
        }

        #endregion

        #region 公共API - 事件触发（重点关注触发位置记录）

        /// <summary>
        /// 触发无参数事件 - 会自动记录触发位置
        /// </summary>
        public void TriggerEvent(string eventName, object context = null)
        {
            var triggerInfo = new TriggerInfo(context);
            TriggerEventInternal(eventName, null, triggerInfo);
        }

        /// <summary>
        /// 触发泛型事件 - 会自动记录触发位置
        /// </summary>
        public void TriggerEvent<T>(string eventName, T parameter, object context = null)
        {
            var triggerInfo = new TriggerInfo(context);
            TriggerEventInternal(eventName, parameter, triggerInfo);
        }

        #endregion

        #region 调试和定位方法

        /// <summary>
        /// 获取指定事件的最后触发位置信息
        /// </summary>
        public TriggerInfo GetLastTrigger(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_eventDict.TryGetValue(eventName, out var eventInfo))
                return null;

            return eventInfo.LastTrigger;
        }

        /// <summary>
        /// 打印指定事件的触发位置信息
        /// </summary>
        public void DebugPrintTriggerLocation(string eventName)
        {
            var triggerInfo = GetLastTrigger(eventName);
            if (triggerInfo != null)
            {
                UnityEngine.Debug.Log($"=== 事件 '{eventName}' 最后触发位置 ===");
                UnityEngine.Debug.Log(triggerInfo.GetDetailedInfo());
            }
            else
            {
                UnityEngine.Debug.Log($"事件 '{eventName}' 暂无触发记录");
            }
        }

        /// <summary>
        /// 打印所有事件的触发位置
        /// </summary>
        public void DebugPrintAllTriggerLocations()
        {
            UnityEngine.Debug.Log("=== 所有事件触发位置信息 ===");

            var activeEvents = _eventDict.Where(kvp => !kvp.Value.IsEmpty()).ToArray();

            foreach (var kvp in activeEvents)
            {
                var triggerInfo = kvp.Value.LastTrigger;
                string locationInfo = triggerInfo?.GetLocationInfo() ?? "未触发";
                UnityEngine.Debug.Log($"'{kvp.Key}' -> 最后触发位置: {locationInfo}");
            }
        }

        #endregion

        #region 公共API - 信息查询

        /// <summary>
        /// 获取指定事件的监听者列表
        /// </summary>
        public List<ListenerInfo> GetEventListeners(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_eventDict.TryGetValue(eventName, out var eventInfo))
                return new List<ListenerInfo>();

            return eventInfo.GetListeners();
        }


        /// <summary>
        /// 获取所有事件名称
        /// </summary>
        public string[] GetAllEventNames()
        {
            return _eventDict.Where(kvp => !kvp.Value.IsEmpty()).Select(kvp => kvp.Key).ToArray();
        }

        /// <summary>
        /// 检查事件是否存在且有监听者
        /// </summary>
        public bool HasEvent(string eventName)
        {
            return !string.IsNullOrEmpty(eventName) &&
                   _eventDict.TryGetValue(eventName, out var eventInfo) &&
                   !eventInfo.IsEmpty();
        }

        /// <summary>
        /// 获取事件监听者数量
        /// </summary>
        public int GetListenerCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_eventDict.TryGetValue(eventName, out var eventInfo))
                return 0;

            return eventInfo.GetListenerCount();
        }

        /// <summary>
        /// 查找指定对象注册的所有事件
        /// </summary>
        public Dictionary<string, List<ListenerInfo>> FindEventsByTarget(object target)
        {
            var result = new Dictionary<string, List<ListenerInfo>>();

            foreach (var kvp in _eventDict)
            {
                var listeners = kvp.Value.GetListeners()
                    .Where(l => l.Target == target)
                    .ToList();

                if (listeners.Count > 0)
                {
                    result[kvp.Key] = listeners;
                }
            }

            return result;
        }

        #endregion

        #region 公共API - 清理

        /// <summary>
        /// 清理无效的监听者
        /// </summary>
        public void CleanupInvalidListeners()
        {
            foreach (var eventInfo in _eventDict.Values)
            {
                eventInfo.CleanupInvalidListeners();
            }
        }

        /// <summary>
        /// 清理指定对象的所有事件监听
        /// </summary>
        public void UnregisterAllEventsForTarget(object target)
        {
            foreach (var eventInfo in _eventDict.Values)
            {
                var listeners = eventInfo.GetListeners();
                foreach (var listener in listeners.Where(l => l.Target == target))
                {
                    listener.IsActive = false;
                }
            }
        }

        /// <summary>
        /// 注销指定事件的所有监听者
        /// </summary>
        public void UnregisterAllEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                eventInfo.Clear();
            }
        }

        /// <summary>
        /// 清理所有事件
        /// </summary>
        public void ClearAllEvents()
        {
            foreach (var eventInfo in _eventDict.Values)
            {
                eventInfo.Clear();
            }
            _eventDict.Clear();
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 打印指定事件的详细信息
        /// </summary>
        public void DebugPrintEventDetails(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                UnityEngine.Debug.Log($"事件 '{eventName}' 不存在");
                return;
            }

            UnityEngine.Debug.Log($"=== 事件 '{eventName}' 详细信息 ===");
            UnityEngine.Debug.Log($"监听者数量: {eventInfo.GetListenerCount()}");

            var listeners = eventInfo.GetListeners();
            if (listeners.Count > 0)
            {
                UnityEngine.Debug.Log("--- 监听者列表 ---");
                for (int i = 0; i < listeners.Count; i++)
                {
                    UnityEngine.Debug.Log($"{i + 1}. {listeners[i].GetShortInfo()}");
                }
            }

            // 显示最后触发者信息
            if (eventInfo.LastTrigger != null)
            {
                UnityEngine.Debug.Log("--- 最后触发者 ---");
                UnityEngine.Debug.Log(eventInfo.LastTrigger.GetDetailedInfo());
            }
        }

        /// <summary>
        /// 打印所有事件信息
        /// </summary>
        public void DebugPrintAllEvents()
        {
            UnityEngine.Debug.Log("=== EventSystem 概览 ===");
            UnityEngine.Debug.Log($"总事件数: {_eventDict.Count}");
            UnityEngine.Debug.Log($"活跃事件数: {_eventDict.Values.Count(e => !e.IsEmpty())}");

            foreach (var kvp in _eventDict.Where(kvp => !kvp.Value.IsEmpty()))
            {
                var lastTrigger = kvp.Value.LastTrigger?.GetShortInfo() ?? "未触发";
                UnityEngine.Debug.Log($"'{kvp.Key}' -> 监听者:{kvp.Value.GetListenerCount()} 最后触发者:{lastTrigger}");
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 内部触发事件方法
        /// </summary>
        private void TriggerEventInternal(string eventName, object parameter, TriggerInfo triggerInfo)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_eventDict.TryGetValue(eventName, out var eventInfo) && !eventInfo.IsEmpty())
            {
                try
                {
                    eventInfo.Invoke(parameter, triggerInfo);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"EventSystem: 事件 {eventName} 执行时发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取或创建事件信息
        /// </summary>
        private T GetOrCreateEventInfo<T>(string eventName) where T : EventInfoBase, new()
        {
            if (!_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                eventInfo = new T();
                _eventDict[eventName] = eventInfo;
            }

            if (eventInfo is T typedEventInfo)
            {
                return typedEventInfo;
            }

            UnityEngine.Debug.LogError($"EventSystem: 事件 {eventName} 类型不匹配");
            return null;
        }

        /// <summary>
        /// 尝试移除回调
        /// </summary>
        private void TryRemoveCallback(string eventName, Delegate callback)
        {
            if (_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                eventInfo.TryRemove(callback);
            }
        }

        #endregion

        #region Unity生命周期

        protected override void OnDestroy()
        {
            ClearAllEvents();
            base.OnDestroy();
        }

        #endregion

        #region 静态API(兼容性)

        public static void S_RegisterEvent(string eventName, Action callback) => Instance.RegisterEvent(eventName, callback);
        public static void S_RegisterEvent<T>(string eventName, Action<T> callback) => Instance.RegisterEvent(eventName, callback);
        public static void S_RegisterEvent(string eventName, Action<object> callback) => Instance.RegisterEvent(eventName, callback);

        public static void S_UnregisterEvent(string eventName, Action callback) => Instance.UnregisterEvent(eventName, callback);
        public static void S_UnregisterEvent<T>(string eventName, Action<T> callback) => Instance.UnregisterEvent(eventName, callback);
        public static void S_UnregisterEvent(string eventName, Action<object> callback) => Instance.UnregisterEvent(eventName, callback);

        public static void S_TriggerEvent(string eventName) => Instance.TriggerEvent(eventName);
        public static void S_TriggerEvent<T>(string eventName, T parameter) => Instance.TriggerEvent(eventName, parameter);

        public static bool S_HasEvent(string eventName) => Instance.HasEvent(eventName);
        public static int S_GetListenerCount(string eventName) => Instance.GetListenerCount(eventName);

        // 新增静态API
        public static TriggerInfo S_GetLastTrigger(string eventName) => Instance.GetLastTrigger(eventName);

        #endregion
    }
}