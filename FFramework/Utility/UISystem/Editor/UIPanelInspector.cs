using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using FFramework.Utility;

namespace SmallFramework.Editor
{
    /// <summary>
    /// UIPanel 检视器 - UI事件检查器
    /// </summary>
    [CustomEditor(typeof(UIPanel), true)]
    public class UIPanelInspector : UnityEditor.Editor
    {
        #region Private Fields
        private UIPanel panel;
        private bool showSummary = true;
        private bool showCleanupActions;
        private Vector2 scrollPos;
        private string searchFilter = "";
        private List<Action> trackedCleanupActions = new List<Action>();
        #endregion

        #region Unity Methods
        public override void OnInspectorGUI()
        {
            panel = (UIPanel)target;
            if (panel == null)
            {
                EditorGUILayout.HelpBox("面板为空。", MessageType.Error);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawSerializedProperties();
            EditorGUILayout.Space(5);
            DrawSummarySection();

            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Main Drawing Methods
        /// <summary>
        /// 绘制序列化属性区域
        /// </summary>
        private void DrawSerializedProperties()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("序列化字段", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("打开脚本", GUILayout.Width(80), GUILayout.Height(20)))
            {
                OpenScript();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // 绘制属性
            serializedObject.Update();
            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyPath == "m_Script") continue;
                EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制UI事件检查器区域
        /// </summary>
        private void DrawSummarySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 折叠标题按钮
            DrawCollapsibleTitle();

            if (!showSummary)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);

            // 各个功能区域
            DrawToolbarAndSearch();
            EditorGUILayout.Space(5);
            DrawStatistics();
            EditorGUILayout.Space(5);
            DrawComponentsList();
            EditorGUILayout.Space(5);
            DrawCleanupActionsSection();
            EditorGUILayout.Space(5);
            DrawQuickActions();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制折叠标题
        /// </summary>
        private void DrawCollapsibleTitle()
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle transparentButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = null },
                hover = { background = null },
                active = { background = null },
                focused = { background = null },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            if (GUILayout.Button(" UI事件检查器 - 完整概览", transparentButtonStyle,
                GUILayout.Height(24), GUILayout.ExpandWidth(true)))
            {
                showSummary = !showSummary;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制工具栏和搜索区域
        /// </summary>
        private void DrawToolbarAndSearch()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // 操作按钮
            if (GUILayout.Button("刷新", GUILayout.Height(20), GUILayout.Width(60)))
            {
                Repaint();
            }
            if (GUILayout.Button("导出报告", GUILayout.Height(20), GUILayout.Width(80)))
            {
                ExportReportToConsole();
            }

            GUILayout.Space(10);

            // 搜索区域
            GUILayout.Label("筛选:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Height(20), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("清除", GUILayout.Width(50), GUILayout.Height(20)))
                searchFilter = "";

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            var allUIComponents = GetAllUIComponentsWithEvents();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("UI组件统计", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已绑定事件的UI组件:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{allUIComponents.Count}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制组件列表
        /// </summary>
        private void DrawComponentsList()
        {
            var allUIComponents = GetAllUIComponentsWithEvents();

            if (allUIComponents.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("已绑定事件的UI组件列表", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);

                foreach (var uiComponent in allUIComponents)
                {
                    if (!IsMatchFilter(uiComponent.Component.gameObject.name))
                        continue;

                    DrawUIComponentItem(uiComponent);
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("没有发现已绑定事件的UI组件", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制事件追踪统计区域
        /// </summary>
        private void DrawCleanupActionsSection()
        {
            FetchCleanupActions();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("事件追踪统计", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"追踪清理动作: {trackedCleanupActions.Count}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // 清理动作详情
            if (trackedCleanupActions.Count > 0)
            {
                DrawCleanupActionsDetails();
            }
            else
            {
                DrawNoCleanupActionsMessage();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制快速操作区域
        /// </summary>
        private void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新扫描并刷新", GUILayout.Height(24)))
            {
                Repaint();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("仅打印当前面板事件概览到控制台", GUILayout.Height(24)))
            {
                ExportReportToConsole(false);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Helper Drawing Methods
        /// <summary>
        /// 绘制清理动作详情
        /// </summary>
        private void DrawCleanupActionsDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"已追踪 {trackedCleanupActions.Count} 个事件清理动作，面板销毁时将自动清理", EditorStyles.miniLabel);

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("查看详情", GUILayout.Height(20), GUILayout.Width(80)))
            {
                showCleanupActions = !showCleanupActions;
            }
            EditorGUILayout.EndHorizontal();

            if (showCleanupActions)
            {
                DrawCleanupActionsDetailsList();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制清理动作详情列表
        /// </summary>
        private void DrawCleanupActionsDetailsList()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("清理动作详情:", EditorStyles.miniLabel);

            int displayCount = Math.Min(trackedCleanupActions.Count, 10);
            for (int i = 0; i < displayCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{i}]", EditorStyles.miniLabel, GUILayout.Width(40));
                EditorGUILayout.LabelField($"{trackedCleanupActions[i]?.Method?.Name ?? "Unknown"}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            if (trackedCleanupActions.Count > 10)
            {
                EditorGUILayout.LabelField($"  ... 还有 {trackedCleanupActions.Count - 10} 个清理动作", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("提示: 这些清理动作会在面板销毁时自动执行", EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制无清理动作消息
        /// </summary>
        private void DrawNoCleanupActionsMessage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("没有追踪的事件清理动作", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("建议使用带自动追踪的绑定方法，以避免内存泄漏", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制单个UI组件项
        /// </summary>
        private void DrawUIComponentItem(UIComponentInfo uiComponent)
        {
            var prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = uiComponent.TypeColor * 0.25f + Color.white * 0.75f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 主要信息行
            EditorGUILayout.BeginHorizontal();

            // 组件类型标签
            var prevColor = GUI.color;
            GUI.color = uiComponent.TypeColor;
            EditorGUILayout.LabelField($"[{uiComponent.ComponentType}]", GUILayout.Width(85));
            GUI.color = prevColor;

            // 组件名称
            EditorGUILayout.LabelField($"{uiComponent.Component.gameObject.name}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 监听器数量
            EditorGUILayout.LabelField($"监听: {uiComponent.ListenerCount}", GUILayout.Width(80));

            // 操作按钮
            if (GUILayout.Button("选中", GUILayout.Width(50), GUILayout.Height(20)))
            {
                Selection.activeObject = uiComponent.Component.gameObject;
                EditorGUIUtility.PingObject(uiComponent.Component.gameObject);
            }

            if (GUILayout.Button("详情", GUILayout.Width(50), GUILayout.Height(20)))
            {
                ShowComponentDetail(uiComponent);
            }

            EditorGUILayout.EndHorizontal();

            // 路径信息行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField($"路径: {GetGameObjectPath(uiComponent.Component.gameObject)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = prevBgColor;
        }
        #endregion

        #region Component Detail Methods
        /// <summary>
        /// 显示组件详情
        /// </summary>
        private void ShowComponentDetail(UIComponentInfo uiComponent)
        {
            switch (uiComponent.ComponentType)
            {
                case "Button":
                    DrawButtonDetail(uiComponent.Component as Button);
                    break;
                case "Toggle":
                    DrawToggleDetail(uiComponent.Component as Toggle);
                    break;
                case "Slider":
                    DrawSliderDetail(uiComponent.Component as Slider);
                    break;
                case "InputField":
                    DrawInputFieldDetail(uiComponent.Component as InputField);
                    break;
                case "Dropdown":
                    DrawDropdownDetail(uiComponent.Component as Dropdown);
                    break;
                case "ScrollRect":
                    DrawScrollRectDetail(uiComponent.Component as ScrollRect);
                    break;
                case "EventTrigger":
                    DrawEventTriggerDetail(uiComponent.Component as EventTrigger);
                    break;
            }
        }

        private void DrawButtonDetail(Button btn)
        {
            string content = $"Button详细信息: {btn.name}\n\n";

            int persistentCount = btn.onClick.GetPersistentEventCount();
            int runtimeCount = GetRuntimeListenerCount(btn.onClick);

            content += $"持久化监听器: {persistentCount}\n";
            for (int i = 0; i < persistentCount; i++)
            {
                UnityEngine.Object targetObj = btn.onClick.GetPersistentTarget(i);
                string method = btn.onClick.GetPersistentMethodName(i);
                content += $"  [P{i}] {targetObj?.name}.{method}()\n";
            }

            content += $"\n运行时监听器: {runtimeCount}\n";
            if (runtimeCount > 0)
            {
                content += "  (运行时绑定的事件，详细信息需要通过调试器查看)\n";
            }

            if (persistentCount == 0 && runtimeCount == 0)
            {
                content += "无事件监听器";
            }

            EditorUtility.DisplayDialog("Button 详情", content, "确定");
        }

        private void DrawToggleDetail(Toggle t)
        {
            string content = $"Toggle详细信息: {t.name}\n";
            content += $"当前状态: {(t.isOn ? "开启" : "关闭")}\n\n";

            int persistentCount = t.onValueChanged.GetPersistentEventCount();
            int runtimeCount = GetRuntimeListenerCount(t.onValueChanged);

            content += $"onValueChanged事件:\n";
            content += $"  持久化监听器: {persistentCount}\n";
            content += $"  运行时监听器: {runtimeCount}\n";

            EditorUtility.DisplayDialog("Toggle 详情", content, "确定");
        }

        private void DrawSliderDetail(Slider s)
        {
            string content = $"Slider详细信息: {s.name}\n";
            content += $"当前值: {s.value:F2} (范围: {s.minValue:F2} - {s.maxValue:F2})\n\n";

            int persistentCount = s.onValueChanged.GetPersistentEventCount();
            int runtimeCount = GetRuntimeListenerCount(s.onValueChanged);

            content += $"onValueChanged事件:\n";
            content += $"  持久化监听器: {persistentCount}\n";
            content += $"  运行时监听器: {runtimeCount}\n";

            EditorUtility.DisplayDialog("Slider 详情", content, "确定");
        }

        private void DrawInputFieldDetail(InputField i)
        {
            string content = $"InputField详细信息: {i.name}\n";
            content += $"当前文本: \"{i.text}\"\n\n";

            int valueChangedPersistent = i.onValueChanged.GetPersistentEventCount();
            int valueChangedRuntime = GetRuntimeListenerCount(i.onValueChanged);
            int endEditPersistent = i.onEndEdit.GetPersistentEventCount();
            int endEditRuntime = GetRuntimeListenerCount(i.onEndEdit);

            content += $"onValueChanged事件:\n";
            content += $"  持久化监听器: {valueChangedPersistent}\n";
            content += $"  运行时监听器: {valueChangedRuntime}\n\n";

            content += $"onEndEdit事件:\n";
            content += $"  持久化监听器: {endEditPersistent}\n";
            content += $"  运行时监听器: {endEditRuntime}\n";

            EditorUtility.DisplayDialog("InputField 详情", content, "确定");
        }

        private void DrawDropdownDetail(Dropdown d)
        {
            string content = $"Dropdown详细信息: {d.name}\n";
            content += $"当前值: {d.value}\n";
            if (d.options != null && d.value >= 0 && d.value < d.options.Count)
            {
                content += $"当前选项: \"{d.options[d.value].text}\"\n";
            }
            content += $"选项总数: {d.options?.Count ?? 0}\n\n";

            int persistentCount = d.onValueChanged.GetPersistentEventCount();
            int runtimeCount = GetRuntimeListenerCount(d.onValueChanged);

            content += $"onValueChanged事件:\n";
            content += $"  持久化监听器: {persistentCount}\n";
            content += $"  运行时监听器: {runtimeCount}\n";

            EditorUtility.DisplayDialog("Dropdown 详情", content, "确定");
        }

        private void DrawScrollRectDetail(ScrollRect sr)
        {
            string content = $"ScrollRect详细信息: {sr.name}\n";
            content += $"当前位置: {sr.normalizedPosition}\n";
            content += $"水平滚动: {(sr.horizontal ? "启用" : "禁用")}\n";
            content += $"垂直滚动: {(sr.vertical ? "启用" : "禁用")}\n\n";

            int persistentCount = sr.onValueChanged.GetPersistentEventCount();
            int runtimeCount = GetRuntimeListenerCount(sr.onValueChanged);

            content += $"onValueChanged事件:\n";
            content += $"  持久化监听器: {persistentCount}\n";
            content += $"  运行时监听器: {runtimeCount}\n";

            EditorUtility.DisplayDialog("ScrollRect 详情", content, "确定");
        }

        private void DrawEventTriggerDetail(EventTrigger trigger)
        {
            string content = $"EventTrigger详细信息: {trigger.name}\n\n";

            if (trigger.triggers == null || trigger.triggers.Count == 0)
            {
                content += "无事件条目。";
            }
            else
            {
                content += $"事件条目数量: {trigger.triggers.Count}\n\n";

                foreach (var entry in trigger.triggers)
                {
                    int count = entry.callback.GetPersistentEventCount();
                    content += $"事件类型: {entry.eventID}\n";
                    content += $"监听器数量: {count}\n";

                    for (int i = 0; i < count; i++)
                    {
                        UnityEngine.Object targetObj = entry.callback.GetPersistentTarget(i);
                        string method = entry.callback.GetPersistentMethodName(i);
                        content += $"  [{i}] {targetObj?.name}.{method}()\n";
                    }
                    content += "\n";
                }
            }

            EditorUtility.DisplayDialog("EventTrigger 详情", content, "确定");
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// UI组件信息数据结构
        /// </summary>
        private class UIComponentInfo
        {
            public Component Component;
            public string ComponentType;
            public int ListenerCount;
            public Color TypeColor;
        }
        #endregion

        #region Data Collection Methods
        /// <summary>
        /// 获取所有有事件绑定的UI组件
        /// </summary>
        private List<UIComponentInfo> GetAllUIComponentsWithEvents()
        {
            var result = new List<UIComponentInfo>();

            // Buttons
            AddComponentsWithEvents<Button>(result, "Button", Color.cyan, GetListenerCount_Button);
            // Toggles
            AddComponentsWithEvents<Toggle>(result, "Toggle", Color.green, GetListenerCount_Toggle);
            // Sliders
            AddComponentsWithEvents<Slider>(result, "Slider", new Color(0.8f, 0.6f, 0.2f), GetListenerCount_Slider);
            // InputFields
            AddComponentsWithEvents<InputField>(result, "InputField", Color.magenta, GetListenerCount_InputField);
            // Dropdowns
            AddComponentsWithEvents<Dropdown>(result, "Dropdown", Color.yellow, GetListenerCount_Dropdown);
            // ScrollRects
            AddComponentsWithEvents<ScrollRect>(result, "ScrollRect", Color.gray, GetListenerCount_ScrollRect);
            // EventTriggers
            AddEventTriggersWithEvents(result);

            return result;
        }

        /// <summary>
        /// 添加有事件的组件到结果列表
        /// </summary>
        private void AddComponentsWithEvents<T>(List<UIComponentInfo> result, string typeName, Color typeColor,
            Func<T, int> getListenerCount) where T : Component
        {
            var components = FetchComponents<T>();
            foreach (var component in components)
            {
                int count = getListenerCount(component);
                if (count > 0)
                {
                    result.Add(new UIComponentInfo
                    {
                        Component = component,
                        ComponentType = typeName,
                        ListenerCount = count,
                        TypeColor = typeColor
                    });
                }
            }
        }

        /// <summary>
        /// 添加有事件的EventTrigger组件
        /// </summary>
        private void AddEventTriggersWithEvents(List<UIComponentInfo> result)
        {
            var eventTriggers = FetchComponents<EventTrigger>();
            foreach (var trigger in eventTriggers)
            {
                int count = (trigger.triggers != null && trigger.triggers.Count > 0) ? trigger.triggers.Count : 0;
                if (count > 0)
                {
                    result.Add(new UIComponentInfo
                    {
                        Component = trigger,
                        ComponentType = "EventTrigger",
                        ListenerCount = count,
                        TypeColor = Color.red
                    });
                }
            }
        }

        /// <summary>
        /// 获取指定类型的组件列表
        /// </summary>
        private List<T> FetchComponents<T>() where T : Component
        {
            if (panel == null) return new List<T>();
            return panel.GetComponentsInChildren<T>(true).ToList();
        }

        /// <summary>
        /// 获取清理动作列表
        /// </summary>
        private void FetchCleanupActions()
        {
            trackedCleanupActions.Clear();
            if (panel == null) return;

            var field = typeof(UIPanel).GetField("eventCleanupActions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var listObj = field.GetValue(panel) as List<Action>;
                if (listObj != null)
                    trackedCleanupActions.AddRange(listObj);
            }
        }
        #endregion

        #region Listener Count Methods
        private int GetListenerCount_Button(Button b)
        {
            if (b == null) return 0;
            return b.onClick.GetPersistentEventCount() + GetRuntimeListenerCount(b.onClick);
        }

        private int GetListenerCount_Toggle(Toggle t)
        {
            if (t == null) return 0;
            return t.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(t.onValueChanged);
        }

        private int GetListenerCount_Slider(Slider s)
        {
            if (s == null) return 0;
            return s.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(s.onValueChanged);
        }

        private int GetListenerCount_InputField(InputField i)
        {
            if (i == null) return 0;
            return i.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(i.onValueChanged) +
                   i.onEndEdit.GetPersistentEventCount() + GetRuntimeListenerCount(i.onEndEdit);
        }

        private int GetListenerCount_Dropdown(Dropdown d)
        {
            if (d == null) return 0;
            return d.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(d.onValueChanged);
        }

        private int GetListenerCount_ScrollRect(ScrollRect sr)
        {
            if (sr == null) return 0;
            return sr.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(sr.onValueChanged);
        }

        /// <summary>
        /// 通过反射获取运行时监听器数量
        /// </summary>
        private int GetRuntimeListenerCount(UnityEventBase unityEvent)
        {
            if (unityEvent == null) return 0;

            try
            {
                var field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    var callsObject = field.GetValue(unityEvent);
                    if (callsObject != null)
                    {
                        var runtimeCallsField = callsObject.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (runtimeCallsField != null)
                        {
                            var runtimeCalls = runtimeCallsField.GetValue(callsObject) as System.Collections.IList;
                            return runtimeCalls?.Count ?? 0;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取运行时监听器数量失败: {e.Message}");
            }

            return 0;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 打开脚本文件
        /// </summary>
        private void OpenScript()
        {
            if (panel == null) return;

            MonoScript script = MonoScript.FromMonoBehaviour(panel);
            if (script != null)
            {
                AssetDatabase.OpenAsset(script);
            }
            else
            {
                Debug.LogWarning("无法找到脚本文件");
            }
        }

        /// <summary>
        /// 检查名称是否匹配搜索过滤器
        /// </summary>
        private bool IsMatchFilter(string name)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;
            return name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 获取GameObject的层级路径
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null && parent != panel.transform)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// 导出报告到控制台
        /// </summary>
        private void ExportReportToConsole(bool detailed = true)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== UIPanel事件报告: {panel.name} ===");
            sb.AppendLine($"Buttons: {FetchComponents<Button>().Count}");
            sb.AppendLine($"Toggles: {FetchComponents<Toggle>().Count}");
            sb.AppendLine($"Sliders: {FetchComponents<Slider>().Count}");
            sb.AppendLine($"InputFields: {FetchComponents<InputField>().Count}");
            sb.AppendLine($"Dropdowns: {FetchComponents<Dropdown>().Count}");
            sb.AppendLine($"ScrollRects: {FetchComponents<ScrollRect>().Count}");
            sb.AppendLine($"EventTriggers: {FetchComponents<EventTrigger>().Count}");

            if (detailed)
            {
                sb.AppendLine("--- 详细持久化监听 ---");
                AppendComponentDetails(sb);
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 添加组件详细信息到报告
        /// </summary>
        private void AppendComponentDetails(System.Text.StringBuilder sb)
        {
            AppendDetail<Button>(sb, "Button", b => b.onClick);
            AppendDetail<Toggle>(sb, "Toggle", t => t.onValueChanged);
            AppendDetail<Slider>(sb, "Slider", s => s.onValueChanged);
            AppendDetail<InputField>(sb, "InputField(onValueChanged)", i => i.onValueChanged);
            AppendDetail<InputField>(sb, "InputField(onEndEdit)", i => i.onEndEdit);
            AppendDetail<Dropdown>(sb, "Dropdown", d => d.onValueChanged);
            AppendDetail<ScrollRect>(sb, "ScrollRect", sr => sr.onValueChanged);

            var triggers = FetchComponents<EventTrigger>();
            foreach (var tr in triggers)
            {
                sb.AppendLine($"EventTrigger: {tr.name} entries={(tr.triggers?.Count ?? 0)}");
                if (tr.triggers != null)
                {
                    foreach (var entry in tr.triggers)
                    {
                        int c = entry.callback.GetPersistentEventCount();
                        sb.AppendLine($"  - {entry.eventID} listeners={c}");
                        for (int i = 0; i < c; i++)
                        {
                            var targetObj = entry.callback.GetPersistentTarget(i);
                            var method = entry.callback.GetPersistentMethodName(i);
                            sb.AppendLine($"      [{i}] {targetObj?.name}.{method}()");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加组件详细信息
        /// </summary>
        private void AppendDetail<T>(System.Text.StringBuilder sb, string label, Func<T, UnityEventBase> getter)
            where T : Component
        {
            var comps = FetchComponents<T>();
            foreach (var c in comps)
            {
                var evt = getter(c);
                int count = evt.GetPersistentEventCount();
                sb.AppendLine($"{label}: {c.name} listeners={count}");
                for (int i = 0; i < count; i++)
                {
                    var targetObj = evt.GetPersistentTarget(i);
                    var method = evt.GetPersistentMethodName(i);
                    sb.AppendLine($"   [{i}] {targetObj?.name}.{method}()");
                }
            }
        }
        #endregion
    }
}