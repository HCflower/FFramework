using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 特效轨道ScriptableObject
    /// 独立的特效轨道数据文件
    /// </summary>
    [CreateAssetMenu(fileName = "EffectTrack", menuName = "FFramework/Tracks/Effect Track", order = 5)]
    public class EffectTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "Effect Track";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("特效片段列表")]
        public List<EffectTrack.EffectClip> effectClips = new List<EffectTrack.EffectClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
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
        public bool ValidateTrack()
        {
            if (string.IsNullOrEmpty(trackName)) return false;

            foreach (var clip in effectClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public EffectTrack ToRuntimeTrack()
        {
            var track = new EffectTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                effectClips = new List<EffectTrack.EffectClip>(this.effectClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(EffectTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.effectClips = new List<EffectTrack.EffectClip>(track.effectClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Effect Track";
        }
    }

    /// <summary>
    /// 特效轨道 - 支持多轨道并行
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

        [Serializable]
        public class EffectClip : ClipBase
        {
            [Tooltip("特效资源")] public GameObject effectPrefab;

            [Header("Transform")]
            [Tooltip("特效位置")] public Vector3 position = Vector3.zero;
            [Tooltip("特效旋转")] public Vector3 rotation = Vector3.zero;
            [Tooltip("特效缩放")] public Vector3 scale = Vector3.one;
        }
    }
}
