using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 伤害检测轨道实现
    /// 专门处理伤害检测（攻击）的创建、显示和配置管理
    /// </summary>
    public class InjuryDetectionSkillEditorTrack : BaseSkillEditorTrack
    {
        /// <summary>
        /// 伤害检测轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public InjuryDetectionSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.AttackTrack, width, skillConfig, trackIndex)
        { }

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的对象（伤害检测轨道主要通过UI创建，不接受拖拽）
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>始终返回false，伤害检测轨道不接受拖拽</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            // 伤害检测轨道通常不接受拖拽，而是通过UI菜单创建
            return false;
        }

        /// <summary>
        /// 从字符串创建伤害检测轨道项
        /// </summary>
        /// <param name="resource">伤害检测名称字符串</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is string injuryDetectionName))
                return null;

            // 伤害检测轨道项默认5帧长度
            int frameCount = 5;
            var newItem = new SkillEditorTrackItem(trackArea, injuryDetectionName, trackType, frameCount, startFrame);

            // 添加到技能配置
            if (addToConfig)
            {
                AddInjuryDetectionToConfig(injuryDetectionName, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用伤害检测轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 伤害检测轨道特有的样式设置
            // 可以在这里添加伤害检测轨道特有的视觉效果
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建新的伤害检测轨道项
        /// </summary>
        /// <param name="injuryDetectionName">伤害检测名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的伤害检测轨道项</returns>
        public SkillEditorTrackItem CreateInjuryDetectionItem(string injuryDetectionName, int startFrame, bool addToConfig = true)
        {
            return AddTrackItem(injuryDetectionName, startFrame, addToConfig);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将伤害检测添加到技能配置的伤害检测轨道中
        /// </summary>
        /// <param name="injuryDetectionName">伤害检测名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddInjuryDetectionToConfig(string injuryDetectionName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保伤害检测轨道存在
            if (skillConfig.trackContainer.injuryDetectionTrack == null)
            {
                // 创建伤害检测轨道ScriptableObject
                var newInjuryDetectionTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.InjuryDetectionTrackSO>();
                skillConfig.trackContainer.injuryDetectionTrack = newInjuryDetectionTrackSO;

#if UNITY_EDITOR
                // 将ScriptableObject保存为资产文件
                var skillConfigPath = UnityEditor.AssetDatabase.GetAssetPath(skillConfig);
                var configDirectory = System.IO.Path.GetDirectoryName(skillConfigPath);
                var configName = System.IO.Path.GetFileNameWithoutExtension(skillConfigPath);
                var tracksFolder = System.IO.Path.Combine(configDirectory, $"{configName}_Tracks");

                if (!System.IO.Directory.Exists(tracksFolder))
                {
                    System.IO.Directory.CreateDirectory(tracksFolder);
                }

                var assetPath = System.IO.Path.Combine(tracksFolder, $"{configName}_InjuryDetectionTracks.asset");
                assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetPath);

                UnityEditor.AssetDatabase.CreateAsset(newInjuryDetectionTrackSO, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }

            // 获取伤害检测轨道SO
            var injuryDetectionTrackSO = skillConfig.trackContainer.injuryDetectionTrack;

            // 确保至少有一个轨道存在
            injuryDetectionTrackSO.EnsureTrackExists();

            // 确保指定索引的轨道存在
            while (injuryDetectionTrackSO.injuryDetectionTracks.Count <= trackIndex)
            {
                injuryDetectionTrackSO.AddTrack($"Damage Detection Track {injuryDetectionTrackSO.injuryDetectionTracks.Count}");
            }

            // 获取指定索引的伤害检测轨道
            var injuryDetectionTrack = injuryDetectionTrackSO.injuryDetectionTracks[trackIndex];

            // 确保伤害检测片段列表存在
            if (injuryDetectionTrack.injuryDetectionClips == null)
            {
                injuryDetectionTrack.injuryDetectionClips = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip>();
            }

            // 创建技能配置中的伤害检测片段数据
            var configInjuryDetectionClip = new FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip
            {
                clipName = injuryDetectionName,
                startFrame = startFrame,
                durationFrame = frameCount,
                targetLayers = -1,
                isMultiInjuryDetection = false,
                multiInjuryDetectionInterval = 0.1f,
                colliderType = FFramework.Kit.ColliderType.Box,
                innerCircleRadius = 0f,
                outerCircleRadius = 1f,
                sectorAngle = 0f,
                sectorThickness = 0.1f,
                position = Vector3.zero,
                rotation = Vector3.zero,
                scale = Vector3.one
            };

            // 添加到对应索引的伤害检测轨道
            injuryDetectionTrack.injuryDetectionClips.Add(configInjuryDetectionClip);

            Debug.Log($"AddInjuryDetectionToConfig: 添加伤害检测 '{injuryDetectionName}' 到轨道索引 {trackIndex}");

#if UNITY_EDITOR
            // 标记轨道数据和技能配置为已修改
            if (injuryDetectionTrackSO != null)
            {
                EditorUtility.SetDirty(injuryDetectionTrackSO);
            }
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion
    }
}