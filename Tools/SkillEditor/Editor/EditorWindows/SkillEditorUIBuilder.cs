using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器UI构建器
    /// 负责创建和管理技能编辑器的所有UI元素，包括主界面结构、全局控制区域、轨道管理区域等
    /// 提供完整的UI构建、事件处理和动态更新功能
    /// </summary>
    public class SkillEditorUIBuilder
    {
        #region 私有字段

        /// <summary>轨道管理器</summary>
        private readonly SkillEditorTrackHandler trackManager;

        /// <summary>所有轨道内容容器</summary>
        private VisualElement allTrackContent;

        /// <summary>轨道控制区域内容容器</summary>
        private VisualElement trackControlContent;

        #endregion

        #region 构造函数

        /// <summary>
        /// UI构建器构造函数
        /// 初始化事件管理器并订阅刷新事件
        /// </summary>
        public SkillEditorUIBuilder()
        {
            this.trackManager = new SkillEditorTrackHandler();

            // 订阅刷新事件
            SkillEditorEvent.OnRefreshRequested += OnRefreshRequested;
        }

        #endregion

        #region 主要结构创建

        /// <summary>
        /// 创建主要内容区域
        /// 作为整个编辑器界面的根容器
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <returns>创建的主内容容器</returns>
        public VisualElement CreateMainContent(VisualElement parent)
        {
            return CreateAndAddElement(parent, "MainContent");
        }

        /// <summary>
        /// 创建全局控制区域
        /// 包含技能配置、播放控制等全局功能
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <returns>创建的全局控制区域容器</returns>
        public VisualElement CreateGlobalControlArea(VisualElement parent)
        {
            return CreateAndAddElement(parent, "GlobalControlContent");
        }

        /// <summary>
        /// 创建轨道区域
        /// 包含所有轨道的显示和管理功能
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <returns>创建的轨道区域容器</returns>
        public VisualElement CreateTrackArea(VisualElement parent)
        {
            return CreateAndAddElement(parent, "TrackContent");
        }

        /// <summary>
        /// 创建轨道结构
        /// 包括轨道控制区域和轨道内容区域的完整结构
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <returns>包含所有轨道相关容器的结构体</returns>
        public TrackStructureResult CreateTrackStructure(VisualElement parent)
        {
            // 创建轨道控制区域
            var controlArea = CreateAndAddElement(parent, "TrackItemControlContent");
            CreateTrackControlButtons(controlArea);
            CreateSearchAndAddArea(controlArea);

            // 创建轨道控制滚动视图
            var (controlScrollView, controlContent) = CreateScrollView(controlArea, "TrackScrollView", "TrackScrollViewContent");
            this.trackControlContent = controlContent;
            SetScrollViewVisibility(controlScrollView, false, false);

            // 创建轨道区域
            var trackArea = CreateAndAddElement(parent, "TrackControlContent");
            var (trackScrollView, trackContent) = CreateScrollView(trackArea, "TrackScrollView", "TrackScrollViewContent");
            this.allTrackContent = trackContent;
            trackContent.style.width = SkillEditorData.CalculateTimelineWidth();

            // 初始化轨道管理器
            trackManager.Initialize(this.trackControlContent, this.allTrackContent);

            return new TrackStructureResult
            {
                ControlArea = controlArea,
                ControlScrollView = controlScrollView,
                ControlContent = controlContent,
                TrackArea = trackArea,
                TrackScrollView = trackScrollView,
                TrackContent = trackContent
            };
        }

        /// <summary>
        /// 轨道结构创建结果
        /// 包含轨道管理所需的所有UI容器引用
        /// </summary>
        public struct TrackStructureResult
        {
            /// <summary>轨道控制区域容器</summary>
            public VisualElement ControlArea;

            /// <summary>轨道控制滚动视图</summary>
            public ScrollView ControlScrollView;

            /// <summary>轨道控制内容容器</summary>
            public VisualElement ControlContent;

            /// <summary>轨道区域容器</summary>
            public VisualElement TrackArea;

            /// <summary>轨道滚动视图</summary>
            public ScrollView TrackScrollView;

            /// <summary>轨道内容容器</summary>
            public VisualElement TrackContent;
        }

        #endregion

        #region 全局控制区域

        /// <summary>
        /// 刷新全局控制区域内容
        /// 根据显示状态决定是显示完整功能面板还是展开按钮
        /// </summary>
        /// <param name="globalControlContent">全局控制内容容器</param>
        public void RefreshGlobalControl(VisualElement globalControlContent)
        {
            globalControlContent.Clear();
            if (SkillEditorData.IsGlobalControlShow)
                CreateGlobalControlFunction(globalControlContent);
            else
                CreateGlobalControlShowButton(globalControlContent);
        }

        /// <summary>
        /// 创建全局控制展开按钮
        /// 当全局控制区域收起时显示的展开按钮
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateGlobalControlShowButton(VisualElement parent)
        {
            var buttonWrapper = CreateAndAddElement(parent, "GlobalControlShowButtonWrapper");
            var showButton = CreateButton("", "Global Setting", () => SkillEditorEvent.TriggerGlobalControlToggled(true));
            showButton.AddToClassList("GlobalControlShowButton");
            buttonWrapper.Add(showButton);
        }

        /// <summary>
        /// 创建全局控制功能面板
        /// 包含技能配置和控制按钮的完整功能界面
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateGlobalControlFunction(VisualElement parent)
        {
            var scrollView = CreateScrollView(parent, "GlobalControlFunctionContent");

            // 技能配置区域
            CreateSkillConfigSection(scrollView);

            // 控制按钮区域
            CreateGlobalControlButtons(scrollView);
        }

        /// <summary>
        /// 创建技能配置区域
        /// 包含技能配置文件选择、帧率设置、技能属性编辑等功能
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateSkillConfigSection(VisualElement parent)
        {
            // 技能配置文件选择
            CreateObjectField(parent, "技能配置:", typeof(SkillConfig), SkillEditorData.CurrentSkillConfig, (obj) =>
            {
                SkillEditorData.CurrentSkillConfig = obj as SkillConfig;

                // 如果配置文件为null，清理所有轨道数据和UI
                if (SkillEditorData.CurrentSkillConfig == null)
                {
                    Debug.Log("配置文件设置为null,清理所有轨道数据");
                    trackManager.ClearAllTracks();
                }

                SkillEditorEvent.TriggerSkillConfigChanged(SkillEditorData.CurrentSkillConfig);
                SkillEditorEvent.TriggerRefreshRequested();
            });

            // 动画帧率输入框
            CreateFrameRateInput(parent);

            // 技能图标选择器
            CreateImageSelector(parent, "技能Icon:");

            // 技能名称输入
            CreateInputField(parent, "技能名称:", SkillEditorData.CurrentSkillConfig?.skillName ?? "", value =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    SkillEditorData.CurrentSkillConfig.skillName = value;
                }
            });

            // 技能ID输入
            CreateSkillIdInput(parent);

            // 技能描述输入
            CreateMultilineInputField(parent, "技能描述:", SkillEditorData.CurrentSkillConfig?.description ?? "", value =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    SkillEditorData.CurrentSkillConfig.description = value;
                }
            });
        }

        /// <summary>
        /// 创建帧率输入控件
        /// 带有输入验证和范围限制的帧率设置界面
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateFrameRateInput(VisualElement parent)
        {
            TextField frameRateField = null;
            frameRateField = CreateInputField(parent, "动画帧率:", SkillEditorData.CurrentSkillConfig?.frameRate.ToString() ?? "30", value =>
            {
                if (SkillEditorData.CurrentSkillConfig == null)
                {
                    Debug.LogWarning("当前没有加载的技能配置，无法设置帧率");
                    if (frameRateField != null) frameRateField.value = "30";
                    return;
                }

                if (float.TryParse(value, out float frameRate))
                {
                    frameRate = Mathf.Clamp(frameRate, 1f, 120f);
                    SkillEditorData.CurrentSkillConfig.frameRate = frameRate;
                    if (frameRateField != null) frameRateField.value = frameRate.ToString();
                }
                else
                {
                    Debug.LogWarning($"无效的帧率值: {value}，请输入有效的数字");
                    if (frameRateField != null) frameRateField.value = SkillEditorData.CurrentSkillConfig.frameRate.ToString();
                }
            });
        }

        /// <summary>
        /// 创建技能ID输入控件
        /// 带有输入验证的技能ID设置界面
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateSkillIdInput(VisualElement parent)
        {
            CreateInputField(parent, "技能ID:", SkillEditorData.CurrentSkillConfig?.skillId.ToString() ?? "", value =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    if (int.TryParse(value, out int skillId))
                    {
                        SkillEditorData.CurrentSkillConfig.skillId = skillId;
                    }
                    else
                    {
                        Debug.LogWarning($"无效的技能ID值: {value}，请输入有效的整数");
                    }
                }
            });
        }

        /// <summary>
        /// 创建全局控制按钮组
        /// 包含隐藏面板、刷新视图、保存配置、添加配置、删除配置等功能按钮
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateGlobalControlButtons(VisualElement parent)
        {
            // 隐藏全局控制区域按钮
            CreateButton(parent, "隐藏全局设置区域", "", () =>
            {
                SkillEditorEvent.TriggerGlobalControlToggled(false);
            });

            // 重置预览数据
            CreateButton(parent, "重置预览数据", "", () =>
            {
                // 触发预览数据刷新事件，让预览器处理器重新加载所有预览数据
                SkillEditorEvent.TriggerRefreshPreviewDataRequested();
            });

            // 刷新视图按钮
            CreateButton(parent, "刷新视图", "", () =>
            {
                SkillEditorEvent.TriggerRefreshRequested();
            });

            // 保存配置文件按钮
            CreateButton(parent, "保存配置文件", "", () =>
            {
                AssetDatabase.SaveAssetIfDirty(SkillEditorData.CurrentSkillConfig);
                Debug.Log($"<color=green>已保存技能配置: {SkillEditorData.CurrentSkillConfig?.skillName}</color>");
            });

            // 添加配置按钮
            CreateAddConfigButton(parent);

            // 删除配置文件按钮
            var deleteButton = CreateButton(parent, "删除配置文件", "", () =>
            {
                // TODO: 实现删除逻辑
            });
            deleteButton.style.backgroundColor = Color.red;
        }

        #endregion

        #region 轨道控制区域

        /// <summary>
        /// 刷新轨道内容区域
        /// 更新轨道宽度并强制刷新滚动视图显示
        /// </summary>
        /// <param name="trackContent">轨道内容容器</param>
        public void RefreshTrackContent(VisualElement trackContent)
        {
            if (trackContent == null) return;

            float newWidth = SkillEditorData.CalculateTimelineWidth();
            UpdateTrackContentWidth(trackContent, newWidth);

            var scrollView = trackContent.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                ForceUpdateScrollView(scrollView);
            }
        }

        /// <summary>
        /// 更新轨道内容区域的宽度
        /// 根据时间轴长度设置轨道容器的宽度和最小宽度
        /// </summary>
        /// <param name="trackContent">轨道内容容器</param>
        /// <param name="width">新的宽度值</param>
        private void UpdateTrackContentWidth(VisualElement trackContent, float width)
        {
            trackContent.style.width = width;
            trackContent.style.minWidth = width;
        }

        /// <summary>
        /// 创建轨道控制按钮区域
        /// 包含播放控制按钮和帧输入区域
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateTrackControlButtons(VisualElement parent)
        {
            var buttonContent = CreateAndAddElement(parent, "TrackItemControlButtonContent");

            // 播放控制按钮组
            CreatePlaybackControls(buttonContent);

            // 帧输入区域
            CreateFrameInputArea(buttonContent);
        }

        /// <summary>
        /// 创建播放控制按钮组
        /// 包含上一帧、播放/暂停、下一帧、循环播放等控制按钮
        /// 集成动画预览功能
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreatePlaybackControls(VisualElement parent)
        {
            // 停止/重置按钮
            CreateUnityIconButton(parent, "d_Animation.FirstKey", "停止并重置到第0帧", () =>
            {
                SkillEditorData.IsPlaying = false;
                SkillEditorData.SetCurrentFrame(0);
                SkillEditorEvent.TriggerPlayStateChanged(false);
            });

            // 上一帧按钮
            CreateUnityIconButton(parent, "d_Animation.PrevKey", "上一帧", () =>
            {
                int newFrame = Mathf.Max(0, SkillEditorData.CurrentFrame - 1);
                SkillEditorData.SetCurrentFrame(newFrame);
            });

            // 播放/暂停按钮 - 通过事件系统通信
            var playButton = CreateUnityIconButton(parent, SkillEditorData.IsPlaying ? "d_PauseButton@2x" : "d_Animation.Play", "播放/暂停", null);

            // 订阅播放状态变更事件来更新按钮外观
            SkillEditorEvent.OnPlayStateChanged += (isPlaying) =>
            {
                if (playButton != null)
                {
                    string iconName = isPlaying ? "d_PauseButton@2x" : "d_Animation.Play";
                    string tooltip = isPlaying ? "暂停" : "播放";
                    playButton.style.backgroundImage = UnityEditor.EditorGUIUtility.IconContent(iconName).image as Texture2D;
                    playButton.tooltip = tooltip;
                }
            };

            playButton.clicked += () =>
            {
                // 切换播放状态
                SkillEditorData.IsPlaying = !SkillEditorData.IsPlaying;

                // 通过事件系统通知预览器管理器处理播放状态变化
                SkillEditorEvent.TriggerPlayStateChanged(SkillEditorData.IsPlaying);
            };

            // 下一帧按钮
            CreateUnityIconButton(parent, "d_Animation.NextKey", "下一帧", () =>
            {
                int maxFrame = SkillEditorData.CurrentSkillConfig?.maxFrames ?? SkillEditorData.MaxFrame;
                int newFrame = Mathf.Min(maxFrame, SkillEditorData.CurrentFrame + 1);
                SkillEditorData.SetCurrentFrame(newFrame);
            });

            // 循环播放按钮 - 使用颜色变化代替图标
            var loopButton = new Button();
            loopButton.AddToClassList("TrackItemControlButton");
            loopButton.style.backgroundImage = UnityEditor.EditorGUIUtility.IconContent("d_preAudioLoopOff@2x").image as Texture2D;
            loopButton.tooltip = "循环播放";

            // 设置初始颜色状态
            loopButton.style.unityBackgroundImageTintColor = SkillEditorData.IsLoop ? Color.yellow : Color.white;

            loopButton.clicked += () =>
            {
                SkillEditorData.IsLoop = !SkillEditorData.IsLoop;

                // 更新按钮颜色
                loopButton.style.unityBackgroundImageTintColor = SkillEditorData.IsLoop ? Color.yellow : Color.white;
            };

            parent.Add(loopButton);
        }

        /// <summary>
        /// 创建帧输入区域
        /// 包含当前帧输入框和最大帧输入框，支持动态更新
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateFrameInputArea(VisualElement parent)
        {
            var frameInputContent = new VisualElement();
            frameInputContent.AddToClassList("FrameInputContent");
            parent.Add(frameInputContent);

            // 当前帧输入
            var currentFrameInput = CreateIntegerField("当前帧", SkillEditorData.CurrentFrame, (value) =>
            {
                SkillEditorData.SetCurrentFrame(value);
            });
            currentFrameInput.name = "current-frame-input";
            frameInputContent.Add(currentFrameInput);

            // 分隔符
            var separator = new Label("|");
            separator.AddToClassList("SeparatorLabel");
            frameInputContent.Add(separator);

            // 最大帧输入框创建和事件订阅
            CreateMaxFrameInput(frameInputContent);
        }

        /// <summary>
        /// 创建最大帧输入控件
        /// 根据技能配置动态显示最大帧数，并支持修改
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateMaxFrameInput(VisualElement parent)
        {
            IntegerField maxFrameInput = null;
            int maxFrameValue = (SkillEditorData.CurrentSkillConfig != null && SkillEditorData.CurrentSkillConfig.maxFrames > 0)
                ? SkillEditorData.CurrentSkillConfig.maxFrames
                : SkillEditorData.MaxFrame;

            maxFrameInput = CreateIntegerField("最大帧", maxFrameValue, (value) =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    SkillEditorData.CurrentSkillConfig.maxFrames = value;
                    SkillEditorEvent.TriggerMaxFrameChanged(value);
                    SkillEditorEvent.TriggerRefreshRequested();
                }
            });
            parent.Add(maxFrameInput);

            // 订阅配置切换事件以更新显示
            SkillEditorEvent.OnSkillConfigChanged += (config) =>
            {
                if (maxFrameInput != null)
                {
                    maxFrameInput.value = (config != null && config.maxFrames > 0) ? config.maxFrames : SkillEditorData.MaxFrame;
                }
            };
        }

        /// <summary>
        /// 创建搜索和添加轨道区域
        /// 包含轨道搜索输入框和添加轨道按钮
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateSearchAndAddArea(VisualElement parent)
        {
            var searchAddArea = CreateAndAddElement(parent, "SearchOrAddTrack");

            // 搜索输入框
            CreateSearchInput(searchAddArea);

            // 添加轨道按钮
            CreateAddTrackButton(searchAddArea);
        }

        /// <summary>
        /// 创建轨道搜索输入框
        /// 提供轨道搜索功能的输入界面
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateSearchInput(VisualElement parent)
        {
            var searchInput = new TextField();
            searchInput.AddToClassList("SearchTrackInput");
            searchInput.tooltip = "搜索轨道";

            // 调整文本元素边距以适应搜索图标
            var textElement = searchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null) textElement.style.marginLeft = 18;

            // 添加搜索图标
            var searchIcon = new Image();
            searchIcon.AddToClassList("SearchTrackInputIcon");
            searchInput.Add(searchIcon);
            parent.Add(searchInput);
        }

        /// <summary>
        /// 创建添加轨道按钮
        /// 点击后显示轨道类型选择菜单
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateAddTrackButton(VisualElement parent)
        {
            var addButton = new Button();
            addButton.AddToClassList("AddTrackButton");
            addButton.clicked += () => trackManager.ShowTrackCreationMenu(addButton);
            parent.Add(addButton);
        }

        #endregion

        #region 刷新事件处理

        /// <summary>
        /// 处理刷新请求事件
        /// 委托给轨道管理器处理轨道相关的刷新逻辑
        /// </summary>
        public void OnRefreshRequested()
        {
            trackManager.OnRefreshRequested();
        }

        /// <summary>
        /// 根据技能配置创建轨道
        /// 委托给轨道管理器处理
        /// </summary>
        public void CreateTracksFromConfig()
        {
            trackManager.CreateTracksFromConfigPublic();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建并添加带有指定样式类的UI元素
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="className">要应用的CSS样式类名</param>
        /// <returns>创建的UI元素</returns>
        private VisualElement CreateAndAddElement(VisualElement parent, string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        /// <summary>
        /// 创建简单的滚动视图
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="className">滚动视图样式类名</param>
        /// <returns>创建的滚动视图</returns>
        private ScrollView CreateScrollView(VisualElement parent, string className)
        {
            var scrollView = new ScrollView();
            scrollView.AddToClassList(className);
            SetScrollViewVisibility(scrollView, false, false);
            parent.Add(scrollView);
            return scrollView;
        }

        /// <summary>
        /// 设置滚动视图的滚动条可见性
        /// </summary>
        /// <param name="scrollView">要设置的滚动视图</param>
        /// <param name="horizontal">水平滚动条是否可见</param>
        /// <param name="vertical">垂直滚动条是否可见</param>
        private void SetScrollViewVisibility(ScrollView scrollView, bool horizontal, bool vertical)
        {
            scrollView.horizontalScrollerVisibility = horizontal ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = vertical ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        }

        /// <summary>
        /// 强制更新滚动视图显示
        /// 通过临时切换滚动条可见性来刷新滚动视图的布局
        /// </summary>
        /// <param name="scrollView">要更新的滚动视图</param>
        private void ForceUpdateScrollView(ScrollView scrollView)
        {
            scrollView.MarkDirtyRepaint();
            scrollView.contentContainer.MarkDirtyRepaint();

            EditorApplication.delayCall += () =>
            {
                if (scrollView != null)
                {
                    var originalVisibility = scrollView.horizontalScrollerVisibility;
                    scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

                    EditorApplication.delayCall += () =>
                    {
                        if (scrollView != null)
                        {
                            scrollView.horizontalScrollerVisibility = originalVisibility;
                        }
                    };
                }
            };
        }

        #endregion

        #region 通用UI创建方法

        /// <summary>
        /// 创建简单按钮
        /// </summary>
        /// <param name="text">按钮显示文本</param>
        /// <param name="tooltip">按钮提示文本</param>
        /// <param name="onClick">点击事件处理</param>
        /// <returns>创建的按钮</returns>
        public Button CreateButton(string text, string tooltip = "", Action onClick = null)
        {
            var button = new Button();
            button.text = text;
            button.tooltip = tooltip;
            if (onClick != null) button.clicked += onClick;
            return button;
        }

        /// <summary>
        /// 创建带容器的按钮
        /// 自动为按钮添加包装容器和样式
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="text">按钮显示文本</param>
        /// <param name="tooltip">按钮提示文本</param>
        /// <param name="onClick">点击事件处理</param>
        /// <returns>创建的按钮</returns>
        public Button CreateButton(VisualElement parent, string text, string tooltip = "", Action onClick = null)
        {
            var buttonContent = CreateButton(text, tooltip, onClick);
            buttonContent.AddToClassList("ControlButton");
            parent.Add(buttonContent);
            // Icon
            Label icon = new Label();
            icon.AddToClassList("ControlButtonButtonIcon");
            buttonContent.Add(icon);
            // name
            return buttonContent;
        }

        /// <summary>
        /// 创建Unity内置图标按钮
        /// 使用Unity内置图标创建按钮
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="iconName">Unity内置图标名称</param>
        /// <param name="tooltip">按钮提示文本</param>
        /// <param name="onClick">点击事件处理</param>
        /// <returns>创建的图标按钮</returns>
        public Button CreateUnityIconButton(VisualElement parent, string iconName, string tooltip = "", Action onClick = null)
        {
            var button = new Button();
            button.AddToClassList("TrackItemControlButton");
            button.tooltip = tooltip;
            button.style.backgroundImage = UnityEditor.EditorGUIUtility.IconContent(iconName).image as Texture2D;
            if (onClick != null) button.clicked += onClick;

            parent.Add(button);
            return button;
        }

        /// <summary>
        /// 创建对象字段
        /// 用于选择Unity对象的输入字段
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="labelText">标签文本</param>
        /// <param name="objectType">对象类型</param>
        /// <param name="currentValue">当前值</param>
        /// <param name="onValueChanged">值变化事件处理</param>
        /// <returns>创建的对象字段</returns>
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

        /// <summary>
        /// 创建文本输入字段
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="labelText">标签文本</param>
        /// <param name="currentValue">当前值</param>
        /// <param name="onValueChanged">值变化事件处理</param>
        /// <returns>创建的文本输入字段</returns>
        public TextField CreateInputField(VisualElement parent, string labelText, string currentValue = "", Action<string> onValueChanged = null)
        {
            var container = CreateLabeledContainer(parent, labelText);

            TextField inputField = new TextField();
            inputField.AddToClassList("Field");
            inputField.value = currentValue;
            inputField.tooltip = labelText;
            inputField.style.fontSize = 10;

            if (onValueChanged != null)
            {
                inputField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            container.Add(inputField);
            return inputField;
        }

        /// <summary>
        /// 创建多行文本输入字段
        /// 用于输入较长的文本内容，如描述信息
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="labelText">标签文本</param>
        /// <param name="currentValue">当前值</param>
        /// <param name="onValueChanged">值变化事件处理</param>
        /// <returns>创建的多行文本输入字段</returns>
        public TextField CreateMultilineInputField(VisualElement parent, string labelText, string currentValue = "", Action<string> onValueChanged = null)
        {
            var inputField = CreateInputField(parent, labelText, currentValue, onValueChanged);
            inputField.multiline = true;
            inputField.style.whiteSpace = WhiteSpace.Normal;
            inputField.style.fontSize = 10;
            inputField.style.height = 60;
            return inputField;
        }

        /// <summary>
        /// 创建整数输入字段
        /// 专门用于输入整数值，如帧数
        /// </summary>
        /// <param name="tooltip">提示文本</param>
        /// <param name="currentValue">当前值</param>
        /// <param name="onValueChanged">值变化事件处理</param>
        /// <returns>创建的整数输入字段</returns>
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

        /// <summary>
        /// 创建图片选择器
        /// 用于选择和显示技能图标等图片资源
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="labelText">标签文本</param>
        public void CreateImageSelector(VisualElement parent, string labelText)
        {
            var container = CreateLabeledContainer(parent, labelText);

            // 创建图片显示区域
            Image skillIcon = new Image();
            skillIcon.AddToClassList("SkillIcon");
            container.Add(skillIcon);

            // 如果当前技能配置有图标，显示它
            if (SkillEditorData.CurrentSkillConfig?.skillIcon != null)
            {
                skillIcon.image = SkillEditorData.CurrentSkillConfig.skillIcon.texture;
            }

            // 创建选择按钮
            var selectButton = new Button();
            selectButton.AddToClassList("SelectIcon");
            selectButton.tooltip = "选择技能Icon";
            selectButton.clicked += () => OpenImageSelector(skillIcon);
            skillIcon.Add(selectButton);
        }

        /// <summary>
        /// 创建带标签和内容的容器
        /// 用于创建标准的标签-内容布局结构
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="labelText">标签文本</param>
        /// <returns>创建的内容容器</returns>
        private VisualElement CreateLabeledContainer(VisualElement parent, string labelText)
        {
            var container = new VisualElement();
            container.AddToClassList("LabeledContainerContent");

            var label = new Label(labelText);
            label.AddToClassList("LabeledContainerTitle");
            container.Add(label);

            parent.Add(container);
            return container;
        }

        /// <summary>
        /// 创建滚动视图及其内容容器
        /// 返回滚动视图和内容容器的元组
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="scrollViewClass">滚动视图样式类</param>
        /// <param name="contentClass">内容容器样式类</param>
        /// <returns>滚动视图和内容容器的元组</returns>
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

        #region 特殊功能UI创建

        /// <summary>
        /// 打开图片选择器对话框
        /// 允许用户选择项目中的图片资源作为技能图标
        /// </summary>
        /// <param name="targetImage">目标图片显示元素</param>
        private void OpenImageSelector(Image targetImage)
        {
            // 使用EditorUtility.OpenFilePanel打开文件选择对话框
            string imagePath = EditorUtility.OpenFilePanel(
                "选择技能图标",
                "Assets",
                "png,jpg,jpeg,tga,psd,tiff,gif,bmp");

            if (!string.IsNullOrEmpty(imagePath))
            {
                // 将绝对路径转换为相对于Assets的路径
                string relativePath = GetRelativeAssetPath(imagePath);

                if (!string.IsNullOrEmpty(relativePath))
                {
                    // 加载选中的图片资源
                    var selectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                    if (selectedSprite != null)
                    {
                        // 更新UI显示
                        targetImage.image = selectedSprite.texture;

                        // 保存到技能配置
                        if (SkillEditorData.CurrentSkillConfig != null)
                        {
                            SkillEditorData.CurrentSkillConfig.skillIcon = selectedSprite;
                            Debug.Log($"技能图标已设置为: {selectedSprite.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载图片资源: {relativePath}");
                    }
                }
                else
                {
                    Debug.LogWarning("选择的文件不在Assets文件夹内请选择项目内的图片资源");
                }
            }
        }

        /// <summary>
        /// 获取文件的相对Assets路径
        /// 将绝对路径转换为Unity可识别的Assets相对路径
        /// </summary>
        /// <param name="absolutePath">文件的绝对路径</param>
        /// <returns>相对于Assets的路径，如果文件不在Assets文件夹内则返回null</returns>
        private string GetRelativeAssetPath(string absolutePath)
        {
            // 获取项目Assets文件夹的绝对路径
            string assetsPath = Application.dataPath;

            // 检查选择的文件是否在Assets文件夹内
            if (absolutePath.StartsWith(assetsPath))
            {
                // 转换为相对路径（相对于Assets父目录）
                string relativePath = "Assets" + absolutePath.Substring(assetsPath.Length);
                return relativePath.Replace('\\', '/'); // 统一使用正斜杠
            }

            return null;
        }

        /// <summary>
        /// 创建添加配置按钮
        /// 支持内联编辑模式的配置文件创建按钮
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreateAddConfigButton(VisualElement parent)
        {
            var addButton = new Button();
            addButton.AddToClassList("ControlButton");
            addButton.style.flexDirection = FlexDirection.Row;
            addButton.text = "创建配置文件";
            addButton.clicked += () => HandleAddConfigClick(addButton);
            // Icon
            Label icon = new Label();
            icon.AddToClassList("ControlButtonButtonIcon");
            addButton.Add(icon);

            parent.Add(addButton);
        }

        /// <summary>
        /// 处理添加配置按钮点击事件
        /// 将按钮转换为内联编辑模式，包含输入框和确认/取消按钮
        /// </summary>
        /// <param name="addButton">添加配置按钮</param>
        private void HandleAddConfigClick(Button addButton)
        {
            addButton.text = "";
            if (addButton.Q<TextField>() != null) return;

            // 创建配置名称输入框
            var configName = new TextField();
            configName.AddToClassList("AddConfigInput");
            configName.tooltip = "请输入SkillConfig名称,回车确认,ESC取消";
            configName.value = "";
            addButton.Add(configName);

            // 定义确认和取消的处理方法
            System.Action confirmAction = () =>
            {
                if (!string.IsNullOrWhiteSpace(configName.value))
                {
                    Debug.Log($"创建配置文件: {configName.value}");
                    // TODO: 实现实际的配置文件创建逻辑
                }
                ResetAddConfigButton(addButton, configName);
            };

            System.Action cancelAction = () =>
            {
                ResetAddConfigButton(addButton, configName);
            };

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

        /// <summary>
        /// 重置添加配置按钮状态
        /// 清除内联编辑元素并恢复按钮原始状态
        /// </summary>
        /// <param name="addButton">添加配置按钮</param>
        /// <param name="configName">配置名称输入框</param>
        private void ResetAddConfigButton(Button addButton, TextField configName)
        {
            addButton.Remove(configName);
            addButton.text = "创建配置文件";
        }

        #endregion
    }
    #endregion
}
