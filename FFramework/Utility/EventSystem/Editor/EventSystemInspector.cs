using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FFramework.Editor
{
    [CustomEditor(typeof(EventSystem))]
    public class EventSystemInspector : UnityEditor.Editor
    {
        private EventSystem eventSystem;
        private VisualElement root;
        private VisualElement listContainer;
        private TextField searchField;
        private Toggle autoRefreshToggle;
        private float refreshInterval = 1.0f;
        private double nextRefreshTime;
        private Button refreshBtn;
        private Label summaryLabel;

        // 保存折叠状态
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        // 自动刷新调度项
        private IVisualElementScheduledItem autoRefreshItem;

        public override VisualElement CreateInspectorGUI()
        {
            eventSystem = (EventSystem)target;
            root = new VisualElement();
            // 加载并应用 USS 样式
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/FFramework-main/FFramework/Utility/EventSystem/Editor/EventSystemInspector.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            root.AddToClassList("Root");

            var header = new Label("-----EventSystem 事件监视器-----");
            header.AddToClassList("EventSystemInspectorTitle");
            root.Add(header);

            // 搜索框区域
            var searchBar = new VisualElement();
            searchBar.AddToClassList("SearchBar");
            // 搜索框搜索区域
            searchField = new TextField();
            searchField.AddToClassList("SearchBarTextField");
            searchField.RegisterValueChangedCallback(_ => RebuildEventList());
            var textInput = searchField.Q<TextElement>();
            textInput.style.paddingLeft = 20; // 为图标腾出空间
            searchBar.Add(searchField);
            // Icon
            Label icon = new Label("S");
            icon.AddToClassList("SearchBarIcon");
            searchField.Add(icon);

            root.Add(searchBar);

            // 控制按钮区域
            var controlBar = new VisualElement();
            controlBar.style.flexDirection = FlexDirection.Row;
            controlBar.style.alignItems = Align.Center;
            controlBar.style.marginBottom = 4;

            refreshBtn = new Button(() => RebuildEventList()) { text = "刷新" };
            controlBar.Add(refreshBtn);

            var expandAllBtn = new Button(() => SetAllFoldouts(true)) { text = "展开" };
            controlBar.Add(expandAllBtn);

            var collapseAllBtn = new Button(() => SetAllFoldouts(false)) { text = "折叠" };
            controlBar.Add(collapseAllBtn);

            autoRefreshToggle = new Toggle("自动刷新(1s)") { value = false };
            autoRefreshToggle.style.marginLeft = 8;
            autoRefreshToggle.RegisterValueChangedCallback(_ => UpdateAutoRefresh());
            controlBar.Add(autoRefreshToggle);

            root.Add(controlBar);

            root.Add(new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.25f,0.25f,0.25f),
                    marginBottom = 4
                }
            });

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            listContainer = new VisualElement();
            scrollView.Add(listContainer);
            root.Add(scrollView);

            // 固定的底部区域
            var bottomContainer = new VisualElement();
            bottomContainer.style.paddingTop = 4;
            bottomContainer.style.borderTopWidth = 1;
            bottomContainer.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f);
            bottomContainer.style.marginTop = 4;

            // 汇总信息
            summaryLabel = new Label("等待加载...");
            summaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            summaryLabel.style.marginBottom = 4;
            bottomContainer.Add(summaryLabel);

            // 底部刷新按钮
            var bottomBar = new VisualElement();
            bottomBar.style.flexDirection = FlexDirection.Row;
            bottomBar.style.justifyContent = Justify.FlexEnd;

            bottomContainer.Add(bottomBar);
            root.Add(bottomContainer);

            RebuildEventList();
            UpdateAutoRefresh();

            return root;
        }

        private void UpdateAutoRefresh()
        {
            // 停止旧调度
            autoRefreshItem?.Pause();
            autoRefreshItem = null;

            if (autoRefreshToggle.value)
            {
                nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                autoRefreshItem = root.schedule.Execute(() =>
                {
                    if (!autoRefreshToggle.value) return;
                    if (EditorApplication.timeSinceStartup >= nextRefreshTime)
                    {
                        nextRefreshTime = EditorApplication.timeSinceStartup + refreshInterval;
                        RebuildEventList();
                    }
                }).Every(250);
            }
        }

        private void CaptureFoldoutStates()
        {
            foldoutStates.Clear();
            foreach (var child in listContainer.Children())
            {
                if (child is Foldout f && f.userData is string evtName)
                {
                    foldoutStates[evtName] = f.value;
                }
            }
        }

        private void RebuildEventList()
        {
            if (eventSystem == null)
            {
                listContainer.Clear();
                listContainer.Add(new Label("未找到 EventSystem 实例"));
                summaryLabel.text = "未找到 EventSystem 实例";
                return;
            }

            // 捕获旧状态
            CaptureFoldoutStates();

            string filter = searchField.value?.Trim();
            listContainer.Clear();

            string[] eventNames;
            try
            {
                eventNames = eventSystem.GetAllEventNames();
            }
            catch
            {
                listContainer.Add(new Label("获取事件失败（可能在 Play 模式切换过程中）"));
                summaryLabel.text = "获取事件失败";
                return;
            }

            if (eventNames == null || eventNames.Length == 0)
            {
                listContainer.Add(new Label("当前没有已注册且含监听者的事件"));
                summaryLabel.text = $"当前没有事件   上次更新: {System.DateTime.Now:HH:mm:ss}";
                return;
            }

            var ordered = eventNames.OrderBy(n => n);
            int totalListeners = 0;

            foreach (var evt in ordered)
            {
                var listeners = eventSystem.GetEventListeners(evt);
                if (listeners == null || listeners.Count == 0)
                    continue;

                if (!string.IsNullOrEmpty(filter) &&
                    !evt.ToLower().Contains(filter.ToLower()) &&
                    !listeners.Any(l =>
                        (l.ClassName?.ToLower().Contains(filter.ToLower()) ?? false) ||
                        (l.MethodName?.ToLower().Contains(filter.ToLower()) ?? false) ||
                        (l.AssemblyName?.ToLower().Contains(filter.ToLower()) ?? false)))
                {
                    continue;
                }

                totalListeners += listeners.Count;

                var foldout = new Foldout
                {
                    text = $"{evt}  (监听:{listeners.Count})",
                    value = foldoutStates.TryGetValue(evt, out var expanded) && expanded
                };
                foldout.userData = evt;
                foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
                foldout.style.marginBottom = 2;

                foreach (var info in listeners)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.style.justifyContent = Justify.SpaceBetween; // 添加这行，让内容两端对齐
                    row.style.paddingLeft = 4;
                    row.style.paddingRight = 4;
                    row.style.marginBottom = 1;
                    row.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);

                    string goPart = "";
                    if (info.Target is MonoBehaviour mb && mb != null)
                        goPart = $" [GO:{mb.gameObject.name}]";
                    else if (info.Target == null)
                        goPart = " [静态]";

                    var label = new Label($"{info.AssemblyName} - {info.ClassName} - {info.MethodName}{goPart}");
                    label.style.flexGrow = 1;
                    label.style.overflow = Overflow.Hidden;
                    label.style.textOverflow = TextOverflow.Ellipsis;
                    label.style.whiteSpace = WhiteSpace.NoWrap;
                    label.tooltip = info.GetDetailedInfo();
                    row.Add(label);

                    // 按钮容器，确保按钮靠右对齐
                    var buttonContainer = new VisualElement();
                    buttonContainer.style.flexDirection = FlexDirection.Row;
                    buttonContainer.style.flexShrink = 0; // 防止按钮被压缩

                    if (info.Target is MonoBehaviour mono && mono != null)
                    {
                        var pingBtn = new Button(() =>
                        {
                            EditorGUIUtility.PingObject(mono.gameObject);
                            Selection.activeObject = mono.gameObject;
                        })
                        { text = "定位" };
                        pingBtn.style.marginLeft = 4;
                        buttonContainer.Add(pingBtn);
                    }

                    // 通用移除按钮（支持所有类型事件）
                    var removeBtn = new Button(() =>
                    {
                        if (EditorUtility.DisplayDialog("移除监听",
                            $"确定移除 {info.ClassName}.{info.MethodName} 对事件 {evt} 的监听？", "确定", "取消"))
                        {
                            // 尝试通过反射移除监听
                            try
                            {
                                if (info.Callback is System.Action actionCb)
                                {
                                    eventSystem.UnregisterEvent(evt, actionCb);
                                }
                                else
                                {
                                    // 对于泛型事件，使用反射调用
                                    var callbackType = info.Callback.GetType();
                                    if (callbackType.IsGenericType && callbackType.GetGenericTypeDefinition() == typeof(System.Action<>))
                                    {
                                        var paramType = callbackType.GetGenericArguments()[0];
                                        var unregisterMethod = eventSystem.GetType().GetMethod("UnregisterEvent", new[] { typeof(string), callbackType });
                                        if (unregisterMethod != null)
                                        {
                                            unregisterMethod.Invoke(eventSystem, new object[] { evt, info.Callback });
                                        }
                                    }
                                }
                                RebuildEventList();
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"移除事件监听失败: {ex.Message}");
                            }
                        }
                    })
                    { text = "移除" };
                    removeBtn.style.marginLeft = 4;
                    buttonContainer.Add(removeBtn);

                    row.Add(buttonContainer);
                    foldout.Add(row);
                }

                listContainer.Add(foldout);
            }

            // 更新汇总信息
            summaryLabel.text = $"事件数: {eventNames.Length}   总监听者: {totalListeners}   上次更新: {System.DateTime.Now:HH:mm:ss}";
        }

        private void SetAllFoldouts(bool expanded)
        {
            foreach (var child in listContainer.Children())
            {
                if (child is Foldout f && f.userData is string evtName)
                {
                    f.value = expanded;
                    foldoutStates[evtName] = expanded;
                }
            }
        }
    }
}