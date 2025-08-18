using FFramework.Kit;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;

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

        /// <summary>当前预览的动画片段</summary>
        private AnimationClip currentPreviewClip;

        /// <summary>全局播放速度倍数</summary>
        private float globalPlaySpeedMultiplier = 1f;

        private PlayableGraph playableGraph;
        private AnimationPlayableOutput animationOutput;
        private AnimationClipPlayable currentClipPlayable;

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
            if (animator == null)
            {
                Debug.LogWarning($"预览目标 {target.name} 没有有效的 Animator 组件");
                return false;
            }

            previewTarget = target;
            previewAnimator = animator;

            // 初始化 PlayableGraph
            if (!playableGraph.IsValid())
            {
                playableGraph = PlayableGraph.Create("SkillAnimationPreviewer");
                animationOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", previewAnimator);
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
            // SkillEditorData.IsPlaying = true; // 设置为播放状态
            if (SkillEditorData.IsPlaying)
            {
                // 计算开始时间
                double currentFrameTime = SkillEditorData.CurrentFrame / (double)skillConfig.frameRate;
                previewStartTime = EditorApplication.timeSinceStartup - currentFrameTime;
                currentPreviewFrame = SkillEditorData.CurrentFrame;
            }

            try
            {
                EditorApplication.update += UpdatePreview;
                Debug.Log($"开始预览技能动画: {skillConfig.skillName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"启动预览时出错: {e.Message}");
                isPreviewing = false;
                SkillEditorData.IsPlaying = false; // 设置为非播放状态
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
            SkillEditorData.IsPlaying = false; // 设置为非播放状态
            EditorApplication.update -= UpdatePreview;

            if (playableGraph.IsValid())
            {
                playableGraph.Stop();
            }

            currentSkillConfig = null;
            currentPreviewFrame = 0;
            currentPreviewClip = null;
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
                    int clipEndFrame = animClip.startFrame + animClip.durationFrame; // 使用配置中的持续帧数
                    if (currentPreviewFrame >= animClip.startFrame && currentPreviewFrame < clipEndFrame)
                    {
                        float playTime = CalculateClipTime(animClip);
                        PlayAnimationAtTime(animClip.clip, playTime, animClip);
                        return; // 找到第一个匹配的动画片段后返回
                    }
                }
            }
        }

        /// <summary>
        /// 计算动画片段播放时间,
        /// </summary>
        private float CalculateClipTime(FFramework.Kit.AnimationTrack.AnimationClip animClip)
        {
            float currentTime = currentPreviewFrame / currentSkillConfig.frameRate;
            float clipStartTime = animClip.startFrame / currentSkillConfig.frameRate;

            if (currentTime < clipStartTime) return 0f;

            float localTime = currentTime - clipStartTime;

            // 应用播放速度：播放速度越快，动画时间进度越快
            float scaledTime = localTime * animClip.animationPlaySpeed;

            float clipDuration = animClip.clip.length;

            // 如果是循环播放，使用模运算来循环时间
            if (clipDuration > 0f)
            {
                scaledTime = scaledTime % clipDuration;
            }

            return Mathf.Clamp(scaledTime, 0f, clipDuration);
        }

        /// <summary>
        /// 在指定时间播放动画
        /// </summary>
        private void PlayAnimationAtTime(AnimationClip clip, float time, FFramework.Kit.AnimationTrack.AnimationClip animClipData = null)
        {
            currentPreviewClip = clip;

            if (!playableGraph.IsValid())
            {
                Debug.LogError("PlayableGraph 无效，无法播放动画");
                return;
            }

            if (currentClipPlayable.IsValid())
            {
                currentClipPlayable.Destroy();
            }

            currentClipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
            currentClipPlayable.SetTime(time);
            currentClipPlayable.SetSpeed(globalPlaySpeedMultiplier * (animClipData?.animationPlaySpeed ?? 1f));

            animationOutput.SetSourcePlayable(currentClipPlayable);

            // 如果当前编辑器不是播放状态（即手动拖拽/暂停），直接 Evaluate 以立刻应用指定时间的姿态
            if (!SkillEditorData.IsPlaying)
            {
                // 确保 graph 已经准备好，然后 Evaluate 以更新 Animator 到指定时间点
                if (!playableGraph.IsPlaying())
                {
                    playableGraph.Play(); // 需要先 Play 一次以让 Evaluate 生效（速度为0时不会推进）
                }
                playableGraph.Evaluate();
            }
            else
            {
                if (!playableGraph.IsPlaying())
                {
                    playableGraph.Play();
                }
            }
        }

        /// <summary>
        /// 设置预览播放速度（全局速度倍数，会叠加到动画片段的播放速度上）
        /// </summary>
        /// <param name="speed">全局播放速度倍数</param>
        public void SetPreviewSpeed(float speed)
        {
            globalPlaySpeedMultiplier = speed;

            if (playableGraph.IsValid() && currentClipPlayable.IsValid())
            {
                currentClipPlayable.SetSpeed(speed);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新预览（编辑器更新回调）
        /// </summary>
        private void UpdatePreview()
        {
            if (!isPreviewing || currentSkillConfig == null || previewAnimator == null || !SkillEditorData.IsPlaying)
            {
                return;
            }

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

            if (playableGraph.IsValid())
            {
                playableGraph.Destroy(); // 在清理时销毁 PlayableGraph
            }
            previewTarget = null;
            previewAnimator = null;
            currentSkillConfig = null;
            currentPreviewClip = null;
        }

        #endregion
    }
}
