using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 摄像机轨道ScriptableObject
    /// 独立的摄像机轨道数据文件
    /// </summary>
    // [CreateAssetMenu(fileName = "CameraTrack", menuName = "FFramework/Tracks/Camera Track", order = 3)]
    public class CameraTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [ShowOnly][Tooltip("轨道名称")] public string trackName = "Camera";
        [ShowOnly][Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;
        [Tooltip("是否启用轨道")] public bool isEnabled = true;

        [Header("摄像机片段列表")]
        public List<CameraTrack.CameraClip> cameraClips = new List<CameraTrack.CameraClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in cameraClips)
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

            foreach (var clip in cameraClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public CameraTrack ToRuntimeTrack()
        {
            var track = new CameraTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                cameraClips = new List<CameraTrack.CameraClip>(this.cameraClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(CameraTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.cameraClips = new List<CameraTrack.CameraClip>(track.cameraClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "Camera";
        }
    }

    /// <summary>
    /// 摄像机轨道 - 控制摄像机的移动、旋转和视野变化
    /// </summary>
    [Serializable]
    public class CameraTrack : TrackBase
    {
        public List<CameraClip> cameraClips = new List<CameraClip>();

        public CameraTrack()
        {
            trackName = "Camera";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in cameraClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class CameraClip : ClipBase
        {
            [Header("摄像机类型")]
            [Tooltip("是否启用位置变换")] public bool enablePosition = true;
            [Tooltip("是否启用旋转变换")] public bool enableRotation = true;
            [Tooltip("是否启用视野变换")] public bool enableFieldOfView = false;

            [Header("起始状态")]
            [Tooltip("起始位置")] public Vector3 startPosition = Vector3.zero;
            [Tooltip("起始旋转")] public Vector3 startRotation = Vector3.zero;
            [Tooltip("起始视野角度")] public float startFieldOfView = 60f;

            [Header("目标状态")]
            [Tooltip("目标位置")] public Vector3 endPosition = Vector3.zero;
            [Tooltip("目标旋转")] public Vector3 endRotation = Vector3.zero;
            [Tooltip("目标视野角度")] public float endFieldOfView = 60f;

            [Header("动画设置")]
            [Tooltip("动画曲线类型")] public AnimationCurveType curveType = AnimationCurveType.Linear;
            [Tooltip("自定义动画曲线")] public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
            [Tooltip("是否相对于当前状态")] public bool isRelative = false;

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
            /// 根据时间进度获取插值后的视野角度
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的视野角度</returns>
            public float GetInterpolatedFieldOfView(float progress)
            {
                if (!enableFieldOfView) return startFieldOfView;

                float curveValue = GetCurveValue(progress);
                return Mathf.Lerp(startFieldOfView, endFieldOfView, curveValue);
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
}
