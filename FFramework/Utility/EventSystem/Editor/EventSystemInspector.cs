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
            public EventSystem.RegistrationInfo RegistrationInfo;

            public EnhancedListenerInfo(EventSystem.ListenerInfo listenerInfo, string eventName)
            {
                EventName = eventName;
                Target = listenerInfo.Target;
                Callback = listenerInfo.Callback;
                RegistrationInfo = listenerInfo.RegistrationInfo;

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
                // 美化回调方法名显示
                string friendlyMethodName = GetFriendlyMethodName(MethodName);
                string friendlyClassName = GetFriendlyClassName(ClassName);

                return $"事件: {EventName}\n" +
                       $"注册位置: {RegistrationInfo.GetLocationInfo()}\n" +
                       $"注册时间: {RegistrationInfo.RegistrationTime:HH:mm:ss.fff}\n" +
                       $"文件: {System.IO.Path.GetFileName(RegistrationInfo.FileName ?? "")}\n" +
                       $"行号: {(RegistrationInfo.LineNumber > 0 ? RegistrationInfo.LineNumber.ToString() : "未知")}\n\n" +
                       $"回调信息:\n" +
                       $"类: {friendlyClassName}\n" +
                       $"方法: {friendlyMethodName}\n" +
                       $"程序集: {AssemblyName}";
            }

            public string GetShortDisplayText()
            {
                // 优先显示注册位置信息，并美化方法名
                string displayMethod = GetFriendlyDisplayMethod();
                string displayClass = GetShortClassName(RegistrationInfo.CallerClass ?? ClassName ?? "Unknown");

                return $"{displayClass}.{TruncateString(displayMethod, 15)}";
            }

            /// <summary>
            /// 获取友好的显示方法名
            /// </summary>
            private string GetFriendlyDisplayMethod()
            {
                // 优先使用注册位置的方法名
                if (!string.IsNullOrEmpty(RegistrationInfo.CallerMethod))
                {
                    return RegistrationInfo.CallerMethod;
                }

                // 回退到回调方法名，并进行美化
                return GetFriendlyMethodName(MethodName ?? "Unknown");
            }

            /// <summary>
            /// 美化方法名显示
            /// </summary>
            private string GetFriendlyMethodName(string methodName)
            {
                if (string.IsNullOrEmpty(methodName))
                    return "Unknown";

                // 处理Lambda表达式方法名
                if (methodName.StartsWith("<") && methodName.Contains(">b__"))
                {
                    // 提取原始方法名
                    int start = methodName.IndexOf('<') + 1;
                    int end = methodName.IndexOf('>');
                    if (end > start)
                    {
                        string originalMethod = methodName.Substring(start, end - start);
                        return $"{originalMethod}(λ)"; // 使用希腊字母λ表示Lambda
                    }
                    return "Lambda表达式";
                }

                // 处理其他编译器生成的方法
                if (methodName.StartsWith("<") && methodName.Contains(">"))
                {
                    int start = methodName.IndexOf('<') + 1;
                    int end = methodName.IndexOf('>');
                    if (end > start)
                    {
                        string originalMethod = methodName.Substring(start, end - start);
                        return $"{originalMethod}(编译器生成)";
                    }
                    return "编译器生成方法";
                }

                return methodName;
            }

            /// <summary>
            /// 美化类名显示
            /// </summary>
            private string GetFriendlyClassName(string className)
            {
                if (string.IsNullOrEmpty(className))
                    return "Unknown";

                // 处理编译器生成的Lambda类
                if (className.StartsWith("<>c"))
                {
                    return "Lambda表达式类";
                }

                // 处理显示类（DisplayClass）
                if (className.Contains("DisplayClass"))
                {
                    return "闭包类";
                }

                return className;
            }

            private string GetShortClassName(string fullClassName)
            {
                if (string.IsNullOrEmpty(fullClassName))
                    return "Unknown";

                string[] parts = fullClassName.Split('.');
                string shortName = parts[parts.Length - 1];

                return TruncateString(shortName, 12);
            }

            private string TruncateString(string text, int maxLength)
            {
                if (string.IsNullOrEmpty(text))
                    return "";

                if (text.Length <= maxLength)
                    return text;

                return text.Substring(0, maxLength - 1) + "..";
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

                // 美化触发方法名显示
                string friendlyMethodName = GetFriendlyTriggerMethodName(CallerMethod);

                return $"事件: {EventName}\n" +
                       $"类: {CallerClass}\n" +
                       $"方法: {friendlyMethodName}\n" +
                       $"触发次数: {TriggerCount}\n" +
                       $"最后触发: {TriggerTime:HH:mm:ss.fff}\n" +
                       $"上下文: {contextInfo}";
            }

            /// <summary>
            /// 美化触发方法名显示
            /// </summary>
            private string GetFriendlyTriggerMethodName(string methodName)
            {
                if (string.IsNullOrEmpty(methodName))
                    return "Unknown";

                // 处理Lambda表达式方法名
                if (methodName.StartsWith("<") && methodName.Contains(">"))
                {
                    int start = methodName.IndexOf('<') + 1;
                    int end = methodName.IndexOf('>');
                    if (end > start)
                    {
                        string originalMethod = methodName.Substring(start, end - start);
                        if (methodName.Contains("b__"))
                        {
                            return $"{originalMethod}(λ)";
                        }
                        return $"{originalMethod}(编译器生成)";
                    }
                }

                return methodName;
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

                // 主要信息按钮 - 使用注册位置信息
                string displayText = info.GetShortDisplayText();

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

                    // 美化方法名显示
                    string friendlyMethodName = GetFriendlyTriggerMethodName(triggerInfo.CallerMethod);
                    string displayText = $"{shortClassName}.{TruncateString(friendlyMethodName, 12)}";

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
        /// 美化触发方法名显示 (Helper方法)
        /// </summary>
        private string GetFriendlyTriggerMethodName(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return "Unknown";

            // 处理Lambda表达式方法名
            if (methodName.StartsWith("<") && methodName.Contains(">"))
            {
                int start = methodName.IndexOf('<') + 1;
                int end = methodName.IndexOf('>');
                if (end > start)
                {
                    string originalMethod = methodName.Substring(start, end - start);
                    if (methodName.Contains("b__"))
                    {
                        return $"{originalMethod}(λ)";
                    }
                    return $"{originalMethod}(编译器生成)";
                }
            }

            return methodName;
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

        /// <summary>
        /// 获取上下文信息的简短显示文本
        /// </summary>
        private string GetContextDisplayText(object context)
        {
            if (context == null)
                return "";

            return context switch
            {
                MonoBehaviour mono when mono != null =>
                    $"[{TruncateString(mono.gameObject.name, 8)}]",
                MonoBehaviour =>
                    "[已销毁]",
                GameObject go when go != null =>
                    $"[{TruncateString(go.name, 8)}]",
                GameObject =>
                    "[已销毁GO]",
                string str when !string.IsNullOrEmpty(str) =>
                    $"[\"{TruncateString(str, 8)}\"]",
                string =>
                    "[空字符串]",
                _ =>
                    $"[{TruncateString(context.GetType().Name, 8)}]"
            };
        }

        /// <summary>
        /// 将 object key 转换为字符串（用于显示）
        /// </summary>
        private string ConvertKeyToString(object key)
        {
            if (key == null) return "null";

            // 如果是字符串，直接返回
            if (key is string str) return str;

            // 如果是值类型，使用 ToString()
            return key.ToString();
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
                // 获取所有事件Key（现在返回 object[]）
                object[] eventKeys = eventSystem.GetAllEventKeys();

                // 将 object[] 转换为 string[]（用于兼容原有逻辑）
                string[] eventNames = eventKeys.Select(k => ConvertKeyToString(k)).ToArray();

                int currentEventStructureHash = GetEventStructureHash(eventKeys);

                bool structureChanged = forceRefresh || currentEventStructureHash != lastEventStructureHash;

                if (structureChanged)
                {
                    RefreshListenerData(eventKeys);
                    lastEventStructureHash = currentEventStructureHash;
                }

                // 更新触发信息
                RefreshTriggerData(eventKeys);

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
        private void RefreshListenerData(object[] eventKeys)
        {
            eventListeners.Clear();
            totalListeners = 0;

            if (eventKeys != null && eventKeys.Length > 0)
            {
                foreach (object eventKey in eventKeys)
                {
                    // 将 key 转换为字符串用于显示
                    string eventName = ConvertKeyToString(eventKey);

                    // 获取监听者列表（需要根据key类型调用不同方法）
                    List<EventSystem.ListenerInfo> listeners = null;

                    if (eventKey is string strKey)
                    {
                        listeners = eventSystem.GetEventListeners(strKey);
                    }
                    else
                    {
                        // 对于结构体key，使用反射调用泛型方法
                        var method = typeof(EventSystem).GetMethod("GetEventListeners",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var genericMethod = method.MakeGenericMethod(eventKey.GetType());
                        listeners = genericMethod.Invoke(eventSystem, new[] { eventKey }) as List<EventSystem.ListenerInfo>;
                    }

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
        private void RefreshTriggerData(object[] eventKeys)
        {
            if (eventKeys == null) return;

            foreach (object eventKey in eventKeys)
            {
                string eventName = ConvertKeyToString(eventKey);

                // 获取最后触发信息
                EventSystem.TriggerInfo triggerInfo = null;

                if (eventKey is string strKey)
                {
                    triggerInfo = eventSystem.GetLastTrigger(strKey);
                }
                else
                {
                    // 对于结构体key，使用反射调用泛型方法
                    var method = typeof(EventSystem).GetMethod("GetLastTrigger",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var genericMethod = method.MakeGenericMethod(eventKey.GetType());
                    triggerInfo = genericMethod.Invoke(eventSystem, new[] { eventKey }) as EventSystem.TriggerInfo;
                }

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
            var currentEventNames = new HashSet<string>(eventKeys.Select(k => ConvertKeyToString(k)));
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
        private int GetEventStructureHash(object[] eventKeys)
        {
            if (eventKeys == null || eventKeys.Length == 0)
                return 0;

            int hash = 17;
            foreach (object eventKey in eventKeys)
            {
                string eventName = ConvertKeyToString(eventKey);
                hash = hash * 31 + eventName.GetHashCode();

                // 获取监听者数量
                int listenerCount = 0;
                if (eventKey is string strKey)
                {
                    listenerCount = eventSystem.GetListenerCount(strKey);
                }
                else
                {
                    // 对于结构体key使用反射
                    var method = typeof(EventSystem).GetMethod("GetListenerCount",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var genericMethod = method.MakeGenericMethod(eventKey.GetType());
                    listenerCount = (int)genericMethod.Invoke(eventSystem, new[] { eventKey });
                }

                hash = hash * 31 + listenerCount;
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