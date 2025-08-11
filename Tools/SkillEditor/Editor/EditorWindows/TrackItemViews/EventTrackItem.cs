using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 事件轨道项
    /// 专门处理事件轨道项的显示、交互和数据管理
    /// 提供事件特有的图标视图和Inspector数据功能
    /// </summary>
    public class EventTrackItem : BaseTrackItemView
    {
        #region 私有字段

        /// <summary>轨道项持续帧数</summary>
        private int frameCount;

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前事件轨道项数据对象</summary>
        private EventTrackItemData currentEventData;

        /// <summary>事件图标标签</summary>
        private Label eventIcon;

        #endregion

        #region 构造函数

        /// <summary>
        /// 事件轨道项构造函数
        /// 创建并初始化事件轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public EventTrackItem(VisualElement visual, string title, int frameCount, int startFrame = 0, int trackIndex = 0)
        {
            this.frameCount = frameCount;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeEventTrackItem();

            // 创建轨道项内容
            itemContent = CreateEventTrackItemContent(title);
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
        /// 获取当前事件轨道项的数据对象
        /// </summary>
        public EventTrackItemData EventData
        {
            get
            {
                if (currentEventData == null)
                {
                    currentEventData = CreateEventTrackItemData();
                }
                return currentEventData;
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
        /// 事件轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectEventTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentEventData != null)
            {
                currentEventData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentEventData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化事件轨道项容器和基础样式
        /// </summary>
        private void InitializeEventTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建事件轨道项的内容容器
        /// 应用事件轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的事件轨道项内容容器</returns>
        private VisualElement CreateEventTrackItemContent(string title)
        {
            VisualElement eventItemContent = new VisualElement();
            eventItemContent.AddToClassList("EventIcon");
            eventItemContent.tooltip = title;

            // 添加标题标签
            AddTitleLabel(eventItemContent, title);

            return eventItemContent;
        }

        /// <summary>
        /// 为事件轨道项内容添加标题标签
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
        /// 在Inspector面板中选中当前事件轨道项
        /// 创建事件轨道项数据并设置为选中对象
        /// </summary>
        private void SelectEventTrackItemInInspector()
        {
            if (currentEventData == null)
            {
                currentEventData = CreateEventTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentEventData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentEventData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentEventData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentEventData);
            }
        }

        /// <summary>
        /// 创建事件轨道项的数据对象
        /// </summary>
        /// <returns>事件轨道项数据对象</returns>
        private EventTrackItemData CreateEventTrackItemData()
        {
            string itemName = GetEventTrackItemName();

            var eventData = ScriptableObject.CreateInstance<EventTrackItemData>();
            eventData.trackItemName = itemName;
            eventData.frameCount = frameCount;
            eventData.startFrame = startFrame;
            eventData.trackIndex = trackIndex; // 设置轨道索引用于多轨道数据定位
            eventData.durationFrame = frameCount;

            // 设置事件特有的默认属性
            SetDefaultEventProperties(eventData);

            // 从技能配置同步事件数据
            SyncWithEventConfigData(eventData, itemName);

            return eventData;
        }

        /// <summary>
        /// 获取事件轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetEventTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置事件数据的默认属性
        /// </summary>
        /// <param name="eventData">要设置的事件数据对象</param>
        private void SetDefaultEventProperties(EventTrackItemData eventData)
        {
            // 默认事件参数
            eventData.eventType = eventData.trackItemName;
            eventData.eventParameters = "";
        }

        /// <summary>
        /// 从技能配置同步事件数据
        /// </summary>
        /// <param name="eventData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithEventConfigData(EventTrackItemData eventData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.eventTrack == null)
                return;

            // 查找对应轨道索引的事件轨道
            var targetTrack = skillConfig.trackContainer.eventTrack.eventTracks?
                .FirstOrDefault(track => track.trackIndex == trackIndex);

            if (targetTrack?.eventClips == null)
                return;

            // 查找对应的事件片段配置
            var configClip = targetTrack.eventClips
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复事件属性
                eventData.durationFrame = configClip.durationFrame;
                eventData.eventType = configClip.eventType;
                eventData.eventParameters = configClip.eventParameters;
            }
        }

        #endregion
    }
}
