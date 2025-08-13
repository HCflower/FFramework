using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 轨道项检查器基类
    /// 提供通用的UI创建方法和数据绑定功能
    /// </summary>
    public abstract class TrackItemDataInspectorBase : Editor
    {
        #region 字段和属性

        protected VisualElement root;
        protected TrackItemDataBase targetData;
        private bool isInitialized = false;
        protected string lastTrackItemName;
        /// <summary>
        /// 获取轨道项类型名称，用于样式类名
        /// </summary>
        protected abstract string TrackItemTypeName { get; }

        /// <summary>
        /// 获取轨道项显示标题
        /// </summary>
        protected abstract string TrackItemDisplayTitle { get; }

        /// <summary>
        /// 获取删除按钮的文本
        /// </summary>
        protected abstract string DeleteButtonText { get; }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 创建检查器UI
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as TrackItemDataBase;
            root = new VisualElement();

            if (targetData == null)
            {
                CreateErrorDisplay("无法获取轨道项数据");
                return root;
            }

            // 初始化检查器
            InitializeInspector();
            isInitialized = true;

            return root;
        }

        /// <summary>
        /// 初始化检查器
        /// </summary>
        private void InitializeInspector()
        {
            // 加载样式表
            LoadStyleSheets();

            // 创建UI结构
            BuildInspectorUI();

            // 注册事件监听
            RegisterEventHandlers();
        }

        /// <summary>
        /// 构建检查器UI
        /// </summary>
        private void BuildInspectorUI()
        {
            // 创建标题
            CreateTitle();

            // 创建基础信息字段
            CreateBasicInfoFields();

            // 创建特定轨道类型的字段
            CreateSpecificFields();

            // 创建操作按钮
            CreateActionButtons();
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        protected virtual void RegisterEventHandlers()
        {
            // 子类可重写此方法来注册特定事件
        }

        /// <summary>
        /// 当组件被销毁时清理资源
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnregisterEventHandlers();
        }

        /// <summary>
        /// 取消事件处理器注册
        /// </summary>
        protected virtual void UnregisterEventHandlers()
        {
            // 子类可重写此方法来清理事件监听
        }

        #endregion

        #region 检查器面板管理

        /// <summary>
        /// 刷新检查器面板
        /// 重新构建整个UI，用于数据结构发生重大变化时
        /// </summary>
        public void RefreshInspectorPanel()
        {
            if (!isInitialized || root == null)
                return;

            SafeExecute(() =>
            {
                // 清空当前UI
                root.Clear();

                // 重新构建UI
                BuildInspectorUI();

                // 重新注册事件
                RegisterEventHandlers();

                Debug.Log($"[{TrackItemTypeName}] 检查器面板已刷新");
            }, "检查器面板刷新");
        }

        /// <summary>
        /// 刷新检查器数据
        /// 只更新数据显示，不重建UI结构
        /// </summary>
        public void RefreshInspectorData()
        {
            if (!isInitialized || root == null)
                return;

            SafeExecute(() =>
            {
                // 重新序列化对象以获取最新数据
                serializedObject.Update();

                // 强制重绘以确保UI元素显示最新值
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (root != null)
                    {
                        // 触发UI重绘
                        root.MarkDirtyRepaint();
                    }
                    Repaint();
                };

                // 触发UI数据更新
                OnDataRefreshed();
            }, "检查器数据刷新");
        }

        /// <summary>
        /// 数据刷新后的回调，子类可重写
        /// </summary>
        protected virtual void OnDataRefreshed()
        {
            // 子类可重写此方法来处理数据刷新后的逻辑
        }

        /// <summary>
        /// 创建错误显示
        /// </summary>
        private void CreateErrorDisplay(string errorMessage)
        {
            var errorLabel = new Label($"错误: {errorMessage}");
            errorLabel.AddToClassList("ErrorLabel");
            root.Add(errorLabel);
        }

        #endregion

        #region 虚方法 - 子类可重写

        /// <summary>
        /// 加载样式表，子类可重写以加载额外的样式
        /// </summary>
        protected virtual void LoadStyleSheets()
        {
            root.styleSheets.Add(Resources.Load<StyleSheet>("USS/ItemDataInspectorStyle"));
        }

        /// <summary>
        /// 创建特定轨道类型的字段，子类必须实现
        /// </summary>
        protected abstract void CreateSpecificFields();

        /// <summary>
        /// 创建额外的操作按钮，子类可重写
        /// </summary>
        protected virtual void CreateAdditionalActionButtons()
        {
            // 默认不添加额外按钮
        }

        /// <summary>
        /// 处理删除操作，子类可重写以实现特定的删除逻辑
        /// </summary>
        protected virtual void OnDeleteButtonClicked()
        {
            SafeExecute(() =>
              {
                  if (EditorUtility.DisplayDialog("删除确认", $"确定要删除此轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销", "确认删除", "取消"))
                      DeleteTrackItem();
              }, "删除游戏物体轨道项");
        }

        protected abstract void DeleteTrackItem();

        #endregion

        #region 受保护的UI创建方法

        /// <summary>
        /// 创建标题
        /// </summary>
        protected void CreateTitle()
        {
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("ItemDataViewTitleContent");

            // left line
            var leftLine = new Label();
            leftLine.AddToClassList("ItemDataViewTitleLine");
            titleContainer.Add(leftLine);
            // title
            Label title = new Label(TrackItemDisplayTitle);
            title.AddToClassList("ItemDataViewTitle");
            titleContainer.Add(title);
            // right line
            var rightLine = new Label();
            rightLine.AddToClassList("ItemDataViewTitleLine");
            titleContainer.Add(rightLine);

            root.Add(titleContainer);
        }

        /// <summary>
        /// 创建基础信息字段
        /// </summary>
        protected void CreateBasicInfoFields()
        {
            // 轨道项名称
            CreateTextField("轨道项名称:", "trackItemName", OnTrackItemNameChanged);

            // 总帧数
            CreateReadOnlyField("基准帧数:", "frameCount");

            // 起始帧
            CreateIntegerField("起始帧:", "startFrame", OnStartFrameChanged);

            // 持续帧数
            CreateIntegerField("持续帧数:", "durationFrame", OnDurationFrameChanged);
        }

        /// <summary>
        /// 创建操作按钮
        /// </summary>
        protected void CreateActionButtons()
        {
            // 创建额外的操作按钮
            CreateAdditionalActionButtons();

            // 创建删除按钮
            CreateDeleteButton();
        }

        /// <summary>
        /// 创建删除按钮
        /// </summary>
        protected void CreateDeleteButton()
        {
            var deleteContent = CreateContentContainer("");
            var deleteButton = new Button(OnDeleteButtonClicked)
            {
                text = DeleteButtonText
            };
            deleteButton.AddToClassList("DeleteButton");
            deleteContent.Add(deleteButton);
            root.Add(deleteContent);
        }

        #endregion

        #region 通用UI创建方法

        /// <summary>
        /// 创建内容容器
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <returns>内容容器</returns>
        protected VisualElement CreateContentContainer(string labelText)
        {
            VisualElement content = new VisualElement();
            content.AddToClassList("ItemDataViewContent");
            content.AddToClassList($"ItemDataViewContent-{TrackItemTypeName}");

            if (!string.IsNullOrEmpty(labelText))
            {
                Label label = new Label(labelText);
                label.AddToClassList("ItemDataViewLabel");
                content.Add(label);
            }

            return content;
        }

        /// <summary>
        /// 创建只读字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        protected void CreateReadOnlyField(string labelText, string propertyName)
        {
            var content = CreateContentContainer(labelText);
            var label = new Label();
            label.BindProperty(serializedObject.FindProperty(propertyName));
            content.Add(label);
            root.Add(content);
        }

        /// <summary>
        /// 创建整数输入字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateIntegerField(string labelText, string propertyName, System.Action<int> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new IntegerField();
            field.AddToClassList("TextField");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 注册值变化事件
            RegisterFieldEvents(field, true, onValueChanged);

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 创建浮点数输入字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateFloatField(string labelText, string propertyName, System.Action<float> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new FloatField();
            field.AddToClassList("TextField");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 注册值变化事件
            RegisterFieldEvents(field, true, onValueChanged);

            content.Add(field);
            root.Add(content);
        }


        /// <summary>
        /// 创建文本输入字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateTextField(string labelText, string propertyName, System.Action<string> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new TextField();
            field.AddToClassList("TextField");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 只在回车时触发onValueChanged，输入时不自动触发
            if (onValueChanged != null)
            {
                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        // 在回车时，field.value 就是新值，但我们需要传递新值给回调
                        onValueChanged(field.value);
                        ApplyDataChangesAndRefresh();
                        evt.StopPropagation();
                    }
                });
            }

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 为字段注册通用事件处理器
        /// </summary>
        /// <typeparam name="T">字段值类型</typeparam>
        /// <param name="field">UI字段</param>
        /// <param name="onValueChanged">值变化回调</param>
        private void RegisterFieldEvents<T>(BaseField<T> field, bool isAutoRefresh, System.Action<T> onValueChanged = null)
        {
            if (isAutoRefresh && onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    // 注意：不在这里自动刷新，让子类控制刷新时机
                    onValueChanged(evt.newValue);
                });
            }

            // 添加回车键事件处理，用于立即刷新
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    ApplyDataChangesAndRefresh();
                    evt.StopPropagation();
                }
            });
        }
        /// <summary>
        /// 创建布尔开关字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateToggleField(string labelText, string propertyName, System.Action<bool> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new Toggle();
            field.AddToClassList("Toggle");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 注册值变化事件（Toggle 不需要回车键处理）
            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    onValueChanged(evt.newValue);
                });
            }

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 创建对象字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="objectType">对象类型</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateObjectField<T>(string labelText, string propertyName, System.Action<T> onValueChanged = null) where T : UnityEngine.Object
        {
            var content = CreateContentContainer(labelText);
            var field = new ObjectField() { objectType = typeof(T) };
            field.AddToClassList("ObjectField");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 对象字段特殊处理（不需要回车键事件）
            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    onValueChanged(evt.newValue as T);
                });
            }

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 创建颜色字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateColorField(string labelText, string propertyName, System.Action<Color> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new ColorField();
            field.AddToClassList("ColorField");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 颜色字段特殊处理（不需要回车键事件）
            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    onValueChanged(evt.newValue);
                });
            }

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 创建动画曲线字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateCurveField(string labelText, string propertyName, System.Action<AnimationCurve> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new CurveField();
            field.AddToClassList("CurveField");
            field.BindProperty(serializedObject.FindProperty(propertyName));

            // 曲线字段特殊处理（不需要回车键事件）
            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    onValueChanged(evt.newValue);
                });
            }

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 创建滑块字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="minValue">最小值</param>
        /// <param name="maxValue">最大值</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateSliderField(string labelText, string propertyName, float minValue, float maxValue, System.Action<float> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new Slider(minValue, maxValue);
            field.AddToClassList("Slider");
            field.BindProperty(serializedObject.FindProperty(propertyName));
            content.Add(field);

            // 当前数值显示
            var currentValueLabel = new Label();
            currentValueLabel.AddToClassList("SliderValueLabel");
            content.Add(currentValueLabel);
            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    currentValueLabel.text = evt.newValue.ToString();
                    onValueChanged(evt.newValue);
                });
            }

            root.Add(content);
        }

        /// <summary>
        /// 创建Vector3字段
        /// </summary>
        /// <param name="labelText">标签文本</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="onValueChanged">值变化回调</param>
        protected void CreateVector3Field(string labelText, string propertyName, System.Action<Vector3> onValueChanged = null)
        {
            var content = CreateContentContainer(labelText);
            var field = new Vector3Field();
            field.AddToClassList("Vector3Field");

            // Vector3Field需要手动设置值
            var property = serializedObject.FindProperty(propertyName);
            field.value = property.vector3Value;

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    property.vector3Value = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                    onValueChanged(evt.newValue);
                });
            }
            else
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    property.vector3Value = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            content.Add(field);
            root.Add(content);
        }

        /// <summary>
        /// 创建自定义按钮
        /// </summary>
        /// <param name="labelText">标签文本（为空则不显示标签）</param>
        /// <param name="buttonText">按钮文本</param>
        /// <param name="onClick">点击回调</param>
        /// <param name="buttonClass">按钮样式类名</param>
        protected void CreateButton(string labelText, string buttonText, System.Action onClick, string buttonClass = "Button")
        {
            var content = CreateContentContainer(labelText);
            var button = new Button(onClick)
            {
                text = buttonText
            };
            button.AddToClassList(buttonClass);
            content.Add(button);
            root.Add(content);
        }

        /// <summary>
        /// 创建分隔线
        /// </summary>
        protected void CreateSeparatorTitle(string separatorTitleText)
        {
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("ItemDataViewTitleContent");

            // left line
            var leftLine = new Label();
            leftLine.AddToClassList("ItemDataViewTitleLine");
            titleContainer.Add(leftLine);
            // title
            Label separatorTitle = new Label();
            separatorTitle.AddToClassList("SeparatorTitle");
            separatorTitle.text = separatorTitleText;
            titleContainer.Add(separatorTitle);
            // right line
            var rightLine = new Label();
            rightLine.AddToClassList("ItemDataViewTitleLine");
            titleContainer.Add(rightLine);

            root.Add(titleContainer);
        }

        /// <summary>
        /// 创建分组容器
        /// </summary>
        /// <param name="groupTitle">分组标题</param>
        /// <returns>分组容器</returns>
        protected VisualElement CreateGroup(string groupTitle)
        {
            var group = new VisualElement();
            group.AddToClassList("Group");

            if (!string.IsNullOrEmpty(groupTitle))
            {
                var title = new Label(groupTitle);
                title.AddToClassList("GroupTitle");
                group.Add(title);
            }

            root.Add(group);
            return group;
        }

        #endregion

        #region 虚方法 - 基础事件处理
        /// <summary>
        /// 轨道项名称变化事件处理
        /// </summary>
        /// <param name="newValue">新的轨道项名称</param>
        protected virtual void OnTrackItemNameChanged(string newValue) { }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected virtual void OnStartFrameChanged(int newValue) { }

        /// <summary>
        /// 持续帧数变化事件处理
        /// </summary>
        /// <param name="newValue">新的持续帧数值</param>
        protected virtual void OnDurationFrameChanged(int newValue) { }

        #endregion

        #region 工具方法和辅助功能

        /// <summary>
        /// 标记技能配置为已修改
        /// </summary>
        protected void MarkSkillConfigDirty()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        protected void SafeExecute(System.Action action, string operationName)
        {
            try
            {
                action?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{TrackItemTypeName}] 执行 {operationName} 时发生错误: {e.Message}");
            }
        }

        /// <summary>
        /// 刷新轨道UI显示
        /// 当检查器数据发生变化时，调用此方法同步更新轨道UI
        /// </summary>
        protected void RefreshTrackUI()
        {
            SafeExecute(() =>
            {
                // 延迟刷新，避免在用户交互过程中立即刷新
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    // 直接触发静态事件刷新
                    SkillEditorEvent.TriggerRefreshRequested();

                    // 触发当前帧变化事件以确保UI同步
                    SkillEditorEvent.TriggerCurrentFrameChanged(SkillEditorData.CurrentFrame);

                    // 标记技能配置为已修改
                    MarkSkillConfigDirty();
                };

            }, "轨道UI刷新");
        }

        /// <summary>
        /// 应用数据变化并刷新相关UI
        /// 综合方法，同时处理检查器和轨道UI的刷新
        /// </summary>
        protected void ApplyDataChangesAndRefresh()
        {
            SafeExecute(() =>
            {
                // 应用序列化对象的修改
                serializedObject.ApplyModifiedProperties();

                // 立即刷新检查器数据显示
                RefreshInspectorData();

                // 立即刷新轨道UI
                RefreshTrackUI();

                // 强制重绘检查器
                Repaint();
            }, "数据变化应用和UI刷新");
        }

        #endregion
    }
}
