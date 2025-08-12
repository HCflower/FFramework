using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(InjuryDetectionTrackItemData))]
    public class InjuryDetectionTrackItemDataInspector : BaseTrackItemDataInspector
    {
        protected override string TrackItemTypeName => "Attack";
        protected override string TrackItemDisplayTitle => "攻击轨道项信息";
        protected override string DeleteButtonText => "删除攻击轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as InjuryDetectionTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 检测设置
            CreateSeparatorTitle("碰撞检测设置");
            CreateObjectField<GameObject>("Hit特效预制体:", "hitEffectPrefab", OnHitEffectPrefabChanged);
            CreateLayerMaskField();
            CreateToggleField("启用所有检测组:", "enableAllCollisionGroups", OnEnableAllColliderChanged);
            CreateIntegerField("碰撞检测组ID:", "collisionGroupId", OnCollisionGroupIdChanged);
        }

        /// <summary>
        /// 创建LayerMask字段
        /// </summary>
        private void CreateLayerMaskField()
        {
            var layerMaskContent = CreateContentContainer("目标层级:");
            var injuryData = targetData as InjuryDetectionTrackItemData;
            var layerMaskField = new LayerMaskField(injuryData.targetLayers);
            layerMaskField.AddToClassList("LayerMaskField");
            layerMaskField.BindProperty(serializedObject.FindProperty("targetLayers"));
            layerMaskField.RegisterValueChangedCallback(evt => OnTargetLayersChanged(evt.newValue));
            layerMaskContent.Add(layerMaskField);
            root.Add(layerMaskContent);
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除攻击轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteAttackTrackItem();
            }
        }

        #region 事件处理方法

        protected override void OnTrackItemNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                // 使用保存的旧名称
                string oldName = lastTrackItemName ?? targetData.trackItemName;

                // 先更新配置中的名称（使用旧名称查找）
                UpdateTrackConfigByName(oldName, configClip => configClip.clipName = newValue, "轨道项名称更新");

                // 更新保存的名称
                lastTrackItemName = newValue;
            }, "轨道项名称更新");
        }

        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
            }, "起始帧更新");
        }

        protected override void OnDurationFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        private void OnTargetLayersChanged(LayerMask newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip =>
                {
                    configClip.targetLayers = newValue;
                }, "目标层级更新");
            }, "目标层级更新");
        }

        private void OnHitEffectPrefabChanged(GameObject newPrefab)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => { configClip.hitEffectPrefab = newPrefab; }, "击中特效预制体更新");
            }, "击中特效预制体更新");
        }

        private void OnEnableAllColliderChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.enableAllCollisionGroups = newValue, "启用所有碰撞体更新");
            }, "启用所有碰撞体更新");
        }


        private void OnCollisionGroupIdChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.collisionGroupId = newValue, "碰撞检测组ID更新");
            }, "碰撞检测组ID更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfig(System.Action<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 根据指定名称查找并更新攻击配置数据
        /// </summary>
        /// <param name="clipName">要查找的片段名称</param>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.injuryDetectionTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或伤害检测轨道为空");
                return;
            }

            // 使用trackIndex精确定位轨道，只通过名称唯一查找
            FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip targetConfigClip = null;
            if (skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks != null)
            {
                var targetTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                    .FirstOrDefault(track => track.trackIndex == targetData.trackIndex);

                if (targetTrack.injuryDetectionClips != null)
                {
                    targetConfigClip = targetTrack.injuryDetectionClips
                        .FirstOrDefault(clip => clip.clipName == clipName);
                }
                else
                {
                    Debug.LogWarning($"无法执行 {operationName}：未找到trackIndex为 {targetData.trackIndex} 的攻击轨道");
                }
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的伤害检测片段配置 (轨道索引: {targetData.trackIndex}, 片段名: {clipName})");
            }
        }

        /// <summary>
        /// 删除攻击轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteAttackTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.injuryDetectionTrack == null || targetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或伤害检测轨道为空");
                    return;
                }

                // 根据trackIndex查找对应的攻击轨道并删除片段
                bool deleted = false;
                if (skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks != null)
                {
                    var targetTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                        .FirstOrDefault(track => track.trackIndex == targetData.trackIndex);

                    if (targetTrack?.injuryDetectionClips != null)
                    {
                        // 查找要删除的片段
                        var clipToRemove = targetTrack.injuryDetectionClips.FirstOrDefault(clip =>
                            clip.clipName == targetData.trackItemName &&
                            clip.startFrame == targetData.startFrame);

                        if (clipToRemove != null)
                        {
                            targetTrack.injuryDetectionClips.Remove(clipToRemove);
                            deleted = true;
                            Debug.Log($"从配置中删除攻击片段: {clipToRemove.clipName} (轨道索引: {targetData.trackIndex})");
                        }
                        else
                        {
                            Debug.LogWarning($"未找到要删除的攻击片段: {targetData.trackItemName} (轨道索引: {targetData.trackIndex})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"未找到trackIndex为 {targetData.trackIndex} 的攻击轨道");
                    }
                }

                if (deleted)
                {
                    // 标记配置文件为已修改
                    MarkSkillConfigDirty();
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                // 删除ScriptableObject资产
                if (targetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(targetData));
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

                        // 直接调用静态事件方法
                        SkillEditorEvent.TriggerRefreshRequested();
                    };
                }

                Debug.Log($"攻击轨道项 \"{targetData.trackItemName}\" 删除成功");
            }, "删除攻击轨道项");
        }

        #endregion
    }
}