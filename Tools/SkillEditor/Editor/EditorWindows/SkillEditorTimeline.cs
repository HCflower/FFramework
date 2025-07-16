using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 时间轴管理器
    /// </summary>
    public class SkillEditorTimeline
    {
        private readonly SkillEditorData skillEditorData;
        private readonly SkillEditorEvent skillEditorEvent;
        private VisualElement timelineContainer;
        private VisualElement timelineIMGUI;
        private VisualElement trackContent; // 添加轨道内容引用

        public SkillEditorTimeline(SkillEditorData skillEditorData, SkillEditorEvent skillEditorEvent)
        {
            this.skillEditorData = skillEditorData;
            this.skillEditorEvent = skillEditorEvent;
        }

        public void InitializeTimeline(VisualElement trackArea)
        {
            timelineContainer = trackArea;
            EnsureCorrectElementOrder();
            DrawCurrentFrameIndicator();
        }

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

        public void RefreshTimeline()
        {
            if (timelineIMGUI != null)
            {
                float newWidth = skillEditorData.CalculateTimelineWidth();
                timelineIMGUI.style.width = newWidth;

                // 同步更新轨道内容宽度
                if (trackContent != null)
                {
                    trackContent.style.width = newWidth;
                }

                DrawTimelineScale();
                UpdateFrameIndicatorPosition();
            }
        }

        // 添加新方法：应用滚动偏移
        private void ApplyScrollOffset()
        {
            if (timelineIMGUI != null)
            {
                timelineIMGUI.style.left = -skillEditorData.TrackViewContentOffsetX;
            }
        }

        private void CreateTimelineIMGUI()
        {
            timelineIMGUI = new VisualElement();
            timelineIMGUI.AddToClassList("TimeLineIMGUI");
            timelineIMGUI.style.width = skillEditorData.CalculateTimelineWidth();
            timelineIMGUI.style.position = Position.Relative;

            DrawTimelineScale();
            RegisterTimelineMouseEvents();
            timelineContainer.Add(timelineIMGUI);
        }

        private void CreateTipsContent()
        {
            var tipsContent = new Label();
            tipsContent.AddToClassList("TipsContent");
            timelineContainer.Add(tipsContent);
        }

        public void SetTrackContent(VisualElement trackContentElement)
        {
            trackContent = trackContentElement;
        }

        private void DrawTimelineScale()
        {
            if (timelineIMGUI == null) return;

            timelineIMGUI.Clear();

            // 获取时间轴容器的可视宽度
            float visibleWidth = timelineIMGUI.resolvedStyle.width;
            if (visibleWidth <= 0)
            {
                visibleWidth = skillEditorData.CalculateTimelineWidth();
                if (visibleWidth <= 0) visibleWidth = 800;
            }

            // 修复：更宽松的起始和结束帧计算
            float scrollOffset = skillEditorData.TrackViewContentOffsetX;
            float frameWidth = skillEditorData.FrameUnitWidth;

            // 向前多计算一帧，确保边界帧不丢失
            int startFrame = Mathf.Max(0, Mathf.FloorToInt((scrollOffset - frameWidth) / frameWidth));

            // 结束帧始终为MaxFrame，确保所有刻度都绘制
            int endFrame = skillEditorData.MaxFrame;

            // 绘制可见区域的刻度
            DrawVisibleScaleTicks(startFrame, endFrame);
        }


        private void DrawVisibleScaleTicks(int startFrame, int endFrame)
        {
            // timelineWidth始终用内容宽度，确保所有帧刻度都能显示
            float timelineWidth = Mathf.Max(timelineIMGUI.resolvedStyle.width, skillEditorData.CalculateTimelineWidth());
            if (timelineWidth <= 0)
            {
                timelineWidth = 800f;
            }

            float frameWidth = skillEditorData.FrameUnitWidth;
            float scrollOffset = skillEditorData.TrackViewContentOffsetX;

            // 修复：统一处理所有帧，包括0帧
            for (int frame = startFrame; frame <= endFrame; frame++)
            {
                // 计算刻度的实际位置
                float framePosition = frame * frameWidth;
                float adjustedPosition = framePosition - scrollOffset;

                // 检查是否在可视范围内
                if (IsTickPositionVisible(adjustedPosition, timelineWidth))
                {
                    bool isMajorTick = frame % skillEditorData.MajorTickInterval == 0;
                    bool isMaxFrame = frame == skillEditorData.MaxFrame;

                    CreateTickLine(adjustedPosition, isMajorTick);

                    // 绘制标签条件：主要刻度、最大帧、或0帧
                    if (isMajorTick || isMaxFrame || frame == 0)
                    {
                        CreateTickLabel(adjustedPosition, frame);
                    }
                }
            }
        }

        private bool IsTickPositionVisible(float position, float timelineWidth)
        {
            // 修复：添加容差，特别是对于放大情况
            float tolerance = skillEditorData.FrameUnitWidth * 0.1f; // 10%的帧宽度作为容差

            // 允许稍微超出边界的刻度也被显示
            return position >= -tolerance && position <= timelineWidth + tolerance;
        }


        private void CreateTickLine(float xPosition, bool isMajorTick)
        {
            var tickLine = new VisualElement();
            tickLine.AddToClassList("Timeline-tick");
            if (isMajorTick) tickLine.AddToClassList("major");
            tickLine.style.left = xPosition; // 直接使用已经应用偏移的位置
            timelineIMGUI.Add(tickLine);
        }

        private void CreateTickLabel(float xPosition, int frameIndex)
        {
            var tickLabel = new Label(frameIndex.ToString());
            tickLabel.AddToClassList("Tick-label");
            tickLabel.style.left = xPosition - 9; // 直接使用已经应用偏移的位置，减去标签偏移
            timelineIMGUI.Add(tickLabel);
        }

        private void RegisterTimelineMouseEvents()
        {
            if (timelineIMGUI == null) return;

            bool isDragging = false;

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

            timelineIMGUI.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    HandleTimelineClick(evt.localMousePosition);
                    evt.StopPropagation();
                }
            });

            timelineIMGUI.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0 && isDragging)
                {
                    isDragging = false;
                    timelineIMGUI.ReleaseMouse();
                    evt.StopPropagation();
                }
            });

            timelineIMGUI.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    timelineIMGUI.ReleaseMouse();
                }
            });

            timelineIMGUI.RegisterCallback<WheelEvent>(evt =>
            {
                if (evt.ctrlKey)
                {
                    float zoomDelta = evt.delta.y > 0 ? -2f : 2f;
                    skillEditorData.SetFrameUnitWidth(skillEditorData.FrameUnitWidth + zoomDelta);

                    // 只触发事件，让外部统一处理刷新
                    skillEditorEvent.TriggerTimelineZoomChanged();

                    evt.StopPropagation();
                }
            });
        }

        private void HandleTimelineClick(Vector2 localMousePosition)
        {
            float mouseX = localMousePosition.x;
            // 加上滚动偏移量，得到在完整时间轴上的实际位置
            float actualTimelineX = mouseX + skillEditorData.TrackViewContentOffsetX;
            float exactFrame = actualTimelineX / skillEditorData.FrameUnitWidth;
            int nearestFrame = Mathf.RoundToInt(exactFrame);
            nearestFrame = Mathf.Clamp(nearestFrame, 0, skillEditorData.MaxFrame);

            skillEditorEvent.TriggerCurrentFrameChanged(nearestFrame);
        }

        public void UpdateCurrentFrame(int frame)
        {
            skillEditorData.SetCurrentFrame(frame);
            UpdateFrameIndicatorPosition();
        }

        public void UpdateMaxFrame(int maxFrame)
        {
            skillEditorData.SetMaxFrame(maxFrame);

            // 强制更新时间轴布局
            if (timelineIMGUI != null)
            {
                float newWidth = skillEditorData.CalculateTimelineWidth();
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
                float newWidth = skillEditorData.CalculateTimelineWidth();
                trackContent.style.width = newWidth;
                trackContent.style.minWidth = newWidth;
            }
        }

        /// <summary>
        /// 设置时间轴的X轴偏移（用于同步刻度和帧指示器位置）
        /// </summary>
        public void SetTimelineOffset(float offsetX)
        {
            if (skillEditorData != null)
            {
                skillEditorData.TrackViewContentOffsetX = offsetX;
                RefreshTimeline();
            }
        }

        public void UpdateScrollOffset(float offsetX)
        {
            skillEditorData.TrackViewContentOffsetX = offsetX;
            // 重新绘制刻度以反映新的偏移
            DrawTimelineScale();
            UpdateFrameIndicatorPosition();
        }

        private void DrawCurrentFrameIndicator()
        {
            if (skillEditorData.CurrentFrame < 0 || skillEditorData.CurrentFrame > skillEditorData.MaxFrame) return;

            var existingIndicator = timelineContainer?.Q("Current-frame-indicator");
            existingIndicator?.RemoveFromHierarchy();

            var indicator = new VisualElement();
            indicator.name = "Current-frame-indicator";
            indicator.AddToClassList("Current-frame-indicator");

            float xPosition = skillEditorData.CurrentFrame * skillEditorData.FrameUnitWidth;
            // 应用滚动偏移，使指示器显示在正确的视觉位置
            indicator.style.left = xPosition - skillEditorData.TrackViewContentOffsetX - 1;

            timelineContainer.Add(indicator);
        }

        private void UpdateFrameIndicatorPosition()
        {
            var indicator = timelineContainer?.Q("Current-frame-indicator");
            if (indicator != null)
            {
                float xPosition = skillEditorData.CurrentFrame * skillEditorData.FrameUnitWidth;
                // 帧指示器位置需要减去滚动偏移，使其显示在正确的视觉位置
                float adjustedPosition = xPosition - skillEditorData.TrackViewContentOffsetX - 1;

                // 检查指示器是否在可视范围内
                float timelineWidth = timelineIMGUI?.resolvedStyle.width ?? 800f;
                bool isVisible = adjustedPosition >= -2f && adjustedPosition <= timelineWidth + 2f; // 给2像素的容差

                indicator.style.left = adjustedPosition;
                indicator.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
