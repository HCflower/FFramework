using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道项
    /// 负责管理单个轨道项的显示、交互和拖拽功能
    /// 支持多种轨道类型的可视化表示和Inspector选择功能
    /// </summary>
    public class SkillEditorTrackItem : VisualElement
    {
        #region 私有字段

        /// <summary>轨道项UI容器</summary>
        private VisualElement trackItem;

        /// <summary>轨道项内容容器</summary>
        private VisualElement itemContent;

        /// <summary>轨道项持续帧数</summary>
        private float frameCount;

        /// <summary>所属轨道类型</summary>
        private TrackType trackType;

        /// <summary>是否正在拖拽中</summary>
        private bool isDragging = false;

        /// <summary>拖拽开始位置</summary>
        private Vector2 dragStartPos;

        /// <summary>拖拽前的原始左边距</summary>
        private float originalLeft;

        /// <summary>轨道项的起始帧位置</summary>
        private float startFrame;

        #endregion

        #region 构造函数

        /// <summary>
        /// 轨道项构造函数
        /// 创建并初始化轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="trackType">轨道类型，决定样式和行为</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        public SkillEditorTrackItem(VisualElement visual, string title, TrackType trackType, float frameCount, float startFrame = 0)
        {
            this.frameCount = frameCount;
            this.trackType = trackType;
            this.startFrame = startFrame;

            // 创建并配置轨道项容器
            InitializeTrackItem();

            // 创建轨道项内容
            itemContent = CreateTrackItemContent(title);
            trackItem.Add(itemContent);

            // 设置宽度和位置
            SetWidth();
            UpdatePosition();
            visual.Add(trackItem);

            // 注册拖拽事件
            RegisterDragEvents();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置轨道项的起始帧位置
        /// 根据帧位置和当前帧单位宽度计算实际的像素位置
        /// </summary>
        /// <param name="frame">起始帧位置</param>
        public void SetStartFrame(float frame)
        {
            startFrame = frame;
            UpdatePosition();
        }

        /// <summary>
        /// 设置轨道项的左边距位置（保持向后兼容）
        /// 根据像素位置反推帧位置
        /// </summary>
        /// <param name="left">左边距值（像素）</param>
        public void SetLeft(float left)
        {
            // 根据像素位置计算对应的帧位置
            startFrame = left / SkillEditorData.FrameUnitWidth;
            trackItem.style.left = left;
        }

        /// <summary>
        /// 更新轨道项的位置
        /// 根据起始帧位置和当前帧单位宽度重新计算像素位置
        /// </summary>
        public void UpdatePosition()
        {
            float pixelPosition = startFrame * SkillEditorData.FrameUnitWidth;
            trackItem.style.left = pixelPosition;
        }

        /// <summary>
        /// 根据帧数和单位宽度设置轨道项宽度
        /// 宽度会根据SkillEditorData中的帧单位宽度动态计算
        /// </summary>
        public void SetWidth()
        {
            itemContent.style.width = frameCount * SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 获取轨道项的起始帧位置
        /// </summary>
        /// <returns>起始帧位置</returns>
        public float GetStartFrame()
        {
            return startFrame;
        }

        /// <summary>
        /// 获取轨道项的结束帧位置
        /// </summary>
        /// <returns>结束帧位置</returns>
        public float GetEndFrame()
        {
            return startFrame + frameCount;
        }

        /// <summary>
        /// 刷新轨道项的显示
        /// 更新宽度和位置以适应缩放变化
        /// </summary>
        public void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化轨道项容器和基础样式
        /// </summary>
        private void InitializeTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        /// <summary>
        /// 注册拖拽相关的鼠标事件
        /// </summary>
        private void RegisterDragEvents()
        {
            trackItem.RegisterCallback<PointerDownEvent>(OnPointerDown);
            trackItem.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            trackItem.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建轨道项的内容容器
        /// 根据轨道类型应用相应的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的轨道项内容容器</returns>
        public VisualElement CreateTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");

            // 应用轨道类型特定样式
            AddTrackTypeStyleClass(itemContent);

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为轨道项内容添加轨道类型特定的样式类
        /// </summary>
        /// <param name="itemContent">要添加样式的内容容器</param>
        private void AddTrackTypeStyleClass(VisualElement itemContent)
        {
            string styleClass = GetTrackTypeStyleClass();
            if (!string.IsNullOrEmpty(styleClass))
                itemContent.AddToClassList(styleClass);
        }

        /// <summary>
        /// 根据轨道类型获取对应的CSS样式类名
        /// </summary>
        /// <returns>轨道类型对应的样式类名</returns>
        private string GetTrackTypeStyleClass()
        {
            return trackType switch
            {
                TrackType.AnimationTrack => "TrackItem-Animation",
                TrackType.AudioTrack => "TrackItem-Audio",
                TrackType.EffectTrack => "TrackItem-Effect",
                TrackType.EventTrack => "TrackItem-Event",
                TrackType.AttackTrack => "TrackItem-Attack",
                _ => ""
            };
        }

        /// <summary>
        /// 为轨道项内容添加标题标签
        /// </summary>
        /// <param name="itemContent">内容容器</param>
        /// <param name="title">标题文本</param>
        private void AddTitleLabel(VisualElement itemContent, string title)
        {
            Label titleLabel = new Label();
            titleLabel.AddToClassList("TrackItemTitle");
            titleLabel.text = title;
            itemContent.Add(titleLabel);
        }

        #endregion

        #region 拖拽事件处理

        /// <summary>
        /// 鼠标按下事件处理
        /// 启动拖拽操作并选中轨道项到Inspector面板
        /// </summary>
        /// <param name="evt">鼠标按下事件参数</param>
        private void OnPointerDown(PointerDownEvent evt)
        {
            StartDrag(evt);
            SelectTrackItemInInspector();
        }

        /// <summary>
        /// 开始拖拽操作
        /// 记录拖拽开始位置和原始帧位置，并捕获指针
        /// </summary>
        /// <param name="evt">鼠标按下事件参数</param>
        private void StartDrag(PointerDownEvent evt)
        {
            isDragging = true;
            dragStartPos = evt.position;
            originalLeft = startFrame * SkillEditorData.FrameUnitWidth; // 基于帧位置计算像素位置
            trackItem.CapturePointer(evt.pointerId);
        }

        /// <summary>
        /// 在Inspector面板中选中当前轨道项
        /// 创建对应类型的ScriptableObject数据并设置为选中对象
        /// </summary>
        private void SelectTrackItemInInspector()
        {
            var trackItemData = CreateTrackItemData();
            if (trackItemData != null)
                UnityEditor.Selection.activeObject = trackItemData;
        }

        /// <summary>
        /// 鼠标移动事件处理
        /// 当正在拖拽时更新轨道项位置并对齐到刻度
        /// </summary>
        /// <param name="evt">鼠标移动事件参数</param>
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging) return;

            float newLeft = CalculateNewPosition(evt);
            newLeft = ClampToTrackBounds(newLeft);

            // 更新像素位置和对应的帧位置
            trackItem.style.left = newLeft;
            startFrame = newLeft / SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 鼠标释放事件处理
        /// 结束拖拽操作并释放指针捕获
        /// </summary>
        /// <param name="evt">鼠标释放事件参数</param>
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging) return;
            isDragging = false;
            trackItem.ReleasePointer(evt.pointerId);
        }

        #endregion

        #region 拖拽辅助方法

        /// <summary>
        /// 计算拖拽时的新位置
        /// 根据鼠标移动距离计算新位置并对齐到帧刻度
        /// </summary>
        /// <param name="evt">鼠标移动事件参数</param>
        /// <returns>对齐到刻度的新左边距位置</returns>
        private float CalculateNewPosition(PointerMoveEvent evt)
        {
            float deltaX = evt.position.x - dragStartPos.x;
            float newLeft = originalLeft + deltaX;

            // 对齐到帧刻度
            float unit = SkillEditorData.FrameUnitWidth;
            return Mathf.Round(newLeft / unit) * unit;
        }

        /// <summary>
        /// 将位置限制在轨道边界内
        /// 确保轨道项不会拖拽到轨道区域之外
        /// </summary>
        /// <param name="newLeft">计算得到的新左边距位置</param>
        /// <returns>限制在边界内的左边距位置</returns>
        private float ClampToTrackBounds(float newLeft)
        {
            if (trackItem.parent == null) return newLeft;

            float trackWidth = trackItem.parent.resolvedStyle.width;
            float itemWidth = trackItem.resolvedStyle.width;
            return Mathf.Clamp(newLeft, 0, trackWidth - itemWidth);
        }

        #endregion

        #region Inspector数据创建方法

        /// <summary>
        /// 创建轨道项对应的数据对象用于Inspector显示
        /// 根据轨道类型创建相应的ScriptableObject数据
        /// </summary>
        /// <returns>创建的轨道项数据对象</returns>
        private ScriptableObject CreateTrackItemData()
        {
            string itemName = GetTrackItemName();

            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    return CreateAnimationTrackItemData(itemName);
                default:
                    return CreateDefaultTrackItemData(itemName);
            }
        }

        /// <summary>
        /// 获取轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 创建动画轨道项的数据对象
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>动画轨道项数据对象</returns>
        private AnimationTrackItemData CreateAnimationTrackItemData(string itemName)
        {
            var animData = ScriptableObject.CreateInstance<AnimationTrackItemData>();
            animData.trackName = itemName;
            animData.frameCount = frameCount;
            // TODO: 补充动画相关参数
            // animData.animationClip = ...;
            // animData.isLoop = ...;
            return animData;
        }

        /// <summary>
        /// 创建默认类型轨道项的数据对象
        /// 用于不需要特殊处理的轨道类型
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <returns>默认轨道项数据对象</returns>
        private AnimationTrackItemData CreateDefaultTrackItemData(string itemName)
        {
            var baseData = ScriptableObject.CreateInstance<AnimationTrackItemData>();
            baseData.trackName = itemName;
            baseData.frameCount = frameCount;
            return baseData;
        }

        #endregion
    }
}
