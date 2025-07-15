using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器 - 重构后的主类
    /// </summary>
    public class SkillEditor : EditorWindow
    {
        #region 管理器
        private SkillEditorDataManager dataManager;
        private SkillEditorUIBuilder uiBuilder;
        private TimelineManager timelineManager;
        private ScrollSyncManager scrollSyncManager;
        private EventManager eventManager;
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
            dataManager?.SaveData();
            eventManager?.Cleanup();
        }

        private void InitializeManagers()
        {
            dataManager = new SkillEditorDataManager();
            eventManager = new EventManager();
            timelineManager = new TimelineManager(dataManager, eventManager);
            uiBuilder = new SkillEditorUIBuilder(eventManager);
            scrollSyncManager = new ScrollSyncManager();

            // 注册事件
            eventManager.OnCurrentFrameChanged += OnCurrentFrameChanged;
            eventManager.OnMaxFrameChanged += OnMaxFrameChanged;
            eventManager.OnGlobalControlToggled += OnGlobalControlToggled;
            eventManager.OnRefreshRequested += RefreshView;
            eventManager.OnTimelineZoomChanged += OnTimelineZoomChanged; // 添加缩放事件处理
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
            // 缩放时需要更新轨道内容宽度
            UpdateTrackContentWidth();
            // 刷新时间轴显示
            timelineManager.RefreshTimeline();
        }

        private void UpdateTrackContentWidth()
        {
            if (trackContent != null)
            {
                float newWidth = dataManager.CalculateTimelineWidth();
                trackContent.style.width = newWidth;

                // 更新所有轨道元素的宽度
                uiBuilder.RefreshTrackContent(trackContent, dataManager);
            }
        }

        private void CreateMainStructure()
        {
            mainContent = uiBuilder.CreateMainContent(rootVisualElement);
            globalControlContent = uiBuilder.CreateGlobalControlArea(mainContent, dataManager.IsGlobalControlShow);
            allTrackContent = uiBuilder.CreateTrackArea(mainContent);

            CreateTrackStructure();
        }

        private void CreateTrackStructure()
        {
            var trackElements = uiBuilder.CreateTrackStructure(allTrackContent, dataManager);

            trackControlArea = trackElements.controlArea;
            trackControlScrollView = trackElements.controlScrollView;
            trackControlContent = trackElements.controlContent;
            trackArea = trackElements.trackArea;
            trackScrollView = trackElements.trackScrollView;
            trackContent = trackElements.trackContent;

            // 初始化时间轴
            timelineManager.InitializeTimeline(trackArea);

            // 设置轨道内容引用，以便时间轴缩放时同步更新
            timelineManager.SetTrackContent(trackContent);

            // 设置滚动同步
            scrollSyncManager.SetupScrollSync(trackControlScrollView, trackScrollView);
        }

        private void RefreshView()
        {
            uiBuilder.RefreshGlobalControl(globalControlContent, dataManager);
            // uiBuilder.RefreshTrackContent(trackControlContent, dataManager);
            timelineManager.RefreshTimeline();
        }

        #region 事件处理
        private void OnCurrentFrameChanged(int newFrame)
        {
            timelineManager.UpdateCurrentFrame(newFrame);

            // 更新帧输入框显示
            UpdateFrameInputDisplay(newFrame);
        }

        private void OnMaxFrameChanged(int newMaxFrame)
        {
            timelineManager.UpdateMaxFrame(newMaxFrame);

            // 只更新必要的内容，不进行全面刷新
            UpdateTimelineAndTrackContent();
        }

        private void UpdateTimelineAndTrackContent()
        {
            // 只更新时间轴和轨道内容，不影响控制区域
            timelineManager.RefreshTimeline();

            // 更新轨道内容宽度
            if (trackContent != null)
            {
                float newWidth = dataManager.CalculateTimelineWidth();
                trackContent.style.width = newWidth;
                uiBuilder.RefreshTrackContent(trackContent, dataManager);
            }
        }

        private void OnGlobalControlToggled(bool isShow)
        {
            dataManager.IsGlobalControlShow = isShow;
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

        #endregion
    }
}
