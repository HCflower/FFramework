using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 事件轨道实现
    /// 专门处理事件的创建、显示和配置管理
    /// </summary>
    public class EventSkillEditorTrack : SkillEditorTrackBase
    {
        #region 构造函数

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

        #endregion

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的对象（事件轨道主要通过UI创建，不接受拖拽）
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>始终返回false，事件轨道不接受拖拽</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return false; // 事件轨道不接受拖拽
        }

        /// <summary>
        /// 从字符串创建事件轨道项
        /// </summary>
        /// <param name="resource">事件名称字符串</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is string eventName))
                return null;

            var newItem = CreateEventTrackItem(eventName, startFrame, 1, addToConfig);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(eventName, startFrame, 1);
                SkillEditorEvent.OnRefreshRequested();
            }

            return newItem;
        }

        /// <summary>
        /// 应用事件轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 事件轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-Event");
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
        public EventTrackItem CreateEventTrackItem(string eventName, int startFrame, int frameCount, bool addToConfig = true)
        {
            var eventItem = new EventTrackItem(trackArea, eventName, frameCount, startFrame, trackIndex);

            trackItems.Add(eventItem);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(eventName, startFrame, frameCount);
                SkillEditorEvent.OnRefreshRequested();
            }

            return eventItem;
        }

        /// <summary>
        /// 刷新轨道项
        /// </summary>
        public override void RefreshTrackItems()
        {
            base.RefreshTrackItems();
        }

        /// <summary>
        /// 根据索引从配置创建事件轨道项
        /// </summary>
        /// <param name="track">事件轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(EventSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var eventTrack = skillConfig.trackContainer.eventTrack;
            if (eventTrack?.eventTracks == null) return;

            // 根据索引获取对应的轨道数据
            var targetTrack = eventTrack.eventTracks.FirstOrDefault(t => t.trackIndex == trackIndex);
            if (targetTrack?.eventClips == null) return;

            foreach (var clip in targetTrack.eventClips)
            {
                var trackItem = track.CreateEventTrackItem(clip.clipName, clip.startFrame, clip.durationFrame, false);

                if (trackItem is EventTrackItem eventTrackItem)
                {
                    RestoreEventData(eventTrackItem, clip);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将事件添加到技能配置的事件轨道中
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddTrackItemDataToConfig(string eventName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            EnsureEventTrackExists();

            var eventTrackSO = skillConfig.trackContainer.eventTrack;

            while (eventTrackSO.eventTracks.Count <= trackIndex)
            {
                eventTrackSO.AddTrack($"Event Track {eventTrackSO.eventTracks.Count}");
            }

            var eventTrack = eventTrackSO.eventTracks[trackIndex];

            if (eventTrack.eventClips == null)
            {
                eventTrack.eventClips = new List<FFramework.Kit.EventTrack.EventClip>();
            }

            string finalName = eventName;
            int suffix = 1;
            while (eventTrack.eventClips.Any(c => c.clipName == finalName))
            {
                finalName = $"{eventName}_{suffix++}";
            }

            var configEventClip = new FFramework.Kit.EventTrack.EventClip
            {
                clipName = finalName,
                startFrame = startFrame,
                durationFrame = frameCount,
                eventType = finalName,
                eventParameters = ""
            };

            eventTrack.eventClips.Add(configEventClip);

#if UNITY_EDITOR
            EditorUtility.SetDirty(eventTrackSO);
            EditorUtility.SetDirty(skillConfig);
#endif
        }

        /// <summary>
        /// 确保事件轨道存在
        /// </summary>
        private void EnsureEventTrackExists()
        {
            if (skillConfig.trackContainer.eventTrack != null) return;

            var newEventTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.EventTrackSO>();
            skillConfig.trackContainer.eventTrack = newEventTrackSO;

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(newEventTrackSO, skillConfig);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// 从配置恢复事件数据
        /// </summary>
        /// <param name="trackItem">轨道项</param>
        /// <param name="clip">事件片段</param>
        private static void RestoreEventData(EventTrackItem trackItem, FFramework.Kit.EventTrack.EventClip clip)
        {
            if (trackItem?.EventData == null) return;

            var eventData = trackItem.EventData;
            eventData.trackItemName = clip.clipName;
            eventData.durationFrame = clip.durationFrame;
            eventData.eventType = clip.eventType;
            eventData.eventParameters = clip.eventParameters;

#if UNITY_EDITOR
            EditorUtility.SetDirty(eventData);
#endif
        }

        #endregion
    }
}
