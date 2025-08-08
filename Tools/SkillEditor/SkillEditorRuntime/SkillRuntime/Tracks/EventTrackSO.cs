using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 事件轨道集合ScriptableObject
    /// 存储所有事件轨道数据的文件
    /// </summary>
    // [CreateAssetMenu(fileName = "EventTracks", menuName = "FFramework/Tracks/Event Tracks", order = 6)]
    public class EventTrackSO : ScriptableObject
    {
        [Header("事件轨道列表 (多轨道并行)")]
        [Tooltip("所有事件轨道数据列表")]
        public List<EventTrack> eventTracks = new List<EventTrack>();

        /// <summary>
        /// 获取所有轨道的最大持续时间
        /// </summary>
        public float GetMaxTrackDuration(float frameRate)
        {
            float maxDuration = 0;
            foreach (var track in eventTracks)
            {
                if (track.isEnabled)
                    maxDuration = Mathf.Max(maxDuration, track.GetTrackDuration(frameRate));
            }
            return maxDuration;
        }

        /// <summary>
        /// 验证所有轨道数据有效性
        /// </summary>
        public bool ValidateAllTracks()
        {
            foreach (var track in eventTracks)
            {
                if (!track.ValidateTrack()) return false;
            }
            return true;
        }

        /// <summary>
        /// 添加新的事件轨道
        /// </summary>
        public EventTrack AddTrack(string trackName = "")
        {
            var newTrack = new EventTrack();
            if (!string.IsNullOrEmpty(trackName))
                newTrack.trackName = trackName;
            else
                newTrack.trackName = $"Event Track {eventTracks.Count}";

            newTrack.trackIndex = eventTracks.Count;
            eventTracks.Add(newTrack);
            return newTrack;
        }

        /// <summary>
        /// 移除指定索引的轨道
        /// </summary>
        public bool RemoveTrack(int index)
        {
            if (index >= 0 && index < eventTracks.Count)
            {
                eventTracks.RemoveAt(index);
                // 重新分配索引
                for (int i = 0; i < eventTracks.Count; i++)
                {
                    eventTracks[i].trackIndex = i;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 确保至少有一个轨道
        /// </summary>
        public void EnsureTrackExists()
        {
            if (eventTracks.Count == 0)
            {
                AddTrack();
            }
        }

        private void OnValidate()
        {
            EnsureTrackExists();
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

        /// <summary>
        /// 验证轨道数据有效性
        /// </summary>
        public override bool ValidateTrack()
        {
            if (string.IsNullOrEmpty(trackName)) return false;

            foreach (var clip in eventClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        [Serializable]
        public class EventClip : ClipBase
        {
            // TODO: 实现事件注册
            [Header("事件参数")]
            [Tooltip("事件类型")] public string eventType;
            [Tooltip("事件参数")] public string eventParameters;

            public override int EndFrame => startFrame; // 事件是瞬时的

            /// <summary>
            /// 验证事件片段数据有效性
            /// </summary>
            public override bool ValidateClip()
            {
                return !string.IsNullOrEmpty(clipName) && !string.IsNullOrEmpty(eventType);
            }
        }
    }
}
