using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 事件轨道ScriptableObject
    /// 独立的事件轨道数据文件
    /// </summary>
    [CreateAssetMenu(fileName = "EventTrack", menuName = "FFramework/Tracks/Event Track", order = 6)]
    public class EventTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "Event";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("事件片段列表")]
        public List<EventTrack.EventClip> eventClips = new List<EventTrack.EventClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in eventClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        /// <summary>
        /// 验证轨道数据有效性
        /// </summary>
        public bool ValidateTrack()
        {
            if (string.IsNullOrEmpty(trackName)) return false;

            foreach (var clip in eventClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public EventTrack ToRuntimeTrack()
        {
            var track = new EventTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                eventClips = new List<EventTrack.EventClip>(this.eventClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(EventTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.eventClips = new List<EventTrack.EventClip>(track.eventClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Event";
        }
    }

    /// <summary>
    /// 事件轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class EventTrack : TrackBase
    {
        public List<EventClip> eventClips = new List<EventClip>();

        public EventTrack()
        {
            trackName = "Event";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in eventClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class EventClip : ClipBase
        {
            // TODO: 实现事件注册
            [Header("事件参数")]
            [Tooltip("事件类型")] public string eventType;
            [Tooltip("事件参数")] public string eventParameters;

            public override int EndFrame => startFrame; // 事件是瞬时的
        }
    }
}
