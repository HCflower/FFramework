using System.Collections.Generic;
using FFramework.Architecture;
using System.Linq;
using UnityEngine;
using System;
using System.Collections;

namespace FFramework.Utility
{
    /// <summary>
    /// 事件系统 - 支持任意类型参数的事件中心
    /// </summary>
    public class EventSystem : SingletonMono<EventSystem>
    {
        #region Inner Classes and Data Structures

        /// <summary>
        /// 触发位置信息（简化版，用于当前触发）
        /// </summary>
        public class CurrentTriggerInfo
        {
            public string MethodName { get; private set; }
            public string ClassName { get; private set; }
            public string AssemblyName { get; private set; }
            public string TriggerTime { get; private set; }

            public CurrentTriggerInfo()
            {
                TriggerTime = DateTime.Now.ToString("HH:mm:ss");
                ExtractCallerInfo(2); // 跳过构造函数和RecordTriggerInfo
            }

            private void ExtractCallerInfo(int skipFrames)
            {
                var stackTrace = new System.Diagnostics.StackTrace(true);
                var frames = stackTrace.GetFrames();

                for (int i = skipFrames; i < frames.Length; i++)
                {
                    var method = frames[i].GetMethod();
                    var declaringType = method.DeclaringType;

                    if (declaringType != null && !IsEventSystemInternalCall(declaringType, method.Name))
                    {
                        MethodName = method.Name;
                        // 使用FullName，保证不会为null
                        ClassName = declaringType.FullName ?? declaringType.Name ?? "Unknown";
                        AssemblyName = declaringType.Assembly.GetName().Name;

                        // 美化显示：去掉编译器生成的显示类后缀
                        if (ClassName.Contains("+<>c"))
                        {
                            // 取+前的部分作为外部类名
                            ClassName = ClassName.Substring(0, ClassName.IndexOf("+<>c"));
                        }
                        return;
                    }
                }

                // 未找到有效调用者
                MethodName = ClassName = AssemblyName = "Unknown";
            }

            private bool IsEventSystemInternalCall(Type type, string methodName)
            {
                return type == typeof(EventSystem) ||
                       type.Name.Contains("EventInfo") ||
                       type.Name.Contains("EventSystem") ||
                       methodName.Contains("TriggerEventInternal") ||
                       methodName.Contains("RecordTriggerInfo") ||
                       methodName == ".ctor";
            }

            private string FindParentMethodName(System.Diagnostics.StackFrame[] frames, int currentIndex, Type declaringType)
            {
                for (int j = currentIndex + 1; j < frames.Length; j++)
                {
                    var parentMethod = frames[j].GetMethod();
                    if (parentMethod.DeclaringType == declaringType)
                    {
                        return $"{parentMethod.Name} (Lambda)";
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 事件触发位置信息（完整版，用于历史记录）
        /// </summary>
        public class TriggerInfo : CurrentTriggerInfo
        {
            public string FilePath { get; private set; }
            public int LineNumber { get; private set; }

            public TriggerInfo() : base()
            {
                ExtractFileInfo();
            }

            private void ExtractFileInfo()
            {
                var stackTrace = new System.Diagnostics.StackTrace(true);
                var frames = stackTrace.GetFrames();

                for (int i = 0; i < frames.Length; i++)
                {
                    var method = frames[i].GetMethod();
                    var declaringType = method.DeclaringType;

                    if (declaringType != null && !IsEventSystemInternalCall(declaringType, method.Name))
                    {
                        FilePath = frames[i].GetFileName() ?? "Unknown";
                        LineNumber = frames[i].GetFileLineNumber();
                        break;
                    }
                }
            }

            private bool IsEventSystemInternalCall(Type type, string methodName)
            {
                return type == typeof(EventSystem) ||
                       type.Name.Contains("EventInfo") ||
                       type.Name.Contains("EventSystem") ||
                       methodName.Contains("TriggerEventInternal") ||
                       methodName.Contains("RecordTriggerInfo") ||
                       methodName == ".ctor";
            }
        }

        /// <summary>
        /// 监听者信息基类
        /// </summary>
        public class ListenerInfo
        {
            public string MethodName { get; protected set; }
            public string ClassName { get; protected set; }
            public string AssemblyName { get; protected set; }
            public object Target { get; protected set; }
            public Type TargetType { get; protected set; }
            public Delegate Callback { get; protected set; }

            public ListenerInfo(Delegate callback)
            {
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
                MethodName = callback.Method.Name;
                TargetType = callback.Target?.GetType();
                ClassName = TargetType?.Name ?? "静态方法";
                AssemblyName = TargetType?.Assembly.GetName().Name ?? "Unknown";
                Target = callback.Target;
            }

            public override string ToString()
            {
                if (Target is MonoBehaviour mono && mono != null)
                {
                    return $"{ClassName}.{MethodName} (GameObject: {mono.gameObject.name})";
                }
                return $"{ClassName}.{MethodName}";
            }

            public virtual string GetDetailedInfo()
            {
                string targetInfo = Target switch
                {
                    null => "静态方法",
                    MonoBehaviour mono => $"MonoBehaviour on GameObject '{mono.gameObject.name}'",
                    _ => $"实例对象 ({ClassName})"
                };

                return $"类: {ClassName}, 方法: {MethodName}, 目标: {targetInfo}, 程序集: {AssemblyName}";
            }
        }

        /// <summary>
        /// 事件注册位置信息
        /// </summary>
        public class RegistrationInfo
        {
            public string MethodName { get; private set; }
            public string ClassName { get; private set; }
            public string AssemblyName { get; private set; }
            public string RegistrationTime { get; private set; }

            public RegistrationInfo()
            {
                RegistrationTime = DateTime.Now.ToString("HH:mm:ss");
                ExtractRegistrationInfo();
            }

            private void ExtractRegistrationInfo()
            {
                var stackTrace = new System.Diagnostics.StackTrace(true);
                var frames = stackTrace.GetFrames();

                for (int i = 0; i < frames.Length; i++)
                {
                    var method = frames[i].GetMethod();
                    var declaringType = method.DeclaringType;

                    if (declaringType != null && !IsEventSystemInternalCall(declaringType))
                    {
                        MethodName = method.Name;
                        ClassName = declaringType.Name;
                        AssemblyName = declaringType.Assembly.GetName().Name;
                        return;
                    }
                }

                MethodName = ClassName = AssemblyName = "Unknown";
            }

            private bool IsEventSystemInternalCall(Type declaringType)
            {
                return declaringType == typeof(EventSystem) ||
                       declaringType.Name.Contains("EventSystemExtensions") ||
                       declaringType.Name.Contains("EventInfo");
            }

            public string GetDetailedInfo()
            {
                return $"注册类: {ClassName}\n注册方法: {MethodName}\n注册时间: {RegistrationTime}\n程序集: {AssemblyName}";
            }
        }

        /// <summary>
        /// 增强的监听者信息，包含注册位置
        /// </summary>
        public class EnhancedListenerInfo : ListenerInfo
        {
            public RegistrationInfo RegistrationInfo { get; private set; }

            public EnhancedListenerInfo(Delegate callback) : base(callback)
            {
                RegistrationInfo = new RegistrationInfo();
            }

            public override string GetDetailedInfo()
            {
                string baseInfo = base.GetDetailedInfo();
                string registrationInfo = $"\n注册位置: {RegistrationInfo.ClassName}.{RegistrationInfo.MethodName} ({RegistrationInfo.RegistrationTime})";
                return baseInfo + registrationInfo;
            }
        }

        /// <summary>
        /// 事件信息基类
        /// </summary>
        private abstract class EventInfoBase
        {
            public string EventName { get; set; }
            public abstract void Invoke(object parameter);
            public abstract bool TryRemove(Delegate callback);
            public abstract int GetListenerCount();
            public abstract bool IsEmpty();
            public abstract void Clear();
            public abstract List<EnhancedListenerInfo> GetListeners();
        }

        /// <summary>
        /// 无参数事件信息
        /// </summary>
        private sealed class VoidEventInfo : EventInfoBase
        {
            private readonly List<(Action callback, EnhancedListenerInfo info)> _callbacks = new();

            public void AddCallback(Action action)
            {
                var info = new EnhancedListenerInfo(action);
                _callbacks.Add((action, info));
            }

            public override void Invoke(object parameter)
            {
                // 创建副本避免在遍历时修改
                var callbacksCopy = _callbacks.ToArray();
                foreach (var (callback, _) in callbacksCopy)
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EventSystem: 执行回调 {callback?.Method.Name} 时发生异常: {ex.Message}");
                    }
                }
            }

            public override bool TryRemove(Delegate callbackToRemove)
            {
                if (callbackToRemove is not Action action) return false;

                for (int i = _callbacks.Count - 1; i >= 0; i--)
                {
                    if (_callbacks[i].callback.Equals(action))
                    {
                        _callbacks.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            public override int GetListenerCount() => _callbacks.Count;
            public override bool IsEmpty() => _callbacks.Count == 0;
            public override void Clear() => _callbacks.Clear();
            public override List<EnhancedListenerInfo> GetListeners() => _callbacks.Select(c => c.info).ToList();
        }

        /// <summary>
        /// 泛型事件信息
        /// </summary>
        private sealed class GenericEventInfo<T> : EventInfoBase
        {
            private readonly List<(Action<T> callback, EnhancedListenerInfo info)> _callbacks = new();

            public void AddCallback(Action<T> action)
            {
                var info = new EnhancedListenerInfo(action);
                _callbacks.Add((action, info));
            }

            public override void Invoke(object parameter)
            {
                var callbacksCopy = _callbacks.ToArray();
                foreach (var (callback, _) in callbacksCopy)
                {
                    try
                    {
                        T typedParam = ConvertParameter(parameter);
                        callback?.Invoke(typedParam);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EventSystem: 执行回调 {callback?.Method.Name} 时发生异常: {ex.Message}");
                    }
                }
            }

            private T ConvertParameter(object parameter)
            {
                // 如果参数为null且T是值类型，返回默认值
                if (parameter == null)
                {
                    if (typeof(T).IsValueType)
                    {
                        Debug.LogWarning($"EventSystem: 泛型事件期望参数类型 {typeof(T)}，但收到null，使用默认值 {default(T)}");
                        return default(T);
                    }
                    return default(T);
                }

                // 如果参数类型完全匹配
                if (parameter is T typedParam)
                    return typedParam;

                // 尝试类型转换
                try
                {
                    if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal))
                    {
                        var converted = Convert.ChangeType(parameter, typeof(T));
                        return (T)converted;
                    }

                    throw new InvalidCastException($"无法转换类型 {parameter.GetType()} 到 {typeof(T)}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventSystem: 参数类型转换失败 - 期望 {typeof(T)}, 实际 {parameter.GetType()}: {ex.Message}");
                    return default(T);
                }
            }

            public override bool TryRemove(Delegate callbackToRemove)
            {
                if (callbackToRemove is not Action<T> action) return false;

                for (int i = _callbacks.Count - 1; i >= 0; i--)
                {
                    if (_callbacks[i].callback.Equals(action))
                    {
                        _callbacks.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            public override int GetListenerCount() => _callbacks.Count;
            public override bool IsEmpty() => _callbacks.Count == 0;
            public override void Clear() => _callbacks.Clear();
            public override List<EnhancedListenerInfo> GetListeners() => _callbacks.Select(c => c.info).ToList();
        }

        #endregion

        #region Constants and Fields

        private const int MAX_TRIGGER_HISTORY = 10;

        // 事件存储
        private readonly Dictionary<string, EventInfoBase> _eventDict = new();

        // 触发位置记录
        private readonly Dictionary<string, CurrentTriggerInfo> _currentTriggers = new();
        private readonly Dictionary<string, List<TriggerInfo>> _triggerHistory = new();

        // 缓存列表，避免重复分配
        private readonly List<string> _emptyEventNames = new();
        private readonly List<EnhancedListenerInfo> _emptyListeners = new();
        private readonly List<TriggerInfo> _emptyTriggers = new();

        #endregion

        #region Public API - Event Registration

        /// <summary>
        /// 注册无参数事件
        /// </summary>
        public void RegisterEvent(string eventName, Action callback)
        {
            if (!ValidateEventRegistration(eventName, callback)) return;

            var eventInfo = GetOrCreateEventInfo<VoidEventInfo>(eventName);
            eventInfo?.AddCallback(callback);
        }

        /// <summary>
        /// 注册泛型事件
        /// </summary>
        public void RegisterEvent<T>(string eventName, Action<T> callback)
        {
            if (!ValidateEventRegistration(eventName, callback)) return;

            var eventInfo = GetOrCreateEventInfo<GenericEventInfo<T>>(eventName);
            eventInfo?.AddCallback(callback);
        }

        /// <summary>
        /// 注册object参数事件（向后兼容）
        /// </summary>
        public void RegisterEvent(string eventName, Action<object> callback)
        {
            RegisterEvent<object>(eventName, callback);
        }

        #endregion

        #region Public API - Event Unregistration

        /// <summary>
        /// 注销无参数事件
        /// </summary>
        public void UnregisterEvent(string eventName, Action callback)
        {
            if (!ValidateEventUnregistration(eventName, callback)) return;
            TryRemoveCallback(eventName, callback);
        }

        /// <summary>
        /// 注销泛型事件
        /// </summary>
        public void UnregisterEvent<T>(string eventName, Action<T> callback)
        {
            if (!ValidateEventUnregistration(eventName, callback)) return;
            TryRemoveCallback(eventName, callback);
        }

        /// <summary>
        /// 注销object参数事件（向后兼容）
        /// </summary>
        public void UnregisterEvent(string eventName, Action<object> callback)
        {
            UnregisterEvent<object>(eventName, callback);
        }

        #endregion

        #region Public API - Event Triggering

        /// <summary>
        /// 触发无参数事件
        /// </summary>
        public void TriggerEvent(string eventName)
        {
            RecordTriggerInfo(eventName);
            TriggerEventInternal(eventName, null);
        }

        /// <summary>
        /// 触发泛型事件
        /// </summary>
        public void TriggerEvent<T>(string eventName, T parameter)
        {
            RecordTriggerInfo(eventName);
            TriggerEventInternal(eventName, parameter);
        }

        /// <summary>
        /// 触发object参数事件（向后兼容）
        /// </summary>
        public void TriggerEvent(string eventName, object parameter)
        {
            RecordTriggerInfo(eventName);
            TriggerEventInternal(eventName, parameter);
        }

        #endregion

        #region Public API - Advanced Features

        /// <summary>
        /// 批量注册无参数事件
        /// </summary>
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
        public void RegisterEvents<T>(Dictionary<string, Action<T>> events)
        {
            foreach (var kvp in events)
            {
                RegisterEvent(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 一次性无参数事件（触发后自动注销）
        /// </summary>
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
        /// 延迟触发无参数事件
        /// </summary>
        public void TriggerEventDelayed(string eventName, float delay)
        {
            RecordTriggerInfo(eventName);
            StartCoroutine(TriggerEventDelayedCoroutine(eventName, null, delay));
        }

        /// <summary>
        /// 延迟触发泛型事件
        /// </summary>
        public void TriggerEventDelayed<T>(string eventName, T parameter, float delay)
        {
            RecordTriggerInfo(eventName);
            StartCoroutine(TriggerEventDelayedCoroutine(eventName, parameter, delay));
        }

        #endregion

        #region Public API - Information and Debug

        /// <summary>
        /// 获取指定事件的监听者列表
        /// </summary>
        public List<EnhancedListenerInfo> GetEventListeners(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                return _emptyListeners;
            }
            return eventInfo.GetListeners();
        }

        /// <summary>
        /// 获取指定事件的当前触发位置
        /// </summary>
        public CurrentTriggerInfo GetCurrentTrigger(string eventName)
        {
            return !string.IsNullOrEmpty(eventName) && _currentTriggers.TryGetValue(eventName, out var trigger)
                ? trigger : null;
        }

        /// <summary>
        /// 获取指定事件的触发历史
        /// </summary>
        public List<TriggerInfo> GetEventTriggers(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_triggerHistory.TryGetValue(eventName, out var history))
            {
                return _emptyTriggers;
            }
            return new List<TriggerInfo>(history);
        }

        /// <summary>
        /// 获取所有事件名称
        /// </summary>
        public string[] GetAllEventNames()
        {
            return _eventDict.Keys.Where(key => !_eventDict[key].IsEmpty()).ToArray();
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
            return string.IsNullOrEmpty(eventName) || !_eventDict.TryGetValue(eventName, out var eventInfo)
                ? 0 : eventInfo.GetListenerCount();
        }

        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public EventStatistics GetEventStatistics()
        {
            var stats = new EventStatistics
            {
                TotalEvents = _eventDict.Count,
                TotalListeners = _eventDict.Values.Sum(e => e.GetListenerCount())
            };

            foreach (var eventInfo in _eventDict.Values)
            {
                if (eventInfo is VoidEventInfo)
                    stats.VoidEvents++;
                else
                    stats.GenericEvents++;
            }

            return stats;
        }

        #endregion

        #region Public API - Cleanup

        /// <summary>
        /// 清理触发位置记录
        /// </summary>
        public void ClearTriggerInfo(string eventName = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                _currentTriggers.Clear();
            }
            else
            {
                _currentTriggers.Remove(eventName);
            }
        }

        /// <summary>
        /// 清理触发历史
        /// </summary>
        public void ClearTriggerHistory(string eventName = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                _triggerHistory.Clear();
            }
            else
            {
                _triggerHistory.Remove(eventName);
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
                _eventDict.Remove(eventName);
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
            _currentTriggers.Clear();
            _triggerHistory.Clear();
        }

        /// <summary>
        /// 定期清理空事件
        /// </summary>
        public void CleanupEmptyEvents()
        {
            _emptyEventNames.Clear();

            foreach (var kvp in _eventDict)
            {
                if (kvp.Value.IsEmpty())
                {
                    _emptyEventNames.Add(kvp.Key);
                }
            }

            foreach (string eventName in _emptyEventNames)
            {
                _eventDict.Remove(eventName);
            }
        }

        #endregion

        #region Public API - Debug Methods

        /// <summary>
        /// 打印指定事件的监听者信息
        /// </summary>
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
        /// 打印所有事件信息
        /// </summary>
        public void DebugPrintAllEvents()
        {
            Debug.Log("<color=yellow>=== EventSystem 所有事件信息 ===</color>");
            foreach (var kvp in _eventDict.Where(kvp => !kvp.Value.IsEmpty()))
            {
                string eventType = kvp.Value switch
                {
                    VoidEventInfo => "Void",
                    _ => $"Generic<{kvp.Value.GetType().GetGenericArguments().FirstOrDefault()?.Name ?? "Unknown"}>"
                };

                var listeners = kvp.Value.GetListeners();
                string listenerNames = string.Join(", ", listeners.Select(l => l.ToString()));

                Debug.Log($"事件: {kvp.Key}, 类型: {eventType}, 监听者数量: {kvp.Value.GetListenerCount()}, 监听者: [{listenerNames}]");
            }
            Debug.Log("<color=yellow>=== EventSystem 事件信息结束 ===</color>");
        }

        /// <summary>
        /// 查找指定GameObject上的所有事件监听
        /// </summary>
        public List<string> FindEventsListenedByGameObject(GameObject gameObject)
        {
            var listenedEvents = new List<string>();

            foreach (var kvp in _eventDict)
            {
                var listeners = kvp.Value.GetListeners();
                if (listeners.Any(listener => listener.Target is MonoBehaviour mono && mono.gameObject == gameObject))
                {
                    listenedEvents.Add(kvp.Key);
                }
            }

            return listenedEvents;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 内部触发事件方法
        /// </summary>
        private void TriggerEventInternal(string eventName, object parameter)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError("EventSystem: 事件名称不能为空");
                return;
            }

            if (_eventDict.TryGetValue(eventName, out var eventInfo) && !eventInfo.IsEmpty())
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
        }

        /// <summary>
        /// 记录触发信息
        /// </summary>
        private void RecordTriggerInfo(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            // 记录当前触发位置
            _currentTriggers[eventName] = new CurrentTriggerInfo();

            // 记录触发历史
            var triggerInfo = new TriggerInfo();

            if (!_triggerHistory.TryGetValue(eventName, out var history))
            {
                history = new List<TriggerInfo>();
                _triggerHistory[eventName] = history;
            }

            history.Add(triggerInfo);

            // 保持历史记录数量限制
            if (history.Count > MAX_TRIGGER_HISTORY)
            {
                history.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取或创建事件信息
        /// </summary>
        private T GetOrCreateEventInfo<T>(string eventName) where T : EventInfoBase, new()
        {
            if (!_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                eventInfo = new T { EventName = eventName };
                _eventDict[eventName] = eventInfo;
            }

            if (eventInfo is T typedEventInfo)
            {
                return typedEventInfo;
            }

            Debug.LogError($"EventSystem: 事件 {eventName} 类型不匹配，期望 {typeof(T).Name}, 实际 {eventInfo.GetType().Name}");
            return null;
        }

        /// <summary>
        /// 尝试移除回调
        /// </summary>
        private void TryRemoveCallback(string eventName, Delegate callback)
        {
            if (_eventDict.TryGetValue(eventName, out var eventInfo))
            {
                if (eventInfo.TryRemove(callback))
                {
                    if (eventInfo.IsEmpty())
                    {
                        _eventDict.Remove(eventName);
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
            return ValidateEventRegistration(eventName, callback);
        }

        /// <summary>
        /// 延迟触发事件协程
        /// </summary>
        private IEnumerator TriggerEventDelayedCoroutine(string eventName, object parameter, float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerEventInternal(eventName, parameter);
        }

        #endregion

        #region Unity Lifecycle

        protected override void OnDestroy()
        {
            ClearAllEvents();
            base.OnDestroy();
        }

        #endregion
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