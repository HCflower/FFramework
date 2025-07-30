using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 轨道项检查器基类
    /// 提供通用的UI创建方法和数据绑定功能
    /// </summary>
    public abstract class BaseTrackItemDataInspector : Editor
    {
        protected VisualElement root;
        protected BaseTrackItemData targetData;

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

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as BaseTrackItemData;
            root = new VisualElement();

            // 加载样式表
            LoadStyleSheets();

            // 创建标题
            CreateTitle();

            // 创建基础信息字段
            CreateBasicInfoFields();

            // 创建特定轨道类型的字段
            CreateSpecificFields();

            // 创建操作按钮
            CreateActionButtons();

            return root;
        }

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
            PerformDelete();
        }

        /// <summary>
        /// 执行实际的删除操作，子类应重写此方法
        /// </summary>
        protected abstract void PerformDelete();

        #endregion

        #region 受保护的UI创建方法

        /// <summary>
        /// 创建标题
        /// </summary>
        protected void CreateTitle()
        {
            Label title = new Label(TrackItemDisplayTitle);
            title.AddToClassList("ItemDataViewTitle");
            root.Add(title);
        }

        /// <summary>
        /// 创建基础信息字段
        /// </summary>
        protected void CreateBasicInfoFields()
        {
            // 轨道项名称
            CreateReadOnlyField("轨道项名称:", "trackItemName");

            // 总帧数
            CreateReadOnlyField("总帧数:", "frameCount");

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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            // 添加回车键事件处理
            if (onValueChanged != null)
            {
                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        SkillEditorEvent.TriggerRefreshRequested();
                        evt.StopPropagation();
                    }
                });
            }

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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            content.Add(field);
            root.Add(content);
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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue as T));
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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
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

            if (onValueChanged != null)
            {
                field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
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
            Label separatorTitle = new Label();
            separatorTitle.AddToClassList("SeparatorTitle");
            separatorTitle.text = separatorTitleText;
            root.Add(separatorTitle);
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

        #region 工具方法

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

                Debug.Log($"[{TrackItemTypeName}] 轨道UI刷新请求已延迟执行");
            }, "轨道UI刷新");
        }
        #endregion
    }
}
