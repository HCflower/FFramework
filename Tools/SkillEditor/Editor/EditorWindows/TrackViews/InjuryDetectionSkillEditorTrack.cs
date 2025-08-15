using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 伤害检测轨道实现
    /// 专门处理伤害检测（攻击）的创建、显示和配置管理
    /// </summary>
    public class InjuryDetectionSkillEditorTrack : SkillEditorTrackBase
    {
        #region 私有字段

        // 私有字段可以在这里声明

        #endregion

        #region 构造函数

        /// <summary>
        /// 伤害检测轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public InjuryDetectionSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.InjuryDetectionTrack, width, skillConfig, trackIndex)
        { }

        #endregion

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
        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is string injuryDetectionName))
                return null;

            // 伤害检测轨道项默认5帧长度
            int frameCount = 5;

            // 添加到技能配置
            if (addToConfig)
            {
                AddTrackItemDataToConfig(injuryDetectionName, startFrame, frameCount);
                SkillEditorEvent.OnRefreshRequested();
            }

            var newItem = new InjuryDetectionTrackItem(trackArea, injuryDetectionName, frameCount, startFrame, trackIndex);
            trackItems.Add(newItem);
            return newItem;
        }

        /// <summary>
        /// 应用伤害检测轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 伤害检测轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-Attack");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 根据索引从配置创建伤害检测轨道项
        /// </summary>
        /// <param name="track">伤害检测轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(InjuryDetectionSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var injuryTrack = skillConfig.trackContainer.injuryDetectionTrack;
            if (injuryTrack?.injuryDetectionTracks == null) return;

            // 根据索引获取对应的轨道数据
            var targetTrack = injuryTrack.injuryDetectionTracks.FirstOrDefault(t => t.trackIndex == trackIndex);
            if (targetTrack?.injuryDetectionClips == null) return;

            foreach (var clip in targetTrack.injuryDetectionClips)
            {
                // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                // 更新轨道项的持续帧数和相关数据
                if (trackItem is InjuryDetectionTrackItem injuryDetectionTrackItem)
                {
                    var attackData = injuryDetectionTrackItem.InjuryDetectionData;
                    attackData.durationFrame = clip.durationFrame;
                    // 从配置中恢复完整的攻击属性
                    attackData.targetLayers = clip.targetLayers;
                    attackData.enableAllCollisionGroups = clip.enableAllCollisionGroups;
                    attackData.collisionGroupId = clip.collisionGroupId;
                    attackData.injuryDetectionEventName = clip.injuryDetectionEventName;
#if UNITY_EDITOR
                    // 标记数据已修改
                    UnityEditor.EditorUtility.SetDirty(attackData);
#endif
                }

                // 更新轨道项的帧数和宽度显示
                if (clip.durationFrame > 0)
                {
                    trackItem?.UpdateFrameCount(clip.durationFrame);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将伤害检测添加到技能配置的伤害检测轨道中
        /// </summary>
        /// <param name="trackItemName">伤害检测名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddTrackItemDataToConfig(string trackItemName, int startFrame, int frameCount)
        {
            // 检查名称是否已存在，如果存在则添加后缀
            string finalName = trackItemName;
            if (skillConfig?.trackContainer?.injuryDetectionTrack != null)
            {
                int suffix = 1;
                while (skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                    .Any(track => track.injuryDetectionClips?
                        .Any(clip => clip.clipName == finalName) == true))
                {
                    finalName = $"{trackItemName}_{suffix++}";
                }
            }
            if (skillConfig?.trackContainer == null) return;

            // 确保伤害检测轨道存在
            if (skillConfig.trackContainer.injuryDetectionTrack == null)
            {
                // 创建伤害检测轨道ScriptableObject
                var newInjuryDetectionTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.InjuryDetectionTrackSO>();
                skillConfig.trackContainer.injuryDetectionTrack = newInjuryDetectionTrackSO;

#if UNITY_EDITOR
                // 将ScriptableObject作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newInjuryDetectionTrackSO, skillConfig);
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
                injuryDetectionTrack.injuryDetectionClips = new List<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip>();
            }

            // 创建技能配置中的伤害检测片段数据
            var configInjuryDetectionClip = new FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip
            {
                clipName = finalName,
                startFrame = startFrame,
                durationFrame = frameCount,
                targetLayers = -1,
                enableAllCollisionGroups = false,
                collisionGroupId = 0,
                injuryDetectionEventName = "OnInjuryDetection",
            };

            // 添加到对应索引的伤害检测轨道
            injuryDetectionTrack.injuryDetectionClips.Add(configInjuryDetectionClip);

            Debug.Log($"AddInjuryDetectionToConfig: 添加伤害检测 '{finalName}' 到轨道索引 {trackIndex}");

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