using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 变换轨道项
    /// 专门处理变换轨道项的显示、交互和数据管理
    /// 提供变换特有的位置、旋转、缩放控制和Inspector数据功能
    /// </summary>
    public class TransformTrackItem : BaseTrackItemView
    {
        #region 私有字段

        /// <summary>轨道项持续帧数</summary>
        private int frameCount;

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前变换轨道项数据对象</summary>
        private TransformTrackItemData currentTransformData;

        #endregion

        #region 构造函数

        /// <summary>
        /// 变换轨道项构造函数
        /// 创建并初始化变换轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public TransformTrackItem(VisualElement visual, string title, int frameCount, int startFrame = 0, int trackIndex = 0)
        {
            this.frameCount = frameCount;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeTransformTrackItem();

            // 创建轨道项内容
            itemContent = CreateTransformTrackItemContent(title);
            trackItem.Add(itemContent);

            // 设置宽度和位置
            SetWidth();
            UpdatePosition();
            visual.Add(trackItem);

            // 注册拖拽事件
            RegisterDragEvents();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取当前变换轨道项的数据对象
        /// </summary>
        public TransformTrackItemData TransformData
        {
            get
            {
                if (currentTransformData == null)
                {
                    currentTransformData = CreateTransformTrackItemData();
                }
                return currentTransformData;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置轨道项的起始帧位置
        /// 根据帧位置和当前帧单位宽度计算实际的像素位置
        /// </summary>
        /// <param name="frame">起始帧位置</param>
        public override void SetStartFrame(int frame)
        {
            startFrame = frame;
            UpdatePosition();
        }

        /// <summary>
        /// 更新轨道项的位置
        /// 根据起始帧位置和当前帧单位宽度重新计算像素位置
        /// </summary>
        public override void UpdatePosition()
        {
            float pixelPosition = startFrame * SkillEditorData.FrameUnitWidth;
            trackItem.style.left = pixelPosition;
        }

        /// <summary>
        /// 根据帧数和单位宽度设置轨道项宽度
        /// 宽度会根据SkillEditorData中的帧单位宽度动态计算
        /// </summary>
        public override void SetWidth()
        {
            itemContent.style.width = frameCount * SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 更新轨道项的帧数，并重新计算宽度
        /// </summary>
        /// <param name="newFrameCount">新的帧数</param>
        public override void UpdateFrameCount(int newFrameCount)
        {
            frameCount = newFrameCount;
            SetWidth();
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
        public override void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
        }

        #endregion

        #region 基类重写方法

        /// <summary>
        /// 变换轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectTransformTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentTransformData != null)
            {
                currentTransformData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentTransformData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化变换轨道项容器和基础样式
        /// </summary>
        private void InitializeTransformTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建变换轨道项的内容容器
        /// 应用变换轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的变换轨道项内容容器</returns>
        private VisualElement CreateTransformTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.AddToClassList("TrackItem-Transform"); // 变换轨道特定样式
            itemContent.tooltip = title;

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为变换轨道项内容添加标题标签
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

        #region Inspector数据管理方法

        /// <summary>
        /// 在Inspector面板中选中当前变换轨道项
        /// 创建变换轨道项数据并设置为选中对象
        /// </summary>
        private void SelectTransformTrackItemInInspector()
        {
            if (currentTransformData == null)
            {
                currentTransformData = CreateTransformTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentTransformData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentTransformData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentTransformData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentTransformData);
            }
        }

        /// <summary>
        /// 创建变换轨道项的数据对象
        /// </summary>
        /// <returns>变换轨道项数据对象</returns>
        private TransformTrackItemData CreateTransformTrackItemData()
        {
            string itemName = GetTransformTrackItemName();

            var transformData = ScriptableObject.CreateInstance<TransformTrackItemData>();
            transformData.trackItemName = itemName;
            transformData.frameCount = frameCount;
            transformData.startFrame = startFrame;
            transformData.trackIndex = trackIndex; // 设置轨道索引用于多轨道数据定位
            transformData.durationFrame = frameCount;

            // 设置变换特有的默认属性
            SetDefaultTransformProperties(transformData);

            // 从技能配置同步变换数据
            SyncWithTransformConfigData(transformData, itemName);

            return transformData;
        }

        /// <summary>
        /// 获取变换轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetTransformTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置变换数据的默认属性
        /// </summary>
        /// <param name="transformData">要设置的变换数据对象</param>
        private void SetDefaultTransformProperties(TransformTrackItemData transformData)
        {
            // 默认变换参数
            transformData.enablePosition = true;
            transformData.enableRotation = false;
            transformData.enableScale = false;
            transformData.positionOffset = Vector3.zero;
            transformData.targetRotation = Vector3.zero;
            transformData.targetScale = Vector3.one;
            transformData.curveType = FFramework.Kit.AnimationCurveType.Linear;
            transformData.customCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }

        /// <summary>
        /// 从技能配置同步变换数据
        /// </summary>
        /// <param name="transformData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithTransformConfigData(TransformTrackItemData transformData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.transformTrack == null)
                return;

            // 查找对应的变换片段配置
            var configClip = skillConfig.trackContainer.transformTrack.transformClips?
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复变换属性
                transformData.durationFrame = configClip.durationFrame;
                transformData.enablePosition = configClip.enablePosition;
                transformData.enableRotation = configClip.enableRotation;
                transformData.enableScale = configClip.enableScale;
                transformData.positionOffset = configClip.positionOffset;
                transformData.targetRotation = configClip.targetRotation;
                transformData.targetScale = configClip.targetScale;
                transformData.curveType = configClip.curveType;
                transformData.customCurve = configClip.customCurve;
            }
        }

        #endregion
    }
}
