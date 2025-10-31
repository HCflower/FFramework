using System.Collections.Generic;
using FFramework.Utility;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

namespace SmallFramework.Editor
{
    [CustomEditor(typeof(EventSystem))]
    public class EventSystemInspector : UnityEditor.Editor
    {
        #region Private Fields
        private EventSystem eventSystem;
        private string searchFilter = "";
        private bool autoRefresh = false;
        private float refreshInterval = 1.0f;
        private double nextRefreshTime;
        private Vector2 scrollPosition;

        // 保存折叠状态
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        // 缓存的事件数据 - 优化为只在结构变化时重建
        private Dictionary<string, List<EventListenerInfo>> eventListeners = new Dictionary<string, List<EventListenerInfo>>();
        private Dictionary<string, List<TriggerDisplayInfo>> cachedTriggers = new Dictionary<string, List<TriggerDisplayInfo>>();
        private int totalListeners = 0;
        private string lastUpdateTime = "";

        // 数据版本控制，避免不必要的UI重建
        private int lastEventStructureHash = 0;
        #endregion

        #region Data Structures
        /// <summary>
        /// 事件监听器信息
        /// </summary>
        private class EventListenerInfo
        {
            public string EventName;
            public object Target;
            public string ClassName;
            public string MethodName;
            public string AssemblyName;
            public Delegate Callback;

            public string GetDetailedInfo()
            {
                return $"事件: {EventName}\n类: {ClassName}\n方法: {MethodName}\n程序集: {AssemblyName}";
            }
        }

        /// <summary>
        /// 触发位置显示信息 - 用于缓存和优化显示
        /// </summary>
        private class TriggerDisplayInfo
        {
            public string EventName;
            public string ClassName;
            public string MethodName;
            public string TriggerTime;
            public string LocationKey;

            public TriggerDisplayInfo(EventSystem.TriggerInfo triggerInfo, string eventName)
            {
                EventName = eventName;
                ClassName = triggerInfo.ClassName;
                MethodName = triggerInfo.MethodName;
                TriggerTime = triggerInfo.TriggerTime;
                LocationKey = $"{ClassName}.{MethodName}";
            }

            public void UpdateTime(string newTime)
            {
                TriggerTime = newTime;
            }

            public string GetDetailedInfo()
            {
                return $"触发类: {ClassName}\n触发方法: {MethodName}\n触发时间: {TriggerTime}";
            }
        }
        #endregion

        #region Unity Methods
        public override void OnInspectorGUI()
        {
            eventSystem = (EventSystem)target;

            if (eventSystem == null)
            {
                EditorGUILayout.HelpBox("未找到 EventSystem 实例", MessageType.Error);
                return;
            }

            // 整体滚动视图
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(2);
            DrawControlBar();
            EditorGUILayout.Space(2);
            DrawSummary();
            EditorGUILayout.Space(2);
            DrawEventList();

            EditorGUILayout.EndScrollView();

            // 处理自动刷新
            HandleAutoRefresh();
        }
        #endregion

        #region Drawing Methods

        /// <summary>
        /// 绘制标题
        /// </summary>
        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };

            EditorGUILayout.LabelField("EventSystem 事件监视器", titleStyle);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制控制栏
        /// </summary>
        private void DrawControlBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 搜索框
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(40), GUILayout.Height(18));
            string newSearchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Height(18));
            if (newSearchFilter != searchFilter)
            {
                searchFilter = newSearchFilter;
                RefreshEventData(forceRefresh: true);
            }

            if (GUILayout.Button("清除", GUILayout.Width(50), GUILayout.Height(18)))
            {
                searchFilter = "";
                RefreshEventData(forceRefresh: true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 控制按钮行
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新", GUILayout.Height(18), GUILayout.Width(60)))
            {
                RefreshEventData(forceRefresh: true);
            }

            if (GUILayout.Button("展开", GUILayout.Height(18), GUILayout.Width(60)))
            {
                SetAllFoldouts(true);
            }

            if (GUILayout.Button("折叠", GUILayout.Height(18), GUILayout.Width(60)))
            {
                SetAllFoldouts(false);
            }

            // 自动刷新开关
            bool newAutoRefresh = EditorGUILayout.ToggleLeft("自动刷新(1s)", autoRefresh, GUILayout.Width(120));
            if (newAutoRefresh != autoRefresh)
            {
                autoRefresh = newAutoRefresh;
                if (autoRefresh)
                {
                    // 延迟到下一帧刷新，避免布局错乱
                    EditorApplication.delayCall += () =>
                    {
                        nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                        RefreshEventData();
                    };
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制汇总信息
        /// </summary>
        private void DrawSummary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("汇总信息", EditorStyles.boldLabel);

            // 使用紧凑的网格布局，每个信息占用固定宽度
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"事件数: {eventListeners.Count}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"监听者: {totalListeners}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"更新: {lastUpdateTime}", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制事件列表
        /// </summary>
        private void DrawEventList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(1);
            EditorGUILayout.LabelField("事件列表", EditorStyles.boldLabel);

            if (eventListeners.Count == 0)
            {
                EditorGUILayout.LabelField("当前没有已注册且含监听者的事件", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.Space(1);

            // 计算内容高度进行自适应
            float totalContentHeight = CalculateEventListHeight();
            float maxHeight = Mathf.Min(totalContentHeight, 500f); // 最大高度限制为500px，避免过高

            // 内部滚动视图，高度自适应但有最大限制
            Vector2 innerScrollPos = EditorGUILayout.BeginScrollView(Vector2.zero,
                totalContentHeight > 500f ? GUILayout.Height(maxHeight) : GUILayout.ExpandHeight(false));

            foreach (var kvp in eventListeners.OrderBy(x => x.Key))
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                DrawEventGroup(kvp.Key, kvp.Value);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 计算事件列表内容的总高度
        /// </summary>
        private float CalculateEventListHeight()
        {
            float totalHeight = 0f;

            foreach (var kvp in eventListeners.OrderBy(x => x.Key))
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                // 事件组标题高度
                totalHeight += 20f; // Foldout 标题高度

                // 如果展开，计算监听器项高度
                bool isExpanded = foldoutStates.ContainsKey(kvp.Key) ? foldoutStates[kvp.Key] : false;
                if (isExpanded)
                {
                    totalHeight += 4f; // 间距
                    totalHeight += kvp.Value.Count * 24f; // 每个监听器项高度(20px + 4px间距)

                    // 添加触发位置区域高度
                    if (cachedTriggers.ContainsKey(kvp.Key))
                    {
                        var triggers = cachedTriggers[kvp.Key];
                        totalHeight += 40f; // 标题区域
                        totalHeight += Math.Min(triggers.Count, 5) * 20f; // 最多显示5条记录
                    }
                }

                totalHeight += 4f; // 事件组间距
            }

            // 添加一些额外的边距
            totalHeight += 10f;

            return totalHeight;
        }

        /// <summary>
        /// 绘制单个事件组
        /// </summary>
        private void DrawEventGroup(string eventName, List<EventListenerInfo> listeners)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 折叠组标题
            bool isExpanded = foldoutStates.ContainsKey(eventName) ? foldoutStates[eventName] : false;

            // 事件组标题按钮
            EditorGUILayout.BeginHorizontal();
            string buttonText = $"{(isExpanded ? "▼" : "▶")} {eventName} (监听:{listeners.Count})";

            // 自定义左对齐按钮样式
            GUIStyle leftButtonStyle = new GUIStyle(EditorStyles.miniButton);
            leftButtonStyle.alignment = TextAnchor.MiddleLeft;
            leftButtonStyle.padding = new RectOffset(8, 4, 0, 0);

            if (GUILayout.Button(buttonText, leftButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(22)))
            {
                foldoutStates[eventName] = !isExpanded;
            }
            EditorGUILayout.EndHorizontal();

            // 如果展开，显示监听器和触发位置
            if (isExpanded)
            {
                EditorGUILayout.Space(2);

                // 监听者区域 - 橙黄色背景
                DrawListenersSection(listeners);

                EditorGUILayout.Space(3);

                // 触发位置区域 - 天青色背景
                DrawTriggersSection(eventName);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }

        /// <summary>
        /// 绘制监听者区域
        /// </summary>
        private void DrawListenersSection(List<EventListenerInfo> listeners)
        {
            // 橙黄色背景
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.2f, 0.3f); // 橙黄色，半透明

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = new Color(0.8f, 0.5f, 0f); // 深橙色
            EditorGUILayout.LabelField($"监听者 ({listeners.Count})", titleStyle);

            // 监听者列表
            foreach (var info in listeners)
            {
                EditorGUILayout.BeginHorizontal();

                // 主要信息按钮
                string shortClassName = GetShortClassName(info.ClassName);
                string displayText = $"{shortClassName}.{TruncateString(info.MethodName, 12)}()";

                // GameObject信息
                string goPart = "";
                if (info.Target is MonoBehaviour mb && mb != null)
                    goPart = $" [{TruncateString(mb.gameObject.name, 10)}]";
                else if (info.Target == null)
                    goPart = " [静态]";

                string fullText = $"{displayText}{goPart}";

                // 主信息按钮
                if (GUILayout.Button(fullText, EditorStyles.miniButton, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
                {
                    EditorUtility.DisplayDialog("监听者详情", info.GetDetailedInfo(), "确定");
                }

                // 间隔2个单位
                GUILayout.Space(2);

                // 定位按钮
                if (info.Target is MonoBehaviour mono && mono != null)
                {
                    if (GUILayout.Button("定位", EditorStyles.miniButtonRight, GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        EditorGUIUtility.PingObject(mono.gameObject);
                        Selection.activeObject = mono.gameObject;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制触发位置区域 - 优化版本，减少UI重建
        /// </summary>
        private void DrawTriggersSection(string eventName)
        {
            // 天青色背景
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f, 0.3f); // 天青色，半透明

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // 标题行
            EditorGUILayout.BeginHorizontal();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = new Color(0f, 0.5f, 0.8f); // 深天青色

            // 使用缓存的触发数据
            var cachedTriggerList = cachedTriggers.ContainsKey(eventName) ? cachedTriggers[eventName] : new List<TriggerDisplayInfo>();
            EditorGUILayout.LabelField($"触发位置 ({cachedTriggerList.Count})", titleStyle);

            // 清理按钮
            if (cachedTriggerList.Count > 0)
            {
                if (GUILayout.Button("清理", EditorStyles.miniButton, GUILayout.Width(40), GUILayout.Height(16)))
                {
                    eventSystem.ClearTriggerHistory(eventName);
                    cachedTriggers[eventName] = new List<TriggerDisplayInfo>();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 触发位置列表
            if (cachedTriggerList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无触发记录", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                // 显示最近的5条记录，按时间倒序
                var recentTriggers = cachedTriggerList.TakeLast(5).Reverse().ToList();

                foreach (var trigger in recentTriggers)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 触发位置信息按钮
                    string shortClassName = GetShortClassName(trigger.ClassName);
                    string displayText = $"{shortClassName}.{TruncateString(trigger.MethodName, 12)}() [{trigger.TriggerTime}]";

                    if (GUILayout.Button(displayText, EditorStyles.miniButton, GUILayout.ExpandWidth(true), GUILayout.Height(18)))
                    {
                        EditorUtility.DisplayDialog("触发位置详情", trigger.GetDetailedInfo(), "确定");
                    }

                    // 间隔2个单位
                    GUILayout.Space(2);

                    // 定位按钮
                    if (GUILayout.Button("定位", EditorStyles.miniButtonRight, GUILayout.Width(40), GUILayout.Height(18)))
                    {
                        // TODO:
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // 如果有更多记录，显示提示
                if (cachedTriggerList.Count > 5)
                {
                    EditorGUILayout.LabelField($"... 还有 {cachedTriggerList.Count - 5} 条历史记录", EditorStyles.centeredGreyMiniLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods
        /// <summary>
        /// 获取简短的类名
        /// </summary>
        private string GetShortClassName(string fullClassName)
        {
            if (string.IsNullOrEmpty(fullClassName))
                return "Unknown";

            // 移除泛型标记
            int genericIndex = fullClassName.IndexOf('`');
            if (genericIndex >= 0)
                fullClassName = fullClassName.Substring(0, genericIndex);

            // 只保留最后一部分类名
            string[] parts = fullClassName.Split('.');
            string shortName = parts[parts.Length - 1];

            // 限制长度到更小
            return TruncateString(shortName, 12);
        }

        /// <summary>
        /// 截断字符串
        /// </summary>
        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 1) + "..";
        }
        #endregion

        #region Data Management
        /// <summary>
        /// 刷新事件数据 - 优化版本，减少不必要的重建
        /// </summary>
        private void RefreshEventData(bool forceRefresh = false)
        {
            if (eventSystem == null)
                return;

            try
            {
                // 检查事件结构是否发生变化
                string[] eventNames = eventSystem.GetAllEventNames();
                int currentEventStructureHash = GetEventStructureHash(eventNames);

                bool structureChanged = forceRefresh || currentEventStructureHash != lastEventStructureHash;

                if (structureChanged)
                {
                    // 事件结构发生变化，需要重新构建监听者数据
                    RefreshListenerData(eventNames);
                    lastEventStructureHash = currentEventStructureHash;
                }

                // 更新触发位置数据（这个相对频繁，但我们优化了处理方式）
                RefreshTriggerData(eventNames);

                lastUpdateTime = System.DateTime.Now.ToString("HH:mm:ss");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"刷新事件数据失败: {ex.Message}");
                eventListeners.Clear();
                cachedTriggers.Clear();
                totalListeners = 0;
                lastUpdateTime = "获取失败";
            }

            Repaint();
        }

        /// <summary>
        /// 刷新监听者数据
        /// </summary>
        private void RefreshListenerData(string[] eventNames)
        {
            eventListeners.Clear();
            totalListeners = 0;

            if (eventNames != null && eventNames.Length > 0)
            {
                foreach (string eventName in eventNames)
                {
                    var enhancedListeners = eventSystem.GetEventListeners(eventName);
                    if (enhancedListeners != null && enhancedListeners.Count > 0)
                    {
                        var eventInfoList = new List<EventListenerInfo>();

                        foreach (var listener in enhancedListeners)
                        {
                            var info = new EventListenerInfo
                            {
                                EventName = eventName,
                                Target = listener.Target,
                                ClassName = listener.ClassName,
                                MethodName = listener.MethodName,
                                AssemblyName = listener.AssemblyName,
                                Callback = listener.Callback
                            };
                            eventInfoList.Add(info);
                        }

                        // 应用搜索过滤器
                        if (!string.IsNullOrEmpty(searchFilter))
                        {
                            bool matchesFilter = eventName.ToLower().Contains(searchFilter.ToLower()) ||
                                               eventInfoList.Any(l =>
                                                   (l.ClassName?.ToLower().Contains(searchFilter.ToLower()) ?? false) ||
                                                   (l.MethodName?.ToLower().Contains(searchFilter.ToLower()) ?? false) ||
                                                   (l.AssemblyName?.ToLower().Contains(searchFilter.ToLower()) ?? false));

                            if (!matchesFilter)
                                continue;
                        }

                        // 添加到最终列表
                        eventListeners[eventName] = eventInfoList;
                        totalListeners += eventInfoList.Count;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新触发位置数据 - 优化版本，同一位置只更新时间
        /// </summary>
        private void RefreshTriggerData(string[] eventNames)
        {
            if (eventNames == null) return;

            foreach (string eventName in eventNames)
            {
                var newTriggers = eventSystem.GetEventTriggers(eventName);
                if (newTriggers == null || newTriggers.Count == 0)
                {
                    // 如果没有触发记录，确保缓存中也为空
                    if (!cachedTriggers.ContainsKey(eventName))
                    {
                        cachedTriggers[eventName] = new List<TriggerDisplayInfo>();
                    }
                    continue;
                }

                // 获取或创建缓存列表
                if (!cachedTriggers.ContainsKey(eventName))
                {
                    cachedTriggers[eventName] = new List<TriggerDisplayInfo>();
                }

                var cachedList = cachedTriggers[eventName];

                // 处理新的触发记录
                foreach (var trigger in newTriggers)
                {
                    var newDisplayInfo = new TriggerDisplayInfo(trigger, eventName);

                    // 查找是否存在相同位置的记录
                    var existingInfo = cachedList.FirstOrDefault(cached =>
                        cached.LocationKey == newDisplayInfo.LocationKey);

                    if (existingInfo != null)
                    {
                        // 同一位置，只更新时间
                        existingInfo.UpdateTime(newDisplayInfo.TriggerTime);
                    }
                    else
                    {
                        // 新位置，添加到列表
                        cachedList.Add(newDisplayInfo);
                    }
                }

                // 保持缓存大小限制
                if (cachedList.Count > 20) // 保留更多历史记录用于去重
                {
                    // 移除最旧的记录，但保留最近的不同位置
                    var groupedByLocation = cachedList.GroupBy(t => t.LocationKey)
                                                     .Select(g => g.OrderByDescending(t => t.TriggerTime).First())
                                                     .OrderByDescending(t => t.TriggerTime)
                                                     .Take(15)
                                                     .ToList();

                    cachedTriggers[eventName] = groupedByLocation;
                }
            }

            // 清理不再存在的事件的缓存
            var currentEventNames = new HashSet<string>(eventNames);
            var cachedEventNames = cachedTriggers.Keys.ToList();

            foreach (string cachedEventName in cachedEventNames)
            {
                if (!currentEventNames.Contains(cachedEventName))
                {
                    cachedTriggers.Remove(cachedEventName);
                }
            }
        }

        /// <summary>
        /// 计算事件结构哈希值，用于检测结构变化
        /// </summary>
        private int GetEventStructureHash(string[] eventNames)
        {
            if (eventNames == null || eventNames.Length == 0)
                return 0;

            // 使用事件名称和监听者数量计算哈希
            int hash = 17;
            foreach (string eventName in eventNames)
            {
                hash = hash * 31 + eventName.GetHashCode();
                hash = hash * 31 + eventSystem.GetListenerCount(eventName);
            }
            return hash;
        }

        /// <summary>
        /// 设置所有折叠状态
        /// </summary>
        private void SetAllFoldouts(bool expanded)
        {
            var eventNames = eventListeners.Keys.ToList();
            foreach (string eventName in eventNames)
            {
                foldoutStates[eventName] = expanded;
            }
            Repaint();
        }

        /// <summary>
        /// 处理自动刷新
        /// </summary>
        private void HandleAutoRefresh()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup >= nextRefreshTime)
            {
                // 避免在Layout事件中刷新，防止GUI错误
                if (Event.current == null || Event.current.type != EventType.Layout)
                {
                    RefreshEventData();
                    nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                }
                else
                {
                    // 延迟到下一帧
                    EditorApplication.delayCall += () =>
                    {
                        if (autoRefresh) // 再次检查，因为可能在延迟期间被关闭
                        {
                            RefreshEventData();
                            nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                        }
                    };
                }
            }

            // 确保在自动刷新模式下持续重绘
            if (autoRefresh)
            {
                Repaint();
            }
        }

        #endregion

        #region Unity Events
        /// <summary>
        /// 启用时初始化
        /// </summary>
        private void OnEnable()
        {
            // 初始化数据
            RefreshEventData(forceRefresh: true);

            // 如果开启了自动刷新，设置下次刷新时间
            if (autoRefresh)
            {
                nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
            }
        }

        /// <summary>
        /// 禁用时清理
        /// </summary>
        private void OnDisable()
        {
            // 可以在这里做一些清理工作
            autoRefresh = false;
        }
        #endregion
    }
}