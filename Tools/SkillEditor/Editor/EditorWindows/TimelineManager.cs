using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 时间轴管理器
    /// </summary>
    public class TimelineManager
    {
        private readonly SkillEditorDataManager dataManager;
        private readonly EventManager eventManager;
        private VisualElement timelineContainer;
        private VisualElement timelineIMGUI;
        private VisualElement trackContent; // 添加轨道内容引用

        public TimelineManager(SkillEditorDataManager dataManager, EventManager eventManager)
        {
            this.dataManager = dataManager;
            this.eventManager = eventManager;
        }

        public void SetTrackContent(VisualElement trackContentElement)
        {
            trackContent = trackContentElement;
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
                float newWidth = dataManager.CalculateTimelineWidth();
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

        private void CreateTimelineIMGUI()
        {
            timelineIMGUI = new VisualElement();
            timelineIMGUI.AddToClassList("TimeLineIMGUI");
            timelineIMGUI.style.width = dataManager.CalculateTimelineWidth();
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

        private void DrawTimelineScale()
        {
            if (timelineIMGUI == null) return;

            timelineIMGUI.Clear();
            float timelineWidth = dataManager.MaxFrame * dataManager.FrameUnitWidth;

            int startIndex = dataManager.TrackViewContentOffsetX < 1 ? 0 :
                Mathf.CeilToInt(dataManager.TrackViewContentOffsetX / dataManager.FrameUnitWidth);

            float startOffset = 0;
            if (startIndex > 0)
                startOffset = dataManager.FrameUnitWidth - (dataManager.TrackViewContentOffsetX % dataManager.FrameUnitWidth);

            DrawScaleTicks(startOffset, timelineWidth, startIndex);
        }

        private void DrawScaleTicks(float startOffset, float timelineWidth, int startIndex)
        {
            int index = startIndex;
            float maxDisplayWidth = Mathf.Min(timelineWidth, dataManager.MaxFrame * dataManager.FrameUnitWidth);

            for (float i = startOffset; i <= maxDisplayWidth || index <= dataManager.MaxFrame; i += dataManager.FrameUnitWidth)
            {
                if (i > maxDisplayWidth && index < dataManager.MaxFrame)
                {
                    i = dataManager.MaxFrame * dataManager.FrameUnitWidth;
                    index = dataManager.MaxFrame;
                }

                if (index > dataManager.MaxFrame) break;

                bool isMajorTick = index % dataManager.MajorTickInterval == 0;
                CreateTickLine(i, isMajorTick);

                if (isMajorTick || index == dataManager.MaxFrame)
                {
                    CreateTickLabel(i, index);
                }

                index++;
                if (i > maxDisplayWidth && index > dataManager.MaxFrame) break;
            }
        }
        private void CreateTickLine(float xPosition, bool isMajorTick)
        {
            var tickLine = new VisualElement();
            tickLine.AddToClassList("Timeline-tick");
            if (isMajorTick) tickLine.AddToClassList("major");
            tickLine.style.left = xPosition;
            timelineIMGUI.Add(tickLine);
        }

        private void CreateTickLabel(float xPosition, int frameIndex)
        {
            var tickLabel = new Label(frameIndex.ToString());
            tickLabel.AddToClassList("Tick-label");
            tickLabel.style.left = xPosition - 9;
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
                    dataManager.SetFrameUnitWidth(dataManager.FrameUnitWidth + zoomDelta);

                    // 只触发事件，让外部统一处理刷新
                    eventManager.TriggerTimelineZoomChanged();

                    evt.StopPropagation();
                }
            });
        }

        private void HandleTimelineClick(Vector2 localMousePosition)
        {
            float mouseX = localMousePosition.x;
            float exactFrame = mouseX / dataManager.FrameUnitWidth;
            int nearestFrame = Mathf.RoundToInt(exactFrame);
            nearestFrame = Mathf.Clamp(nearestFrame, 0, dataManager.MaxFrame);

            eventManager.TriggerCurrentFrameChanged(nearestFrame);
        }

        public void UpdateCurrentFrame(int frame)
        {
            dataManager.SetCurrentFrame(frame);
            UpdateFrameIndicatorPosition();
        }

        public void UpdateMaxFrame(int maxFrame)
        {
            dataManager.SetMaxFrame(maxFrame);

            // 只刷新时间轴，不触发其他区域的更新
            if (timelineIMGUI != null)
            {
                timelineIMGUI.style.width = dataManager.CalculateTimelineWidth();
                DrawTimelineScale();
                UpdateFrameIndicatorPosition();
            }

            // 同步更新轨道内容宽度（如果已设置）
            if (trackContent != null)
            {
                trackContent.style.width = dataManager.CalculateTimelineWidth();
            }
        }

        private void DrawCurrentFrameIndicator()
        {
            if (dataManager.CurrentFrame < 0 || dataManager.CurrentFrame > dataManager.MaxFrame) return;

            var existingIndicator = timelineContainer?.Q("Current-frame-indicator");
            existingIndicator?.RemoveFromHierarchy();

            var indicator = new VisualElement();
            indicator.name = "Current-frame-indicator";
            indicator.AddToClassList("Current-frame-indicator");

            float xPosition = dataManager.CurrentFrame * dataManager.FrameUnitWidth;
            indicator.style.left = xPosition - 1;

            timelineContainer.Add(indicator);
        }

        private void UpdateFrameIndicatorPosition()
        {
            var indicator = timelineContainer?.Q("Current-frame-indicator");
            if (indicator != null)
            {
                float xPosition = dataManager.CurrentFrame * dataManager.FrameUnitWidth;
                indicator.style.left = xPosition - 1;
            }
        }
    }
}
