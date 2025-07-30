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
        // 技能持续时间
        public string Duration => $"{FramesToTime(maxFrames):F2}s";

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
                foreach (var audioTrack in trackContainer.audioTrack.audioTracks)
                {
                    if (audioTrack.isEnabled && audioTrack.audioClips != null)
                    {
                        foreach (var clip in audioTrack.audioClips)
                        {
                            if (frame >= clip.startFrame && frame < clip.startFrame + clip.durationFrame)
                            {
                                info.AppendLine($"  Audio Track '{audioTrack.trackName}': {clip.clipName}");
                            }
                        }
                    }
                }
            }

            // 检查特效轨道的活动片段
            if (trackContainer.effectTrack != null)
            {
                foreach (var effectTrack in trackContainer.effectTrack.effectTracks)
                {
                    if (effectTrack.isEnabled && effectTrack.effectClips != null)
                    {
                        foreach (var clip in effectTrack.effectClips)
                        {
                            if (frame >= clip.startFrame && frame < clip.startFrame + clip.durationFrame)
                            {
                                info.AppendLine($"  Effect Track '{effectTrack.trackName}': {clip.clipName}");
                            }
                        }
                    }
                }
            }

            // 检查事件轨道的活动片段
            if (trackContainer.eventTrack != null)
            {
                foreach (var eventTrack in trackContainer.eventTrack.eventTracks)
                {
                    if (eventTrack.isEnabled && eventTrack.eventClips != null)
                    {
                        foreach (var clip in eventTrack.eventClips)
                        {
                            if (frame >= clip.startFrame && frame < clip.startFrame + clip.durationFrame)
                            {
                                info.AppendLine($"  Event Track '{eventTrack.trackName}': {clip.clipName}");
                            }
                        }
                    }
                }
            }

            // 检查伤害检测轨道的活动片段
            if (trackContainer.injuryDetectionTrack != null)
            {
                foreach (var injuryTrack in trackContainer.injuryDetectionTrack.injuryDetectionTracks)
                {
                    if (injuryTrack.isEnabled && injuryTrack.injuryDetectionClips != null)
                    {
                        foreach (var clip in injuryTrack.injuryDetectionClips)
                        {
                            if (frame >= clip.startFrame && frame < clip.startFrame + clip.durationFrame)
                            {
                                info.AppendLine($"  Injury Detection Track '{injuryTrack.trackName}': {clip.clipName}");
                            }
                        }
                    }
                }
            }

            // 检查游戏物体轨道的活动片段
            if (trackContainer.gameObjectTrack != null)
            {
                foreach (var goTrack in trackContainer.gameObjectTrack.gameObjectTracks)
                {
                    if (goTrack.isEnabled && goTrack.gameObjectClips != null)
                    {
                        foreach (var clip in goTrack.gameObjectClips)
                        {
                            if (frame >= clip.startFrame && frame < clip.startFrame + clip.durationFrame)
                            {
                                info.AppendLine($"  GameObject Track '{goTrack.trackName}': {clip.clipName}");
                            }
                        }
                    }
                }
            }

            return info.ToString();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 清理没有被引用的嵌套子SO文件
        /// </summary>
        [Button("清理多余的轨道配置", "yellow")]
        private void CleanupUnreferencedSubAssets()
        {
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("无法获取技能配置文件路径");
                return;
            }

            // 获取所有子资产
            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var referencedAssets = new HashSet<UnityEngine.Object>();
            var unreferencedAssets = new List<UnityEngine.Object>();

            // 添加主资产（SkillConfig本身）
            referencedAssets.Add(this);

            // 收集所有被引用的轨道SO
            CollectReferencedTrackAssets(referencedAssets);

            // 找出未被引用的子资产
            foreach (var asset in subAssets)
            {
                if (asset != this && !referencedAssets.Contains(asset))
                {
                    unreferencedAssets.Add(asset);
                }
            }

            // 清理未被引用的资产
            int cleanedCount = 0;
            foreach (var unreferencedAsset in unreferencedAssets)
            {
                if (unreferencedAsset != null)
                {
                    Debug.Log($"清理未引用的子资产: {unreferencedAsset.name} ({unreferencedAsset.GetType().Name})");
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(unreferencedAsset);
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
            {
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log($"清理完成，共移除 {cleanedCount} 个未引用的子资产");
            }
            else
            {
                Debug.Log("没有发现未引用的子资产");
            }
        }

        /// <summary>
        /// 收集所有被引用的轨道资产
        /// </summary>
        private void CollectReferencedTrackAssets(HashSet<UnityEngine.Object> referencedAssets)
        {
            if (trackContainer == null) return;

            // 收集所有轨道SO引用
            if (trackContainer.animationTrack != null)
                referencedAssets.Add(trackContainer.animationTrack);

            if (trackContainer.audioTrack != null)
                referencedAssets.Add(trackContainer.audioTrack);

            if (trackContainer.effectTrack != null)
                referencedAssets.Add(trackContainer.effectTrack);

            if (trackContainer.eventTrack != null)
                referencedAssets.Add(trackContainer.eventTrack);

            if (trackContainer.cameraTrack != null)
                referencedAssets.Add(trackContainer.cameraTrack);

            if (trackContainer.transformTrack != null)
                referencedAssets.Add(trackContainer.transformTrack);

            if (trackContainer.gameObjectTrack != null)
                referencedAssets.Add(trackContainer.gameObjectTrack);

            if (trackContainer.injuryDetectionTrack != null)
                referencedAssets.Add(trackContainer.injuryDetectionTrack);
        }
#endif

        #endregion
    }

    #region  轨道

    /// <summary>
    /// 轨道容器 - 统一管理所有轨道引用
    /// 使用ScriptableObject引用的方式，提高数据可读性和模块化
    /// 
    /// 架构说明：
    /// - 单轨道序列：AnimationTrack, CameraTrack - 每个SO文件包含一个轨道的多个片段
    /// - 多轨道并行：AudioTrack, EffectTrack等 - 每个SO文件包含多个独立轨道，每个轨道包含多个片段
    /// </summary>
    [Serializable]
    public class TrackContainer
    {
        [TextLable("动画轨道 (单轨道序列)")][Tooltip("动画轨道数据文件引用")] public AnimationTrackSO animationTrack;
        [TextLable("变换轨道(单轨道序列)")][Tooltip("变换轨道数据文件引用")] public TransformTrackSO transformTrack;
        [TextLable("摄像机轨道(单轨道序列)")][Tooltip("摄像机轨道数据文件引用")] public CameraTrackSO cameraTrack;
        [TextLable("音效轨道 (多轨道并行)")][Tooltip("音效轨道数据文件引用")] public AudioTrackSO audioTrack;
        [TextLable("特效轨道 (多轨道并行)")][Tooltip("特效轨道数据文件引用")] public EffectTrackSO effectTrack;
        [TextLable("伤害检测轨道(多轨道并行)")][Tooltip("伤害检测轨道数据文件引用")] public InjuryDetectionTrackSO injuryDetectionTrack;
        [TextLable("事件轨道(多轨道并行)")][Tooltip("事件轨道数据文件引用")] public EventTrackSO eventTrack;
        [TextLable("游戏物体轨道(多轨道并行)")][Tooltip("游戏物体轨道数据文件引用")] public GameObjectTrackSO gameObjectTrack;

        /// <summary>
        /// 获取所有轨道（转换为运行时数据）
        /// </summary>
        public IEnumerable<TrackBase> GetAllTracks()
        {
            // 单轨道序列
            if (animationTrack != null) yield return animationTrack.ToRuntimeTrack();
            if (cameraTrack != null) yield return cameraTrack.ToRuntimeTrack();

            // 多轨道并行 - 需要遍历所有启用的轨道
            if (audioTrack != null)
            {
                foreach (var track in audioTrack.GetEnabledTracks())
                {
                    yield return track;
                }
            }

            if (effectTrack != null)
            {
                foreach (var track in effectTrack.GetEnabledTracks())
                {
                    yield return track;
                }
            }

            // 多轨道并行 - 遍历所有启用的轨道
            if (injuryDetectionTrack != null)
            {
                foreach (var track in injuryDetectionTrack.injuryDetectionTracks)
                {
                    if (track.isEnabled)
                        yield return track;
                }
            }

            if (eventTrack != null)
            {
                foreach (var track in eventTrack.eventTracks)
                {
                    if (track.isEnabled)
                        yield return track;
                }
            }

            if (gameObjectTrack != null)
            {
                foreach (var track in gameObjectTrack.gameObjectTracks)
                {
                    if (track.isEnabled)
                        yield return track;
                }
            }

            // 注意：以下轨道类型还需要按照相同模式修改其SO文件
            if (transformTrack != null) yield return transformTrack.ToRuntimeTrack();
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
            if (audioTrack != null && !audioTrack.ValidateAllTracks()) return false;
            if (effectTrack != null && !effectTrack.ValidateAllTracks()) return false;
            if (injuryDetectionTrack != null && !injuryDetectionTrack.ValidateAllTracks()) return false;
            if (eventTrack != null && !eventTrack.ValidateAllTracks()) return false;
            if (transformTrack != null && !transformTrack.ValidateTrack()) return false;
            if (gameObjectTrack != null && !gameObjectTrack.ValidateAllTracks()) return false;

            return true;
        }
    }
    #endregion
}
