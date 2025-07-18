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

        [Header("动画轨道 (单轨道序列)")]
        public AnimationTrack animationTrack = new AnimationTrack();

        [Header("音效轨道 (多轨道并行)")]
        public List<AudioTrack> audioTracks = new List<AudioTrack>();

        [Header("特效轨道 (多轨道并行)")]
        public List<EffectTrack> effectTracks = new List<EffectTrack>();

        [Header("伤害检测轨道(多轨道并行)")]
        public List<InjuryDetectionTrack> injuryDetectionTracks = new List<InjuryDetectionTrack>();

        // [Header("事件轨道(多轨道并行)")]
        // public List<EventTrack> eventTracks = new List<EventTrack>();

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
            foreach (var track in audioTracks)
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
    /// 动画轨道 - 单轨道，动画按顺序播放
    /// </summary>
    [Serializable]
    public class AnimationTrack
    {
        [Tooltip("轨道名称")] public string trackName = "Animation";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        public List<AnimationClip> animationClips = new List<AnimationClip>();

        [Serializable]
        public class AnimationClip
        {
            [Tooltip("动画名称")] public string clipName;
            [Tooltip("动画片段")] public UnityEngine.AnimationClip clip;
            [Tooltip("动画起始帧"), Min(0)] public int startFrame;
            [Tooltip("动画持续帧数(-1表示完整播放)"), Min(-1)] public int durationFrame = -1;
            [Tooltip("动画播放速度"), Min(0)] public float playSpeed = 1.0f;
            [Tooltip("动画是否循环播放")] public bool loop = false;
            [Tooltip("是否应用动画根运动")] public bool applyRootMotion = false;
        }
    }

    /// <summary>
    /// 音效轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class AudioTrack
    {
        [Tooltip("轨道名称")] public string trackName = "Audio Track";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;
        public List<AudioClip> audioClips = new List<AudioClip>();

        [Serializable]
        public class AudioClip
        {
            [Tooltip("音效名称")] public string clipName;
            public UnityEngine.AudioClip clip;
            [Tooltip("音效起始帧"), Min(0)] public int startFrame;
            [Tooltip("音频持续帧数(-1表示完整播放)"), Min(-1)] public int durationFrame = -1;
            public float volume = 1.0f;
            public float pitch = 1.0f;
            public bool loop = false;
        }
    }

    /// <summary>
    /// 特效轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class EffectTrack
    {
        [Tooltip("轨道名称")] public string trackName = "Effect Track";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;
        public List<EffectClip> effectClips = new List<EffectClip>();

        [Serializable]
        public class EffectClip
        {
            [Tooltip("特效名称")] public string clipName;
            [Tooltip("特效资源")] public GameObject effectPrefab;
            [Tooltip("特效起始帧"), Min(0)] public int startFrame;
            [Tooltip("特效持续帧数(-1表示完整播放)"), Min(-1)] public int durationFrame = -1;

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
    public class InjuryDetectionTrack
    {
        [Tooltip("轨道名称")] public string trackName = "Damage Detection";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        public List<InjuryDetectionClip> injuryDetectionClips = new List<InjuryDetectionClip>();

        [Serializable]
        public class InjuryDetectionClip
        {
            [Tooltip("伤害检测片段名称")] public string clipName;
            [Tooltip("伤害检测起始帧"), Min(0)] public int startFrame;
            [Tooltip("伤害检测持续帧数"), Min(1)] public int durationFrame = 1;
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
        }
    }

    /// <summary>
    /// 事件轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class EventTrack
    {
        [Tooltip("轨道名称")] public string trackName = "Event";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        public List<EventClip> eventClips = new List<EventClip>();

        [Serializable]
        public class EventClip
        {
            [Tooltip("事件名称")] public string clipName;
            [Tooltip("事件起始帧"), Min(1)] public int startFrame;
            // TODO:实现事件注册
        }
    }

    #endregion


}
