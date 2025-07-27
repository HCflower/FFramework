using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 伤害检测轨道ScriptableObject
    /// 独立的伤害检测轨道数据文件
    /// </summary>
    [CreateAssetMenu(fileName = "InjuryDetectionTrack", menuName = "FFramework/Tracks/Injury Detection Track", order = 7)]
    public class InjuryDetectionTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "Damage Detection";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("伤害检测片段列表")]
        public List<InjuryDetectionTrack.InjuryDetectionClip> injuryDetectionClips = new List<InjuryDetectionTrack.InjuryDetectionClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in injuryDetectionClips)
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

            foreach (var clip in injuryDetectionClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public InjuryDetectionTrack ToRuntimeTrack()
        {
            var track = new InjuryDetectionTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                injuryDetectionClips = new List<InjuryDetectionTrack.InjuryDetectionClip>(this.injuryDetectionClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(InjuryDetectionTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.injuryDetectionClips = new List<InjuryDetectionTrack.InjuryDetectionClip>(track.injuryDetectionClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Damage Detection";
        }
    }

    /// <summary>
    /// 伤害检测轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class InjuryDetectionTrack : TrackBase
    {
        public List<InjuryDetectionClip> injuryDetectionClips = new List<InjuryDetectionClip>();

        public InjuryDetectionTrack()
        {
            trackName = "Damage Detection";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in injuryDetectionClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class InjuryDetectionClip : ClipBase
        {
            [Tooltip("目标层级")] public LayerMask targetLayers = -1;
            [Tooltip("是否是多段伤害检测")] public bool isMultiInjuryDetection = false;
            [Tooltip("多段伤害检测间隔"), Min(0)] public float multiInjuryDetectionInterval = 0.1f;

            [Tooltip("碰撞体类型")] public ColliderType colliderType = ColliderType.Box;
            [Header("Sector collider setting")]
            [Tooltip("扇形内圆半径"), Min(0)] public float innerCircleRadius = 0;
            [Tooltip("扇形外圆半径"), Min(1)] public float outerCircleRadius = 1;
            [Tooltip("扇形角度"), Range(0, 360)] public float sectorAngle = 0;
            [Tooltip("扇形厚度"), Min(0.1f)] public float sectorThickness = 0.1f;

            [Header("Transform")]
            [Tooltip("碰撞体位置")] public Vector3 position = Vector3.zero;
            [Tooltip("碰撞体旋转")] public Vector3 rotation = Vector3.zero;
            [Tooltip("碰撞体缩放")] public Vector3 scale = Vector3.one;

            public override int EndFrame => startFrame + Mathf.Max(1, durationFrame);
        }
    }
}
