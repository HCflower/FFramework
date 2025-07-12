using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器
    /// </summary>
    public class SkillEditor : EditorWindow
    {
        #region 数据

        private SkillConfig currentSkillConfig;     // 当前技能配置
        private bool isGlobalControlShow = false;   // 是否显示全局控制区域

        #endregion

        #region GUI

        private VisualElement mainContent;
        private VisualElement globalControlContent;
        private VisualElement trackControlContent;
        private ObjectField configObjectField;
        private TextField nameInputField;
        private TextField idInputField;
        private TextField descriptionField;
        private ScrollView track;
        private ScrollView trackItemContent;

        #endregion

        /// <summary>
        /// 技能编辑窗口
        /// </summary>
        [MenuItem("FFramework/⚔️SkillEditor #S", priority = 5)]
        public static void SkillEditorCreateWindow()
        {
            SkillEditor window = GetWindow<SkillEditor>();
            window.minSize = new Vector2(1000, 460);
            window.titleContent = new GUIContent("SkillEditor");
            window.Show();
        }

        // 窗口初始化
        private void OnEnable()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorStyle"));
            CreateMainContent(rootVisualElement);
            CreateGlobalControlContent(mainContent);
            CreateTrackControlContent(mainContent);
        }

        // 窗口关闭 - 保存数据
        private void OnDisable()
        {

        }

        // 刷新视图
        private void RefreshView()
        {
            // 刷新全局控制区域
            globalControlContent.Clear();
            if (isGlobalControlShow) CreateGlobalControlFunctionContent(globalControlContent);
            else CreateGlobalControlShowButton(globalControlContent);
        }

        #region 各个功能区域绘制

        // 创建主区域
        private void CreateMainContent(VisualElement visual)
        {
            mainContent = new VisualElement();
            mainContent.AddToClassList("MainContent");
            visual.Add(mainContent);
        }

        #region  全局控制区域

        // 创建全局控制区域
        private void CreateGlobalControlContent(VisualElement visual)
        {
            globalControlContent = new VisualElement();
            globalControlContent.AddToClassList("GlobalControlContent");
            visual.Add(globalControlContent);
            //创建全局控制区域显示按钮
            CreateGlobalControlShowButton(globalControlContent);
        }

        // 创建全局控制区域隐藏按钮
        private void CreateGlobalControlShowButton(VisualElement visual)
        {
            Button hideOrShowButton = new Button();
            hideOrShowButton.AddToClassList("GlobalControlShowButton");
            hideOrShowButton.text = "▶";
            hideOrShowButton.tooltip = "显示全局控制区域";
            hideOrShowButton.clicked += () =>
            {
                isGlobalControlShow = true;
                RefreshView();
            };
            visual.Add(hideOrShowButton);
        }

        // 创建全局控制区域功能区域
        private void CreateGlobalControlFunctionContent(VisualElement visual)
        {
            ScrollView functionContentScrollView = new ScrollView();
            // 设置滚动条隐藏
            functionContentScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            functionContentScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            functionContentScrollView.AddToClassList("GlobalControlFunctionContent");
            visual.Add(functionContentScrollView);

            // 创建技能配置选择区域
            CreateObjectField(functionContentScrollView, "技能配置:", ref configObjectField, typeof(SkillConfig), () =>
            {
                currentSkillConfig = configObjectField.value as SkillConfig;
            });
            if (currentSkillConfig) configObjectField.value = currentSkillConfig;

            CreateSelectImage(functionContentScrollView, "技能Icon:");

            // 创建名称设置区域
            CreateInputField(functionContentScrollView, "技能名称:", ref nameInputField, () =>
            {

            });

            // 创建技能ID设置区域
            CreateInputField(functionContentScrollView, "技能ID:", ref idInputField, () =>
            {

            });

            // 创建技能ID设置区域
            CreateInputField(functionContentScrollView, "技能描述:", ref descriptionField, () =>
            {

            });
            descriptionField.multiline = true;
            descriptionField.style.whiteSpace = WhiteSpace.Normal;
            descriptionField.style.height = 60;

            // 创建隐藏全局控制区域按钮
            CreateGlobalControlButton(functionContentScrollView, "↩", out Button _, () =>
            {
                isGlobalControlShow = false;
                RefreshView();
            }, "隐藏全局控制区域");

            // 创建保存数据按钮
            CreateGlobalControlButton(functionContentScrollView, "Save Config File", out Button _, () =>
            {

            });

            // 创建刷新视图按钮
            CreateGlobalControlButton(functionContentScrollView, "Refresh View", out Button _, () =>
            {

            });

            // 创建创建配置文件按钮
            CreateAddConfigButton(functionContentScrollView);

            // 创建删除配置文件按钮
            CreateGlobalControlButton(functionContentScrollView, "Delete Config File", out Button deleteButton, () =>
            {

            });
            deleteButton.style.backgroundColor = Color.red;
        }

        private void CreateAddConfigButton(VisualElement visual)
        {
            // 创建创建配置文件按钮
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("ControlButtonContent");
            // 添加配置按钮
            Button addConfigButton = new Button();
            addConfigButton.AddToClassList("Field");
            addConfigButton.style.flexDirection = FlexDirection.Row;
            addConfigButton.text = "Create Config File";
            addConfigButton.clicked += () =>
            {
                addConfigButton.text = "";
                // 避免重复添加输入框
                if (addConfigButton.Q<TextField>() != null) return;
                // 创建输入框
                TextField configName = new TextField();
                configName.AddToClassList("AddConfigInput");
                configName.tooltip = "请输入SkillConfig名称";
                configName.value = ""; // 确保初始值为空
                addConfigButton.Add(configName);
                // 创建确认按钮
                Button sureAddConfig = new Button();
                sureAddConfig.text = "+"; // 设置按钮文本
                sureAddConfig.style.fontSize = 20;
                sureAddConfig.AddToClassList("AddConfigButton");
                addConfigButton.Add(sureAddConfig);
                // 创建取消按钮
                Button cancelAddConfig = new Button();
                cancelAddConfig.AddToClassList("AddConfigButton");
                cancelAddConfig.text = "X"; // 设置取消按钮文本
                addConfigButton.Add(cancelAddConfig);
                // 确认按钮事件
                sureAddConfig.clicked += () =>
                {
                    // TODO: 创建配置文件
                    Debug.Log("创建配置文件");
                };

                // 取消按钮事件
                cancelAddConfig.clicked += () =>
                {
                    addConfigButton.Remove(configName);
                    addConfigButton.Remove(sureAddConfig);
                    addConfigButton.Remove(cancelAddConfig);
                    addConfigButton.text = "Create Config File";
                };

                // 回车键确认
                configName.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        evt.StopPropagation();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        evt.StopPropagation();
                    }
                });

                // 聚焦到输入框
                configName.Focus();
            };
            controlButtonContent.Add(addConfigButton);

            visual.Add(controlButtonContent);
        }

        // 创建图像选择区域
        private void CreateSelectImage(ScrollView visual, string titleText)
        {
            VisualElement controlButtonContent = ControlButtonContent(titleText);
            // 选择技能Icon
            Image skillIcon = new Image();
            skillIcon.AddToClassList("SkillIcon");
            controlButtonContent.Add(skillIcon);
            // 创建选择技能Icon按钮
            Button selectIcon = new Button();
            selectIcon.AddToClassList("SelectIcon");
            selectIcon.text = "Select";
            selectIcon.tooltip = "选择技能Icon";
            selectIcon.clicked += () =>
            {
                // TODO: 选择技能Icon
                Debug.Log("选择技能Icon");
            };
            skillIcon.Add(selectIcon);

            visual.Add(controlButtonContent);
        }

        #endregion


        #region  轨道控制区域

        // 创建轨道控制区域
        private void CreateTrackControlContent(VisualElement visual)
        {
            trackControlContent = new VisualElement();
            trackControlContent.AddToClassList("TrackControlContent");
            visual.Add(trackControlContent);
        }

        #endregion

        #endregion

        #region  通用绘制

        // 创建全局控制按钮
        private void CreateGlobalControlButton(VisualElement visual, string buttonText, out Button controlButton, Action action = null, string description = null)
        {
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("ControlButtonContent");
            // 按钮
            controlButton = new Button();
            controlButton.AddToClassList("Field");
            controlButton.text = buttonText;
            controlButton.tooltip = description;
            controlButton.clicked += () => { action?.Invoke(); };
            controlButtonContent.Add(controlButton);

            visual.Add(controlButtonContent);
        }

        // 创建ObjectField选择区域
        private void CreateObjectField(VisualElement visual, string titleText, ref ObjectField objectField, Type type, Action action = null)
        {
            VisualElement controlButtonContent = ControlButtonContent(titleText);
            // 对象选择区域
            objectField = new ObjectField();
            objectField.AddToClassList("Field");
            if (type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                objectField.objectType = type;
            }
            objectField.RegisterValueChangedCallback((e) => { action?.Invoke(); });
            controlButtonContent.Add(objectField);

            visual.Add(controlButtonContent);
        }

        // 创建inputField选择区域
        private void CreateInputField(VisualElement visual, string titleText, ref TextField inputField, Action action = null)
        {
            VisualElement controlButtonContent = ControlButtonContent(titleText);

            // 文本输入框
            inputField = new TextField();
            inputField.AddToClassList("Field");
            inputField.tooltip = titleText;
            inputField.RegisterValueChangedCallback((e) => { action?.Invoke(); });
            controlButtonContent.Add(inputField);

            visual.Add(controlButtonContent);
        }

        // 创建控制按钮区域
        private VisualElement ControlButtonContent(string titleText)
        {
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("ControlButtonContent");
            // title
            Label title = new Label();
            title.AddToClassList("ControlButtonContentTitle");
            title.text = titleText;
            controlButtonContent.Add(title);
            return controlButtonContent;
        }

        private void CreateTrack(VisualElement visual)
        {

        }

        private void CreateTrackItem(VisualElement visual)
        {

        }

        #endregion

    }
}