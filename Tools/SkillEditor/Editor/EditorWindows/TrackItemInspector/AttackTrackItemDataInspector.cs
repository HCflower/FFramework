using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(AttackTrackItemData))]
    public class AttackTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private AttackTrackItemData attackTargetData;

        protected override string TrackItemTypeName => "Attack";
        protected override string TrackItemDisplayTitle => "攻击轨道项信息";
        protected override string DeleteButtonText => "删除攻击轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            attackTargetData = target as AttackTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 检测设置
            CreateLayerMaskField();
            CreateToggleField("多段伤害检测:", "isMultiInjuryDetection", OnMultiInjuryDetectionChanged);
            CreateFloatField("检测间隔:", "multiInjuryDetectionInterval", OnMultiInjuryDetectionIntervalChanged);

            // 碰撞体设置
            CreateColliderTypeField();

            // 扇形碰撞体设置
            CreateFloatField("扇形内圆半径:", "innerCircleRadius", OnInnerCircleRadiusChanged);
            CreateFloatField("扇形外圆半径:", "outerCircleRadius", OnOuterCircleRadiusChanged);
            CreateFloatField("扇形角度:", "sectorAngle", OnSectorAngleChanged);
            CreateFloatField("扇形厚度:", "sectorThickness", OnSectorThicknessChanged);

            // Transform设置
            CreateVector3Field("碰撞体位置:", "position", OnPositionChanged);
            CreateVector3Field("碰撞体旋转:", "rotation", OnRotationChanged);
            CreateVector3Field("碰撞体缩放:", "scale", OnScaleChanged);
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
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "目标层级更新");
        }

        private void OnMultiInjuryDetectionChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "多段伤害检测更新");
        }

        private void OnMultiInjuryDetectionIntervalChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "检测间隔更新");
        }

        private void OnColliderTypeChanged(FFramework.Kit.ColliderType newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "碰撞体类型更新");
        }

        private void OnInnerCircleRadiusChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "扇形内圆半径更新");
        }

        private void OnOuterCircleRadiusChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "扇形外圆半径更新");
        }

        private void OnSectorAngleChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "扇形角度更新");
        }

        private void OnSectorThicknessChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "扇形厚度更新");
        }

        private void OnPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "碰撞体位置更新");
        }

        private void OnRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "碰撞体旋转更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "碰撞体缩放更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 删除攻击轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteAttackTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.injuryDetectionTracks == null || attackTargetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或伤害检测轨道为空");
                    return;
                }

                // TODO: 实现攻击轨道项的配置数据删除逻辑
                // 由于攻击轨道的具体数据结构需要进一步确认，这里保留删除框架

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

                Debug.Log($"攻击轨道项 \"{attackTargetData.trackItemName}\" 删除成功");
            }, "删除攻击轨道项");
        }

        #endregion
    }
}