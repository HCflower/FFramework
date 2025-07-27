using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace FFramework.Kit
{
    /// <summary>
    /// 轨道数据迁移工具
    /// 用于将旧的内联轨道数据迁移到新的ScriptableObject格式
    /// </summary>
    public static class TrackDataMigrationTool
    {
        /// <summary>
        /// 迁移技能配置数据到新格式
        /// </summary>
        /// <param name="skillConfig">要迁移的技能配置</param>
        /// <param name="outputFolder">输出文件夹路径</param>
        [MenuItem("FFramework/Tools/Migrate Track Data to ScriptableObjects")]
        public static void MigrateSkillConfigData()
        {
            // 查找所有SkillConfig资产
            var skillConfigs = FindAllSkillConfigs();

            if (skillConfigs.Count == 0)
            {
                Debug.LogWarning("没有找到需要迁移的SkillConfig文件");
                return;
            }

            foreach (var skillConfig in skillConfigs)
            {
                MigrateSkillConfig(skillConfig);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"数据迁移完成，共处理 {skillConfigs.Count} 个技能配置文件");
        }

        /// <summary>
        /// 查找所有SkillConfig资产
        /// </summary>
        private static List<SkillConfig> FindAllSkillConfigs()
        {
            var skillConfigs = new List<SkillConfig>();
            var guids = AssetDatabase.FindAssets("t:SkillConfig");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skillConfig = AssetDatabase.LoadAssetAtPath<SkillConfig>(path);
                if (skillConfig != null)
                {
                    skillConfigs.Add(skillConfig);
                }
            }

            return skillConfigs;
        }

        /// <summary>
        /// 迁移单个技能配置
        /// </summary>
        private static void MigrateSkillConfig(SkillConfig skillConfig)
        {
            var skillConfigPath = AssetDatabase.GetAssetPath(skillConfig);
            var configDirectory = Path.GetDirectoryName(skillConfigPath);
            var configName = Path.GetFileNameWithoutExtension(skillConfigPath);
            var tracksFolder = Path.Combine(configDirectory, $"{configName}_Tracks");

            // 创建轨道文件夹
            if (!Directory.Exists(tracksFolder))
            {
                Directory.CreateDirectory(tracksFolder);
            }

            var container = skillConfig.trackContainer;

            // 迁移动画轨道（如果存在旧数据且新数据为空）
            if (container.animationTrack == null)
            {
                container.animationTrack = CreateAnimationTrackSO(tracksFolder, configName);
            }

            // 迁移摄像机轨道（如果存在旧数据且新数据为空）
            if (container.cameraTrack == null)
            {
                container.cameraTrack = CreateCameraTrackSO(tracksFolder, configName);
            }

            // 标记技能配置为已修改
            EditorUtility.SetDirty(skillConfig);

            Debug.Log($"技能配置 {configName} 迁移完成，轨道文件保存在: {tracksFolder}");
        }

        /// <summary>
        /// 创建动画轨道ScriptableObject
        /// </summary>
        private static AnimationTrackSO CreateAnimationTrackSO(string folder, string configName)
        {
            var animationTrack = ScriptableObject.CreateInstance<AnimationTrackSO>();
            animationTrack.trackName = "Animation";

            var assetPath = Path.Combine(folder, $"{configName}_AnimationTrack.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(animationTrack, assetPath);
            return animationTrack;
        }

        /// <summary>
        /// 创建摄像机轨道ScriptableObject
        /// </summary>
        private static CameraTrackSO CreateCameraTrackSO(string folder, string configName)
        {
            var cameraTrack = ScriptableObject.CreateInstance<CameraTrackSO>();
            cameraTrack.trackName = "Camera";

            var assetPath = Path.Combine(folder, $"{configName}_CameraTrack.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(cameraTrack, assetPath);
            return cameraTrack;
        }

        /// <summary>
        /// 创建音频轨道ScriptableObject
        /// </summary>
        public static AudioTrackSO CreateAudioTrackSO(string folder, string configName, int index = 0)
        {
            var audioTrack = ScriptableObject.CreateInstance<AudioTrackSO>();
            audioTrack.trackName = index == 0 ? "Audio Track" : $"Audio Track {index + 1}";
            audioTrack.trackIndex = index;

            var assetPath = Path.Combine(folder, $"{configName}_AudioTrack_{index}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(audioTrack, assetPath);
            return audioTrack;
        }

        /// <summary>
        /// 创建变换轨道ScriptableObject
        /// </summary>
        public static TransformTrackSO CreateTransformTrackSO(string folder, string configName, int index = 0)
        {
            var transformTrack = ScriptableObject.CreateInstance<TransformTrackSO>();
            transformTrack.trackName = index == 0 ? "Transform" : $"Transform {index + 1}";
            transformTrack.trackIndex = index;

            var assetPath = Path.Combine(folder, $"{configName}_TransformTrack_{index}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(transformTrack, assetPath);
            return transformTrack;
        }

        /// <summary>
        /// 创建特效轨道ScriptableObject
        /// </summary>
        public static EffectTrackSO CreateEffectTrackSO(string folder, string configName, int index = 0)
        {
            var effectTrack = ScriptableObject.CreateInstance<EffectTrackSO>();
            effectTrack.trackName = index == 0 ? "Effect Track" : $"Effect Track {index + 1}";
            effectTrack.trackIndex = index;

            var assetPath = Path.Combine(folder, $"{configName}_EffectTrack_{index}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(effectTrack, assetPath);
            return effectTrack;
        }

        /// <summary>
        /// 创建事件轨道ScriptableObject
        /// </summary>
        public static EventTrackSO CreateEventTrackSO(string folder, string configName, int index = 0)
        {
            var eventTrack = ScriptableObject.CreateInstance<EventTrackSO>();
            eventTrack.trackName = index == 0 ? "Event" : $"Event {index + 1}";
            eventTrack.trackIndex = index;

            var assetPath = Path.Combine(folder, $"{configName}_EventTrack_{index}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(eventTrack, assetPath);
            return eventTrack;
        }

        /// <summary>
        /// 创建伤害检测轨道ScriptableObject
        /// </summary>
        public static InjuryDetectionTrackSO CreateInjuryDetectionTrackSO(string folder, string configName, int index = 0)
        {
            var injuryTrack = ScriptableObject.CreateInstance<InjuryDetectionTrackSO>();
            injuryTrack.trackName = index == 0 ? "Damage Detection" : $"Damage Detection {index + 1}";
            injuryTrack.trackIndex = index;

            var assetPath = Path.Combine(folder, $"{configName}_InjuryDetectionTrack_{index}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(injuryTrack, assetPath);
            return injuryTrack;
        }

        /// <summary>
        /// 创建游戏物体轨道ScriptableObject
        /// </summary>
        public static GameObjectTrackSO CreateGameObjectTrackSO(string folder, string configName, int index = 0)
        {
            var gameObjectTrack = ScriptableObject.CreateInstance<GameObjectTrackSO>();
            gameObjectTrack.trackName = index == 0 ? "GameObject Track" : $"GameObject Track {index + 1}";
            gameObjectTrack.trackIndex = index;

            var assetPath = Path.Combine(folder, $"{configName}_GameObjectTrack_{index}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(gameObjectTrack, assetPath);
            return gameObjectTrack;
        }
    }
}
