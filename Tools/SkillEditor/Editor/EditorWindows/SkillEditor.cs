using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器 - 重构后的主类
    /// 提供技能时间轴编辑功能，包括轨道管理、时间线控制和UI交互
    /// </summary>
    public class SkillEditor : EditorWindow
    {
        #region 管理器
        /// <summary>UI构建器 - 负责创建和刷新编辑器界面</summary>
        private SkillEditorUIBuilder uiBuilder;
        /// <summary>时间轴管理器 - 处理时间轴显示和帧控制</summary>
        private SkillEditorTimeline skillEditorTimeline;
        /// <summary>滚动同步管理器 - 协调各滚动视图的同步</summary>
        private SkillEditorScrollSync skillEditorScrollSync;

        /// <summary>当前选中的轨道项</summary>
        public static SkillEditorTrackItem CurrentTrackItem;
        #endregion

        #region GUI元素
        /// <summary>主内容容器</summary>
        private VisualElement mainContent;
        /// <summary>全局控制区域</summary>
        private VisualElement globalControlContent;
        /// <summary>所有轨道容器</summary>
        private VisualElement allTrackContent;
        /// <summary>轨道控制区域</summary>
        private VisualElement trackControlArea;
        /// <summary>轨道控制滚动视图</summary>
        private ScrollView trackControlScrollView;
        /// <summary>轨道控制内容</summary>
        private VisualElement trackControlContent;
        /// <summary>轨道显示区域</summary>
        private VisualElement trackArea;
        /// <summary>轨道滚动视图</summary>
        private ScrollView trackScrollView;
        /// <summary>轨道内容容器</summary>
        private VisualElement trackContent;
        #endregion

        /// <summary>
        /// 创建技能编辑器窗口的菜单项
        /// 可通过快捷键 Shift+S 快速打开
        /// </summary>
        [MenuItem("FFramework/⚔️SkillEditor #S", priority = 5)]
        public static void SkillEditorCreateWindow()
        {
            SkillEditor window = GetWindow<SkillEditor>();
            window.minSize = new Vector2(1000, 450);
            window.titleContent = new GUIContent("SkillEditor");
            window.Show();
        }

        /// <summary>
        /// 编辑器窗口启用时的初始化
        /// 依次初始化管理器和UI界面
        /// </summary>
        private void OnEnable()
        {
            InitializeManagers();
            InitializeUI();
        }

        /// <summary>
        /// 编辑器窗口禁用时的清理工作
        /// 保存数据并清理事件监听
        /// </summary>
        private void OnDisable()
        {
            SkillEditorData.SaveData();
            SkillEditorEvent.Cleanup();

            // 清理编辑器数据，释放内存资源
            SkillEditorData.ResetData();
        }

        /// <summary>
        /// 初始化各个管理器实例
        /// 创建管理器对象并注册事件监听
        /// </summary>
        private void InitializeManagers()
        {
            // SkillEditorData为静态类，无需实例化
            skillEditorTimeline = new SkillEditorTimeline();
            uiBuilder = new SkillEditorUIBuilder();
            skillEditorScrollSync = new SkillEditorScrollSync();

            // 注册事件监听
            SkillEditorEvent.OnCurrentFrameChanged += OnCurrentFrameChanged;
            SkillEditorEvent.OnMaxFrameChanged += OnMaxFrameChanged;
            SkillEditorEvent.OnGlobalControlToggled += OnGlobalControlToggled;
            SkillEditorEvent.OnRefreshRequested += RefreshView;
            SkillEditorEvent.OnTimelineZoomChanged += OnTimelineZoomChanged;
        }

        /// <summary>
        /// 初始化UI界面
        /// 清空根元素，加载样式表，创建主体结构
        /// </summary>
        private void InitializeUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorStyle"));
            CreateMainStructure();
            RefreshView();
        }

        /// <summary>
        /// 时间轴缩放变化时的处理
        /// 刷新时间轴显示并更新轨道内容宽度，同步滚动条状态
        /// </summary>
        private void OnTimelineZoomChanged()
        {
            // 刷新时间轴，内部会同步轨道内容宽度
            skillEditorTimeline.RefreshTimeline();
            UpdateTrackContentWidth();

            // 延迟刷新滚动条状态，确保布局计算完成
            EditorApplication.delayCall += ForceRefreshScrollBar;
        }

        /// <summary>
        /// 更新轨道内容区域的宽度
        /// 根据时间轴缩放重新计算并应用宽度，同时更新所有轨道和轨道项
        /// </summary>
        private void UpdateTrackContentWidth()
        {
            if (trackContent == null) return;

            float newWidth = SkillEditorData.CalculateTimelineWidth();
            trackContent.style.width = newWidth;

            // 更新所有轨道元素的宽度
            uiBuilder.RefreshTrackContent(trackContent);

            // 同步所有轨道宽度和轨道项位置及宽度
            foreach (var item in SkillEditorData.tracks)
            {
                if (item.Track != null)
                {
                    item.Track.SetWidth(newWidth);
                    item.Track.RefreshTrackItems(); // 使用新的方法刷新轨道项位置和宽度
                }
            }
        }

        /// <summary>
        /// 创建编辑器主体结构
        /// 构建主内容区、全局控制区和轨道区域
        /// </summary>
        private void CreateMainStructure()
        {
            mainContent = uiBuilder.CreateMainContent(rootVisualElement);
            globalControlContent = uiBuilder.CreateGlobalControlArea(mainContent);
            allTrackContent = uiBuilder.CreateTrackArea(mainContent);

            CreateTrackStructure();
        }

        /// <summary>
        /// 创建轨道结构
        /// 构建轨道控制区、轨道显示区，初始化时间轴和滚动同步
        /// </summary>
        private void CreateTrackStructure()
        {
            var trackElements = uiBuilder.CreateTrackStructure(allTrackContent);

            // 获取轨道结构元素
            trackControlArea = trackElements.ControlArea;
            trackControlScrollView = trackElements.ControlScrollView;
            trackControlContent = trackElements.ControlContent;
            trackArea = trackElements.TrackArea;
            trackScrollView = trackElements.TrackScrollView;
            trackContent = trackElements.TrackContent;

            // 设置水平滚动条自动显示/隐藏
            if (trackScrollView != null)
            {
                trackScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            }

            // 初始化时间轴
            skillEditorTimeline.InitializeTimeline(trackArea);
            skillEditorTimeline.SetTrackContent(trackContent);

            // 初始同步轨道内容宽度
            UpdateTrackContentWidth();

            // 设置滚动同步和X轴滚动回调
            skillEditorScrollSync.SetupScrollSync(trackControlScrollView, trackScrollView, skillEditorTimeline);
            skillEditorScrollSync.OnTrackScrollXChanged = skillEditorTimeline.SetTimelineOffset;
        }

        /// <summary>
        /// 刷新整个编辑器视图
        /// 更新全局控制区和时间轴显示
        /// </summary>
        private void RefreshView()
        {
            uiBuilder.RefreshGlobalControl(globalControlContent);
            skillEditorTimeline.RefreshTimeline();
        }

        #region 事件处理

        /// <summary>
        /// 当前帧变化时的处理
        /// 更新时间轴的帧指示器和帧输入框显示
        /// </summary>
        /// <param name="newFrame">新的帧数</param>
        private void OnCurrentFrameChanged(int newFrame)
        {
            skillEditorTimeline.UpdateCurrentFrame(newFrame);
            UpdateFrameInputDisplay(newFrame);
        }

        /// <summary>
        /// 更新时间轴和轨道内容
        /// 刷新时间轴显示并重新计算轨道内容宽度
        /// </summary>
        private void UpdateTimelineAndTrackContent()
        {
            skillEditorTimeline.RefreshTimeline();

            if (trackContent != null)
            {
                float newWidth = SkillEditorData.CalculateTimelineWidth();
                trackContent.style.width = newWidth;
                trackContent.style.minWidth = newWidth;
                uiBuilder.RefreshTrackContent(trackContent);

                // 延迟检查滚动条状态
                CheckAndForceScrollBar(newWidth);
            }
        }

        /// <summary>
        /// 检查并强制更新滚动条状态
        /// 根据内容宽度和可用宽度决定是否显示水平滚动条
        /// </summary>
        /// <param name="contentWidth">内容宽度</param>
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

            // 根据内容宽度决定滚动条显示状态
            trackScrollView.horizontalScrollerVisibility = contentWidth > availableWidth
                ? ScrollerVisibility.Auto
                : ScrollerVisibility.Hidden;

            trackScrollView.MarkDirtyRepaint();
        }

        /// <summary>
        /// 强制刷新滚动条状态
        /// 延迟执行确保布局计算完成后更新滚动条
        /// </summary>
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

                    trackScrollView.horizontalScrollerVisibility = contentWidth > availableWidth + 1f
                        ? ScrollerVisibility.Auto
                        : ScrollerVisibility.Hidden;

                    trackScrollView.MarkDirtyRepaint();
                };
            };
        }

        /// <summary>
        /// 全局控制区显示状态切换处理
        /// 更新数据状态并刷新视图
        /// </summary>
        /// <param name="isShow">是否显示全局控制区</param>
        private void OnGlobalControlToggled(bool isShow)
        {
            SkillEditorData.IsGlobalControlShow = isShow;
            RefreshView();
        }

        /// <summary>
        /// 更新帧输入框的显示值
        /// 同步当前帧到UI输入框，不触发值变化事件
        /// </summary>
        /// <param name="frame">要显示的帧数</param>
        private void UpdateFrameInputDisplay(int frame)
        {
            var currentFrameInput = rootVisualElement.Q<IntegerField>("current-frame-input");
            currentFrameInput?.SetValueWithoutNotify(frame);
        }

        /// <summary>
        /// 最大帧数变化时的处理
        /// 更新时间轴管理器并延迟刷新相关内容和滚动条
        /// </summary>
        /// <param name="newMaxFrame">新的最大帧数</param>
        private void OnMaxFrameChanged(int newMaxFrame)
        {
            // 更新时间轴管理器
            skillEditorTimeline.UpdateMaxFrame(newMaxFrame);

            // 延迟更新确保时间轴更新完成
            EditorApplication.delayCall += () =>
            {
                UpdateTimelineAndTrackContent();
                UpdateTrackContentWidth();

                // 额外延迟刷新滚动条
                EditorApplication.delayCall += ForceRefreshScrollBar;
            };
        }

        /// <summary>
        /// 刷新特效预览器数据
        /// 当轨道项发生变化时调用，确保特效预览与最新的配置数据同步
        /// </summary>
        public void RefreshEffectPreviewerData()
        {
            skillEditorTimeline?.RefreshEffectPreviewerData();
        }

        #endregion
    }
}
