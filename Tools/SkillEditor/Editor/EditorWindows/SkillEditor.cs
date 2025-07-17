using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器 - 重构后的主类
    /// </summary>
    public class SkillEditor : EditorWindow
    {
        #region 管理器
        private SkillEditorData skillEditorData;
        private SkillEditorUIBuilder uiBuilder;
        private SkillEditorTimeline skillEditorTimeline;
        private SkillEditorScrollSync skillEditorScrollSync;
        private SkillEditorEvent skillEditorEvent;
        #endregion

        #region GUI元素
        private VisualElement mainContent;
        private VisualElement globalControlContent;
        private VisualElement allTrackContent;
        private VisualElement trackControlArea;
        private ScrollView trackControlScrollView;
        private VisualElement trackControlContent;
        private VisualElement trackArea;
        private ScrollView trackScrollView;
        private VisualElement trackContent;
        #endregion

        /// <summary>
        /// 技能编辑窗口
        /// </summary>
        [MenuItem("FFramework/⚔️SkillEditor #S", priority = 5)]
        public static void SkillEditorCreateWindow()
        {
            SkillEditor window = GetWindow<SkillEditor>();
            window.minSize = new Vector2(900, 450);
            window.titleContent = new GUIContent("SkillEditor");
            window.Show();
        }

        private void OnEnable()
        {
            InitializeManagers();
            InitializeUI();
        }

        private void OnDisable()
        {
            skillEditorData?.SaveData();
            skillEditorEvent?.Cleanup();
        }

        private void InitializeManagers()
        {
            skillEditorData = new SkillEditorData();
            skillEditorEvent = new SkillEditorEvent();
            skillEditorTimeline = new SkillEditorTimeline(skillEditorData, skillEditorEvent);
            uiBuilder = new SkillEditorUIBuilder(skillEditorData, skillEditorEvent);
            skillEditorScrollSync = new SkillEditorScrollSync();

            // 注册事件
            skillEditorEvent.OnCurrentFrameChanged += OnCurrentFrameChanged;
            skillEditorEvent.OnMaxFrameChanged += OnMaxFrameChanged;
            skillEditorEvent.OnGlobalControlToggled += OnGlobalControlToggled;
            skillEditorEvent.OnRefreshRequested += RefreshView;
            skillEditorEvent.OnTimelineZoomChanged += OnTimelineZoomChanged; // 添加缩放事件处理
        }

        private void InitializeUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorStyle"));
            CreateMainStructure();
            RefreshView();
        }

        private void OnTimelineZoomChanged()
        {
            // 只刷新时间轴，内部会同步轨道内容宽度
            skillEditorTimeline.RefreshTimeline();
            UpdateTrackContentWidth();
            // 延迟刷新滚动条状态，确保布局计算完成
            EditorApplication.delayCall += () =>
            {
                ForceRefreshScrollBar();
            };
        }

        private void UpdateTrackContentWidth()
        {
            if (trackContent != null)
            {
                float newWidth = skillEditorData.CalculateTimelineWidth();
                trackContent.style.width = newWidth;

                // 更新所有轨道元素的宽度
                uiBuilder.RefreshTrackContent(trackContent, skillEditorData);

                // 同步所有轨道宽度
                foreach (var item in skillEditorData.tracks)
                {
                    if (item.Track != null)
                    {
                        item.Track.SetWidth(newWidth);
                    }
                }
            }
        }

        private void CreateMainStructure()
        {
            mainContent = uiBuilder.CreateMainContent(rootVisualElement);
            globalControlContent = uiBuilder.CreateGlobalControlArea(mainContent);
            allTrackContent = uiBuilder.CreateTrackArea(mainContent);

            CreateTrackStructure();
        }

        private void CreateTrackStructure()
        {
            var trackElements = uiBuilder.CreateTrackStructure(allTrackContent, skillEditorData);

            // 修正解包字段名为TrackStructureResult的属性名（首字母大写）
            trackControlArea = trackElements.ControlArea;
            trackControlScrollView = trackElements.ControlScrollView;
            trackControlContent = trackElements.ControlContent;
            trackArea = trackElements.TrackArea;
            trackScrollView = trackElements.TrackScrollView;
            trackContent = trackElements.TrackContent;

            // 设置trackScrollView水平滚动条自动显示/隐藏
            if (trackScrollView != null)
            {
                trackScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            }

            // 初始化时间轴
            skillEditorTimeline.InitializeTimeline(trackArea);

            // 设置轨道内容引用，以便时间轴缩放时同步更新
            skillEditorTimeline.SetTrackContent(trackContent);

            // 初始同步轨道内容宽度和所有轨道宽度
            UpdateTrackContentWidth();

            // 设置滚动同步
            skillEditorScrollSync.SetupScrollSync(trackControlScrollView, trackScrollView, skillEditorTimeline);
            // 注册X轴滚动回调，实现时间轴刻度和帧指示器同步偏移
            skillEditorScrollSync.OnTrackScrollXChanged = (offsetX) =>
            {
                skillEditorTimeline.SetTimelineOffset(offsetX);
            };
        }

        private void RefreshView()
        {
            uiBuilder.RefreshGlobalControl(globalControlContent, skillEditorData);
            skillEditorTimeline.RefreshTimeline();
        }

        #region 事件处理

        private void OnCurrentFrameChanged(int newFrame)
        {
            skillEditorTimeline.UpdateCurrentFrame(newFrame);

            // 更新帧输入框显示
            UpdateFrameInputDisplay(newFrame);
        }

        private void UpdateTimelineAndTrackContent()
        {
            skillEditorTimeline.RefreshTimeline();

            if (trackContent != null)
            {
                float newWidth = skillEditorData.CalculateTimelineWidth();
                trackContent.style.width = newWidth;
                trackContent.style.minWidth = newWidth;
                uiBuilder.RefreshTrackContent(trackContent, skillEditorData);

                // 延迟检查滚动条状态
                CheckAndForceScrollBar(newWidth);
            }
        }

        private void CheckAndForceScrollBar(float contentWidth)
        {
            if (trackScrollView == null) return;

            // 获取轨道区域的可用宽度
            float availableWidth = trackArea?.resolvedStyle.width ?? 800f;

            // 如果轨道区域宽度还没计算出来，尝试使用父容器
            if (availableWidth <= 0)
            {
                availableWidth = allTrackContent?.resolvedStyle.width ?? 800f;
            }

            // 直接比较内容宽度和可用宽度
            if (contentWidth > availableWidth)
            {
                trackScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            }
            else
            {
                trackScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            trackScrollView.MarkDirtyRepaint();
        }

        private void ForceRefreshScrollBar()
        {
            if (trackScrollView == null || trackContent == null) return;

            // 多帧延迟确保布局计算完成
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    if (trackScrollView == null || trackContent == null) return;

                    float contentWidth = trackContent.resolvedStyle.width;
                    float availableWidth = trackArea?.resolvedStyle.width ?? 800f;

                    if (contentWidth > availableWidth + 1f)
                    {
                        trackScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
                    }
                    else
                    {
                        trackScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    }

                    trackScrollView.MarkDirtyRepaint();
                };
            };
        }

        private void OnGlobalControlToggled(bool isShow)
        {
            skillEditorData.IsGlobalControlShow = isShow;
            RefreshView();
        }

        private void UpdateFrameInputDisplay(int frame)
        {
            // 找到当前帧输入框并更新其显示值
            var currentFrameInput = rootVisualElement.Q<IntegerField>("current-frame-input");
            if (currentFrameInput != null)
            {
                currentFrameInput.SetValueWithoutNotify(frame);
            }
        }

        private void OnMaxFrameChanged(int newMaxFrame)
        {
            // 先更新时间轴管理器
            skillEditorTimeline.UpdateMaxFrame(newMaxFrame);

            // 延迟更新轨道内容和宽度，确保时间轴更新完成
            EditorApplication.delayCall += () =>
            {
                UpdateTimelineAndTrackContent();
                UpdateTrackContentWidth();

                // 额外延迟刷新滚动条，确保内容宽度更新完成
                EditorApplication.delayCall += () =>
                {
                    ForceRefreshScrollBar();
                };
            };
        }

        #endregion
    }
}
