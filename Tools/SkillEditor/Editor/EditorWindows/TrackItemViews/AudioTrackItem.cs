using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 音频轨道项
    /// 专门处理音频轨道项的显示、交互和数据管理
    /// 提供音频特有的事件视图和Inspector数据功能
    /// </summary>
    public class AudioTrackItem : BaseTrackItemView
    {
        #region 私有字段

        /// <summary>轨道项持续帧数</summary>
        private int frameCount;

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前音频轨道项数据对象</summary>
        private AudioTrackItemData currentAudioData;

        /// <summary>音频事件标签</summary>
        private Label audioEvent;

        #endregion

        #region 构造函数

        /// <summary>
        /// 音频轨道项构造函数
        /// 创建并初始化音频轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public AudioTrackItem(VisualElement visual, string title, int frameCount, int startFrame = 0, int trackIndex = 0)
        {
            this.frameCount = frameCount;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeAudioTrackItem();

            // 创建轨道项内容
            itemContent = CreateAudioTrackItemContent(title);
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
        /// 获取当前音频轨道项的数据对象
        /// </summary>
        public AudioTrackItemData AudioData
        {
            get
            {
                if (currentAudioData == null)
                {
                    currentAudioData = CreateAudioTrackItemData();
                }
                return currentAudioData;
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

        /// <summary>
        /// 设置音频事件视图的显示
        /// 用于显示音频轨道中的事件可视化表示
        /// </summary>
        /// <param name="leftFrame">相对左边界帧</param>
        /// <param name="startFrame">事件起始帧</param>
        /// <param name="durationFrame">事件持续帧数</param>
        public void SetAudioEventView(int leftFrame, int startFrame, int durationFrame)
        {
            SetEventView(leftFrame, startFrame, durationFrame, audioEvent, Color.blue);
        }

        /// <summary>
        /// 设置事件视图的显示
        /// 用于在轨道项中显示特定事件的可视化表示
        /// </summary>
        /// <param name="leftFrame">相对左边界帧</param>
        /// <param name="startFrame">事件起始帧</param>
        /// <param name="durationFrame">事件持续帧数</param>
        /// <param name="eventElement">要设置的事件元素</param>
        /// <param name="color">事件显示颜色</param>
        protected virtual void SetEventView(int leftFrame, int startFrame, int durationFrame, VisualElement eventElement, Color color)
        {
            if (eventElement == null) return;

            eventElement.style.width = durationFrame * SkillEditorData.FrameUnitWidth;
            eventElement.style.left = (startFrame - leftFrame) * SkillEditorData.FrameUnitWidth;
            eventElement.style.backgroundColor = color;
        }

        #endregion

        #region 基类重写方法

        /// <summary>
        /// 音频轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectAudioTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentAudioData != null)
            {
                currentAudioData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentAudioData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化音频轨道项容器和基础样式
        /// </summary>
        private void InitializeAudioTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建音频轨道项的内容容器
        /// 应用音频轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的音频轨道项内容容器</returns>
        private VisualElement CreateAudioTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.AddToClassList("TrackItem-Audio"); // 音频轨道特定样式
            itemContent.tooltip = title;

            // 创建音频事件标签
            audioEvent = new Label();
            itemContent.Add(audioEvent);

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为音频轨道项内容添加标题标签
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
        /// 在Inspector面板中选中当前音频轨道项
        /// 创建音频轨道项数据并设置为选中对象
        /// </summary>
        private void SelectAudioTrackItemInInspector()
        {
            if (currentAudioData == null)
            {
                currentAudioData = CreateAudioTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentAudioData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentAudioData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentAudioData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentAudioData);
            }
        }

        /// <summary>
        /// 创建音频轨道项的数据对象
        /// </summary>
        /// <returns>音频轨道项数据对象</returns>
        private AudioTrackItemData CreateAudioTrackItemData()
        {
            string itemName = GetAudioTrackItemName();

            var audioData = ScriptableObject.CreateInstance<AudioTrackItemData>();
            audioData.trackItemName = itemName;
            audioData.frameCount = frameCount;
            audioData.startFrame = startFrame;
            audioData.trackIndex = trackIndex; // 设置轨道索引用于多轨道数据定位
            audioData.durationFrame = frameCount;

            // 设置音频特有的默认属性
            SetDefaultAudioProperties(audioData);

            // 从技能配置同步音频数据
            SyncWithAudioConfigData(audioData, itemName);

            return audioData;
        }

        /// <summary>
        /// 获取音频轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetAudioTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置音频数据的默认属性
        /// </summary>
        /// <param name="audioData">要设置的音频数据对象</param>
        private void SetDefaultAudioProperties(AudioTrackItemData audioData)
        {
            // 默认音频参数
            audioData.audioClip = null; // 默认音频剪辑为空
            audioData.volume = 1.0f; // 默认音量为1
            audioData.pitch = 1.0f; // 默认音调为1
            audioData.spatialBlend = 0.0f; // 默认空间混合为2D
            audioData.reverbZoneMix = 1.0f; // 默认混响区域混合为1
        }

        /// <summary>
        /// 从技能配置同步音频数据
        /// </summary>
        /// <param name="audioData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithAudioConfigData(AudioTrackItemData audioData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTrack == null)
                return;

            // 查找对应的音频片段配置
            var configClip = skillConfig.trackContainer.audioTrack.audioTracks?
                .Where(track => track.trackIndex == trackIndex)
                .SelectMany(track => track.audioClips ?? new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>())
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复音频属性
                audioData.audioClip = configClip.clip;
                audioData.durationFrame = configClip.durationFrame;
                audioData.volume = configClip.volume;
                audioData.pitch = configClip.pitch;
                audioData.spatialBlend = configClip.spatialBlend;
                audioData.reverbZoneMix = configClip.reverbZoneMix;
            }
        }

        #endregion
    }
}
