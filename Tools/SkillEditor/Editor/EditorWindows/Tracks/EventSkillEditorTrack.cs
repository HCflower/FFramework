using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 事件轨道实现
    /// 专门处理事件的创建、显示和配置管理
    /// </summary>
    public class EventSkillEditorTrack : BaseSkillEditorTrack
    {
        /// <summary>
        /// 事件轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public EventSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.EventTrack, width, skillConfig, trackIndex)
        { }

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的对象（事件轨道主要通过UI创建，不接受拖拽）
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>始终返回false，事件轨道不接受拖拽</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            // 事件轨道通常不接受拖拽，而是通过UI菜单创建
            return false;
        }

        /// <summary>
        /// 从字符串创建事件轨道项
        /// </summary>
        /// <param name="resource">事件名称字符串</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is string eventName))
                return null;

            // 事件轨道项默认1帧长度
            int frameCount = 1;
            var newItem = new SkillEditorTrackItem(trackArea, eventName, trackType, frameCount, startFrame);

            // 添加到技能配置
            if (addToConfig)
            {
                AddEventToConfig(eventName, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用事件轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 事件轨道特有的样式设置
            // 可以在这里添加事件轨道特有的视觉效果
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建新的事件轨道项
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的事件轨道项</returns>
        public SkillEditorTrackItem CreateEventItem(string eventName, int startFrame, bool addToConfig = true)
        {
            return AddTrackItem(eventName, startFrame, addToConfig);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将事件添加到技能配置的事件轨道中
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddEventToConfig(string eventName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保事件轨道列表存在
            if (skillConfig.trackContainer.eventTracks == null)
            {
                skillConfig.trackContainer.eventTracks = new System.Collections.Generic.List<FFramework.Kit.EventTrack>();
            }

            // 确保有足够的轨道数据，如果不足则创建
            while (skillConfig.trackContainer.eventTracks.Count <= trackIndex)
            {
                var newEventTrack = new FFramework.Kit.EventTrack();
                // 使用当前列表长度作为新轨道的索引来生成名称
                int currentTrackIndex = skillConfig.trackContainer.eventTracks.Count;
                string factoryTrackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.EventTrack, currentTrackIndex);
                newEventTrack.trackName = factoryTrackName;
                newEventTrack.eventClips = new System.Collections.Generic.List<FFramework.Kit.EventTrack.EventClip>();
                skillConfig.trackContainer.eventTracks.Add(newEventTrack);
            }

            // 获取对应索引的事件轨道
            var eventTrack = skillConfig.trackContainer.eventTracks[trackIndex];

            // 确保事件片段列表存在
            if (eventTrack.eventClips == null)
            {
                eventTrack.eventClips = new System.Collections.Generic.List<FFramework.Kit.EventTrack.EventClip>();
            }

            // 创建技能配置中的事件片段数据
            var configEventClip = new FFramework.Kit.EventTrack.EventClip
            {
                clipName = eventName,
                startFrame = startFrame,
                durationFrame = frameCount,
                eventType = eventName,
                eventParameters = ""
            };

            // 添加到对应索引的事件轨道
            eventTrack.eventClips.Add(configEventClip);

            Debug.Log($"AddEventToConfig: 添加事件 '{eventName}' 到轨道索引 {trackIndex}");

#if UNITY_EDITOR
            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion
    }
}
