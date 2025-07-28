using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 动画轨道ScriptableObject
    /// 独立的动画轨道数据文件
    /// </summary>
    // [CreateAssetMenu(fileName = "AnimationTrack", menuName = "FFramework/Tracks/Animation Track", order = 1)]
    public class AnimationTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "Animation";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("动画片段列表")]
        public List<AnimationTrack.AnimationClip> animationClips = new List<AnimationTrack.AnimationClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in animationClips)
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

            foreach (var clip in animationClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public AnimationTrack ToRuntimeTrack()
        {
            var track = new AnimationTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                animationClips = new List<AnimationTrack.AnimationClip>(this.animationClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(AnimationTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.animationClips = new List<AnimationTrack.AnimationClip>(track.animationClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Animation";
        }
    }

    /// <summary>
    /// 动画轨道 - 单轨道，动画按顺序播放
    /// </summary>
    [Serializable]
    public class AnimationTrack : TrackBase
    {
        public List<AnimationClip> animationClips = new List<AnimationClip>();

        public AnimationTrack()
        {
            trackName = "Animation";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in animationClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class AnimationClip : ClipBase
        {
            [Tooltip("动画片段")] public UnityEngine.AnimationClip clip;
            [Tooltip("动画播放速度"), Min(0)] public float playSpeed = 1.0f;
            [Tooltip("动画是否循环播放")] public bool isLoop = false;
            [Tooltip("是否应用动画根运动")] public bool applyRootMotion = false;
        }
    }
}
