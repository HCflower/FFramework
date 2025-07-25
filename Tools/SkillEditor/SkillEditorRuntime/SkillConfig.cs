using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能编辑器配置
    /// </summary>
    [CreateAssetMenu(fileName = nameof(SkillConfig), menuName = "FFramework/SkillConfig", order = 3)]
    public class SkillConfig : ScriptableObject
    {
        [Header("时间轴设置")]
        [Tooltip("帧率"), Min(1)] public float frameRate = 30;
        [Tooltip("技能最大帧数"), Min(1)] public int maxFrames = 60;

        [Header("技能基础信息")]
        [Tooltip("技能拥有者")] public GameObject owner;
        [Tooltip("技能图标")] public Sprite skillIcon;
        [Tooltip("技能名称")] public string skillName;
        [Tooltip("技能ID")] public int skillId;
        [TextArea(3, 5)]
        [Tooltip("技能描述")] public string description;

        [Header("技能参数")]
        [Tooltip("冷却时间")] public float cooldown = 1.0f;

        [Header("轨道管理")]
        public TrackContainer trackContainer = new TrackContainer();

        #region 编辑器辅助方法

        /// <summary>
        /// 帧转换为时间
        /// </summary>
        public float FramesToTime(int frames)
        {
            return (float)frames / frameRate;
        }

        /// <summary>
        /// 时间转换为帧
        /// </summary>
        public int TimeToFrames(float time)
        {
            return Mathf.RoundToInt(time * frameRate);
        }

        /// <summary>
        /// 获取指定帧的所有活动片段信息（用于编辑器预览）
        /// </summary>
        public string GetFrameInfo(int frame)
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Frame {frame} ({FramesToTime(frame):F2}s):");

            // 检查各轨道的活动片段
            foreach (var track in trackContainer.audioTracks)
            {
                foreach (var clip in track.audioClips)
                {
                    if (frame >= clip.startFrame && frame < clip.startFrame + clip.durationFrame)
                    {
                        info.AppendLine($"  Audio: {clip.clipName}");
                    }
                }
            }

            return info.ToString();
        }

        #endregion
    }


    #region 枚举参数

    // 碰撞体类型
    public enum ColliderType
    {
        Box = 0,        // 立方体
        Sphere = 1,     // 球体
        Capsule = 2,    // 胶囊体   
        sector = 3,     // 扇形
    }


    #endregion

    #region  轨道

    /// <summary>
    /// 轨道容器 - 统一管理所有轨道
    /// </summary>
    [Serializable]
    public class TrackContainer
    {
        [Header("动画轨道 (单轨道序列)")]
        public AnimationTrack animationTrack = new AnimationTrack();

        [Header("音效轨道 (多轨道并行)")]
        public List<AudioTrack> audioTracks = new List<AudioTrack>();

        [Header("特效轨道 (多轨道并行)")]
        public List<EffectTrack> effectTracks = new List<EffectTrack>();

        [Header("伤害检测轨道(多轨道并行)")]
        public List<InjuryDetectionTrack> injuryDetectionTracks = new List<InjuryDetectionTrack>();

        [Header("事件轨道(多轨道并行)")]
        public List<EventTrack> eventTracks = new List<EventTrack>();

        [Header("变换轨道(多轨道并行)")]
        public List<TransformTrack> transformTracks = new List<TransformTrack>();

        [Header("摄像机轨道(单轨道)")]
        public CameraTrack cameraTrack;

        [Header("游戏物体轨道(多轨道并行)")]
        public List<GameObjectTrack> gameObjectTracks = new List<GameObjectTrack>();

        /// <summary>
        /// 获取所有轨道
        /// </summary>
        public IEnumerable<TrackBase> GetAllTracks()
        {
            yield return animationTrack;
            foreach (var track in audioTracks) yield return track;
            foreach (var track in effectTracks) yield return track;
            foreach (var track in injuryDetectionTracks) yield return track;
            foreach (var track in eventTracks) yield return track;
            foreach (var track in transformTracks) yield return track;
            yield return cameraTrack;
            foreach (var track in gameObjectTracks) yield return track;
        }

        /// <summary>
        /// 获取技能总时长
        /// </summary>
        public float GetTotalDuration(float frameRate)
        {
            float maxDuration = 0;
            foreach (var track in GetAllTracks())
            {
                if (track.isEnabled)
                    maxDuration = Mathf.Max(maxDuration, track.GetTrackDuration(frameRate));
            }
            return maxDuration;
        }

        /// <summary>
        /// 添加新轨道
        /// </summary>
        public T AddTrack<T>() where T : TrackBase, new()
        {
            var track = new T();
            if (track is AudioTrack audioTrack)
                audioTracks.Add(audioTrack);
            else if (track is EffectTrack effectTrack)
                effectTracks.Add(effectTrack);
            else if (track is InjuryDetectionTrack injuryTrack)
                injuryDetectionTracks.Add(injuryTrack);
            else if (track is EventTrack eventTrack)
                eventTracks.Add(eventTrack);
            else if (track is TransformTrack transformTrack)
                transformTracks.Add(transformTrack);
            else if (track is CameraTrack cameraTrackItem)
                cameraTrack = cameraTrackItem;

            return track;
        }

        /// <summary>
        /// 验证所有轨道数据
        /// </summary>
        public bool ValidateAllTracks()
        {
            foreach (var track in GetAllTracks())
            {
                if (!track.ValidateTrack())
                    return false;
            }
            return true;
        }
    }

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

    /// <summary>
    /// 事件轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class EventTrack : TrackBase
    {
        public List<EventClip> eventClips = new List<EventClip>();

        public EventTrack()
        {
            trackName = "Event";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in eventClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class EventClip : ClipBase
        {
            // TODO: 实现事件注册
            [Header("事件参数")]
            [Tooltip("事件类型")] public string eventType;
            [Tooltip("事件参数")] public string eventParameters;

            public override int EndFrame => startFrame; // 事件是瞬时的
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

    /// <summary>
    /// 游戏物体轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class GameObjectTrack : TrackBase
    {
        public List<GameObjectClip> gameObjectClips = new List<GameObjectClip>();

        public GameObjectTrack()
        {
            trackName = "GameObject Track";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in gameObjectClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        [Serializable]
        public class GameObjectClip : ClipBase
        {
            [Header("游戏物体设置")]
            public GameObject prefab;
            [Tooltip("是否自动销毁")] public bool autoDestroy = true;
            [Tooltip("生成位置偏移")] public Vector3 positionOffset = Vector3.zero;
            [Tooltip("生成旋转偏移")] public Vector3 rotationOffset = Vector3.zero;
            [Tooltip("生成缩放")] public Vector3 scale = Vector3.one;

            [Header("父对象设置")]
            [Tooltip("是否作为子对象")] public bool useParent = false;
            [Tooltip("父对象名称")] public string parentName = "";

            [Header("生命周期设置")]
            [Tooltip("延迟销毁时间(秒), -1表示不销毁")] public float destroyDelay = -1f;
        }
    }

    #endregion


}
