using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(CameraTrackItemData))]
    public class CameraTrackItemDataInspector : BaseTrackItemDataInspector
    {
        protected override string TrackItemTypeName => "Camera";
        protected override string TrackItemDisplayTitle => "摄像机轨道项信息";
        protected override string DeleteButtonText => "删除摄像机轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as CameraTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 摄像机类型设置
            CreateToggleField("启用位置变换:", "enablePosition", OnEnablePositionChanged);
            CreateToggleField("启用旋转变换:", "enableRotation", OnEnableRotationChanged);
            CreateToggleField("启用视野变换:", "enableFieldOfView", OnEnableFieldOfViewChanged);

            // 目标状态设置
            CreateSeparatorTitle("摄像机目标状态");
            CreateVector3Field("位置偏移:", "positionOffset", OnPositionOffsetChanged);
            CreateVector3Field("目标旋转:", "targetRotation", OnTargetRotationChanged);
            CreateFloatField("目标视野角度:", "targetFieldOfView", OnTargetFieldOfViewChanged);
            CreateCurveTypeField();
            CreateCurveField("自定义曲线:", "customCurve", OnCustomCurveChanged);
            CreateIntegerField("还原状态所需帧:", "restoreFrame", OnRestoreFrameChanged);

            CreateSeparatorTitle("摄像机震动设置");
            CreateToggleField("启用震动:", "enableShake", OnEnableShakeChanged);
            CreateIntegerField("震动起始帧偏移:", "animationStartFrameOffset", OnAnimationStartFrameOffsetChanged);
            CreateIntegerField("震动持续帧:", "animationDurationFrame", OnAnimationDurationFrameChanged);
            CreateObjectField<FFramework.Kit.ShakePreset>("震动配置:", "shakePreset", OnShakeConfigChanged);
        }

        /// <summary>
        /// 创建曲线类型选择字段
        /// </summary>
        private void CreateCurveTypeField()
        {
            var curveTypeContent = CreateContentContainer("动画曲线类型:");
            var curveTypeField = new EnumField(((CameraTrackItemData)targetData).curveType);
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

        private void OnEnableFieldOfViewChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.enableFieldOfView = newValue, "视野变换启用状态更新");
            }, "视野变换启用状态更新");
        }

        private void OnPositionOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.positionOffset = newValue, "位置偏移更新");
            }, "位置偏移更新");
        }

        private void OnTargetRotationChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.targetRotation = newValue, "目标旋转更新");
            }, "目标旋转更新");
        }

        private void OnTargetFieldOfViewChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.targetFieldOfView = newValue, "目标视野角度更新");
            }, "目标视野角度更新");
        }

        private void OnCustomCurveChanged(AnimationCurve newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.customCurve = newValue, "自定义曲线更新");
            }, "自定义曲线更新");
        }

        private void OnRestoreFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.restoreFrame = newValue, "还原状态所需帧更新");
            }, "还原状态所需帧更新");
        }

        private void OnCurveTypeChanged(FFramework.Kit.AnimationCurveType newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.curveType = newValue, "曲线类型更新");
            }, "曲线类型更新");
        }

        private void OnEnableShakeChanged(bool newValue)
        {
            SafeExecute(() =>
          {
              UpdateTrackConfig(configClip => configClip.enableShake = newValue, "是否启用震动更新");
          }, "是否启用震动更新");
        }

        private void OnAnimationStartFrameOffsetChanged(int newValue)
        {
            SafeExecute(() =>
            {
                var cameraTrackItemData = targetData as CameraTrackItemData;
                if (cameraTrackItemData != null && newValue >= 0 && newValue <= cameraTrackItemData.durationFrame - cameraTrackItemData.animationDurationFrame)
                {
                    UpdateTrackConfig(configClip => configClip.animationStartFrameOffset = newValue, "动画开始帧更新");
                }
                else
                {
                    Debug.LogError("动画开始帧超出范围");
                }
            }, "动画开始帧更新");
        }

        private void OnAnimationDurationFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {

                if (newValue >= 0 && newValue <= targetData.durationFrame)
                {
                    UpdateTrackConfig(configClip => configClip.animationDurationFrame = newValue, "动画持续时间更新");
                }
                else
                {
                    Debug.LogError("动画持续帧超出范围");
                }
            }, "动画持续时间更新");
        }

        private void OnShakeConfigChanged(FFramework.Kit.ShakePreset shakePreset)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip =>
                {
                    configClip.shakePreset = shakePreset;
                }, "震动预设更新");

            }, "震动预设更新");
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

        protected override void CreateAdditionalActionButtons()
        {
            // 打开震动预设文件
            var openShakePresetContent = CreateContentContainer("");
            var openShakePresetButton = new Button()
            {
                text = "打开震动预设文件"
            };
            openShakePresetButton.clicked += () =>
            {
                // 打开预设SO面板
                var cameraTrackItemData = targetData as CameraTrackItemData;
                if (cameraTrackItemData != null && cameraTrackItemData.shakePreset != null)
                {
                    UnityEditor.Selection.activeObject = cameraTrackItemData.shakePreset;
                    UnityEditor.EditorGUIUtility.PingObject(cameraTrackItemData.shakePreset);
                }
            };
            openShakePresetButton.AddToClassList("CustomButton");
            openShakePresetContent.Add(openShakePresetButton);
            root.Add(openShakePresetContent);
        }
        /// <summary>
        /// 统一的配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfig(System.Action<FFramework.Kit.CameraTrack.CameraClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 根据指定名称查找并更新攻击配置数据
        /// </summary>
        /// <param name="clipName">要查找的片段名称</param>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.CameraTrack.CameraClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.cameraTrack == null || targetData == null)
            {
                return;
            }

            // 只通过名称唯一查找
            FFramework.Kit.CameraTrack.CameraClip targetConfigClip = null;
            if (skillConfig.trackContainer.cameraTrack.cameraClips != null)
            {
                targetConfigClip = skillConfig.trackContainer.cameraTrack.cameraClips
                    .FirstOrDefault(clip => clip.clipName == clipName);
            }

            if (targetConfigClip != null)
            {
                try
                {
                    updateAction(targetConfigClip);
                    MarkSkillConfigDirty();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"{operationName} 失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的摄像机片段配置 (片段名: {clipName})");
            }
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除摄像机轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteCameraTrackItem();
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
                if (skillConfig?.trackContainer?.cameraTrack == null || targetData == null)
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
                        clip.clipName == targetData.trackItemName &&
                        clip.startFrame == targetData.startFrame);

                    if (clipToRemove != null)
                    {
                        cameraTrack.cameraClips.Remove(clipToRemove);
                        deleted = true;
                        Debug.Log($"从配置中删除摄像机片段: {clipToRemove.clipName}");
                    }
                    else
                    {
                        Debug.LogWarning($"未找到要删除的摄像机片段: {targetData.trackItemName}");
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

                Debug.Log($"摄像机轨道项 \"{targetData.trackItemName}\" 删除成功");
            }, "删除摄像机轨道项");
        }

        #endregion
    }
}
