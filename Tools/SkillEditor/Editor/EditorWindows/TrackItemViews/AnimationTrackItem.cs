using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 动画轨道项
    /// 专门处理动画轨道项的显示、交互和数据管理
    /// 提供动画特有的事件视图和Inspector数据功能
    /// </summary>
    public class AnimationTrackItem : BaseTrackItemView
    {
        #region 私有字段

        /// <summary>轨道项持续帧数</summary>
        private int frameCount;

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前动画轨道项数据对象</summary>
        private AnimationTrackItemData currentAnimationData;

        /// <summary>动画事件标签</summary>
        private Label animationEvent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 动画轨道项构造函数
        /// 创建并初始化动画轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public AnimationTrackItem(VisualElement visual, string title, int frameCount, int startFrame = 0, int trackIndex = 0)
        {
            this.frameCount = frameCount;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeAnimationTrackItem();

            // 创建轨道项内容
            itemContent = CreateAnimationTrackItemContent(title);
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
        /// 获取当前动画轨道项的数据对象
        /// </summary>
        public AnimationTrackItemData AnimationData
        {
            get
            {
                if (currentAnimationData == null)
                {
                    currentAnimationData = CreateAnimationTrackItemData();
                }
                return currentAnimationData;
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
        /// 动画轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectAnimationTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentAnimationData != null)
            {
                currentAnimationData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentAnimationData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化动画轨道项容器和基础样式
        /// </summary>
        private void InitializeAnimationTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建动画轨道项的内容容器
        /// 应用动画轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的动画轨道项内容容器</returns>
        private VisualElement CreateAnimationTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.AddToClassList("TrackItem-Animation"); // 动画轨道特定样式
            itemContent.tooltip = title;

            // 创建动画事件标签
            animationEvent = new Label();
            itemContent.Add(animationEvent);

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为动画轨道项内容添加标题标签
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
        /// 在Inspector面板中选中当前动画轨道项
        /// 创建动画轨道项数据并设置为选中对象
        /// </summary>
        private void SelectAnimationTrackItemInInspector()
        {
            if (currentAnimationData == null)
            {
                currentAnimationData = CreateAnimationTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentAnimationData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentAnimationData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentAnimationData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentAnimationData);
            }
        }

        /// <summary>
        /// 创建动画轨道项的数据对象
        /// </summary>
        /// <returns>动画轨道项数据对象</returns>
        private AnimationTrackItemData CreateAnimationTrackItemData()
        {
            string itemName = GetAnimationTrackItemName();

            var animationData = ScriptableObject.CreateInstance<AnimationTrackItemData>();
            animationData.trackItemName = itemName;
            animationData.startFrame = startFrame;
            animationData.trackIndex = trackIndex; // 设置轨道索引用于多轨道数据定位

            // 设置动画特有的默认属性
            SetDefaultAnimationProperties(animationData);

            // 从技能配置同步动画数据
            SyncWithAnimationConfigData(animationData, itemName);

            // 基准帧数应该在同步配置数据后设置，如果配置中没有数据则使用当前轨道项帧数
            // 这样确保基准帧数反映的是动画的原始长度
            if (animationData.frameCount <= 0)
            {
                animationData.frameCount = frameCount;
            }

            // 如果持续帧数还没有设置，则使用基准帧数
            if (animationData.durationFrame <= 0)
            {
                animationData.durationFrame = animationData.frameCount;
            }

            return animationData;
        }

        /// <summary>
        /// 获取动画轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetAnimationTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置动画数据的默认属性
        /// </summary>
        /// <param name="animationData">要设置的动画数据对象</param>
        private void SetDefaultAnimationProperties(AnimationTrackItemData animationData)
        {
            // 默认动画参数
            animationData.animationPlaySpeed = 1f; // 默认播放速度为1
            animationData.applyRootMotion = false; // 默认不应用根运动
            animationData.animationClip = null; // 默认动画片段为空
            // 注意：frameCount 和 durationFrame 在外部设置
        }

        /// <summary>
        /// 从技能配置同步动画数据
        /// </summary>
        /// <param name="animationData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithAnimationConfigData(AnimationTrackItemData animationData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null)
                return;

            // 查找对应的动画片段配置
            var configClip = skillConfig.trackContainer.animationTrack.animationClips?
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复动画属性
                animationData.animationClip = configClip.clip;
                animationData.durationFrame = configClip.durationFrame;
                animationData.animationPlaySpeed = configClip.animationPlaySpeed;
                animationData.applyRootMotion = configClip.applyRootMotion;

                // 如果配置中有原始帧数，则使用配置中的值作为基准帧数
                // 否则使用动画片段的实际长度作为基准帧数
                if (configClip.clip != null)
                {
                    float frameRate = SkillEditorData.CurrentSkillConfig?.frameRate ?? 30f;
                    animationData.frameCount = Mathf.RoundToInt(configClip.clip.length * frameRate);
                }
            }
        }

        #endregion
    }
}
