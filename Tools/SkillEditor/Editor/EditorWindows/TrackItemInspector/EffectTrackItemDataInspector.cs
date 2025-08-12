using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(EffectTrackItemData))]
    public class EffectTrackItemDataInspector : BaseTrackItemDataInspector
    {
        protected override string TrackItemTypeName => "Effect";
        protected override string TrackItemDisplayTitle => "特效轨道项信息";
        protected override string DeleteButtonText => "删除特效轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as EffectTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
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
                UpdateTrackConfig(configClip => { configClip.effectPrefab = newPrefab; }, "特效预制体更新");

            }, "特效预制体更新");
        }

        private void OnEffectPlaySpeedChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip =>
                {
                    configClip.effectPlaySpeed = newValue;
                    //刷新持续帧
                    configClip.durationFrame = (int)(base.targetData.frameCount / newValue);
                    targetData.durationFrame = configClip.durationFrame;
                }, "特效播放速度更新");
            }, "特效播放速度更新");
        }

        private void OnIsCutEffectChanged(bool newValue)
        {
            SafeExecute(() => { UpdateTrackConfig(configClip => configClip.isCutEffect = newValue, "是否截断特效更新"); }, "是否截断特效更新");
        }

        private void OnCutEffectFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                if (0 <= newValue && newValue < targetData.startFrame + targetData.durationFrame)
                {
                    UpdateTrackConfig(configClip => configClip.cutEffectFrameOffset = newValue, "特效截断帧更新");
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
                UpdateTrackConfig(configClip => { configClip.position = newValue; }, "特效位置更新");
            }, "特效位置更新");
        }

        private void OnRotationChanged(Vector3 newValue)
        {
            SafeExecute(() => { UpdateTrackConfig(configClip => { configClip.rotation = newValue; }, "特效旋转更新"); }, "特效旋转更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => { configClip.scale = newValue; }, "特效缩放更新");

            }, "特效缩放更新");
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
                targetData.trackItemName = newValue;
                Debug.Log($"特效轨道项名称已更新: {oldName} - {lastTrackItemName} - {targetData.trackItemName}");
            }, "轨道项名称更新");
        }

        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
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
                UpdateTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
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

                    var effectTrackData = targetData as EffectTrackItemData;
                    effectTrackData.rotation = euler;

                    // 更新特效轨道项的Transform信息
                    effectTrackData.position = effectObject.transform.position;
                    effectTrackData.scale = effectObject.transform.localScale;
                    // 保存数据
                    UpdateTrackConfig(configClip => { configClip.position = effectObject.transform.position; }, "特效position更新");
                    UpdateTrackConfig(configClip => { configClip.rotation = euler; }, "特效rotation更新");
                    UpdateTrackConfig(configClip => { configClip.scale = effectObject.transform.localScale; }, "特效localScale更新");
                }
            }, "获取特效Transform信息");
        }

        /// <summary>
        /// 在场景中查找对应的特效物体
        /// </summary>
        /// <returns>找到的特效物体，如果未找到返回null</returns>
        private GameObject FindEffectObjectInScene()
        {
            if (targetData == null) return null;

            // 直接通过轨道项名称查找
            GameObject effectObject = GameObject.Find(targetData.trackItemName);
            if (effectObject != null) return effectObject;

            // 通过预制体名称查找
            var effectTrackData = targetData as EffectTrackItemData;
            if (effectTrackData?.effectPrefab != null)
            {
                string prefabName = effectTrackData.effectPrefab.name;
                effectObject = GameObject.Find(prefabName);
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
                if (skillConfig?.trackContainer?.effectTrack == null || targetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或特效轨道为空");
                    return;
                }

                // 根据trackIndex查找对应的特效轨道并删除片段
                bool deleted = false;
                if (skillConfig.trackContainer.effectTrack.effectTracks != null)
                {
                    var targetTrack = skillConfig.trackContainer.effectTrack.effectTracks
                        .FirstOrDefault(track => track.trackIndex == targetData.trackIndex);

                    if (targetTrack?.effectClips != null)
                    {
                        // 查找要删除的片段
                        var clipToRemove = targetTrack.effectClips.FirstOrDefault(clip =>
                            clip.clipName == targetData.trackItemName &&
                            clip.startFrame == targetData.startFrame);

                        if (clipToRemove != null)
                        {
                            targetTrack.effectClips.Remove(clipToRemove);
                            deleted = true;
                            Debug.Log($"从配置中删除特效片段: {clipToRemove.clipName} (轨道索引: {targetData.trackIndex})");
                        }
                        else
                        {
                            Debug.LogWarning($"未找到要删除的特效片段: {targetData.trackItemName} (轨道索引: {targetData.trackIndex})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"未找到trackIndex为 {targetData.trackIndex} 的特效轨道");
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

                Debug.Log($"特效轨道项 \"{targetData.trackItemName}\" 删除成功");
            }, "删除特效轨道项");
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除特效轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteEffectTrackItem();
            }
        }
        /// <summary>
        /// 统一的配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfig(System.Action<FFramework.Kit.EffectTrack.EffectClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 根据指定名称查找并更新攻击配置数据
        /// </summary>
        /// <param name="clipName">要查找的片段名称</param>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.EffectTrack.EffectClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.injuryDetectionTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或伤害检测轨道为空");
                return;
            }

            // 使用trackIndex精确定位轨道，只通过名称唯一查找
            FFramework.Kit.EffectTrack.EffectClip targetConfigClip = null;
            if (skillConfig.trackContainer.effectTrack != null)
            {
                var targetTrack = skillConfig.trackContainer.effectTrack.effectTracks
                    .FirstOrDefault(track => track.trackIndex == targetData.trackIndex);

                if (targetTrack.effectClips != null)
                {
                    targetConfigClip = targetTrack.effectClips
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

        #endregion
    }
}
