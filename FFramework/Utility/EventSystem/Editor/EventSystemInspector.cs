using System.Collections.Generic;
using FFramework.Utility;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

namespace FFramework.Editor
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

        // 缓存的事件数据
        private Dictionary<string, List<EnhancedListenerInfo>> eventListeners = new Dictionary<string, List<EnhancedListenerInfo>>();
        private Dictionary<string, List<EnhancedTriggerInfo>> eventTriggers = new Dictionary<string, List<EnhancedTriggerInfo>>();
        private int totalListeners = 0;
        private string lastUpdateTime = "";

        // 数据版本控制
        private int lastEventStructureHash = 0;
        #endregion

        #region Data Structures
        /// <summary>
        /// 增强的监听器信息
        /// </summary>
        private class EnhancedListenerInfo
        {
            public string EventName;
            public object Target;
            public string ClassName;
            public string MethodName;
            public string AssemblyName;
            public Delegate Callback;

            public EnhancedListenerInfo(EventSystem.ListenerInfo listenerInfo, string eventName)
            {
                EventName = eventName;
                Target = listenerInfo.Target;
                Callback = listenerInfo.Callback;

                if (listenerInfo.Callback != null)
                {
                    var method = listenerInfo.Callback.Method;
                    MethodName = method.Name;
                    ClassName = method.DeclaringType?.Name ?? "Unknown";
                    AssemblyName = method.DeclaringType?.Assembly?.GetName()?.Name ?? "Unknown";
                }
                else
                {
                    MethodName = ClassName = AssemblyName = "Unknown";
                }
            }

            public string GetDetailedInfo()
            {
                return $"事件: {EventName}\n类: {ClassName}\n方法: {MethodName}\n程序集: {AssemblyName}";
            }
        }

        /// <summary>
        /// 增强的触发器信息
        /// </summary>
        private class EnhancedTriggerInfo
        {
            public string EventName;
            public string CallerClass;
            public string CallerMethod;
            public DateTime TriggerTime;
            public object TriggerContext;
            public int TriggerCount;

            public EnhancedTriggerInfo(EventSystem.TriggerInfo triggerInfo, string eventName)
            {
                EventName = eventName;
                CallerClass = triggerInfo.CallerClass ?? "Unknown";
                CallerMethod = triggerInfo.CallerMethod ?? "Unknown";
                TriggerTime = triggerInfo.TriggerTime;
                TriggerContext = triggerInfo.TriggerContext;
                TriggerCount = 1;
            }

            public void UpdateTriggerTime(DateTime newTime)
            {
                TriggerTime = newTime;
                TriggerCount++;
            }

            public string GetLocationKey()
            {
                return $"{CallerClass ?? "Unknown"}.{CallerMethod ?? "Unknown"}";
            }

            public string GetDetailedInfo()
            {
                var contextInfo = GetContextDescription(TriggerContext);

                return $"事件: {EventName}\n" +
                       $"类: {CallerClass}\n" +
                       $"方法: {CallerMethod}\n" +
                       $"触发次数: {TriggerCount}\n" +
                       $"最后触发: {TriggerTime:HH:mm:ss.fff}\n" +
                       $"上下文: {contextInfo}";
            }

            private string GetContextDescription(object context)
            {
                if (context == null) return "无";

                return context switch
                {
                    MonoBehaviour mono when mono != null =>
                        $"{mono.gameObject.name} ({mono.GetType().Name})",
                    MonoBehaviour =>
                        "MonoBehaviour (已销毁)",
                    GameObject go when go != null =>
                        $"{go.name} (GameObject)",
                    GameObject =>
                        "GameObject (已销毁)",
                    string str =>
                        $"\"{str}\"",
                    _ =>
                        $"{context.GetType().Name} 实例"
                };
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

            if (GUILayout.Button("清理无效", GUILayout.Height(18), GUILayout.Width(80)))
            {
                eventSystem.CleanupInvalidListeners();
                RefreshEventData(forceRefresh: true);
            }

            // 自动刷新开关
            bool newAutoRefresh = EditorGUILayout.ToggleLeft("自动刷新(1s)", autoRefresh, GUILayout.Width(120));
            if (newAutoRefresh != autoRefresh)
            {
                autoRefresh = newAutoRefresh;
                if (autoRefresh)
                {
                    nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                    RefreshEventData();
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"事件数: {eventListeners.Count}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"监听者: {totalListeners}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"更新: {lastUpdateTime}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制事件列表
        /// </summary>
        private void DrawEventList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("事件列表", EditorStyles.boldLabel);

            if (eventListeners.Count == 0)
            {
                EditorGUILayout.LabelField("当前没有已注册且含监听者的事件", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(2);

            foreach (var kvp in eventListeners.OrderBy(x => x.Key))
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                    continue;

                DrawEventGroup(kvp.Key, kvp.Value);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制单个事件组
        /// </summary>
        private void DrawEventGroup(string eventName, List<EnhancedListenerInfo> listeners)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 折叠组标题
            bool isExpanded = foldoutStates.ContainsKey(eventName) ? foldoutStates[eventName] : false;

            // 事件组标题按钮
            EditorGUILayout.BeginHorizontal();
            string buttonText = $"{(isExpanded ? "▼" : "▶")} {eventName} (监听:{listeners.Count})";

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

                // 监听者区域
                DrawListenersSection(listeners);

                EditorGUILayout.Space(3);

                // 触发信息区域
                DrawTriggerSection(eventName);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }

        /// <summary>
        /// 绘制监听者区域
        /// </summary>
        private void DrawListenersSection(List<EnhancedListenerInfo> listeners)
        {
            // 天青色背景
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f, 0.3f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = new Color(0f, 0.5f, 0.8f);
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
        /// 绘制触发信息区域
        /// </summary>
        private void DrawTriggerSection(string eventName)
        {
            // 橙黄色背景
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.2f, 0.3f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // 标题行
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = new Color(0.8f, 0.5f, 0f);

            EditorGUILayout.LabelField("触发位置信息", titleStyle);

            // 触发信息内容
            if (eventTriggers.ContainsKey(eventName) && eventTriggers[eventName] != null && eventTriggers[eventName].Count > 0)
            {
                var triggers = eventTriggers[eventName].OrderByDescending(t => t.TriggerTime).ToList();

                foreach (var triggerInfo in triggers)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 触发位置信息按钮
                    string shortClassName = GetShortClassName(triggerInfo.CallerClass);
                    string displayText = $"{shortClassName}.{TruncateString(triggerInfo.CallerMethod, 10)}()";
                    string timeText = triggerInfo.TriggerTime.ToString("HH:mm:ss.fff");
                    string countText = triggerInfo.TriggerCount > 1 ? $"×{triggerInfo.TriggerCount}" : "";

                    // 添加上下文信息到显示文本
                    string contextPart = GetContextDisplayText(triggerInfo.TriggerContext);
                    string fullText = $"{displayText} {contextPart} [{timeText}] {countText}";

                    // 设置按钮颜色（最新的触发显示为更亮的颜色）
                    var btnColor = GUI.backgroundColor;
                    if (triggerInfo == triggers.First())
                    {
                        GUI.backgroundColor = new Color(1f, 0.8f, 0.2f, 0.3f); // 最新触发为橙色
                    }

                    if (GUILayout.Button(fullText, EditorStyles.miniButton, GUILayout.ExpandWidth(true), GUILayout.Height(18)))
                    {
                        EditorUtility.DisplayDialog("触发信息详情", triggerInfo.GetDetailedInfo(), "确定");
                    }

                    GUI.backgroundColor = btnColor;

                    // 不再显示定位按钮

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(1);
                }
            }
            else
            {
                EditorGUILayout.LabelField("暂无触发记录", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 获取上下文的显示文本（简短版本）
        /// </summary>
        private string GetContextDisplayText(object context)
        {
            if (context == null) return "";

            return context switch
            {
                MonoBehaviour mono when mono != null => $"[{TruncateString(mono.gameObject.name, 8)}]",
                MonoBehaviour => "[已销毁]",
                GameObject go when go != null => $"[{TruncateString(go.name, 8)}]",
                GameObject => "[已销毁]",
                string str when !string.IsNullOrEmpty(str) => $"[{TruncateString(str, 8)}]",
                _ => $"[{context.GetType().Name}]"
            };
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
        /// 刷新事件数据
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
                    RefreshListenerData(eventNames);
                    lastEventStructureHash = currentEventStructureHash;
                }

                // 更新触发信息
                RefreshTriggerData(eventNames);

                lastUpdateTime = System.DateTime.Now.ToString("HH:mm:ss");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"刷新事件数据失败: {ex.Message}");
                eventListeners.Clear();
                eventTriggers.Clear();
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
                    var listeners = eventSystem.GetEventListeners(eventName);
                    if (listeners != null && listeners.Count > 0)
                    {
                        var enhancedListeners = listeners.Select(l => new EnhancedListenerInfo(l, eventName)).ToList();

                        // 应用搜索过滤器
                        if (!string.IsNullOrEmpty(searchFilter))
                        {
                            bool matchesFilter = eventName.ToLower().Contains(searchFilter.ToLower()) ||
                                               enhancedListeners.Any(l =>
                                                   (l.ClassName?.ToLower().Contains(searchFilter.ToLower()) ?? false) ||
                                                   (l.MethodName?.ToLower().Contains(searchFilter.ToLower()) ?? false));

                            if (!matchesFilter)
                                continue;
                        }

                        eventListeners[eventName] = enhancedListeners;
                        totalListeners += enhancedListeners.Count;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新触发信息数据
        /// </summary>
        private void RefreshTriggerData(string[] eventNames)
        {
            if (eventNames == null) return;

            foreach (string eventName in eventNames)
            {
                var triggerInfo = eventSystem.GetLastTrigger(eventName);
                if (triggerInfo != null)
                {
                    // 如果事件触发列表不存在，创建新的
                    if (!eventTriggers.ContainsKey(eventName))
                    {
                        eventTriggers[eventName] = new List<EnhancedTriggerInfo>();
                    }

                    var enhancedTrigger = new EnhancedTriggerInfo(triggerInfo, eventName);
                    string locationKey = enhancedTrigger.GetLocationKey();

                    // 查找是否已有相同位置的触发信息
                    var existingTrigger = eventTriggers[eventName].FirstOrDefault(t => t.GetLocationKey() == locationKey);

                    if (existingTrigger != null)
                    {
                        // 如果是同一位置，只更新时间和计数
                        if (existingTrigger.TriggerTime < triggerInfo.TriggerTime)
                        {
                            existingTrigger.UpdateTriggerTime(triggerInfo.TriggerTime);
                            // 同时更新上下文信息
                            existingTrigger.TriggerContext = triggerInfo.TriggerContext;
                        }
                    }
                    else
                    {
                        // 新的触发位置，添加到列表
                        eventTriggers[eventName].Add(enhancedTrigger);
                    }

                    // 限制每个事件最多显示10个不同的触发位置
                    if (eventTriggers[eventName].Count > 10)
                    {
                        var oldest = eventTriggers[eventName].OrderBy(t => t.TriggerTime).First();
                        eventTriggers[eventName].Remove(oldest);
                    }
                }
            }

            // 清理不再存在的事件的缓存
            var currentEventNames = new HashSet<string>(eventNames);
            var cachedEventNames = eventTriggers.Keys.ToList();

            foreach (string cachedEventName in cachedEventNames)
            {
                if (!currentEventNames.Contains(cachedEventName))
                {
                    eventTriggers.Remove(cachedEventName);
                }
            }
        }

        /// <summary>
        /// 计算事件结构哈希值
        /// </summary>
        private int GetEventStructureHash(string[] eventNames)
        {
            if (eventNames == null || eventNames.Length == 0)
                return 0;

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
                if (Event.current == null || Event.current.type != EventType.Layout)
                {
                    RefreshEventData();
                    nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                }
                else
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (autoRefresh)
                        {
                            RefreshEventData();
                            nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                        }
                    };
                }
            }

            if (autoRefresh)
            {
                Repaint();
            }
        }

        #endregion
    }
}