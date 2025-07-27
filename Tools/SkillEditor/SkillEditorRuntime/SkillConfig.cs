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

            // 检查音频轨道的活动片段
            if (trackContainer.audioTrack != null)
            {
                foreach (var clip in trackContainer.audioTrack.audioClips)
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
    /// 轨道容器 - 统一管理所有轨道引用
    /// 使用ScriptableObject引用的方式，提高数据可读性和模块化
    /// 每个轨道类型最多只有一个SO文件，List数据存储在SO文件内部
    /// </summary>
    [Serializable]
    public class TrackContainer
    {
        [Header("动画轨道 (单轨道序列)")]
        [Tooltip("动画轨道数据文件引用")] public AnimationTrackSO animationTrack;

        [Header("音效轨道 (多轨道并行)")]
        [Tooltip("音效轨道数据文件引用")] public AudioTrackSO audioTrack;

        [Header("特效轨道 (多轨道并行)")]
        [Tooltip("特效轨道数据文件引用")] public EffectTrackSO effectTrack;

        [Header("伤害检测轨道(多轨道并行)")]
        [Tooltip("伤害检测轨道数据文件引用")] public InjuryDetectionTrackSO injuryDetectionTrack;

        [Header("事件轨道(多轨道并行)")]
        [Tooltip("事件轨道数据文件引用")] public EventTrackSO eventTrack;

        [Header("变换轨道(多轨道并行)")]
        [Tooltip("变换轨道数据文件引用")] public TransformTrackSO transformTrack;

        [Header("摄像机轨道(单轨道)")]
        [Tooltip("摄像机轨道数据文件引用")] public CameraTrackSO cameraTrack;

        [Header("游戏物体轨道(多轨道并行)")]
        [Tooltip("游戏物体轨道数据文件引用")] public GameObjectTrackSO gameObjectTrack;

        /// <summary>
        /// 获取所有轨道（转换为运行时数据）
        /// </summary>
        public IEnumerable<TrackBase> GetAllTracks()
        {
            if (animationTrack != null) yield return animationTrack.ToRuntimeTrack();
            if (audioTrack != null) yield return audioTrack.ToRuntimeTrack();
            if (effectTrack != null) yield return effectTrack.ToRuntimeTrack();
            if (injuryDetectionTrack != null) yield return injuryDetectionTrack.ToRuntimeTrack();
            if (eventTrack != null) yield return eventTrack.ToRuntimeTrack();
            if (transformTrack != null) yield return transformTrack.ToRuntimeTrack();
            if (cameraTrack != null) yield return cameraTrack.ToRuntimeTrack();
            if (gameObjectTrack != null) yield return gameObjectTrack.ToRuntimeTrack();
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
        /// 添加新轨道ScriptableObject
        /// </summary>
        public T AddTrackSO<T>() where T : ScriptableObject
        {
            var track = ScriptableObject.CreateInstance<T>();

            if (track is AudioTrackSO audioTrackSO)
                audioTrack = audioTrackSO;
            else if (track is EffectTrackSO effectTrackSO)
                effectTrack = effectTrackSO;
            else if (track is InjuryDetectionTrackSO injuryTrackSO)
                injuryDetectionTrack = injuryTrackSO;
            else if (track is EventTrackSO eventTrackSO)
                eventTrack = eventTrackSO;
            else if (track is TransformTrackSO transformTrackSO)
                transformTrack = transformTrackSO;
            else if (track is CameraTrackSO cameraTrackSO)
                cameraTrack = cameraTrackSO;
            else if (track is GameObjectTrackSO gameObjectTrackSO)
                gameObjectTrack = gameObjectTrackSO;
            else if (track is AnimationTrackSO animationTrackSO)
                animationTrack = animationTrackSO;

            return track;
        }

        /// <summary>
        /// 验证所有轨道数据
        /// </summary>
        public bool ValidateAllTracks()
        {
            // 验证动画轨道
            if (animationTrack != null && !animationTrack.ValidateTrack())
                return false;

            // 验证摄像机轨道
            if (cameraTrack != null && !cameraTrack.ValidateTrack())
                return false;

            // 验证其他轨道
            if (audioTrack != null && !audioTrack.ValidateTrack()) return false;
            if (effectTrack != null && !effectTrack.ValidateTrack()) return false;
            if (injuryDetectionTrack != null && !injuryDetectionTrack.ValidateTrack()) return false;
            if (eventTrack != null && !eventTrack.ValidateTrack()) return false;
            if (transformTrack != null && !transformTrack.ValidateTrack()) return false;
            if (gameObjectTrack != null && !gameObjectTrack.ValidateTrack()) return false;

            return true;
        }

        /// <summary>
        /// 同步所有轨道数据（从运行时数据同步到ScriptableObject）
        /// </summary>
        public void SyncFromRuntimeData(TrackContainer runtimeContainer)
        {
            // 这个方法可以用于从旧格式迁移数据
            // 具体实现可以根据需要添加
        }
    }
    #endregion
}
