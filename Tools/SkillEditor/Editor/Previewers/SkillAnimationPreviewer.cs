using UnityEngine;
using UnityEditor;
using FFramework.Kit;

namespace SkillEditor
{
    /// <summary>
    /// 技能动画预览管理器
    /// 负责在编辑模式下预览技能动画
    /// </summary>
    public class SkillAnimationPreviewer
    {
        #region 私有字段

        /// <summary>当前预览的游戏对象</summary>
        private GameObject previewTarget;

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
        public GameObject PreviewTarget => previewTarget;

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置预览目标
        /// </summary>
        /// <param name="target">要预览的游戏对象</param>
        /// <returns>是否设置成功</returns>
        public bool SetPreviewTarget(GameObject target)
        {
            if (target == null)
            {
                previewTarget = null;
                previewAnimator = null;
                Debug.Log("清空预览目标");
                return false;
            }

            // 检查目标是否有Animator组件
            var animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"预览目标 {target.name} 没有Animator组件");
                return false;
            }

            // 检查Animator是否有控制器
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"预览目标 {target.name} 的Animator没有设置控制器！请确保已经分配了AnimatorController");
                return false;
            }

            previewTarget = target;
            previewAnimator = animator;

            Debug.Log($"设置预览目标: {target.name}, 控制器: {animator.runtimeAnimatorController.name}");

            // 验证Skill状态是否存在
            if (HasSkillState())
            {
                Debug.Log("已找到Skill状态，可以开始预览");
            }
            else
            {
                Debug.LogWarning($"控制器 {animator.runtimeAnimatorController.name} 中没有找到'Skill'状态，请确保控制器中包含名为'Skill'的状态");
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
            if (skillConfig == null)
            {
                Debug.LogWarning("技能配置为空，无法开始预览");
                return false;
            }

            if (previewAnimator == null)
            {
                Debug.LogWarning("没有设置预览目标，无法开始预览");
                return false;
            }

            // 检查是否有动画轨道数据
            if (skillConfig.trackContainer?.animationTrack == null ||
                skillConfig.trackContainer.animationTrack.animationClips == null ||
                skillConfig.trackContainer.animationTrack.animationClips.Count == 0)
            {
                Debug.LogWarning("技能配置中没有动画轨道数据");
                return false;
            }

            currentSkillConfig = skillConfig;
            isPreviewing = true;

            // 根据当前帧计算正确的开始时间，使播放从当前帧开始
            double currentFrameTime = SkillEditorData.CurrentFrame / (double)skillConfig.frameRate;
            previewStartTime = EditorApplication.timeSinceStartup - currentFrameTime;
            currentPreviewFrame = SkillEditorData.CurrentFrame;

            // 判断是编辑模式还是运行模式
            if (Application.isPlaying)
            {
                // 运行时模式：使用Animator播放
                return StartRuntimePreview();
            }
            else
            {
                // 编辑模式：使用AnimationMode
                return StartEditModePreview();
            }
        }

        /// <summary>
        /// 开始运行时预览
        /// </summary>
        private bool StartRuntimePreview()
        {
            isEditModePreview = false;

            // 重新检查Animator控制器是否存在（可能在运行时被清空）
            if (previewAnimator.runtimeAnimatorController == null)
            {
                Debug.LogError($"预览开始时发现Animator控制器丢失！请重新设置预览目标或检查 {previewTarget?.name} 的Animator设置");
                return false;
            }

            // 进入Skill状态
            if (HasSkillState())
            {
                try
                {
                    previewAnimator.Play("Skill", 0, 0f);
                    // 设置播放速度为1，确保能正常播放
                    previewAnimator.speed = 1.0f;
                    Debug.Log($"开始运行时预览技能动画: {currentSkillConfig.skillName}");

                    // 订阅编辑器更新事件
                    EditorApplication.update += UpdatePreview;
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"播放Skill状态时出错: {e.Message}");
                    isPreviewing = false;
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Animator状态机中没有找到Skill状态");
                isPreviewing = false;
                return false;
            }
        }

        /// <summary>
        /// 开始编辑模式预览
        /// </summary>
        private bool StartEditModePreview()
        {
            isEditModePreview = true;

            try
            {
                // 启动AnimationMode
                AnimationMode.StartAnimationMode();
                Debug.Log($"开始编辑模式预览技能动画: {currentSkillConfig.skillName}");

                // 订阅编辑器更新事件
                EditorApplication.update += UpdatePreview;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"启动编辑模式预览时出错: {e.Message}");
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
            currentSkillConfig = null;
            currentPreviewFrame = 0;
            currentPreviewClip = null;

            // 取消订阅编辑器更新事件
            EditorApplication.update -= UpdatePreview;

            // 如果是编辑模式预览，停止AnimationMode
            if (isEditModePreview)
            {
                AnimationMode.StopAnimationMode();
                isEditModePreview = false;
                Debug.Log("停止编辑模式动画预览");
            }
            else
            {
                Debug.Log("停止运行时动画预览");
            }
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

            // 获取当前帧的动画数据
            var frameData = currentSkillConfig.GetTrackDataAtFrame(currentPreviewFrame);

            if (isEditModePreview)
            {
                // 编辑模式预览
                PreviewFrameInEditMode(frameData);
            }
            else
            {
                // 运行时预览
                PreviewFrameInRuntime(frameData, frame);
            }
        }

        /// <summary>
        /// 编辑模式下的帧预览
        /// </summary>
        private void PreviewFrameInEditMode(FFramework.Kit.FrameTrackData frameData)
        {
            if (frameData.animationClips.Count > 0)
            {
                var animClip = frameData.animationClips[0]; // 取第一个动画片段
                if (animClip.clip != null)
                {
                    // 计算动画片段中的播放时间
                    float currentTime = currentPreviewFrame / currentSkillConfig.frameRate;
                    float clipStartTime = animClip.startFrame / currentSkillConfig.frameRate;
                    float clipDuration = animClip.clip.length;

                    if (currentTime >= clipStartTime)
                    {
                        float localTime = currentTime - clipStartTime;
                        float normalizedTime = Mathf.Clamp01(localTime / clipDuration);
                        float clipTime = normalizedTime * clipDuration;

                        // 如果动画片段改变了，需要重新采样
                        if (currentPreviewClip != animClip.clip)
                        {
                            currentPreviewClip = animClip.clip;
                        }

                        // 使用AnimationMode采样动画
                        AnimationMode.SampleAnimationClip(previewTarget, animClip.clip, clipTime);

                        // Debug.Log($"编辑模式预览帧 {currentPreviewFrame}: 动画 {animClip.clip.name}, 时间 {clipTime:F3}s");
                    }
                }
            }
            else
            {
                // 没有动画片段时，重置到默认状态
                if (currentPreviewClip != null)
                {
                    AnimationMode.SampleAnimationClip(previewTarget, currentPreviewClip, 0f);
                }
            }
        }

        /// <summary>
        /// 运行时模式下的帧预览
        /// </summary>
        private void PreviewFrameInRuntime(FFramework.Kit.FrameTrackData frameData, int frame)
        {
            // 检查Animator控制器是否存在
            if (previewAnimator.runtimeAnimatorController == null)
            {
                Debug.LogError($"预览对象 {previewTarget?.name} 的Animator没有AnimatorController，无法预览动画");
                return;
            }

            // 如果当前帧有动画片段，则播放对应动画
            if (frameData.animationClips.Count > 0)
            {
                var animClip = frameData.animationClips[0]; // 取第一个动画片段
                if (animClip.clip != null)
                {
                    // 计算动画片段中的播放时间
                    float currentTime = currentPreviewFrame / currentSkillConfig.frameRate;

                    // 计算在动画片段中的归一化时间
                    float clipStartTime = animClip.startFrame / currentSkillConfig.frameRate;
                    float clipDuration = animClip.clip.length;

                    if (currentTime >= clipStartTime)
                    {
                        float localTime = currentTime - clipStartTime;
                        float normalizedTime = Mathf.Clamp01(localTime / clipDuration);

                        // 设置Animator控制器中Skill状态的动画片段
                        UpdateSkillStateClip(animClip.clip);

                        // 检查Skill状态是否存在后再播放
                        if (HasSkillState())
                        {
                            previewAnimator.Play("Skill", 0, normalizedTime);
                            Debug.Log($"运行时预览帧 {frame}: 动画 {animClip.clip.name}, 时间 {normalizedTime:F3}");
                        }
                        else
                        {
                            Debug.LogWarning("Skill状态不存在，无法播放动画");
                        }
                    }
                }
            }
            else
            {
                // 如果没有动画片段，检查Skill状态是否存在后再播放
                if (HasSkillState())
                {
                    previewAnimator.Play("Skill", 0, 0f);
                }
            }
        }

        /// <summary>
        /// 设置预览播放速度
        /// </summary>
        /// <param name="speed">播放速度倍数</param>
        public void SetPreviewSpeed(float speed)
        {
            if (isEditModePreview)
            {
                // 编辑模式下不需要设置速度，因为是手动控制帧的
                return;
            }

            if (previewAnimator != null)
            {
                bool wasPlaying = previewAnimator.speed > 0f;
                previewAnimator.speed = speed;

                // 如果从暂停状态恢复到播放状态，重新计算开始时间
                if (!wasPlaying && speed > 0f && isPreviewing && currentSkillConfig != null)
                {
                    // 根据当前帧重新计算开始时间，避免跳帧
                    double frameTime = currentPreviewFrame / currentSkillConfig.frameRate;
                    previewStartTime = EditorApplication.timeSinceStartup - frameTime;
                }
            }
        }

        /// <summary>
        /// 重置预览到第0帧
        /// </summary>
        public void ResetPreview()
        {
            if (isPreviewing)
            {
                PreviewFrame(0);
            }
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

            // 编辑模式下，只有播放速度大于0才自动更新帧
            if (isEditModePreview)
            {
                // 编辑模式下的播放逻辑
                UpdateEditModePreview();
            }
            else
            {
                // 运行时模式下的播放逻辑
                UpdateRuntimePreview();
            }
        }

        /// <summary>
        /// 更新编辑模式预览
        /// </summary>
        private void UpdateEditModePreview()
        {
            // 如果不在播放状态，不自动推进帧数
            if (!SkillEditorData.IsPlaying)
            {
                return;
            }

            // 计算当前应该显示的帧
            double elapsedTime = EditorApplication.timeSinceStartup - previewStartTime;
            int targetFrame = Mathf.FloorToInt((float)(elapsedTime * currentSkillConfig.frameRate));

            // 处理循环播放或结束条件
            if (targetFrame >= currentSkillConfig.maxFrames)
            {
                if (SkillEditorData.IsLoop)
                {
                    previewStartTime = EditorApplication.timeSinceStartup;
                    targetFrame = 0;
                }
                else
                {
                    // 确保最后一帧能够显示
                    targetFrame = currentSkillConfig.maxFrames;
                    if (currentPreviewFrame != targetFrame)
                    {
                        PreviewFrame(targetFrame);
                        SkillEditorEvent.TriggerCurrentFrameChanged(targetFrame);
                    }
                    // 在显示最后一帧后停止预览
                    StopPreview();
                    return;
                }
            }

            // 更新当前帧
            if (targetFrame != currentPreviewFrame)
            {
                PreviewFrame(targetFrame);
                // 触发帧变更事件，同步时间轴显示
                SkillEditorEvent.TriggerCurrentFrameChanged(targetFrame);
            }
        }

        /// <summary>
        /// 更新运行时预览
        /// </summary>
        private void UpdateRuntimePreview()
        {
            // 如果播放速度为0（暂停状态），不更新帧
            if (previewAnimator.speed <= 0f)
            {
                return;
            }

            // 计算当前应该显示的帧
            double elapsedTime = EditorApplication.timeSinceStartup - previewStartTime;
            int targetFrame = Mathf.FloorToInt((float)(elapsedTime * currentSkillConfig.frameRate));

            // 处理循环播放或结束条件
            if (targetFrame >= currentSkillConfig.maxFrames)
            {
                if (SkillEditorData.IsLoop)
                {
                    previewStartTime = EditorApplication.timeSinceStartup;
                    targetFrame = 0;
                }
                else
                {
                    // 确保最后一帧能够显示
                    targetFrame = currentSkillConfig.maxFrames;
                    if (currentPreviewFrame != targetFrame)
                    {
                        PreviewFrame(targetFrame);
                        SkillEditorEvent.TriggerCurrentFrameChanged(targetFrame);
                    }
                    // 在显示最后一帧后停止预览
                    StopPreview();
                    return;
                }
            }

            // 更新当前帧
            if (targetFrame != currentPreviewFrame)
            {
                PreviewFrame(targetFrame);

                // 触发帧变更事件，同步时间轴显示
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
