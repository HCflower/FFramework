using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(CameraTrackItemData))]
    public class CameraTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private CameraTrackItemData cameraTargetData;

        protected override string TrackItemTypeName => "Camera";
        protected override string TrackItemDisplayTitle => "摄像机轨道项信息";
        protected override string DeleteButtonText => "删除摄像机轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            cameraTargetData = target as CameraTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 摄像机类型设置
            CreateToggleField("启用位置变换:", "enablePosition", OnEnablePositionChanged);
            CreateToggleField("启用旋转变换:", "enableRotation", OnEnableRotationChanged);
            CreateToggleField("启用视野变换:", "enableFieldOfView", OnEnableFieldOfViewChanged);

            // 起始状态设置
            CreateVector3Field("起始位置:", "startPosition", OnStartPositionChanged);
            CreateVector3Field("起始旋转:", "startRotation", OnStartRotationChanged);
            CreateFloatField("起始视野角度:", "startFieldOfView", OnStartFieldOfViewChanged);
            CreateSeparator();

            // 目标状态设置
            CreateVector3Field("目标位置:", "endPosition", OnEndPositionChanged);
            CreateVector3Field("目标旋转:", "endRotation", OnEndRotationChanged);
            CreateFloatField("目标视野角度:", "endFieldOfView", OnEndFieldOfViewChanged);
            CreateSeparator();

            // 动画设置
            CreateCurveTypeField();
            CreateCurveField("自定义曲线:", "customCurve", OnCustomCurveChanged);
            CreateToggleField("相对于当前状态:", "isRelative", OnIsRelativeChanged);

            // 摄像机引用设置
            CreateObjectField<Camera>("目标摄像机:", "targetCamera", OnTargetCameraChanged);
            CreateTextField("摄像机路径:", "cameraPath", OnCameraPathChanged);
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除摄像机轨道项 \"{cameraTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteCameraTrackItem();
            }
        }

        /// <summary>
        /// 创建曲线类型选择字段
        /// </summary>
        private void CreateCurveTypeField()
        {
            var curveTypeContent = CreateContentContainer("动画曲线类型:");
            var curveTypeField = new EnumField(cameraTargetData.curveType);
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
                UpdateCameraTrackConfig(configClip => configClip.enablePosition = newValue, "位置变换启用状态更新");
            }, "位置变换启用状态更新");
        }

        private void OnEnableRotationChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.enableRotation = newValue, "旋转变换启用状态更新");
            }, "旋转变换启用状态更新");
        }

        private void OnEnableFieldOfViewChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.enableFieldOfView = newValue, "视野变换启用状态更新");
            }, "视野变换启用状态更新");
        }

        private void OnStartPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.startPosition = newValue, "起始位置更新");
            }, "起始位置更新");
        }

        private void OnStartRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.startRotation = newValue, "起始旋转更新");
            }, "起始旋转更新");
        }

        private void OnStartFieldOfViewChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.startFieldOfView = newValue, "起始视野角度更新");
            }, "起始视野角度更新");
        }

        private void OnEndPositionChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.endPosition = newValue, "目标位置更新");
            }, "目标位置更新");
        }

        private void OnEndRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.endRotation = newValue, "目标旋转更新");
            }, "目标旋转更新");
        }

        private void OnEndFieldOfViewChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.endFieldOfView = newValue, "目标视野角度更新");
            }, "目标视野角度更新");
        }

        private void OnCustomCurveChanged(AnimationCurve newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.customCurve = newValue, "自定义曲线更新");
            }, "自定义曲线更新");
        }

        private void OnCurveTypeChanged(FFramework.Kit.AnimationCurveType newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.curveType = newValue, "曲线类型更新");
            }, "曲线类型更新");
        }

        private void OnIsRelativeChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.isRelative = newValue, "相对状态更新");
            }, "相对状态更新");
        }

        private void OnTargetCameraChanged(Camera newCamera)
        {
            SafeExecute(() =>
            {
                // 摄像机引用主要用于编辑器阶段，不需要同步到配置文件
                MarkSkillConfigDirty();
            }, "目标摄像机更新");
        }

        private void OnCameraPathChanged(string newPath)
        {
            SafeExecute(() =>
            {
                // 摄像机路径主要用于运行时查找，不需要同步到配置文件
                MarkSkillConfigDirty();
            }, "摄像机路径更新");
        }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateCameraTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
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
                UpdateCameraTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的摄像机配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateCameraTrackConfig(System.Action<FFramework.Kit.CameraTrack.CameraClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.cameraTrack == null || cameraTargetData == null)
            {
                return;
            }

            // 摄像机轨道是单轨道，直接在cameraClips中查找对应的片段
            FFramework.Kit.CameraTrack.CameraClip targetConfigClip = null;

            if (skillConfig.trackContainer.cameraTrack.cameraClips != null)
            {
                // 首先尝试精确匹配（名称和起始帧都匹配）
                var exactMatches = skillConfig.trackContainer.cameraTrack.cameraClips
                    .Where(clip => clip.clipName == cameraTargetData.trackItemName &&
                                   clip.startFrame == cameraTargetData.startFrame)
                    .ToList();

                if (exactMatches.Count > 0)
                {
                    targetConfigClip = exactMatches[0];
                    if (exactMatches.Count > 1)
                    {
                        Debug.LogWarning($"找到多个匹配的摄像机片段配置，使用第一个。名称: {cameraTargetData.trackItemName}, 起始帧: {cameraTargetData.startFrame}");
                    }
                }
                else
                {
                    // 如果精确匹配失败，尝试只匹配名称
                    var nameMatches = skillConfig.trackContainer.cameraTrack.cameraClips
                        .Where(clip => clip.clipName == cameraTargetData.trackItemName)
                        .ToList();

                    if (nameMatches.Count > 0)
                    {
                        targetConfigClip = nameMatches[0];
                        // Debug.LogWarning($"未找到精确匹配的摄像机片段，使用名称匹配。名称: {cameraTargetData.trackItemName}");
                    }
                    else
                    {
                        Debug.LogWarning($"未找到名称匹配的摄像机片段: {cameraTargetData.trackItemName}");
                    }
                }
            }

            if (targetConfigClip != null)
            {
                // 执行更新操作
                try
                {
                    updateAction(targetConfigClip);
                    MarkSkillConfigDirty();
                    // Debug.Log($"{operationName} 成功同步到配置文件 (片段名: {cameraTargetData.trackItemName})");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"{operationName} 失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的摄像机片段配置 (片段名: {cameraTargetData.trackItemName}, 起始帧: {cameraTargetData.startFrame})");
            }
        }

        /// <summary>
        /// 删除摄像机轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteCameraTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.cameraTrack == null || cameraTargetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或摄像机轨道为空");
                    return;
                }

                // 摄像机轨道是单轨道，需要从cameraClips列表中移除对应的片段
                bool deleted = false;
                var cameraTrack = skillConfig.trackContainer.cameraTrack;
                if (cameraTrack?.cameraClips != null)
                {
                    var clipToRemove = cameraTrack.cameraClips.FirstOrDefault(clip =>
                        clip.clipName == cameraTargetData.trackItemName &&
                        clip.startFrame == cameraTargetData.startFrame);

                    if (clipToRemove != null)
                    {
                        cameraTrack.cameraClips.Remove(clipToRemove);
                        deleted = true;
                        Debug.Log($"从配置中删除摄像机片段: {clipToRemove.clipName}");
                    }
                    else
                    {
                        Debug.LogWarning($"未找到要删除的摄像机片段: {cameraTargetData.trackItemName}");
                    }
                }

                if (deleted)
                {
                    // 标记配置文件为已修改
                    MarkSkillConfigDirty();
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                // 删除ScriptableObject资产
                if (cameraTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(cameraTargetData));
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

                Debug.Log($"摄像机轨道项 \"{cameraTargetData.trackItemName}\" 删除成功");
            }, "删除摄像机轨道项");
        }

        #endregion
    }
}
