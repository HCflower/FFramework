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
                UpdateTransformTrackConfig(configClip => configClip.enablePosition = newValue, "位置变换启用状态更新");
            }, "位置变换启用状态更新");
        }

        private void OnEnableRotationChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.enableRotation = newValue, "旋转变换启用状态更新");
            }, "旋转变换启用状态更新");
        }

        private void OnEnableScaleChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.enableScale = newValue, "缩放变换启用状态更新");
            }, "缩放变换启用状态更新");
        }

        private void OnPositionOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.positionOffset = newValue, "目标偏移更新");
            }, "目标偏移更新");
        }

        private void OnTargetRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.targetRotation = newValue, "目标旋转更新");
            }, "目标旋转更新");
        }

        private void OnTargetScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.targetScale = newValue, "目标缩放更新");
            }, "目标缩放更新");
        }

        private void OnCustomCurveChanged(AnimationCurve newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.customCurve = newValue, "自定义曲线更新");
            }, "自定义曲线更新");
        }

        private void OnCurveTypeChanged(FFramework.Kit.AnimationCurveType newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.curveType = newValue, "曲线类型更新");
            }, "曲线类型更新");
        }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTransformTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
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
                UpdateTransformTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的变换配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTransformTrackConfig(System.Action<FFramework.Kit.TransformTrack.TransformClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.transformTrack == null || transformTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或变换轨道为空");
                return;
            }

            // 查找对应的变换片段配置
            FFramework.Kit.TransformTrack.TransformClip targetConfigClip = null;

            if (skillConfig.trackContainer.transformTrack.transformClips != null)
            {
                var candidateClips = skillConfig.trackContainer.transformTrack.transformClips
                    .Where(clip => clip.clipName == transformTargetData.trackItemName).ToList();

                if (candidateClips.Count > 0)
                {
                    if (candidateClips.Count == 1)
                    {
                        targetConfigClip = candidateClips[0];
                    }
                    else
                    {
                        // 如果有多个同名片段，尝试通过起始帧匹配
                        var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == transformTargetData.startFrame);
                        targetConfigClip = exactMatch ?? candidateClips[0];
                    }
                }
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的变换片段配置");
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

                        // 直接调用静态事件方法
                        SkillEditorEvent.TriggerRefreshRequested();
                    };
                }

                Debug.Log($"变换轨道项 \"{transformTargetData.trackItemName}\" 删除成功");
            }, "删除变换轨道项");
        }

        #endregion
    }
}