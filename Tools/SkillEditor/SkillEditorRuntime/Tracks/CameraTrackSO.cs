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
        /// 执行轨道在指定帧的摄像机控制
        /// </summary>
        /// <param name="targetCamera">目标Camera组件</param>
        /// <param name="currentFrame">当前帧</param>
        /// <returns>是否执行了摄像机控制</returns>
        public bool ExecuteAtFrame(Camera targetCamera, int currentFrame)
        {
            if (!isEnabled || targetCamera == null || cameraClips == null || cameraClips.Count == 0)
                return false;

            bool executed = false;

            // 遍历所有摄像机片段，应用当前帧内的摄像机状态
            foreach (var clip in cameraClips)
            {
                Vector3 position, rotation;
                float fieldOfView;
                if (clip.GetCameraAtFrame(currentFrame, out position, out rotation, out fieldOfView))
                {
                    // 应用摄像机状态
                    if (clip.enablePosition)
                        targetCamera.transform.position = position;

                    if (clip.enableRotation)
                        targetCamera.transform.rotation = Quaternion.Euler(rotation);

                    if (clip.enableFieldOfView)
                        targetCamera.fieldOfView = fieldOfView;

                    executed = true;
                }
            }

            return executed;
        }

        /// <summary>
        /// 初始化轨道中所有片段的初始摄像机状态
        /// </summary>
        /// <param name="targetCamera">目标Camera组件</param>
        public void InitializeCameras(Camera targetCamera)
        {
            if (targetCamera == null || cameraClips == null)
                return;

            foreach (var clip in cameraClips)
            {
                clip.SetInitialCamera(
                    targetCamera.transform.position,
                    targetCamera.transform.rotation.eulerAngles,
                    targetCamera.fieldOfView
                );
            }
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

            [Header("目标状态")]
            [Tooltip("位置偏移量（相对于初始位置）")] public Vector3 positionOffset = Vector3.zero;
            [Tooltip("目标旋转（绝对值）")] public Vector3 targetRotation = Vector3.zero;
            [Tooltip("目标视野角度（绝对值）")] public float targetFieldOfView = 60f;

            [Header("动画设置")]
            [Tooltip("动画曲线类型")] public AnimationCurveType curveType = AnimationCurveType.Linear;
            [Tooltip("自定义动画曲线")] public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);

            // 运行时初始摄像机状态（不序列化，每次播放时设置）
            [System.NonSerialized] private Vector3 runtimeStartPosition;
            [System.NonSerialized] private Vector3 runtimeStartRotation;
            [System.NonSerialized] private float runtimeStartFieldOfView;
            [System.NonSerialized] private bool isInitialized = false;

            /// <summary>
            /// 设置初始摄像机状态（通常在开始播放时调用）
            /// </summary>
            /// <param name="currentPosition">当前位置</param>
            /// <param name="currentRotation">当前旋转</param>
            /// <param name="currentFieldOfView">当前视野角度</param>
            public void SetInitialCamera(Vector3 currentPosition, Vector3 currentRotation, float currentFieldOfView)
            {
                runtimeStartPosition = currentPosition;
                runtimeStartRotation = currentRotation;
                runtimeStartFieldOfView = currentFieldOfView;
                isInitialized = true;
            }

            /// <summary>
            /// 获取实际的目标位置（位置使用加模式）
            /// </summary>
            private Vector3 GetActualEndPosition()
            {
                if (!isInitialized) return positionOffset;
                return runtimeStartPosition + positionOffset;
            }

            /// <summary>
            /// 获取实际的目标旋转（旋转使用直接变换）
            /// </summary>
            private Vector3 GetActualEndRotation()
            {
                return targetRotation;
            }

            /// <summary>
            /// 获取实际的目标视野角度（视野使用直接变换）
            /// </summary>
            private float GetActualEndFieldOfView()
            {
                return targetFieldOfView;
            }

            /// <summary>
            /// 根据时间进度获取插值后的位置
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的位置</returns>
            public Vector3 GetInterpolatedPosition(float progress)
            {
                if (!enablePosition) return isInitialized ? runtimeStartPosition : Vector3.zero;

                Vector3 startPos = isInitialized ? runtimeStartPosition : Vector3.zero;
                float curveValue = GetCurveValue(progress);
                return Vector3.Lerp(startPos, GetActualEndPosition(), curveValue);
            }

            /// <summary>
            /// 根据时间进度获取插值后的旋转
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的旋转</returns>
            public Vector3 GetInterpolatedRotation(float progress)
            {
                if (!enableRotation) return isInitialized ? runtimeStartRotation : Vector3.zero;

                Vector3 startRot = isInitialized ? runtimeStartRotation : Vector3.zero;
                float curveValue = GetCurveValue(progress);
                return Vector3.Lerp(startRot, GetActualEndRotation(), curveValue);
            }

            /// <summary>
            /// 根据时间进度获取插值后的视野角度
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的视野角度</returns>
            public float GetInterpolatedFieldOfView(float progress)
            {
                if (!enableFieldOfView) return isInitialized ? runtimeStartFieldOfView : 60f;

                float startFov = isInitialized ? runtimeStartFieldOfView : 60f;
                float curveValue = GetCurveValue(progress);
                return Mathf.Lerp(startFov, GetActualEndFieldOfView(), curveValue);
            }

            /// <summary>
            /// 应用摄像机状态到指定的Camera组件
            /// </summary>
            /// <param name="camera">目标Camera组件</param>
            /// <param name="progress">时间进度 (0-1)</param>
            public void ApplyCamera(Camera camera, float progress)
            {
                if (camera == null) return;

                if (enablePosition)
                {
                    camera.transform.position = GetInterpolatedPosition(progress);
                }

                if (enableRotation)
                {
                    Vector3 rotation = GetInterpolatedRotation(progress);
                    camera.transform.rotation = Quaternion.Euler(rotation);
                }

                if (enableFieldOfView)
                {
                    camera.fieldOfView = GetInterpolatedFieldOfView(progress);
                }
            }

            /// <summary>
            /// 获取指定帧的摄像机状态
            /// </summary>
            /// <param name="currentFrame">当前帧</param>
            /// <param name="position">输出位置</param>
            /// <param name="rotation">输出旋转</param>
            /// <param name="fieldOfView">输出视野角度</param>
            /// <returns>是否在片段范围内</returns>
            public bool GetCameraAtFrame(int currentFrame, out Vector3 position, out Vector3 rotation, out float fieldOfView)
            {
                // 默认返回运行时初始值或默认值
                position = isInitialized ? runtimeStartPosition : Vector3.zero;
                rotation = isInitialized ? runtimeStartRotation : Vector3.zero;
                fieldOfView = isInitialized ? runtimeStartFieldOfView : 60f;

                if (!IsFrameInRange(currentFrame))
                    return false;

                float progress = durationFrame > 0 ? (float)(currentFrame - startFrame) / durationFrame : 0f;
                progress = Mathf.Clamp01(progress);

                position = GetInterpolatedPosition(progress);
                rotation = GetInterpolatedRotation(progress);
                fieldOfView = GetInterpolatedFieldOfView(progress);

                return true;
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
