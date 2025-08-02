using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 变换轨道Script(bleObject
    /// 独立的变换轨道数据文件
    /// </summary>
    // [CreateAssetMenu(fileName = "TransformTrack", menuName = "FFramework/Tracks/Transform Track", order = 4)]
    public class TransformTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [ShowOnly][Tooltip("轨道名称")] public string trackName = "Transform";
        [ShowOnly][Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;
        [Tooltip("是否启用轨道")] public bool isEnabled = true;

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
        /// 执行轨道在指定帧的变换
        /// </summary>
        /// <param name="targetTransform">目标Transform组件</param>
        /// <param name="currentFrame">当前帧</param>
        /// <returns>是否执行了变换</returns>
        public bool ExecuteAtFrame(Transform targetTransform, int currentFrame)
        {
            if (!isEnabled || targetTransform == null || transformClips == null || transformClips.Count == 0)
                return false;

            bool executed = false;

            // 遍历所有变换片段，应用当前帧内的变换
            foreach (var clip in transformClips)
            {
                Vector3 position, rotation, scale;
                if (clip.GetTransformAtFrame(currentFrame, out position, out rotation, out scale))
                {
                    // 应用变换
                    if (clip.enablePosition)
                        targetTransform.position = position;

                    if (clip.enableRotation)
                        targetTransform.rotation = Quaternion.Euler(rotation);

                    if (clip.enableScale)
                        targetTransform.localScale = scale;

                    executed = true;
                }
            }

            return executed;
        }

        /// <summary>
        /// 初始化轨道中所有片段的初始变换值
        /// </summary>
        /// <param name="targetTransform">目标Transform组件</param>
        public void InitializeTransforms(Transform targetTransform)
        {
            if (targetTransform == null || transformClips == null)
                return;

            foreach (var clip in transformClips)
            {
                clip.SetInitialTransform(
                    targetTransform.position,
                    targetTransform.rotation.eulerAngles,
                    targetTransform.localScale
                );
            }
        }

        /// <summary>
        /// 获取所有片段在最后一帧的累积变换结果（用于循环累加）
        /// </summary>
        /// <param name="maxFrame">最大帧数</param>
        /// <param name="currentTransform">当前Transform状态</param>
        /// <returns>累积后的Transform状态</returns>
        public (Vector3 position, Vector3 rotation, Vector3 scale) GetAccumulatedTransformAtEnd(int maxFrame, Transform currentTransform)
        {
            Vector3 accumulatedPosition = currentTransform.position;
            Vector3 accumulatedRotation = currentTransform.rotation.eulerAngles;
            Vector3 accumulatedScale = currentTransform.localScale;

            // 计算最后一帧时所有启用片段的累积效果
            foreach (var clip in transformClips)
            {
                if (clip.IsFrameInRange(maxFrame - 1))
                {
                    Vector3 clipPos, clipRot, clipScale;
                    if (clip.GetTransformAtFrame(maxFrame - 1, out clipPos, out clipRot, out clipScale))
                    {
                        if (clip.enablePosition) accumulatedPosition = clipPos;
                        if (clip.enableRotation) accumulatedRotation = clipRot;
                        if (clip.enableScale) accumulatedScale = clipScale;
                    }
                }
            }

            return (accumulatedPosition, accumulatedRotation, accumulatedScale);
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
            [Header("目标变换")]
            [Tooltip("位置偏移量（相对于初始位置）")] public Vector3 positionOffset = Vector3.zero;
            [Tooltip("目标旋转（绝对值）")] public Vector3 targetRotation = Vector3.zero;
            [Tooltip("目标缩放（绝对值）")] public Vector3 targetScale = Vector3.one;

            [Header("动画设置")]
            [Tooltip("动画曲线类型")] public AnimationCurveType curveType = AnimationCurveType.Linear;
            [Tooltip("自定义动画曲线")] public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
            [Header("变换类型")]
            [Tooltip("是否启用位置变换")] public bool enablePosition = true;
            [Tooltip("是否启用旋转变换")] public bool enableRotation = false;
            [Tooltip("是否启用缩放变换")] public bool enableScale = false;

            // 运行时初始变换值（不序列化，每次播放时设置）
            [System.NonSerialized] private Vector3 runtimeStartPosition;
            [System.NonSerialized] private Vector3 runtimeStartRotation;
            [System.NonSerialized] private Vector3 runtimeStartScale;
            [System.NonSerialized] private bool isInitialized = false;

            /// <summary>
            /// 设置初始变换值（通常在开始播放时调用）
            /// </summary>
            /// <param name="currentPosition">当前位置</param>
            /// <param name="currentRotation">当前旋转</param>
            /// <param name="currentScale">当前缩放</param>
            public void SetInitialTransform(Vector3 currentPosition, Vector3 currentRotation, Vector3 currentScale)
            {
                // 存储初始变换值
                runtimeStartPosition = currentPosition;
                runtimeStartRotation = currentRotation;
                runtimeStartScale = currentScale;

                isInitialized = true;
            }

            /// <summary>
            /// 获取实际的目标位置（位置使用加模式）
            /// </summary>
            private Vector3 GetActualEndPosition()
            {
                if (!isInitialized) return positionOffset;
                // 位置总是使用加模式：初始位置 + 偏移量
                return runtimeStartPosition + positionOffset;
            }

            /// <summary>
            /// 获取实际的目标旋转（旋转使用直接变换）
            /// </summary>
            private Vector3 GetActualEndRotation()
            {
                // 旋转使用直接变换：直接使用目标旋转值
                return targetRotation;
            }

            /// <summary>
            /// 获取实际的目标缩放（缩放使用直接变换）
            /// </summary>
            private Vector3 GetActualEndScale()
            {
                // 缩放使用直接变换：直接使用目标缩放值
                return targetScale;
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
            /// 根据时间进度获取插值后的缩放
            /// </summary>
            /// <param name="progress">时间进度 (0-1)</param>
            /// <returns>插值后的缩放</returns>
            public Vector3 GetInterpolatedScale(float progress)
            {
                if (!enableScale) return isInitialized ? runtimeStartScale : Vector3.one;

                Vector3 startScale = isInitialized ? runtimeStartScale : Vector3.one;
                float curveValue = GetCurveValue(progress);
                return Vector3.Lerp(startScale, GetActualEndScale(), curveValue);
            }

            /// <summary>
            /// 应用变换到指定的Transform组件
            /// </summary>
            /// <param name="transform">目标Transform组件</param>
            /// <param name="progress">时间进度 (0-1)</param>
            public void ApplyTransform(Transform transform, float progress)
            {
                if (transform == null) return;

                if (enablePosition)
                {
                    transform.position = GetInterpolatedPosition(progress);
                }

                if (enableRotation)
                {
                    Vector3 rotation = GetInterpolatedRotation(progress);
                    transform.rotation = Quaternion.Euler(rotation);
                }

                if (enableScale)
                {
                    transform.localScale = GetInterpolatedScale(progress);
                }
            }

            /// <summary>
            /// 获取指定帧的变换值
            /// </summary>
            /// <param name="currentFrame">当前帧</param>
            /// <param name="position">输出位置</param>
            /// <param name="rotation">输出旋转</param>
            /// <param name="scale">输出缩放</param>
            /// <returns>是否在片段范围内</returns>
            public bool GetTransformAtFrame(int currentFrame, out Vector3 position, out Vector3 rotation, out Vector3 scale)
            {
                // 默认返回运行时初始值或默认值
                position = isInitialized ? runtimeStartPosition : Vector3.zero;
                rotation = isInitialized ? runtimeStartRotation : Vector3.zero;
                scale = isInitialized ? runtimeStartScale : Vector3.one;

                if (!IsFrameInRange(currentFrame))
                    return false;

                float progress = durationFrame > 0 ? (float)(currentFrame - startFrame) / durationFrame : 0f;
                progress = Mathf.Clamp01(progress);

                position = GetInterpolatedPosition(progress);
                rotation = GetInterpolatedRotation(progress);
                scale = GetInterpolatedScale(progress);

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
