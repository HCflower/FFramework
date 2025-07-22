using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System.Linq;
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

        /// <summary>技能编辑器事件管理器</summary>
        private readonly SkillEditorEvent skillEditorEvent;

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
        /// <param name="skillEditorEvent">技能编辑器事件管理器实例</param>
        public SkillEditorUIBuilder(SkillEditorEvent skillEditorEvent)
        {
            this.skillEditorEvent = skillEditorEvent;

            // 订阅刷新事件
            if (skillEditorEvent != null)
                skillEditorEvent.OnRefreshRequested += OnRefreshRequested;
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
            var showButton = CreateButton("", "Global Setting", () => skillEditorEvent.TriggerGlobalControlToggled(true));
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
                skillEditorEvent.TriggerSkillConfigChanged(SkillEditorData.CurrentSkillConfig);
                skillEditorEvent.TriggerRefreshRequested();
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
                skillEditorEvent.TriggerGlobalControlToggled(false);
            });

            // 刷新视图按钮
            CreateButton(parent, "刷新视图", "", () =>
            {
                skillEditorEvent.TriggerRefreshRequested();
            });

            // 保存配置文件按钮
            CreateButton(parent, "保存配置文件", "", () =>
            {
                AssetDatabase.SaveAssetIfDirty(SkillEditorData.CurrentSkillConfig);
                Debug.Log($"已保存技能配置: {SkillEditorData.CurrentSkillConfig?.skillName}");
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
        /// </summary>
        /// <param name="parent">父容器</param>
        private void CreatePlaybackControls(VisualElement parent)
        {
            // 上一帧按钮
            CreateIconButton(parent, "d_Animation.PrevKey", "上一帧", () =>
            {
                skillEditorEvent.TriggerCurrentFrameChanged(SkillEditorData.CurrentFrame - 1);
            });

            // 播放/暂停按钮
            CreateIconButton(parent, SkillEditorData.IsPlaying ? "d_PauseButton@2x" : "d_Animation.Play", "播放", () =>
            {
                SkillEditorData.IsPlaying = !SkillEditorData.IsPlaying;
                skillEditorEvent.TriggerPlayStateChanged(SkillEditorData.IsPlaying);
            });

            // 下一帧按钮
            CreateIconButton(parent, "d_Animation.NextKey", "下一帧", () =>
            {
                skillEditorEvent.TriggerCurrentFrameChanged(SkillEditorData.CurrentFrame + 1);
            });

            // 循环播放按钮
            CreateIconButton(parent, "d_preAudioLoopOff@2x", "循环播放", () =>
            {
                SkillEditorData.IsLoop = !SkillEditorData.IsLoop;
            });
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
                skillEditorEvent.TriggerCurrentFrameChanged(value);
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
                    skillEditorEvent.TriggerMaxFrameChanged(value);
                    skillEditorEvent.TriggerRefreshRequested();
                }
            });
            parent.Add(maxFrameInput);

            // 订阅配置切换事件以更新显示
            skillEditorEvent.OnSkillConfigChanged += (config) =>
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
            addButton.clicked += () => ShowTrackCreationMenu(addButton);
            parent.Add(addButton);
        }

        #endregion

        #region 轨道创建和管理

        /// <summary>
        /// 显示轨道创建菜单
        /// 根据轨道类型限制显示可创建的轨道类型选项
        /// </summary>
        /// <param name="button">触发菜单的按钮元素</param>
        private void ShowTrackCreationMenu(VisualElement button)
        {
            var menu = new GenericMenu();

            // 检查是否已存在动画轨道（限制只能有一个）
            bool hasAnimationTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.AnimationTrack);

            // 创建动画轨道菜单项
            menu.AddItem(new GUIContent("创建 Animation Track"), hasAnimationTrack, () =>
            {
                if (!hasAnimationTrack)
                    CreateTrack(TrackType.AnimationTrack);
                else
                    Debug.LogWarning("(动画轨道只可存在一条)已存在动画轨道，无法创建新的动画轨道。");
            });

            // 创建其他类型轨道菜单项
            menu.AddItem(new GUIContent("创建 Audio Track"), false, () => CreateTrack(TrackType.AudioTrack));
            menu.AddItem(new GUIContent("创建 Effect Track"), false, () => CreateTrack(TrackType.EffectTrack));
            menu.AddItem(new GUIContent("创建 Attack Track"), false, () => CreateTrack(TrackType.AttackTrack));
            menu.AddItem(new GUIContent("创建 Event Track"), false, () => CreateTrack(TrackType.EventTrack));

            // 在按钮下方显示菜单
            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        /// <summary>
        /// 创建指定类型的轨道
        /// 创建轨道控制器和轨道内容，并订阅相关事件
        /// </summary>
        /// <param name="trackType">要创建的轨道类型</param>
        private void CreateTrack(TrackType trackType)
        {
            string trackName = $"{trackType}";
            Debug.Log($"CreateTrack: 创建轨道 {trackType}");

            // 创建轨道控制器和轨道内容
            var trackControl = new SkillEditorTrackControl(trackControlContent, trackType, trackName);
            var track = new SkillEditorTrack(allTrackContent, trackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);

            // 创建轨道信息并添加到数据中
            var trackInfo = new SkillEditorTrackInfo(trackControl, track, trackType, trackName);
            SkillEditorData.tracks.Add(trackInfo);
            Debug.Log($"CreateTrack: 轨道 {trackType} 创建成功，当前轨道总数: {SkillEditorData.tracks.Count}");

            // 订阅轨道事件
            SubscribeTrackEvents(trackControl);

            // 如果配置文件中有对应轨道的数据，自动创建轨道项
            Debug.Log($"CreateTrack: 准备为轨道 {trackType} 创建轨道项");
            CreateTrackItemsFromConfig(track, trackType);
        }

        /// <summary>
        /// 根据技能配置自动创建所有包含数据的轨道
        /// </summary>
        public void CreateTracksFromConfig()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null)
            {
                Debug.Log("CreateTracksFromConfig: 没有技能配置或轨道容器");
                return;
            }

            Debug.Log($"CreateTracksFromConfig: 开始检查配置 {skillConfig.skillName}");

            // 检查动画轨道
            if (HasAnimationTrackData(skillConfig) && !HasTrackType(TrackType.AnimationTrack))
            {
                Debug.Log("CreateTracksFromConfig: 创建动画轨道");
                CreateTrack(TrackType.AnimationTrack);
            }

            // 检查音频轨道
            if (HasAudioTrackData(skillConfig) && !HasTrackType(TrackType.AudioTrack))
            {
                CreateTrack(TrackType.AudioTrack);
            }

            // 检查特效轨道
            if (HasEffectTrackData(skillConfig) && !HasTrackType(TrackType.EffectTrack))
            {
                CreateTrack(TrackType.EffectTrack);
            }

            // 检查伤害检测轨道
            if (HasInjuryDetectionTrackData(skillConfig) && !HasTrackType(TrackType.AttackTrack))
            {
                CreateTrack(TrackType.AttackTrack);
            }

            // 检查事件轨道
            if (HasEventTrackData(skillConfig) && !HasTrackType(TrackType.EventTrack))
            {
                CreateTrack(TrackType.EventTrack);
            }
        }

        /// <summary>
        /// 检查是否已存在指定类型的轨道
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        private bool HasTrackType(TrackType trackType)
        {
            return SkillEditorData.tracks.Any(t => t.TrackType == trackType);
        }

        /// <summary>
        /// 根据配置数据为指定轨道创建轨道项
        /// </summary>
        /// <param name="track">轨道实例</param>
        /// <param name="trackType">轨道类型</param>
        private void CreateTrackItemsFromConfig(SkillEditorTrack track, TrackType trackType)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer == null)
            {
                Debug.Log($"CreateTrackItemsFromConfig: 没有技能配置或轨道容器，轨道类型: {trackType}");
                return;
            }

            Debug.Log($"CreateTrackItemsFromConfig: 为轨道类型 {trackType} 创建轨道项");

            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    CreateAnimationTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.AudioTrack:
                    CreateAudioTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.EffectTrack:
                    CreateEffectTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.AttackTrack:
                    CreateInjuryDetectionTrackItemsFromConfig(track, skillConfig);
                    break;
                case TrackType.EventTrack:
                    CreateEventTrackItemsFromConfig(track, skillConfig);
                    break;
            }
        }

        #region 轨道数据检查方法

        /// <summary>
        /// 检查动画轨道是否有数据
        /// </summary>
        private bool HasAnimationTrackData(SkillConfig skillConfig)
        {
            bool hasData = skillConfig.trackContainer.animationTrack != null &&
                   skillConfig.trackContainer.animationTrack.animationClips != null &&
                   skillConfig.trackContainer.animationTrack.animationClips.Count > 0;

            Debug.Log($"HasAnimationTrackData: {hasData}");
            if (hasData)
            {
                Debug.Log($"HasAnimationTrackData: 找到 {skillConfig.trackContainer.animationTrack.animationClips.Count} 个动画片段");
            }

            return hasData;
        }

        /// <summary>
        /// 检查音频轨道是否有数据
        /// </summary>
        private bool HasAudioTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.audioTracks != null &&
                   skillConfig.trackContainer.audioTracks.Count > 0 &&
                   skillConfig.trackContainer.audioTracks.Any(track => track.audioClips != null && track.audioClips.Count > 0);
        }

        /// <summary>
        /// 检查特效轨道是否有数据
        /// </summary>
        private bool HasEffectTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.effectTracks != null &&
                   skillConfig.trackContainer.effectTracks.Count > 0 &&
                   skillConfig.trackContainer.effectTracks.Any(track => track.effectClips != null && track.effectClips.Count > 0);
        }

        /// <summary>
        /// 检查伤害检测轨道是否有数据
        /// </summary>
        private bool HasInjuryDetectionTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.injuryDetectionTracks != null &&
                   skillConfig.trackContainer.injuryDetectionTracks.Count > 0 &&
                   skillConfig.trackContainer.injuryDetectionTracks.Any(track => track.injuryDetectionClips != null && track.injuryDetectionClips.Count > 0);
        }

        /// <summary>
        /// 检查事件轨道是否有数据
        /// </summary>
        private bool HasEventTrackData(SkillConfig skillConfig)
        {
            return skillConfig.trackContainer.eventTracks != null &&
                   skillConfig.trackContainer.eventTracks.Count > 0 &&
                   skillConfig.trackContainer.eventTracks.Any(track => track.eventClips != null && track.eventClips.Count > 0);
        }

        #endregion

        #region 轨道项创建方法

        /// <summary>
        /// 从配置创建动画轨道项
        /// </summary>
        private void CreateAnimationTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var animationTrack = skillConfig.trackContainer.animationTrack;
            if (animationTrack?.animationClips == null)
            {
                Debug.Log("CreateAnimationTrackItemsFromConfig: 没有动画片段数据");
                return;
            }

            Debug.Log($"CreateAnimationTrackItemsFromConfig: 找到 {animationTrack.animationClips.Count} 个动画片段");

            foreach (var clip in animationTrack.animationClips.ToList())
            {
                if (clip.clip != null)
                {
                    Debug.Log($"CreateAnimationTrackItemsFromConfig: 创建轨道项 {clip.clip.name} 起始帧 {clip.startFrame}");
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    track.AddTrackItem(clip.clip, clip.startFrame, false);
                }
                else
                {
                    Debug.LogWarning($"CreateAnimationTrackItemsFromConfig: 片段数据中 clip 为空");
                }
            }
        }

        /// <summary>
        /// 从配置创建音频轨道项
        /// </summary>
        private void CreateAudioTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var audioTracks = skillConfig.trackContainer.audioTracks;
            if (audioTracks == null) return;

            foreach (var audioTrack in audioTracks)
            {
                if (audioTrack.audioClips != null)
                {
                    foreach (var clip in audioTrack.audioClips)
                    {
                        if (clip.clip != null)
                        {
                            // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                            track.AddTrackItem(clip.clip, clip.startFrame, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从配置创建特效轨道项
        /// </summary>
        private void CreateEffectTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var effectTracks = skillConfig.trackContainer.effectTracks;
            if (effectTracks == null) return;

            foreach (var effectTrack in effectTracks)
            {
                if (effectTrack.effectClips != null)
                {
                    foreach (var clip in effectTrack.effectClips)
                    {
                        if (clip.effectPrefab != null)
                        {
                            // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                            track.AddTrackItem(clip.effectPrefab, clip.startFrame, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从配置创建伤害检测轨道项
        /// </summary>
        private void CreateInjuryDetectionTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var injuryTracks = skillConfig.trackContainer.injuryDetectionTracks;
            if (injuryTracks == null) return;

            foreach (var injuryTrack in injuryTracks)
            {
                if (injuryTrack.injuryDetectionClips != null)
                {
                    foreach (var clip in injuryTrack.injuryDetectionClips)
                    {
                        // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                        track.AddTrackItem(clip.clipName, clip.startFrame, false);
                    }
                }
            }
        }

        /// <summary>
        /// 从配置创建事件轨道项
        /// </summary>
        private void CreateEventTrackItemsFromConfig(SkillEditorTrack track, SkillConfig skillConfig)
        {
            var eventTracks = skillConfig.trackContainer.eventTracks;
            if (eventTracks == null) return;

            foreach (var eventTrack in eventTracks)
            {
                if (eventTrack.eventClips != null)
                {
                    foreach (var clip in eventTrack.eventClips)
                    {
                        // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                        track.AddTrackItem(clip.clipName, clip.startFrame, false);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 处理刷新请求事件
        /// 清空现有轨道UI并重新创建所有轨道
        /// </summary>
        public void OnRefreshRequested()
        {
            Debug.Log("OnRefreshRequested: 开始刷新");

            // 清空现有轨道UI
            trackControlContent?.Clear();
            allTrackContent?.Clear();

            // 重新创建所有轨道UI（包含轨道项）
            RefreshAllTracks();

            // 根据配置创建配置中存在但UI中还没有的轨道
            Debug.Log("OnRefreshRequested: 准备从配置创建新轨道");
            CreateTracksFromConfig();

            Debug.Log("OnRefreshRequested: 刷新完成");
        }

        /// <summary>
        /// 刷新所有轨道UI
        /// 重新创建所有轨道的UI元素，优先创建动画轨道
        /// </summary>
        private void RefreshAllTracks()
        {
            if (SkillEditorData.tracks == null) return;

            Debug.Log($"RefreshAllTracks: 开始重建 {SkillEditorData.tracks.Count} 个现有轨道");

            // 保存现有轨道信息并清空列表
            var tracksToRecreate = new List<SkillEditorTrackInfo>(SkillEditorData.tracks);
            SkillEditorData.tracks.Clear();

            // 优先创建动画轨道
            RecreateTracksByType(tracksToRecreate, TrackType.AnimationTrack);

            // 创建其他类型轨道
            RecreateTracksByType(tracksToRecreate, t => t != TrackType.AnimationTrack);

            Debug.Log($"RefreshAllTracks: 完成重建，当前轨道数量: {SkillEditorData.tracks.Count}");
        }

        /// <summary>
        /// 按类型重新创建轨道
        /// </summary>
        /// <param name="tracksToRecreate">需要重新创建的轨道列表</param>
        /// <param name="targetType">目标轨道类型</param>
        private void RecreateTracksByType(List<SkillEditorTrackInfo> tracksToRecreate, TrackType targetType)
        {
            foreach (var oldTrackInfo in tracksToRecreate.Where(t => t.TrackType == targetType))
            {
                var newTrackControl = new SkillEditorTrackControl(trackControlContent, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                var newTrack = new SkillEditorTrack(allTrackContent, oldTrackInfo.TrackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);
                var newTrackInfo = new SkillEditorTrackInfo(newTrackControl, newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackName);

                SkillEditorData.tracks.Add(newTrackInfo);
                SubscribeTrackEvents(newTrackControl);

                // 重新创建轨道项 - 从当前配置文件加载数据
                Debug.Log($"RecreateTracksByType: 为重建的轨道 {targetType} 创建轨道项");
                CreateTrackItemsFromConfig(newTrack, oldTrackInfo.TrackType);
            }
        }

        /// <summary>
        /// 按类型重新创建轨道（使用谓词筛选）
        /// </summary>
        /// <param name="tracksToRecreate">需要重新创建的轨道列表</param>
        /// <param name="predicate">轨道类型筛选谓词</param>
        private void RecreateTracksByType(List<SkillEditorTrackInfo> tracksToRecreate, Func<TrackType, bool> predicate)
        {
            foreach (var oldTrackInfo in tracksToRecreate.Where(t => predicate(t.TrackType)))
            {
                var newTrackControl = new SkillEditorTrackControl(trackControlContent, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                var newTrack = new SkillEditorTrack(allTrackContent, oldTrackInfo.TrackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);
                var newTrackInfo = new SkillEditorTrackInfo(newTrackControl, newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackName);

                SkillEditorData.tracks.Add(newTrackInfo);
                SubscribeTrackEvents(newTrackControl);

                // 重新创建轨道项 - 从当前配置文件加载数据
                Debug.Log($"RecreateTracksByType: 为重建的轨道 {oldTrackInfo.TrackType} 创建轨道项");
                CreateTrackItemsFromConfig(newTrack, oldTrackInfo.TrackType);
            }
        }

        /// <summary>
        /// 订阅轨道控制器事件
        /// 包含删除轨道、激活状态变化、添加轨道项等事件处理
        /// </summary>
        /// <param name="trackControl">轨道控制器实例</param>
        private void SubscribeTrackEvents(SkillEditorTrackControl trackControl)
        {
            // 订阅轨道删除事件
            trackControl.OnDeleteTrack += HandleTrackDelete;

            // 订阅激活状态变化事件
            trackControl.OnActiveStateChanged += HandleTrackActiveStateChanged;

            // 订阅添加轨道项事件
            trackControl.OnAddTrackItem += HandleAddTrackItem;
        }

        /// <summary>
        /// 处理轨道删除事件
        /// 从数据中移除轨道并触发刷新
        /// </summary>
        /// <param name="ctrl">要删除的轨道控制器</param>
        private void HandleTrackDelete(SkillEditorTrackControl ctrl)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                SkillEditorData.tracks.Remove(info);
                Debug.Log($"删除轨道: {info.TrackName}，剩余轨道数量: {SkillEditorData.tracks.Count}");
                skillEditorEvent?.TriggerRefreshRequested();
            }
        }

        /// <summary>
        /// 处理轨道激活状态变化事件
        /// 更新轨道激活状态并刷新显示
        /// </summary>
        /// <param name="ctrl">轨道控制器</param>
        /// <param name="isActive">新的激活状态</param>
        private void HandleTrackActiveStateChanged(SkillEditorTrackControl ctrl, bool isActive)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                info.IsActive = isActive;
                info.Control.RefreshState(isActive);
                Debug.Log($"轨道[{info.TrackName}]激活状态: {(isActive ? "激活" : "失活")}");
            }
        }

        /// <summary>
        /// 处理添加轨道项事件
        /// 根据轨道类型添加相应的轨道项
        /// </summary>
        /// <param name="ctrl">轨道控制器</param>
        private void HandleAddTrackItem(SkillEditorTrackControl ctrl)
        {
            var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
            if (info != null)
            {
                if (info.TrackType == TrackType.EventTrack)
                {
                    info.Track.AddTrackItem("Event");
                }
                else if (info.TrackType == TrackType.AttackTrack)
                {
                    info.Track.AddTrackItem("Attack");
                }
            }
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
            var controlContent = new VisualElement();
            controlContent.AddToClassList("ControlButtonContent");

            var button = CreateButton(text, tooltip, onClick);
            button.AddToClassList("Field");
            controlContent.Add(button);

            parent.Add(controlContent);
            return button;
        }

        /// <summary>
        /// 创建图标按钮
        /// 使用指定图标路径创建带图标的按钮
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="iconPath">图标资源路径</param>
        /// <param name="tooltip">按钮提示文本</param>
        /// <param name="onClick">点击事件处理</param>
        /// <returns>创建的图标按钮</returns>
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
            configName.tooltip = "请输入SkillConfig名称";
            configName.value = "";
            addButton.Add(configName);

            // 创建确认按钮
            var confirmButton = new Button();
            confirmButton.text = "+";
            confirmButton.style.fontSize = 20;
            confirmButton.AddToClassList("AddConfigButton");
            addButton.Add(confirmButton);

            // 创建取消按钮
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

        /// <summary>
        /// 重置添加配置按钮状态
        /// 清除内联编辑元素并恢复按钮原始状态
        /// </summary>
        /// <param name="addButton">添加配置按钮</param>
        /// <param name="configName">配置名称输入框</param>
        /// <param name="confirmButton">确认按钮</param>
        /// <param name="cancelButton">取消按钮</param>
        private void ResetAddConfigButton(Button addButton, TextField configName, Button confirmButton, Button cancelButton)
        {
            addButton.Remove(configName);
            addButton.Remove(confirmButton);
            addButton.Remove(cancelButton);
            addButton.text = "创建配置文件";
        }

        #endregion
    }
    #endregion
}
