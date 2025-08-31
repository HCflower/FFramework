using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 特效轨道项
    /// 专门处理特效轨道项的显示、交互和数据管理
    /// 提供特效特有的事件视图和Inspector数据功能
    /// </summary>
    public class EffectTrackItem : TrackItemViewBase
    {
        #region 私有字段

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前特效轨道项数据对象</summary>
        private EffectTrackItemData currentEffectData;

        /// <summary>特效事件标签</summary>
        private Label cutEffectFrameView;

        #endregion

        #region 构造函数

        /// <summary>
        /// 特效轨道项构造函数
        /// 创建并初始化特效轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="durationFrame">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public EffectTrackItem(VisualElement visual, string title, int durationFrame, int startFrame = 0, int trackIndex = 0)
        {
            this.trackItemDurationFrame = durationFrame;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeEffectTrackItem();

            // 创建轨道项内容
            itemContent = CreateEffectTrackItemContent(title);
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
        /// 获取当前特效轨道项的数据对象
        /// </summary>
        public EffectTrackItemData EffectData
        {
            get
            {
                if (currentEffectData == null)
                {
                    currentEffectData = CreateEffectTrackItemData();
                    // 设置特效截断帧视图
                    SetCutEffectFrameView(currentEffectData.cutEffectFrameOffset, cutEffectFrameView);
                }
                return currentEffectData;
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
            itemContent.style.width = trackItemDurationFrame * SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 更新轨道项的帧数，并重新计算宽度
        /// </summary>
        /// <param name="newFrameCount">新的帧数</param>
        public override void UpdateFrameCount(int newFrameCount)
        {
            trackItemDurationFrame = newFrameCount;
            SetWidth();
        }

        /// <summary>
        /// 获取轨道项的结束帧位置
        /// </summary>
        /// <returns>结束帧位置</returns>
        public float GetEndFrame()
        {
            return startFrame + trackItemDurationFrame;
        }

        /// <summary>
        /// 刷新轨道项的显示
        /// 更新宽度和位置以适应缩放变化
        /// </summary>
        public override void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
            SetCutEffectFrameView(currentEffectData.cutEffectFrameOffset, cutEffectFrameView);
        }

        /// <summary>
        /// 创建特效轨道项数据对象
        /// </summary>
        /// <param name="startFrame">事件起始帧</param>
        /// <param name="cutEffectFrameOffset">特效截断帧偏移</param>
        /// <param name="visualElement">要设置的事件元素</param>
        private void SetCutEffectFrameView(int cutEffectFrameOffset, VisualElement visualElement)
        {
            if (visualElement == null || cutEffectFrameOffset == 0) return;
            visualElement.AddToClassList("CutEffectFrameView");
            visualElement.style.right = (currentEffectData.durationFrame - cutEffectFrameOffset) * SkillEditorData.FrameUnitWidth;
        }

        #endregion

        #region 基类重写方法

        /// <summary>
        /// 特效轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectEffectTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentEffectData != null)
            {
                currentEffectData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentEffectData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化特效轨道项容器和基础样式
        /// </summary>
        private void InitializeEffectTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建特效轨道项的内容容器
        /// 应用特效轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的特效轨道项内容容器</returns>
        private VisualElement CreateEffectTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.AddToClassList("TrackItem-Effect"); // 特效轨道特定样式
            itemContent.tooltip = title;

            // 创建特效事件标签
            cutEffectFrameView = new Label();
            itemContent.Add(cutEffectFrameView);

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为特效轨道项内容添加标题标签
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
        /// 在Inspector面板中选中当前特效轨道项
        /// 创建特效轨道项数据并设置为选中对象
        /// </summary>
        private void SelectEffectTrackItemInInspector()
        {
            if (currentEffectData == null)
            {
                currentEffectData = CreateEffectTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentEffectData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentEffectData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentEffectData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentEffectData);
            }
        }

        /// <summary>
        /// 创建特效轨道项的数据对象
        /// </summary>
        /// <returns>特效轨道项数据对象</returns>
        private EffectTrackItemData CreateEffectTrackItemData()
        {
            string itemName = GetEffectTrackItemName();

            var effectData = ScriptableObject.CreateInstance<EffectTrackItemData>();
            effectData.trackItemName = itemName;
            effectData.frameCount = trackItemDurationFrame;
            effectData.startFrame = startFrame;
            // 设置轨道索引用于多轨道数据定位
            effectData.trackIndex = trackIndex;
            effectData.durationFrame = trackItemDurationFrame;

            // 设置特效特有的默认属性
            SetDefaultEffectProperties(effectData);

            // 从技能配置同步特效数据
            SyncWithEffectConfigData(effectData, itemName);

            return effectData;
        }

        /// <summary>
        /// 获取特效轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetEffectTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置特效数据的默认属性
        /// </summary>
        /// <param name="effectData">要设置的特效数据对象</param>
        private void SetDefaultEffectProperties(EffectTrackItemData effectData)
        {
            // 默认特效参数
            effectData.effectPrefab = null;      // 默认特效预制体为空
            effectData.effectPlaySpeed = 1.0f;   // 默认播放速度为1
            effectData.isCutEffect = false;      // 默认不裁剪特效
            effectData.cutEffectFrameOffset = 0; // 默认裁剪帧为0
            effectData.position = Vector3.zero;  // 默认位置为原点
            effectData.rotation = Vector3.zero;  // 默认旋转为零
            effectData.scale = Vector3.one;      // 默认缩放为1
        }

        /// <summary>
        /// 从技能配置同步特效数据
        /// </summary>
        /// <param name="effectData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithEffectConfigData(EffectTrackItemData effectData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.effectTrack == null)
            {
                Debug.Log($"EffectTrackItem: 没有配置数据，保持默认值");
                return;
            }

            // 查找对应的特效片段配置
            var configClip = skillConfig.trackContainer.effectTrack.effectTracks?
                .Where(track => track.trackIndex == trackIndex)
                .SelectMany(track => track.effectClips ?? new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>())
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复特效属性
                effectData.trackItemName = configClip.clipName;
                effectData.effectPrefab = configClip.effectPrefab;
                effectData.durationFrame = configClip.durationFrame;
                effectData.effectPlaySpeed = configClip.effectPlaySpeed;
                effectData.isCutEffect = configClip.isCutEffect;
                effectData.cutEffectFrameOffset = configClip.cutEffectFrameOffset;
                effectData.position = configClip.position;
                effectData.rotation = configClip.rotation;
                effectData.scale = configClip.scale;
            }
            else
            {
                Debug.Log($"EffectTrackItem: 未找到配置数据，保持默认值");
            }
        }

        #endregion
    }
}
