using FFramework.Kit;
using UnityEngine;
using UnityEditor;

namespace SkillEditor
{
    /// <summary>
    /// 技能动画预览管理器
    /// 负责在编辑模式下预览技能动画
    /// </summary>
    public class SkillAnimationPreviewer : System.IDisposable
    {
        #region 私有字段

        /// <summary>当前预览的游戏对象</summary>
        private SkillRuntimeController previewTarget;

        /// <summary>预览对象的Animator组件</summary>
        private Animator previewAnimator;

        /// <summary>是否正在预览</summary>
        private bool isPreviewing = false;

        /// <summary>当前预览的技能配置</summary>
        private SkillConfig currentSkillConfig;

        /// <summary>预览开始时间</summary>
        private double previewStartTime;

        /// <summary>当前预览帧</summary>
        private int currentPreviewFrame = 0;

        /// <summary>是否在编辑模式下预览</summary>
        private bool isEditModePreview = false;

        /// <summary>当前预览的动画片段</summary>
        private AnimationClip currentPreviewClip;

        #endregion

        #region 公共属性

        /// <summary>是否正在预览动画</summary>
        public bool IsPreviewing => isPreviewing;

        /// <summary>当前预览的游戏对象</summary>
        public SkillRuntimeController PreviewTarget => previewTarget;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置预览目标
        /// </summary>
        /// <param name="target">要预览的游戏对象</param>
        /// <returns>是否设置成功</returns>
        public bool SetPreviewTarget(SkillRuntimeController target)
        {
            if (target == null)
            {
                previewTarget = null;
                previewAnimator = null;
                return false;
            }

            var animator = target.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"预览目标 {target.name} 没有有效的Animator组件或控制器");
                return false;
            }

            previewTarget = target;
            previewAnimator = animator;

            if (!HasSkillState())
            {
                Debug.LogWarning($"控制器中没有找到'Skill'状态");
            }

            return true;
        }

        /// <summary>
        /// 开始预览技能动画
        /// </summary>
        /// <param name="skillConfig">技能配置</param>
        /// <returns>是否开始预览成功</returns>
        public bool StartPreview(SkillConfig skillConfig)
        {
            if (skillConfig == null || previewAnimator == null)
            {
                Debug.LogWarning("技能配置或预览目标为空，无法开始预览");
                return false;
            }

            if (skillConfig.trackContainer?.animationTrack?.animationClips?.Count == 0)
            {
                Debug.LogWarning("技能配置中没有动画轨道数据");
                return false;
            }

            currentSkillConfig = skillConfig;
            isPreviewing = true;
            isEditModePreview = !Application.isPlaying;

            // 计算开始时间
            double currentFrameTime = SkillEditorData.CurrentFrame / (double)skillConfig.frameRate;
            previewStartTime = EditorApplication.timeSinceStartup - currentFrameTime;
            currentPreviewFrame = SkillEditorData.CurrentFrame;

            try
            {
                if (isEditModePreview)
                {
                    AnimationMode.StartAnimationMode();
                }
                else if (!HasSkillState())
                {
                    Debug.LogWarning("Animator状态机中没有找到Skill状态");
                    return false;
                }

                EditorApplication.update += UpdatePreview;
                Debug.Log($"开始预览技能动画: {skillConfig.skillName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"启动预览时出错: {e.Message}");
                isPreviewing = false;
                return false;
            }
        }

        /// <summary>
        /// 停止预览
        /// </summary>
        public void StopPreview()
        {
            if (!isPreviewing) return;

            isPreviewing = false;
            EditorApplication.update -= UpdatePreview;

            if (isEditModePreview)
            {
                AnimationMode.StopAnimationMode();
                isEditModePreview = false;
            }

            currentSkillConfig = null;
            currentPreviewFrame = 0;
            currentPreviewClip = null;

            Debug.Log("停止动画预览");
        }

        /// <summary>
        /// 跳转到指定帧进行预览
        /// </summary>
        /// <param name="frame">目标帧</param>
        public void PreviewFrame(int frame)
        {
            if (!isPreviewing || currentSkillConfig == null || previewAnimator == null)
                return;

            currentPreviewFrame = Mathf.Clamp(frame, 0, currentSkillConfig.maxFrames);

            // 检查动画轨道是否为激活状态
            var animationTrack = currentSkillConfig.trackContainer?.animationTrack;
            if (animationTrack == null || !animationTrack.isEnabled)
                return;

            // 检查轨道中的动画片段
            if (animationTrack.animationClips != null)
            {
                foreach (var animClip in animationTrack.animationClips)
                {
                    if (animClip.clip == null) continue;

                    // 检查当前帧是否在动画片段的播放范围内
                    int clipEndFrame = animClip.startFrame + Mathf.RoundToInt(animClip.clip.length * currentSkillConfig.frameRate);
                    if (currentPreviewFrame >= animClip.startFrame && currentPreviewFrame < clipEndFrame)
                    {
                        float playTime = CalculateClipTime(animClip);
                        PlayAnimationAtTime(animClip.clip, playTime);
                        return; // 找到第一个匹配的动画片段后返回
                    }
                }
            }
        }

        /// <summary>
        /// 计算动画片段播放时间
        /// </summary>
        private float CalculateClipTime(FFramework.Kit.AnimationTrack.AnimationClip animClip)
        {
            float currentTime = currentPreviewFrame / currentSkillConfig.frameRate;
            float clipStartTime = animClip.startFrame / currentSkillConfig.frameRate;

            if (currentTime < clipStartTime) return 0f;

            float localTime = currentTime - clipStartTime;
            float clipDuration = animClip.clip.length;
            return Mathf.Clamp(localTime, 0f, clipDuration);
        }

        /// <summary>
        /// 在指定时间播放动画
        /// </summary>
        private void PlayAnimationAtTime(AnimationClip clip, float time)
        {
            currentPreviewClip = clip;

            if (isEditModePreview)
            {
                AnimationMode.SampleAnimationClip(previewTarget.gameObject, clip, time);
            }
            else
            {
                if (HasSkillState())
                {
                    UpdateSkillStateClip(clip);
                    float normalizedTime = clip.length > 0 ? time / clip.length : 0f;
                    previewAnimator.Play("Skill", 0, normalizedTime);
                }
            }
        }

        /// <summary>
        /// 设置预览播放速度
        /// </summary>
        /// <param name="speed">播放速度倍数</param>
        public void SetPreviewSpeed(float speed)
        {
            if (isEditModePreview || previewAnimator == null) return;

            bool wasPlaying = previewAnimator.speed > 0f;
            previewAnimator.speed = speed;

            // 如果从暂停恢复播放，重新计算开始时间
            if (!wasPlaying && speed > 0f && isPreviewing && currentSkillConfig != null)
            {
                double frameTime = currentPreviewFrame / currentSkillConfig.frameRate;
                previewStartTime = EditorApplication.timeSinceStartup - frameTime;
            }
        }

        /// <summary>
        /// 重置预览到第0帧
        /// </summary>
        public void ResetPreview()
        {
            if (isPreviewing) PreviewFrame(0);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查Animator是否有Skill状态
        /// </summary>
        /// <returns>是否存在Skill状态</returns>
        private bool HasSkillState()
        {
            if (previewAnimator == null || previewAnimator.runtimeAnimatorController == null)
                return false;

            var controller = previewAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller == null) return false;

            // 检查第一层是否有Skill状态
            if (controller.layers.Length > 0)
            {
                var layer = controller.layers[0];
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name == "Skill")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 更新Skill状态的动画片段
        /// </summary>
        /// <param name="clip">要设置的动画片段</param>
        private void UpdateSkillStateClip(AnimationClip clip)
        {
            if (previewAnimator == null || previewAnimator.runtimeAnimatorController == null)
                return;

            var controller = previewAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller == null) return;

            // 查找Skill状态并更新其动画片段
            if (controller.layers.Length > 0)
            {
                var layer = controller.layers[0];
                foreach (var childState in layer.stateMachine.states)
                {
                    if (childState.state.name == "Skill")
                    {
                        // 更新状态的动画片段
                        childState.state.motion = clip;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 更新预览（编辑器更新回调）
        /// </summary>
        private void UpdatePreview()
        {
            if (!isPreviewing || currentSkillConfig == null || previewAnimator == null)
            {
                StopPreview();
                return;
            }

            // 检查是否应该播放
            bool shouldPlay = isEditModePreview ? SkillEditorData.IsPlaying : previewAnimator.speed > 0f;
            if (!shouldPlay) return;

            // 计算目标帧
            double elapsedTime = EditorApplication.timeSinceStartup - previewStartTime;
            int targetFrame = Mathf.FloorToInt((float)(elapsedTime * currentSkillConfig.frameRate));

            // 处理循环或结束
            if (targetFrame >= currentSkillConfig.maxFrames)
            {
                if (SkillEditorData.IsLoop)
                {
                    previewStartTime = EditorApplication.timeSinceStartup;
                    targetFrame = 0;
                }
                else
                {
                    targetFrame = currentSkillConfig.maxFrames;
                    if (currentPreviewFrame != targetFrame)
                    {
                        PreviewFrame(targetFrame);
                        SkillEditorEvent.TriggerCurrentFrameChanged(targetFrame);
                    }
                    // 非循环播放到最后一帧时，结束播放状态
                    SkillEditorData.IsPlaying = false;
                    SkillEditorEvent.TriggerPlayStateChanged(false);
                    StopPreview();
                    return;
                }
            }

            // 更新帧
            if (targetFrame != currentPreviewFrame)
            {
                PreviewFrame(targetFrame);
                SkillEditorEvent.TriggerCurrentFrameChanged(targetFrame);
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            StopPreview();
            previewTarget = null;
            previewAnimator = null;
            currentSkillConfig = null;
            currentPreviewClip = null;
        }

        #endregion
    }
}
