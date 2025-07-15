using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// UI构建器
    /// </summary>
    public class SkillEditorUIBuilder
    {
        private readonly EventManager eventManager;

        public SkillEditorUIBuilder(EventManager eventManager)
        {
            this.eventManager = eventManager;
        }

        #region 主要结构创建
        public VisualElement CreateMainContent(VisualElement parent)
        {
            var mainContent = new VisualElement();
            mainContent.AddToClassList("MainContent");
            parent.Add(mainContent);
            return mainContent;
        }

        public VisualElement CreateGlobalControlArea(VisualElement parent, bool isShow)
        {
            var globalControlContent = new VisualElement();
            globalControlContent.AddToClassList("GlobalControlContent");
            parent.Add(globalControlContent);
            return globalControlContent;
        }

        public VisualElement CreateTrackArea(VisualElement parent)
        {
            var allTrackContent = new VisualElement();
            allTrackContent.AddToClassList("TrackContent");
            parent.Add(allTrackContent);
            return allTrackContent;
        }

        public (VisualElement controlArea, ScrollView controlScrollView, VisualElement controlContent,
                VisualElement trackArea, ScrollView trackScrollView, VisualElement trackContent)
        CreateTrackStructure(VisualElement parent, SkillEditorDataManager dataManager)
        {
            // 创建轨道控制区域
            var controlArea = new VisualElement();
            controlArea.AddToClassList("TrackItemControlContent");
            parent.Add(controlArea);

            // 创建控制按钮区域
            CreateTrackControlButtons(controlArea, dataManager);

            // 创建搜索和添加区域
            CreateSearchAndAddArea(controlArea);

            // 创建轨道控制滚动视图
            var (controlScrollView, controlContent) = CreateScrollView(controlArea, "TrackScrollView", "TrackScrollViewContent");
            controlScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            controlScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // 添加测试轨道控制
            AddTestTrackControls(controlContent);

            // 创建轨道区域
            var trackArea = new VisualElement();
            trackArea.AddToClassList("TrackControlContent");
            parent.Add(trackArea);

            // 创建轨道滚动视图
            var (trackScrollView, trackContent) = CreateScrollView(trackArea, "TrackScrollView", "TrackScrollViewContent");
            // 设置轨道内容的初始宽度
            trackContent.style.width = dataManager.CalculateTimelineWidth();
            // 添加测试轨道
            AddTestTracks(trackContent, dataManager.CalculateTimelineWidth());

            return (controlArea, controlScrollView, controlContent, trackArea, trackScrollView, trackContent);
        }
        #endregion

        #region 全局控制区域
        public void RefreshGlobalControl(VisualElement globalControlContent, SkillEditorDataManager dataManager)
        {
            globalControlContent.Clear();
            if (dataManager.IsGlobalControlShow)
                CreateGlobalControlFunction(globalControlContent, dataManager);
            else
                CreateGlobalControlShowButton(globalControlContent);
        }

        private void CreateGlobalControlShowButton(VisualElement parent)
        {
            var buttonWrapper = new VisualElement();
            buttonWrapper.AddToClassList("GlobalControlShowButtonWrapper");

            var showButton = CreateButton("", "Global Setting", () =>
            {
                eventManager.TriggerGlobalControlToggled(true);
            });
            showButton.AddToClassList("GlobalControlShowButton");

            buttonWrapper.Add(showButton);
            parent.Add(buttonWrapper);
        }

        private void CreateGlobalControlFunction(VisualElement parent, SkillEditorDataManager dataManager)
        {
            var scrollView = new ScrollView();
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.AddToClassList("GlobalControlFunctionContent");
            parent.Add(scrollView);

            // 技能配置选择
            CreateObjectField(scrollView, "技能配置:", typeof(SkillConfig), dataManager.CurrentSkillConfig, (obj) =>
            {
                dataManager.CurrentSkillConfig = obj as SkillConfig;
                eventManager.TriggerSkillConfigChanged(dataManager.CurrentSkillConfig);
            });

            // 技能Icon选择
            CreateImageSelector(scrollView, "技能Icon:");

            // 输入字段
            CreateInputField(scrollView, "技能名称:", "", null);
            CreateInputField(scrollView, "技能ID:", "", null);
            CreateMultilineInputField(scrollView, "技能描述:", "", null);

            // 控制按钮
            CreateControlButtons(scrollView);
        }

        private void CreateControlButtons(VisualElement parent)
        {
            CreateButton(parent, "↩", "隐藏全局控制区域", () =>
            {
                eventManager.TriggerGlobalControlToggled(false);
            });

            CreateButton(parent, "刷新视图", "", () =>
            {
                eventManager.TriggerRefreshRequested();
            });

            CreateButton(parent, "保存配置文件", "", () =>
            {
                // 保存逻辑
            });

            CreateAddConfigButton(parent);

            var deleteButton = CreateButton(parent, "删除配置文件", "", () =>
            {
                // 删除逻辑
            });
            deleteButton.style.backgroundColor = Color.red;
        }
        #endregion

        #region 轨道控制区域
        public void RefreshTrackContent(VisualElement trackContent, SkillEditorDataManager dataManager)
        {
            if (trackContent != null)
            {
                float newWidth = dataManager.CalculateTimelineWidth();
                trackContent.style.width = newWidth;

                // 更新轨道内容的宽度
                trackContent.style.minWidth = newWidth;

                // 重新创建轨道以确保宽度正确
                AddTestTracks(trackContent, newWidth);
            }
        }

        private void CreateTrackControlButtons(VisualElement parent, SkillEditorDataManager dataManager)
        {
            var buttonContent = new VisualElement();
            buttonContent.AddToClassList("TrackItemControlButtonContent");
            parent.Add(buttonContent);

            // 播放控制按钮
            CreateIconButton(buttonContent, "d_Animation.PrevKey", "上一帧", () =>
            {
                eventManager.TriggerCurrentFrameChanged(dataManager.CurrentFrame - 1);
            });

            CreateIconButton(buttonContent, dataManager.IsPlaying ? "d_PauseButton@2x" : "d_Animation.Play", "播放", () =>
            {
                dataManager.IsPlaying = !dataManager.IsPlaying;
                eventManager.TriggerPlayStateChanged(dataManager.IsPlaying);
            });

            CreateIconButton(buttonContent, "d_Animation.NextKey", "下一帧", () =>
            {
                eventManager.TriggerCurrentFrameChanged(dataManager.CurrentFrame + 1);
            });

            CreateIconButton(buttonContent, "d_preAudioLoopOff@2x", "循环播放", () =>
            {
                dataManager.IsLoop = !dataManager.IsLoop;
            });

            // 帧输入区域
            CreateFrameInputArea(buttonContent, dataManager);
        }

        private void CreateFrameInputArea(VisualElement parent, SkillEditorDataManager dataManager)
        {
            var frameInputContent = new VisualElement();
            frameInputContent.AddToClassList("FrameInputContent");
            parent.Add(frameInputContent);

            // 当前帧输入
            var currentFrameInput = CreateIntegerField("当前帧", dataManager.CurrentFrame, (value) =>
            {
                eventManager.TriggerCurrentFrameChanged(value);
            });
            currentFrameInput.name = "current-frame-input"; // 添加名称以便查找
            frameInputContent.Add(currentFrameInput);

            // 分隔符
            var separator = new Label("|");
            separator.AddToClassList("SeparatorLabel");
            frameInputContent.Add(separator);

            // 最大帧输入
            var maxFrameInput = CreateIntegerField("最大帧", dataManager.MaxFrame, (value) =>
            {
                eventManager.TriggerMaxFrameChanged(value);
            });
            frameInputContent.Add(maxFrameInput);
        }

        private void CreateSearchAndAddArea(VisualElement parent)
        {
            var searchAddArea = new VisualElement();
            searchAddArea.AddToClassList("SearchOrAddTrack");
            parent.Add(searchAddArea);

            // 搜索输入框
            var searchInput = new TextField();
            searchInput.AddToClassList("SearchTrackInput");
            searchInput.tooltip = "搜索轨道";

            var textElement = searchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null) textElement.style.marginLeft = 18;

            var searchIcon = new Image();
            searchIcon.AddToClassList("SearchTrackInputIcon");
            searchInput.Add(searchIcon);
            searchAddArea.Add(searchInput);

            // 添加轨道按钮
            var addButton = new Button();
            addButton.AddToClassList("AddTrackButton");
            searchAddArea.Add(addButton);
        }

        private void AddTestTrackControls(VisualElement parent)
        {
            for (int i = 0; i < 15; i++)
            {
                new SkillEditorTrackControl(parent);
            }
        }

        private void AddTestTracks(VisualElement parent, float width)
        {
            // 清除现有轨道（如果是刷新操作）
            parent.Clear();

            for (int i = 0; i < 15; i++)
            {
                new SkillEditorTrack(parent, width);
            }
        }

        #endregion

        #region 通用UI创建方法
        public Button CreateButton(string text, string tooltip = "", Action onClick = null)
        {
            var button = new Button();
            button.text = text;
            button.tooltip = tooltip;
            if (onClick != null) button.clicked += onClick;
            return button;
        }

        public Button CreateButton(VisualElement parent, string text, string tooltip = "", Action onClick = null)
        {
            var controlContent = new VisualElement();
            controlContent.AddToClassList("ControlButtonContent");

            var button = CreateButton(text, tooltip, onClick);
            button.AddToClassList("Field");
            controlContent.Add(button);

            parent.Add(controlContent);
            return button;
        }

        public Button CreateIconButton(VisualElement parent, string iconPath, string tooltip = "", Action onClick = null)
        {
            var button = new Button();
            button.AddToClassList("TrackItemControlButton");
            button.tooltip = tooltip;
            button.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
            if (onClick != null) button.clicked += onClick;

            parent.Add(button);
            return button;
        }

        public ObjectField CreateObjectField(VisualElement parent, string labelText, Type objectType, UnityEngine.Object currentValue = null, Action<UnityEngine.Object> onValueChanged = null)
        {
            var container = CreateLabeledContainer(parent, labelText);

            var objectField = new ObjectField();
            objectField.AddToClassList("Field");
            objectField.objectType = objectType;
            objectField.value = currentValue;

            if (onValueChanged != null)
            {
                objectField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            container.Add(objectField);
            return objectField;
        }

        public TextField CreateInputField(VisualElement parent, string labelText, string currentValue = "", Action<string> onValueChanged = null)
        {
            var container = CreateLabeledContainer(parent, labelText);

            var inputField = new TextField();
            inputField.AddToClassList("Field");
            inputField.value = currentValue;
            inputField.tooltip = labelText;

            if (onValueChanged != null)
            {
                inputField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            container.Add(inputField);
            return inputField;
        }

        public TextField CreateMultilineInputField(VisualElement parent, string labelText, string currentValue = "", Action<string> onValueChanged = null)
        {
            var inputField = CreateInputField(parent, labelText, currentValue, onValueChanged);
            inputField.multiline = true;
            inputField.style.whiteSpace = WhiteSpace.Normal;
            inputField.style.height = 60;
            return inputField;
        }

        public IntegerField CreateIntegerField(string tooltip, int currentValue, Action<int> onValueChanged = null)
        {
            var integerField = new IntegerField();
            integerField.AddToClassList("FrameInput");
            integerField.tooltip = tooltip;
            integerField.value = currentValue;

            if (onValueChanged != null)
            {
                integerField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            return integerField;
        }

        public void CreateImageSelector(VisualElement parent, string labelText)
        {
            var container = CreateLabeledContainer(parent, labelText);

            var skillIcon = new Image();
            skillIcon.AddToClassList("SkillIcon");
            container.Add(skillIcon);

            var selectButton = new Button();
            selectButton.AddToClassList("SelectIcon");
            selectButton.text = "Select";
            selectButton.tooltip = "选择技能Icon";
            selectButton.clicked += () =>
            {
                Debug.Log("选择技能Icon");
            };
            skillIcon.Add(selectButton);
        }

        private void CreateAddConfigButton(VisualElement parent)
        {
            var controlContent = new VisualElement();
            controlContent.AddToClassList("ControlButtonContent");

            var addButton = new Button();
            addButton.AddToClassList("Field");
            addButton.style.flexDirection = FlexDirection.Row;
            addButton.text = "创建配置文件";
            addButton.clicked += () => HandleAddConfigClick(addButton);

            controlContent.Add(addButton);
            parent.Add(controlContent);
        }

        private void HandleAddConfigClick(Button addButton)
        {
            addButton.text = "";
            if (addButton.Q<TextField>() != null) return;

            var configName = new TextField();
            configName.AddToClassList("AddConfigInput");
            configName.tooltip = "请输入SkillConfig名称";
            configName.value = "";
            addButton.Add(configName);

            var confirmButton = new Button();
            confirmButton.text = "+";
            confirmButton.style.fontSize = 20;
            confirmButton.AddToClassList("AddConfigButton");
            addButton.Add(confirmButton);

            var cancelButton = new Button();
            cancelButton.text = "X";
            cancelButton.AddToClassList("AddConfigButton");
            addButton.Add(cancelButton);

            // 定义确认和取消的处理方法
            System.Action confirmAction = () =>
            {
                Debug.Log($"创建配置文件: {configName.value}");
                ResetAddConfigButton(addButton, configName, confirmButton, cancelButton);
            };

            System.Action cancelAction = () =>
            {
                ResetAddConfigButton(addButton, configName, confirmButton, cancelButton);
            };

            // 绑定按钮事件
            confirmButton.clicked += confirmAction;
            cancelButton.clicked += cancelAction;

            // 键盘事件处理
            configName.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    confirmAction();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    cancelAction();
                    evt.StopPropagation();
                }
            });

            configName.Focus();
        }

        private void ResetAddConfigButton(Button addButton, TextField configName, Button confirmButton, Button cancelButton)
        {
            addButton.Remove(configName);
            addButton.Remove(confirmButton);
            addButton.Remove(cancelButton);
            addButton.text = "创建配置文件";
        }

        public (ScrollView scrollView, VisualElement content) CreateScrollView(VisualElement parent, string scrollViewClass, string contentClass)
        {
            var scrollView = new ScrollView();
            scrollView.AddToClassList(scrollViewClass);

            var content = new VisualElement();
            content.AddToClassList(contentClass);
            scrollView.Add(content);

            parent.Add(scrollView);
            return (scrollView, content);
        }

        private VisualElement CreateLabeledContainer(VisualElement parent, string labelText)
        {
            var container = new VisualElement();
            container.AddToClassList("ControlButtonContent");

            var label = new Label(labelText);
            label.AddToClassList("ControlButtonContentTitle");
            container.Add(label);

            parent.Add(container);
            return container;
        }
        #endregion
    }
}

