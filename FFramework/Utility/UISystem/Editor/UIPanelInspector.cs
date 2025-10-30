using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
        private UIPanel panel;
        private bool showSummary = true;
        private Vector2 scrollPos;
        private GUIStyle headerStyle;
        private GUIStyle badgeStyle;
        private double lastScanTime;
        private float autoRefreshInterval = 2f;
        private bool autoRefresh;
        private string searchFilter = "";
        private bool showCleanupActions;
        private List<Action> trackedCleanupActions = new List<Action>(); // 通过反射获取

        public override void OnInspectorGUI()
        {
            panel = (UIPanel)target;
            if (panel == null)
            {
                EditorGUILayout.HelpBox("面板为空。", MessageType.Error);
                return;
            }

            PrepareStyles();
            DrawHeader();
            DrawToolbar();
            DrawSearchBar();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 只保留统一的统计概览，包含所有信息
            DrawSummarySection();

            EditorGUILayout.EndScrollView();

            DrawFooterOperations();

            if (autoRefresh && EditorApplication.timeSinceStartup - lastScanTime > autoRefreshInterval)
            {
                Repaint();
                lastScanTime = EditorApplication.timeSinceStartup;
            }
        }

        private new void DrawHeader()
        {
            EditorGUILayout.Space(4);
            GUILayout.Label($"UI事件检查器 - {panel.name}", headerStyle);
            EditorGUILayout.Space(2);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新", GUILayout.Height(22)))
            {
                Repaint();
            }
            if (GUILayout.Button("重新初始化 Init()", GUILayout.Height(22)))
            {
                panel.Init();
                EditorUtility.DisplayDialog("操作完成", "已调用 Init()", "确定");
            }
            if (GUILayout.Button("解绑全部事件", GUILayout.Height(22)))
            {
                panel.UnbindAllEvents();
                EditorUtility.DisplayDialog("操作完成", "已解绑全部 UI 事件。", "确定");
            }
            if (GUILayout.Button("导出报告", GUILayout.Height(22)))
            {
                ExportReportToConsole();
            }
            autoRefresh = GUILayout.Toggle(autoRefresh, "自动刷新", GUILayout.Width(80));
            autoRefreshInterval = EditorGUILayout.Slider(autoRefreshInterval, 0.5f, 5f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("筛选:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("清除", GUILayout.Width(50)))
                searchFilter = "";
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummarySection()
        {
            showSummary = EditorGUILayout.Foldout(showSummary, "UI事件检查器 - 完整概览", true);
            if (!showSummary) return;

            var buttons = FetchComponents<Button>();
            var toggles = FetchComponents<Toggle>();
            var sliders = FetchComponents<Slider>();
            var inputs = FetchComponents<InputField>();
            var dropdowns = FetchComponents<Dropdown>();
            var scrolls = FetchComponents<ScrollRect>();
            var triggers = FetchComponents<EventTrigger>();

            int activeBtns = buttons.Count(b => GetListenerCount_Button(b) > 0);
            int activeToggles = toggles.Count(t => GetListenerCount_Toggle(t) > 0);
            int activeSliders = sliders.Count(s => GetListenerCount_Slider(s) > 0);
            int activeInputs = inputs.Count(i => GetListenerCount_InputField(i) > 0);
            int activeDropdowns = dropdowns.Count(d => GetListenerCount_Dropdown(d) > 0);
            int activeScrolls = scrolls.Count(sr => GetListenerCount_ScrollRect(sr) > 0);
            int activeTriggers = triggers.Count(et => et.triggers != null && et.triggers.Count > 0);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 组件统计和详细列表合并显示
            EditorGUILayout.LabelField("📊 UI组件统计与详情", EditorStyles.boldLabel);

            // Buttons
            DrawComponentBadgeAndDetails("Buttons", buttons, activeBtns, Color.cyan, GetListenerCount_Button, DrawButtonDetail);

            // Toggles  
            DrawComponentBadgeAndDetails("Toggles", toggles, activeToggles, Color.green, GetListenerCount_Toggle, DrawToggleDetail);

            // Sliders
            DrawComponentBadgeAndDetails("Sliders", sliders, activeSliders, new Color(0.8f, 0.6f, 0.2f), GetListenerCount_Slider, DrawSliderDetail);

            // InputFields
            DrawComponentBadgeAndDetails("InputFields", inputs, activeInputs, Color.magenta, GetListenerCount_InputField, DrawInputFieldDetail);

            // Dropdowns
            DrawComponentBadgeAndDetails("Dropdowns", dropdowns, activeDropdowns, Color.yellow, GetListenerCount_Dropdown, DrawDropdownDetail);

            // ScrollRects
            DrawComponentBadgeAndDetails("ScrollRects", scrolls, activeScrolls, Color.gray, GetListenerCount_ScrollRect, DrawScrollRectDetail);

            // EventTriggers
            DrawEventTriggersDetails(triggers, activeTriggers);

            EditorGUILayout.Space(8);

            // 总计统计
            int totalComponents = buttons.Count + toggles.Count + sliders.Count + inputs.Count + dropdowns.Count + scrolls.Count + triggers.Count;
            int totalActive = activeBtns + activeToggles + activeSliders + activeInputs + activeDropdowns + activeScrolls + activeTriggers;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📈 总计: {totalComponents} 组件，{totalActive} 个已绑定事件", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // 事件追踪统计部分
            FetchCleanupActions();
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🧹 事件追踪统计", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"追踪清理动作: {trackedCleanupActions.Count}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // 清理动作详情
            if (trackedCleanupActions.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"📝 已追踪 {trackedCleanupActions.Count} 个事件清理动作，面板销毁时将自动清理", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🧹 手动执行所有清理", GUILayout.Height(20)))
                {
                    int executeCount = 0;
                    var actionsToExecute = new List<Action>(trackedCleanupActions); // 复制列表避免修改时的问题

                    foreach (var action in actionsToExecute)
                    {
                        try
                        {
                            action?.Invoke();
                            executeCount++;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"执行清理动作时出错: {e.Message}");
                        }
                    }

                    // 清理执行后，重新获取清理动作列表（应该为空或减少）
                    FetchCleanupActions();

                    EditorUtility.DisplayDialog("清理完成",
                        $"已执行 {executeCount} 个清理动作\n当前剩余追踪动作: {trackedCleanupActions.Count}",
                        "确定");
                    Repaint();
                }

                if (GUILayout.Button("📋 查看详情", GUILayout.Height(20)))
                {
                    showCleanupActions = !showCleanupActions;
                }
                EditorGUILayout.EndHorizontal();

                // 简化的清理动作列表显示（只显示信息，不提供单个执行）
                if (showCleanupActions)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("清理动作详情:", EditorStyles.miniLabel);
                    for (int i = 0; i < Math.Min(trackedCleanupActions.Count, 10); i++) // 最多显示10个，避免界面过长
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  [{i}] Action", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"{trackedCleanupActions[i]?.Method?.Name ?? "Unknown"}", EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }

                    if (trackedCleanupActions.Count > 10)
                    {
                        EditorGUILayout.LabelField($"  ... 还有 {trackedCleanupActions.Count - 10} 个清理动作", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("💡 提示: 这些清理动作会在面板销毁时自动执行", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("⚠️ 没有追踪的事件清理动作", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("建议使用带自动追踪的绑定方法，以避免内存泄漏", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // 新的合并方法：统计徽章 + 详细列表
        private void DrawComponentBadgeAndDetails<T>(string title, List<T> components, int activeCount, Color color,
            Func<T, int> listenerCounter, Action<T> detailDrawer) where T : Component
        {
            var prev = GUI.color;
            GUI.color = color;

            // 统计徽章行
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            bool foldout = EditorGUILayout.Foldout(GetFoldoutState(title), $"{title}: {components.Count} ({activeCount} 活跃)", true);
            SetFoldoutState(title, foldout);
            EditorGUILayout.EndHorizontal();

            GUI.color = prev;

            // 如果展开，显示详细列表
            if (foldout && components.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var comp in components)
                {
                    if (!IsMatchFilter(comp.gameObject.name))
                        continue;

                    int listenerCount = listenerCounter(comp);

                    var prevBgColor = GUI.backgroundColor;
                    GUI.backgroundColor = (listenerCount > 0 ? Color.green : Color.gray) * 0.3f + Color.white * 0.7f;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    string statusIcon = listenerCount > 0 ? "✅" : "⚪";
                    EditorGUILayout.LabelField($"{statusIcon} {comp.gameObject.name}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (listenerCount > 0)
                    {
                        EditorGUILayout.LabelField($"监听: {listenerCount}", GUILayout.Width(80));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("未绑定", GUILayout.Width(80));
                    }

                    if (GUILayout.Button("选中", GUILayout.Width(50)))
                    {
                        Selection.activeObject = comp.gameObject;
                        EditorGUIUtility.PingObject(comp.gameObject);
                    }

                    if (listenerCount > 0 && GUILayout.Button("详情", GUILayout.Width(50)))
                    {
                        detailDrawer?.Invoke(comp);
                    }

                    EditorGUILayout.EndHorizontal();

                    if (listenerCount > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(20);
                        EditorGUILayout.LabelField($"路径: {GetGameObjectPath(comp.gameObject)}", EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = prevBgColor;
                }
                EditorGUILayout.EndVertical();
            }
        }

        // EventTriggers 的特殊处理
        private void DrawEventTriggersDetails(List<EventTrigger> triggers, int activeCount)
        {
            var prev = GUI.color;
            GUI.color = Color.red;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            bool foldout = EditorGUILayout.Foldout(GetFoldoutState("EventTriggers"), $"EventTriggers: {triggers.Count} ({activeCount} 活跃)", true);
            SetFoldoutState("EventTriggers", foldout);
            EditorGUILayout.EndHorizontal();

            GUI.color = prev;

            if (foldout && triggers.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var trigger in triggers)
                {
                    if (!IsMatchFilter(trigger.gameObject.name))
                        continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(trigger.gameObject.name, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("选中", GUILayout.Width(50)))
                    {
                        Selection.activeObject = trigger.gameObject;
                        EditorGUIUtility.PingObject(trigger.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();

                    if (trigger.triggers == null || trigger.triggers.Count == 0)
                    {
                        EditorGUILayout.LabelField("无事件条目。");
                    }
                    else
                    {
                        foreach (var entry in trigger.triggers)
                        {
                            int count = entry.callback.GetPersistentEventCount();
                            EditorGUILayout.LabelField($"- {entry.eventID} (监听数={count})");
                            for (int i = 0; i < count; i++)
                            {
                                UnityEngine.Object targetObj = entry.callback.GetPersistentTarget(i);
                                string method = entry.callback.GetPersistentMethodName(i);
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField($"    [{i}] {targetObj?.name}.{method}()", GUILayout.MinWidth(100));
                                if (GUILayout.Button("Ping", GUILayout.Width(40)))
                                {
                                    if (targetObj != null)
                                    {
                                        Selection.activeObject = targetObj;
                                        EditorGUIUtility.PingObject(targetObj);
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
        }

        // 修改详情绘制方法，使用弹窗显示
        private void DrawButtonDetail(Button btn)
        {
            string content = $"Button详细信息: {btn.name}\n\n";

            // 获取持久化和运行时事件信息
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

        private void DrawFooterOperations()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
            if (GUILayout.Button("重新扫描并刷新"))
            {
                Repaint();
            }
            if (GUILayout.Button("仅打印当前面板事件概览到控制台"))
            {
                ExportReportToConsole(false);
            }
            EditorGUILayout.EndVertical();
        }

        #region Helpers

        private List<T> FetchComponents<T>() where T : Component
        {
            if (panel == null) return new List<T>();
            return panel.GetComponentsInChildren<T>(true).ToList();
        }

        private bool IsMatchFilter(string name)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;
            return name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 检查组件是否被追踪（改进版本）
        private bool IsComponentTracked(GameObject gameObject)
        {
            // 简化检查 - 如果有清理动作且当前游戏运行中，认为可能被追踪
            return trackedCleanupActions.Count > 0 && Application.isPlaying;
        }

        // 更精确的组件追踪检查（可选）
        private bool IsSpecificComponentTracked<T>(T component) where T : Component
        {
            if (component == null) return false;

            // 检查运行时事件数量
            int runtimeCount = 0;

            if (component is Button btn)
                runtimeCount = GetRuntimeListenerCount(btn.onClick);
            else if (component is Toggle toggle)
                runtimeCount = GetRuntimeListenerCount(toggle.onValueChanged);
            else if (component is Slider slider)
                runtimeCount = GetRuntimeListenerCount(slider.onValueChanged);
            else if (component is InputField inputField)
                runtimeCount = GetRuntimeListenerCount(inputField.onValueChanged) + GetRuntimeListenerCount(inputField.onEndEdit);
            else if (component is Dropdown dropdown)
                runtimeCount = GetRuntimeListenerCount(dropdown.onValueChanged);
            else if (component is ScrollRect scrollRect)
                runtimeCount = GetRuntimeListenerCount(scrollRect.onValueChanged);

            // 如果有运行时监听器，说明被追踪了
            return runtimeCount > 0;
        }

        // 修改监听器计数方法，加入运行时事件检测
        private int GetListenerCount_Button(Button b)
        {
            if (b == null) return 0;

            // 持久化事件数量
            int persistentCount = b.onClick.GetPersistentEventCount();

            // 运行时事件数量（通过反射获取）
            int runtimeCount = GetRuntimeListenerCount(b.onClick);

            return persistentCount + runtimeCount;
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

        // 新增：通过反射获取运行时监听器数量
        private int GetRuntimeListenerCount(UnityEventBase unityEvent)
        {
            if (unityEvent == null) return 0;

            try
            {
                // 通过反射访问 UnityEvent 的私有字段
                var field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    var callsObject = field.GetValue(unityEvent);
                    if (callsObject != null)
                    {
                        // 获取运行时调用列表
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

        // 添加获取GameObject路径的辅助方法
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

        // 折叠状态管理
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        private bool GetFoldoutState(string key)
        {
            return foldoutStates.ContainsKey(key) ? foldoutStates[key] : false;
        }

        private void SetFoldoutState(string key, bool value)
        {
            foldoutStates[key] = value;
        }

        private void PrepareStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            if (badgeStyle == null)
            {
                badgeStyle = new GUIStyle(EditorStyles.label);
            }
        }

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
            Debug.Log(sb.ToString());
        }

        private void AppendDetail<T>(System.Text.StringBuilder sb, string label, Func<T, UnityEventBase> getter) where T : Component
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

        private void FetchCleanupActions()
        {
            trackedCleanupActions.Clear();
            if (panel == null) return;
            // 反射访问 private List<Action> eventCleanupActions
            var field = typeof(UIPanel).GetField("eventCleanupActions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var listObj = field.GetValue(panel) as List<Action>;
                if (listObj != null)
                    trackedCleanupActions.AddRange(listObj);
            }
        }

        #endregion
    }
}