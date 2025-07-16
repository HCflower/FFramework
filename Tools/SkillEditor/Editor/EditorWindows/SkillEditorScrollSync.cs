using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 滚动同步管理器
    /// </summary>
    public class SkillEditorScrollSync
    {
        private ScrollView trackControlScrollView;
        private ScrollView trackScrollView;
        private bool isUpdatingScroll = false;
        private SkillEditorTimeline timelineManager;            // 添加时间轴管理器引用
        public System.Action<float> OnTrackScrollXChanged;      //当trackScrollView的X轴滚动发生变化时触发

        public void SetupScrollSync(ScrollView controlScrollView, ScrollView contentScrollView, SkillEditorTimeline timelineManager)
        {
            trackControlScrollView = controlScrollView;
            trackScrollView = contentScrollView;
            this.timelineManager = timelineManager;

            if (trackControlScrollView == null || trackScrollView == null) return;

            RegisterScrollEvents();
            SetupScrollbarSync();
        }

        private void RegisterScrollEvents()
        {
            trackControlScrollView.RegisterCallback<WheelEvent>(evt =>
            {
                if (!isUpdatingScroll && !evt.ctrlKey)
                {
                    isUpdatingScroll = true;
                    trackScrollView.scrollOffset = new UnityEngine.Vector2(
                        trackScrollView.scrollOffset.x,
                        trackControlScrollView.scrollOffset.y
                    );
                    isUpdatingScroll = false;
                }
            });

            trackScrollView.RegisterCallback<WheelEvent>(evt =>
            {
                if (!isUpdatingScroll && !evt.ctrlKey)
                {
                    isUpdatingScroll = true;
                    trackControlScrollView.scrollOffset = new UnityEngine.Vector2(
                        trackControlScrollView.scrollOffset.x,
                        trackScrollView.scrollOffset.y
                    );
                    isUpdatingScroll = false;
                }
            });

            // 添加水平滚动监听
            if (trackScrollView != null)
            {
                trackScrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewChanged);

                // 监听滚动事件
                trackScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    UpdateTimelineOffset();
                });
            }
        }

        private void OnScrollViewChanged(GeometryChangedEvent evt)
        {
            UpdateTimelineOffset();
        }

        private void UpdateTimelineOffset()
        {
            if (trackScrollView != null && timelineManager != null)
            {
                float currentOffsetX = trackScrollView.scrollOffset.x;
                timelineManager.UpdateScrollOffset(currentOffsetX);
            }
        }

        private void SetupScrollbarSync()
        {
            if (trackControlScrollView != null && trackScrollView != null)
            {
                // 设置水平滚动条监听
                if (trackScrollView.horizontalScroller != null)
                {
                    trackScrollView.horizontalScroller.valueChanged += (newValue) =>
                    {
                        if (!isUpdatingScroll && timelineManager != null)
                        {
                            timelineManager.UpdateScrollOffset(newValue);
                        }
                    };
                }

                // 设置垂直滚动条同步
                if (trackControlScrollView.verticalScroller != null)
                {
                    trackControlScrollView.verticalScroller.valueChanged += (newValue) =>
                    {
                        if (!isUpdatingScroll)
                        {
                            isUpdatingScroll = true;
                            trackScrollView.scrollOffset = new UnityEngine.Vector2(
                                trackScrollView.scrollOffset.x,
                                newValue
                            );
                            isUpdatingScroll = false;
                        }
                    };
                }

                if (trackScrollView.verticalScroller != null)
                {
                    trackScrollView.verticalScroller.valueChanged += (newValue) =>
                    {
                        if (!isUpdatingScroll)
                        {
                            isUpdatingScroll = true;
                            trackControlScrollView.scrollOffset = new UnityEngine.Vector2(
                                trackControlScrollView.scrollOffset.x,
                                newValue
                            );
                            isUpdatingScroll = false;
                        }
                    };
                }

                // 同步滚动条可见性
                if (trackScrollView.horizontalScrollerVisibility != ScrollerVisibility.Hidden)
                {
                    trackControlScrollView.style.borderBottomWidth = 13;
                    trackControlScrollView.style.borderBottomColor = new UnityEngine.Color(0.0f, 0.0f, 0.0f, 0.0f);
                }
            }
        }
    }
}