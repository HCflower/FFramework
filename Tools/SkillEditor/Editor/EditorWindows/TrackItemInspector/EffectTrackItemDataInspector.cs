using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

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
            CreateFloatField("特效播放速度:", "effectPlaySpeed", OnEffectPlaySpeedChanged);
            CreateToggleField("是否截断特效:", "isCutEffect", OnIsCutEffectChanged);
            CreateIntegerField("特效截断帧偏移:", "cutEffectFrameOffset", OnCutEffectFrameChanged);
            // Transform 设置
            CreateVector3Field("特效位置:", "position", OnPositionChanged);
            CreateVector3Field("特效旋转:", "rotation", OnRotationChanged);
            CreateVector3Field("特效缩放:", "scale", OnScaleChanged);
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

     

        private void OnEffectPlaySpeedChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip =>
                {
                    configClip.effectPlaySpeed = newValue;
                    //刷新持续帧
                    configClip.durationFrame = (int)(targetData.frameCount / newValue);
                    effectTargetData.durationFrame = configClip.durationFrame;
                }, "特效播放速度更新");
            }, "特效播放速度更新");
        }

        private void OnIsCutEffectChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateEffectTrackConfig(configClip => configClip.isCutEffect = newValue, "是否截断特效更新");
            }, "是否截断特效更新");
        }

        private void OnCutEffectFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                if (0 <= newValue && newValue < effectTargetData.startFrame + effectTargetData.durationFrame)
                {
                    UpdateEffectTrackConfig(configClip => configClip.cutEffectFrameOffset = newValue, "特效截断帧更新");
                }
                else
                {
                    Debug.LogError("特效截断帧超出范围");
                }
            }, "特效截断帧更新");
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

        protected override void CreateAdditionalActionButtons()
        {
            // 打开震动预设文件
            var getEffectPrefabContent = CreateContentContainer("");
            var openShakePresetButton = new Button()
            {
                text = "获取特效变换"
            };
            openShakePresetButton.clicked += () =>
            {
                GetEffectTransformFromScene();
            };
            openShakePresetButton.AddToClassList("CustomButton");
            getEffectPrefabContent.Add(openShakePresetButton);
            root.Add(getEffectPrefabContent);
        }

        /// <summary>
        /// 从场景中获取特效物体的Transform信息并更新到配置中
        /// </summary>
        private void GetEffectTransformFromScene()
        {
            SafeExecute(() =>
            {
                // 查找场景中的特效物体
                GameObject effectObject = FindEffectObjectInScene();
                if (effectObject != null)
                {
                    // 使用四元数转换，但转换为标准化的欧拉角
                    Quaternion quat = effectObject.transform.rotation;
                    Vector3 euler = quat.eulerAngles;

                    // 标准化角度到-180到180度范围
                    euler.x = (euler.x > 180) ? euler.x - 360 : euler.x;
                    euler.y = (euler.y > 180) ? euler.y - 360 : euler.y;
                    euler.z = (euler.z > 180) ? euler.z - 360 : euler.z;
                    // 交换y和z轴的值
                    float temp = euler.y;
                    euler.y = euler.z;
                    euler.z = temp;

                    effectTargetData.rotation = euler;

                    // 更新特效轨道项的Transform信息
                    effectTargetData.position = effectObject.transform.position;
                    effectTargetData.scale = effectObject.transform.localScale;
                    // 保存数据
                    UpdateEffectTrackConfig(configClip => { configClip.position = effectObject.transform.position; }, "特效position更新");
                    UpdateEffectTrackConfig(configClip => { configClip.rotation = euler; }, "特效rotation更新");
                    UpdateEffectTrackConfig(configClip => { configClip.scale = effectObject.transform.localScale; }, "特效localScale更新");
                }
            }, "获取特效Transform信息");
        }

        /// <summary>
        /// 在场景中查找对应的特效物体
        /// </summary>
        /// <returns>找到的特效物体，如果未找到返回null</returns>
        private GameObject FindEffectObjectInScene()
        {
            if (effectTargetData == null) return null;

            // 直接通过轨道项名称查找
            GameObject effectObject = GameObject.Find(effectTargetData.trackItemName);
            if (effectObject != null) return effectObject;

            // 通过预制体名称查找
            if (effectTargetData.effectPrefab != null)
            {
                string prefabName = effectTargetData.effectPrefab.name;
                effectObject = GameObject.Find(prefabName);
                if (effectObject != null) return effectObject;

                if (effectObject != null) return effectObject;
            }
            return null;
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

                        // 直接调用静态事件方法
                        SkillEditorEvent.TriggerRefreshRequested();
                    };
                }

                Debug.Log($"特效轨道项 \"{effectTargetData.trackItemName}\" 删除成功");
            }, "删除特效轨道项");
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
