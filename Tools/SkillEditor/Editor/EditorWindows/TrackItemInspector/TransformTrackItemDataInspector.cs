using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(TransformTrackItemData))]
    public class TransformTrackItemDataInspector : BaseTrackItemDataInspector
    {
        protected override string TrackItemTypeName => "Transform";
        protected override string TrackItemDisplayTitle => "变换轨道项信息";
        protected override string DeleteButtonText => "删除变换轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as TransformTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 变换类型设置
            CreateToggleField("启用位置:", "enablePosition", OnEnablePositionChanged);
            CreateToggleField("启用旋转:", "enableRotation", OnEnableRotationChanged);
            CreateToggleField("启用缩放:", "enableScale", OnEnableScaleChanged);
            // 目标变换设置
            CreateSeparatorTitle("变换目标设置");
            CreateVector3Field("位置偏移:", "positionOffset", OnPositionOffsetChanged);
            CreateVector3Field("目标旋转:", "targetRotation", OnTargetRotationChanged);
            CreateVector3Field("目标缩放:", "targetScale", OnTargetScaleChanged);
            // 动画设置
            CreateSeparatorTitle("动画设置");
            CreateCurveTypeField();
            CreateCurveField("自定义曲线:", "customCurve", OnCustomCurveChanged);
        }

        /// <summary>
        /// 创建曲线类型选择字段
        /// </summary>
        private void CreateCurveTypeField()
        {
            var curveTypeContent = CreateContentContainer("曲线类型:");
            var curveTypeField = new EnumField(((TransformTrackItemData)targetData).curveType);
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
                UpdateTrackConfig(configClip => configClip.enablePosition = newValue, "位置变换启用状态更新");
            }, "位置变换启用状态更新");
        }

        private void OnEnableRotationChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.enableRotation = newValue, "旋转变换启用状态更新");
            }, "旋转变换启用状态更新");
        }

        private void OnEnableScaleChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.enableScale = newValue, "缩放变换启用状态更新");
            }, "缩放变换启用状态更新");
        }

        private void OnPositionOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.positionOffset = newValue, "目标偏移更新");
            }, "目标偏移更新");
        }

        private void OnTargetRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.targetRotation = newValue, "目标旋转更新");
            }, "目标旋转更新");
        }

        private void OnTargetScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.targetScale = newValue, "目标缩放更新");
            }, "目标缩放更新");
        }

        private void OnCustomCurveChanged(AnimationCurve newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.customCurve = newValue, "自定义曲线更新");
            }, "自定义曲线更新");
        }

        private void OnCurveTypeChanged(FFramework.Kit.AnimationCurveType newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.curveType = newValue, "曲线类型更新");
            }, "曲线类型更新");
        }

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

        #endregion

        #region 数据同步方法
        /// <summary>
        /// 统一的配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfig(System.Action<FFramework.Kit.TransformTrack.TransformClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 统一的变换配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.TransformTrack.TransformClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.transformTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或变换轨道为空");
                return;
            }

            // 只通过名称唯一查找
            FFramework.Kit.TransformTrack.TransformClip targetConfigClip = null;
            if (skillConfig.trackContainer.transformTrack.transformClips != null)
            {
                targetConfigClip = skillConfig.trackContainer.transformTrack.transformClips
                    .FirstOrDefault(clip => clip.clipName == clipName);
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();

                // 刷新Transform预览器数据
                var skillEditorWindow = EditorWindow.GetWindow<SkillEditor>(false, null, false);
                if (skillEditorWindow != null)
                {
                    skillEditorWindow.RefreshTransformPreviewerData();
                }
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的变换片段配置");
            }
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除变换轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteTransformTrackItem();
            }
        }

        /// <summary>
        /// 删除变换轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteTransformTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.transformTrack == null || targetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或变换轨道为空");
                    return;
                }

                // TODO: 实现变换轨道项的配置数据删除逻辑
                // 由于变换轨道的具体数据结构需要进一步确认，这里保留删除框架

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

                Debug.Log($"变换轨道项 \"{targetData.trackItemName}\" 删除成功");
            }, "删除变换轨道项");
        }

        #endregion
    }
}