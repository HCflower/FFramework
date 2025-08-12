using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 时间轴管理器
    /// 负责管理技能编辑器的时间轴显示，包括刻度绘制、帧指示器更新和用户交互处理
    /// </summary>
    public class SkillEditorTimeline
    {
        #region 私有字段

        /// <summary>时间轴容器元素</summary>
        private VisualElement timelineContainer;

        /// <summary>时间轴IMGUI元素，用于绘制刻度和接收鼠标事件</summary>
        private VisualElement timelineIMGUI;

        /// <summary>轨道内容容器的引用，用于同步宽度</summary>
        private VisualElement trackContent;

        /// <summary>技能配置显示字段</summary>
        private ObjectField skillConfigField;

        /// <summary>技能使用者显示字段</summary>
        private ObjectField skillOwnerField;

        /// <summary>技能持续时间显示标签</summary>
        private Label durationLable;

        /// <summary>预览器处理器引用</summary>
        private SkillEditorPreviewerHandler previewerHandler;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，初始化时间轴管理器
        /// </summary>
        public SkillEditorTimeline()
        {
            // 订阅技能配置变更事件
            SkillEditorEvent.OnSkillConfigChanged += UpdateTipsDisplay;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化时间轴
        /// 设置时间轴容器并创建基本的UI元素结构
        /// </summary>
        /// <param name="trackArea">轨道区域容器</param>
        public void InitializeTimeline(VisualElement trackArea)
        {
            timelineContainer = trackArea;
            EnsureCorrectElementOrder();
            DrawCurrentFrameIndicator();

            // 初始化后刷新提示显示
            RefreshTipsDisplay();
        }

        /// <summary>
        /// 设置轨道内容容器的引用
        /// 用于在时间轴缩放时同步更新轨道内容的宽度
        /// </summary>
        /// <param name="trackContentElement">轨道内容容器元素</param>
        public void SetTrackContent(VisualElement trackContentElement)
        {
            trackContent = trackContentElement;
        }

        /// <summary>
        /// 设置预览器处理器引用
        /// </summary>
        /// <param name="handler">预览器处理器实例</param>
        public void SetPreviewerHandler(SkillEditorPreviewerHandler handler)
        {
            previewerHandler = handler;
        }

        /// <summary>
        /// 获取预览器处理器
        /// </summary>
        /// <returns>预览器处理器实例</returns>
        public SkillEditorPreviewerHandler GetPreviewerHandler()
        {
            return previewerHandler;
        }

        /// <summary>
        /// 获取动画预览管理器
        /// </summary>
        /// <returns>动画预览管理器实例</returns>
        public SkillAnimationPreviewer GetAnimationPreviewer()
        {
            return previewerHandler?.AnimationPreviewer;
        }

        /// <summary>
        /// 获取Transform预览管理器
        /// </summary>
        /// <returns>Transform预览管理器实例</returns>
        public SkillTransformPreviewer GetTransformPreviewer()
        {
            return previewerHandler?.TransformPreviewer;
        }

        /// <summary>
        /// 刷新时间轴显示
        /// 重新计算时间轴宽度并更新刻度显示和帧指示器位置
        /// </summary>
        public void RefreshTimeline()
        {
            if (timelineIMGUI == null) return;

            float newWidth = SkillEditorData.CalculateTimelineWidth() + 10;
            timelineIMGUI.style.width = newWidth;

            // 同步更新轨道内容宽度
            if (trackContent != null)
            {
                trackContent.style.width = newWidth;
            }

            DrawTimelineScale();
            UpdateFrameIndicatorPosition();
        }

        /// <summary>
        /// 更新当前帧
        /// 设置新的当前帧数并更新帧指示器位置
        /// </summary>
        /// <param name="frame">新的当前帧数</param>
        public void UpdateCurrentFrame(int frame)
        {
            SkillEditorData.SetCurrentFrame(frame);
            UpdateFrameIndicatorPosition();
        }

        /// <summary>
        /// 更新最大帧数
        /// 设置新的最大帧数并重新计算时间轴宽度和刻度显示
        /// </summary>
        /// <param name="maxFrame">新的最大帧数</param>
        public void UpdateMaxFrame(int maxFrame)
        {
            SkillEditorData.SetMaxFrame(maxFrame);

            // 强制更新时间轴布局
            if (timelineIMGUI != null)
            {
                float newWidth = SkillEditorData.CalculateTimelineWidth() + 10;
                timelineIMGUI.style.width = newWidth;
                timelineIMGUI.style.minWidth = newWidth;

                // 强制标记需要重新布局
                timelineIMGUI.MarkDirtyRepaint();

                // 延迟绘制确保宽度更新完成
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DrawTimelineScale();
                    UpdateFrameIndicatorPosition();
                };
            }

            // 同步更新轨道内容宽度
            if (trackContent != null)
            {
                float newWidth = SkillEditorData.CalculateTimelineWidth() + 10;
                trackContent.style.width = newWidth;
                trackContent.style.minWidth = newWidth;
            }
        }

        /// <summary>
        /// 设置时间轴的水平偏移
        /// 用于同步时间轴刻度和帧指示器与轨道内容的滚动位置
        /// </summary>
        /// <param name="offsetX">水平偏移量（像素）</param>
        public void SetTimelineOffset(float offsetX)
        {
            SkillEditorData.TrackViewContentOffsetX = offsetX;
            RefreshTimeline();
        }

        /// <summary>
        /// 更新提示区域显示
        /// 根据当前技能配置更新提示区域的配置显示，并更新特效预览器
        /// </summary>
        /// <param name="newConfig">新的技能配置</param>
        public void UpdateTipsDisplay(SkillConfig newConfig)
        {
            if (skillConfigField != null)
            {
                skillConfigField.value = newConfig;
            }
            if (durationLable != null)
            {
                string durationText = newConfig != null ? newConfig.Duration.ToString() : "未加载技能配置";
                durationLable.text = durationText;
            }

            // 更新预览器配置
            previewerHandler?.UpdateSkillConfig(newConfig);
        }

        /// <summary>
        /// 刷新提示区域显示
        /// 重新读取当前配置并更新显示
        /// </summary>
        public void RefreshTipsDisplay()
        {
            if (skillConfigField != null)
            {
                skillConfigField.value = SkillEditorData.CurrentSkillConfig;
            }
            if (durationLable != null)
            {
                string durationText = SkillEditorData.CurrentSkillConfig != null ?
                    SkillEditorData.CurrentSkillConfig.Duration.ToString() : "未加载技能配置";
                durationLable.text = durationText;
            }
        }

        /// <summary>
        /// 更新滚动偏移
        /// 根据新的滚动偏移量重新绘制时间轴刻度和更新帧指示器位置
        /// </summary>
        /// <param name="offsetX">新的滚动偏移量（像素）</param>
        public void UpdateScrollOffset(float offsetX)
        {
            SkillEditorData.TrackViewContentOffsetX = offsetX;
            DrawTimelineScale();
            UpdateFrameIndicatorPosition();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            SkillEditorEvent.OnSkillConfigChanged -= UpdateTipsDisplay;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新技能所属对象
        /// </summary>
        /// <param name="selectedGameObject">选择的游戏对象</param>
        private void UpdateSkillOwner(SkillRuntimeController selectedGameObject)
        {
            // 通过预览器处理器更新技能所有者
            previewerHandler?.UpdateSkillOwner(selectedGameObject);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确保时间轴元素的正确显示顺序
        /// 清理现有元素并按正确顺序重新创建，确保时间轴在最底层，滚动视图在最上层
        /// </summary>
        private void EnsureCorrectElementOrder()
        {
            // 清理现有元素
            var existingTimeline = timelineContainer.Q(className: "TimeLineIMGUI");
            var existingTips = timelineContainer.Q(className: "TipsContent");

            existingTimeline?.RemoveFromHierarchy();
            existingTips?.RemoveFromHierarchy();

            // 按正确顺序重新添加
            CreateTimelineIMGUI();
            CreateTipsContent();

            // 确保滚动视图在最后（如果存在的话，将其移到最后）
            var scrollView = timelineContainer.Q<ScrollView>();
            if (scrollView != null)
            {
                scrollView.RemoveFromHierarchy();
                timelineContainer.Add(scrollView);
            }
        }

        /// <summary>
        /// 创建时间轴IMGUI元素
        /// 初始化时间轴的可视元素，包括样式设置、刻度绘制和鼠标事件注册
        /// </summary>
        private void CreateTimelineIMGUI()
        {
            timelineIMGUI = new VisualElement();
            timelineIMGUI.AddToClassList("TimeLineIMGUI");
            timelineIMGUI.style.width = SkillEditorData.CalculateTimelineWidth() + 10;
            timelineIMGUI.style.position = Position.Relative;
            timelineIMGUI.tooltip = "移动帧: 鼠标左键\n缩放时间轴: ctrl+鼠标中键";
            DrawTimelineScale();
            RegisterTimelineMouseEvents();
            timelineContainer.Add(timelineIMGUI);
        }

        /// <summary>
        ///TODO 创建提示内容元素
        /// 添加用于显示时间轴相关提示信息的标签元素
        /// </summary>
        private void CreateTipsContent()
        {
            var tipsContent = new Label();
            tipsContent.AddToClassList("TipsContent");

            // 当前配置  
            var configTip = TipsContent("当前选择的配置:");
            skillConfigField = TipsObjectField(typeof(SkillConfig), "技能配置请在设置面板中选择", true);
            skillConfigField.value = SkillEditorData.CurrentSkillConfig; // 设置当前配置
            configTip.Add(skillConfigField);
            tipsContent.Add(configTip);

            // 当前技能使用者  
            var ownerTip = TipsContent("技能使用者:");
            skillOwnerField = TipsObjectField(typeof(SkillRuntimeController), "技能使用者");
            skillOwnerField.RegisterValueChangedCallback(evt => UpdateSkillOwner(evt.newValue as SkillRuntimeController));
            ownerTip.Add(skillOwnerField);
            tipsContent.Add(ownerTip);

            // 技能总时长
            var durationTip = TipsContent("技能总时长:");
            string durationText = SkillEditorData.CurrentSkillConfig != null ?
                SkillEditorData.CurrentSkillConfig.Duration.ToString() : "未加载技能配置";
            durationLable = TipsTextField(durationText, "技能总时长");
            durationTip.Add(durationLable);
            tipsContent.Add(durationTip);

            timelineContainer.Add(tipsContent);
        }

        private VisualElement TipsContent(string titleName)
        {
            // Tip区域
            VisualElement tipContent = new VisualElement();
            tipContent.AddToClassList("TipContent");
            // Tip标题
            Label title = new Label(titleName);
            title.AddToClassList("TipTitle");
            tipContent.Add(title);

            return tipContent;
        }

        private ObjectField TipsObjectField(Type type, string tooltip, bool isShowOnly = false)
        {
            // 技能使用对象
            ObjectField ownerObjectField = new ObjectField();
            ownerObjectField.objectType = type;
            ownerObjectField.AddToClassList("TipObject");
            ownerObjectField.tooltip = tooltip;
            if (isShowOnly)
            {
                ownerObjectField.style.display = DisplayStyle.Flex;
                ownerObjectField.SetEnabled(false);
            }
            return ownerObjectField;
        }

        private Label TipsTextField(string titleName, string tooltip)
        {
            Label textLabel = new Label();
            textLabel.text = titleName;
            textLabel.AddToClassList("TipText");
            textLabel.tooltip = tooltip;
            return textLabel;
        }

        /// <summary>
        /// 绘制时间轴刻度
        /// 根据当前滚动偏移和缩放比例绘制可见区域内的时间刻度和标签
        /// </summary>
        private void DrawTimelineScale()
        {
            if (timelineIMGUI == null) return;

            timelineIMGUI.Clear();

            // 获取时间轴容器的可视宽度
            float visibleWidth = timelineIMGUI.resolvedStyle.width;
            if (visibleWidth <= 0)
            {
                visibleWidth = SkillEditorData.CalculateTimelineWidth() + 10;
                if (visibleWidth <= 0) visibleWidth = 800;
            }

            // 计算可见区域的帧范围
            float scrollOffset = SkillEditorData.TrackViewContentOffsetX;
            float frameWidth = SkillEditorData.FrameUnitWidth;

            // 向前多计算一帧，确保边界帧不丢失
            int startFrame = Mathf.Max(0, Mathf.FloorToInt((scrollOffset - frameWidth) / frameWidth));
            // 结束帧始终为MaxFrame，确保所有刻度都绘制
            int endFrame = SkillEditorData.MaxFrame;

            // 绘制可见区域的刻度
            DrawVisibleScaleTicks(startFrame, endFrame);
        }

        /// <summary>
        /// 绘制指定范围内的可见刻度
        /// 在指定的帧范围内绘制刻度线和标签，根据重要程度决定显示样式
        /// </summary>
        /// <param name="startFrame">开始帧</param>
        /// <param name="endFrame">结束帧</param>
        private void DrawVisibleScaleTicks(int startFrame, int endFrame)
        {
            // 使用内容宽度，确保所有帧刻度都能显示
            float timelineWidth = Mathf.Max(timelineIMGUI.resolvedStyle.width, SkillEditorData.CalculateTimelineWidth() + 10);
            if (timelineWidth <= 0)
            {
                timelineWidth = 810f;
            }

            float frameWidth = SkillEditorData.FrameUnitWidth;
            float scrollOffset = SkillEditorData.TrackViewContentOffsetX;

            // 统一处理所有帧，包括0帧
            for (int frame = startFrame; frame <= endFrame; frame++)
            {
                // 计算刻度的实际位置
                float framePosition = frame * frameWidth;
                float adjustedPosition = framePosition - scrollOffset;

                // 检查是否在可视范围内
                if (IsTickPositionVisible(adjustedPosition, timelineWidth))
                {
                    bool isMajorTick = frame % SkillEditorData.MajorTickInterval == 0;
                    bool isMaxFrame = frame == SkillEditorData.MaxFrame;

                    CreateTickLine(adjustedPosition, isMajorTick);

                    // 绘制标签条件：主要刻度、或0帧
                    if (isMajorTick || frame == 0)
                    {
                        CreateTickLabel(adjustedPosition, frame);
                    }
                }
            }
        }

        /// <summary>
        /// 检查刻度位置是否在可视范围内
        /// 使用容差值确保边界处的刻度能正确显示
        /// </summary>
        /// <param name="position">刻度位置</param>
        /// <param name="timelineWidth">时间轴宽度</param>
        /// <returns>是否在可视范围内</returns>
        private bool IsTickPositionVisible(float position, float timelineWidth)
        {
            // 添加容差，特别是对于放大情况
            float tolerance = SkillEditorData.FrameUnitWidth * 0.1f; // 10%的帧宽度作为容差

            // 允许稍微超出边界的刻度也被显示
            return position >= -tolerance && position <= timelineWidth + tolerance;
        }


        /// <summary>
        /// 创建刻度线元素
        /// 在指定位置创建垂直的刻度线，根据是否为主刻度应用不同的样式
        /// </summary>
        /// <param name="xPosition">刻度线的X位置</param>
        /// <param name="isMajorTick">是否为主要刻度</param>
        private void CreateTickLine(float xPosition, bool isMajorTick)
        {
            var tickLine = new VisualElement();
            tickLine.AddToClassList("Timeline-tick");
            if (isMajorTick) tickLine.AddToClassList("major");
            tickLine.style.left = xPosition;
            timelineIMGUI.Add(tickLine);
        }

        /// <summary>
        /// 创建刻度标签
        /// 在指定位置创建显示帧数的文本标签
        /// </summary>
        /// <param name="xPosition">标签的X位置</param>
        /// <param name="frameIndex">要显示的帧数</param>
        private void CreateTickLabel(float xPosition, int frameIndex)
        {
            var tickLabel = new Label(frameIndex.ToString());
            tickLabel.AddToClassList("Tick-label");
            tickLabel.style.left = xPosition - 9; // 减去标签偏移以居中显示
            timelineIMGUI.Add(tickLabel);
        }

        /// <summary>
        /// 注册时间轴鼠标事件
        /// 为时间轴元素注册鼠标点击、拖拽、滚轮缩放等交互事件
        /// </summary>
        private void RegisterTimelineMouseEvents()
        {
            if (timelineIMGUI == null) return;

            bool isDragging = false;

            // 鼠标按下事件 - 开始帧选择
            timelineIMGUI.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    isDragging = true;
                    timelineIMGUI.CaptureMouse();
                    HandleTimelineClick(evt.localMousePosition);
                    evt.StopPropagation();
                }
            });

            // 鼠标移动事件 - 拖拽时连续更新帧
            timelineIMGUI.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    HandleTimelineClick(evt.localMousePosition);
                    evt.StopPropagation();
                }
            });

            // 鼠标释放事件 - 结束帧选择
            timelineIMGUI.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0 && isDragging)
                {
                    isDragging = false;
                    timelineIMGUI.ReleaseMouse();
                    evt.StopPropagation();
                }
            });

            // 鼠标离开事件 - 防止拖拽状态残留
            timelineIMGUI.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    timelineIMGUI.ReleaseMouse();
                }
            });

            // 滚轮事件 - Ctrl+滚轮进行缩放
            timelineIMGUI.RegisterCallback<WheelEvent>(evt =>
            {
                if (evt.ctrlKey)
                {
                    float zoomDelta = evt.delta.y > 0 ? -2f : 2f;
                    SkillEditorData.SetFrameUnitWidth(SkillEditorData.FrameUnitWidth + zoomDelta);

                    // 触发缩放事件，让外部统一处理刷新
                    SkillEditorEvent.TriggerTimelineZoomChanged();

                    evt.StopPropagation();
                }
            });
        }

        /// <summary>
        /// 处理时间轴点击事件
        /// 根据鼠标位置计算对应的帧数并更新当前帧
        /// </summary>
        /// <param name="localMousePosition">鼠标在时间轴元素中的本地位置</param>
        private void HandleTimelineClick(Vector2 localMousePosition)
        {
            float mouseX = localMousePosition.x;
            // 加上滚动偏移量，得到在完整时间轴上的实际位置
            float actualTimelineX = mouseX + SkillEditorData.TrackViewContentOffsetX;
            float exactFrame = actualTimelineX / SkillEditorData.FrameUnitWidth;
            int nearestFrame = Mathf.RoundToInt(exactFrame);
            nearestFrame = Mathf.Clamp(nearestFrame, 0, SkillEditorData.MaxFrame);

            SkillEditorData.SetCurrentFrame(nearestFrame);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 绘制当前帧指示器
        /// 创建或更新显示当前帧位置的可视指示器
        /// </summary>
        private void DrawCurrentFrameIndicator()
        {
            if (SkillEditorData.CurrentFrame < 0 || SkillEditorData.CurrentFrame > SkillEditorData.MaxFrame) return;

            // 移除现有指示器
            var existingIndicator = timelineContainer?.Q("Current-frame-indicator");
            existingIndicator?.RemoveFromHierarchy();
  
            // 创建新指示器
            var indicator = new VisualElement();
            indicator.name = "Current-frame-indicator";
            indicator.AddToClassList("Current-frame-indicator");
            indicator.pickingMode = PickingMode.Ignore;
            // 创建新指示器Icon
            var indicatorIcon = new Label();
            indicatorIcon.AddToClassList("Current-frame-indicator-icon");
            indicatorIcon.pickingMode = PickingMode.Ignore;
            indicator.Add(indicatorIcon);
            // 计算指示器位置（考虑滚动偏移）
            float xPosition = SkillEditorData.CurrentFrame * SkillEditorData.FrameUnitWidth;
            indicator.style.left = xPosition - SkillEditorData.TrackViewContentOffsetX - 1;

            timelineContainer.Add(indicator);
        }

        /// <summary>
        /// 更新帧指示器位置
        /// 根据当前帧数和滚动偏移更新帧指示器的显示位置和可见性
        /// </summary>
        private void UpdateFrameIndicatorPosition()
        {
            var indicator = timelineContainer?.Q("Current-frame-indicator");
            if (indicator == null) return;

            // 计算指示器位置
            float xPosition = SkillEditorData.CurrentFrame * SkillEditorData.FrameUnitWidth;
            float adjustedPosition = xPosition - SkillEditorData.TrackViewContentOffsetX - 1;

            // 检查指示器是否在可视范围内
            float timelineWidth = timelineIMGUI?.resolvedStyle.width ?? 800f;
            bool isVisible = adjustedPosition >= -2f && adjustedPosition <= timelineWidth + 2f; // 给2像素的容差

            // 更新位置和可见性
            indicator.style.left = adjustedPosition;
            indicator.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion

        #region 预览器管理

        /// <summary>
        /// 刷新特效预览器数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshEffectPreviewerData()
        {
            previewerHandler?.RefreshEffectPreviewerData();
        }

        /// <summary>
        /// 刷新Transform预览器数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshTransformPreviewerData()
        {
            previewerHandler?.RefreshTransformPreviewerData();
        }

        #endregion
    }
}
