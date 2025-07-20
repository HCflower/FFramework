using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 滚动同步管理器
    /// 负责协调轨道控制区和轨道内容区的滚动同步，以及时间轴偏移更新
    /// </summary>
    public class SkillEditorScrollSync
    {
        #region 私有字段

        /// <summary>轨道控制区滚动视图</summary>
        private ScrollView trackControlScrollView;

        /// <summary>轨道内容区滚动视图</summary>
        private ScrollView trackScrollView;

        /// <summary>防止无限循环的标志位</summary>
        private bool isUpdatingScroll = false;

        /// <summary>时间轴管理器引用，用于同步滚动偏移</summary>
        private SkillEditorTimeline timelineManager;

        /// <summary>轨道水平滚动变化回调事件</summary>
        public System.Action<float> OnTrackScrollXChanged;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置滚动同步
        /// 初始化两个滚动视图之间的同步机制，包括滚动事件监听和滚动条同步
        /// </summary>
        /// <param name="controlScrollView">轨道控制区的滚动视图</param>
        /// <param name="contentScrollView">轨道内容区的滚动视图</param>
        /// <param name="timelineManager">时间轴管理器，用于同步时间轴偏移</param>
        public void SetupScrollSync(ScrollView controlScrollView, ScrollView contentScrollView, SkillEditorTimeline timelineManager)
        {
            trackControlScrollView = controlScrollView;
            trackScrollView = contentScrollView;
            this.timelineManager = timelineManager;

            if (trackControlScrollView == null || trackScrollView == null) return;

            RegisterScrollEvents();
            SetupScrollbarSync();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 注册滚动事件监听
        /// 为两个滚动视图注册鼠标滚轮事件和几何变化事件，实现垂直滚动同步和水平滚动监听
        /// </summary>
        private void RegisterScrollEvents()
        {
            // 监听轨道控制区的滚轮事件，同步垂直滚动到轨道内容区
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

            // 监听轨道内容区的滚轮事件，同步垂直滚动到轨道控制区
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

            // 监听轨道内容区的几何变化和滚动变化
            if (trackScrollView != null)
            {
                trackScrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewChanged);

                // 监听内容容器的几何变化以更新时间轴偏移
                trackScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    UpdateTimelineOffset();
                });
            }
        }

        /// <summary>
        /// 滚动视图几何变化事件处理
        /// 当滚动视图布局发生变化时更新时间轴偏移
        /// </summary>
        /// <param name="evt">几何变化事件参数</param>
        private void OnScrollViewChanged(GeometryChangedEvent evt)
        {
            UpdateTimelineOffset();
        }

        /// <summary>
        /// 更新时间轴偏移
        /// 根据轨道内容区的水平滚动位置更新时间轴的显示偏移，同时触发回调事件
        /// </summary>
        private void UpdateTimelineOffset()
        {
            if (trackScrollView == null) return;

            float currentOffsetX = trackScrollView.scrollOffset.x;

            // 更新时间轴管理器的偏移
            timelineManager?.UpdateScrollOffset(currentOffsetX);

            // 触发水平滚动变化事件
            OnTrackScrollXChanged?.Invoke(currentOffsetX);
        }

        /// <summary>
        /// 设置滚动条同步
        /// 配置水平和垂直滚动条的同步机制，包括滚动条值变化监听和可见性同步
        /// </summary>
        private void SetupScrollbarSync()
        {
            if (trackControlScrollView == null || trackScrollView == null) return;

            // 设置水平滚动条监听，同步时间轴偏移
            if (trackScrollView.horizontalScroller != null)
            {
                trackScrollView.horizontalScroller.valueChanged += (newValue) =>
                {
                    if (!isUpdatingScroll)
                    {
                        timelineManager?.UpdateScrollOffset(newValue);
                        OnTrackScrollXChanged?.Invoke(newValue);
                    }
                };
            }

            // 设置垂直滚动条同步 - 轨道控制区到轨道内容区
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

            // 设置垂直滚动条同步 - 轨道内容区到轨道控制区
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

            // 同步滚动条可见性，为轨道控制区添加底部边框以对齐水平滚动条
            SyncScrollbarVisibility();
        }

        /// <summary>
        /// 同步滚动条可见性
        /// 当轨道内容区显示水平滚动条时，为轨道控制区添加相应的底部间距以保持视觉对齐
        /// </summary>
        private void SyncScrollbarVisibility()
        {
            if (trackScrollView?.horizontalScrollerVisibility != ScrollerVisibility.Hidden)
            {
                trackControlScrollView.style.borderBottomWidth = 13;
                trackControlScrollView.style.borderBottomColor = new UnityEngine.Color(0.0f, 0.0f, 0.0f, 0.0f);
            }
        }

        /// <summary>
        /// 清理滚动同步
        /// 移除所有事件监听器并重置引用，防止内存泄漏
        /// </summary>
        public void Cleanup()
        {
            OnTrackScrollXChanged = null;
            trackControlScrollView = null;
            trackScrollView = null;
            timelineManager = null;
            isUpdatingScroll = false;
        }

        #endregion
    }
}