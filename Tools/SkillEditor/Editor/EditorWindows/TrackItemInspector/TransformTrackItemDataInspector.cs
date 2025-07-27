using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(TransformTrackItemData))]
    public class TransformTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private TransformTrackItemData transformTargetData;

        protected override string TrackItemTypeName => "Transform";
        protected override string TrackItemDisplayTitle => "变换轨道项信息";
        protected override string DeleteButtonText => "删除变换轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            transformTargetData = target as TransformTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 变换类型设置
            CreateToggleField("启用位置:", "enablePosition", OnEnablePositionChanged);
            CreateToggleField("启用旋转:", "enableRotation", OnEnableRotationChanged);
            CreateToggleField("启用缩放:", "enableScale", OnEnableScaleChanged);
            // 起始变换设置
            CreateVector3Field("起始位置:", "startPosition", OnStartPositionChanged);
            CreateVector3Field("起始旋转:", "startRotation", OnStartRotationChanged);
            CreateVector3Field("起始缩放:", "startScale", OnStartScaleChanged);
            CreateSeparator();
            // 目标变换设置
            CreateVector3Field("目标位置:", "endPosition", OnEndPositionChanged);
            CreateVector3Field("目标旋转:", "endRotation", OnEndRotationChanged);
            CreateVector3Field("目标缩放:", "endScale", OnEndScaleChanged);
            // 动画设置
            CreateCurveTypeField();
            CreateCurveField("自定义曲线:", "customCurve", OnCustomCurveChanged);
        }
        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除变换轨道项 \"{transformTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteTransformTrackItem();
            }
        }

        /// <summary>
        /// 创建曲线类型选择字段
        /// </summary>
        private void CreateCurveTypeField()
        {
            var curveTypeContent = CreateContentContainer("曲线类型:");
            var curveTypeField = new EnumField(transformTargetData.curveType);
            curveTypeField.AddToClassList("EnumField");
            curveTypeField.BindProperty(serializedObject.FindProperty("curveType"));
            curveTypeField.RegisterValueChangedCallback(evt => OnCurveTypeChanged((FFramework.Kit.AnimationCurveType)evt.newValue));
            curveTypeContent.Add(curveTypeField);
            root.Add(curveTypeContent);
        }

        #region 事件处理方法

        private void OnEnablePositionChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "位置变换启用状态更新");
        }

        private void OnEnableRotationChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "旋转变换启用状态更新");
        }

        private void OnEnableScaleChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "缩放变换启用状态更新");
        }

        private void OnStartPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "起始位置更新");
        }

        private void OnStartRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "起始旋转更新");
        }

        private void OnStartScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "起始缩放更新");
        }

        private void OnEndPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "目标位置更新");
        }

        private void OnEndRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "目标旋转更新");
        }

        private void OnEndScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "目标缩放更新");
        }

        private void OnCustomCurveChanged(AnimationCurve newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "自定义曲线更新");
        }

        private void OnCurveTypeChanged(FFramework.Kit.AnimationCurveType newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "曲线类型更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 删除变换轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteTransformTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.transformTrack == null || transformTargetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或变换轨道为空");
                    return;
                }

                // TODO: 实现变换轨道项的配置数据删除逻辑
                // 由于变换轨道的具体数据结构需要进一步确认，这里保留删除框架

                // 删除ScriptableObject资产
                if (transformTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(transformTargetData));
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

                Debug.Log($"变换轨道项 \"{transformTargetData.trackItemName}\" 删除成功");
            }, "删除变换轨道项");
        }

        #endregion
    }
}