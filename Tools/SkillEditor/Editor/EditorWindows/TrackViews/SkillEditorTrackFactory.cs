using UnityEngine.UIElements;
using FFramework.Kit;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道工厂
    /// 负责根据轨道类型创建对应的轨道实例，便于轨道类型的扩展
    /// </summary>
    public static class SkillEditorTrackFactory
    {
        /// <summary>
        /// 创建指定类型的轨道实例
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        /// <returns>创建的轨道实例</returns>
        public static SkillEditorTrackBase CreateTrack(TrackType trackType, VisualElement visual, float width, SkillConfig skillConfig, int trackIndex = 0)
        {
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    return new AnimationSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.AudioTrack:
                    return new AudioSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.EffectTrack:
                    return new EffectSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.EventTrack:
                    return new EventSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.InjuryDetectionTrack:
                    return new InjuryDetectionSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.TransformTrack:
                    return new TransformSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.CameraTrack:
                    return new CameraSkillEditorTrack(visual, width, skillConfig, trackIndex);

                case TrackType.GameObjectTrack:
                    return new GameObjectSkillEditorTrack(visual, width, skillConfig, trackIndex);

                default:
                    UnityEngine.Debug.LogWarning($"未知的轨道类型: {trackType}");
                    return null;
            }
        }

        /// <summary>
        /// 获取轨道类型的显示名称
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <returns>显示名称</returns>
        public static string GetTrackTypeName(TrackType trackType)
        {
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    return "动画轨道";

                case TrackType.AudioTrack:
                    return "音频轨道";

                case TrackType.EffectTrack:
                    return "特效轨道";

                case TrackType.EventTrack:
                    return "事件轨道";

                case TrackType.InjuryDetectionTrack:
                    return "伤害检测轨道";

                case TrackType.TransformTrack:
                    return "变化轨道";

                case TrackType.CameraTrack:
                    return "摄像机轨道";

                case TrackType.GameObjectTrack:
                    return "游戏物体轨道";

                default:
                    return "未知轨道";
            }
        }

        /// <summary>
        /// 检查轨道类型是否支持多轨道
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <returns>是否支持多轨道</returns>
        public static bool IsMultiTrackSupported(TrackType trackType)
        {
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                case TrackType.TransformTrack:
                case TrackType.CameraTrack:
                    return false; // 动画轨道,摄像机轨道,变换轨道只有一个

                case TrackType.AudioTrack:
                case TrackType.EffectTrack:
                case TrackType.EventTrack:
                case TrackType.InjuryDetectionTrack:
                case TrackType.GameObjectTrack:
                    return true; // 这些轨道支持多个

                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取轨道类型的默认轨道名称
        /// </summary>
        /// <param name="trackType">轨道类型</param>
        /// <param name="trackIndex">轨道索引</param>
        /// <returns>默认轨道名称</returns>
        public static string GetDefaultTrackName(TrackType trackType, int trackIndex = 0)
        {
            // 优先查找技能配置数据中的轨道名称
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig != null && skillConfig.trackContainer != null)
            {
                switch (trackType)
                {
                    case TrackType.AnimationTrack:
                        if (skillConfig.trackContainer.animationTrack != null && !string.IsNullOrEmpty(skillConfig.trackContainer.animationTrack.trackName))
                            return skillConfig.trackContainer.animationTrack.trackName;
                        break;
                    case TrackType.CameraTrack:
                        if (skillConfig.trackContainer.cameraTrack != null && !string.IsNullOrEmpty(skillConfig.trackContainer.cameraTrack.trackName))
                            return skillConfig.trackContainer.cameraTrack.trackName;
                        break;
                    case TrackType.TransformTrack:
                        if (skillConfig.trackContainer.transformTrack != null && !string.IsNullOrEmpty(skillConfig.trackContainer.transformTrack.trackName))
                            return skillConfig.trackContainer.transformTrack.trackName;
                        break;
                    case TrackType.AudioTrack:
                        if (skillConfig.trackContainer.audioTrack?.audioTracks != null)
                        {
                            var audioTrack = skillConfig.trackContainer.audioTrack.audioTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (audioTrack != null && !string.IsNullOrEmpty(audioTrack.trackName))
                                return audioTrack.trackName;
                        }
                        break;
                    case TrackType.EffectTrack:
                        if (skillConfig.trackContainer.effectTrack?.effectTracks != null)
                        {
                            var effectTrack = skillConfig.trackContainer.effectTrack.effectTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (effectTrack != null && !string.IsNullOrEmpty(effectTrack.trackName))
                                return effectTrack.trackName;
                        }
                        break;
                    case TrackType.InjuryDetectionTrack:
                        if (skillConfig.trackContainer.injuryDetectionTrack?.injuryDetectionTracks != null)
                        {
                            var injuryTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (injuryTrack != null && !string.IsNullOrEmpty(injuryTrack.trackName))
                                return injuryTrack.trackName;
                        }
                        break;
                    case TrackType.EventTrack:
                        if (skillConfig.trackContainer.eventTrack?.eventTracks != null)
                        {
                            var eventTrack = skillConfig.trackContainer.eventTrack.eventTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (eventTrack != null && !string.IsNullOrEmpty(eventTrack.trackName))
                                return eventTrack.trackName;
                        }
                        break;
                    case TrackType.GameObjectTrack:
                        if (skillConfig.trackContainer.gameObjectTrack?.gameObjectTracks != null)
                        {
                            var goTrack = skillConfig.trackContainer.gameObjectTrack.gameObjectTracks
                                .FirstOrDefault(t => t.trackIndex == trackIndex);
                            if (goTrack != null && !string.IsNullOrEmpty(goTrack.trackName))
                                return goTrack.trackName;
                        }
                        break;
                }
            }

            // 其次查找编辑器数据中的轨道名称
            var trackInfo = SkillEditorData.tracks.FirstOrDefault(t => t.TrackType == trackType && t.TrackIndex == trackIndex);
            if (trackInfo != null && !string.IsNullOrEmpty(trackInfo.TrackName))
            {
                return trackInfo.TrackName;
            }

            // 没有轨道数据则按规则生成
            string baseName = GetTrackTypeName(trackType);

            if (IsMultiTrackSupported(trackType) && trackIndex > 0)
            {
                return $"{baseName} {trackIndex + 1}";
            }

            return baseName;
        }
    }
}
