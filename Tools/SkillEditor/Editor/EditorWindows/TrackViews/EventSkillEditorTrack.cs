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
        protected override BaseTrackItemView CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is string eventName))
                return null;

            // 事件轨道项默认1帧长度
            int frameCount = 1;
            var newItem = new EventTrackItem(trackArea, eventName, frameCount, startFrame, trackIndex);

            // 添加到基类的轨道项列表，确保在时间轴缩放时能够被刷新
            trackItems.Add(newItem);

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
        public EventTrackItem CreateEventItem(string eventName, int startFrame, bool addToConfig = true)
        {
            return (EventTrackItem)AddTrackItem(eventName, startFrame, addToConfig);
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

            // 确保事件轨道存在
            if (skillConfig.trackContainer.eventTrack == null)
            {
                // 创建事件轨道ScriptableObject
                var newEventTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.EventTrackSO>();
                skillConfig.trackContainer.eventTrack = newEventTrackSO;

#if UNITY_EDITOR
                // 将ScriptableObject作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newEventTrackSO, skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }

            // 获取事件轨道SO
            var eventTrackSO = skillConfig.trackContainer.eventTrack;

            // 确保至少有一个轨道存在
            eventTrackSO.EnsureTrackExists();

            // 确保指定索引的轨道存在
            while (eventTrackSO.eventTracks.Count <= trackIndex)
            {
                eventTrackSO.AddTrack($"Event Track {eventTrackSO.eventTracks.Count}");
            }

            // 获取指定索引的事件轨道
            var eventTrack = eventTrackSO.eventTracks[trackIndex];

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
            // 标记轨道数据和技能配置为已修改
            if (eventTrackSO != null)
            {
                EditorUtility.SetDirty(eventTrackSO);
            }
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion

        #region 配置恢复方法

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
                // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                // 更新轨道项的持续帧数和相关数据
                if (trackItem is EventTrackItem eventTrackItem)
                {
                    var eventData = eventTrackItem.EventData;
                    eventData.durationFrame = clip.durationFrame;
                    // 从配置中恢复完整的事件属性
                    eventData.eventType = clip.eventType;
                    eventData.eventParameters = clip.eventParameters;

#if UNITY_EDITOR
                    // 标记数据已修改
                    UnityEditor.EditorUtility.SetDirty(eventData);
#endif
                }

                // 更新轨道项的帧数和宽度显示
                if (clip.durationFrame > 0)
                {
                    trackItem?.UpdateFrameCount(clip.durationFrame);
                }
            }
        }

        #endregion
    }
}
