using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(EffectTrackItemData))]
    public class EffectTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private EffectTrackItemData effectTargetData;

        protected override string TrackItemTypeName => "Effect";
        protected override string TrackItemDisplayTitle => "特效轨道项信息";
        protected override string DeleteButtonText => "删除特效轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            effectTargetData = target as EffectTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 特效预制体字段
            CreateObjectField<GameObject>("特效预制体:", "effectPrefab", OnEffectPrefabChanged);

            // Transform 设置
            CreateVector3Field("特效位置:", "position", OnPositionChanged);
            CreateVector3Field("特效旋转:", "rotation", OnRotationChanged);
            CreateVector3Field("特效缩放:", "scale", OnScaleChanged);
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除特效轨道项 \"{effectTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteEffectTrackItem();
            }
        }

        /// <summary>
        /// 删除特效轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteEffectTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.effectTrack == null || effectTargetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或特效轨道为空");
                    return;
                }

                // 根据trackIndex查找对应的特效轨道并删除片段
                bool deleted = false;
                if (skillConfig.trackContainer.effectTrack.effectTracks != null)
                {
                    var targetTrack = skillConfig.trackContainer.effectTrack.effectTracks
                        .FirstOrDefault(track => track.trackIndex == effectTargetData.trackIndex);

                    if (targetTrack?.effectClips != null)
                    {
                        // 查找要删除的片段
                        var clipToRemove = targetTrack.effectClips.FirstOrDefault(clip =>
                            clip.clipName == effectTargetData.trackItemName &&
                            clip.startFrame == effectTargetData.startFrame);

                        if (clipToRemove != null)
                        {
                            targetTrack.effectClips.Remove(clipToRemove);
                            deleted = true;
                            Debug.Log($"从配置中删除特效片段: {clipToRemove.clipName} (轨道索引: {effectTargetData.trackIndex})");
                        }
                        else
                        {
                            Debug.LogWarning($"未找到要删除的特效片段: {effectTargetData.trackItemName} (轨道索引: {effectTargetData.trackIndex})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"未找到trackIndex为 {effectTargetData.trackIndex} 的特效轨道");
                    }
                }

                if (deleted)
                {
                    // 标记配置文件为已修改
                    MarkSkillConfigDirty();
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                // 删除ScriptableObject资产
                if (effectTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(effectTargetData));
                }

                // 清空Inspector选择
                UnityEditor.Selection.activeObject = null;

                // 触发界面刷新以移除UI元素
                var window = UnityEditor.EditorWindow.GetWindow<SkillEditor>();
                if (window != null)
                {
                    // 使用EditorApplication.delayCall确保在下一帧执行刷新
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        window.Repaint();

                        // 通过反射获取skillEditorEvent实例并触发刷新
                        var skillEditorEventField = typeof(SkillEditor).GetField("skillEditorEvent",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (skillEditorEventField != null)
                        {
                            var skillEditorEvent = skillEditorEventField.GetValue(window) as SkillEditorEvent;
                            skillEditorEvent?.TriggerRefreshRequested();
                        }
                    };
                }

                Debug.Log($"特效轨道项 \"{effectTargetData.trackItemName}\" 删除成功");
            }, "删除特效轨道项");
        }

        #region 事件处理方法

        private void OnEffectPrefabChanged(GameObject newPrefab)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip =>
                {
                    configClip.effectPrefab = newPrefab;
                }, "特效预制体更新");
            }, "特效预制体更新");
        }

        private void OnPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip =>
                {
                    configClip.position = newValue;
                }, "特效位置更新");
            }, "特效位置更新");
        }

        private void OnRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip =>
                {
                    configClip.rotation = newValue;
                }, "特效旋转更新");
            }, "特效旋转更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip =>
                {
                    configClip.scale = newValue;
                }, "特效缩放更新");
            }, "特效缩放更新");
        }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
            }, "起始帧更新");
        }

        /// <summary>
        /// 持续帧数变化事件处理
        /// </summary>
        /// <param name="newValue">新的持续帧数值</param>
        protected override void OnDurationFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的特效配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateEffectTrackConfig(System.Action<FFramework.Kit.EffectTrack.EffectClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.effectTrack == null || effectTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或特效轨道为空");
                return;
            }

            // 使用trackIndex精确定位对应的轨道
            FFramework.Kit.EffectTrack.EffectClip targetConfigClip = null;

            if (skillConfig.trackContainer.effectTrack.effectTracks != null)
            {
                // 根据trackIndex查找对应的轨道
                var targetTrack = skillConfig.trackContainer.effectTrack.effectTracks
                    .FirstOrDefault(track => track.trackIndex == effectTargetData.trackIndex);

                if (targetTrack?.effectClips != null)
                {
                    // 在指定轨道中查找对应的片段
                    var candidateClips = targetTrack.effectClips
                        .Where(clip => clip.clipName == effectTargetData.trackItemName).ToList();

                    if (candidateClips.Count > 0)
                    {
                        if (candidateClips.Count == 1)
                        {
                            targetConfigClip = candidateClips[0];
                        }
                        else
                        {
                            // 如果有多个同名片段，尝试通过起始帧匹配
                            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == effectTargetData.startFrame);
                            targetConfigClip = exactMatch ?? candidateClips[0];
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"无法执行 {operationName}：未找到trackIndex为 {effectTargetData.trackIndex} 的特效轨道");
                }
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
                // Debug.Log($"{operationName} 成功同步到配置文件 (轨道索引: {effectTargetData.trackIndex})");
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的特效片段配置 (轨道索引: {effectTargetData.trackIndex}, 片段名: {effectTargetData.trackItemName})");
            }
        }

        #endregion
    }
}
