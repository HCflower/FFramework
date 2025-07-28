using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 变换轨道ScriptableObject
    /// 独立的变换轨道数据文件
    /// </summary>
    // [CreateAssetMenu(fileName = "TransformTrack", menuName = "FFramework/Tracks/Transform Track", order = 4)]
    public class TransformTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "Transform";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("变换片段列表")]
        public List<TransformTrack.TransformClip> transformClips = new List<TransformTrack.TransformClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in transformClips)
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

            foreach (var clip in transformClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public TransformTrack ToRuntimeTrack()
        {
            var track = new TransformTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                transformClips = new List<TransformTrack.TransformClip>(this.transformClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(TransformTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.transformClips = new List<TransformTrack.TransformClip>(track.transformClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Transform";
        }
    }

    /// <summary>
    /// 变换轨道 - 支持多轨道并行
    /// 用于控制对象的位置、旋转、缩放变换动画
    /// </summary>
    [Serializable]
    public class TransformTrack : TrackBase
    {
        public List<TransformClip> transformClips = new List<TransformClip>();

        public TransformTrack()
        {
            trackName = "Transform";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in transformClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class TransformClip : ClipBase
        {
            [Header("变换类型")]
            [Tooltip("是否启用位置变换")] public bool enablePosition = true;
            [Tooltip("是否启用旋转变换")] public bool enableRotation = false;
            [Tooltip("是否启用缩放变换")] public bool enableScale = false;

            [Header("起始变换")]
            [Tooltip("起始位置")] public Vector3 startPosition = Vector3.zero;
            [Tooltip("起始旋转")] public Vector3 startRotation = Vector3.zero;
            [Tooltip("起始缩放")] public Vector3 startScale = Vector3.one;

            [Header("目标变换")]
            [Tooltip("目标位置")] public Vector3 endPosition = Vector3.zero;
            [Tooltip("目标旋转")] public Vector3 endRotation = Vector3.zero;
            [Tooltip("目标缩放")] public Vector3 endScale = Vector3.one;

            [Header("动画设置")]
            [Tooltip("动画曲线类型")] public AnimationCurveType curveType = AnimationCurveType.Linear;
            [Tooltip("自定义动画曲线")] public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
            [Tooltip("是否相对于当前变换")] public bool isRelative = false;

            /// <summary>
            /// 根据时间进度获取插值后的位置
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的位置</returns>
            public Vector3 GetInterpolatedPosition(float progress)
            {
                if (!enablePosition) return startPosition;

                float curveValue = GetCurveValue(progress);
                return Vector3.Lerp(startPosition, endPosition, curveValue);
            }

            /// <summary>
            /// 根据时间进度获取插值后的旋转
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的旋转</returns>
            public Vector3 GetInterpolatedRotation(float progress)
            {
                if (!enableRotation) return startRotation;

                float curveValue = GetCurveValue(progress);
                return Vector3.Lerp(startRotation, endRotation, curveValue);
            }

            /// <summary>
            /// 根据时间进度获取插值后的缩放
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的缩放</returns>
            public Vector3 GetInterpolatedScale(float progress)
            {
                if (!enableScale) return startScale;

                float curveValue = GetCurveValue(progress);
                return Vector3.Lerp(startScale, endScale, curveValue);
            }

            /// <summary>
            /// 根据曲线类型和进度获取曲线值
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>曲线值</returns>
            private float GetCurveValue(float progress)
            {
                switch (curveType)
                {
                    case AnimationCurveType.Linear:
                        return progress;
                    case AnimationCurveType.EaseIn:
                        return progress * progress;
                    case AnimationCurveType.EaseOut:
                        return 1f - (1f - progress) * (1f - progress);
                    case AnimationCurveType.EaseInOut:
                        return progress < 0.5f ? 2f * progress * progress : 1f - 2f * (1f - progress) * (1f - progress);
                    case AnimationCurveType.Custom:
                        return customCurve.Evaluate(progress);
                    default:
                        return progress;
                }
            }
        }
    }

    /// <summary>
    /// 动画曲线类型
    /// </summary>
    public enum AnimationCurveType
    {
        [Tooltip("线性插值")] Linear,
        [Tooltip("缓入")] EaseIn,
        [Tooltip("缓出")] EaseOut,
        [Tooltip("缓入缓出")] EaseInOut,
        [Tooltip("自定义曲线")] Custom
    }
}
