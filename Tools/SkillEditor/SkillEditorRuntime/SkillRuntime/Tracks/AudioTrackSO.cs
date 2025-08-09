using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频轨道集合ScriptableObject
    /// 存储所有音频轨道数据的文件
    /// </summary>
    // [CreateAssetMenu(fileName = "AudioTracks", menuName = "FFramework/Tracks/Audio Tracks", order = 2)]
    public class AudioTrackSO : ScriptableObject
    {
        [Header("音频轨道列表 (多轨道并行)")]
        [Tooltip("所有音频轨道数据列表")]
        public List<AudioTrack> audioTracks = new List<AudioTrack>();

        /// <summary>
        /// 获取所有轨道的最大持续时间
        /// </summary>
        public float GetMaxTrackDuration(float frameRate)
        {
            float maxDuration = 0;
            foreach (var track in audioTracks)
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
            foreach (var track in audioTracks)
            {
                if (!track.ValidateTrack()) return false;
            }
            return true;
        }

        /// <summary>
        /// 获取所有启用的轨道（转换为运行时数据）
        /// </summary>
        public IEnumerable<AudioTrack> GetEnabledTracks()
        {
            foreach (var track in audioTracks)
            {
                if (track.isEnabled)
                    yield return track;
            }
        }

        /// <summary>
        /// 添加新的音频轨道
        /// </summary>
        public AudioTrack AddNewTrack(string trackName = "New Audio Track")
        {
            var newTrack = new AudioTrack
            {
                trackName = trackName,
                isEnabled = true,
                trackIndex = audioTracks.Count
            };
            audioTracks.Add(newTrack);
            return newTrack;
        }

        /// <summary>
        /// 移除指定轨道
        /// </summary>
        public bool RemoveTrack(AudioTrack track)
        {
            return audioTracks.Remove(track);
        }

        /// <summary>
        /// 移除指定索引的轨道
        /// </summary>
        public bool RemoveTrackAt(int index)
        {
            if (index >= 0 && index < audioTracks.Count)
            {
                audioTracks.RemoveAt(index);
                return true;
            }
            return false;
        }

        private void OnValidate()
        {
            // 确保每个轨道都有有效的名称
            for (int i = 0; i < audioTracks.Count; i++)
            {
                if (string.IsNullOrEmpty(audioTracks[i].trackName))
                    audioTracks[i].trackName = $"Audio Track {i + 1}";
            }
        }
    }

    /// <summary>
    /// 音效轨道 - 单个音频轨道的数据
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

        /// <summary>
        /// 验证轨道数据有效性
        /// </summary>
        public override bool ValidateTrack()
        {
            if (string.IsNullOrEmpty(trackName)) return false;

            foreach (var clip in audioClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        [Serializable]
        public class AudioClip : ClipBase
        {
            public UnityEngine.AudioClip clip;
            [Range(0, 1.0f)] public float volume = 1.0f;
            [Range(0, 3.0f)] public float pitch = 1.0f;
            [Range(0, 1.0f)] public float spatialBlend = 0.0f;             // 空间混合
            [Range(0, 1.0f)] public float reverbZoneMix = 0.0f;            // 反响区域混合
        }
    }
}
