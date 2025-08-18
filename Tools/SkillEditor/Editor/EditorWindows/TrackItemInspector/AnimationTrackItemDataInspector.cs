using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(AnimationTrackItemData))]
    public class AnimationTrackItemDataInspector : TrackItemDataInspectorBase
    {
        protected override string TrackItemTypeName => "Animation";
        protected override string TrackItemDisplayTitle => "动画轨道项信息";
        protected override string DeleteButtonText => "删除动画轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as AnimationTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 动画片段字段
            CreateObjectField<AnimationClip>("动画片段:", "animationClip", OnAnimationClipChanged);

            // 播放速度
            CreateFloatField("播放速度:", "animationPlaySpeed", OnPlaySpeedChanged);

            // 动画过渡时间
            CreateIntegerField("动画过渡帧数:", "transitionDurationFrame", OnTransitionTimeChanged);

            // 应用根运动
            CreateToggleField("应用根运动:", "applyRootMotion", OnApplyRootMotionChanged);
        }

        #region 事件处理方法

        private void OnAnimationClipChanged(AnimationClip newClip)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.clip = newClip, "动画片段更新");
            }, "动画片段更新");
        }

        private void OnPlaySpeedChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip =>
                {
                    configClip.animationPlaySpeed = newValue;
                    //刷新持续帧
                    configClip.durationFrame = (int)(base.targetData.frameCount / newValue);
                    targetData.durationFrame = configClip.durationFrame;
                }, "播放速度更新");

            }, "播放速度更新");
        }

        private void OnTransitionTimeChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.transitionDurationFrame = newValue, "过渡时间更新");
            }, "过渡时间更新");

        }

        private void OnApplyRootMotionChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.applyRootMotion = newValue, "根运动设置更新");
            }, "根运动设置更新");
        }

        protected override void OnTrackItemNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                // 使用保存的旧名称
                string oldName = lastTrackItemName ?? targetData.trackItemName;
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
        private void UpdateTrackConfig(System.Action<FFramework.Kit.AnimationTrack.AnimationClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 统一的动画配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.AnimationTrack.AnimationClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或动画轨道为空");
                return;
            }

            // 只通过名称唯一查找
            FFramework.Kit.AnimationTrack.AnimationClip targetConfigClip = null;
            if (skillConfig.trackContainer.animationTrack.animationClips != null)
            {
                targetConfigClip = skillConfig.trackContainer.animationTrack.animationClips
                    .FirstOrDefault(clip => clip.clipName == clipName);
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的动画片段配置 (片段名: {clipName})");
            }
        }

        /// <summary>
        /// 删除动画轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        protected override void DeleteTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null || targetData == null)
            {
                Debug.LogWarning("无法删除轨道项：技能配置或动画轨道为空");
                return;
            }

            // 直接通过唯一名称查找目标配置
            var targetConfigClip = skillConfig.trackContainer.animationTrack.animationClips
                ?.FirstOrDefault(clip => clip.clipName == targetData.trackItemName);

            if (targetConfigClip != null)
            {
                // 从配置中移除动画片段
                skillConfig.trackContainer.animationTrack.animationClips.Remove(targetConfigClip);
                MarkSkillConfigDirty();

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
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        window.Repaint();
                        SkillEditorEvent.TriggerRefreshRequested();
                    };
                }

                Debug.Log($"动画轨道项 \"{targetData.trackItemName}\" 删除成功");
            }
            else
            {
                Debug.LogWarning($"无法删除轨道项：找不到对应的动画片段配置 \"{targetData.trackItemName}\"");
            }
        }

        #endregion
    }
}
