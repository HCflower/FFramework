using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频轨道ScriptableObject
    /// 独立的音频轨道数据文件
    /// </summary>
    [CreateAssetMenu(fileName = "AudioTrack", menuName = "FFramework/Tracks/Audio Track", order = 2)]
    public class AudioTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "Audio Track";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("音频片段列表")]
        public List<AudioTrack.AudioClip> audioClips = new List<AudioTrack.AudioClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in audioClips)
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

            foreach (var clip in audioClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public AudioTrack ToRuntimeTrack()
        {
            var track = new AudioTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                audioClips = new List<AudioTrack.AudioClip>(this.audioClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(AudioTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.audioClips = new List<AudioTrack.AudioClip>(track.audioClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Audio Track";
        }
    }

    /// <summary>
    /// 音效轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class AudioTrack : TrackBase
    {
        public List<AudioClip> audioClips = new List<AudioClip>();

        public AudioTrack()
        {
            trackName = "Audio Track";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in audioClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class AudioClip : ClipBase
        {
            public UnityEngine.AudioClip clip;
            public float volume = 1.0f;
            public float pitch = 1.0f;
            public bool isLoop = false;
        }
    }
}
