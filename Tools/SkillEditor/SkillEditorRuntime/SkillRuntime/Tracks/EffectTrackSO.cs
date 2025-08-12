using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 特效轨道集合ScriptableObject
    /// 存储所有特效轨道数据的文件
    /// </summary>
    // [CreateAssetMenu(fileName = "EffectTracks", menuName = "FFramework/Tracks/Effect Tracks", order = 5)]
    public class EffectTrackSO : ScriptableObject
    {
        [Header("特效轨道列表 (多轨道并行)")]
        [Tooltip("所有特效轨道数据列表")]
        public List<EffectTrack> effectTracks = new List<EffectTrack>();

        /// <summary>
        /// 获取所有轨道的最大持续时间
        /// </summary>
        public float GetMaxTrackDuration(float frameRate)
        {
            float maxDuration = 0;
            foreach (var track in effectTracks)
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
            foreach (var track in effectTracks)
            {
                if (!track.ValidateTrack()) return false;
            }
            return true;
        }

        /// <summary>
        /// 获取所有启用的轨道（转换为运行时数据）
        /// </summary>
        public IEnumerable<EffectTrack> GetEnabledTracks()
        {
            foreach (var track in effectTracks)
            {
                if (track.isEnabled)
                    yield return track;
            }
        }

        /// <summary>
        /// 添加新的特效轨道
        /// </summary>
        public EffectTrack AddNewTrack(string trackName = "New Effect Track")
        {
            var newTrack = new EffectTrack
            {
                trackName = trackName,
                isEnabled = true,
                trackIndex = effectTracks.Count
            };
            effectTracks.Add(newTrack);
            return newTrack;
        }

        /// <summary>
        /// 移除指定轨道
        /// </summary>
        public bool RemoveTrack(EffectTrack track)
        {
            return effectTracks.Remove(track);
        }

        /// <summary>
        /// 移除指定索引的轨道
        /// </summary>
        public bool RemoveTrackAt(int index)
        {
            if (index >= 0 && index < effectTracks.Count)
            {
                effectTracks.RemoveAt(index);
                return true;
            }
            return false;
        }

        private void OnValidate()
        {
            // 确保每个轨道都有有效的名称
            for (int i = 0; i < effectTracks.Count; i++)
            {
                if (string.IsNullOrEmpty(effectTracks[i].trackName))
                    effectTracks[i].trackName = $"Effect Track {i + 1}";
            }
        }
    }

    /// <summary>
    /// 特效轨道 - 单个特效轨道的数据
    /// </summary>
    [Serializable]
    public class EffectTrack : TrackBase
    {
        public List<EffectClip> effectClips = new List<EffectClip>();

        public EffectTrack()
        {
            trackName = "Effect Track";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in effectClips)
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

            foreach (var clip in effectClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        [Serializable]
        public class EffectClip : ClipBase
        {
            [Header("特效设置")]
            [Tooltip("特效资源")] public GameObject effectPrefab;
            [Tooltip("特效播放速度")] public float effectPlaySpeed = 1f;
            [Tooltip("是否截断特效")] public bool isCutEffect = false;
            [Tooltip("截断帧偏移")] public int cutEffectFrameOffset = 0;
            [Header("Transform")]
            [Tooltip("特效位置")] public Vector3 position = Vector3.zero;
            [Tooltip("特效旋转")] public Vector3 rotation = Vector3.zero;
            [Tooltip("特效缩放")] public Vector3 scale = Vector3.one;
        }
    }
}
