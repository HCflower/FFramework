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
        private InjuryDetectionTrackItemData attackTargetData;

        protected override string TrackItemTypeName => "Attack";
        protected override string TrackItemDisplayTitle => "攻击轨道项信息";
        protected override string DeleteButtonText => "删除攻击轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            attackTargetData = target as InjuryDetectionTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 检测设置
            CreateLayerMaskField();
            CreateToggleField("多段伤害检测:", "isMultiInjuryDetection", OnMultiInjuryDetectionChanged);
            CreateFloatField("检测间隔:", "multiInjuryDetectionInterval", OnMultiInjuryDetectionIntervalChanged);

            // Transform设置
            CreateSeparatorTitle("Transform设置");
            CreateVector3Field("碰撞体位置:", "position", OnPositionChanged);
            CreateVector3Field("碰撞体旋转:", "rotation", OnRotationChanged);
            CreateVector3Field("碰撞体缩放:", "scale", OnScaleChanged);

            // 碰撞体设置
            CreateSeparatorTitle("碰撞体设置");
            CreateColliderTypeField();
            // 扇形碰撞体设置
            CreateFloatField("扇形内圆半径:", "innerCircleRadius", OnInnerCircleRadiusChanged);
            CreateFloatField("扇形外圆半径:", "outerCircleRadius", OnOuterCircleRadiusChanged);
            CreateFloatField("扇形角度:", "sectorAngle", OnSectorAngleChanged);
            CreateFloatField("扇形厚度:", "sectorThickness", OnSectorThicknessChanged);
        }

        /// <summary>
        /// 创建LayerMask字段
        /// </summary>
        private void CreateLayerMaskField()
        {
            var layerMaskContent = CreateContentContainer("目标层级:");
            var layerMaskField = new LayerMaskField(attackTargetData.targetLayers);
            layerMaskField.AddToClassList("LayerMaskField");
            layerMaskField.BindProperty(serializedObject.FindProperty("targetLayers"));
            layerMaskField.RegisterValueChangedCallback(evt => OnTargetLayersChanged(evt.newValue));
            layerMaskContent.Add(layerMaskField);
            root.Add(layerMaskContent);
        }

        /// <summary>
        /// 创建碰撞体类型选择字段
        /// </summary>
        private void CreateColliderTypeField()
        {
            var colliderTypeContent = CreateContentContainer("碰撞体类型:");
            var colliderTypeField = new EnumField(attackTargetData.colliderType);
            colliderTypeField.AddToClassList("EnumField");
            colliderTypeField.BindProperty(serializedObject.FindProperty("colliderType"));
            colliderTypeField.RegisterValueChangedCallback(evt => OnColliderTypeChanged((FFramework.Kit.ColliderType)evt.newValue));
            colliderTypeContent.Add(colliderTypeField);
            root.Add(colliderTypeContent);
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除攻击轨道项 \"{attackTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteAttackTrackItem();
            }
        }

        #region 事件处理方法

        private void OnTargetLayersChanged(LayerMask newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.targetLayers = newValue;
                }, "目标层级更新");
            }, "目标层级更新");
        }

        private void OnMultiInjuryDetectionChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.isMultiInjuryDetection = newValue;
                }, "多段伤害检测更新");
            }, "多段伤害检测更新");
        }

        private void OnMultiInjuryDetectionIntervalChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.multiInjuryDetectionInterval = newValue;
                }, "检测间隔更新");
            }, "检测间隔更新");
        }

        private void OnColliderTypeChanged(FFramework.Kit.ColliderType newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.colliderType = newValue;
                }, "碰撞体类型更新");
            }, "碰撞体类型更新");
        }

        private void OnInnerCircleRadiusChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.innerCircleRadius = newValue;
                }, "扇形内圆半径更新");
            }, "扇形内圆半径更新");
        }

        private void OnOuterCircleRadiusChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.outerCircleRadius = newValue;
                }, "扇形外圆半径更新");
            }, "扇形外圆半径更新");
        }

        private void OnSectorAngleChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.sectorAngle = newValue;
                }, "扇形角度更新");
            }, "扇形角度更新");
        }

        private void OnSectorThicknessChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.sectorThickness = newValue;
                }, "扇形厚度更新");
            }, "扇形厚度更新");
        }

        private void OnPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.position = newValue;
                }, "碰撞体位置更新");
            }, "碰撞体位置更新");
        }

        private void OnRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.rotation = newValue;
                }, "碰撞体旋转更新");
            }, "碰撞体旋转更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip =>
                {
                    configClip.scale = newValue;
                }, "碰撞体缩放更新");
            }, "碰撞体缩放更新");
        }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateAttackTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
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
                UpdateAttackTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的攻击配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateAttackTrackConfig(System.Action<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.injuryDetectionTrack == null || attackTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或伤害检测轨道为空");
                return;
            }

            // 使用trackIndex精确定位对应的轨道
            FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip targetConfigClip = null;

            if (skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks != null)
            {
                // 根据trackIndex查找对应的轨道
                var targetTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                    .FirstOrDefault(track => track.trackIndex == attackTargetData.trackIndex);

                if (targetTrack?.injuryDetectionClips != null)
                {
                    // 在指定轨道中查找对应的片段
                    var candidateClips = targetTrack.injuryDetectionClips
                        .Where(clip => clip.clipName == attackTargetData.trackItemName).ToList();

                    if (candidateClips.Count > 0)
                    {
                        if (candidateClips.Count == 1)
                        {
                            targetConfigClip = candidateClips[0];
                        }
                        else
                        {
                            // 如果有多个同名片段，尝试通过起始帧匹配
                            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == attackTargetData.startFrame);
                            targetConfigClip = exactMatch ?? candidateClips[0];
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"无法执行 {operationName}：未找到trackIndex为 {attackTargetData.trackIndex} 的攻击轨道");
                }
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
                // Debug.Log($"{operationName} 成功同步到配置文件 (轨道索引: {attackTargetData.trackIndex})");
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的伤害检测片段配置 (轨道索引: {attackTargetData.trackIndex}, 片段名: {attackTargetData.trackItemName})");
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
                if (skillConfig?.trackContainer?.injuryDetectionTrack == null || attackTargetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或伤害检测轨道为空");
                    return;
                }

                // 根据trackIndex查找对应的攻击轨道并删除片段
                bool deleted = false;
                if (skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks != null)
                {
                    var targetTrack = skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks
                        .FirstOrDefault(track => track.trackIndex == attackTargetData.trackIndex);

                    if (targetTrack?.injuryDetectionClips != null)
                    {
                        // 查找要删除的片段
                        var clipToRemove = targetTrack.injuryDetectionClips.FirstOrDefault(clip =>
                            clip.clipName == attackTargetData.trackItemName &&
                            clip.startFrame == attackTargetData.startFrame);

                        if (clipToRemove != null)
                        {
                            targetTrack.injuryDetectionClips.Remove(clipToRemove);
                            deleted = true;
                            Debug.Log($"从配置中删除攻击片段: {clipToRemove.clipName} (轨道索引: {attackTargetData.trackIndex})");
                        }
                        else
                        {
                            Debug.LogWarning($"未找到要删除的攻击片段: {attackTargetData.trackItemName} (轨道索引: {attackTargetData.trackIndex})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"未找到trackIndex为 {attackTargetData.trackIndex} 的攻击轨道");
                    }
                }

                if (deleted)
                {
                    // 标记配置文件为已修改
                    MarkSkillConfigDirty();
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                // 删除ScriptableObject资产
                if (attackTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(attackTargetData));
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

                Debug.Log($"攻击轨道项 \"{attackTargetData.trackItemName}\" 删除成功");
            }, "删除攻击轨道项");
        }

        #endregion
    }
}