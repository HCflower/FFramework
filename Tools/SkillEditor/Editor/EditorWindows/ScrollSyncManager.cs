using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 滚动同步管理器
    /// </summary>
    public class ScrollSyncManager
    {
        private ScrollView trackControlScrollView;
        private ScrollView trackScrollView;
        private bool isUpdatingScroll = false;

        public void SetupScrollSync(ScrollView controlScrollView, ScrollView contentScrollView)
        {
            trackControlScrollView = controlScrollView;
            trackScrollView = contentScrollView;

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
        }

        private void SetupScrollbarSync()
        {
            // 尝试多种方式查找滚动条
            var trackControlVerticalScroller = trackControlScrollView.Q<Scroller>(className: "unity-scroller--vertical")
                                              ?? trackControlScrollView.Q<Scroller>("VerticalScroller")
                                              ?? trackControlScrollView.verticalScroller;

            var trackVerticalScroller = trackScrollView.Q<Scroller>(className: "unity-scroller--vertical")
                                       ?? trackScrollView.Q<Scroller>("VerticalScroller")
                                       ?? trackScrollView.verticalScroller;

            UnityEngine.Debug.Log($"Scrollers found - Control: {trackControlVerticalScroller != null}, Content: {trackVerticalScroller != null}");

            if (trackControlVerticalScroller != null)
            {
                trackControlVerticalScroller.valueChanged += (newValue) =>
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

            if (trackVerticalScroller != null)
            {
                trackVerticalScroller.valueChanged += (newValue) =>
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