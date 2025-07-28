using UnityEngine.UIElements;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(AnimationTrackItemData))]
    public class AnimationTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private AnimationTrackItemData animationTargetData;

        protected override string TrackItemTypeName => "Animation";
        protected override string TrackItemDisplayTitle => "动画轨道项信息";
        protected override string DeleteButtonText => "删除动画轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            animationTargetData = target as AnimationTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 动画片段字段
            CreateObjectField<AnimationClip>("动画片段:", "animationClip", OnAnimationClipChanged);

            // 播放速度
            CreateFloatField("播放速度:", "playSpeed", OnPlaySpeedChanged);

            // 循环播放
            CreateToggleField("是否循环播放:", "isLoop", OnLoopChanged);

            // 应用根运动
            CreateToggleField("应用根运动:", "applyRootMotion", OnApplyRootMotionChanged);
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除动画轨道项 \"{animationTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteAnimationTrackItem();
            }
        }

        #region 事件处理方法

        private void OnAnimationClipChanged(AnimationClip newClip)
        {
            SafeExecute(() =>
            {
                UpdateAnimationTrackConfig(configClip => configClip.clip = newClip, "动画片段更新");
            }, "动画片段更新");
        }

        private void OnPlaySpeedChanged(float newValue)
        {
            SafeExecute(() =>
            {
                // Debug.Log($"播放速度更改为: {newValue}");
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "播放速度更新");
        }

        private void OnLoopChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // Debug.Log($"循环播放设置为: {newValue}");
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "循环播放设置更新");
        }

        private void OnApplyRootMotionChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                // Debug.Log($"应用根运动设置为: {newValue}");
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "根运动设置更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的动画配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateAnimationTrackConfig(System.Action<FFramework.Kit.AnimationTrack.AnimationClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null || animationTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或动画轨道为空");
                return;
            }

            // 查找对应的动画片段配置
            FFramework.Kit.AnimationTrack.AnimationClip targetConfigClip = null;

            if (skillConfig.trackContainer.animationTrack.animationClips != null)
            {
                var candidateClips = skillConfig.trackContainer.animationTrack.animationClips
                    .Where(clip => clip.clipName == animationTargetData.trackItemName).ToList();

                if (candidateClips.Count > 0)
                {
                    if (candidateClips.Count == 1)
                    {
                        targetConfigClip = candidateClips[0];
                    }
                    else
                    {
                        // 如果有多个同名片段，尝试通过起始帧匹配
                        var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == animationTargetData.startFrame);
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
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的动画片段配置");
            }
        }

        /// <summary>
        /// 删除动画轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteAnimationTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null || animationTargetData == null)
            {
                Debug.LogWarning("无法删除轨道项：技能配置或动画轨道为空");
                return;
            }

            // 标记要删除的动画片段配置
            FFramework.Kit.AnimationTrack.AnimationClip targetConfigClip = null;

            // 查找对应的动画片段配置
            if (skillConfig.trackContainer.animationTrack.animationClips != null)
            {
                var candidateClips = skillConfig.trackContainer.animationTrack.animationClips
                    .Where(clip => clip.clipName == animationTargetData.trackItemName).ToList();

                if (candidateClips.Count > 0)
                {
                    if (candidateClips.Count == 1)
                    {
                        targetConfigClip = candidateClips[0];
                    }
                    else
                    {
                        // 如果有多个同名片段，尝试通过起始帧匹配
                        var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == animationTargetData.startFrame);
                        targetConfigClip = exactMatch ?? candidateClips[0];
                    }
                }
            }

            if (targetConfigClip != null)
            {
                // 从配置中移除动画片段
                skillConfig.trackContainer.animationTrack.animationClips.Remove(targetConfigClip);
                MarkSkillConfigDirty();

                // 删除ScriptableObject资产
                if (animationTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(animationTargetData));
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

                Debug.Log($"动画轨道项 \"{animationTargetData.trackItemName}\" 删除成功");
            }
            else
            {
                Debug.LogWarning($"无法删除轨道项：找不到对应的动画片段配置 \"{animationTargetData.trackItemName}\"");
            }
        }

        #endregion
    }
}
