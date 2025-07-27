using System;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 轨道基类
    /// </summary>
    [Serializable]
    public abstract class TrackBase
    {
        [Tooltip("轨道名称")] public string trackName;
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        public abstract float GetTrackDuration(float frameRate);

        /// <summary>
        /// 验证轨道数据有效性
        /// </summary>
        public virtual bool ValidateTrack()
        {
            return !string.IsNullOrEmpty(trackName);
        }
    }

    /// <summary>
    /// 片段基类
    /// </summary>
    [Serializable]
    public abstract class ClipBase
    {
        [Tooltip("片段名称")] public string clipName;
        [Tooltip("起始帧"), Min(0)] public int startFrame;
        [Tooltip("持续帧数(-1表示完整播放)"), Min(-1)] public int durationFrame = -1;

        public virtual int EndFrame => startFrame + (durationFrame > 0 ? durationFrame : 0);

        /// <summary>
        /// 判断指定帧是否在片段范围内
        /// </summary>
        public virtual bool IsFrameInRange(int frame)
        {
            return frame >= startFrame && frame <= EndFrame;
        }

        /// <summary>
        /// 验证片段数据有效性
        /// </summary>
        public virtual bool ValidateClip()
        {
            return !string.IsNullOrEmpty(clipName) && startFrame >= 0;
        }
    }
}
